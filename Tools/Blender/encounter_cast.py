"""Exact opening Jack as the animated web victim and Ethan as the wreck casualty."""
import bpy,bmesh,math,sys,json
from pathlib import Path
from mathutils import Matrix,Vector
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
from encounter_assets import save,ellipsoid,tube

bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/'hanging_survivor_01.blend'),load_ui=False,use_scripts=False)
reference=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rotations={b.name:b.matrix_local.to_quaternion().copy() for b in reference.data.bones}

def standing(name):
    bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/('intro_'+name+'_01.blend')),load_ui=False,use_scripts=False)
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.animation_data_clear()
    for a in list(bpy.data.actions):bpy.data.actions.remove(a)
    def depth(b):
        n=0
        while b.parent:n+=1;b=b.parent
        return n
    for b in sorted(rig.pose.bones,key=depth):
        b.matrix=Matrix.Translation(b.head)@rotations[b.name].to_matrix().to_4x4();bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get()
    low=min((o.matrix_world@v.co).z for o in bpy.context.scene.objects if o.type=='MESH' for v in o.evaluated_get(deps).data.vertices)
    root=rig.pose.bones['root'];root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-low));bpy.context.view_layer.update()
    for ob in list(bpy.context.scene.objects):
        if ob.type!='MESH':continue
        for mod in list(ob.modifiers):
            if mod.type=='ARMATURE':H.activate(ob);bpy.ops.object.modifier_apply(modifier=mod.name)
        for i,m in enumerate(ob.data.materials):
            if m.name.startswith('IntroSkin'):
                key='ArrivalSkin'+m.name[len('IntroSkin'):];skin=bpy.data.materials.get(key)
                if not skin:skin=m.copy();skin.name=key
                ob.data.materials[i]=skin
    H.activate(rig);bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    for ob in bpy.context.scene.objects:
        if ob.type=='MESH':mod=ob.modifiers.new('Exact passenger weighted anatomy','ARMATURE');mod.object=rig
    return rig

rig=standing('jack');rig.name='JackWebRig';rig.animation_data_create()
for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'
with bpy.data.libraries.load(str(H.SOURCE/'hanging_survivor_01.blend'),link=False) as (source,target):target.actions=[n for n in source.actions if n.startswith('Victim_')]
rig.animation_data.action=bpy.data.actions['Victim_Hanging']
if rig.animation_data.action.slots:rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.render.fps=30;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61;bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
save('web_jack_01',True)

rig=standing('ethan');abdomen=(rig.pose.bones['spine01'].head+rig.pose.bones['spine02'].head)*.5
parts=[]
for ob in list(bpy.context.scene.objects):
    if ob.type!='MESH':continue
    for mod in list(ob.modifiers):
        if mod.type=='ARMATURE':H.activate(ob);bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.data.transform(ob.matrix_world);ob.parent=None;ob.matrix_world=Matrix.Identity(4)
    if any('Skin' in m.name or 'Outfit' in m.name for m in ob.data.materials):
        bm=bmesh.new();bm.from_mesh(ob.data)
        cut=[v for v in bm.verts if (v.co.x/.13)**2+((v.co.z-abdomen.z)/.17)**2<1 and v.co.y<-.01]
        bmesh.ops.delete(bm,geom=cut,context='VERTS');bm.to_mesh(ob.data);bm.free()
    parts.append(ob)
bpy.data.objects.remove(rig,do_unlink=True)
back=max(v.co.y for ob in parts for v in ob.data.vertices)
flesh=bpy.data.materials.new('IntestinalSerosa');flesh.use_nodes=True
node=flesh.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(H.TEX/'IntestinalSerosa_BaseColor.png'),check_existing=True)
bs=flesh.node_tree.nodes.get('Principled BSDF');flesh.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.27
dark=bpy.data.materials.new('CrawlerThroat');dark.diffuse_color=(.035,.003,.005,1)
parts.append(ellipsoid('Abdominal cavity',(0,-.04,abdomen.z),(.13,.033,.175),dark))
ring=[(.131*math.cos(a),-.085,abdomen.z+.172*math.sin(a)) for a in (n*math.tau/64 for n in range(65))]
parts.append(tube('Opened abdominal rim',ring,[.015]*len(ring),flesh,24))
points=[]
for i in range(240):
    t=i/239;fall=max(0,(t-.65)/.35)
    y=(-.105-.025*math.sin(t*math.pi*5))*(1-fall)+(back-.008)*fall
    points.append((.090*math.sin(t*math.pi*11)+.32*max(0,(t-.64)/.36),y,abdomen.z+.13-.26*t-.16*max(0,(t-.6)/.4)))
gut=tube('Continuous textured bowel and ground spill',points,[.018+.003*math.sin(i*.55) for i in range(len(points))],flesh,24);parts.append(gut)
# Continuous cylindrical UVs keep vessels flowing along the modeled tissue.
arc=[0]
for a,b in zip(points,points[1:]):arc.append(arc[-1]+(Vector(b)-Vector(a)).length)
layer=gut.data.uv_layers.active
for face in gut.data.polygons:
    ring=face.index//24;side=face.index%24
    coords=((side/24,arc[ring]/.24),((side+1)/24,arc[ring]/.24),((side+1)/24,arc[ring+1]/.24),(side/24,arc[ring+1]/.24))
    for loop,coord in zip(face.loop_indices,coords):layer.data[loop].uv=coord
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name='eviscerated_casualty_01'
ob.data.transform(Matrix.Rotation(-math.pi/2,4,'X'));low=min(v.co.z for v in ob.data.vertices);ob.data.transform(Matrix.Translation((0,0,.025-low)))
for p in ob.data.polygons:p.use_smooth=True
save('eviscerated_casualty_01',False)
(H.ROOT/'ArtSource/EncounterCast.json').write_text(json.dumps({'tyler':{'role':'first map-bearing corpse','source':'intro_tyler_01.blend','asset':'arrival_tyler_01'},'jack':{'role':'web victim','source':'intro_jack_01.blend','asset':'web_jack_01','animations':'retained Victim_Hanging/Overpowered/Dying on aligned rest bones'},'ethan':{'role':'burning-car casualty','source':'intro_ethan_01.blend','asset':'eviscerated_casualty_01','intestinesTexture':'IntestinalSerosa_BaseColor.png'}},indent=2)+'\n')
print('ENCOUNTER CAST: Tyler first corpse, exact Jack web rig, exact Ethan textured wreck casualty',flush=True)
