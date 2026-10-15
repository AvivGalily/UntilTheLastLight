"""CC0 human anatomy: a full-body shadow and an actual closed axe grip."""
import bpy,bmesh,math,sys
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
ROOT=H.ROOT

def open_source():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Blender/hanging_survivor_01.blend'),load_ui=False,use_scripts=False)
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.animation_data_clear()
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    for b in rig.pose.bones:b.matrix_basis.identity()
    bpy.context.view_layer.update()
    return rig

def sleeve(hand,frame):
    """A fitted forearm sleeve/cuff, rather than displaced source-shirt faces."""
    material=bpy.data.materials.get('PlayerSleeve') or bpy.data.materials.new('PlayerSleeve')
    material.diffuse_color=(.14,.18,.16,1)
    vertices=[];faces=[];segments=40;rings=30
    inverse=frame.inverted()
    skin=[inverse@v.co for v in hand.data.vertices]
    end=min(v.y for v in skin)+.005
    for ring in range(rings):
        t=ring/(rings-1);y=-.108+(end+.108)*t
        sample=[v for v in skin if abs(v.y-y)<.022]
        if sample:
            lo=np.quantile([(v.x,v.z) for v in sample],.04,axis=0)
            hi=np.quantile([(v.x,v.z) for v in sample],.96,axis=0)
            cx,cz=(lo+hi)*.5;rx,rz=np.maximum((hi-lo)*.5,(.028,.026))
        else:cx=-.056+t*-.075;cz=.018;rx=.032+t*.017;rz=.029+t*.014
        hem=.004*math.exp(-t*45)
        for n in range(segments):
            a=n*math.tau/segments
            fold=.0018*math.sin(a*7+t*19)*math.sin(math.pi*t)**2
            vertices.append((cx+(rx+.004+hem+fold)*math.cos(a),y,cz+(rz+.004+hem+fold)*math.sin(a)))
    for ring in range(rings-1):
        for n in range(segments):
            faces.append((ring*segments+n,ring*segments+(n+1)%segments,(ring+1)*segments+(n+1)%segments,(ring+1)*segments+n))
    if frame.determinant()<0:faces=[tuple(reversed(face)) for face in faces]
    mesh=bpy.data.meshes.new('Fitted field jacket cuff');mesh.from_pydata([frame@Vector(v) for v in vertices],[],faces);mesh.materials.append(material)
    layer=mesh.uv_layers.new(name='UVMap')
    for face in mesh.polygons:
        ring=face.index//segments;n=face.index%segments
        for loop,coord in zip(face.loop_indices,[(n/segments,ring/(rings-1)),((n+1)/segments,ring/(rings-1)),((n+1)/segments,(ring+1)/(rings-1)),(n/segments,(ring+1)/(rings-1))]):layer.data[loop].uv=coord
        face.use_smooth=True
    ob=bpy.data.objects.new('Rounded cuff and sleeve folds',mesh);bpy.context.collection.objects.link(ob)
    H.activate(hand);ob.select_set(True);bpy.ops.object.join()

