"""CC0 NewDLC wolf adaptation and original field axe. Reproducible Blender sources."""
import bpy,math,sys
import shutil
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from zombie_anatomy import add_half_skull
from mathutils import Matrix,Vector,Quaternion
ROOT=Path(__file__).resolve().parents[2]
TEX=ROOT/'Assets/_Game/Art/Textures';OUT=ROOT/'ArtSource/Blender';MESH=ROOT/'Assets/_Game/Art/Meshes'

def image_pixels(path):
    im=bpy.data.images.load(str(path),check_existing=False);p=np.empty(len(im.pixels),dtype=np.float32);im.pixels.foreach_get(p)
    return im,p.reshape((im.size[1],im.size[0],4))

def save_image(name,pixels):
    im=bpy.data.images.new(name,width=pixels.shape[1],height=pixels.shape[0],alpha=True)
    im.pixels.foreach_set(pixels.astype(np.float32).ravel());im.filepath_raw=str(TEX/(name+'.png'));im.file_format='PNG';im.save();return im

def material(key,color=(.25,.22,.18,1),rough=.6):
    m=bpy.data.materials.new(key);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=rough
    return m

def export(name,animated=False):
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')),compress=True)
    bpy.ops.export_scene.fbx(filepath=str(MESH/(name+'.fbx')),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,use_armature_deform_only=True,use_mesh_modifiers=False,bake_anim=animated,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)

