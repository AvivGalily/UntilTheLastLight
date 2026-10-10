"""Clothed adult with an articulated skeleton, original crow and gallows branch."""
import bpy,sys,math,random
from pathlib import Path
from mathutils import Vector,Euler
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
ROOT=H.ROOT
def clean():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for a in list(bpy.data.actions):bpy.data.actions.remove(a)
def save(name,animated=False):
    bpy.ops.wm.save_as_mainfile(filepath=str(H.SOURCE/(name+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(name+'.fbx')),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=animated,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
def victim():
    clean();sys.path.insert(0,str(next((H.VENDOR/'mpfb-2.0.17').glob('*/src'))))
    import addon_utils
    bpy.utils.extension_path_user=lambda package,path='',create=False:str(H.VENDOR/'mpfb-workspace'/path)
    addon_utils.enable('mpfb',default_set=True)
    from mpfb.services.humanservice import HumanService
    from mpfb.services.targetservice import TargetService
    macro=TargetService.get_default_macro_info_dict();macro.update(gender=1,age=.65,muscle=.38,weight=.47)
    human=HumanService.create_human(macro_detail_dict=macro);human.data.materials.clear()
    human.data.materials.append(H.material('HumanSkin0',H.VENDOR/'male-skins-cc0/skins/mindfront_aksel_skin/mindfront_aksel_skin.mhmat'))
    rig=HumanService.add_builtin_rig(human,'default');rig.name='VictimRig'
    for key,path,kind in [('HumanOutfit2','clothes/male_worksuit01/male_worksuit01.mhclo','Clothes'),('HumanBoots','clothes/shoes04/shoes04.mhclo','Clothes'),('HumanHair','hair/short01/short01.mhclo','Hair'),('HumanEyes','eyes/high-poly/high-poly.mhclo','Eyes'),('HumanBrows','eyebrows/eyebrow001/eyebrow001.mhclo','Eyebrows')]:
        file=H.SYSTEM/path;ob=HumanService.add_mhclo_asset(str(file),human,asset_type=kind,subdiv_levels=0,import_subrig=False)
        matfile=next(file.parent.glob('*.mhmat'),None)
        for line in file.read_text(encoding='utf-8',errors='replace').splitlines():
            if line.startswith('material '):matfile=(file.parent/line.split(maxsplit=1)[1]).resolve();break
        ob.data.materials.clear();ob.data.materials.append(H.material(key,matfile))
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        for mod in ob.modifiers:
            if mod.type=='ARMATURE':mod.show_viewport=False;mod.show_render=False
        bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
        mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        ob.data=mesh;ob.modifiers.clear();mod=ob.modifiers.new('Weighted human skeleton','ARMATURE');mod.object=rig
        for poly in mesh.polygons:poly.use_smooth=True
    for s,sign in [('L',1),('R',-1)]:
        H.point_bone(rig,'upperarm01.'+s,(sign*.28,0,1));H.point_bone(rig,'lowerarm01.'+s,(-sign*.14,-.04,1))
        H.point_bone(rig,'upperleg01.'+s,(sign*.08,.05,-1));H.point_bone(rig,'lowerleg01.'+s,(0,.18,-1))
    suspended={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    for s,sign in [('L',1),('R',-1)]:
        H.point_bone(rig,'upperarm01.'+s,(sign*.70,-.25,-.68));H.point_bone(rig,'lowerarm01.'+s,(sign*.30,-.50,-.65))
        H.point_bone(rig,'upperleg01.'+s,(sign*.13,.06,-1));H.point_bone(rig,'lowerleg01.'+s,(0,.30,-1))
    collapsed={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    rig.animation_data_create()
    for name,end in [('Victim_Hanging',60),('Victim_Overpowered',30),('Victim_Dying',195)]:
        act=bpy.data.actions.new(name);rig.animation_data.action=act
        for frame in range(1,end+2):
            seconds=(frame-1)/30;phase=seconds*math.tau/2;blend=min(1,seconds/.78);blend=blend*blend*(3-2*blend)
            for bone in rig.pose.bones:
                matrix=collapsed[bone.name] if name=='Victim_Dying' else suspended[bone.name]
                bone.matrix_basis=matrix;bone.rotation_mode='QUATERNION'
                if name=='Victim_Overpowered':bone.rotation_quaternion=suspended[bone.name].to_quaternion().slerp(collapsed[bone.name].to_quaternion(),blend)
                tremor=math.sin(seconds*18)*(max(0,1-seconds/3)**2) if name=='Victim_Dying' else 0
                angles=Vector((0,0,0))
                if bone.name.startswith('lowerleg01'):angles.x=math.sin(phase+(0 if bone.name.endswith('L') else 2))*(.10 if name=='Victim_Hanging' else .17*(1-blend) if name=='Victim_Overpowered' else tremor*.09)
                if bone.name.startswith('upperarm01') and name=='Victim_Dying':angles.y=tremor*.08
                if bone.name=='head':angles.x=.10 if name=='Victim_Hanging' else .18*blend;angles.y=math.sin(phase)*(.10 if name=='Victim_Hanging' else .06)+tremor*.12
                if bone.name.startswith('spine'):angles.x=math.sin(phase)*.013 if name=='Victim_Hanging' else -.11*blend if name=='Victim_Overpowered' else tremor*.04
                bone.rotation_quaternion=bone.rotation_quaternion@Euler(angles,'XYZ').to_quaternion()
                bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame);bone.keyframe_insert('scale',frame=frame)
        act.use_fake_user=True
    rig.animation_data.action=bpy.data.actions['Victim_Hanging'];bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61;bpy.context.scene.render.fps=30;bpy.context.scene.frame_set(1)
    save('hanging_survivor_01',True)
def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);return m
def ellipsoid(name,p,size,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=p);o=bpy.context.object;o.name=name;o.scale=size;H.activate(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
    for f in o.data.polygons:f.use_smooth=True
    return o
def tube(name,points,radii,mat,sides=10):
    vertices=[];faces=[]
    for i,p in enumerate(points):
        direction=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        axis=direction.cross(Vector((0,1,0)))
        if axis.length<.01:axis=direction.cross(Vector((1,0,0)))
        axis.normalize();second=direction.cross(axis).normalized()
        for j in range(sides):vertices.append(Vector(p)+radii[i]*(axis*math.cos(j*math.tau/sides)+second*math.sin(j*math.tau/sides)))
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.materials.append(mat);o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    H.activate(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
    for p in mesh.polygons:p.use_smooth=True
    return o
def crow():
    clean();black=material('CrowFeathers',(.028,.036,.048));beak=material('CrowBeak',(.023,.025,.028));eye=material('CrowEye',(.006,.006,.008))
    anatomy=[ellipsoid('CrowBody',(0,0,.19),(.082,.15,.095),black),ellipsoid('CrowNeck',(0,-.10,.25),(.056,.084,.070),black),ellipsoid('CrowHead',(0,-.16,.29),(.057,.071,.054),black)]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in anatomy:ob.select_set(True)
    bpy.context.view_layer.objects.active=anatomy[0];bpy.ops.object.join();body=bpy.context.object
    rem=body.modifiers.new('Continuous bird anatomy','REMESH');rem.mode='VOXEL';rem.voxel_size=.007;H.activate(body);bpy.ops.object.modifier_apply(modifier=rem.name)
    smooth=body.modifiers.new('Feather contour','SMOOTH');smooth.factor=.7;smooth.iterations=4;bpy.ops.object.modifier_apply(modifier=smooth.name)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
    for f in body.data.polygons:f.use_smooth=True
    tube('Beak',[(0,-.203,.285),(0,-.28,.275),(0,-.33,.25)],[.028,.020,.001],beak)
    for s in [-1,1]:
        for row in range(3):
            for n in range(8):
                o=ellipsoid('Scapular feather',(s*(.04+row*.013),-.045+n*.019,.267-row*.014),(.014,.040,.004),black);o.rotation_euler.z=s*(.12+row*.09)
    for s in [-1,1]:
        ellipsoid('Eye',(s*.05,-.185,.308),(.006,.009,.006),eye)
        tube('Shin',[(s*.04,0,.14),(s*.041,.02,.075),(s*.045,-.015,.03)],[.009,.008,.005],beak)
        for t in [-1,0,1]:tube('Claw',[(s*.045,-.015,.032),(s*.045+t*.017,-.063,.02),(s*.045+t*.022,-.075,.013)],[.005,.004,.001],beak,6)
    for n in range(7):
        o=ellipsoid('TailFeather',((n-3)*.014,.19,.19),(.010,.105,.006),black);o.rotation_euler.z=(n-3)*.07
    # Separate organic feather fans rotate at shoulder and wrist at runtime.
    for s,label in [(-1,'L'),(1,'R')]:
        parts=[]
        outline=[(.06,-.05,.24),(.14,-.07,.24),(.24,-.025,.242),(.32,.04,.23),(.31,.10,.22),(.18,.14,.222),(.08,.12,.238)]
        vertices=[(s*.16,.025,.26)]+[(s*x,y,z) for x,y,z in outline]
        faces=[(0,n+1,(n+1)%len(outline)+1) for n in range(len(outline))]
        mesh=bpy.data.meshes.new('Tapered wing coverts');mesh.from_pydata(vertices,[],faces);mesh.materials.append(black)
        cover=bpy.data.objects.new('Wing coverts',mesh);bpy.context.collection.objects.link(cover);parts.append(cover);H.activate(cover)
        sol=cover.modifiers.new('Feather volume','SOLIDIFY');sol.thickness=.009;bpy.ops.object.modifier_apply(modifier=sol.name)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT')
        for f in cover.data.polygons:f.use_smooth=True
        for n in range(12):
            o=ellipsoid('Primary feather',(s*(.18+n*.014),.075+n*.005,.229),(.013,.135-n*.003,.0065),black);o.rotation_euler.z=s*(-.85+n*.055);parts.append(o)
        bpy.ops.object.select_all(action='DESELECT')
        for o in parts:o.select_set(True)
        bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();wing=bpy.context.object;wing.name='CrowWing_'+label
        bpy.context.scene.cursor.location=(s*.06,-.025,.24);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    # One static body surface plus two hinged wings, rather than one renderer
    # for every feather and toe on every bird in the forest.
    static=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.name.startswith('CrowWing_')]
    bpy.ops.object.select_all(action='DESELECT')
    for o in static:o.select_set(True)
    bpy.context.view_layer.objects.active=static[0];bpy.ops.object.join();body=bpy.context.object;body.name='CrowBody'
    body.data.calc_loop_triangles();count=len(body.data.loop_triangles)
    if count>6500:
        dec=body.modifiers.new('Bird surface budget','DECIMATE');dec.ratio=6500/count;bpy.ops.object.modifier_apply(modifier=dec.name)
    save('crow_01')
def branch():
    clean();mat=material('Bark',(.2,.16,.12));parts=[]
    parts.append(tube('Ancient trunk',[(0,0,0),(.03,0,1.2),(-.13,.08,2.6),(.1,0,3.8),(-.2,.1,5.4),(-.5,0,7)],[.58,.45,.34,.26,.13,.025],mat,18))
    parts.append(tube('Victim overhang',[(0,0,3),(.65,-.07,3.7),(1.5,0,4.0),(2.5,-.2,3.82),(3.0,-.25,4.15)],[.27,.21,.16,.075,.008],mat,14))
    parts.append(tube('Fork',[(0,0,4.2),(-.9,.3,5),(-1.7,.4,5.4),(-2.4,.8,5.2)],[.2,.13,.07,.008],mat))
    for n in range(7):
        a=n*math.tau/7;parts.append(tube('Roots',[(0,0,.32),(.7*math.cos(a),.7*math.sin(a),.07),(1.5*math.cos(a+.1),1.5*math.sin(a+.1),-.07)],[.23,.12,.006],mat))
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name='gallows_oak_01_LOD0'
    H.activate(ob);sub=ob.modifiers.new('Organic bark contours','SUBSURF');sub.levels=2;bpy.ops.object.modifier_apply(modifier=sub.name)
    for level,ratio in [(1,.5),(2,.18)]:
        o=ob.copy();o.data=ob.data.copy();bpy.context.collection.objects.link(o);o.name='gallows_oak_01_LOD'+str(level);H.activate(o);m=o.modifiers.new('LOD','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name)
    save('gallows_oak_01')
if __name__=='__main__':
    if '--only-crow' in sys.argv:crow()
    elif '--only-victim' in sys.argv:victim()
    else:victim();crow();branch()
