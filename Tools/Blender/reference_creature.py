"""Original reference-guided fleshy crawler: sculpt, PBR, weighted rig and crawl.
Blender 5.2 --background --python this_file -- --project PROJECT
Coordinates below are Unity metres (X right, Y up, Z forward); export is explicit FBX.
The user's creature reference guides anatomy; skin uses a separate generated albedo.
"""
import argparse, math, random, sys
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector, Matrix
sys.path.insert(0,str(Path(__file__).parent))
from crawler_bite import add_bite_shape

parser=argparse.ArgumentParser()
parser.add_argument('--project', type=Path, required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=args.project.resolve(); source=root/'ArtSource/Blender'; textures=root/'Assets/_Game/Art/Textures'
source.mkdir(parents=True,exist_ok=True);textures.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.preferences.filepaths.save_version=0
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
random.seed(7219)
def C(p):return Vector((-p[0],-p[2],p[1]))
def U(p):return Vector((-p.x,p.z,-p.y))

def mat(name,color,rough=.4):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=rough
    if name=='CrawlerSkin':
        bs.inputs['Subsurface Weight'].default_value=.12
        for suffix,socket in [('BaseColor','Base Color'),('Normal','Normal')]:
            tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(textures/(name+'_'+suffix+'.png')))
            if suffix=='Normal':
                tex.image.colorspace_settings.name='Non-Color';normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.7
                m.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs[0],bs.inputs[socket])
            else:m.node_tree.links.new(tex.outputs['Color'],bs.inputs[socket])
    return m
mats={name:mat(name,col,rough) for name,col,rough in [('CrawlerSkin',(.65,.43,.38),.4),('CrawlerGums',(.25,.045,.055),.22),('CrawlerTeeth',(.70,.61,.39),.28),('CrawlerThroat',(.027,.004,.009),.25),('CrawlerSaliva',(.29,.19,.17),.08)]}

def sphere(name,p,scale,material='CrawlerSkin',segments=24,rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=C(p))
    o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mats[material]);return o
