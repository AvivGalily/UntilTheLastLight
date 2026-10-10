"""Apply original mouth PBR maps and sharpen existing rigged tooth tips in place."""
import argparse,math,runpy,sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
helpers=runpy.run_path(str(Path(__file__).with_name('skin_material.py')))

def bake_lip_edge(root,skin):
    # Feather the generated lip tissue into the rest-space skin atlas, preserving
    # continuous color across seams and every subsequent jaw deformation.
    folder=root/'Assets/_Game/Art/Textures';mesh=skin.data;material=next(m for m in mesh.materials if m.name.split('.')[0]=='CrawlerSkin')
    nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
    def node(kind):return nodes.new(kind)
    def math_node(op,value=None):
        n=node('ShaderNodeMath');n.operation=op
        if value is not None:n.inputs[1].default_value=value
        return n
    coord=node('ShaderNodeTexCoord');xyz=node('ShaderNodeSeparateXYZ');links.new(coord.outputs['Object'],xyz.inputs[0])
    delta=node('ShaderNodeVectorMath');delta.operation='SUBTRACT';delta.inputs[1].default_value=(0,-.93,1.43);links.new(coord.outputs['Object'],delta.inputs[0])
    scale=node('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(1/.183,0,1/.235);links.new(delta.outputs[0],scale.inputs[0])
    radius=node('ShaderNodeVectorMath');radius.operation='LENGTH';links.new(scale.outputs[0],radius.inputs[0])
    edge=node('ShaderNodeMapRange');edge.interpolation_type='SMOOTHSTEP';edge.inputs['From Min'].default_value=1.0;edge.inputs['From Max'].default_value=1.45;edge.inputs['To Min'].default_value=.9;edge.inputs['To Max'].default_value=0;links.new(radius.outputs['Value'],edge.inputs['Value'])
    depth=math_node('MULTIPLY',-1);links.new(xyz.outputs['Y'],depth.inputs[0]);front=node('ShaderNodeMapRange');front.interpolation_type='SMOOTHSTEP';front.inputs['From Min'].default_value=.83;front.inputs['From Max'].default_value=.93;links.new(depth.outputs[0],front.inputs['Value'])
    mask=math_node('MULTIPLY');links.new(edge.outputs[0],mask.inputs[0]);links.new(front.outputs[0],mask.inputs[1])
    u=math_node('MULTIPLY_ADD',1/.40);u.inputs[2].default_value=.5;links.new(xyz.outputs['X'],u.inputs[0])
    v=math_node('MULTIPLY_ADD',1/.50);v.inputs[2].default_value=-1.18/.50;links.new(xyz.outputs['Z'],v.inputs[0])
    combine=node('ShaderNodeCombineXYZ');links.new(u.outputs[0],combine.inputs[0]);links.new(v.outputs[0],combine.inputs[1])
    lip=node('ShaderNodeTexImage');lip.image=bpy.data.images.load(str(folder/'CrawlerGums_BaseColor.png'),check_existing=True);links.new(combine.outputs[0],lip.inputs['Vector'])
    uv=node('ShaderNodeUVMap');uv.uv_map='SkinAtlas';base=node('ShaderNodeTexImage');base.image=bpy.data.images.load(str(folder/'CrawlerSkin_Anatomy.png'),check_existing=False);links.new(uv.outputs[0],base.inputs['Vector'])
    mix=node('ShaderNodeMixRGB');links.new(mask.outputs[0],mix.inputs[0]);links.new(base.outputs['Color'],mix.inputs[1]);links.new(lip.outputs['Color'],mix.inputs[2])
    emit=node('ShaderNodeEmission');links.new(mix.outputs[0],emit.inputs['Color']);output=node('ShaderNodeOutputMaterial');links.new(emit.outputs[0],output.inputs[0])
    image=bpy.data.images.new('CrawlerMouth_Anatomy',4096,4096,alpha=True);image.colorspace_settings.name='sRGB';image.filepath_raw=str(folder/'CrawlerMouth_Anatomy.png');image.file_format='PNG'
    target=node('ShaderNodeTexImage');target.image=image;nodes.active=target
    scratch=bpy.data.images.new('Mouth bake scratch',16,16,alpha=True);temporary=[]
    for other in mesh.materials:
        if other==material:continue
        n=other.node_tree.nodes.new('ShaderNodeTexImage');n.image=scratch;other.node_tree.nodes.active=n;temporary.append((other,n))
    mesh.uv_layers.active=mesh.uv_layers['SkinAtlas'];mesh.uv_layers['SkinAtlas'].active_render=True
    bpy.ops.object.select_all(action='DESELECT');skin.select_set(True);bpy.context.view_layer.objects.active=skin
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.cycles.device='CPU';scene.render.bake.margin=16;scene.render.bake.use_clear=True
    modifiers=[(m,m.show_render) for m in skin.modifiers];shapes=[(k,k.value) for k in mesh.shape_keys.key_blocks]
    for m,_ in modifiers:m.show_render=False
    for k,_ in shapes:k.value=0
    try:bpy.ops.object.bake(type='EMIT');image.save()
    finally:
        for m,enabled in modifiers:m.show_render=enabled
        for k,value in shapes:k.value=value
        for other,n in temporary:other.node_tree.nodes.remove(n)
    nodes.clear();uv=node('ShaderNodeUVMap');uv.uv_map='SkinAtlas';color=node('ShaderNodeTexImage');color.image=image;links.new(uv.outputs[0],color.inputs['Vector'])
    surface=node('ShaderNodeBsdfPrincipled');surface.inputs['Roughness'].default_value=.42;surface.inputs['Subsurface Weight'].default_value=.12;links.new(color.outputs['Color'],surface.inputs['Base Color']);output=node('ShaderNodeOutputMaterial');links.new(surface.outputs[0],output.inputs[0])
    print('LIP EDGE: generated scarred tissue feathered into a separate 4096px rest-space anatomy atlas; original epidermis atlas retained.',flush=True)

def apply(root,skin):
    folder=root/'Assets/_Game/Art/Textures';mesh=skin.data
    for name in ('CrawlerTeeth','CrawlerGums','CrawlerThroat'):
        image=bpy.data.images.load(str(folder/(name+'_BaseColor.png')),check_existing=False);image.colorspace_settings.name='Non-Color'
        width,height=image.size;rgb=np.asarray(image.pixels[:],dtype=np.float32).reshape(height,width,4)[...,:3]
        lum=rgb@np.array([.2126,.7152,.0722],dtype=np.float32);micro=lum-helpers['blur'](lum,5)
        relief=helpers['blur'](micro,.8);strength=1.7 if name=='CrawlerTeeth' else 2.3
        dx=(np.roll(relief,-1,1)-np.roll(relief,1,1))*strength;dy=(np.roll(relief,-1,0)-np.roll(relief,1,0))*strength
        length=np.sqrt(1+dx*dx+dy*dy);normal=np.ones((height,width,4),dtype=np.float32)
        normal[...,0]=.5-dx/length*.5;normal[...,1]=.5-dy/length*.5;normal[...,2]=.5+.5/length
        helpers['save_map'](folder,name+'_Normal',normal)
        mask=np.ones_like(normal);mask[...,0]=0;mask[...,1]=np.clip(.98+micro*.20,.90,1);mask[...,2]=.05
        mask[...,3]=np.clip((.26+lum*.24 if name=='CrawlerTeeth' else .42+lum*.24)+micro*.12,.22,.65)
        helpers['save_map'](folder,name+'_Mask',mask);bpy.data.images.remove(image)
        material=next(m for m in mesh.materials if m.name.split('.')[0]==name);material.use_nodes=True
        nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
        surface=nodes.new('ShaderNodeBsdfPrincipled');output=nodes.new('ShaderNodeOutputMaterial');links.new(surface.outputs[0],output.inputs['Surface'])
        surface.inputs['Roughness'].default_value=.68 if name=='CrawlerTeeth' else .48
        uv=nodes.new('ShaderNodeUVMap');uv.uv_map=mesh.uv_layers[0].name
        color=nodes.new('ShaderNodeTexImage');color.image=bpy.data.images.load(str(folder/(name+'_BaseColor.png')),check_existing=True);color.image.colorspace_settings.name='sRGB'
        links.new(uv.outputs['UV'],color.inputs['Vector']);links.new(color.outputs['Color'],surface.inputs['Base Color'])
        detail=nodes.new('ShaderNodeTexImage');detail.image=bpy.data.images.load(str(folder/(name+'_Normal.png')),check_existing=True);detail.image.colorspace_settings.name='Non-Color'
        tangent=nodes.new('ShaderNodeNormalMap');tangent.inputs['Strength'].default_value=.55;links.new(uv.outputs['UV'],detail.inputs['Vector']);links.new(detail.outputs['Color'],tangent.inputs['Color']);links.new(tangent.outputs['Normal'],surface.inputs['Normal'])
    slot=next(i for i,m in enumerate(mesh.materials) if m.name.split('.')[0]=='CrawlerTeeth')
    faces=[p for p in mesh.polygons if p.material_index==slot];adjacency={i:set() for p in faces for i in p.vertices}
    for face in faces:
        for a in face.vertices:adjacency[a].update(face.vertices)
    remaining=set(adjacency);components=[]
    while remaining:
        stack=[remaining.pop()];group=set(stack)
        while stack:
            for vertex in adjacency[stack.pop()]:
                if vertex in remaining:remaining.remove(vertex);group.add(vertex);stack.append(vertex)
        components.append(sorted(group))
    if len(components)!=26 or any(len(group)!=48 for group in components):raise ValueError('Expected 26 original three-ring teeth')
    if not skin.get('sharp_rotten_teeth_v1'):
        for group in components:
            for key in mesh.shape_keys.key_blocks:
                centers=[sum((key.data[i].co for i in group[ring*16:(ring+1)*16]),Vector())/16 for ring in range(3)]
                direction=(centers[2]-centers[0]).normalized()
                for ring,factor,extension in ((1,.62,.003),(2,.14,.007)):
                    for i in group[ring*16:(ring+1)*16]:key.data[i].co=centers[ring]+(key.data[i].co-centers[ring])*factor+direction*extension
        for vertex,basis in zip(mesh.vertices,mesh.shape_keys.key_blocks['Basis'].data):vertex.co=basis.co
        skin['sharp_rotten_teeth_v1']=True
    # Cylindrical tooth UVs sample varied areas of the enamel swatch; tissue keeps its existing primary UVs.
    uv=mesh.uv_layers[0];membership={v:(n,group.index(v)) for n,group in enumerate(components) for v in group}
    for face in faces:
        angles=[membership[mesh.loops[i].vertex_index][1]%16/16 for i in face.loop_indices];seam=max(angles)-min(angles)>.5
        for index,angle in zip(face.loop_indices,angles):
            tooth,vertex=membership[mesh.loops[index].vertex_index];u=angle+(1 if seam and angle<.5 else 0)
            uv.data[index].uv=((tooth%5)*.16+u*.28,.07+(1-vertex//16/2)*.86)
    runpy.run_path(str(Path(__file__).with_name('crawler_bite.py')))['add_bite_shape'](skin,components)
    mesh.update();bake_lip_edge(root,skin);print('MOUTH MATERIALS: three native albedos with normal/masks, 26 sharpened weighted teeth; all jaw morphs, bones and clips retained.',flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,required=True);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.project.resolve()
    path=root/'ArtSource/Blender/walker_01.blend';bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
    skin=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.shape_keys);apply(root,skin)
    bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
