"""CC0 human anatomy: a full-body shadow and an actual closed axe grip."""
import bpy,bmesh,math,sys
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

def bake(name,keep=None,transform=None):
    deps=bpy.context.evaluated_depsgraph_get();parts=[]
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        if keep is not None:
            # Delete by original deform groups before evaluating the fitted surface.
            accepted={v.index for v in ob.data.vertices if keep(ob,v)}
            mesh=ob.data.copy();bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
            bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in accepted],context='VERTS');bm.to_mesh(mesh);bm.free();ob.data=mesh
            if not mesh.polygons:continue
        bpy.context.view_layer.update()
        mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        mesh.transform((transform or Matrix.Identity(4))@ob.matrix_world)
        copy=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(copy);parts.append(copy)
    for ob in list(bpy.context.scene.objects):
        if ob not in parts:bpy.data.objects.remove(ob,do_unlink=True)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in parts:ob.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name
    if name=='player_axe_hand_01':
        smooth=ob.modifiers.new('Continuous finger and knuckle contours','SUBSURF');smooth.levels=1;smooth.uv_smooth='PRESERVE_BOUNDARIES'
        bpy.ops.object.modifier_apply(modifier=smooth.name)
    for p in ob.data.polygons:p.use_smooth=True
    bpy.ops.wm.save_as_mainfile(filepath=str(H.SOURCE/(name+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(name+'.fbx')),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)
    print('PLAYER ASSET',name,'vertices',len(ob.data.vertices),flush=True)

def body():
    rig=open_source()
    for side,sign in [('L',1),('R',-1)]:
        H.point_bone(rig,'upperarm01.'+side,(sign*.20,0,-1))
        H.point_bone(rig,'lowerarm01.'+side,(sign*.10,-.32,-1))
    bake('player_body_01')

def grip():
    rig=open_source()
    # Curl real finger segments around a 3 cm shaft, preserving skin UVs/nails.
    roots=[rig.pose.bones['finger'+str(i)+'-1.R'].head.copy() for i in range(2,6)]
    axis=(roots[-1]-roots[0]).normalized();origin=sum(roots,Vector())/4
    outward=(rig.pose.bones['finger3-2.R'].tail-origin);outward-=axis*outward.dot(axis);outward.normalize()
    normal=axis.cross(outward).normalized();centre=origin+outward*.037-normal*.018
    for finger in range(2,6):
        root=rig.pose.bones[f'finger{finger}-1.R'].head.copy();along=(root-centre).dot(axis)
        for joint,angle in [(1,38),(2,102),(3,160)]:
            radians=math.radians(angle)
            target=centre+axis*along+outward*(math.sin(radians)*.026)+normal*(math.cos(radians)*.026)
            bone=rig.pose.bones[f'finger{finger}-{joint}.R']
            H.point_bone(rig,bone.name,target-bone.head)
    # Opposed thumb closes across the fingers.
    for joint in [1,2,3]:
        bone=rig.pose.bones[f'finger1-{joint}.R']
        target=centre+axis*.035+normal*.024+outward*(.032-joint*.014)
        H.point_bone(rig,bone.name,target-bone.head)
    bpy.context.view_layer.update()
    # Match the shaft to Blender Z; the palm is offset beside it, not over it.
    arm_groups={'lowerarm01.R'}|{bone.name for bone in rig.pose.bones['lowerarm01.R'].children_recursive}
    frame=Matrix((normal,outward,axis)).transposed().to_4x4()
    transform=frame.inverted()@Matrix.Translation(-centre)
    def keep(ob,v):
        return any(ob.vertex_groups[g.group].name in arm_groups and g.weight>.025 for g in v.groups)
    bake('player_axe_hand_01',keep,transform)

if __name__=='__main__':body();grip()
