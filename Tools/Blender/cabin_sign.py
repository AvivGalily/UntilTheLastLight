"""Adapt a retained CC0 sign into a wall plaque; preserve its wood UVs/maps."""
import argparse,hashlib,json,sys
from pathlib import Path
import bpy
from mathutils import Vector

parser=argparse.ArgumentParser()
parser.add_argument('--project',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
root=args.project.resolve();vendor=root/'ArtSource/Vendor/wooden-sign-cc0'
source=vendor/'source';textures=root/'Assets/_Game/Art/Textures'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source/'medievalSign.fbx'))
panel=bpy.context.scene.objects.get('Circle')
if panel is None:raise ValueError('Missing source sign mesh')
bpy.ops.object.select_all(action='DESELECT');panel.select_set(True);bpy.context.view_layer.objects.active=panel
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
# Retain the wooden plaque/frame; remove the perpendicular shop bracket/hangers.
panel=max((o for o in bpy.context.scene.objects if o.type=='MESH' and any(o.data.materials[p.material_index].name.startswith('Wood') for p in o.data.polygons)),key=lambda o:len(o.data.polygons))
for ob in list(bpy.context.scene.objects):
    if ob!=panel:bpy.data.objects.remove(ob,do_unlink=True)
points=[panel.matrix_world@v.co for v in panel.data.vertices]
low=Vector([min(p[i] for p in points) for i in range(3)]);high=Vector([max(p[i] for p in points) for i in range(3)])
center=(low+high)*.5;extent=high-low
for v,p in zip(panel.data.vertices,points):v.co=Vector(((p.x-center.x)*3/extent.x,(p.y-center.y)*.10/extent.y,(p.z-center.z)*.82/extent.z))
panel.matrix_world.identity();panel.name='Weathered wooden cabin plaque'
wood_indices={i for i,m in enumerate(panel.data.materials) if m.name.startswith('Wood')}
for face in panel.data.polygons:face.material_index=0 if face.material_index in wood_indices else 1
panel.data.materials.clear()
wood=bpy.data.materials.new('CabinSignWood');wood.use_nodes=True
principled=wood.node_tree.nodes.get('Principled BSDF');principled.inputs['Roughness'].default_value=.78
for suffix,socket in [('BaseColor','Base Color'),('Normal',None)]:
    node=wood.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(textures/f'CabinSignWood_{suffix}.png'),check_existing=True)
    if socket:wood.node_tree.links.new(node.outputs['Color'],principled.inputs[socket])
    else:
        node.image.colorspace_settings.name='Non-Color';normal=wood.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.55
        wood.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);wood.node_tree.links.new(normal.outputs['Normal'],principled.inputs['Normal'])
iron=bpy.data.materials.new('CabinSignIron');iron.diffuse_color=(.07,.065,.05,1);iron.use_nodes=True
iron.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=iron.diffuse_color
iron.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.65
iron.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.72
panel.data.materials.append(wood);panel.data.materials.append(iron)
# Four small fasteners make the wall attachment visible; front is Blender -Y.
for x in (-1.12,1.12):
    for z in (-.23,.23):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,location=(x,-.052,z))
        nail=bpy.context.object;nail.name='Dark iron mounting stud';nail.scale=(.018,.008,.018);nail.data.materials.append(iron)
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        nail.data.uv_layers[0].name=panel.data.uv_layers[0].name
        for face in nail.data.polygons:face.use_smooth=True
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=panel;bpy.ops.object.join()
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
authoring=root/'ArtSource/Blender/cabin_sign_01.blend';authoring.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(authoring),compress=True)
record={'title':'Textured Medieval Sign','author':'allinonemovie','license':'CC0','page':'https://opengameart.org/content/textured-medieval-sign','download':'https://opengameart.org/sites/default/files/medievalSign.zip','sha256':hashlib.sha256((vendor/'medievalSign.zip').read_bytes()).hexdigest(),'adaptation':'Wall-mounted 3m x 0.82m plaque; bracket removed, source wood UVs preserved, original four mounting studs. Localized yellow lettering is attached in Unity.'}
(vendor/'source.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print('CABIN SIGN: retained CC0 panel/frame with original UVs; 3m x 0.82m, four mounting studs.')
