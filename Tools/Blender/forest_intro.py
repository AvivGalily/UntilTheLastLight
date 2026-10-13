"""Original metre-scale 4x4 interior and three seated CC0 human adaptations.
Retains the project's human skin UVs, clothing and actual deform skeleton.
"""
import argparse, bpy, json, math, sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
from encounter_assets import save
bpy.context.preferences.filepaths.save_version=0

def mat(name,color):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color=(*color,1);return m
def point(p):return (p[0],-p[2],p[1])
def box(name,p,size,m,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=point(p));o=bpy.context.object;o.name=name
    o.scale=(size[0],size[2],size[1]);H.activate(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(m)
    if bevel:
        b=o.modifiers.new('Worn rounded edges','BEVEL');b.width=bevel;b.segments=3;bpy.ops.object.modifier_apply(modifier=b.name)
    return o
def cylinder(name,p,r,depth,m,rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=depth,location=point(p));o=bpy.context.object;o.name=name
    if rotation:o.rotation_euler=rotation
    o.data.materials.append(m)
    for f in o.data.polygons:f.use_smooth=True
    return o
def join_meshes(name):
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=name
def vehicle():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    steel=mat('Steel',(.2,.25,.22));rubber=mat('IntroDash',(.05,.07,.06));fabric=mat('IntroSeat',(.15,.19,.14));glass=mat('Glass',(.08,.14,.15));door=mat('IntroDoor',(.12,.14,.12));floor=mat('IntroFloor',(.05,.055,.05))
    box('Chassis',(0,.35,0),(1.95,.3,4.3),steel)
    box('Hood',(0,1.05,1.75),(1.86,.18,1.15),steel)
    box('Roof',(0,2.06,-.12),(1.96,.10,2.65),steel)
    box('Dashboard',(0,1.03,.86),(1.78,.24,.46),rubber,.05)
    box('Dash padded brow',(0,1.16,.86),(1.80,.075,.48),fabric)
    box('Centre console',(.05,.66,.25),(.27,.34,1.05),rubber)
    box('Gear lever boot',(.06,.89,.49),(.12,.08,.13),rubber)
    cylinder('Gear lever',(.06,1.0,.49),.013,.22,steel)
    box('Gear knob',(.06,1.12,.49),(.075,.05,.075),rubber)
    for x in [-.88,.88]:
        box('Door panel',(x,.92,-.22),(.10,.75,2.55),door)
        box('Armrest',(x*.96,1.08,.03),(.12,.10,.64),fabric)
        box('Pull handle',(x*.95,1.30,.33),(.06,.06,.20),steel)
        for z in [-1.32,-.40,.98]:
            pillar=box('Window pillar',(x,1.68,z),(.075,.77,.09),steel)
            if z==.98:pillar.rotation_euler.x=math.radians(-13)
        box('Window top seal',(x,2.0,-.12),(.08,.07,2.55),rubber)
        box('Window sill',(x,1.40,-.1),(.08,.05,2.6),rubber)
        box('Side mirror',(x*1.18,1.42,.87),(.27,.19,.10),rubber)
        for z in [-1.42,1.38]:
            box('Fender',(x,.64,z),(.19,.18,.94),steel)
            tyre=cylinder('Tyre',(x*1.08,.43,z),.40,.25,rubber,(0,math.pi/2,0))
            cylinder('Steel wheel',(x*1.09,.43,z),.23,.26,steel,(0,math.pi/2,0))
    box('Windshield lower seal',(0,1.27,1.10),(1.77,.06,.06),rubber)
    box('Windshield upper seal',(0,1.99,.96),(1.77,.06,.06),rubber)
    # Open forward pane avoids darkening the only view; narrow edge strips imply glass.
    for x in [-.80,.80]:box('Windshield fogged edge',(x,1.64,1.01),(.035,.57,.012),glass,.005)
    box('Rear body panel',(0,1.09,-1.45),(1.9,.95,.10),steel)
    box('Rear window border',(0,1.85,-1.45),(1.9,.18,.08),steel)
    for x in [-.44,.44]:
        for z in [0,-.95]:
            box('Seat cushion',(x,.55,z),(.66,.16,.55),fabric,.07)
            seat=box('Seat back',(x,.98,z-.29),(.65,.87,.17),fabric,.06);seat.rotation_euler.x=math.radians(-7)
            box('Headrest',(x,1.47,z-.33),(.36,.24,.12),rubber,.04)
            # Stitched seat panels.
            for stripe in [-.19,0,.19]:box('Seat seam',(x+stripe,.64,z),(.006,.003,.42),steel,.001)
    for x in [-.54,.70]:
        box('Vent surround',(x,1.08,.60),(.28,.11,.045),steel,.015)
        for i in range(5):box('Vent blade',(x-.10+i*.05,1.08,.57),(.009,.08,.016),rubber,.002)
    for x in [-.17,.0,.17]:cylinder('Dash switch',(x,.98,.61),.024,.035,steel,(math.pi/2,0,0))
    box('Rear equipment bag',(0,.57,-1.13),(.42,.26,.34),fabric,.07)
    box('Bag strap',(0,.71,-1.13),(.05,.025,.36),steel)
    for x in [-.44,.44]:
        box('Textured rubber floor mat',(x,.50,.55),(.66,.025,1.0),floor,.018)
        for rib in range(10):box('Floor mat ribs',(x-.27+rib*.06,.519,.55),(.012,.008,.88),rubber,.003)
    # Gauges and lettering are separate Unity elements in the editable prefab.
    join_meshes('HunterVehicle');save('intro_vehicle_01')
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    rim=mat('IntroWheelLeather',(.04,.055,.047));metal=mat('Steel',(.22,.25,.24))
    bpy.ops.mesh.primitive_torus_add(major_segments=96,minor_segments=14,major_radius=.235,minor_radius=.015)
    o=bpy.context.object;o.name='Continuous stitched steering rim';o.rotation_euler.x=math.pi/2;o.data.materials.append(rim)
    for f in o.data.polygons:f.use_smooth=True
    box('Wheel hub',(0,0,0),(.17,.11,.06),rim,.025)
    for sign in [-1,1]:
        ob=box('Wheel spoke',(sign*.11,-.065,0),(.19,.035,.02),metal,.009);ob.rotation_euler.y=sign*math.radians(-22)
    box('Lower spoke',(0,-.13,0),(.035,.21,.024),metal)
    join_meshes('SteeringWheel');save('intro_wheel_01')

def passenger(identity):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    sys.path.insert(0,str(next((H.VENDOR/'mpfb-2.0.17').glob('*/src'))))
    import addon_utils
    bpy.utils.extension_path_user=lambda package,path='',create=False:str(H.VENDOR/'mpfb-workspace'/path)
    addon_utils.enable('mpfb',default_set=True)
    from mpfb.services.humanservice import HumanService
    from mpfb.services.targetservice import TargetService
    # The same macro proportions and garments as the existing later casualties.
    body_index=[1,0,2][identity]
    macro=TargetService.get_default_macro_info_dict()
    macro.update(gender=1,age=[.55,.78,.43,.62][body_index],muscle=[.48,.33,.65,.42][body_index],weight=[.48,.44,.58,.4][body_index])
    human=HumanService.create_human(macro_detail_dict=macro)
    # Preserve the cast's age/build while giving each living face its own
    # restrained anatomical nose, lips and jaw contours.
    profiles=[{'nose-volume-incr':.22,'nose-greek-incr':.18,'mouth-upperlip-volume-incr':.12},
              {'nose-width1-decr':.22,'mouth-scale-horiz-incr':.18,'chin-width-decr':.12},
              {'nose-width1-incr':.20,'nose-scale-depth-incr':.16,'mouth-scale-horiz-decr':.14,'chin-width-incr':.16}]
    targets=next((H.VENDOR/'mpfb-2.0.17').glob('*/src/mpfb/data/targets'))
    for name,weight in profiles[identity].items():
        path=next(targets.rglob(name+'.target.gz'))
        TargetService.load_target(human,str(path),weight=weight,name='Opening '+name)
    skin_key='IntroSkin'+str(body_index%2)
    skin=bpy.data.materials.get(skin_key) or bpy.data.materials.new(skin_key);skin.use_nodes=True
    bs=skin.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.60
    node=skin.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(H.TEX/(skin_key+'_BaseColor.png')),check_existing=True)
    skin.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color']);human.data.materials.clear();human.data.materials.append(skin)
    rig=HumanService.add_builtin_rig(human,'default')
    def asset(key,path,kind='Clothes',material_path=None):
        file=H.SYSTEM/path
        ob=HumanService.add_mhclo_asset(str(file),human,asset_type=kind,subdiv_levels=0,import_subrig=False)
        matfile=material_path
        if matfile is None:
            for line in file.read_text(encoding='utf-8',errors='replace').splitlines():
                if line.startswith('material '):matfile=(file.parent/line.split(maxsplit=1)[1]).resolve();break
        if not matfile or not matfile.exists():matfile=next(file.parent.glob('*.mhmat'),None)
        ob.data.materials.clear();ob.data.materials.append(H.material(key,matfile));return ob
    outfit=['male_casualsuit05','male_casualsuit01','male_worksuit01'][body_index]
    asset('IntroOutfit'+str(body_index),f'clothes/{outfit}/{outfit}.mhclo')
    asset('HumanBoots','clothes/shoes04/shoes04.mhclo')
    hair=['short02','short03','short04'][identity]
    asset('IntroHair'+str(identity),f'hair/{hair}/{hair}.mhclo','Hair')
    asset('IntroEyes'+str(identity),'eyes/high-poly/high-poly.mhclo','Eyes',H.SYSTEM/'eyes/materials'/['brown.mhmat','bluegreen.mhmat','grey.mhmat'][identity])
    asset('HumanBrows','eyebrows/eyebrow001/eyebrow001.mhclo','Eyebrows')
    # Resolve fitted clothes/helpers into the actual weighted anatomy first.
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        for modifier in ob.modifiers:
            if modifier.type=='ARMATURE':modifier.show_viewport=False;modifier.show_render=False
        bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
        ob.data=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        ob.modifiers.clear();modifier=ob.modifiers.new('Living weighted anatomy','ARMATURE');modifier.object=rig
    bpy.context.view_layer.update()
    # The root local basis is tilted; convert a world downshift to that basis.
    rig.pose.bones['root'].location=rig.pose.bones['root'].bone.matrix_local.to_3x3().inverted()@Vector((0,0,-.34))
    bpy.context.view_layer.update()
    for side,sign in [('L',1),('R',-1)]:
        for n in ['upperleg01','upperleg02']:H.point_bone(rig,n+'.'+side,(sign*.05,-1,-.05))
        for n in ['lowerleg01','lowerleg02']:H.point_bone(rig,n+'.'+side,(0,0,-1))
        H.point_bone(rig,'foot.'+side,(0,-1,-.15))
        for n in ['upperarm01','upperarm02']:H.point_bone(rig,n+'.'+side,(sign*.15,-.12,-1))
        H.point_bone(rig,'lowerarm01.'+side,(sign*.04,-.72,-.45))
    # Bake the seated rest pose without borrowing a hanging/dying head pose.
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        for modifier in list(ob.modifiers):
            if modifier.type=='ARMATURE':H.activate(ob);bpy.ops.object.modifier_apply(modifier=modifier.name)
        # Keep the living skin, and use tinted field clothing in Unity.
        for poly in ob.data.polygons:poly.use_smooth=True
    H.activate(rig);bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    for ob in list(bpy.context.scene.objects):
        if ob.type=='MESH':
            modifier=ob.modifiers.new('Seated hunter anatomy','ARMATURE');modifier.object=rig
    rig.name=['JackRig','TylerRig','EthanRig'][identity]
    save('intro_'+['jack','tyler','ethan'][identity]+'_01',False)
    print('LIVING SEATED HUNTER',identity,'matches forest_body_0'+str(body_index+1),flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--passengers-only',action='store_true');parser.add_argument('--vehicle-only',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    if not args.passengers_only:vehicle()
    if not args.vehicle_only:
        for i in range(3):passenger(i)