def wolf():
    source=ROOT/'ArtSource/Vendor/newdlc-wolf-cc0'
    bpy.ops.wm.open_mainfile(filepath=str(source/'dog2.blend'),load_ui=False,use_scripts=False)
    for ob in list(bpy.context.scene.objects):
        if ob.type not in {'MESH','ARMATURE'}:bpy.data.objects.remove(ob,do_unlink=True)
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    for ob in [mesh,rig]:
        world=ob.matrix_world.copy();ob.parent=None;ob.matrix_world=Matrix.Scale(.38,4)@world
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.context.view_layer.objects.active=mesh
    smooth=mesh.modifiers.new('Continuous wolf silhouette','SUBSURF');smooth.levels=1;smooth.uv_smooth='PRESERVE_BOUNDARIES';bpy.ops.object.modifier_apply(modifier=smooth.name)
    for p in mesh.data.polygons:p.use_smooth=True
    # Preserve the artist's fur UV and normal map; weather the color into an ashen,
    # diseased coat. Wounds are independently placed in Unity object space.
    original,pixels=image_pixels(source/'dog2Color.png');rgb=pixels[:,:,:3];luma=rgb@np.array([.2126,.7152,.0722]);rgb[:]=rgb*.47+luma[:,:,None]*np.array([.47,.49,.46]);rgb[:]*=.83
    realistic=ROOT/'ArtSource/Textures/WolfSkin_Realistic.png'
    if realistic.exists():
        shutil.copyfile(realistic,TEX/'WolfSkin_BaseColor.png');albedo=bpy.data.images.load(str(TEX/'WolfSkin_BaseColor.png'),check_existing=False)
    else:albedo=save_image('WolfSkin_BaseColor',pixels)
    _,normal=image_pixels(source/'dog2Normal.png');normal_im=save_image('WolfSkin_Normal',normal)
    _,rough=image_pixels(source/'dog2Roughness.png');mask=np.ones_like(rough);mask[:,:,0]=0;mask[:,:,2]=.5;mask[:,:,3]=np.clip(1-rough[:,:,0],.08,.4)
    save_image('WolfSkin_Mask',mask)
    mat=material('WolfSkin');nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
    t=nodes.new('ShaderNodeTexImage');t.image=albedo;links.new(t.outputs['Color'],bs.inputs['Base Color'])
    t=nodes.new('ShaderNodeTexImage');t.image=normal_im;t.image.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.5;links.new(t.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
    if realistic.exists():
        fur=nodes.new('ShaderNodeTexImage');fur.image=albedo;bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.30;bump.inputs['Distance'].default_value=.003
        links.new(fur.outputs['Color'],bump.inputs['Height']);links.new(n.outputs['Normal'],bump.inputs['Normal']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    mesh.data.materials.clear();mesh.data.materials.append(mat);mesh.name='Undead wolf';rig.name='WolfRig'
    eyes=mat.copy();eyes.name='WolfEyes';eyes.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.32;mesh.data.materials.append(eyes)
    eye_groups={g.index for g in mesh.vertex_groups if 'Eye' in g.name}
    eye_vertices={v.index for v in mesh.data.vertices if any(g.group in eye_groups and g.weight>.4 for g in v.groups)}
    for p in mesh.data.polygons:
        if sum(v in eye_vertices for v in p.vertices)>len(p.vertices)/2:p.material_index=1
    if realistic.exists():
        # Native Blender tangent-normal baking adds new strand relief to the original anatomy normal.
        baked=bpy.data.images.new('Wolf strand and anatomy normal',2048,2048,alpha=False);baked.colorspace_settings.name='Non-Color'
        bake_nodes=[]
        for surface in mesh.data.materials:
            node=surface.node_tree.nodes.new('ShaderNodeTexImage');node.image=baked;surface.node_tree.nodes.active=node;bake_nodes.append((surface,node))
        bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
        bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=1;bpy.context.scene.render.bake.margin=12
        bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT',use_selected_to_active=False)
        baked.filepath_raw=str(TEX/'WolfSkin_Normal.png');baked.file_format='PNG';baked.save()
        for surface,node in bake_nodes:
            surface.node_tree.nodes.remove(node)
            for texture in surface.node_tree.nodes:
                if texture.type=='TEX_IMAGE' and texture.image==normal_im:texture.image=baked
    mesh=add_half_skull(mesh,rig,material,save_image)
    for a in list(bpy.data.actions):bpy.data.actions.remove(a)
    rig.animation_data_create()
    def rotate(name,angle,axis=(1,0,0)):
        b=rig.pose.bones[name];rest=b.bone.matrix_local.to_quaternion();b.rotation_quaternion=rest.inverted()@Quaternion(Vector(axis),math.radians(angle))@rest
    def translate(name,v):
        b=rig.pose.bones[name];b.location=b.bone.matrix_local.to_3x3().inverted()@Vector(v)
    def reset():
        for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)
    for name,end in [('Wolf_Idle',90),('Wolf_Run',18),('Wolf_Bite',30),('Wolf_Death',42)]:
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame in range(1,end+2):
            reset();t=(frame-1)/end;phase=t*2*math.pi
            rotate('b_Neck',-9);rotate('b_Jaw',9)
            if name=='Wolf_Idle':
                translate('b_Hip',(0,0,.008*math.sin(phase)));rotate('b_Head',3*math.sin(phase),(0,0,1));rotate('b_Jaw',10+3*math.sin(phase))
            if name=='Wolf_Run':
                translate('b_Hip',(0,0,.018*math.sin(2*phase)));rotate('b_Neck',-13+3*math.sin(phase));rotate('b_Spine02',2*math.sin(phase))
                for side,offset in [('Left',0),('Right',math.pi)]:
                    s=math.sin(phase+offset);lift=max(0,math.cos(phase+offset))
                    rotate('b_'+side+'UpperArm',40*s);rotate('b_'+side+'ForeArm',-32*lift);rotate('b_'+side+'Hand01',-24*s+16*lift)
                    rotate('b_'+side+'Leg01',-36*s);rotate('b_'+side+'Leg02',26*max(0,-math.cos(phase+offset)));rotate('b_'+side+'Foot01',25*s)
                rotate('b_Tail01',5*math.sin(phase),(0,0,1))
            if name=='Wolf_Bite':
                # Open while drawing back; close at .60 s, then finish recovery.
                jaw=10+34*min(1,t/.34) if t<.34 else 44-41*min(1,(t-.34)/.26) if t<.60 else 3+7*min(1,(t-.72)/.28)
                draw=math.sin(min(t/.60,1)*math.pi/2)
                recoil=max(0,1-(t-.6)/.4) if t>=.6 else draw
                rotate('b_Neck',-12-20*recoil);rotate('b_Head',-10*recoil);rotate('b_Jaw',jaw);translate('b_Hip',(0,-.08*recoil,0))
            if name=='Wolf_Death':
                fall=min(t/.72,1);ease=fall*fall*(3-2*fall)
                rotate('b_Root',82*ease,(0,1,0));translate('b_Root',(0,0,.18*ease))
                rotate('b_Head',-18*ease);rotate('b_Jaw',18);rotate('b_Tail01',-20*ease)
                for side in ['Left','Right']:
                    rotate('b_'+side+'UpperArm',-28*ease);rotate('b_'+side+'ForeArm',-22*ease);rotate('b_'+side+'Leg01',20*ease)
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_quaternion',frame=frame);b.keyframe_insert('location',frame=frame)
        action.use_fake_user=True
    from wolf_motion import apply as improve_motion
    improve_motion(rig)
    rig.animation_data.action=bpy.data.actions['Wolf_Idle'];bpy.context.scene.render.fps=30
    bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=91;export('zombie_wolf_01',True)
    print('WOLF EXPORTED',len(mesh.data.vertices),len(rig.data.bones),flush=True)

def axe():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    wood=material('Timber',(.2,.11,.055,1));steel=material('Steel',(.23,.26,.26,1),.34);wrap=material('BodyLeather',(.08,.045,.027,1))
    objects=[]
    # Tapered, curved hickory handle with an oval grip, rather than a straight cylinder.
    vertices=[];faces=[];rings=21;sides=16
    for j in range(rings):
        t=j/(rings-1);z=t*.77;x=.035*math.sin(t*math.pi)-.022*t
        radius=.023+.010*(1-t)+.007*math.exp(-((t-.12)/.09)**2)
        for i in range(sides):
            a=i*2*math.pi/sides;vertices.append((x+math.cos(a)*radius,math.sin(a)*radius*.7,z))
        if j:
            for i in range(sides):a=(j-1)*sides+i;b=(j-1)*sides+(i+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((rings-1)*sides+i for i in range(sides))])
    data=bpy.data.meshes.new('Shaped hickory');data.from_pydata(vertices,[],faces);ob=bpy.data.objects.new('Hickory handle',data);bpy.context.collection.objects.link(ob);ob.data.materials.append(wood);objects.append(ob)
    # A forged wedge and swept cutting edge, with thickness tapering to the blade.
    profile=[(-.09,.655),(-.09,.76),(.035,.78),(.235,.85),(.27,.65),(.14,.655)]
    verts=[]
    for side in [-1,1]:
        for x,z in profile:verts.append((x,side*(.007 if x>.2 else .027),z))
    n=len(profile);faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    data=bpy.data.meshes.new('Forged head');data.from_pydata(verts,[],faces);ob=bpy.data.objects.new('Steel axe head',data);bpy.context.collection.objects.link(ob);ob.data.materials.append(steel);objects.append(ob)
    for obj in objects:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bevel=obj.modifiers.new('Worn edges','BEVEL');bevel.width=.003;bevel.segments=3;bpy.ops.object.modifier_apply(modifier=bevel.name)
        for p in obj.data.polygons:p.use_smooth=True
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name='Field axe';export('field_axe_01')
    print('AXE EXPORTED',flush=True)

if __name__=='__main__':wolf();axe()
