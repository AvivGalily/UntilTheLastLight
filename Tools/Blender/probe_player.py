import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Blender/hanging_survivor_01.blend'),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rig.animation_data_clear()
for b in rig.pose.bones:b.matrix_basis.identity()
bpy.context.view_layer.update()
for b in rig.data.bones:
    if any(w in b.name for w in ['arm','hand','finger','spine','root','leg','head']):print('BONE',b.name,'HEAD',list(rig.matrix_world@b.head_local),'TAIL',list(rig.matrix_world@b.tail_local),flush=True)
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        print('MESH',o.name,len(o.data.vertices),[m.name for m in o.data.materials],flush=True)
        region=[v for v in o.data.vertices if v.co.x<-.43 and .88<v.co.z<1.12]
        print('PALM GROUPS',sorted({o.vertex_groups[g.group].name for v in region for g in v.groups if g.weight>.05}),flush=True)
print('FOREARM CHILDREN',[b.name for b in rig.pose.bones['lowerarm01.R'].children_recursive],flush=True)
