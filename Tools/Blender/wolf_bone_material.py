"""Bake retained bone artwork into a reproducible wolf PBR material in Blender."""
from pathlib import Path
import shutil
import bpy
import numpy as np


def apply_bone_surface(material, root):
    root = Path(root)
    source = root / 'ArtSource/Textures/WolfBone_Realistic.png'
    if not source.exists():
        return False
    textures = root / 'Assets/_Game/Art/Textures'
    shutil.copyfile(source, textures / 'WolfBone_BaseColor.png')
    albedo = bpy.data.images.load(str(textures / 'WolfBone_BaseColor.png'), check_existing=False)
    albedo.colorspace_settings.name = 'sRGB'
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    surface = nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .8
    links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    color = nodes.new('ShaderNodeTexImage'); color.image = albedo
    links.new(color.outputs['Color'], surface.inputs['Base Color'])
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .6
    bump.inputs['Distance'].default_value = .00018
    links.new(color.outputs['Color'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], surface.inputs['Normal'])

    # Bake tangent-space microrelief on a UV tile with a physical 22 cm scale.
    # This produces a reusable normal field without rebaking overlapping skull UVs.
    selected = list(bpy.context.selected_objects)
    active = bpy.context.view_layer.objects.active
    scene = bpy.context.scene
    engine, samples, margin = scene.render.engine, scene.cycles.samples, scene.render.bake.margin
    plane = None
    normal = bpy.data.images.new('WolfBone baked pore normal', 2048, 2048, alpha=False)
    normal.colorspace_settings.name = 'Non-Color'
    target = nodes.new('ShaderNodeTexImage'); target.image = normal; nodes.active = target
    try:
        bpy.ops.object.select_all(action='DESELECT')
        bpy.ops.mesh.primitive_plane_add(size=.22)
        plane = bpy.context.object; plane.name = 'Temporary bone material bake tile'
        plane.data.materials.append(material)
        scene.render.engine = 'CYCLES'; scene.cycles.samples = 1; scene.render.bake.margin = 0
        bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', use_selected_to_active=False, use_clear=True)
        normal.filepath_raw = str(textures / 'WolfBone_Normal.png')
        normal.file_format = 'PNG'; normal.save()
    finally:
        if plane:
            data = plane.data
            bpy.data.objects.remove(plane, do_unlink=True)
            bpy.data.meshes.remove(data)
        scene.render.engine = engine; scene.cycles.samples = samples; scene.render.bake.margin = margin
        bpy.ops.object.select_all(action='DESELECT')
        for obj in selected:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = active
        nodes.remove(target)

    # Pack a linear metal/AO/thickness/smoothness mask from surface variation.
    # The albedo itself remains the exact retained generated image.
    raw = np.empty(len(albedo.pixels), dtype=np.float32); albedo.pixels.foreach_get(raw)
    pixels = raw.reshape((albedo.size[1], albedo.size[0], 4))
    luma = pixels[:, :, :3] @ np.array([.2126, .7152, .0722])
    mask_pixels = np.ones_like(pixels)
    mask_pixels[:, :, 0] = 0
    mask_pixels[:, :, 1] = .85 + .15 * luma
    mask_pixels[:, :, 2] = .5
    mask_pixels[:, :, 3] = .10 + .13 * (1 - luma)
    mask = bpy.data.images.new('WolfBone linear surface mask', albedo.size[0], albedo.size[1], alpha=True)
    mask.colorspace_settings.name = 'Non-Color'
    mask.pixels.foreach_set(mask_pixels.astype(np.float32).ravel())
    mask.filepath_raw = str(textures / 'WolfBone_Mask.png'); mask.file_format = 'PNG'; mask.save()
    tex = nodes.new('ShaderNodeTexImage'); tex.image = mask
    roughness = nodes.new('ShaderNodeMath'); roughness.operation = 'SUBTRACT'; roughness.inputs[0].default_value = 1
    links.new(tex.outputs['Alpha'], roughness.inputs[1]); links.new(roughness.outputs[0], surface.inputs['Roughness'])
    tex = nodes.new('ShaderNodeTexImage'); tex.image = normal
    mapped = nodes.new('ShaderNodeNormalMap'); mapped.inputs['Strength'].default_value = .6
    links.new(tex.outputs['Color'], mapped.inputs['Color']); links.new(mapped.outputs['Normal'], surface.inputs['Normal'])
    print('WOLF BONE SURFACE: retained albedo', tuple(albedo.size), 'native Blender normal', tuple(normal.size), 'linear roughness/AO mask', flush=True)
    return True
