import bpy
import json
import sys
from vector import JsonVector, get_matrix, get_accumulative_local_matrix
from rna_traveler import Rna_traveler
from mathutils import Matrix, Quaternion, Vector

def load(fp):
    bpy.ops.wm.open_mainfile(filepath = fp, load_ui=False)

def debug():
    load('C:/Users/Deltin/Documents/Blender/Web.blend')

class tree_item:
    def __init__(self):
        self.children = []
        self.parent = -1

# The root of the blend file data.
class blend_file:
    def __init__(self, objects):
        self.objects = objects

# The base, abstract class for objects in the scene.
class blend_object(tree_item):
    def __init__(self, name, animation_data):
        super(blend_object, self).__init__()
        self.name = name
        self.animation_data = animation_data

# A generic mesh, inherits blend_object.
class blend_mesh(blend_object):
    def __init__(self, name, animation_data, vertex_groups, vertices, edges):
        super(blend_mesh, self).__init__(name, animation_data)
        self.vertex_groups = vertex_groups
        self.vertices = vertices
        self.edges = edges

class vertex_group:
    def __init__(self, index, name):
        self.index = index
        self.name = name

class vertex_group_element:
    def __init__(self, group, weight):
        self.group = group
        self.weight = weight

class vertex:
    def __init__(self, co, groups):
        self.co = co
        self.groups = groups

class edge:
    def __init__(self, v1, v2):
        self.v1 = v1
        self.v2 = v2

class blend_armature(blend_object):
    def __init__(self, name, animation_data, bones, empties):
        super(blend_armature, self).__init__(name, animation_data)
        self.bones = bones
        self.empties = empties

class bone(tree_item):
    def __init__(self, original):
        super(bone, self).__init__()
        self.name = original.name
        self.head = JsonVector(original.head, False)
        self.head_local = JsonVector(original.head_local, True)
        self.tail = JsonVector(original.tail, False)
        self.tail_local = JsonVector(original.tail_local, True)
        self.length = original.length
        self.matrix = get_matrix(get_accumulative_local_matrix(original))
        self.use_connect = original.use_connect

class empty:
    def __init__(self, original):
        self.name = original.name
        self.location = JsonVector(original.matrix_local.decompose()[0], False)
        self.location_local = JsonVector(original.matrix_basis.decompose()[0], True)
        self.parent_bone = original.parent_bone

class blend_action:
    def __init__(self, name, fcurves, frame_range):
        self.name = name
        self.fcurves = fcurves
        self.frame_range = frame_range

class fcurve:
    def __init__(self, keyframes, fcurve_type, target, krange):
        self.fcurve_type = fcurve_type
        self.keyframes = keyframes
        self.target = target
        self.range = krange

class keyframe:
    def __init__(self, start, value):
        self.start = start
        self.value = value

class tree_link_controller:
    def __init__(self):
        self.links = []

    def add(self, value, original):
        self.links.append(tree_link(value, original, len(self.links)))
    
    def link(self):
        for link in self.links:
            link.link(self.links)
        return [a.value for a in self.links]

class tree_link:
    def __init__(self, value, original, index):
        self.value = value
        self.original = original
        self.index = index
    
    def link(self, all_links):
        self.value.children = []

        for real_child in self.original.children:
            for i, link in enumerate(all_links):
                if link.original == real_child:
                    self.value.children.append(i)
                    link.value.parent = self.index
                    break

def get_project_json():
    try:
        file = get_blend_file()
        return json.dumps(file, default = serialize)
    except Exception as e:
        return str(e)

def get_blend_file():
    # Get the objects.
    object_linker = tree_link_controller()

    for object in bpy.context.scene.objects:
        newValue = get_object(object)
        if newValue != None:
            object_linker.add(newValue, object)
    
    return blend_file(object_linker.link())

def get_object(obj):
    # Mesh
    if isinstance(obj.data, bpy.types.Mesh):
        return get_mesh(obj)
    # Armature
    if isinstance(obj.data, bpy.types.Armature):
        return get_armature(obj)

    return None

