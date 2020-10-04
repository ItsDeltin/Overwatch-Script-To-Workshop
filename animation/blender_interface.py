import bpy
import json
import sys
from vector import Vector
from rna_traveler import Rna_traveler

def load(fp):
    bpy.ops.wm.open_mainfile(filepath = fp, load_ui=False)

def debug():
    # load('C:/Users/Deltin/Downloads/hook4.blend')
    load('C:/Users/Deltin/Documents/Blender/debug.blend')

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
    def __init__(self, name, animation_data, bones):
        super(blend_armature, self).__init__(name, animation_data)
        self.bones = bones

class bone(tree_item):
    def __init__(self, original):
        super(bone, self).__init__()
        self.name = original.name
        self.head = Vector(original.head)
        self.head_local = Vector(original.head_local)
        self.tail = Vector(original.tail)
        self.tail_local = Vector(original.tail_local)
        self.length = original.length
        self.rotation = Vector(original.matrix.to_quaternion())
        self.use_connect = original.use_connect

class blend_action:
    def __init__(self, name, fcurves):
        self.name = name
        self.fcurves = fcurves

class fcurve:
    def __init__(self, keyframes, fcurve_type, target):
        self.fcurve_type = fcurve_type
        self.keyframes = keyframes
        self.target = target

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
    file = get_blend_file()
    return json.dumps(file, default = serialize)

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
        vertices.append(vertex(Vector(vert.co), [vertex_group_element(a.group, a.weight) for a in vert.groups]))
    
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
    
    # Link the bone's children and parents.
    return blend_armature(obj.name, get_animation_data(obj), bone_linker.link())

def get_animation_data(obj):
    if not obj.animation_data or not obj.animation_data.action:
        return None
    
    action = obj.animation_data.action
    fcurves = []
    for fc in action.fcurves:
        # Get RNA data.
        value_handler = Rna_traveler(fc.data_path, fc.array_index).scan()

        # The array of keyframes. There should usually be just one.
        keyframes = []

        for k in fc.keyframe_points:
            # The target frame of the keyframe.
            frame = k.co.x

            # Set the current frame.
            bpy.context.scene.frame_set(frame)

            # Get the value.
            keyframe_value = value_handler.get_value(obj)

            # Append the keyframe.
            keyframes.append(keyframe(frame, keyframe_value))
        
        # Append the fcurve.
        fcurves.append(fcurve(keyframes, value_handler.get_type(), value_handler.get_target(obj)))
    
    # Return the action.
    return blend_action(action.name, fcurves)

def serialize(obj):
    return obj.__dict__

load(input())
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