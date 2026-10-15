"""Original rigged aquatic horror and preserved zoological specimens, in metres."""
import bpy,math,sys
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).parent))
import forest_bodies as F
F.ROOT=ROOT
C=F.C;S=F.sphere;T=F.tube

def distance(p,a,b):
    d=b-a;return (p-(a+d*max(0,min(1,(p-a).dot(d)/d.length_squared)))).length

def anatomical_candidates(p,bones,jaw=False,tail=False):
    if jaw:return ['Jaw']
    if tail:return ['Tail','TailTip','Pelvis']
    if p.y>1.79:return ['Head']
    # Continuous proximity weights across shoulders, armpits, hips and digits.
    # Spatial cutoffs create abrupt boundaries between unrelated bone groups.
    return [name for name in bones if name not in ('AquaticRoot','Jaw','Tail','TailTip')]

def assign_weights(groups,vert,p,bones,candidates):
    ranked=sorted((distance(p,bones[name][0],bones[name][1]),name) for name in candidates)[:4]
    weights=[1/(d+.035)**4 for d,name in ranked];total=sum(weights)
    for (d,name),weight in zip(ranked,weights):groups[name].add([vert.index],weight/total,'REPLACE')

def scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version=0;F.PARTS.clear();F.MATS.clear()
    for key,colour in [('AbyssSkin',(.78,.85,.79)),('AbyssBelly',(.76,.80,.73)),('AbyssRidge',(.48,.56,.49)),('AbyssGums',(.21,.055,.065)),('AbyssTeeth',(.65,.58,.40)),('AbyssThroat',(.016,.006,.01)),('LabPreservedTissue',(.65,.57,.44)),('LabAnimalHide',(.34,.30,.22)),('LabSclera',(.65,.64,.47)),('LabIris',(.49,.39,.14)),('LabPupil',(.008,.01,.008))]:F.material(key,colour,.68,texture='LabAbyssDermisV2' if key in ('AbyssSkin','AbyssBelly','AbyssRidge') else None)

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj

def fuse(objects,name,voxel=.008):
    body=F.join(objects,name);activate(body);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    remesh=body.modifiers.new('Continuous organic surface','REMESH');remesh.mode='VOXEL';remesh.voxel_size=voxel;remesh.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=body.modifiers.new('Soft anatomical transitions','SMOOTH');smooth.factor=.65;smooth.iterations=3;bpy.ops.object.modifier_apply(modifier=smooth.name)
    reduce=body.modifiers.new('Specimen triangle budget','DECIMATE');reduce.ratio=.52;bpy.ops.object.modifier_apply(modifier=reduce.name)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.cube_project(cube_size=1);bpy.ops.object.mode_set(mode='OBJECT')
    return body

def export(name,rig=None):
    if not rig:
        objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
        F.join(objects,name)
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':
            activate(obj);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    if rig:bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Blender'/(name+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_Game/Art/Meshes'/(name+'.fbx')),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=bool(rig),bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
    print('RESEARCH EXPORT',name,flush=True)

