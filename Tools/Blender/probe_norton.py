import bpy,sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
for source in ['ArtSource/Vendor/creomoto/car.blend','ArtSource/Blender/hanging_survivor_01.blend']:
    bpy.ops.wm.open_mainfile(filepath=str(root/source),load_ui=False,use_scripts=False)
    print('SOURCE',source,flush=True)
    for o in bpy.context.scene.objects:
        if o.type=='MESH':print(o.name,len(o.data.vertices),tuple(round(a,3) for a in o.dimensions),[m.name for m in o.data.materials if m],flush=True)
        if o.type=='ARMATURE':
            for b in o.data.bones:
                if b.name in ['head','neck01','neck02','jaw'] or 'jaw' in b.name.lower():print('BONE',b.name,tuple(b.head_local),tuple(b.tail_local),flush=True)
