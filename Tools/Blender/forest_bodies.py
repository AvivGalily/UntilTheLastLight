"""Original human forest casualties, authored in Blender in metres.

Four dressed static poses, fused facial anatomy and hands, cloth folds, boots,
satchels/documents and three distance LODs. No imported character library.
Blender --background --python this_file -- --project PROJECT
"""
import argparse
import math
from pathlib import Path
import random
import sys
import bpy
import numpy as np
from mathutils import Vector

ROOT = None
MATS = {}
PARTS = []


def C(p): return Vector((-p[0], -p[2], p[1]))


def material(name, color, roughness=.7, texture=None):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True; mat.node_tree.nodes.clear()
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bs=nodes.new('ShaderNodeBsdfPrincipled'); out=nodes.new('ShaderNodeOutputMaterial')
    links.new(bs.outputs[0],out.inputs[0]);bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=roughness
    mat.diffuse_color=(*color,1)
    if name=='BodySkin':
        bs.inputs['IOR'].default_value=1.4;bs.inputs['Subsurface Weight'].default_value=.08
        bs.inputs['Subsurface Scale'].default_value=.01;texture='CrawlerSkin'
    if texture:
        for suffix in ('BaseColor','Normal','Mask'):
            tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'Assets/_Game/Art/Textures'/(texture+'_'+suffix+'.png')),check_existing=True)
            tex.image.filepath='//../../Assets/_Game/Art/Textures/'+texture+'_'+suffix+'.png'
            if suffix=='BaseColor':
                if name=='BodySkin':
                    coord=nodes.new('ShaderNodeTexCoord');scale=nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs['Scale'].default_value=3.2
                    links.new(coord.outputs['Object'],scale.inputs[0]);links.new(scale.outputs['Vector'],tex.inputs['Vector']);tex.projection='BOX';tex.projection_blend=.35
                multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1;multiply.inputs[2].default_value=(*color,1)
                links.new(tex.outputs['Color'],multiply.inputs[1]);links.new(multiply.outputs[0],bs.inputs['Base Color'])
            elif suffix=='Normal':
                tex.image.colorspace_settings.name='Non-Color';normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.15 if name=='BodySkin' else .5
                links.new(tex.outputs['Color'],normal.inputs[1]);links.new(normal.outputs[0],bs.inputs['Normal'])
            else:
                tex.image.colorspace_settings.name='Non-Color';inverse=nodes.new('ShaderNodeMath');inverse.operation='SUBTRACT';inverse.inputs[0].default_value=1
                links.new(tex.outputs['Alpha'],inverse.inputs[1]);links.new(inverse.outputs[0],bs.inputs['Roughness'])
    MATS[name]=mat;return mat


def noise(n, grid, rng):
    a=rng.random((grid,grid)).astype(np.float32);p=np.arange(n)*grid/n;i=p.astype(int);t=p-i;t=t*t*(3-2*t)
    return ((a[i[:,None]%grid,i[None,:]%grid]*(1-t[:,None])+a[(i[:,None]+1)%grid,i[None,:]%grid]*t[:,None])*(1-t[None,:])
            +(a[i[:,None]%grid,(i[None,:]+1)%grid]*(1-t[:,None])+a[(i[:,None]+1)%grid,(i[None,:]+1)%grid]*t[:,None])*t[None,:])


def save_map(name, data):
    im=bpy.data.images.new(name,data.shape[1],data.shape[0],alpha=True);im.colorspace_settings.name='Non-Color'
    im.pixels.foreach_set(np.clip(data,0,1).astype(np.float32).ravel());im.filepath_raw=str(ROOT/'Assets/_Game/Art/Textures'/(name+'.png'));im.file_format='PNG';im.save();bpy.data.images.remove(im)


