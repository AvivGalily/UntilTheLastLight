"""Human epidermis PBR maps and seam-free rest-pose skin color atlas.

Run in Blender with --project to refresh the existing creature without rebuilding
its geometry, weights, morph or actions. Also called by reference_creature.py.
The source albedo is an original imagegen surface texture, not a person's photo.
"""
import argparse
import math
import sys
from pathlib import Path
import bpy
import numpy as np


def save_map(folder, name, data):
    # Store exact channel values. Color arrays are explicitly sRGB encoded;
    # normals and the packed mask remain linear. Avoid a second gamma encoding.
    image = bpy.data.images.new(name, data.shape[1], data.shape[0], alpha=True)
    image.colorspace_settings.name = 'Non-Color'
    image.pixels.foreach_set(np.clip(data, 0, 1).astype(np.float32).ravel())
    image.filepath_raw = str(folder / (name + '.png'))
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def periodic(image):
    """Periodic-plus-smooth decomposition removes opposite-edge color jumps.

    Only the smooth boundary component is removed; pores/pigment stay intact.
    This is preferable to mirroring the patch, which repeats obvious veins.
    """
    h, w = image.shape[:2]
    edge = np.zeros_like(image)
    edge[0] = image[-1] - image[0]
    edge[-1] = -edge[0]
    edge[:, 0] += image[:, -1] - image[:, 0]
    edge[:, -1] -= image[:, -1] - image[:, 0]
    denominator = (2 * np.cos(math.tau * np.arange(h) / h)[:, None]
                   + 2 * np.cos(math.tau * np.arange(w) / w)[None, :] - 4)
    denominator[0, 0] = 1
    spectrum = np.fft.fft2(edge, axes=(0, 1)) / denominator[..., None]
    spectrum[0, 0] = 0
    return (image - np.fft.ifft2(spectrum, axes=(0, 1)).real).astype(np.float32)


def blur(image, sigma):
    h, w = image.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.rfftfreq(w)[None, :]
    kernel = np.exp(-2 * math.pi ** 2 * sigma ** 2 * (fx * fx + fy * fy))
    return np.fft.irfft2(np.fft.rfft2(image) * kernel, s=(h, w)).astype(np.float32)


def maps(root):
    folder = root / 'Assets/_Game/Art/Textures'
    image = bpy.data.images.load(str(root / 'ArtSource/Textures/CrawlerSkin_AlbedoSource.png'), check_existing=False)
    image.colorspace_settings.name = 'Non-Color'
    image.scale(2048, 2048)
    rgb = np.asarray(image.pixels[:], dtype=np.float32).reshape(2048, 2048, 4)[..., :3]
    bpy.data.images.remove(image)
    before = float(np.abs(rgb[0] - rgb[-1]).mean() + np.abs(rgb[:, 0] - rgb[:, -1]).mean())
    rgb = np.clip(periodic(rgb), .04, .94)
    # A restrained cold pallor; preserve the source's red pigment and flesh hue.
    rgb = rgb * np.array([.97, .99, 1.01], dtype=np.float32)
    rgba = np.ones((2048, 2048, 4), dtype=np.float32)
    rgba[..., :3] = rgb
    save_map(folder, 'CrawlerSkin_BaseColor', rgba)
    luminance = rgb @ np.array([.2126, .7152, .0722], dtype=np.float32)
    # Remove broad pigment/veins from relief. Only epidermal creases and pores
    # affect the normal, so blood vessels do not become raised dark cables.
    micro = luminance - blur(luminance, 5.5)
    height = blur(micro, .7)
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * 1.8
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * 1.8
    length = np.sqrt(1 + dx * dx + dy * dy)
    normal = np.ones_like(rgba)
    normal[..., 0] = .5 - dx / length * .5
    normal[..., 1] = .5 - dy / length * .5
    normal[..., 2] = .5 + .5 / length
    save_map(folder, 'CrawlerSkin_Normal', normal)
    oil = np.clip((blur(luminance, 90) - luminance.mean()) * 3 + .5, 0, 1)
    mask = np.ones_like(rgba)
    mask[..., 0] = 0  # Never metallic.
    mask[..., 1] = np.clip(.99 - np.maximum(-micro, 0) * .8, .92, 1)
    mask[..., 2] = .35 + oil * .35  # Thin-skin scatter, not emissive light.
    mask[..., 3] = np.clip(.30 + oil * .16 + micro * .55, .25, .52)
    save_map(folder, 'CrawlerSkin_Mask', mask)
    print('SKIN MAPS: 2048px; boundary error', before, '->',
          float(np.abs(rgb[0] - rgb[-1]).mean() + np.abs(rgb[:, 0] - rgb[:, -1]).mean()), flush=True)


