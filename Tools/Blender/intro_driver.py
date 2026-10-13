"""A seated, clothed CC0 driver with anatomical wheel grips and a weighted body.
Reuses the healthy Tyler rest rig/UVs, retaining editable bones and skin weights.
"""
import bpy,bmesh,math,sys,json
from pathlib import Path
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
from encounter_assets import save

def point(p):return Vector((p[0],-p[2],p[1]))

def hand_frame(rig,side):
    roots=[rig.pose.bones[f'finger{i}-1.{side}'].head.copy() for i in range(2,6)]
    axis=(roots[-1]-roots[0]).normalized();origin=sum(roots,Vector())/4
    outward=origin-rig.pose.bones['wrist.'+side].head
    outward-=axis*outward.dot(axis);outward.normalize()
    # A cross product changes sign under reflection. The native left/right
    # skeletons therefore need opposite signs to describe the PALM surface.
    normal=axis.cross(outward).normalized()*(1 if side=='L' else -1)
    return origin,axis,outward,normal

def curl(rig,side):
    origin,axis,outward,normal=hand_frame(rig,side)
    centre=origin+normal*.027
    for finger in range(2,6):
        root=rig.pose.bones[f'finger{finger}-1.{side}'].head-centre
        along=root.dot(axis);radius=math.hypot(root.dot(outward),root.dot(normal))
        angle=math.atan2(root.dot(outward),-root.dot(normal))
        # Each native phalanx keeps its length. Successive chord endpoints
        # wrap around the 30 mm rim; shorter pinkies bend less than middles.
        for joint in (1,2,3):
            bone=rig.pose.bones[f'finger{finger}-{joint}.{side}']
            angle+=2*math.asin(min(.99,bone.length/(2*radius)))
            target=centre+axis*along+outward*(math.sin(angle)*radius)-normal*(math.cos(angle)*radius)
            H.point_bone(rig,bone.name,target-bone.head)
    # Oppose the thumb from the INDEX side of each hand, across the inner
    # edge of the rim. These coordinates reflect with the anatomical frame.
    thumb_targets=[(-.044,-.038,-.020),(-.024,-.038,.014),(-.018,-.010,.021)]
    for joint in (1,2,3):
        f,a,n=thumb_targets[joint-1]
        bone=rig.pose.bones[f'finger1-{joint}.{side}'];target=centre+outward*f+axis*a+normal*n
        H.point_bone(rig,bone.name,target-bone.head)
    bpy.context.view_layer.update()

def hand_centre(rig,side):
    origin,axis,outward,normal=hand_frame(rig,side)
    return origin+normal*.027

def limb_pose(rig,side,target,leg=False):
    upper=rig.pose.bones[('upperleg01.' if leg else 'upperarm01.')+side];lower=rig.pose.bones[('lowerleg01.' if leg else 'lowerarm01.')+side];wrist=rig.pose.bones[('foot.' if leg else 'wrist.')+side]
    shoulder=upper.head.copy();a=(lower.head-shoulder).length;b=(wrist.head-lower.head).length
    line=target-shoulder;distance=min(line.length,a+b-.001);direction=line.normalized()
    along=(a*a-b*b+distance*distance)/(2*distance)
    sign=1 if shoulder.x>0 else -1
    pole=point((sign*.16,.87,.35) if leg else (sign*.27,.76,.18))-shoulder;pole-=direction*pole.dot(direction);pole.normalize()
    elbow=shoulder+direction*along+pole*math.sqrt(max(0,a*a-along*along))
    H.point_bone(rig,upper.name,elbow-upper.head)
    H.point_bone(rig,lower.name,target-lower.head)

bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/'intro_tyler_01.blend'),load_ui=False,use_scripts=False)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.animation_data_clear();rig.name='SeatedDriverRig'
for action in list(bpy.data.actions):bpy.data.actions.remove(action)
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':continue
    keys=[m.name for m in ob.data.materials]
    if any('Hair' in k or 'Eye' in k or 'Brow' in k for k in keys):bpy.data.objects.remove(ob,do_unlink=True);continue
    if any('Skin' in k for k in keys):
        mesh=ob.data;bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
        # First-person rendering excludes the driver's own head, while keeping
        # the chest, shoulders, hands, hips, thighs, lower legs and boots.
        head_groups={'head','jaw','neck01','neck02','neck03'}|{b.name for b in rig.pose.bones['head'].children_recursive}
        indices={v.index for v in mesh.vertices if any(ob.vertex_groups[g.group].name in head_groups and g.weight>.01 for g in v.groups)}
        remove=[v for v in bm.verts if v.index in indices or (ob.matrix_world@v.co).z>1.18]
        bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(mesh);bm.free()
        ob.name='Driver anatomical torso and hands'
    elif any('Outfit' in k for k in keys):
        ob.name='Driver field jacket sleeves and trousers'
        # Give the jacket actual clearance over the skin at bent elbows.
        for v in ob.data.vertices:v.co+=v.normal*.014
    else:ob.name='Driver boots'
    H.activate(ob)
    mod=ob.modifiers.new('Rounded anatomical contours','SUBSURF');mod.levels=1
    # Subdivide in the existing bind/rest space, before posing the armature.
    while ob.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in ob.data.polygons:p.use_smooth=True

