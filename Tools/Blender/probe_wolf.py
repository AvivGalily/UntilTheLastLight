import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Vendor/newdlc-wolf-cc0/dog2.blend'),load_ui=False,use_scripts=False)
for ob in bpy.context.scene.objects:
    if ob.type not in {'ARMATURE','MESH'}:continue
    print('OBJECT',ob.name,'WORLD',list(map(list,ob.matrix_world)),'BOUNDS',[list(ob.matrix_world@Vector(c)) for c in ob.bound_box],flush=True)
    if ob.type=='ARMATURE':
        for b in ob.data.bones:
            print('BONE',b.name,'HEAD',list(ob.matrix_world@b.head_local),'TAIL',list(ob.matrix_world@b.tail_local),flush=True)
    else:print('MATS',[(m.name,[(n.type,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes] if m.use_nodes else []) for m in ob.data.materials],flush=True)
