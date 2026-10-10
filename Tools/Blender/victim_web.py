"""Original irregular three-dimensional silk scaffold with an intact central sheet."""
import argparse,math,random,sys
from pathlib import Path
import bpy
from mathutils import Vector
parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,required=True);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.project.resolve()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);random.seed(8169)
material=bpy.data.materials.new('WebSilk');material.diffuse_color=(.48,.52,.48,1)
vertices={'Web_Remnant_Rim':[],'Web_Intact_Core':[]};faces={name:[] for name in vertices}
def strand(name,a,b,radius):
    # Blender Z-up, front -Y. Root is centered behind the suspended body in Unity.
    a=Vector((a[0],-a[2],a[1]));b=Vector((b[0],-b[2],b[1]));axis=(b-a).normalized()
    side=axis.cross(Vector((0,1,0))).normalized();other=axis.cross(side).normalized();base=len(vertices[name])
    for p in (a,b):
        for j in range(6):vertices[name].append(p+radius*(math.cos(j*math.tau/6)*side+math.sin(j*math.tau/6)*other))
    for j in range(6):faces[name].append((base+j,base+(j+1)%6,base+(j+1)%6+6,base+j+6))
count=22;radii=(.23,.40,.60,.81,1)
def point(index,radius):
    angle=index*math.tau/count;wobble=1+.04*math.sin(index*2.7+radius*13)
    return Vector((math.cos(angle)*1.65*radius*wobble,math.sin(angle)*1.86*radius*wobble,.05*math.sin(angle*3+radius*8)))
for index in range(count):
    previous=Vector((0,0,0))
    for radius in radii:
        p=point(index,radius);strand('Web_Intact_Core' if radius<=.60 else 'Web_Remnant_Rim',previous,p,.0037 if index%3==0 else .0025);previous=p
for radius in radii:
    name='Web_Intact_Core' if radius<=.60 else 'Web_Remnant_Rim'
    for index in range(count):
        a=point(index,radius);b=point((index+1)%count,radius);mid=(a+b)*.5;mid.y-=.018+random.random()*.03
        strand(name,a,mid,.0018);strand(name,mid,b,.0018)
for index,end in ((11,(-2.25,0,0)),(6,(.4,1.7,0)),(14,(-2.15,-1.3,0)),(0,(.7,1.8,0)),(16,(.4,-2.15,-.2))):
    strand('Web_Remnant_Rim',point(index,1),end,.0033)
for name in vertices:
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices[name],[],faces[name]);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(material)
    for face in mesh.polygons:face.use_smooth=True
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/Blender/victim_web_01.blend'),compress=True)
print('VICTIM WEB: original 22-radial silk scaffold, five sagging rings, removable inner mesh and persistent torn rim.')
