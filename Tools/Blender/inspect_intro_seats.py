"""Inspect source rig landmarks in Unity metres to calibrate physical car seating."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[2]
for identity in ('jack','tyler','ethan'):
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Blender'/('intro_'+identity+'_01.blend')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    landmarks={}
    for key in ('head','neck01','spine01','pelvis','root','upperleg01.L','foot.L'):
        if key not in rig.data.bones:continue
        p=rig.matrix_world@rig.data.bones[key].head_local
        landmarks[key]=[round(p.x,3),round(p.z,3),round(-p.y,3)]
    print(identity,landmarks,flush=True)
