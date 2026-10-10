import bpy,sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(next((ROOT/'ArtSource/Vendor/mpfb-2.0.17').glob('*/src'))))
import addon_utils
# Local authoring-only addon session; no installation or saved user preferences.
bpy.utils.extension_path_user=lambda package,path='',create=False:str(ROOT/'ArtSource/Vendor/mpfb-workspace'/path)
addon_utils.enable('mpfb',default_set=True)
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict();macro['gender']=1;macro['age']=.6
human=HumanService.create_human(macro_detail_dict=macro)
rig=HumanService.add_builtin_rig(human,'default')
print('HUMAN',human.name,len(human.data.vertices),list(human.dimensions),flush=True)
print('BONES',[(p.name,list(p.head),list(p.tail)) for p in rig.data.bones],flush=True)
print('TARGETS',list((next((ROOT/'ArtSource/Vendor/mpfb-2.0.17').glob('*/src'))/'mpfb/data').glob('*')),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Vendor/human-probe.blend'))
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Vendor/newdlc-wolf-cc0/dog2.blend'),load_ui=False,use_scripts=False)
for ob in bpy.data.objects:
    print('WOLF OBJECT',ob.name,ob.type,list(ob.dimensions),list(ob.location),flush=True)
    if ob.type=='ARMATURE':print('WOLF BONES',[(b.name,list(b.head),list(b.tail)) for b in ob.data.bones],flush=True)
print('WOLF ACTIONS',[(a.name,list(a.frame_range)) for a in bpy.data.actions],flush=True)
