import bpy,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
for asset in ['hanging_survivor_01','player_body_01','player_axe_hand_01']:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Blender'/f'{asset}.blend'),load_ui=False,use_scripts=False)
    print('ASSET',asset,flush=True)
    for o in bpy.context.scene.objects:
        if o.type=='MESH':
            coords=[o.matrix_world@v.co for v in o.data.vertices]
            print('MESH',o.name,'bounds',[(round(min(v[k] for v in coords),4),round(max(v[k] for v in coords),4)) for k in range(3)],flush=True)
        elif o.type=='ARMATURE':
            o.animation_data_clear()
            for b in o.pose.bones:b.matrix_basis.identity()
            bpy.context.view_layer.update()
            for n in ['root','pelvis','spine01','upperleg01.R','upperleg02.R','lowerleg01.R','lowerleg02.R','foot.R','upperarm01.R','lowerarm01.R']:
                if n in o.pose.bones:
                    b=o.pose.bones[n];print('BONE',n,tuple(round(x,3) for x in b.head),tuple(round(x,3) for x in b.tail),'parent',b.parent.name if b.parent else None,flush=True)
