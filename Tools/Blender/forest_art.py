"""Original deterministic forest art. Blender 5.2, metres, no external assets.
blender --background --python Tools/Blender/forest_art.py -- --project PATH
"""
import argparse, math, random, sys
from pathlib import Path
import bpy
import bmesh
import numpy as np
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('--project', type=Path, required=True)
parser.add_argument('--seed', type=int, default=1407)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = args.project.resolve()
source = root / 'ArtSource/Blender'
textures = root / 'Assets/_Game/Art/Textures'
source.mkdir(parents=True, exist_ok=True)
textures.mkdir(parents=True, exist_ok=True)
random.seed(args.seed)
bpy.context.preferences.filepaths.save_version = 0

def noise(size, grid, rng):
    values = rng.random((grid, grid)).astype(np.float32)
    coord = np.arange(size, dtype=np.float32) * grid / size
    i = coord.astype(int)
    t = coord - i
    t = t * t * (3 - 2 * t)
    a = values[i[:, None] % grid, i[None, :] % grid]
    b = values[(i[:, None]+1) % grid, i[None, :] % grid]
    c = values[i[:, None] % grid, (i[None, :]+1) % grid]
    d = values[(i[:, None]+1) % grid, (i[None, :]+1) % grid]
    return (a*(1-t[:, None])+b*t[:, None])*(1-t[None, :])+(c*(1-t[:, None])+d*t[:, None])*t[None, :]

def png(name, pixels, linear=False):
    image = bpy.data.images.new(name, pixels.shape[1], pixels.shape[0], alpha=True)
    image.colorspace_settings.name = 'Non-Color' if linear else 'sRGB'
    image.pixels.foreach_set(pixels.astype(np.float32).reshape(-1))
    image.filepath_raw = str(textures/(name+'.png'))
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)