def textures():
    n=2048;rng=np.random.default_rng(82147);u,v=np.meshgrid(np.arange(n)/n,np.arange(n)/n)
    broad=noise(n,11,rng);fine=noise(n,241,rng)
    for key in ('BodyFabric','BodyLeather'):
        if key=='BodyFabric':
            weave=(np.sin(u*512*math.tau)*np.sin(v*512*math.tau))*.5+.5
            height=weave*.008+fine*.006;shade=.86+broad*.15+weave*.025
            base=(.66,.65,.61);smoothness=.17+fine*.06
        else:
            grain=noise(n,431,rng);height=grain*.045+fine*.014;shade=.72+broad*.22+grain*.07
            base=(.29,.21,.135);smoothness=.30+fine*.12
        rgba=np.ones((n,n,4),dtype=np.float32)
        for c in range(3):rgba[...,c]=base[c]*shade
        save_map(key+'_BaseColor',rgba)
        dx=(np.roll(height,-1,1)-np.roll(height,1,1))*2;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*2
        length=np.sqrt(1+dx*dx+dy*dy);normal=np.ones_like(rgba)
        normal[...,0]=.5-dx/length*.5;normal[...,1]=.5-dy/length*.5;normal[...,2]=.5+.5/length
        save_map(key+'_Normal',normal)
        mask=np.ones_like(rgba);mask[...,0]=0;mask[...,1]=.98;mask[...,2]=0;mask[...,3]=smoothness
        save_map(key+'_Mask',mask)


def track(obj, name, mat):
    obj.name=name;obj.data.materials.append(MATS[mat]);PARTS.append(obj)
    for face in obj.data.polygons:face.use_smooth=True
    return obj


def sphere(name, p, scale, mat, segments=32, rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=C(p))
    obj=bpy.context.object;obj.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return track(obj,name,mat)


def tube(name, points, radii, mat, sides=16, folds=0):
    points=[Vector(p) for p in points];verts=[];faces=[]
    for i,p in enumerate(points):
        axis=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        ref=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0));a=axis.cross(ref).normalized();b=axis.cross(a).normalized()
        for j in range(sides):
            angle=j*math.tau/sides;r=radii[i]+folds*math.sin(i*1.8+j*.7)*math.sin(i/max(1,len(points)-1)*math.pi)
            verts.append(C(p+r*(a*math.cos(angle)+b*math.sin(angle))))
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    return track(obj,name,mat)