def get_mesh(obj):
    # Get the vertex groups.
    vertex_groups = []
    for group in obj.vertex_groups:
        vertex_groups.append(vertex_group(group.index, group.name))

    # Get the vertices.
    vertices = []
    for vert in obj.data.vertices:
        vertices.append(vertex(JsonVector(vert.co, False), [vertex_group_element(a.group, a.weight) for a in vert.groups]))
    
    # Get the edges.
    edges = []
    for e in obj.data.edges:
        edges.append(edge(e.vertices[0], e.vertices[1]))
    
    # Return mesh data.
    return blend_mesh(obj.name, get_animation_data(obj), vertex_groups, vertices, edges)

def get_armature(obj):
    bone_linker = tree_link_controller()

    # Get the bones.
    for raw_bone in obj.data.bones:
        bone_linker.add(bone(raw_bone), raw_bone)
    
    # Get linked empties.
    empties = []
    for scene_object in bpy.context.scene.objects:
        if obj is scene_object.parent and scene_object.type == 'EMPTY' and scene_object.parent_type == 'BONE':
            empties.append(empty(scene_object))
    
    # Link the bone's children and parents.
    return blend_armature(obj.name, get_animation_data(obj), bone_linker.link(), empties)

def get_animation_data(obj):
    if not obj.animation_data:
        return None
    
    actions = []
    
    for track in obj.animation_data.nla_tracks:
        if len(track.strips) >= 1:
            actions.append(get_action(obj, track.strips[0].action))

    return actions

def get_action(obj, action):
    fcurves = get_action_animation_data(action, obj.parent == None)

    # Return the action.
    return blend_action(action.name, fcurves, JsonVector(action.frame_range, False))

def get_action_animation_data(action, is_root):
    groups = {}
    
    for curve in action.fcurves:
        groups.setdefault(curve.data_path, [])
        groups[curve.data_path].append(curve)
    
    fcurves = []

    for group_name in groups:
        group = groups[group_name]

        keyframes = []
        targetType, target = Rna_traveler(group[0].data_path).scan()
        rng = JsonVector(group[0].range(), False)

        if targetType == -1:
            continue

        for k in group[0].keyframe_points:
            # The target frame of the keyframe.
            frame = k.co.x

            # Get the value.
            value = None
        
            if targetType == 0: # Location
                pass
            elif targetType == 1: # Bone rotation
                value = JsonVector(Quaternion((
                    group[0].evaluate(frame),
                    group[1].evaluate(frame),
                    group[2].evaluate(frame),
                    group[3].evaluate(frame)
                )), False)
            elif targetType == 2: # Bone location
                value = JsonVector(Vector((
                    group[0].evaluate(frame),
                    group[1].evaluate(frame),
                    group[2].evaluate(frame)
                )), False)
                #)), is_root)
                
            # Append the keyframe.
            if value is not None:
                keyframes.append(keyframe(frame, value))

        fcurves.append(fcurve(keyframes, targetType, target, rng))
    
    return fcurves

def serialize(obj):
    if hasattr(obj, '__dict__'):
        return obj.__dict__
    return None

load(input())
# load('C:/Users/Deltin/Documents/Blender/Web.blend')
print(get_project_json())

# run command: 'Python: Start REPL'
# 'import blender_interface'
# 'import bpy'
# bpy.context.scene.objects[#].data.bones[#].matrix.to_quaternion() # Original
# bpy.context.scene.objects[#].pose.bones[#].matrix.to_quaternion() # Current Frame Pose
# bpy.context.scene.objects[2].pose :eyes:

# TO FUTURE DELTIN - fcurve info:
# keyframe_points[0] is the start, keyframe_points[1] is the end.
#   In the event where there may be more than 2 or less than 2 __len__(), pray and repent
# The X of the co is the start frame. The purpose of Y is unknown, sorry guy
# Execute 'bpy.context.scene.frame_set(co.x)' then get the source bone quaternion data with the fcurve's data path.
#
# To get fcurve source:
# fcurve.data_path, fcurve.array_index
#
# https://blender.stackexchange.com/questions/81085/get-bones-associated-with-specific-action-in-python
# https://docs.blender.org/api/blender_python_api_current/bpy.types.bpy_struct.html#bpy.types.bpy_struct.path_resolve
#
# this doesn't seem like the correct way, but it seems to be the only way