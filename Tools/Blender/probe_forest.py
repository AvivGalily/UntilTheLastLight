import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'ArtSource/Vendor/polyhaven'
for slug in ['rock_moss_set_01','dead_tree_trunk','tree_stump_02','fir_tree_01']:
    bpy.ops.wm.open_mainfile(filepath=str(root/slug/(slug+'.blend')))
    print('SOURCE',slug,flush=True)
    for col in bpy.data.collections:print('COLLECTION',col.name,len(col.objects),flush=True)
    for ob in bpy.context.scene.objects:
        if ob.type=='MESH':print('OBJECT',ob.name,'v',len(ob.data.vertices),'dims',tuple(round(x,2) for x in ob.dimensions),'collections',[c.name for c in ob.users_collection],'materials',[m.name if m else '-' for m in ob.data.materials], 'mods',[(m.type,m.name) for m in ob.modifiers],flush=True)