def box(name,p,size,mat,bevel=.01):
    bpy.ops.mesh.primitive_cube_add(size=1,location=C(p));obj=bpy.context.object;obj.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    track(obj,name,mat)
    if bevel:
        mod=obj.modifiers.new('Soft worn edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def join(objects,name):
    removed={obj.as_pointer() for obj in objects}
    keep=[obj for obj in PARTS if obj.as_pointer() not in removed]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    PARTS[:]=keep+[obj]
    return obj


def sculpt(objects,name,voxel,ratio,relax=3):
    obj=join(objects,name);bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Continuous sculpt','REMESH');mod.mode='VOXEL';mod.voxel_size=voxel;mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=obj.modifiers.new('Sculpt relaxation','SMOOTH');mod.factor=.65;mod.iterations=relax;bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=obj.modifiers.new('Surface budget','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
    for f in obj.data.polygons:f.use_smooth=True
    return obj


def head(variant):
    center=Vector((0,.172,.742));angle=math.radians((-7,11,-13,6)[variant]);ca=math.cos(angle);sa=math.sin(angle)
    def H(x,h,d):return center+Vector((ca*x-sa*d,sa*x+ca*d,h))
    start=len(PARTS)
    def skin(name,x,h,d,rx,rh,rd):return sphere(name,H(x,h,d),(rx,rd,rh),'BodySkin',40,24)
    skin('Human cranium',0,.012,-.014,.092,.125,.085)
    skin('Continuous facial plane',0,-.016,.057,.078,.088,.040)
    skin('Mandible',0,-.068,.013,.074,.057,.064);skin('Chin',0,-.104,.057,.036,.021,.028)
    skin('Oral facial plane',0,-.065,.073,.035,.024,.018)
    skin('Continuous neck',0,-.174,-.002,.049,.083,.045)
    skin('Nose bridge',0,.012,.082,.013,.040,.020);skin('Nose tip',0,-.022,.107,.018,.016,.024)
    for side in (-1,1):
        skin('Cheekbone',side*.045,-.020,.061,.035,.038,.026)
        skin('Brow ridge',side*.039,.045,.065,.033,.009,.019)
        skin('Closed orbital plane',side*.039,.027,.062,.033,.018,.021)
        skin('Nostril wing',side*.016,-.030,.098,.011,.009,.012)
        skin('Human ear',side*.093,-.003,-.017,.012,.030,.017)
    sculpt(PARTS[start:],'Sculpted human face',.0022,.25,25)
    for side in (-1,1):
        eye=[H(side*.041+x,.025+.003*math.cos(x/.024*math.pi/2),.076+.012*math.cos(x/.024*math.pi/2)) for x in (-.023,-.016,-.008,0,.008,.016,.023)]
        tube('Closed eyelid',eye,[.0022]*len(eye),'BodySkin',10)
        tube('Closed eye crease',[p+Vector((0,.0007,-.0016)) for p in eye],[.0009]*len(eye),'BodyCrease',8)
        brow=[H(side*.041+x,.048+.004*math.cos(x/.024*math.pi/2),.077) for x in (-.024,-.012,0,.012,.024)]
        tube('Eyebrow',brow,[.001,.002,.0022,.0018,.0006],'BodyHair',8)
        sphere('Nostril shadow',H(side*.014,-.033,.115),(.003,.0017,.003),'BodyCrease',16,10)
        ear=[H(side*.103,.025*math.cos(t),-.014+.013*math.sin(t)) for t in np.linspace(0,math.tau,18)]
        tube('Ear helix',ear,[.0025]*len(ear),'BodySkin',8)
    for upper in (True,False):
        lip=[]
        for x in np.linspace(-.030,.030,13):
            h=-.068+( .0023*math.cos(x/.030*math.pi) if upper else -.0035*math.cos(x/.030*math.pi/2))
            lip.append(H(x,h,.091+.004*math.cos(x/.030*math.pi/2)))
        tube('Natural closed lip',lip,[.0012+.0012*math.sin(i/12*math.pi) for i in range(13)],'BodyLips',10)
    tube('Mouth crease',[H(x,-.069,.096) for x in np.linspace(-.028,.028,12)],[.0008]*12,'BodyCrease',6)
    # A cap with an irregular hairline, not an ellipsoid helmet. Short strands
    # break the contour and follow the head's scalp rather than face normals.
    verts=[];faces=[];rings=12;sides=56
    for i in range(rings+1):
        for j in range(sides):
            theta=math.tau*j/sides;front=math.sin(theta)>0
            end=1.02 if front else 2.02;phi=(i/rings)*end+.016*math.sin(j*2.1)*i/rings
            verts.append(C(H(.094*math.sin(phi)*math.cos(theta),.013+.127*math.cos(phi),-.014+.087*math.sin(phi)*math.sin(theta))))
    for i in range(rings):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    data=bpy.data.meshes.new('Human scalp hair');data.from_pydata(verts,[],faces);data.update();obj=bpy.data.objects.new('Hairline',data);bpy.context.collection.objects.link(obj);track(obj,'Hairline','BodyHair')
    rng=random.Random(variant+781)
    for i in range(45):
        theta=rng.random()*math.tau;phi=rng.uniform(.15,1.0 if math.sin(theta)>0 else 1.75)
        strand=[]
        for q in np.linspace(0,1,5):
            p=phi+q*.19
            strand.append(H(.095*math.sin(p)*math.cos(theta),.013+.129*math.cos(p),-.014+.090*math.sin(p)*math.sin(theta)))
        tube('Short scalp strand',strand,[.0012,.0011,.001,.0008,.0002],'BodyHair',6)


def hand(wrist, elbow, side):
    wrist=Vector(wrist);direction=Vector(wrist)-Vector(elbow);direction.y=0;direction.normalize()
    across=Vector((direction.z,0,-direction.x));start=len(PARTS)
    palm=wrist+direction*.033
    sphere('Human palm',palm,(.032,.019,.043),'BodySkin',24,16)
    for finger in range(4):
        offset=across*(finger-1.5)*.017;length=(.061,.079,.074,.056)[finger]
        a=palm+direction*.030+offset;b=a+direction*length*.48+Vector((0,.007,0));c=a+direction*length+Vector((0,-.007,0))
        tube('Articulated finger',[a,a.lerp(b,.5),b,b.lerp(c,.5),c],[.010,.009,.008,.0065,.005],'BodySkin',12)
        sphere('Fingernail',c-direction*.008+Vector((0,.005,0)),(.006,.0013,.008),'BodyNails',16,10)
    a=palm+across*side*.027;b=a+across*side*.023+direction*.016;c=b+direction*.028
    tube('Opposed thumb',[a,b,c],[.011,.010,.006],'BodySkin',12)
    # Palm and digits meet smoothly; nails keep their independent surface.
    skin=[obj for obj in PARTS[start:] if obj.data.materials[0].name=='BodySkin']
    sculpt(skin,'Continuous hand',.0028,.28)


def coat(variant):
    key=('BodyCoatOlive','BodyCoatBrown','BodyCoatBlue','BodyCoatGrey')[variant];parts=[]
    # Anatomical shoulder/chest/waist/hip profiles instead of a single oval.
    profiles=[(.48,.105,.060),(.40,.215,.080),(.31,.205,.107),(.16,.175,.104),(.03,.155,.090),(-.13,.178,.090),(-.23,.185,.072)]
    verts=[];faces=[];sides=56
    for i,(z,width,depth) in enumerate(profiles):
        for j in range(sides):
            a=j*math.tau/sides;x=width*math.cos(a);y=.165+depth*math.sin(a)
            y+=.0035*math.sin(j*.9+i*2.1)*max(0,math.sin(a))
            verts.append(C((x,y,z)))
    for i in range(len(profiles)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(profiles)-1)*sides+j for j in range(sides))])
    data=bpy.data.meshes.new('Tailored jacket');data.from_pydata(verts,[],faces);data.update();obj=bpy.data.objects.new('Jacket',data);bpy.context.collection.objects.link(obj);parts.append(track(obj,'Human jacket',key))
    arm_poses=[[(.37,.095,.13),(.43,.056,-.24)],[(.37,.08,.11),(.15,.235,.05)],[(.40,.064,.10),(.47,.042,-.24)],[(.34,.12,.12),(.26,.225,-.02)]]
    for side in (-1,1):
        shoulder=Vector((side*.19,.174,.345));pose=arm_poses[variant if side>0 else (variant+2)%4]
        elbow=Vector((side*pose[0][0],pose[0][1],pose[0][2]));wrist=Vector((side*pose[1][0],pose[1][1],pose[1][2]))
        points=[shoulder,shoulder.lerp(elbow,.27),shoulder.lerp(elbow,.60),elbow,elbow.lerp(wrist,.22),elbow.lerp(wrist,.60),wrist]
        parts.append(tube('Folded coat sleeve',points,[.075,.078,.065,.057,.056,.049,.037],key,24,.005))
        hand(wrist,elbow,side)
        tube('Sleeve cuff',[wrist-direction*.013 for direction in [(wrist-elbow).normalized(),Vector((0,0,0))]],[.041,.039],key,24)
    sculpt(parts,'Continuous tailored coat',.0055,.42)
    # Lapels, stitched pocket rims and a metal zipper provide readable details.
    for side in (-1,1):
        tube('Coat lapel',[(side*.08,.238,.48),(side*.073,.271,.36),(side*.043,.28,.27)],[.011,.013,.006],key,12)
        box('Chest pocket',(side*.115,.277,.25),(.10,.012,.113),key,.012)
        tube('Pocket stitched edge',[(side*.065,.285,.30),(side*.115,.291,.303),(side*.165,.28,.30)],[.0014]*3,'BodyStitch',8)
        for z in (-.10,-.02,.06):
            tube('Waist cloth fold',[(side*.018,.267,z),(side*.09,.266,z-.018),(side*.16,.229,z-.03)],[.004,.005,.0018],key,10)
    tube('Zipper seam',[(0,.245,.44),(0,.277,.31),(0,.274,.13),(0,.257,-.10),(0,.242,-.21)],[.003]*5,'BodyStitch',10)
    for n in range(22):box('Zipper tooth',(0,.279 if n<12 else .26,.32-n*.020),(.004,.003,.009),'BodyMetal',.001)