def aquatic():
    scene()
    from aquatic_anatomy import build
    build(F,fuse)
    bones={}
    def bone(name,a,b,parent):bones[name]=(Vector(a),Vector(b),parent)
    bone('AquaticRoot',(0,0,0),(0,.20,0),None);bone('Pelvis',(0,.89,0),(0,1.13,0),'AquaticRoot')
    bone('Spine',(0,1.13,0),(0,1.58,0),'Pelvis');bone('Neck',(0,1.58,0),(0,1.82,.03),'Spine');bone('Head',(0,1.82,.03),(0,2.15,.03),'Neck')
    bone('Jaw',(0,1.83,.07),(0,1.78,.30),'Head');bone('Tail',(0,.9,-.13),(.1,.42,-.49),'Pelvis');bone('TailTip',(.1,.42,-.49),(.53,.38,-.08),'Tail')
    for s,label in [(-1,'L'),(1,'R')]:
        bone('UpperArm_'+label,(s*.27,1.53,0),(s*.48,1.13,.105),'Spine');bone('Forearm_'+label,(s*.48,1.13,.105),(s*.58,1,.18),'UpperArm_'+label);bone('Hand_'+label,(s*.58,1,.18),(s*.60,.82,.29),'Forearm_'+label)
        bone('Thigh_'+label,(s*.17,.9,0),(s*.23,.55,.055),'Pelvis');bone('Shin_'+label,(s*.23,.55,.055),(s*.25,.10,.15),'Thigh_'+label);bone('Foot_'+label,(s*.25,.10,.15),(s*.25,.06,.4),'Shin_'+label)
    data=bpy.data.armatures.new('AquaticSkeleton');rig=bpy.data.objects.new('AquaticRig',data);bpy.context.collection.objects.link(rig);activate(rig);bpy.ops.object.mode_set(mode='EDIT')
    for name,(a,b,parent) in bones.items():
        e=data.edit_bones.new(name);e.head=C(a);e.tail=C(b)
        if parent:e.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    parts=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in parts:
        activate(obj);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        groups={name:obj.vertex_groups.new(name=name) for name in bones}
        oral_weights=obj.data.attributes.get('oral_jaw_weight')
        for vert in obj.data.vertices:
            if oral_weights:
                weight=oral_weights.data[vert.index].value
                groups['Jaw'].add([vert.index],weight,'REPLACE');groups['Head'].add([vert.index],1-weight,'REPLACE')
                continue
            q=vert.co;p=Vector((-q.x,q.z,-q.y));s='L' if p.x<0 else 'R'
            candidates=anatomical_candidates(p,bones,obj.name.startswith('Lower'),obj.name.startswith('Continuous muscular tail'))
            assign_weights(groups,vert,p,bones,candidates)
        mod=obj.modifiers.new('Anatomical skeleton','ARMATURE');mod.object=rig;obj.parent=rig
    body=F.join(parts,'R09_Amphibian');body.parent=rig
    body.hide_set(True)
    rig.animation_data_create();bpy.context.scene.render.fps=30
    def rotate(name,degrees,axis=(1,0,0)):
        b=rig.pose.bones[name];rest=b.bone.matrix_local.to_quaternion();b.rotation_quaternion=rest.inverted()@Quaternion(Vector(axis),math.radians(degrees))@rest
    def translate(name,p):
        b=rig.pose.bones[name];b.location=b.bone.matrix_local.to_3x3().inverted()@C(p)
    def place_bone(name,head,tail):
        pose=rig.pose.bones[name];rest=pose.bone
        rotation=(rest.tail_local-rest.head_local).rotation_difference(C(tail-head))@rest.matrix_local.to_quaternion()
        pose.matrix=Matrix.Translation(C(head))@rotation.to_matrix().to_4x4()
    def leg(label,t):
        # The stance travels 0.36 * 2.3m per cycle, matching runtime displacement.
        # Solve both joints from a planted ankle, rather than pumping loose shins.
        side=-1 if label=='L' else 1;p=t%1;stance=.36;travel=2.3*stance
        if p<stance:z=travel*(.5-p/stance);lift=0;pitch=-5+10*p/stance
        else:
            u=(p-stance)/(1-stance);ease=u*u*(3-2*u)
            z=travel*(ease-.5);lift=.18*math.sin(math.pi*u)**1.5;pitch=5-10*u+16*math.sin(math.pi*u)
        bpy.context.view_layer.update();q=rig.pose.bones['Thigh_'+label].head;hip=Vector((-q.x,q.z,-q.y))
        ankle=Vector((side*.25,.103+lift,z+.10));direction=ankle-hip
        first=(bones['Thigh_'+label][1]-bones['Thigh_'+label][0]).length;second=(bones['Shin_'+label][1]-bones['Shin_'+label][0]).length
        distance=min(direction.length,first+second-.001);axis=direction.normalized();ankle=hip+axis*distance
        pole=Vector((side*.24,.48,.38))-hip;pole=(pole-axis*pole.dot(axis)).normalized()
        along=(first*first-second*second+distance*distance)/(2*distance)
        knee=hip+axis*along+pole*math.sqrt(max(0,first*first-along*along))
        place_bone('Thigh_'+label,hip,knee);bpy.context.view_layer.update();place_bone('Shin_'+label,knee,ankle);bpy.context.view_layer.update()
        foot=rig.pose.bones['Foot_'+label];rotation=Quaternion(Vector((1,0,0)),math.radians(pitch))@foot.bone.matrix_local.to_quaternion()
        foot.matrix=Matrix.Translation(C(ankle))@rotation.to_matrix().to_4x4()
    for name,frames in [('Aquatic_Idle',60),('Aquatic_Escape',120),('Aquatic_Run',30),('Aquatic_Attack',33),('Aquatic_Death',66),('Aquatic_Hit',15)]:
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame in range(frames+1):
            # Dependency-graph updates for the analytic IK must not re-evaluate
            # already inserted keys over the pose currently being authored.
            rig.animation_data.action=None;bpy.context.scene.frame_set(frame+1)
            t=frame/frames;phase=t*math.tau
            for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
            rotate('Head',-8);rotate('Jaw',5)
            if name=='Aquatic_Idle':
                rotate('Neck',2*math.sin(phase));rotate('Head',-10+2*math.sin(phase));rotate('TailTip',4*math.sin(phase),(0,0,1));translate('Pelvis',(0,.008*math.sin(phase),0))
            elif name=='Aquatic_Run':
                rotate('Spine',12);rotate('Head',-14);rotate('Jaw',8);translate('Pelvis',(0,-.12+.012*(1-math.cos(phase*2)),0))
                for side,offset in [('L',0),('R',math.pi)]:
                    wave=math.sin(phase+offset);rotate('UpperArm_'+side,-23*wave+14);rotate('Forearm_'+side,28+7*wave);rotate('Hand_'+side,-10)
                    leg(side,t+offset/math.tau)
                rotate('Tail',5*math.sin(phase),(0,0,1));rotate('TailTip',7*math.sin(phase-.6),(0,0,1))
            elif name=='Aquatic_Escape':
                brace=math.sin(min(t/.65,1)*math.pi);step=max(0,min(1,(t-.35)/.55));snarl=math.sin(max(0,(t-.72)/.28)*math.pi)
                rotate('Spine',14*brace);rotate('Head',-20+26*brace);rotate('Jaw',8+32*snarl)
                for side,s in [('L',1),('R',-1)]:
                    rotate('UpperArm_'+side,55*brace);rotate('Forearm_'+side,32*brace);rotate('Thigh_'+side,math.sin(step*math.tau+s*.9)*25*math.sin(step*math.pi));rotate('Shin_'+side,-20*brace)
                translate('Pelvis',(0,-.055*brace,0))
            elif name=='Aquatic_Attack':
                swing=math.sin(min(t/.53,1)*math.pi/2) if t<.53 else max(0,1-(t-.53)/.47)
                # The mandible closes through the recovery rather than jumping
                # from a full gape to four degrees at the strike boundary.
                recovery=max(0,min(1,(t-.53)/.47));recovery=recovery*recovery*(3-2*recovery)
                gape=4+38*math.sin(t/.53*math.pi/2) if t<.53 else 42-38*recovery
                rotate('Spine',20*swing);rotate('Head',-14+18*swing);rotate('Jaw',gape)
                for side in ['L','R']:rotate('UpperArm_'+side,-35+105*swing);rotate('Forearm_'+side,35-24*swing);rotate('Hand_'+side,-22*swing)
            elif name=='Aquatic_Hit':rotate('Spine',-15*math.sin(t*math.pi));rotate('Head',-25*math.sin(t*math.pi));rotate('Jaw',20)
            elif name=='Aquatic_Death':
                f=min(1,t/.65);f=f*f*(3-2*f);rotate('AquaticRoot',86*f,(0,1,0));translate('AquaticRoot',(0,.30*f,0));rotate('Spine',18*f);rotate('Jaw',22*f);rotate('TailTip',55*f,(0,0,1))
                for side in ['L','R']:
                    rotate('Thigh_'+side,22*f);rotate('Shin_'+side,-45*f);rotate('UpperArm_'+side,(30 if side=='L' else -30)*f,(0,1,0));rotate('Forearm_'+side,30*f)
            rig.animation_data.action=action
            for b in rig.pose.bones:b.keyframe_insert('rotation_quaternion',frame=frame+1);b.keyframe_insert('location',frame=frame+1)
        action.use_fake_user=True
    body.hide_set(False);body.data.calc_loop_triangles();print('R09 runtime triangles:',len(body.data.loop_triangles),flush=True)
    rig.animation_data.action=bpy.data.actions['Aquatic_Idle'];bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61;export('research_abomination_01',rig)

