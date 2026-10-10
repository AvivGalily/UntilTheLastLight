"""Adapt Poly Haven CC0 scans to metre-scale FBX and a three-level game budget."""
import bpy,sys,math,numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).parent));import licensed_humans as H
ROOT=H.ROOT;V=H.VENDOR/'polyhaven'
def image_array(file,size=2048,color=False):
    im=bpy.data.images.load(str(file),check_existing=False)
    # Read as raw channels, including sRGB albedo bytes (do not double encode).
    im.colorspace_settings.name='Non-Color';im.scale(size,size)
    a=np.array(im.pixels[:],dtype=np.float32).reshape(size,size,4)[::-1].copy();bpy.data.images.remove(im);return a
def texture_mat(slug,prefix,key,alpha=False):
    root=V/slug/'textures'
    def file(kind):
        candidates=[p for p in root.iterdir() if p.name.startswith(prefix+'_'+kind+'_')]
        return candidates[0] if candidates else None
    base=image_array(file('diff'));normal=image_array(file('nor_gl'));rough=image_array(file('rough'))[:,:,0]
    if alpha:base[:,:,3]=image_array(file('alpha'))[:,:,0]
    H.png(H.TEX/(key+'_BaseColor.png'),base);H.png(H.TEX/(key+'_Normal.png'),normal)
    mask=np.ones_like(base);mask[:,:,0]=0;mask[:,:,2]=.5;mask[:,:,3]=1-rough;H.png(H.TEX/(key+'_Mask.png'),mask)
    m=bpy.data.materials.new(key);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF')
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(H.TEX/(key+'_BaseColor.png')));m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if alpha:m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
    return m
def emit(source,id,materials,budget,height=None):
    mesh=source.data.copy();mesh.transform(source.matrix_world)
    # Poly Haven's Geometry Nodes stores UVMap as a 3-vector CORNER
    # attribute. FBX only exports actual 2-vector UV layers.
    if not mesh.uv_layers:
        attr=mesh.attributes.get('UVMap')
        if not attr or attr.domain!='CORNER':raise ValueError('Missing source UVs: '+source.name)
        uvdata=[tuple(v.vector[:2]) for v in attr.data];mesh.attributes.remove(attr)
        uv=mesh.uv_layers.new(name='UVMap')
        for i,value in enumerate(uvdata):uv.data[i].uv=value
        for face in mesh.polygons:
            mat=mesh.materials[face.material_index]
            if mat and mat.name=='fir_tree_01_bark':
                for i in face.loop_indices:uv.data[i].uv.x*=1.2;uv.data[i].uv.y*=.1
    xyz=np.array([v.co[:] for v in mesh.vertices]);low=xyz[:,2].min();foot=xyz[xyz[:,2]<low+.22];cx,cy=foot[:,:2].mean(axis=0)
    scale=height/(xyz[:,2].max()-low) if height else 1
    for v in mesh.vertices:v.co=(v.co-Vector((cx,cy,low)))*scale
    for f in mesh.polygons:f.use_smooth=True
    for slot in range(len(mesh.materials)):
        old=mesh.materials[slot];mesh.materials[slot]=materials.get(old.name if old else '',next(iter(materials.values())))
    generated=[]
    for level,target in enumerate(budget):
        ob=bpy.data.objects.new(id+'_LOD'+str(level),mesh.copy());bpy.context.collection.objects.link(ob);H.activate(ob)
        ob.data.calc_loop_triangles();count=len(ob.data.loop_triangles)
        if count>target:
            mod=ob.modifiers.new('Game distance budget','DECIMATE');mod.ratio=target/count;bpy.ops.object.modifier_apply(modifier=mod.name)
        generated.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in generated:ob.select_set(True)
    # Save only the finished asset, never the multi-million-polygon source library.
    scene=bpy.context.scene;originals=[o for o in list(scene.objects) if o not in generated]
    for ob in originals:
        for col in list(ob.users_collection):col.objects.unlink(ob)
    path=H.SOURCE/(id+'.blend');bpy.data.libraries.write(str(path),{scene},path_remap='RELATIVE',compress=True)
    bpy.ops.export_scene.fbx(filepath=str(H.MESH/(id+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=False,bake_anim=False)
    for ob in originals:scene.collection.objects.link(ob)
    for ob in generated:bpy.data.objects.remove(ob,do_unlink=True)
    print('FOREST EXPORTED',id,budget,flush=True)
def main():
    for slug,prefix in [('rock_moss_set_01','PH_MossRock'),('dead_tree_trunk','PH_FallenWood'),('tree_stump_02','PH_Stump')]:
        bpy.ops.wm.open_mainfile(filepath=str(V/slug/(slug+'.blend')));mat=texture_mat(slug,slug,prefix);objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
        if slug=='rock_moss_set_01':
            for i,ob in enumerate(sorted(objects,key=lambda o:o.name)[:3]):emit(ob,'moss_rock_0'+str(i+1),{slug:mat},[8000,2400,650])
        else:emit(objects[0],'fallen_log_01' if slug=='dead_tree_trunk' else 'scanned_stump_01',{slug:mat},[8000,2600,650])
    slug='fir_tree_01';bpy.ops.wm.open_mainfile(filepath=str(V/slug/(slug+'.blend')))
    mapping={}
    for suffix,key in [('bark','PH_FirBark'),('trunk_b','PH_FirTrunkB'),('trunk_c','PH_FirTrunkC'),('twig','PH_FirTwig')]:mapping[slug+'_'+suffix]=texture_mat(slug,slug+'_'+suffix,key,suffix=='twig')
    mapping[slug+'_dead_branches']=mapping[slug+'_bark']
    for variant,id in [('c','blackpine_01'),('b','blackpine_02')]:emit(bpy.data.objects[slug+'_'+variant+'_LOD2'],id,mapping,[16000,5000,1400],10)
if __name__=='__main__':main()