def bake(name,keep=None,transform=None,poses=None,sleeve_frame=None):
    deps=bpy.context.evaluated_depsgraph_get();parts=[]
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        if keep is not None:
            if not any(m and m.name.startswith('HumanSkin') for m in ob.data.materials):continue
            # Delete by original deform groups before evaluating the fitted surface.
            accepted={v.index for v in ob.data.vertices if keep(ob,v)}
            mesh=ob.data.copy();bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
            bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in accepted],context='VERTS');bm.to_mesh(mesh);bm.free();ob.data=mesh
            if not mesh.polygons:continue
        bpy.context.view_layer.update()
        mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        mesh.transform((transform or Matrix.Identity(4))@ob.matrix_world)
        copy=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(copy);parts.append(copy)
        if poses:
            copy.shape_key_add(name='Basis')
            for key,pose in poses.items():
                restore=pose()
                bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
                variant=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
                variant.transform(ob.matrix_world)
                assert len(variant.vertices)==len(mesh.vertices),'Crouch topology changed'
                morph=copy.shape_key_add(name=key)
                for target,vertex in zip(morph.data,variant.vertices):target.co=vertex.co
                bpy.data.meshes.remove(variant);restore();bpy.context.view_layer.update()
    for ob in list(bpy.context.scene.objects):
        if ob not in parts:bpy.data.objects.remove(ob,do_unlink=True)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts:ob.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name
    if name in ('player_axe_hand_01','player_shotgun_hand_01','player_flashlight_hand_01'):
        for material in ob.data.materials:
            if material and material.name.startswith('HumanSkin'):material.name='PlayerSkin'
        smooth=ob.modifiers.new('Continuous finger and knuckle contours','SUBSURF');smooth.levels=2;smooth.uv_smooth='PRESERVE_BOUNDARIES'
        bpy.ops.object.modifier_apply(modifier=smooth.name)
        sleeve(ob,sleeve_frame)
    for p in ob.data.polygons:p.use_smooth=True
    bpy.ops.wm.save_as_mainfile(filepath=str(H.SOURCE/(name+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(name+'.fbx')),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)
    print('PLAYER ASSET',name,'vertices',len(ob.data.vertices),flush=True)

def crouch_pose(rig):
    basis={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    feet={s:rig.pose.bones['foot.'+s].head.copy() for s in ['L','R']}
    root=rig.pose.bones['root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-.50))
    bpy.context.view_layer.update()
    for side in ['L','R']:
        upper=['upperleg01.'+side,'upperleg02.'+side];lower=['lowerleg01.'+side,'lowerleg02.'+side]
        hip=rig.pose.bones[upper[0]].head.copy();ankle=feet[side]
        thigh=sum(rig.pose.bones[n].length for n in upper);shin=sum(rig.pose.bones[n].length for n in lower)
        line=ankle-hip;distance=line.length;direction=line.normalized()
        along=(thigh*thigh-shin*shin+distance*distance)/(2*distance)
        bend=Vector((0,-1,0));bend-=direction*bend.dot(direction);bend.normalize()
        knee=hip+direction*along+bend*math.sqrt(max(0,thigh*thigh-along*along))
        for n in upper:H.point_bone(rig,n,knee-rig.pose.bones[n].head)
        for n in lower:H.point_bone(rig,n,ankle-rig.pose.bones[n].head)
        H.point_bone(rig,'foot.'+side,(0,-.062,-.033))
    def restore():
        for b in rig.pose.bones:b.matrix_basis=basis[b.name]
    return restore

def gait_pose(rig,phase,running=False):
    basis={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    feet={side:rig.pose.bones['foot.'+side].head.copy() for side in ('L','R')}
    reach=.47 if running else .39
    offsets=[(reach,-reach),(0,0),(-reach,reach),(0,0)][phase]
    # Lower the pelvis enough for the extended stance leg to reach the floor.
    root=rig.pose.bones['root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-(.15 if running else .11)))
    bpy.context.view_layer.update()
    for side,offset in zip(('L','R'),offsets):
        upper=['upperleg01.'+side,'upperleg02.'+side];lower=['lowerleg01.'+side,'lowerleg02.'+side]
        target=feet[side]+Vector((0,-offset,.18 if running else .12)) if (phase==1 and side=='R') or (phase==3 and side=='L') else feet[side]+Vector((0,-offset,0))
        hip=rig.pose.bones[upper[0]].head.copy();thigh=sum(rig.pose.bones[n].length for n in upper);shin=sum(rig.pose.bones[n].length for n in lower)
        line=target-hip;distance=min(line.length,thigh+shin-.001);direction=line.normalized()
        along=(thigh*thigh-shin*shin+distance*distance)/(2*distance)
        bend=Vector((0,-1,0));bend-=direction*bend.dot(direction);bend.normalize()
        knee=hip+direction*along+bend*math.sqrt(max(0,thigh*thigh-along*along))
        for name in upper:H.point_bone(rig,name,knee-rig.pose.bones[name].head)
        for name in lower:H.point_bone(rig,name,target-rig.pose.bones[name].head)
        # Restore the planted boot's rest orientation after leg articulation.
        foot=rig.pose.bones['foot.'+side];rest=foot.bone.matrix_local
        foot.matrix=Matrix.Translation(foot.head)@rest.to_quaternion().to_matrix().to_4x4()
        sign=1 if side=='L' else -1
        swing=offset*(1.3 if running else .8)
        H.point_bone(rig,'upperarm01.'+side,(sign*.18,swing,-1))
        H.point_bone(rig,'lowerarm01.'+side,(sign*.1,-.65 if running else -.3,-1))
    def restore():
        for b in rig.pose.bones:b.matrix_basis=basis[b.name]
    return restore

def body():
    rig=open_source()
    for side,sign in [('L',1),('R',-1)]:
        H.point_bone(rig,'upperarm01.'+side,(sign*.20,0,-1))
        H.point_bone(rig,'lowerarm01.'+side,(sign*.10,-.32,-1))
    poses={'Crouch':lambda:crouch_pose(rig)}
    for running in (False,True):
        for phase in range(4):poses[('Run' if running else 'Walk')+str(phase)]=lambda phase=phase,running=running:gait_pose(rig,phase,running)
    bake('player_body_01',poses=poses)

def grip(trigger=False,side='R',radius=.027,return_pose=False):
    rig=open_source()
    # Curl real finger segments around a 3 cm shaft, preserving skin UVs/nails.
    roots=[rig.pose.bones[f'finger{i}-1.{side}'].head.copy() for i in range(2,6)]
    axis=(roots[-1]-roots[0]).normalized();origin=sum(roots,Vector())/4
    outward=origin-rig.pose.bones['wrist.'+side].head;outward-=axis*outward.dot(axis);outward.normalize()
    normal=axis.cross(outward).normalized()*(1 if side=='L' else -1);centre=origin+normal*radius
    for finger in range(2,6):
        root=rig.pose.bones[f'finger{finger}-1.{side}'].head-centre;along=root.dot(axis)
        r=math.hypot(root.dot(outward),root.dot(normal));angle=math.atan2(root.dot(outward),-root.dot(normal))
        for joint in (1,2,3):
            bone=rig.pose.bones[f'finger{finger}-{joint}.{side}']
            angle+=2*math.asin(min(.99,bone.length/(2*r)))
            target=centre+axis*along+outward*(math.sin(angle)*r)-normal*(math.cos(angle)*r)
            if trigger and finger==2:
                direction=[outward*.7-normal*.65+axis*.15,outward*.5+normal*.7+axis*.5,-outward*.6+normal*.4+axis*.7][joint-1]
                target=bone.head+direction
            H.point_bone(rig,bone.name,target-bone.head)
    # Opposed thumb closes across the fingers.
    for joint,(f,a,n) in enumerate([(-.044,-.038,-.020),(-.024,-.038,.014),(-.018,-.010,.021)],1):
        bone=rig.pose.bones[f'finger1-{joint}.{side}']
        target=centre+outward*f+axis*a+normal*n*(radius/.027)
        H.point_bone(rig,bone.name,target-bone.head)
    bpy.context.view_layer.update()
    # Canonical grip frame: normal points into the handle from the palm,
    # outward follows the fingers away from the wrist, axis crosses the knuckles.
    arm_groups={'lowerarm01.'+side}|{bone.name for bone in rig.pose.bones['lowerarm01.'+side].children_recursive}
    frame=Matrix((normal,outward,axis)).transposed().to_4x4()
    # Axe: vertical shaft, inward palm and a wrist behind the grip.
    # Left flashlight: palm above the cylinder, thumb at the rear switch,
    # wrist on the outside of the left arm rather than folded across the body.
    if side=='L':destination=Matrix(((0,0,-1),(-1,0,0),(0,-1,0))).transposed().to_4x4()
    else:
        # Stock/axe: the thumb saddles the top, palm rests against the outside,
        # and fingers descend around the grip from a slightly raised wrist.
        # This is a real diagonal grip, not an upside-down supporting palm.
        tilt=math.radians(40 if trigger else 15);c,s=math.cos(tilt),math.sin(tilt)
        destination=Matrix(((1,0,0),(0,-c,-s),(0,s,-c))).transposed().to_4x4()
    transform=destination@frame.inverted()@Matrix.Translation(-centre)
    assert transform.to_3x3().determinant()>.999,'Equipment hand must use a proper anatomical rotation'
    def keep(ob,v):
        return any(ob.vertex_groups[g.group].name in arm_groups and g.weight>.025 for g in v.groups)
    name='player_shotgun_hand_01' if trigger else 'player_flashlight_hand_01' if side=='L' else 'player_axe_hand_01'
    if return_pose:return rig,transform,name
    bake(name,keep,transform,sleeve_frame=destination)

if __name__=='__main__':
    task=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'all'
    if task in ('all','body'):body()
    if task in ('all','hands'):grip();grip(True,radius=.030);grip(side='L',radius=.040)