def texture_set(name, base, kind, size=1024, metallic=0, smoothness=.18):
    rng = np.random.default_rng(args.seed+sum(map(ord, name)))
    n = sum(noise(size, grid, rng)*weight for grid, weight in [(5,.35),(17,.30),(61,.22),(193,.13)])
    u,v = np.meshgrid(np.arange(size)/size, np.arange(size)/size)
    detail = rng.random((size,size)).astype(np.float32)
    height = n.copy()
    color = np.ones((size,size,4), dtype=np.float32)
    shade = .60+n*.75
    if kind in ('wood','bark'):
        grain = np.sin((u*(78 if kind=='wood' else 38)+n*2.5+np.sin(v*math.tau)*.8)*math.tau)
        grooves = np.clip((grain-.4)*1.4,0,1)
        height = n*.5-grooves*.28
        shade *= .65+.35*(1-grooves)
        if kind=='wood':
            rings = np.sin(np.sqrt(((u-.32)*8)**2+((v-.52)*2)**2)*24)
            shade *= 1-np.exp(-((u-.32)**2*180+(v-.52)**2*18))*(.2+.1*rings)
    if kind in ('earth','trail'):
        height = n+np.clip((detail-.985)*55,0,1)*.18
        shade *= .65+detail*.45
        moss = np.clip((noise(size,11,rng)-.54)*3,0,.7)
        for channel,value in enumerate(base):
            color[:,:,channel] = value*shade*(1-moss)+(.11,.145,.074)[channel]*moss
    else:
        for channel,value in enumerate(base): color[:,:,channel] = value*shade
    if kind=='metal':
        rust = np.clip((n-.46)*4,0,.85)
        for c in range(3): color[:,:,c] = color[:,:,c]*(1-rust)+(.31,.125,.055)[c]*rust
        height = n*.15+rust*.12
    if kind=='leaf':
        veins = np.abs(np.sin((u*22+v*13)*math.tau))
        color[:,:,:3] *= (.7+veins*.3)[:,:,None]
        height = n*.15+veins*.1
    png(name+'_BaseColor',np.clip(color,0,1))
    dx = (np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*5
    dy = (np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*5
    length = np.sqrt(dx*dx+dy*dy+1)
    normal = np.ones_like(color)
    normal[:,:,0] = .5-dx/length*.5
    normal[:,:,1] = .5-dy/length*.5
    normal[:,:,2] = .5+1/length*.5
    png(name+'_Normal',normal,True)
    mask = np.ones_like(color)
    mask[:,:,0] = metallic*(.45+n*.55)
    mask[:,:,1] = .70+n*.30
    mask[:,:,2] = 0
    mask[:,:,3] = smoothness*(.6+n*.4)
    png(name+'_Mask',mask,True)

def leaf_alpha(name,broad=False,size=1024):
    # Original needle silhouettes, not solid triangular foliage cards.
    u,v=np.meshgrid(np.arange(size,dtype=np.float32)/size,np.arange(size,dtype=np.float32)/size)
    image=bpy.data.images.load(str(textures/(name+'_BaseColor.png')),check_existing=False)
    color=np.array(image.pixels[:],dtype=np.float32).reshape((size,size,4))
    bpy.data.images.remove(image)
    if broad:
        alpha=np.clip((1-((u-.5)/.46)**2-((v-.48)/.50)**2)*18,0,1)
    else:
        alpha=np.clip((.005-np.abs(u-.5))*size+1,0,1)
        rng=random.Random(args.seed)
        for n in range(31):
            t=.045+n*.028
            for side in (-1,1):
                ax,ay=.5,t
                bx,by=.5+side*(.42*(1-t)**.58)*rng.uniform(.78,1.07),t+rng.uniform(.08,.13)
                dx,dy=bx-ax,by-ay
                fraction=np.clip(((u-ax)*dx+(v-ay)*dy)/(dx*dx+dy*dy),0,1)
                distance=np.sqrt((u-ax-fraction*dx)**2+(v-ay-fraction*dy)**2)
                alpha=np.maximum(alpha,np.clip((.0028-distance)*size+.5,0,1))
    color[:,:,3]=alpha
    png(name+'_BaseColor',color)

for data in [('Timber',(.38,.29,.20),'wood',2048,0,.23),('Bark',(.23,.22,.18),'bark',2048,0,.12),
             ('ForestFloor',(.22,.235,.16),'earth',2048,0,.12),('Trail',(.31,.27,.19),'trail',1024,0,.20),
             ('Stone',(.32,.35,.31),'stone',1024,0,.32),('Steel',(.30,.34,.33),'metal',1024,.7,.40),
             ('Needles',(.13,.24,.15),'leaf',1024,0,.12),('Broadleaf',(.19,.27,.12),'leaf',1024,0,.12),
             ('PaleBark',(.49,.48,.38),'bark',1024,0,.16)]:
    texture_set(*data)
leaf_alpha('Needles')
leaf_alpha('Broadleaf',True)
print('Generated nine original PBR texture sets and foliage alpha masks',flush=True)

def clean():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1

def material(name,color,emission=0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color,1)
    mat.use_nodes = True
    p = mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = .7
    if emission:
        p.inputs['Emission Color'].default_value = (*color,1)
        p.inputs['Emission Strength'].default_value = emission
    return mat

mats = {name:material(name,color,emission) for name,color,emission in [
    ('Bark',(.23,.22,.18),0),('Needles',(.13,.24,.15),0),('Timber',(.38,.29,.20),0),
    ('Steel',(.30,.34,.33),0),('Stone',(.32,.35,.31),0),('PaleBark',(.49,.48,.38),0),
    ('Broadleaf',(.19,.27,.12),0),('Grass',(.13,.24,.15),0),
    ('Glass',(.13,.22,.24),0),('Amber',(1,.53,.15),2),('Fabric',(.16,.18,.16),0)]}

def segment(a,b,ra,rb,verts,faces,count=8):
    a,b = Vector(a),Vector(b)
    axis = (b-a).normalized()
    u = axis.cross(Vector((0,1,0)))
    if u.length<.1: u = axis.cross(Vector((1,0,0)))
    u.normalize()
    v = axis.cross(u).normalized()
    base = len(verts)
    for point,radius in ((a,ra),(b,rb)):
        for i in range(count):
            angle = i*math.tau/count
            verts.append(tuple(point+radius*(u*math.cos(angle)+v*math.sin(angle))))
    for i in range(count):
        j = (i+1)%count
        faces.append((base+i,base+j,base+count+j,base+count+i))
    faces.extend([tuple(base+i for i in reversed(range(count))),tuple(base+count+i for i in range(count))])

def leaf(a,direction,length,width,verts,faces):
    a,direction = Vector(a),Vector(direction).normalized()
    side = Vector((-direction.y,direction.x,0)).normalized()
    base = len(verts)
    points = [a,a+direction*length*.28+side*width,
              a+direction*length*.7+side*width*.55+Vector((0,0,-.08)),a+direction*length+Vector((0,0,-.16)),
              a+direction*length*.7-side*width*.55+Vector((0,0,-.08)),a+direction*length*.28-side*width,
              a+direction*length*.48+Vector((0,0,.12))]
    verts.extend(map(tuple,points))
    for i in range(6):
        faces.append((base+i,base+(i+1)%6,base+6))
        faces.append((base+6,base+(i+1)%6,base+i))

def mesh(name,verts,faces,mat,smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts,[],faces)
    data.update()
    obj = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(mats[mat])
    for face in data.polygons: face.use_smooth = smooth
    if mat in ('Needles','Broadleaf'):
        uv=data.uv_layers.new(name='UVMap')
        points=[(.5,0),(0,.28),(.225,.70),(.5,1),(.775,.70),(1,.28),(.5,.48)]
        for polygon in data.polygons:
            for loop in polygon.loop_indices:
                uv.data[loop].uv=points[data.loops[loop].vertex_index%7]
    return obj

def box(name,pos,size,mat,bevel=.02,rotation=None):
    # Blender Y is negative Unity Z. FBX's Unity import reverses handedness.
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-pos[0],-pos[2],pos[1]))
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0],size[2],size[1])
    if rotation: obj.rotation_euler = (rotation[0],-rotation[1],rotation[2])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        modifier = obj.modifiers.new('Worn edges','BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj

def simplify_components(obj,level):
    # Decimate cannot remove the many disconnected leaf/twig islands by itself.
    # Keep a deterministic subset of them, with all central trunk segments intact.
    data=obj.data
    parent=list(range(len(data.vertices)))
    def find(n):
        while parent[n]!=n:
            parent[n]=parent[parent[n]]
            n=parent[n]
        return n
    for polygon in data.polygons:
        first=find(polygon.vertices[0])
        for vertex in polygon.vertices[1:]: parent[find(vertex)]=first
    islands={}
    for vertex in data.vertices: islands.setdefault(find(vertex.index),[]).append(vertex.co)
    keep={}
    for n,(key,points) in enumerate(sorted(islands.items())):
        center=sum(points,Vector())/len(points)
        keep[key]=(center.x*center.x+center.y*center.y<.16 or n%(2 if level==1 else 6)==0)
    bm=bmesh.new()
    bm.from_mesh(data)
    bm.faces.ensure_lookup_table()
    remove=[face for face in bm.faces if not keep[find(face.verts[0].index)]]
    bmesh.ops.delete(bm,geom=remove,context='FACES')
    bm.to_mesh(data)
    bm.free()
    data.update()

def finish(asset_id,lod=False,hero=False):
    objects = [obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = asset_id+('_LOD0' if lod else '')
    bpy.context.scene.cursor.location = (0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    foliage_uv={}
    if obj.data.uv_layers.active:
        for polygon in obj.data.polygons:
            if obj.data.materials[polygon.material_index].name in ('Needles','Broadleaf'):
                for loop in polygon.loop_indices: foliage_uv[loop]=tuple(obj.data.uv_layers.active.data[loop].uv)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.008)
    bpy.ops.object.mode_set(mode='OBJECT')
    for loop,value in foliage_uv.items(): obj.data.uv_layers.active.data[loop].uv=value
    if asset_id in ('blackpine_01','blackpine_02'):
        modifier = obj.modifiers.new('Game mesh budget','DECIMATE')
        modifier.ratio = .70
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    if hero:
        obj.shape_key_add(name='Basis')
        for name,amount in [('Dread_Stage1',.28),('Dread_Stage2',.65)]:
            key = obj.shape_key_add(name=name)
            for point in key.data:
                z = point.co.z
                factor = max(0,(z-2)/8)
                if abs(point.co.x)+abs(point.co.y)>.35:
                    point.co.x += math.sin(z*.6)*factor*amount
                    point.co.y += math.cos(z*.7)*factor*amount*.6
    if lod:
        for level,ratio in [(1,.38),(2,.13)]:
            copy = obj.copy()
            copy.data = obj.data.copy()
            copy.name = asset_id+'_LOD'+str(level)
            bpy.context.collection.objects.link(copy)
            bpy.context.view_layer.objects.active = copy
            simplify_components(copy,level)
            mod = copy.modifiers.new('Distance silhouette','DECIMATE')
            mod.ratio = ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
            copy.hide_render = True
    bpy.ops.wm.save_as_mainfile(filepath=str(source/(asset_id+'.blend')))
    obj.data.calc_loop_triangles()
    print('ART ASSET',asset_id,len(obj.data.loop_triangles),'LOD0 triangles',flush=True)

def pine(asset_id,variant=0,dry=False,hero=False):
    clean()
    rng = random.Random(args.seed+variant*91+(100 if dry else 0))
    verts,faces,foliage,leaves = [],[],[],[]
    for n in range(14):
        z = n*10/14
        segment((.08*math.sin(n*.5),.06*math.cos(n*.6),z),(.08*math.sin((n+1)*.5),.06*math.cos((n+1)*.6),z+10/14),
                .32*(1-n/15)**.9,.32*(1-(n+1)/15)**.9,verts,faces,10)
    for n in range(6):
        angle = n*math.tau/6
        segment((0,0,.4),(math.cos(angle)*.95,math.sin(angle)*.95,.03),.19,.03,verts,faces)
    for tier in range(10):
        z, count = 1.6+tier*.79, 3 if dry else 5
        for n in range(count):
            angle = n*math.tau/count+tier*1.3+rng.uniform(-.2,.2)
            direction = Vector((math.cos(angle),math.sin(angle),0))
            length = (3.05-tier*.255)*rng.uniform(.78,1.12)*(.85 if dry else 1)
            start = Vector((0,0,z))
            mid = start+direction*length*.6+Vector((0,0,-.22))
            end = start+direction*length+Vector((0,0,.15 if dry else -.06))
            segment(start,mid,.095,.046,verts,faces,7)
            segment(mid,end,.046,.012,verts,faces,5)
            turn = Vector((-direction.y,direction.x,0))
            for twig in range(3):
                origin = start.lerp(end,.35+twig*.22)
                for side in (-1,1):
                    branch = (direction*.5+turn*side).normalized()
                    tip = origin+branch*(.62-tier*.035)+Vector((0,0,.14))
                    segment(origin,tip,.023,.003,verts,faces,4)
                    if not dry:
                        leaf(origin,branch,rng.uniform(.58,.88)*(1-tier*.03),.24-tier*.013,foliage,leaves)
            if not dry: leaf(mid,direction,length*.58,.32,foliage,leaves)
    mesh('Branch skeleton',verts,faces,'Bark')
    if foliage: mesh('Needle sprays',foliage,leaves,'Needles',False)
    finish(asset_id,not hero,hero)

pine('pine_hero_01',hero=True)
pine('dead_hero_01',dry=True,hero=True)
pine('blackpine_01',variant=1)
pine('blackpine_02',variant=2)
pine('snag_01',variant=3,dry=True)

clean()
verts,faces,foliage,leaves = [],[],[],[]
segment((0,0,0),(.15,.06,7.5),.24,.06,verts,faces,10)
for n in range(18):
    angle,z = n*2.399,3.2+n*.2
    direction = Vector((math.sin(angle),math.cos(angle),.3))
    a = Vector((.08,0,z))
    b = a+direction*random.uniform(1.8,2.8)
    segment(a,b,.07,.014,verts,faces,6)
    for m in range(14):
        p = a.lerp(b,.35+m*.045)+Vector((random.uniform(-.6,.6),random.uniform(-.6,.6),random.uniform(-.15,.45)))
        leaf(p,(math.sin(m*2.4),math.cos(m*2.4),.2),.38,.13,foliage,leaves)
mesh('Pale trunk',verts,faces,'PaleBark')
mesh('Broad leaves',foliage,leaves,'Broadleaf',False)
finish('birch_01',True)

for variant in range(3):
    clean()
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=1,location=(0,0,.48))
    obj = bpy.context.object
    rng = random.Random(args.seed+variant)
    for vertex in obj.data.vertices:
        vertex.co *= rng.uniform(.82,1.18)
        vertex.co.x *= 1.25+variant*.12
        vertex.co.y *= .85
        vertex.co.z *= .66
    obj.data.materials.append(mats['Stone'])
    for face in obj.data.polygons: face.use_smooth = True
    finish('moss_rock_0'+str(variant+1),True)

clean()
verts,faces = [],[]
for n in range(9):
    angle = n*math.tau/9
    direction = Vector((math.sin(angle),math.cos(angle),0))
    for m in range(9):
        f = (m+1)/10
        p = direction*f*.85+Vector((0,0,math.sin(f*math.pi)*.52))
        for side in (-1,1):
            turn = Vector((-direction.y,direction.x,0))*side
            leaf(p,direction*.3+turn,.22*(1-f*.65),.045,verts,faces)
mesh('Forest fern',verts,faces,'Needles',False)
finish('fern_01',True)
clean()
verts,faces = [],[]
for n in range(32):
    angle = random.uniform(0,math.tau)
    base = Vector((random.uniform(-.18,.18),random.uniform(-.18,.18),0))
    segment(base,base+Vector((math.sin(angle)*.19,math.cos(angle)*.19,random.uniform(.25,.65))),.015,.001,verts,faces,3)
mesh('Grass tuft',verts,faces,'Grass',False)
finish('grass_01',True)

def cabin(asset_id,store=False):
    clean()
    box('Stone foundation',(0,-.10,0),(9.1,.2,8.1),'Stone')
    for n in range(12): box('Floorboard',(-4.1+n*.74,.035,0),(.70,.06,7.8),'Timber',.006)
    for n in range(15):
        y = .15+n*.205
        box('Back siding',(0,y,-4),(9,.193,.24),'Timber',.012)
        for side in (-1,1):
            box('Side siding',(side*4.5,y,0),(.24,.193,8),'Timber',.012)
            box('Front siding',(side*3.15,y,4),(2.7,.193,.24),'Timber',.012)
    box('Door lintel',(0,3,4),(3.6,.4,.25),'Timber')
    for x in (-4.42,-1.82,1.82,4.42): box('Front trim',(x,1.62,4.14),(.12,3.2,.13),'PaleBark',.008)
    for x in (-4.42,4.42): box('Corner post',(x,1.62,-3.9),(.16,3.2,.16),'PaleBark')
    for side in (-1,1):
        for n in range(14):
            box('Roof panel',(side*2.45,3.99,-4.36+n*.67),(5.15,.13,.645),'Steel',.015,(0,math.radians(side*17),0))
        for n in range(12): box('Gable board',(0,3.25+n*.12,side*4),(max(.12,8.95-n*.73),.11,.2),'Timber',.008)
    box('Ridge cap',(0,4.77,0),(.20,.15,9.4),'Steel')
    for side in (-1,1): box('Roof fascia',(side*2.45,3.97,4.65),(5.2,.18,.13),'PaleBark',.015,(0,math.radians(side*17),0))
    for x in (-3.16,3.16):
        box('Window glass',(x,1.85,4.145),(1.15,1,.025),'Glass',.002)
        for dx in (-.63,.63): box('Window stile',(x+dx,1.85,4.18),(.075,1.18,.08),'PaleBark',.006)
        for y in (1.28,1.85,2.42): box('Window rail',(x,y,4.19),(1.30,.065,.09),'PaleBark',.006)
        box('Window mullion',(x,1.85,4.20),(.055,1.1,.08),'PaleBark',.005)
        box('Window sill',(x,1.22,4.26),(1.42,.09,.25),'Timber')
    for n in range(13): box('Porch plank',(-4.2+n*.7,.015,4.72),(.675,.03,1.45),'Timber',.008)
    for x in (-4.25,4.25): box('Porch post',(x,1.8,5.1),(.14,3.6,.14),'Timber')
    box('Desk top',(-2.7,.95,1),(2.2,.2,1.1),'Timber',.045)
    for x in (-3.55,-1.85):
        for z in (.65,1.35): box('Desk leg',(x,.45,z),(.11,.9,.11),'Timber')
    box('Desk apron',(-2.7,.78,1),(2,.22,.94),'Timber')
    for x in (-2.8,0,2.8): box('Ceiling rafter',(x,3.15,0),(.17,.17,8),'Timber')
    for y in (.35,1.1,1.85,2.6): box('Storage shelf',(3.65,y,-2.1),(1.25,.07,2.4),'Timber')
    for z in (-3.2,-1):
        for x in (3.07,4.25): box('Shelf upright',(x,1.45,z),(.08,2.9,.08),'Steel')
    for n in range(7 if store else 3):
        x,y = 3.5+(n%2)*.45,.58+(n//2)*.74
        box('Field supply crate',(x,y,-2.5),(.43,.36,.6),'Timber')
        for dx in (-.16,.16): box('Crate strap',(x+dx,y,-2.19),(.035,.39,.025),'Steel',.002)
    if not store:
        box('Cot frame',(-2.8,.42,-2.45),(1.15,.14,2.35),'Steel')
        box('Cot mattress',(-2.8,.56,-2.45),(1.10,.15,2.2),'Fabric',.05)
        box('Pillow',(-2.8,.70,-3.18),(.72,.13,.40),'PaleBark',.05)
        box('Chimney',(2.6,4.9,-1.7),(.65,1.65,.7),'Stone')
        box('Chimney hood',(2.6,5.75,-1.7),(.85,.12,.9),'Steel')
    finish(asset_id)

cabin('ranger_cabin_01')
cabin('storehouse_01',True)
clean()
box('Lamp backplate',(0,0,-.09),(.30,.54,.08),'Steel')
box('Amber glass',(0,0,0),(.20,.32,.18),'Amber',.04)
for x in (-.14,.14): box('Lantern bars',(x,0,.07),(.03,.42,.03),'Steel',.006)
for y in (-.22,.22): box('Lantern cap',(0,y,0),(.35,.065,.26),'Steel')
finish('shelter_lantern_01')

clean()
verts,faces = [],[]
segment((0,0,-.15),(0,0,.09),.028,.028,verts,faces,20)
segment((0,0,.09),(0,0,.14),.028,.042,verts,faces,20)
for n in range(9):
    z = -.12+n*.021
    segment((0,0,z),(0,0,z+.007),.031,.031,verts,faces,20)
segment((0,0,.14),(0,0,.166),.045,.045,verts,faces,24)
mesh('Flashlight housing',verts,faces,'Steel')
verts,faces = [],[]
segment((0,0,.163),(0,0,.167),.037,.037,verts,faces,24)
mesh('Flashlight lens',verts,faces,'Glass')
box('Power button',(0,.022,-.02),(.018,.013,.028),'Fabric',.004)
finish('field_flashlight_01')

clean()
for x in (-2.45,2.45): box('Gate upright',(x,1.8,0),(.10,3.6,.12),'Steel')
for y in (.10,1.8,3.50): box('Gate crossbar',(0,y,0),(5,.10,.12),'Steel')
for n in range(11): box('Vertical gate bars',(-2.25+n*.45,1.8,0),(.045,3.4,.055),'Steel',.008)
box('Gate warning plate',(0,2.6,.08),(1.4,.5,.05),'Steel')
finish('forest_gate_01')
clean()
for x in (-4.9,0,4.9): box('Fence post',(x,1.8,0),(.1,3.6,.10),'Steel')
for y in (.14,1.2,2.3,3.45): box('Fence rail',(0,y,0),(10,.06,.065),'Steel',.008)
for n in range(30): box('Fence pales',(-4.8+n*.33,1.8,0),(.025,3.4,.045),'Steel',.005)
finish('forest_fence_01')
clean()
box('Fuse panel housing',(0,0,0),(.8,.9,.16),'Steel',.04)
box('Recessed panel face',(0,0,.09),(.67,.74,.025),'Fabric',.01)
for n in range(3):
    box('Fuse socket',(-.18+n*.18,.10,.12),(.10,.24,.075),'Steel',.012)
    box('Fuse ceramic',(-.18+n*.18,.10,.16),(.05,.14,.02),'PaleBark',.008)
box('Master switch',(0,-.24,.15),(.22,.13,.08),'Steel',.015)
box('Amber indicator',(.23,-.23,.13),(.045,.045,.025),'Amber',.005)
finish('field_power_panel_01')
print('ALL ART ASSETS COMPLETE',flush=True)

exec(compile((Path(__file__).parent/'creature_art.py').read_text(encoding='utf-8'), 'creature_art.py','exec'))
