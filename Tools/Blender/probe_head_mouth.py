import bpy,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
for name in ['hanging_survivor_01','cinder_head_01']:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Blender'/f'{name}.blend'),load_ui=False,use_scripts=False)
    for rig in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:
        rig.animation_data_clear()
        for b in rig.pose.bones:b.matrix_basis.identity()
    bpy.context.view_layer.update()
    print('SOURCE',name,flush=True)
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        print('MESH',o.name,[m.name for m in o.data.materials],flush=True)
        print('GROUPS',[(g.index,g.name) for g in o.vertex_groups if any(s in g.name.lower() for s in ['jaw','lip','mouth'])],flush=True)
        if name=='hanging_survivor_01' and o.name=='Human':
            for group in ['lips','joint-mouth','joint-jaw']:
                index=o.vertex_groups[group].index
                verts=[o.matrix_world@v.co for v in o.data.vertices if any(g.group==index and g.weight>.5 for g in v.groups)]
                if verts:print('MOUTH GROUP',group,'bounds',[(min(v[n] for v in verts),max(v[n] for v in verts)) for n in range(3)],flush=True)
        if name=='cinder_head_01':
            for z in [.56,.60,.64,.68,.72,.76,.8]:
                candidates=[v.co for v in o.data.vertices if abs(v.co.x)<.06 and abs(v.co.z-z)<.012 and v.co.y<-.15]
                if candidates:print('FRONT',z,[(round(v.y,4),round(v.z,4)) for v in sorted(candidates,key=lambda v:v.y)[:6]],flush=True)
