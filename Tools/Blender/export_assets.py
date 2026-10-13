"""Validate meshes and export explicit FBX; Unity never imports .blend files."""
import argparse
import json
import math
import sys
from pathlib import Path
import bpy

parser=argparse.ArgumentParser()
parser.add_argument('--project',type=Path,required=True)
parser.add_argument('--asset',help='Refresh a single source and retain other manifest records')
parser.add_argument('--manifest-only',action='store_true',help='Validate and record already exported art without rewriting FBXs')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=args.project.resolve()
out=root/'Assets'/'_Game'/'Art'/'Meshes'
out.mkdir(parents=True,exist_ok=True)
records=[]
paths=sorted((root/'ArtSource'/'Blender').glob('*.blend'))
if args.asset:
    paths=[path for path in paths if path.stem==args.asset]
    if not paths:raise ValueError('Unknown asset '+args.asset)
    manifest=root/'Docs/AssetManifest.json'
    if manifest.exists():records=[record for record in json.loads(manifest.read_text(encoding='utf-8'))['assets'] if record['id']!=args.asset]
for path in paths:
    bpy.ops.wm.open_mainfile(filepath=str(path))
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    if not meshes: raise ValueError('Empty scene '+str(path))
    triangles=0
    lods={}
    shapes=[]
    materials=set()
    for obj in meshes:
        if not obj.data.vertices or not obj.data.uv_layers: raise ValueError('Empty mesh or missing UVs')
        if any(not math.isfinite(axis) for vertex in obj.data.vertices for axis in vertex.co): raise ValueError('Invalid vertex')
        if max(obj.dimensions)>30 or max(obj.dimensions)<(.001 if path.stem in ('crow_01','cinder_head_01') else .01 if path.stem in ('hanging_survivor_01','web_jack_01') or path.stem.startswith(('intro_jack','intro_tyler','intro_ethan')) else .1): raise ValueError('Implausible metres: '+obj.name)
        if path.stem.endswith('hero_01'):
            if not obj.data.shape_keys or len(obj.data.shape_keys.key_blocks)<3: raise ValueError('Missing Dread shapes')
            shapes.extend(key.name for key in obj.data.shape_keys.key_blocks if key.name!='Basis')
        if not path.stem.endswith('hero_01') and obj.data.shape_keys: shapes.extend(key.name for key in obj.data.shape_keys.key_blocks if key.name!='Basis')
        obj.data.calc_loop_triangles();triangles+=len(obj.data.loop_triangles)
        if path.stem=='zombie_wolf_01':
            bone_faces=[p for p in obj.data.polygons if obj.data.materials[p.material_index].name=='WolfBone']
            if bone_faces:
                uv=obj.data.uv_layers[0]
                points=[uv.data[index].uv for face in bone_faces for index in face.loop_indices]
                if any(max(p[axis] for p in points)-min(p[axis] for p in points)<.25 for axis in (0,1)):
                    raise ValueError('Wolf skull needs nondegenerate primary UVs; joined source UV names must match')
        level=obj.name.rsplit('_LOD',1)[-1] if '_LOD' in obj.name else '0'
        lods['LOD'+level]=lods.get('LOD'+level,0)+len(obj.data.loop_triangles)
        materials.update(mat.name for mat in obj.data.materials if mat)
        obj.hide_render=False
    target=out/(path.stem+'.fbx')
    animated=path.stem in ('walker_01','zombie_wolf_01','hanging_survivor_01','web_jack_01','cinder_head_01')
    rigs=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    if animated and (len(rigs)!=1 or len(rigs[0].data.bones)<28):raise ValueError('Crawler requires a weighted 28-bone rig')
    if not args.manifest_only:
        bpy.ops.export_scene.fbx(filepath=str(target),object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=False,add_leaf_bones=False,use_armature_deform_only=True,bake_anim=animated,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
    records.append({'id':path.stem,'source':path.relative_to(root).as_posix(),'export':target.relative_to(root).as_posix(),'units':'metres','triangles':lods.get('LOD0',triangles),'lodTriangles':lods,'materials':sorted(materials),'textures':['Assets/_Game/Art/Textures/'+name+'_'+suffix+'.png' for name in sorted(materials) for suffix in ('BaseColor','Normal','Mask','Anatomy') if (root/'Assets/_Game/Art/Textures'/(name+'_'+suffix+'.png')).exists()],'collider':'separate simple Unity collider; decorative foliage has no collision','blendShapes':shapes,'animations':[action.name for action in bpy.data.actions] if animated else [],'bones':sum(len(r.data.bones) for r in rigs),'license':'original project model; original imagegen skin albedo' if animated else 'original procedural project asset','status':'reference-guided organic sculpt; human epidermis atlas and pore/roughness maps; weighted eight-limb crawler, in-place skeletal clips and jaw morph' if animated else 'original art pass'})
    if path.stem.startswith('forest_body_') or path.stem=='hanging_survivor_01':
        record=records[-1]
        record['license']='CC0 MakeHuman/MPFB graphics, system clothes and Mindfront/Jartur69 skin; project pose and optimization'
        record['status']='anatomical human mesh with original UV skin, fitted clothing, hair and eyes; posed dressed casualty; three LODs; Unity wound shader'
        if path.stem=='hanging_survivor_01':record['status']='clothed adult survivor; weighted human skeleton; separate suspended struggle, overpowering and dying clips; Unity chest-contact feeding choreography'
    if path.stem in ('blackpine_01','blackpine_02','moss_rock_01','moss_rock_02','moss_rock_03','fallen_log_01','scanned_stump_01'):
        records[-1]['license']='Poly Haven CC0; project metre scale and three optimized LODs'
        records[-1]['status']='photographed material maps with source UVs; see ArtSource/Vendor/polyhaven/sources.json'
    if path.stem=='zombie_wolf_01':
        records[-1]['license']='CC0 NewDLC wolf; project authored half-skull and idle/run/bite/death; original imagegen fur/bone albedos and Blender-baked normal maps'
        records[-1]['status']='36-bone CC0 wolf adaptation with smoother silhouette; original exposed half-skull, empty orbit, jaw-weighted mandible/canine teeth, generated fur and aged bone artwork, native Blender normal bakes and varied bone roughness/AO; four clips with bite closure at .60 seconds; Unity wounds'
    if path.stem=='cabin_sign_01':
        records[-1]['license']='CC0 Textured Medieval Sign by allinonemovie; project wall-plaque adaptation and mounting studs'
        records[-1]['status']='Wood panel, iron frame, source UVs and authored wood maps; 3m x 0.82m. Separate Hebrew/English yellow name-only lettering follows the runtime language.'
    if path.stem=='walker_01':
        records[-1]['textures'].append('Assets/_Game/Art/Textures/CrawlerMouth_Anatomy.png')
        records[-1]['license']='Original project crawler and rig; original imagegen human epidermis and mouth albedos'
        records[-1]['status']='Weighted 28-bone eight-limb crawler; retained skin atlas/clips/jaw morphs, 26 sharpened teeth with decayed enamel, wounded lips/gums and dark mucosa maps; explicit primary oral UVs'
    if path.stem=='victim_web_01':
        records[-1]['status']='Original 22-radial silk web, five sagging rings and tree/ground anchor strands; separate intact core and persistent torn rim; Unity bone-attached bindings release on impact'
    if path.stem in ('player_body_01','player_axe_hand_01','player_shotgun_hand_01','player_flashlight_hand_01'):
        records[-1]['license']='CC0 MakeHuman/MPFB anatomy, clothing and Mindfront skin; project pose and extraction'
        records[-1]['status']='Full clothed anatomical shadow body with grounded Crouch, four Walk and four Run leg/arm poses; displacement gait phase shared with footsteps; level capsule-height placement' if path.stem=='player_body_01' else 'Native weighted left flashlight/right axe/right shotgun upper arm, forearm, wrist and curled hand; fitted source sleeve; normalized arm-only weights and evaluated grip-frame bone export; runtime length-preserving two-bone IK without mesh shear. ArtSource/FirstPersonArms.json; reproduce with first_person_arms.py.'
        if path.stem!='player_body_01':
            records[-1]['textures']=['Assets/_Game/Art/Textures/'+key+'_'+suffix+'.png' for key in ('HumanSkin0','BodyFabric') for suffix in ('BaseColor','Normal','Mask')]
    if path.stem in ('cinder_head_01','eviscerated_casualty_01'):
        records[-1]['license']='CC0 MakeHuman/MPFB anatomy and Mindfront skin; original project adaptation, six-legged rig and exposed viscera'
        records[-1]['status']='Sunken asymmetric anatomical UV head, necrotic lips and pointed rotten teeth; six continuous muscle/tendon legs and hooked claws; 28 bones; idle/scuttle/bite/spit and 3.2-second grounded folding/side-roll death; dedicated human pore/roughness materials with bruised veins and Unity char/ash dissolution' if path.stem=='cinder_head_01' else 'Exact opening Ethan face/hair/clothes and UVs; opened abdomen, continuous cylindrical-UV intestinal coils and ground spill; generated capillary-rich serosa albedo with wet material. ArtSource/EncounterCast.json and ArtSource/Textures/IntestinalSerosa_Imagegen.json.'
        for key in (['HumanSkin0','CrawlerSkin','CrawlerGums','CrawlerTeeth','CrawlerThroat','WolfBone','HumanEyes'] if path.stem=='cinder_head_01' else ['HumanSkin0','CrawlerSkin','CrawlerGums','HumanOutfit2','HumanEyes']):
            for suffix in ['BaseColor','Normal','Mask']:
                texture='Assets/_Game/Art/Textures/'+key+'_'+suffix+'.png'
                if (root/texture).exists() and texture not in records[-1]['textures']:records[-1]['textures'].append(texture)
    if path.stem=='burning_car_01':
        records[-1]['license']='CC0 Creomoto Car from OpenGameArt; project wreck adaptation'
        records[-1]['status']='Metre-scale crushed/charred CC0 vehicle; source retained in ArtSource/Vendor/creomoto'
    if path.stem.startswith(('intro_jack','intro_tyler','intro_ethan')):
        records[-1]['license']='CC0 MakeHuman/MPFB anatomy, clothes and Mindfront/Jartur69 skin; imagegen healthy skin derivative; project seated rest rig'
        records[-1]['status']='Living seated hunter with matching forest-corpse age/build/clothes: Jack=body02, Tyler=body01, Ethan=body03; distinct CC0 short02/short03/short04 hair and authored nose/lip/jaw targets, clean skin/iris maps, head/spine/arm acting and supplied English dialogue VO.'
    if path.stem.startswith('arrival_'):
        records[-1]['license']='Retained CC0 MakeHuman passenger anatomy/clothes/hair and healthy skin derivative; exact living mesh reused in a fallen pose'
        records[-1]['status']='Exact Jack/Tyler/Ethan passenger face, hair, clothing and UVs, posed on the ground with three distance LODs and corpse/blood shading; cast provenance ArtSource/ArrivalCast.json'
    if path.stem=='web_jack_01':
        records[-1]['license']='Retained CC0 MakeHuman opening Jack anatomy, clothing, hair and Jartur69 skin; project pose/retargeting and existing three encounter clips'
        records[-1]['status']='Exact opening Jack as the suspended victim; aligned standing rest rig with quaternion bone animation for Hanging/Overpowered/Dying; Unity bone-attached silk, wounds and physical falling/feeding choreography; ArtSource/EncounterCast.json'
    if path.stem=='intro_driver_01':
        records[-1]['license']='CC0 MakeHuman/MPFB anatomy, clothes and retained healthy IntroSkin0 derivative; project driver bind pose and skeletal wheel IK'
        records[-1]['status']='Full seated first-person torso, field jacket sleeves, trousers, articulated legs and boots; own head excluded; independent native left/right palms with mirrored wrist frames, inner-rim opposed thumbs and length-preserving finger curls; skeletal elbows/wrists follow the wheel without rigid-mesh forearm stretching. Anatomical pose receipt: ArtSource/IntroDriverPose.json.'
    if path.stem in ('intro_vehicle_01','intro_wheel_01'):
        records[-1]['status']='Original metre-scale 4x4 interior or separate steering rim; explicit UVs, retained project leather/fabric maps and procedural PBR grain/weave relief; distinct dashboard, door, seat, ribbed floor mat and wheel surfaces.'
        records[-1]['textures']=['Assets/_Game/Art/Textures/'+key+'_'+suffix+'.png' for key in ('BodyLeather','BodyFabric') for suffix in ('BaseColor','Normal','Mask')]
(root/'Docs').mkdir(exist_ok=True)
(root/'Docs'/'AssetManifest.json').write_text(json.dumps({'seed':1407,'assets':sorted(records,key=lambda record:record['id']),'otherAssets':'Unity interaction/collision, blood shaders and overlays; original SFX, CC0 footsteps/voices, and twelve supplied Runway opening dialogue takes plus four supplied effects. See ThirdPartyNotices.md and ArtSource/Audio/Runway.'},indent=2),encoding='utf-8')
print('EXPORTED',len(paths),'validated assets; manifest contains',len(records),'project assets')
