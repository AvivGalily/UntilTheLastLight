"""Preserved head variants retain the credited CC0 NewDLC wolf anatomy and UVs."""
import bpy,bmesh,sys,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).parent))
import forest_bodies as F
F.ROOT=ROOT
for animal in ['wolf','deer']:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Blender/zombie_wolf_01.blend'),load_ui=False,use_scripts=False)
    bpy.context.preferences.filepaths.save_version=0
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.animation_data_clear();bpy.context.scene.frame_set(1)
    for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=(1,0,0,0);bone.location=(0,0,0)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];parts=[]
    for obj in meshes:
        groups={g.index:g.name for g in obj.vertex_groups}
        keep={v.index for v in obj.data.vertices if sum(g.weight for g in v.groups if groups.get(g.group,'') in ['b_Head','b_Jaw'])>.3}
        if not keep:continue
        evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=bpy.data.meshes.new_from_object(evaluated);copy=bpy.data.objects.new('Preserved head anatomy',mesh);bpy.context.collection.objects.link(copy);copy.matrix_world=obj.matrix_world
        bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(mesh);bm.free()
        parts.append(copy)
    for obj in list(bpy.context.scene.objects):
        if obj not in parts:bpy.data.objects.remove(obj,do_unlink=True)
    if not parts:raise RuntimeError('CC0 wolf head groups missing')
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();head=bpy.context.object
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # Fit the anatomical head, retaining authored fur/skin UVs, into a 22 cm jar.
    vertices=head.data.vertices;minimum=Vector(tuple(min(v.co[i] for v in vertices) for i in range(3)));maximum=Vector(tuple(max(v.co[i] for v in vertices) for i in range(3)))
    center=(minimum+maximum)*.5;scale=.31/max((maximum-minimum).x,(maximum-minimum).y,(maximum-minimum).z)
    for v in vertices:
        p=(v.co-center)*scale
        # The source wolf faces -Y; Blender/Unity export preserves that forward.
        if animal=='deer':p.x*=.76;p.y*=1.15
        p.z+=.205;v.co=p
    # Muted embalmed fur uses the existing licensed fur atlas and relief.
    for mat in head.data.materials:
        if mat and mat.name.startswith('WolfSkin'):mat.name='LabPreservedFur'
    for poly in head.data.polygons:poly.use_smooth=True
    F.PARTS.clear();F.MATS.clear()
    for key,colour in [('LabPreservedTissue',(.5,.44,.31)),('AbyssTeeth',(.6,.54,.4))]:F.material(key,colour,.6)
    F.sphere('Preserved neck cut',(0,.11,-.03),(.075,.065,.065),'LabPreservedTissue',32,20)
    if animal=='deer':
        # A fictional ungulate archive specimen, adapted from the same CC0 head.
        for side in [-1,1]:
            F.tube('Branched antler',[(side*.055,.32,-.04),(side*.075,.42,-.06),(side*.12,.50,-.02)],[.013,.009,.001],'AbyssTeeth',16)
            F.tube('Antler tine',[(side*.075,.41,-.06),(side*.025,.46,.005)],[.01,.001],'AbyssTeeth',14)
    bpy.ops.object.select_all(action='DESELECT')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=head;bpy.ops.object.join()
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Blender'/('lab_'+animal+'_head_01.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_Game/Art/Meshes'/('lab_'+animal+'_head_01.fbx')),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)
    print('PRESERVED CC0 HEAD',animal,len(head.data.vertices),flush=True)