def connect(root, material):
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    links = material.node_tree.links
    bs = nodes.new('ShaderNodeBsdfPrincipled')
    bs.inputs['IOR'].default_value = 1.4
    bs.inputs['Subsurface Weight'].default_value = .10
    bs.inputs['Subsurface Radius'].default_value = (1, .38, .20)
    bs.inputs['Subsurface Scale'].default_value = .012
    out = nodes.new('ShaderNodeOutputMaterial')
    links.new(bs.outputs['BSDF'], out.inputs['Surface'])
    atlas_uv = nodes.new('ShaderNodeUVMap'); atlas_uv.uv_map = 'SkinAtlas'
    detail_uv = nodes.new('ShaderNodeUVMap'); detail_uv.uv_map = 'UVMap'
    for suffix in ('Anatomy', 'Normal', 'Mask'):
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(str(root / 'Assets/_Game/Art/Textures' / ('CrawlerSkin_' + suffix + '.png')), check_existing=False)
        tex.image.filepath = '//../../Assets/_Game/Art/Textures/CrawlerSkin_' + suffix + '.png'
        links.new((atlas_uv if suffix == 'Anatomy' else detail_uv).outputs['UV'], tex.inputs['Vector'])
        if suffix == 'Anatomy':
            links.new(tex.outputs['Color'], bs.inputs['Base Color'])
        elif suffix == 'Normal':
            tex.image.colorspace_settings.name = 'Non-Color'
            normal = nodes.new('ShaderNodeNormalMap'); normal.uv_map = 'UVMap'
            normal.inputs['Strength'].default_value = .65
            links.new(tex.outputs['Color'], normal.inputs['Color']); links.new(normal.outputs['Normal'], bs.inputs['Normal'])
        else:
            tex.image.colorspace_settings.name = 'Non-Color'
            roughness = nodes.new('ShaderNodeMath'); roughness.operation = 'SUBTRACT'
            roughness.inputs[0].default_value = 1
            links.new(tex.outputs['Alpha'], roughness.inputs[1]); links.new(roughness.outputs[0], bs.inputs['Roughness'])
    bs.location = (350, 100); out.location = (650, 100)
    material.diffuse_color = (.64, .43, .38, 1)


def apply(root, skin):
    maps(root)
    mesh = skin.data
    # Preserve the original micro-detail UVs; the second channel is a unique
    # 0..1 atlas. Anatomy color is projected in the unanimated object space,
    # then baked into the atlas so it follows the bones and jaw during motion.
    atlas = mesh.uv_layers.get('SkinAtlas')
    if atlas is None:
        atlas = mesh.uv_layers.new(name='SkinAtlas')
        for dst, src in zip(atlas.data, mesh.uv_layers[0].data):
            dst.uv = src.uv / 3
    mesh.uv_layers.active = atlas
    atlas.active_render = True
    bake = bpy.data.images.new('CrawlerSkin_Anatomy', 4096, 4096, alpha=True)
    bake.colorspace_settings.name = 'sRGB'
    folder = root / 'Assets/_Game/Art/Textures'
    bake.filepath_raw = str(folder / 'CrawlerSkin_Anatomy.png'); bake.file_format = 'PNG'
    mat = next(material for material in mesh.materials if material.name.split('.')[0] == 'CrawlerSkin')
    mat.use_nodes = True; nodes = mat.node_tree.nodes; nodes.clear(); links = mat.node_tree.links
    coord = nodes.new('ShaderNodeTexCoord')
    scale = nodes.new('ShaderNodeVectorMath'); scale.operation = 'SCALE'; scale.inputs['Scale'].default_value = 3.2
    links.new(coord.outputs['Object'], scale.inputs[0])
    albedo = nodes.new('ShaderNodeTexImage')
    albedo.image = bpy.data.images.load(str(folder / 'CrawlerSkin_BaseColor.png'), check_existing=False)
    albedo.projection = 'BOX'; albedo.projection_blend = .35
    links.new(scale.outputs['Vector'], albedo.inputs['Vector'])
    emit = nodes.new('ShaderNodeEmission'); links.new(albedo.outputs['Color'], emit.inputs['Color'])
    out = nodes.new('ShaderNodeOutputMaterial'); links.new(emit.outputs[0], out.inputs['Surface'])
    # Give every material a target, including the oral UV islands. Only the
    # skin slots consume this map at runtime; the mouth keeps its own materials.
    oral_target = bpy.data.images.new('Oral bake scratch', 16, 16, alpha=True)
    for material in mesh.materials:
        material.use_nodes = True
        target = material.node_tree.nodes.new('ShaderNodeTexImage')
        target.image = bake if material == mat else oral_target
        material.node_tree.nodes.active = target
    bpy.ops.object.select_all(action='DESELECT'); skin.select_set(True); bpy.context.view_layer.objects.active = skin
    scene = bpy.context.scene; scene.render.engine = 'CYCLES'; scene.cycles.samples = 1; scene.cycles.device = 'CPU'
    scene.render.bake.margin = 16; scene.render.bake.use_clear = True
    # Bake the sculpt's actual rest mesh, with no rig or jaw displacement.
    modifiers = [(mod, mod.show_render) for mod in skin.modifiers]
    for mod, enabled in modifiers: mod.show_render = False
    try:
        bpy.ops.object.bake(type='EMIT')
    finally:
        for mod, enabled in modifiers: mod.show_render = enabled
    bake.save()
    for material in mesh.materials:
        for node in list(material.node_tree.nodes):
            if node.type == 'TEX_IMAGE' and node.image in (bake, oral_target):
                material.node_tree.nodes.remove(node)
    bpy.data.images.remove(bake)
    bpy.data.images.remove(oral_target)
    connect(root, mat)
    mesh.uv_layers.active_index = 0
    mesh.uv_layers[0].active_render = True
    skin['skin_note'] = 'Human skin: imagegen albedo; 4K seam-free rest-space atlas; 2K micro normal + variable roughness; IOR 1.4, subtle subsurface.'
    print('HUMAN SKIN: 4096px anatomy atlas, 2048px pores/mask, two UV channels', flush=True)


def refresh(root):
    path = root / 'ArtSource/Blender/walker_01.blend'
    bpy.ops.wm.open_mainfile(filepath=str(path))
    bpy.context.preferences.filepaths.save_version = 0
    skin = bpy.data.objects['CrawlerAnatomy']
    apply(root, skin)
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    bpy.ops.export_scene.fbx(filepath=str(root / 'Assets/_Game/Art/Meshes/walker_01.fbx'),
        object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
        bake_space_transform=False, use_mesh_modifiers=False, add_leaf_bones=False,
        use_armature_deform_only=True, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(); parser.add_argument('--project', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    refresh(args.project.resolve())
