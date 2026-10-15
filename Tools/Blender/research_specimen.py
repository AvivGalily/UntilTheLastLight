"""Original suspended reptilian humanoid, in metres; also renders the Walker study views."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
import forest_bodies as F
F.ROOT = ROOT
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
F.material('SpecimenSkin', (.23,.34,.27), .42)
F.material('SpecimenBelly', (.42,.46,.32), .5)
F.material('SpecimenSpine', (.11,.19,.17), .38)
F.material('SpecimenEye', (.76,.63,.22), .2)
F.material('SpecimenPupil', (.008,.014,.011), .15)
S=F.sphere; T=F.tube
# Slightly arched, head-up torso, limp asymmetrical limbs and a coiled tail.
S('Pelvis',(0,.88,0),(.27,.26,.19),'SpecimenSkin')
S('Abdomen',(0,1.17,0),(.23,.37,.17),'SpecimenSkin')
S('Rib cage',(0,1.49,-.015),(.34,.30,.21),'SpecimenSkin')
S('Ventral scutes',(0,1.27,.153),(.19,.43,.046),'SpecimenBelly')
for i in range(10):
    S('Abdominal plate %02d'%i,(0,.94+i*.069,.179),(.19*(.8+.2*math.sin(i*.35)),.026,.022),'SpecimenBelly',24,12)
T('Throat',[(0,1.61,0),(0,1.78,.025),(0,1.87,.06)],[.14,.125,.13],'SpecimenSkin')
head=S('Reptilian cranium',(0,1.94,.075),(.19,.22,.17),'SpecimenSkin')
head.rotation_euler.x=math.radians(-18)
S('Long muzzle',(0,1.94,.225),(.14,.095,.21),'SpecimenSkin')
S('Lower jaw',(0,1.875,.205),(.135,.055,.19),'SpecimenBelly')
for side in (-1,1):
    S('Nostril',(side*.065,1.99,.403),(.022,.012,.006),'SpecimenPupil',20,12)
    S('Heavy brow',(side*.146,2.031,.169),(.075,.035,.095),'SpecimenSpine')
    S('Recessed eye',(side*.17,1.999,.182),(.038,.039,.048),'SpecimenEye')
    S('Vertical slit pupil',(side*.194,2,.206),(.009,.029,.011),'SpecimenPupil',20,12)
    # Three dark gill slits behind either cheek.
    for i in range(3):
        S('Gill slit',(side*.142,1.77+i*.043,.07),(.011,.014,.065),'SpecimenSpine',20,12)
    a=side
    arm=[(a*.27,1.58,0),(a*.43,1.34,.015),(a*.48,1.10,.11),(a*.57,1.01,.19)]
    T('Slack upper arm',arm[:3],[.12,.10,.065],'SpecimenSkin',24)
    T('Forearm',arm[1:],[.095,.07,.052],'SpecimenSkin',24)
    S('Palm',arm[-1],(.082,.11,.045),'SpecimenSkin')
    for finger in range(4):
        x=a*(.51+finger*.042)
        T('Long finger',[(x,1,.20),(x+a*.018,.86+finger*.012,.24),(x+a*.01,.81+finger*.012,.28)],[.022,.017,.006],'SpecimenSkin',12)
        T('Finger claw',[(x+a*.01,.81+finger*.012,.28),(x,.81+finger*.012,.32)],[.012,.001],'SpecimenSpine',10)
    # A relaxed knee and elongated reptilian heel.
    leg=[(a*.17,.89,0),(a*.23,.56,.055+a*.05),(a*.20,.26,-.055),(a*.25,.14,.13)]
    T('Thigh',leg[:3],[.15,.115,.062],'SpecimenSkin',28)
    T('Digitigrade shin',leg[1:],[.105,.057,.052],'SpecimenSkin',24)
    S('Foot',leg[-1],(.09,.06,.15),'SpecimenSkin')
    for toe in range(3):
        x=a*.25+(toe-1)*.064
        T('Toe',[(x,.15,.15),(x,.11,.32),(x,.09,.36)],[.028,.020,.003],'SpecimenSpine',12)
T('Coiled muscular tail',[(0,.91,-.12),(0,.66,-.29),(.15,.40,-.42),(.37,.31,-.35),(.51,.45,-.18),(.47,.61,-.03),(.38,.65,.02)],[.14,.12,.09,.065,.045,.025,.002],'SpecimenSkin',28)
for i in range(15):
    y=.82+i*.078
    T('Dorsal ridge',[(0,y,-.17),(0,y+.015,-.27),(0,y+.07,-.17)],[.039,.016,.001],'SpecimenSpine',12)
# Real raised scales break the silhouette; the material supplies smaller scales.
for side in (-1,1):
    for row in range(16):
        for col in range(6):
            theta=(col/5)*math.pi*.9 + (.05 if row%2 else 0)
            x=side*(.235+.03*math.sin(row*.4))*math.sin(theta)
            z=.177*math.cos(theta)
            S('Dermal scale',(x,.92+row*.043,z),(.020,.024,.009),'SpecimenSkin',12,8)
# Fuse the overlapping anatomical volumes into one organic skin surface.
bpy.ops.object.select_all(action='DESELECT')
skin_parts=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.materials and o.data.materials[0].name=='SpecimenSkin']
for o in skin_parts:o.select_set(True)
bpy.context.view_layer.objects.active=skin_parts[0];bpy.ops.object.join();skin=bpy.context.object
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
remesh=skin.modifiers.new('Continuous anatomical surface','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.009;remesh.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=skin.modifiers.new('Soft tissue transitions','SMOOTH');smooth.factor=.7;smooth.iterations=3;bpy.ops.object.modifier_apply(modifier=smooth.name)
reduce=skin.modifiers.new('Containment specimen budget','DECIMATE');reduce.ratio=.45;bpy.ops.object.modifier_apply(modifier=reduce.name)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.cube_project(cube_size=1);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='SELECT')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.join();body=bpy.context.object;body.name='SuspendedReptilianSpecimen'
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
# The authoring coordinate conversion reflects X: repair tube winding after joining.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/Blender/research_specimen_01.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_Game/Art/Meshes/research_specimen_01.fbx'),object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False)

def render_study():
    # Study sheets use the actual main creature, rendered without its skin textures.
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Blender/walker_01.blend'))
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x=1100;scene.render.resolution_y=760;scene.render.resolution_percentage=100
    scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG'
    shade=scene.display.shading;shade.light='STUDIO';shade.color_type='SINGLE';shade.single_color=(.68,.69,.65)
    shade.show_shadows=True;shade.show_cavity=True;shade.cavity_type='BOTH';shade.show_object_outline=True
    shade.object_outline_color=(.06,.12,.13);shade.curvature_ridge_factor=1.5;shade.curvature_valley_factor=1.4
    scene.view_settings.view_transform='Standard'
    points=[o.matrix_world@Vector(c) for o in scene.objects if o.type=='MESH' for c in o.bound_box]
    center=sum(points,Vector())/len(points)
    cam_data=bpy.data.cameras.new('Study orthographic');cam=bpy.data.objects.new('Study orthographic',cam_data);scene.collection.objects.link(cam);scene.camera=cam
    cam_data.type='ORTHO';cam_data.ortho_scale=4.5
    folder=ROOT/'ArtSource/Research';folder.mkdir(exist_ok=True)
    for name,direction in [('front',(0,-1,.08)),('side',(1,0,.06)),('dorsal',(0,0,1)),('oblique',(.8,-1,.7))]:
        cam.location=center+Vector(direction).normalized()*9
        cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(folder/('walker_'+name+'.png'));bpy.ops.render.render(write_still=True)

if '--specimen-only' not in sys.argv:render_study()
print('RESEARCH ART EXPORT COMPLETE',flush=True)
