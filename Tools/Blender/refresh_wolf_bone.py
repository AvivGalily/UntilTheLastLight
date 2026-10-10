"""Refresh only the wolf material, preserving the existing mesh, weights and actions."""
import sys
from pathlib import Path
import bpy

sys.path.insert(0, str(Path(__file__).parent))
from wolf_bone_material import apply_bone_surface

root = Path(__file__).resolve().parents[2]
source = root / 'ArtSource/Blender/zombie_wolf_01.blend'
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
primary = mesh.data.uv_layers[0]
legacy = mesh.data.uv_layers.get('UVMap')
if legacy and legacy.name != primary.name:
    skull_slots = {i for i, mat in enumerate(mesh.data.materials) if mat.name in {'WolfBone', 'WolfSocket', 'WolfTissue'}}
    copied = 0
    for face in mesh.data.polygons:
        if face.material_index in skull_slots:
            for index in face.loop_indices:
                primary.data[index].uv = legacy.data[index].uv
                copied += 1
    mesh.data.uv_layers.remove(legacy)
    mesh.data.uv_layers.active_index = 0; primary.active_render = True
    print('WOLF UV FIX: migrated', copied, 'skull/tissue loops to', primary.name, '; original coat UVs retained', flush=True)
if not apply_bone_surface(bpy.data.materials['WolfBone'], root):
    raise ValueError('Missing retained WolfBone_Realistic.png')
bpy.ops.wm.save_as_mainfile(filepath=str(source), compress=True)
print('REFRESHED wolf bone material/primary UVs; geometry/rig/animation retained', flush=True)
