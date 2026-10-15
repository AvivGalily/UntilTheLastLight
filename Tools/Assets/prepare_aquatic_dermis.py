"""Technical PBR derivatives; the generated dermis colour original is preserved."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/_Game/Art/Textures'
key = 'LabAbyssDermisV2'
path = OUT / (key + '_BaseColor.png')
source = Image.open(path).convert('RGB')
rgb = np.asarray(source, dtype=np.float32) / 255
lum = rgb @ np.array([.2126, .7152, .0722], np.float32)
blur = np.asarray(source.convert('L').filter(ImageFilter.GaussianBlur(3)), dtype=np.float32) / 255
detail = lum - blur
# Fine, low-relief pores. Colour variation must not turn into deep stone grooves.
height = detail * .38
dx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
dy = np.roll(height, -1, 0) - np.roll(height, 1, 0)
length = np.sqrt(1 + dx * dx + dy * dy)
normal = np.stack([.5 - dx / length * .5, .5 - dy / length * .5, .5 + .5 / length], -1)
mask = np.zeros((*lum.shape, 4), np.float32)
mask[..., 1] = np.clip(.98 + detail * .10, .93, 1)
mask[..., 2] = .24  # bounded dermal thickness for the existing skin shader
mask[..., 3] = np.clip(.28 + detail * .34 + blur * .10, .22, .42)
for suffix, data in [('Normal', normal), ('Mask', mask)]:
    Image.fromarray(np.uint8(np.clip(data, 0, 1) * 255)).save(OUT / (key + '_' + suffix + '.png'))
record = {
    'mode': 'built-in image_gen', 'transparent_background': False,
    'prompt': 'Use case: photorealistic-natural. Asset type: final seamless 2048 x 2048 square albedo material for an original humanoid amphibian creature in a realistic science-fiction horror game. Primary request: a believable natural amphibian skin surface, photographed as a completely flat macro material swatch spanning about 30 centimetres of physical skin. Very fine irregular pores and flattened small pebbled scales in continuous fleshy dermis, extremely subtle branching blue-purple capillaries below the skin, uneven desaturated olive-gray and slate gray-brown mottling with subdued pale gray-pink flecks and healed fine scratches. Soft creases belong to supple living skin. Real organic variation at several scales, restrained colour, credible animal anatomy. Surface should feel moist but the colour map itself has perfectly even diffuse lighting without reflections, specular highlights or shadows. Edge to edge seamless material with no obvious repeat and no large bands. No face, head, eyes, limbs, creature silhouette or body, no raised circular armor plates, no metal, no stone, no fabric, no coral, no thick polygonal scales, no glowing details, no slime bubbles, no text, no label, no border or watermark. Texture is intended for real-time PBR and must retain the fine natural pores without exaggerated embossed grooves.',
    'asset': path.relative_to(ROOT).as_posix(),
    'original': 'ArtSource/Research/Materials/LabAbyssDermisV2_Imagegen.png',
    'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    'dimensions': list(source.size),
    'derivatives': 'Technical normal RGB; mask R nonmetal, G AO, B dermal thickness, A varied smoothness. Generated colour is unchanged.',
}
(ROOT / 'ArtSource/Research/Materials/DermisV2Imagegen.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
print('Aquatic dermis V2 PBR prepared:', source.size)
