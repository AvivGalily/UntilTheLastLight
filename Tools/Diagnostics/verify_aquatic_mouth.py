"""Check the authored mouth has an opaque recessed interior, not a skin plug.

Run with Tools/Run-BlenderStep.ps1; the URP frontal/profile/attack views remain
the visual check. These rays reproduce the frontal obstruction found in review.
"""
import json
import sys
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/Blender'))
from forest_bodies import C

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Blender/research_abomination_01.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Aquatic_Idle']
bpy.context.scene.frame_set(8)
deps=bpy.context.evaluated_depsgraph_get()
measurements=[]
for x in (0,.08):
    for y in (1.87,1.89):
        hit,location,normal,index,obj,matrix=bpy.context.scene.ray_cast(deps,C((x,y,.99)),C((0,0,-1)))
        assert hit, 'The oral lining has a hole'
        evaluated=obj.evaluated_get(deps)
        material=evaluated.data.materials[evaluated.data.polygons[index].material_index].name
        z=-location.y
        assert material=='AbyssThroat', 'Flesh obstructs the mouth interior: '+material
        assert .025<z<.16, 'The oral lining is not recessed behind the dental arch: '+str(z)
        measurements.append({'x':x,'y':y,'interiorZ':z,'material':material})
receipt={'passed':True,'fixture':'R09 Idle at frame 8, front rays behind incisors and lateral teeth','measurements':measurements}
(ROOT/'Logs/AquaticMouthVerification.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print('AQUATIC MOUTH DEPTH PASS:',json.dumps(measurements),flush=True)