def specimens():
    scene()
    S('Vascular eyeball',(0,.105,0),(.088,.088,.088),'LabSclera',40,24)
    S('Iris',(0,.108,.079),(.043,.043,.013),'LabIris',40,20)
    S('Pupil',(0,.108,.090),(.016,.016,.004),'LabPupil',28,16)
    T('Optic nerve',[(0,.1,-.07),(.025,.08,-.12),(.02,.045,-.15)],[.024,.018,.01],'LabPreservedTissue',20)
    export('lab_eye_01')
    scene()
    S('Curled embryonic cranium',(0,.29,.015),(.095,.105,.086),'LabPreservedTissue',40,24)
    S('Embryonic face',(0,.26,.074),(.06,.055,.037),'LabPreservedTissue')
    S('Curled torso',(0,.17,-.022),(.071,.10,.055),'LabPreservedTissue')
    for s in [-1,1]:
        S('Closed eyelid',(s*.034,.285,.088),(.021,.006,.012),'LabAnimalHide',20,12)
        T('Folded arm',[(s*.058,.23,0),(s*.09,.18,.03),(s*.035,.19,.072)],[.024,.022,.012],'LabPreservedTissue',20)
        T('Curled hind limb',[(s*.04,.12,-.02),(s*.063,.065,.035),(s*.04,.11,.074)],[.028,.024,.014],'LabPreservedTissue',20)
    T('Umbilical tissue',[(0,.17,.03),(.075,.14,.07),(.10,.07,.03),(.045,.03,-.01),(0,.05,.03)],[.013,.011,.01,.009,.006],'LabPreservedTissue',16)
    fuse([o for o in list(bpy.context.scene.objects) if o.type=='MESH' and o.data.materials[0].name=='LabPreservedTissue'],'Preserved curled embryo',.003)
    export('lab_embryo_01')
    for animal in ['wolf','deer']:
        scene()
        S('Neck specimen',(0,.08,-.02),(.09,.08,.075),'LabPreservedTissue')
        S('Animal skull',(0,.18,0),(.105,.13,.10),'LabAnimalHide',40,24)
        S('Long animal muzzle',(0,.13,.12),(.064 if animal=='wolf' else .052,.055,.125),'LabAnimalHide')
        S('Nose pad',(0,.14,.228),(.06 if animal=='wolf' else .045,.037,.018),'LabPupil')
        S('Lower animal jaw',(0,.09,.12),(.055,.023,.105),'LabPreservedTissue')
        for s in [-1,1]:
            S('Eye socket',(s*.087,.20,.062),(.03,.028,.028),'LabPupil',24,16)
            S('Clouded preserved eye',(s*.094,.201,.077),(.018,.019,.019),'LabSclera',24,16)
            if animal=='wolf':T('Pointed pinna',[(s*.075,.25,-.02),(s*.11,.36,-.045),(s*.14,.26,-.035)],[.04,.003,.03],'LabAnimalHide',20)
            else:
                ear=S('Deer pinna',(s*.13,.24,-.015),(.084,.027,.04),'LabAnimalHide');ear.rotation_euler.y=s*.45
                T('Antler',[(s*.06,.28,-.045),(s*.085,.39,-.06),(s*.12,.46,-.025)],[.017,.011,.001],'AbyssTeeth',16)
                T('Antler tine',[(s*.08,.37,-.06),(s*.04,.42,-.01)],[.012,.001],'AbyssTeeth',14)
            S('Nostril',(s*.025,.15,.24),(.011,.009,.006),'LabPupil',16,12)
        fuse([o for o in list(bpy.context.scene.objects) if o.type=='MESH' and o.data.materials[0].name=='LabAnimalHide'],'Preserved '+animal+' head',.003)
        export('lab_'+animal+'_head_01')