receipt=[]
for side in ('L','R'):
    wrist=rig.pose.bones['wrist.'+side];curl(rig,side)
    sign=1 if wrist.head.x>0 else -1
    _,axis,outward,normal=hand_frame(rig,side)
    wheel_up=point((0,math.cos(math.radians(18)),math.sin(math.radians(18))))
    radial=(Vector((sign*.211,0,0))+wheel_up*.102).normalized()
    tangent=(Vector((sign*.102,0,0))-wheel_up*.211).normalized()
    # Map independent native hands onto mirrored radial/tangent frames.
    # Both palms face the wheel; both thumb sides face into its opening.
    source=Matrix((outward,axis,outward.cross(axis))).transposed()
    destination=Matrix((radial,tangent,radial.cross(tangent))).transposed()
    rotation=destination@source.inverted()
    assert rotation.determinant()>.999,'A hand was reflected instead of rotated'
    m=wrist.matrix.copy()
    wrist.matrix=Matrix.Translation(m.translation)@rotation.to_4x4()@m.to_quaternion().to_matrix().to_4x4();bpy.context.view_layer.update()
    desired_rotation=wrist.matrix.to_quaternion().copy()
    target=point((sign*.211,1.12-.07+.102*math.cos(math.radians(18)),.50-.04+.102*math.sin(math.radians(18))))
    for n in range(3):
        wrist_target=wrist.head+(target-hand_centre(rig,side));limb_pose(rig,side,wrist_target)
        m=wrist.matrix.copy();wrist.matrix=Matrix.Translation(m.translation)@desired_rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
    _,axis,outward,normal=hand_frame(rig,side)
    receipt.append({'side':side,'contact':list(hand_centre(rig,side)),'target':list(target),'error':(hand_centre(rig,side)-target).length,
        'palm_normal':list(normal),'finger_row':list(axis),'thumb_tip':list(rig.pose.bones['finger1-3.'+side].tail)})

# Every corresponding hand joint must be the anatomical reflection of its
# counterpart, including the thumb. This catches asymmetric curl/roll bugs.
mirror_error=max((rig.pose.bones[f'finger{i}-{j}.L'].tail-Vector((-rig.pose.bones[f'finger{i}-{j}.R'].tail.x,rig.pose.bones[f'finger{i}-{j}.R'].tail.y,rig.pose.bones[f'finger{i}-{j}.R'].tail.z))).length for i in range(1,6) for j in range(1,4))
assert mirror_error<.001,f'Left/right finger pose mismatch: {mirror_error}'

for side in ('L','R'):
    foot=rig.pose.bones['foot.'+side];sign=1 if foot.head.x>0 else -1
    limb_pose(rig,side,point((sign*.15,.45,.68)),True)
    H.point_bone(rig,foot.name,(0,-1,-.05))

# Bake this wheel pose as the bind pose, keeping the complete skeletal arm
# chain for runtime two-bone articulation rather than stretching a rigid mesh.
for ob in list(bpy.context.scene.objects):
    if ob.type=='MESH':
        for mod in list(ob.modifiers):
            if mod.type=='ARMATURE':H.activate(ob);bpy.ops.object.modifier_apply(modifier=mod.name)
H.activate(rig);bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
for ob in list(bpy.context.scene.objects):
    if ob.type=='MESH':mod=ob.modifiers.new('Anatomical driver skeleton','ARMATURE');mod.object=rig
save('intro_driver_01',False)
(H.ROOT/'ArtSource/IntroDriverPose.json').write_text(json.dumps({'source':'intro_tyler_01.blend','license':'retained CC0 MakeHuman anatomy/clothes; healthy IntroSkin0','mirrored_joint_error_m':mirror_error,'hands':receipt},indent=2)+'\n')
print('SEATED DRIVER',json.dumps(receipt),flush=True)
