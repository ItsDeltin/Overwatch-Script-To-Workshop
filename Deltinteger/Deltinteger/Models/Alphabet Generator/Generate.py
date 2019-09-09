import os, bpy

exportFolder = ""

objects = bpy.data.objects
for object in objects:
    bpy.ops.object.select_all(action='DESELECT')
    object.select_set(True)
    exportName = exportFolder + object.name + '.obj'
    bpy.ops.export_scene.obj(filepath=exportName, use_selection=True)
    os.remove(exportFolder + object.name + '.mtl')