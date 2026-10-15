"""Reuse seated Tyler as the first corpse. Jack/Ethan are built by encounter_cast."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H

receipts=[]
for identity,name in ((1,'tyler'),):
    source='intro_'+name+'_01'
    bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/(source+'.blend')),load_ui=False,use_scripts=False)
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.animation_data_clear()
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    for side,sign in (('L',1),('R',-1)):
        for part in ('upperleg01','upperleg02','lowerleg01','lowerleg02'):H.point_bone(rig,part+'.'+side,(sign*.025,0,-1))
        H.point_bone(rig,'foot.'+side,(0,-.08,-.035))
        H.point_bone(rig,'upperarm01.'+side,(sign*(.24+identity*.04),-.06,-1))
        H.point_bone(rig,'lowerarm01.'+side,(sign*.2,-.12,-1))
    deps=bpy.context.evaluated_depsgraph_get();parts=[]
    rotation=Matrix.Rotation(math.pi,4,'Z')@Matrix.Rotation(-math.pi/2,4,'X')
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        mesh.transform(rotation@ob.matrix_world)
        copy=bpy.data.objects.new('Fallen '+name+' '+ob.name,mesh);bpy.context.collection.objects.link(copy);parts.append(copy)
        for i,material in enumerate(mesh.materials):
            if material.name.startswith('IntroSkin'):
                key='ArrivalSkin'+material.name[len('IntroSkin'):]
                skin=bpy.data.materials.get(key)
                if not skin:skin=material.copy();skin.name=key
                mesh.materials[i]=skin
    for ob in list(bpy.context.scene.objects):
        if ob not in parts:bpy.data.objects.remove(ob,do_unlink=True)
    low=min(v.co.z for ob in parts for v in ob.data.vertices)
    ys=[v.co.y for ob in parts for v in ob.data.vertices];centre=(min(ys)+max(ys))/2
    for ob in parts:
        for v in ob.data.vertices:v.co.y-=centre;v.co.z+=.025-low
        for p in ob.data.polygons:p.use_smooth=True
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts:ob.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object
    asset='arrival_'+name+'_01';body.name=asset+'_LOD0'
    for level,ratio in ((1,.42),(2,.13)):
        ob=body.copy();ob.data=body.data.copy();ob.name=asset+'_LOD'+str(level);bpy.context.collection.objects.link(ob)
        H.activate(ob);mod=ob.modifiers.new('Fallen hunter distance mesh','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.wm.save_as_mainfile(filepath=str(H.SOURCE/(asset+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(asset+'.fbx')),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,bake_anim=False)
    receipts.append({'character':name,'source':str((H.SOURCE/(source+'.blend')).relative_to(H.ROOT)).replace('\\','/'),'fallenAsset':asset,'sameFaceHairClothesUVs':True,'license':'retained CC0 MakeHuman anatomy and clothing; retained healthy skin derivative','dimensions':list(body.dimensions)})
    print('FALLEN PASSENGER',name,tuple(body.dimensions),flush=True)
(H.ROOT/'ArtSource/ArrivalCast.json').write_text(json.dumps({'characters':receipts},indent=2)+'\n')