def legs(variant):
    for side in (-1,1):
        hip=Vector((side*.107,.145,-.175));knee=Vector((side*(.13+.016*variant),.102,-.585));ankle=Vector((side*(.11+.025*variant),.087,-.970))
        points=[hip,hip.lerp(knee,.23),hip.lerp(knee,.55),knee,knee.lerp(ankle,.22),knee.lerp(ankle,.65),ankle]
        tube('Tailored trouser leg',points,[.088,.089,.075,.067,.063,.052,.038],'BodyPants',28,.004)
        for t in (.70,.79):
            p=hip.lerp(knee,t);tube('Knee cloth fold',[p+Vector((-.048,.056,0)),p+Vector((0,.068,-.008)),p+Vector((.048,.053,-.016))],[.002,.004,.002],'BodyPants',10)
        boot=ankle+Vector((0,.01,-.060));sphere('Leather boot',boot,(.061,.061,.112),'BodyLeather',32,18)
        tube('Boot ankle',[(ankle.x,.071,ankle.z+.03),(ankle.x,.095,ankle.z-.028)],[.058,.060],'BodyLeather',24)
        box('Boot sole',boot+Vector((0,-.042,-.006)),(.125,.025,.218),'BodySole',.02)
        for n in range(5):tube('Boot lace',[(ankle.x-.027,.144,-.977-n*.019),(ankle.x+.027,.145,-.989-n*.019)],[.0018,.0018],'BodyStitch',8)
    box('Belt',(0,.239,-.157),(.33,.015,.028),'BodyLeather',.006)
    box('Belt buckle',(0,.252,-.156),(.044,.008,.034),'BodyMetal',.003)