def tube(name,points,radii,material='CrawlerSkin',sides=12):
    verts=[];faces=[]
    for i,p in enumerate(points):
        p=C(p);direction=C(points[min(i+1,len(points)-1)])-C(points[max(i-1,0)])
        direction.normalize();ref=Vector((0,0,1)) if abs(direction.z)<.9 else Vector((1,0,0))
        x=direction.cross(ref).normalized();y=direction.cross(x).normalized()
        for j in range(sides):verts.append(p+radii[i]*(math.cos(j*math.tau/sides)*x+math.sin(j*math.tau/sides)*y))
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(mats[material]);return o
def join(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');return o
def voxel(objects,name,voxel_size=.013):
    o=join(objects,name);mod=o.modifiers.new('Organic fused anatomy','REMESH');mod.mode='VOXEL';mod.voxel_size=voxel_size;mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    smooth=o.modifiers.new('Sculpt relaxation','SMOOTH');smooth.factor=.8;smooth.iterations=4;bpy.ops.object.modifier_apply(modifier=smooth.name)
    return o
def finish_mesh(o,ratio=1):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    if ratio<1:
        mod=o.modifiers.new('Game topology','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
        if o.name=='CrawlerAnatomy':
            sub=o.modifiers.new('Sculpt surface continuity','SUBSURF');sub.levels=1;sub.subdivision_type='CATMULL_CLARK';bpy.ops.object.modifier_apply(modifier=sub.name)
    for f in o.data.polygons:f.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT')
    # Repeating micro-skin rather than stretching one image over isolated tiny islands.
    if o.name=='CrawlerAnatomy':
        for loop in o.data.uv_layers.active.data:loop.uv*=3

parts=[]
def flesh(name,p,s):parts.append(sphere(name,p,s));return parts[-1]
flesh('Horizontal ribcage',(0,1.05,.24),(.31,.29,.58))
flesh('Hollow waist',(0,.90,-.30),(.205,.22,.35));flesh('Fleshy pelvis',(0,.98,-.64),(.32,.27,.33))
flesh('Shoulder girdle',(0,1.16,.49),(.44,.22,.25))
flesh('Crouched neck',(0,1.31,.60),(.16,.25,.18))
flesh('Eyeless skull',(0,1.62,.76),(.238,.265,.238))
flesh('Chin',(0,1.20,.84),(.17,.095,.12))
for side in (-1,1):
    flesh('Cheek', (side*.178,1.43,.79),(.068,.23,.12))
    flesh('Sealed eye fold',(side*.096,1.692,.929),(.086,.023,.024))
    flesh('Folded cheek beside mouth',(side*.179,1.46,.902),(.047,.191,.060))
    flesh('Trapezius',(side*.20,1.26,.45),(.23,.13,.28))
    flesh('Scapula',(side*.24,1.15,.08),(.10,.19,.36))
    # Actual ribs blend into the emaciated ribcage, with a deep abdominal underside.
    for j in range(7):
        z=.48-j*.112
        parts.append(tube('Rib fold',[(side*.09,1.12,z),(side*.29,1.06,z-.02),(side*.23,.88,z-.04)],[.023,.028,.008],sides=10))
for j in range(10):flesh('Spine vertebra',(0,1.265-j*.011,.43-j*.11),(.037,.032,.05))

chains=[]
for i in range(8):
    side=-1 if i<4 else 1;n=i%4
    if n==0:
        a=Vector((side*.34,1.12,.48));b=Vector((side*.67,.67,.88));c=Vector((side*.61,.085,1.43));end=c+Vector((0,-.035,.20));radius=.116
    else:
        a=Vector((side*(.28 if n<3 else .26),1.04,-.08-(n-1)*.30))
        b=Vector((side*(.90 if n==1 else .93 if n==2 else .72),1.46 if n==1 else 1.32, .52 if n==1 else -.33 if n==2 else -.98))
        c=Vector((side*(1.20 if n==1 else 1.25 if n==2 else 1.02),.08, .84 if n==1 else -.20 if n==2 else -1.24))
        end=c+Vector((side*.14,-.045,.13 if n<3 else -.21));radius=.091
    chains.append((a,b,c,end))
    mid=a.lerp(b,.39);mid2=b.lerp(c,.36)
    parts.append(tube('Long fleshy limb', [a,a.lerp(b,.14),mid,a.lerp(b,.77),b,b.lerp(c,.12),mid2,b.lerp(c,.76),c], [radius,radius*1.1,radius*.88,radius*.54,radius*.66,radius*.79,radius*.78,radius*.42,.036]))
    flesh('Bony elbow',b,(.069,.089,.070))
    flesh('Tendon on forearm',mid2,(.059,.17,.06))
    if n==0:
        palm=c+Vector((0,-.018,.11));flesh('Human hand',palm,(.10,.049,.145))
        for digit in range(5):
            if digit==0:
                finger=[palm+Vector((-side*.06,0,-.025)),palm+Vector((-side*.16,-.018,.06)),palm+Vector((-side*.19,-.032,.135))]
            else:
                x=(digit-2.5)*.052;length=[.19,.235,.22,.16][digit-1]
                finger=[palm+Vector((x,0,.09)),palm+Vector((x*1.30,-.018,.09+length*.55)),palm+Vector((x*1.6,-.033,.09+length))]
            parts.append(tube('Articulated human finger',finger,[.024,.018,.009],sides=10))
            flesh('Finger knuckle',finger[1],(.022,.022,.025))
    else:
        parts.append(tube('Long planted foot',[c,c.lerp(end,.55),end],[.045,.04,.011]))
        for digit in range(3):
            start=c.lerp(end,.65);offset=Vector((side*(digit-1)*.035,0,.06 if n<3 else -.06))
            parts.append(tube('Claw toe',[start,start+offset+Vector((side*.06,-.008,0)),end+offset],[.018,.014,.004],sides=8))

skin=voxel(parts,'CrawlerAnatomy',.010)
# A true recess cut through the face, with cheeks and chin surviving around it.
cutter=sphere('Mouth cutter',(0,1.43,.978),(.157,.214,.32),segments=48,rings=32)
bpy.context.view_layer.objects.active=skin;mod=skin.modifiers.new('Open mouth recess','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
finish_mesh(skin,.045)
# Fine sculpted skin undulation and neck/forearm folds catch grazing light.
for vert in skin.data.vertices:
    p=U(vert.co);amount=.0015*math.sin(p.x*71+p.z*31)*math.sin(p.y*113+p.z*13)
    if p.z>.59 and p.y>1.1:
        amount+=.0018*math.sin(p.y*124+p.x*33)*max(0,1-abs(p.x)/.28)
    vert.co+=vert.normal*amount
    if p.z>.86 and p.y>1.63:
        # Subtle fused eye-socket furrows, without eyes or a nose.
        depress=sum(math.exp(-((p.x-side*.09)/.062)**2-((p.y-1.682)/.025)**2) for side in (-1,1))*.013
        vert.co+=C((0,0,-depress))
skin.data.update()

# Mouth tissue and irregular blunt human teeth. All are part of the weighted mesh,
# with a shared jaw morph so gums, teeth, tongue and saliva cannot drift apart.
oral=[]
oral.append(sphere('Deep throat',(0,1.43,.793),(.150,.202,.065),'CrawlerThroat'))
oral.append(sphere('Tongue',(0,1.313,.863),(.095,.063,.073),'CrawlerGums'))
points=[]
for j in range(65):
    t=j*math.tau/64;points.append((math.cos(t)*.164,1.43+math.sin(t)*.219,.930-.012*abs(math.cos(t))))
oral.append(tube('Wrinkled lips',points,[.013+.003*abs(math.sin(j*math.tau/64)) for j in range(65)],'CrawlerGums',16))
for row in (0,1):
    pts=[]
    for j in range(25):
        angle=math.pi*j/24;pts.append((math.cos(angle)*.143,1.43+(1 if row==0 else -1)*math.sin(angle)*.199,.933))
    oral.append(tube('Upper gum' if row==0 else 'Lower gum',pts,[.014]*len(pts),'CrawlerGums',16))
    for j in range(13):
        angle=math.pi*(j+.5)/13;x=math.cos(angle)*.139;y=1.43+(1 if row==0 else -1)*math.sin(angle)*.189
        length=.045+random.random()*.022+(0.014 if j in (2,10) else 0)
        direction=-1 if row==0 else 1
        incisor=j in (4,5,6,7,8);width=.014 if incisor else .011
        tip=.003 if j in (2,10) else width*.72
        o=tube('Irregular tooth',[(x,y,.941),(x*.982,y+direction*length*.72,.946),(x*.97,y+direction*length, .944)],[width,width*.96,tip],'CrawlerTeeth',16)
        oral.append(o)
for side in (-1,1):
    oral.append(tube('Wet saliva strand',[(side*.10,1.57,.948),(side*.112,1.39,.96),(side*.105,1.228,.942)],[.0019,.0011,.0017],'CrawlerSaliva',6))
face=join(oral,'CrawlerOral');finish_mesh(face)
# Join after UV creation, retaining distinct skin/tissue material slots and UVs.
skin=join([skin,face],'CrawlerAnatomy')
skin.shape_key_add(name='Basis');jaw=skin.shape_key_add(name='Jaw_Open')
for vert in skin.data.vertices:
    p=U(vert.co);is_face=p.z>.66 and p.y>1.08 and abs(p.x)<.30
    # Basis is a tense half-closed mouth; morph exposes the full reference gape.
    if is_face:
        def smooth(a,b,x):
            t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
        f=max(0,min(1,(1.49-p.y)/.25))*smooth(.66,.86,p.z)*smooth(1.08,1.23,p.y)*(1-smooth(.22,.30,abs(p.x)))
        delta=C((0,.09*f,-.008*f))
        skin.data.shape_keys.key_blocks['Basis'].data[vert.index].co+=delta
        vert.co+=delta

add_bite_shape(skin)
armdata=bpy.data.armatures.new('CrawlerSkeleton');rig=bpy.data.objects.new('CrawlerRig',armdata);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);skin.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,a,b,parent=None):
    e=armdata.edit_bones.new(name);e.head=C(a);e.tail=C(b)
    if parent:e.parent=armdata.edit_bones[parent]
    return e
bone('Root',(0,0,0),(0,.25,0));bone('Thorax',(0,1.01,-.36),(0,1.10,.49),'Root')
bone('Neck',(0,1.16,.48),(0,1.44,.65),'Thorax');bone('Head',(0,1.44,.65),(0,1.79,.77),'Neck')
for i,(a,b,c,end) in enumerate(chains):
    bone(f'Limb_{i}_Upper',a,b,'Thorax');bone(f'Limb_{i}_Lower',b,c,f'Limb_{i}_Upper');bone(f'Limb_{i}_Foot',c,end,f'Limb_{i}_Lower')
bpy.ops.object.mode_set(mode='OBJECT')
# Analytic envelope weights: distal rigid digits, blended joints and shoulders.
groups={b.name:skin.vertex_groups.new(name=b.name) for b in armdata.bones}
for vtx in skin.data.vertices:
    p=U(vtx.co)
    if p.z>.51 and p.y>1.12 and abs(p.x)<.30:
        w=1 if p.y>1.40 or p.z>.78 else max(0,min(1,(p.y-1.22)/.18));weights={'Head':w,'Neck':1-w}
    else:
        distances=[]
        for i,(a,b,c,end) in enumerate(chains):
            for suffix,x,y,r in [('Upper',a,b,.095),('Lower',b,c,.075),('Foot',c,end,.07)]:
                t=max(0,min(1,(p-x).dot(y-x)/(y-x).length_squared));d=(p-x-(y-x)*t).length
                # Radius-aware support keeps fleshy torso vertices on the thorax.
                distances.append((max(.002,d-r*.55),f'Limb_{i}_{suffix}'))
        t=max(0,min(1,(p-Vector((0,1.01,-.5))).dot(Vector((0,.1,1)))/1.01))
        torso_dist=(p-Vector((0,1.01+.1*t,-.5+t))).length
        distances.append((max(.025,torso_dist-.20),'Thorax'))
        if p.z>.47 and abs(p.x)<.19:distances.append((max(.008,(p-Vector((0,1.28,.59))).length-.15),'Neck'))
        nearest=sorted(distances)[:3];raw=[(1/max(.025,d))**6 for d,n in nearest];total=sum(raw)
        weights={n:w/total for (d,n),w in zip(nearest,raw)}
    for name,w in weights.items():
        if w>.002:groups[name].add([vtx.index],w,'REPLACE')
# Relax envelope boundaries on the connected skin so bends have smooth muscles,
# rather than a visible nearest-bone boundary through a shoulder or neck.
count=len(skin.data.vertices);names=list(groups);weights=np.zeros((count,len(names)),dtype=np.float32)
for v in skin.data.vertices:
    for g in v.groups:weights[v.index,g.group]=g.weight
edges=np.array([e.vertices[:] for e in skin.data.edges],dtype=np.int32)
degree=np.bincount(edges.ravel(),minlength=count).astype(np.float32)
for iteration in range(4):
    avg=np.zeros_like(weights);np.add.at(avg,edges[:,0],weights[edges[:,1]]);np.add.at(avg,edges[:,1],weights[edges[:,0]])
    weights=weights*.45+avg/np.maximum(1,degree[:,None])*.55
for v in skin.data.vertices:
    strongest=np.argsort(weights[v.index])[-4:];total=float(sum(weights[v.index,k] for k in strongest))
    for g in groups.values():g.remove([v.index])
    for k in strongest:
        w=float(weights[v.index,k])/total
        if w>.0001:groups[names[k]].add([v.index],w,'REPLACE')
skin.parent=rig;mod=skin.modifiers.new('Crawler skinning','ARMATURE');mod.object=rig

def solve_knee(a,c,rest,upper,lower):
    axis=(c-a).normalized();dist=min((c-a).length,upper+lower-.0001)
    along=(upper*upper-lower*lower+dist*dist)/(2*max(.0001,dist))
    bend=rest-a-axis*(rest-a).dot(axis)
    if bend.length<.001:bend=Vector((0,1,0)).cross(axis)
    return a+axis*along+bend.normalized()*math.sqrt(max(0,upper*upper-along*along))
def pose_bone(name,a,b):
    pb=rig.pose.bones[name];rest=armdata.bones[name]
    # Preserve the sculpt's rest roll. Reconstructing roll with track_quat twists
    # shoulder weights even when the limb's direction barely changes.
    rotation=(rest.tail_local-rest.head_local).rotation_difference(C(b)-C(a))@rest.matrix_local.to_quaternion()
    pb.matrix=Matrix.LocRotScale(C(a),rotation,Vector((1,1,1)))
def animate(name,duration,moving):
    bpy.context.scene.render.fps=30;frames=round(duration*30);bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=frames+1
    rig.animation_data_create();rig.animation_data.action=bpy.data.actions.new(name)
    for frame in range(frames+1):
        t=frame/frames;bpy.context.scene.frame_set(frame+1)
        bob=(.018*math.sin(t*math.tau*2) if moving else .012*math.sin(t*math.tau))
        pb=rig.pose.bones['Thorax'];pb.location=Vector((0,0,bob));pb.rotation_mode='XYZ';pb.rotation_euler=(0,.025*math.sin(t*math.tau),.020*math.sin(t*math.tau))
        for prop in ('location','rotation_euler'):pb.keyframe_insert(prop,frame=frame+1)
        bpy.context.view_layer.update()
        for i,(a0,b0,c0,end) in enumerate(chains):
            phase=(t+(0 if i%2==0 else .5)+(0.5 if i>=4 else 0))%1
            c=c0.copy()
            if moving:
                # 62% planted stance; the recovery arc lifts hands and feet clear.
                if phase<.62:c.z+=(.5-phase/.62)*.48
                else:
                    q=(phase-.62)/.38;c.z+=(-.5+q)*.48;c.y+=math.sin(q*math.pi)*(.13 if i%4==0 else .17)
            parent=rig.pose.bones['Thorax'];rest=armdata.bones['Thorax'].matrix_local.inverted()@C(a0)
            a=U(rig.matrix_world@parent.matrix@rest)
            b=solve_knee(a,c,b0,(b0-a0).length,(c0-b0).length)
            pose_bone(f'Limb_{i}_Upper',a,b);bpy.context.view_layer.update();pose_bone(f'Limb_{i}_Lower',b,c);bpy.context.view_layer.update()
            pose_bone(f'Limb_{i}_Foot',c,c+(end-c0));bpy.context.view_layer.update()
            for suffix in ('Upper','Lower','Foot'):
                p=rig.pose.bones[f'Limb_{i}_{suffix}'];p.rotation_mode='QUATERNION'
                for prop in ('location','rotation_quaternion','scale'):p.keyframe_insert(prop,frame=frame+1)
        neck=rig.pose.bones['Neck'];neck.rotation_mode='XYZ';neck.rotation_euler=(.015*math.sin(t*math.tau),.022*math.sin(t*math.tau),0);neck.keyframe_insert('rotation_euler',frame=frame+1)
    action=rig.animation_data.action;rig.animation_data.action=None
    track=rig.animation_data.nla_tracks.new();track.name=name;strip=track.strips.new(name,1,action);strip.action_frame_start=1;strip.action_frame_end=frames+1;track.mute=True
    return action
animate('Crawler_Idle',3,False);animate('Crawler_Crawl',1.2,True)
rig.animation_data.action=None
for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
rig['animation_note']='Eight weighted three-bone limbs; alternating four-limb support; planted stance and lifted recovery; root stays at origin.'
skin['reference_note']='Original pale eyeless humanoid crawler guided by user-supplied reference. No chitin abdomen or vertical torso.'
# Human epidermis maps and a seam-free rest-space atlas shared with the focused
# skin refresh tool. Geometry, skin weights and animation are already complete.
import runpy
skin_material=runpy.run_path(str(Path(__file__).with_name('skin_material.py')))
skin_material['apply'](root,skin)
mouth_material=runpy.run_path(str(Path(__file__).with_name('crawler_mouth_material.py')))
mouth_material['apply'](root,skin)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'walker_01.blend'))
out=root/'Assets/_Game/Art/Meshes/walker_01.fbx'
bpy.ops.export_scene.fbx(filepath=str(out),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
skin.data.calc_loop_triangles()
print('REFERENCE CRAWLER:',len(skin.data.loop_triangles),'triangles,',len(armdata.bones),'bones,',len(bpy.data.actions),'actions; actual FBX exported',flush=True)
