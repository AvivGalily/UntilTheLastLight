import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];args=sys.argv[sys.argv.index('--')+1:];name=args[0] if args else 'forest_body_01'
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Blender'/(name+'.blend')),load_ui=False,use_scripts=False)
for ob in bpy.data.objects:
    if ob.type=='MESH':print('MESH',ob.name,list(ob.dimensions),flush=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.09,.09,.09);scene.view_settings.view_transform='AgX'
def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
def area(pos,power,size):
    data=bpy.data.lights.new('Studio softbox','AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new('Studio softbox',data);scene.collection.objects.link(ob);ob.location=pos;aim(ob,(0,0,.2))
area((1,-1.5,3),160,3);area((-2,1,1.8),95,2)
data=bpy.data.cameras.new('Review');camera=bpy.data.objects.new('Review',data);scene.collection.objects.link(camera);scene.camera=camera
camera.location=(1.3,-1.9,2.25);data.lens=48;aim(camera,(0,0,.15))
if name=='zombie_wolf_01':camera.location=(1.2,-1.8,1.5);aim(camera,(0,-.12,.67));scene.cycles.samples=12
out=root/'Docs/Screenshots/Combat';out.mkdir(parents=True,exist_ok=True)
scene.render.filepath=str(out/(name+'-studio.png'));bpy.ops.render.render(write_still=True)
camera.location=(.25,-1.40,.80);data.lens=60;aim(camera,(0,-.65,.18))
if name=='zombie_wolf_01':camera.location=(.58,-1.23,1.17);aim(camera,(.025,-.59,.92));data.lens=64
scene.render.filepath=str(out/(name+'-face.png'));bpy.ops.render.render(write_still=True)
