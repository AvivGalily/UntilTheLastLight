# Crawler human skin source

`CrawlerSkin_AlbedoSource.png` is an original surface texture generated with the
built-in **imagegen** tool on 2026-10-08. It is not a photographed person or a
downloaded stock scan. The supplied creature reference was used for anatomy in
the previous model pass; no pixels from that reference are used here.

Final generation prompt:

> Use case: photorealistic-natural
>
> Asset type: seamless square human skin base-color/albedo texture for a real-time 3D horror creature, not an illustration of a creature.
>
> Primary request: an extremely realistic macro surface scan of pale human skin, evenly covering the whole square edge to edge. A flat hairless skin patch about 18 cm across. Natural pale beige-pink flesh with subtle irregular reddish capillary mottling, tiny freckles and pigment speckles, faint diffuse blue-violet subdermal veins, real fine pores and delicate intersecting micro skin creases. Fine skin detail should be sharply resolved; color should be softly varied and believable, not uniform plastic. Veins must be faint and diffuse, never drawn dark lines. Healthy human epidermal structure with a slightly cold pallor appropriate to a faceless humanoid horror creature.
>
> Composition/framing: perfectly flat, orthographic, no body silhouette, no body parts, no edges, no background. Seamlessly tileable in both directions, consistent texture density.
>
> Lighting: color-calibrated diffuse cross-polarized surface scan, absolutely no baked directional light, gradients, shadows, gloss highlights, ambient occlusion, or vignette.
>
> Constraints: texture only, full square, opaque. No text, labels, watermark, eyes, mouth, hair, wounds, blood, scales, bark, marble, large wrinkle folds, or leathery cracked pattern. Render at the highest available square resolution.

`Tools/Blender/skin_material.py` removes opposite-edge color jumps, derives
subtle pore relief and a linear metal/AO/diffusion/smoothness mask, and bakes
rest-space box projection into a 4096px anatomy atlas. Generated files live in
`Assets/_Game/Art/Textures/CrawlerSkin_*`. The normal and mask are 2048px; the
source is retained at its native resolution. Re-running the tool never calls
the image service and does not require an API key.

## Wolf fur — 0.7.0

`WolfSkin_Realistic.png` is an original UV-atlas edit produced with the built-in **imagegen** tool on 2026-10-09. The exact prompt is retained in `WolfSkin_Realistic.prompt.txt`; the previous atlas is retained in `WolfSkin_Previous.png`. The edit preserves the CC0 wolf's UV layout and adds layered grey/taupe fur, a pebbled nose, textured eyes, dirt and restrained dried blood. It is generated artwork, not a photographed animal or a commercial model texture. The requested 2048px output returned at a native 1254×1254 resolution; that native size is preserved in Unity.

`Tools/Blender/combat_assets.py` copies this retained source to `WolfSkin_BaseColor.png`, bakes a 2048×2048 tangent normal in Blender and exports the rigged wolf. Rebuilding never calls the image service. Runtime maps are under `Assets/_Game/Art/Textures`; original skull geometry/material maps are authored in `Tools/Blender/zombie_anatomy.py`. Source and audio credits: `Docs/ThirdPartyNotices.md`.

## Wolf bone — 0.7.1

`WolfBone_Realistic.png` is original artwork generated with the built-in **imagegen** tool on 2026-10-09. `WolfBone_Realistic.prompt.txt` retains the exact prompt, and `WolfBone_Previous.png` preserves the earlier surface. The image is a flat material field of aged ivory bone with fine pores, cracks, soil and dried blood; it is not a scan. The requested 2048px image returned at 1254×1254, which is preserved in Unity.

`Tools/Blender/wolf_bone_material.py` copies the albedo unchanged, bakes a 2048×2048 tangent normal on a 22 cm UV tile and derives a linear metal/AO/thickness/smoothness mask. The source geometry, weights and all four animation clips are retained. A focused refresh uses Blender `--background --disable-autoexec --python-exit-code 1 --python Tools/Blender/refresh_wolf_bone.py`, followed by `Tools/Blender/export_assets.py -- --project C:/dev/UntilTheLastLight --asset zombie_wolf_01`. The complete `combat_assets.py` pipeline also applies the same retained material. No image service/API key is required to rebuild. Unity tasks Generate, Smoke and Build refresh the scenes, verification views and executable.

The focused refresh also migrates legacy skull `UVMap` coordinates into the body's `UVChannel_1` without changing the coat coordinates. New skull parts use that shared layer name before joining. This is required because Unity's material samples the primary UV channel; the previous appended skull had zero coordinates there. Blender export and Unity's imported-asset check guard against this regression.