def props():
    box('Worn document satchel',(.29,.205,-.02),(.25,.20,.36),'BodyLeather',.036)
    box('Satchel flap',(.29,.303,-.01),(.248,.018,.28),'BodyLeather',.014)
    tube('Satchel strap',[(.28,.303,-.13),(.14,.31,.01),(-.09,.282,.21),(-.19,.238,.34)],[.011]*4,'BodyLeather',12)
    box('Satchel buckle',(.29,.318,.098),(.032,.008,.038),'BodyMetal',.004)
    for n in range(12):
        box('Satchel stitch',(.184+n*.019,.318,-.145),(.007,.002,.002),'BodyStitch',.0005)
    box('Recovered folded document',(.28,.326,-.014),(.195,.003,.25),'BodyPaper',.001)
    for n in range(8):
        box('Document handwriting',(.262,.329,-.105+n*.023),(.09+(n%3)*.018,.001,.0018),'BodyInk',.0003)
    box('Paper fold',(.28,.332,-.013),(.002,.001,.24),'BodyStitch',.0003)


def finish(variant):
    asset='forest_body_0'+str(variant+1)
    obj=join([o for o in bpy.context.scene.objects if o.type=='MESH'],asset+'_LOD0')
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.12,island_margin=.004);bpy.ops.object.mode_set(mode='OBJECT')
    for loop in obj.data.uv_layers.active.data:loop.uv*=3
    obj['art_note']='Original human face, closed eyes, ears, lips, hair, opposed thumbs and fingers; tailored clothing, boots, bag and paper.'
    obj.data.calc_loop_triangles()
    if len(obj.data.loop_triangles)>65000:
        mod=obj.modifiers.new('Human body game budget','DECIMATE');mod.ratio=65000/len(obj.data.loop_triangles);bpy.ops.object.modifier_apply(modifier=mod.name)
    for level,ratio in ((1,.38),(2,.13)):
        copy=obj.copy();copy.data=obj.data.copy();copy.name=asset+'_LOD'+str(level);bpy.context.collection.objects.link(copy);bpy.context.view_layer.objects.active=copy
        mod=copy.modifiers.new('Distance topology','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name);copy.hide_render=True
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Blender'/(asset+'.blend')))
    for mesh in bpy.context.scene.objects:
        if mesh.type=='MESH':mesh.hide_render=False
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_Game/Art/Meshes'/(asset+'.fbx')),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,bake_anim=False,use_mesh_modifiers=False)
    obj.data.calc_loop_triangles();print('HUMAN BODY',asset,len(obj.data.loop_triangles),'LOD0 triangles,',obj.dimensions[:],flush=True)


def generate(root):
    global ROOT, PARTS
    ROOT=Path(root);bpy.context.preferences.filepaths.save_version=0;textures()
    for key,color in [('BodyCoatOlive',(.23,.30,.20)),('BodyCoatBrown',(.32,.23,.16)),('BodyCoatBlue',(.17,.24,.30)),('BodyCoatGrey',(.27,.29,.28)),('BodyPants',(.18,.19,.16))]:material(key,color,texture='BodyFabric')
    material('BodySkin',(.78,.82,.86));material('BodyLeather',(1,1,1),texture='BodyLeather')
    for key,color,rough in [('BodyHair',(.055,.041,.031),.9),('BodyCrease',(.08,.052,.044),.9),('BodyLips',(.25,.13,.13),.58),('BodyNails',(.42,.39,.36),.48),('BodySole',(.035,.038,.035),.85),('BodyMetal',(.23,.25,.24),.4),('BodyStitch',(.28,.27,.21),.8),('BodyPaper',(.70,.66,.54),.95),('BodyInk',(.055,.051,.047),1)]:material(key,color,rough)
    for variant in range(4):
        bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);PARTS=[]
        head(variant);coat(variant);legs(variant);props();finish(variant)


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);generate(args.project.resolve())
