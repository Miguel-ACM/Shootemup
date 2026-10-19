import bpy
bpy.context.scene.render.engine = 'CYCLES'
bpy.context.scene.cycles.samples = 24
bpy.ops.render.render(write_still=True)
print(bpy.context.scene.render.filepath)
