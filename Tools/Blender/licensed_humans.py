"""Pose CC0 MakeHuman anatomy/clothes in Blender and export four forest casualties.

Uses the pinned, unmodified MPFB 2.0.17 authoring library from ArtSource/Vendor.
The generated game assets contain CC0 graphics, not the GPL authoring addon.
"""
import argparse, json, math, shutil, struct, sys, zlib
from pathlib import Path
import bpy, numpy as np
from mathutils import Vector, Matrix, Quaternion

ROOT=Path(__file__).resolve().parents[2]
VENDOR=ROOT/'ArtSource/Vendor'; SYSTEM=VENDOR/'human-system-cc0'; TEX=ROOT/'Assets/_Game/Art/Textures'
SOURCE=ROOT/'ArtSource/Blender'; MESH=ROOT/'Assets/_Game/Art/Meshes'

def png(path,rgba):
    rgba=np.uint8(np.clip(rgba,0,1)*255);h,w,_=rgba.shape
    def chunk(k,v):return struct.pack('!I',len(v))+k+v+struct.pack('!I',zlib.crc32(k+v)&0xffffffff)
    # Rows passed here are image-file top to bottom, not Blender pixel order.
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(b'\0'+r.tobytes() for r in rgba),7))+chunk(b'IEND',b''))

def material(key,mhmat=None):
    if key in bpy.data.materials:return bpy.data.materials[key]
    values={}
    if mhmat:
        for line in Path(mhmat).read_text(encoding='utf-8',errors='replace').splitlines():
            fields=line.strip().split(maxsplit=1)
            if len(fields)==2 and not line.startswith('#'):values[fields[0]]=fields[1]
    mat=bpy.data.materials.new(key);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF')
    base=Path(mhmat).parent/values['diffuseTexture'] if 'diffuseTexture' in values else None
    if base and base.exists():
        target=TEX/(key+'_BaseColor.png')
        if base.suffix.lower()=='.png':shutil.copyfile(base,target)
        else:
            image=bpy.data.images.load(str(base),check_existing=True);image.filepath_raw=str(target);image.file_format='PNG';image.save()
        image=bpy.data.images.load(str(target),check_existing=True);node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
        mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
        if 'Hair' in key or 'Brow' in key or 'Lash' in key:mat.node_tree.links.new(node.outputs['Alpha'],bs.inputs['Alpha'])
    else:
        color=(.57,.51,.42,1) if 'Eye' in key else (.11,.085,.055,1)
        png(TEX/(key+'_BaseColor.png'),np.tile(color,(16,16,1)))
    normal=Path(mhmat).parent/values['normalmapTexture'] if 'normalmapTexture' in values else None
    if normal and normal.exists():shutil.copyfile(normal,TEX/(key+'_Normal.png'))
    else:png(TEX/(key+'_Normal.png'),np.tile((.5,.5,1,1),(32,32,1)))
    image=bpy.data.images.load(str(TEX/(key+'_Normal.png')),check_existing=True);image.colorspace_settings.name='Non-Color'
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;node=mat.node_tree.nodes.new('ShaderNodeNormalMap');node.inputs['Strength'].default_value=.65
    mat.node_tree.links.new(tex.outputs['Color'],node.inputs['Color']);mat.node_tree.links.new(node.outputs['Normal'],bs.inputs['Normal'])
    smooth=.29 if 'Skin' in key else .14 if 'Outfit' in key else .38 if 'Eyes' in key else .12
    mask=np.tile((0,1,.5,smooth),(256,256,1)).astype(np.float32)
    if 'aomapTexture' in values:
        source=Path(mhmat).parent/values['aomapTexture']
        if source.exists():
            image=bpy.data.images.load(str(source),check_existing=True);image.colorspace_settings.name='Non-Color';image.scale(256,256)
            mask[:,:,1]=np.array(image.pixels[:]).reshape(256,256,4)[::-1,:,0]
    png(TEX/(key+'_Mask.png'),mask);bs.inputs['Roughness'].default_value=1-smooth
    bs.inputs['IOR'].default_value=1.4
    if 'Skin' in key:bs.inputs['Subsurface Weight'].default_value=.09
    return mat

def activate(ob):
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob

def point_bone(rig,name,direction):
    bone=rig.pose.bones.get(name)
    if not bone:return
    bpy.context.view_layer.update();matrix=bone.matrix.copy()
    q=bone.y_axis.normalized().rotation_difference(Vector(direction).normalized())
    bone.matrix=Matrix.Translation(matrix.translation)@q.to_matrix().to_4x4()@matrix.to_quaternion().to_matrix().to_4x4()
    bpy.context.view_layer.update()

