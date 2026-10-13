"""Weighted CC0 upper arms, elbows, wrists and proper native equipment grips."""
import bpy,bmesh,sys,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
import player_presence as P

receipts=[]
for trigger,side,radius in ((False,'R',.027),(True,'R',.030),(False,'L',.040)):
    rig,frame,name=P.grip(trigger,side,radius,True)
    allowed={'upperarm01.'+side}|{b.name for b in rig.pose.bones['upperarm01.'+side].children_recursive}
    meshes=[]
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        skin=any(m.name.startswith('HumanSkin') for m in ob.data.materials)
        cloth=any('Outfit' in m.name for m in ob.data.materials)
        if not skin and not cloth:bpy.data.objects.remove(ob,do_unlink=True);continue
        accepted={v.index for v in ob.data.vertices if sum(g.weight for g in v.groups if ob.vertex_groups[g.group].name in allowed)>.5}
        bm=bmesh.new();bm.from_mesh(ob.data);bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in accepted],context='VERTS');bm.to_mesh(ob.data);bm.free()
        if not len(ob.data.polygons):bpy.data.objects.remove(ob,do_unlink=True);continue
        for group in list(ob.vertex_groups):
            if group.name not in allowed:ob.vertex_groups.remove(group)
        for v in ob.data.vertices:
            total=sum(g.weight for g in v.groups)
            for g in list(v.groups):ob.vertex_groups[g.group].add([v.index],g.weight/total,'REPLACE')
            if cloth:v.co+=v.normal*.012
        H.activate(ob);sub=ob.modifiers.new('Smooth anatomical skin and sleeve','SUBSURF');sub.levels=2 if skin else 1
        while ob.modifiers.find(sub.name)>0:bpy.ops.object.modifier_move_up(modifier=sub.name)
        bpy.ops.object.modifier_apply(modifier=sub.name)
        for m in ob.data.materials:m.name='PlayerSkin' if skin else 'PlayerSleeve'
        for p in ob.data.polygons:p.use_smooth=True
        meshes.append(ob)
    for ob in meshes:
        for mod in list(ob.modifiers):
            if mod.type=='ARMATURE':H.activate(ob);bpy.ops.object.modifier_apply(modifier=mod.name)
        ob.data.transform(frame@ob.matrix_world);ob.parent=None;ob.matrix_world=Matrix.Identity(4)
    H.activate(rig);bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.transform(frame);rig.matrix_world=Matrix.Identity(4);rig.name='EquipmentArm_'+side
    bpy.ops.object.select_all(action='DESELECT')
    for ob in meshes:ob.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();ob=bpy.context.object;ob.name='Weighted anatomical '+name
    modifier=ob.modifiers.new('Shoulder elbow wrist and finger weights','ARMATURE');modifier.object=rig
    ob.parent=rig;ob.matrix_parent_inverse=Matrix.Identity(4)
    # FBX reads evaluated pose matrices, not just the changed rest-bone data.
    bpy.context.view_layer.update()
    vertex_count=len(ob.data.vertices)
    path=H.SOURCE/(name+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
    # Reload also refreshes Blender's FBX armature-rest cache after a data-space
    # rebase. Without it the mesh is canonical but exported bones stay in body space.
    bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(name+'.fbx')),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=False)
    receipts.append({'asset':name,'side':side,'gripRadius':radius,'upper':'upperarm01.'+side,'lower':'lowerarm01.'+side,'wrist':'wrist.'+side,'geometry':'native CC0 skin and fitted clothing; normalized arm-only bone weights','source':'hanging_survivor_01.blend','runtime':'length-preserving shoulder/elbow/wrist IK; no vertex shear'})
    print('WEIGHTED FIRST PERSON ARM',name,vertex_count,flush=True)
(H.ROOT/'ArtSource/FirstPersonArms.json').write_text(json.dumps({'arms':receipts},indent=2)+'\n')