def refresh_weights():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Blender/research_abomination_01.blend'))
    bpy.context.preferences.filepaths.save_version=0
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    def original(q):return Vector((-q.x,q.z,-q.y))
    bones={b.name:(original(b.head_local),original(b.tail_local),None) for b in rig.data.bones}
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        groups={n:obj.vertex_groups[n] for n in bones}
        for vert in obj.data.vertices:
            assigned={obj.vertex_groups[g.group].name:g.weight for g in vert.groups}
            if assigned.get('Jaw',0)>0:
                # Preserve the authored cheek/throat blend, not just rigid jaw
                # parts, when refreshing body weights without rebuilding art.
                weight=assigned['Jaw']
                for index in [g.group for g in vert.groups]:obj.vertex_groups[index].remove([vert.index])
                groups['Jaw'].add([vert.index],weight,'REPLACE');groups['Head'].add([vert.index],1-weight,'REPLACE')
                continue
            candidates=anatomical_candidates(original(vert.co),bones,assigned.get('Jaw',0)>.5,assigned.get('Tail',0)+assigned.get('TailTip',0)>0)
            for index in [g.group for g in vert.groups]:obj.vertex_groups[index].remove([vert.index])
            assign_weights(groups,vert,original(vert.co),bones,candidates)
    rig.animation_data.action=bpy.data.actions['Aquatic_Idle'];export('research_abomination_01',rig)

if '--refresh-weights' in sys.argv:refresh_weights()
else:
    if '--specimens-only' not in sys.argv:aquatic()
    if '--creature-only' not in sys.argv:specimens()
