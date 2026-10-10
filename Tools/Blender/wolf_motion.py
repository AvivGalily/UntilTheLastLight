"""Focused attack/death refresh on the retained textured and weighted wolf."""
import bpy,math,sys
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[2]
def apply(rig):
    for name in ['Wolf_Bite','Wolf_Death']:
        if name in bpy.data.actions:bpy.data.actions.remove(bpy.data.actions[name])
    rig.animation_data_create()
    def rotate(name,angle,axis=(1,0,0)):
        bone=rig.pose.bones[name];rest=bone.bone.matrix_local.to_quaternion()
        bone.rotation_quaternion=rest.inverted()@Quaternion(Vector(axis),math.radians(angle))@rest
    def translate(name,xyz):rig.pose.bones[name].location=rig.pose.bones[name].bone.matrix_local.to_3x3().inverted()@Vector(xyz)
    def ease(a,b,t):
        q=max(0,min(1,(t-a)/(b-a)));return q*q*(3-2*q)
    for name,end in [('Wolf_Bite',39),('Wolf_Death',72)]:
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame in range(1,end+2):
            t=(frame-1)/30
            for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0)
            if name=='Wolf_Bite':
                load=ease(0,.24,t);strike=ease(.24,.56,t);recover=ease(.67,1.3,t)
                jaw=10+36*ease(0,.34,t)-43*ease(.40,.60,t)+7*recover
                rotate('b_Jaw',jaw);rotate('b_Neck',-9+9*load-30*strike+30*recover)
                rotate('b_Head',8*load-20*strike+20*recover)
                rotate('b_Spine02',-10*load+14*strike-4*recover)
                translate('b_Hip',(0,.035*load-.09*strike+.055*recover,-.055*load+.095*strike-.04*recover))
                for side,sign in [('Left',1),('Right',-1)]:
                    rotate('b_'+side+'UpperArm',-20*load+52*strike-32*recover+sign*3*strike)
                    rotate('b_'+side+'ForeArm',-22*load-24*strike+46*recover)
                    rotate('b_'+side+'Hand01',17*load-30*strike+13*recover)
                    rotate('b_'+side+'Leg01',22*load-30*strike+8*recover)
                    rotate('b_'+side+'Leg02',-22*load+18*strike+4*recover)
                    rotate('b_'+side+'Foot01',-8*load+12*strike-4*recover)
                rotate('b_Tail01',-15*load+10*strike+5*recover)
            else:
                shock=ease(0,.16,t)*(1-ease(.16,.37,t));fall=ease(.14,.77,t)
                twitch=math.sin((t-.77)*23)*math.exp(-max(0,t-.77)*5) if t>.77 else 0
                rotate('b_Root',88*fall,(0,1,0));translate('b_Root',(.12*fall,.08*fall,.02*shock-.09*fall))
                rotate('b_Spine02',-11*shock+7*fall+3*twitch)
                rotate('b_Neck',18*shock-9*fall);rotate('b_Head',-22*shock-16*fall+5*twitch)
                rotate('b_Jaw',8+16*fall+4*twitch);rotate('b_Tail01',-20*fall+8*twitch)
                for side,sign in [('Left',1),('Right',-1)]:
                    rotate('b_'+side+'UpperArm',-24*shock-35*fall+sign*10*fall+8*twitch)
                    rotate('b_'+side+'ForeArm',-48*fall-12*twitch);rotate('b_'+side+'Hand01',24*fall)
                    rotate('b_'+side+'Leg01',26*shock+35*fall-7*twitch)
                    rotate('b_'+side+'Leg02',-50*fall+10*twitch);rotate('b_'+side+'Foot01',28*fall)
                # Keep the lowest deformed skin at ground contact throughout the fall.
                bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
                minimum=min((ob.matrix_world@Vector(corner)).z for ob in bpy.context.scene.objects if ob.type=='MESH' for corner in ob.evaluated_get(deps).bound_box)
                root=rig.pose.bones['b_Root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,.018-minimum))
            for bone in rig.pose.bones:
                bone.keyframe_insert('rotation_quaternion',frame=frame);bone.keyframe_insert('location',frame=frame)
        action.use_fake_user=True
    rig.animation_data.action=bpy.data.actions['Wolf_Idle'];bpy.context.scene.render.fps=30;bpy.context.scene.frame_set(1)
if __name__=='__main__':
    source=ROOT/'ArtSource/Blender/zombie_wolf_01.blend'
    bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False,use_scripts=False)
    apply(next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'))
    bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
    print('WOLF MOTION: anticipation, push-off, .60s jaw contact and recovery; shock, side collapse and damped settling',flush=True)