def main():
    sys.path.insert(0,str(next((VENDOR/'mpfb-2.0.17').glob('*/src'))))
    import addon_utils
    bpy.utils.extension_path_user=lambda package,path='',create=False:str(VENDOR/'mpfb-workspace'/path)
    addon_utils.enable('mpfb',default_set=True)
    from mpfb.services.humanservice import HumanService
    from mpfb.services.targetservice import TargetService
    outfits=['male_casualsuit05','male_casualsuit01','male_worksuit01','male_casualsuit03']
    skins=[VENDOR/'male-skins-cc0/skins/mindfront_aksel_skin/mindfront_aksel_skin.mhmat',
           VENDOR/'male-skins-cc0/skins/jartur69_old_slavic_male_with_genitals_and_beard/jartur69_old_slavic_male_with_genitals_and_beard.mhmat']
    for index in range(4):
        bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
        bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
        macro=TargetService.get_default_macro_info_dict();macro.update(gender=1,age=[.55,.78,.43,.62][index],muscle=[.48,.33,.65,.42][index],weight=[.48,.44,.58,.4][index])
        human=HumanService.create_human(macro_detail_dict=macro);human.data.materials.clear();human.data.materials.append(material('HumanSkin'+str(index%2),skins[index%2]))
        rig=HumanService.add_builtin_rig(human,'default')
        def asset(key,subpath,kind='Clothes'):
            file=SYSTEM/subpath;obj=HumanService.add_mhclo_asset(str(file),human,asset_type=kind,subdiv_levels=1,import_subrig=False)
            matfile=None
            for line in file.read_text(encoding='utf-8',errors='replace').splitlines():
                if line.startswith('material '):matfile=(file.parent/line.split(maxsplit=1)[1]).resolve();break
            if not matfile or not matfile.exists():matfile=next(file.parent.glob('*.mhmat'),None)
            obj.data.materials.clear();obj.data.materials.append(material(key,matfile));return obj
        outfit=outfits[index];asset('HumanOutfit'+str(index),f'clothes/{outfit}/{outfit}.mhclo')
        asset('HumanBoots','clothes/shoes04/shoes04.mhclo')
        asset('HumanHair','hair/short01/short01.mhclo','Hair')
        asset('HumanEyes','eyes/high-poly/high-poly.mhclo','Eyes')
        asset('HumanBrows','eyebrows/eyebrow001/eyebrow001.mhclo','Eyebrows')
        # Real anatomical joints and relaxed fingers, rather than disconnected proxy parts.
        for side,sign in [('L',1),('R',-1)]:
            outward=[.2,.37,.12,.28][index]
            point_bone(rig,'upperarm01.'+side,(sign*outward,-.05,-1))
            point_bone(rig,'lowerarm01.'+side,(sign*(.15 if index%2 else .5),-.2,-1))
            for bone in rig.pose.bones:
                if bone.name.startswith('finger') and bone.name.endswith('.'+side):
                    bone.rotation_mode='XYZ';bone.rotation_euler.x=math.radians(7+(index%2)*4)
        # Turn the head a little and let the legs settle at different angles.
        for name,angles in [('head',(0,[8,-12,4,-7][index],[5,-7,10,-4][index])),('upperleg01.L',(2,0,-3)),('upperleg01.R',(-2,0,5))]:
            bone=rig.pose.bones.get(name)
            if bone:bone.rotation_mode='XYZ';bone.rotation_euler=tuple(math.radians(a) for a in angles)
        bpy.context.view_layer.update()
        # Bake the fitted clothing, shape targets, helper masks and skeleton into
        # one static, correctly UV-mapped corpse. Authoring rig remains reproducible.
        objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
        baked=[];deps=bpy.context.evaluated_depsgraph_get()
        rotation=Matrix.Rotation(math.pi,4,'Z')@Matrix.Rotation(-math.pi/2,4,'X')
        for ob in objects:
            evaluated=ob.evaluated_get(deps);mesh=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=deps)
            mesh.transform(rotation@ob.matrix_world)
            copy=bpy.data.objects.new('Anatomy '+ob.name,mesh);bpy.context.collection.objects.link(copy);baked.append(copy)
        for ob in objects+[rig]:bpy.data.objects.remove(ob,do_unlink=True)
        low=min(v.co.z for ob in baked for v in ob.data.vertices);ys=[v.co.y for ob in baked for v in ob.data.vertices]
        centre=(min(ys)+max(ys))/2
        for ob in baked:
            for v in ob.data.vertices:v.co.y-=centre;v.co.z+=.025-low
            for poly in ob.data.polygons:poly.use_smooth=True
        bpy.ops.object.select_all(action='DESELECT')
        for ob in baked:ob.select_set(True)
        bpy.context.view_layer.objects.active=baked[0];bpy.ops.object.join();body=bpy.context.object
        # Subdivision improves the face silhouette without changing UV placement.
        sub=body.modifiers.new('Surface continuity','SUBSURF');sub.levels=1;activate(body);bpy.ops.object.modifier_apply(modifier=sub.name)
        body.data.calc_loop_triangles();count=len(body.data.loop_triangles)
        if count>76000:
            dec=body.modifiers.new('Game mesh budget','DECIMATE');dec.ratio=76000/count;bpy.ops.object.modifier_apply(modifier=dec.name)
        body.name=f'forest_body_0{index+1}_LOD0'
        for level,ratio in [(1,.42),(2,.13)]:
            ob=body.copy();ob.data=body.data.copy();ob.name=f'forest_body_0{index+1}_LOD{level}';bpy.context.collection.objects.link(ob)
            activate(ob);dec=ob.modifiers.new('Distance mesh','DECIMATE');dec.ratio=ratio;bpy.ops.object.modifier_apply(modifier=dec.name);ob.hide_render=True
        # Source-only embedded texture paths remain stable relative to this project.
        path=SOURCE/f'forest_body_0{index+1}.blend';bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
        for ob in bpy.context.scene.objects:ob.hide_render=False
        bpy.ops.export_scene.fbx(filepath=str(MESH/f'forest_body_0{index+1}.fbx'),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,bake_anim=False)
        print('HUMAN EXPORTED',index+1,tuple(body.dimensions),len(body.data.vertices),flush=True)

if __name__=='__main__':main()
