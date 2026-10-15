# Reference crawler — Blender and Unity

## Oral surfaces — 0.9.0

The mouth now uses original imagegen decayed-enamel, wounded lip/gum and dark-mucosa artwork, stored as one native 1254px source atlas and three 1254×418 runtime crops. Native normal/AO/smoothness maps add fine detail. The lip tissue is also feathered into a separate 4096px anatomy atlas so the exposed outer lip has bruised, scarred color that follows the skin and jaw. The original human-skin source and atlas remain retained. The 26 teeth have pointed tips and independent enamel UVs; all 28 bones, skin weights, idle/crawl clips and both jaw shapes remain. Bite closure preserves each tooth's shape and avoids the previous folding in the cheeks.

Reproduce from checked-in sources with `python Tools/Assets/prepare_crawler_mouth.py`, then Blender `crawler_mouth_material.py -- --project C:\dev\UntilTheLastLight` and the focused `export_assets.py -- --project C:\dev\UntilTheLastLight --asset walker_01`. The full art wrapper includes mouth preparation/application. New `07-decayed-mouth-open.png` and `08-decayed-mouth-bite.png` are actual Unity close views, alongside the refreshed model views and crawl frames. See `ThirdPartyNotices.md` for the exact source/prompt paths.

The user-supplied image guides the pale humanoid anatomy, horizontal torso, long bent limbs, eyeless head and large tooth-filled mouth. The model, textures and animation are authored in this project; the reference image is not embedded or redistributed. This is a stylized game model based on that reference.

- Source: `ArtSource/Blender/walker_01.blend`. Select `CrawlerRig` in Blender's Pose Mode; select `Crawler_Crawl` or `Crawler_Idle` in the Action Editor. NLA tracks are muted to keep the sculpt in its rest pose when opening the file.
- Export: `Assets/_Game/Art/Meshes/walker_01.fbx`. A Generic skeleton with 28 bones drives one weighted skin; each of the eight limbs has upper, lower and foot bones. The two front limbs end in five-fingered human hands.
- The sculpt uses fused flesh, bony elbows, shoulders, ribs, tendons and a carved mouth recess. Subdivision and relaxed vertex weights smooth the surface and joint transitions. The jaw morph also moves gums, irregular teeth, tongue and saliva.
- Skin uses an original imagegen human epidermis albedo, with natural pigment, diffuse capillaries and fine pores. A periodic boundary correction removes tiling jumps. Rest-space box projection is baked into a **4096px anatomy atlas** on a second UV channel; this keeps color continuous across the sculpt's UV seams and attached to the animated body. The retained source is 1254px, not a 4K photo scan; the 4K atlas describes the entire body. Source and exact generation prompt: `ArtSource/Textures/README.md`.
- Fine pore normals and a linear metal/AO/diffusion/smoothness mask are generated at 2048px from the epidermal detail. Broad pigment and veins are filtered out of the normal, so they are not raised ridges. Variable roughness and skin dielectric reflectance (0.028, approximately IOR 1.4) replace the former waxy finish. Blender uses the same maps with a Principled skin material and subtle subsurface scattering. Unity uses bounded warm light diffusion, with real shadow/distance attenuation, rather than a volumetric scattering simulation. Wet mouth tissue and ivory teeth retain separate materials. Bright flashlight highlights roll off before ACES to retain readable flesh at close range. Sculpt authoring seed: 7219.
- `Crawler_Crawl` is a 1.2-second in-place loop with alternating four-limb support, a planted stance and lifted recovery. `Crawler_Idle` is a three-second breathing loop. Their root stays fixed; gameplay navigation moves the monster.
- `MonsterPresentation` evaluates the clips in the ordered simulation and blends idle/crawl by actual movement. An analytic two-bone solver plants hands and feet in world space, adjusts ground height, and retracts feet against walls. Zero simulation time freezes the pose and mouth.
- The existing proximity-driven jaw, attack windup, light protection and audio remain connected. The navigation/light capsule height now matches the low creature's 1.95m silhouette.

Regenerate the creature alone:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python Tools/Blender/reference_creature.py -- --project C:\dev\UntilTheLastLight
./Tools/Invoke-Unity.ps1 -Task Generate
./Tools/Invoke-Unity.ps1 -Task CreatureCapture
```

The full `Invoke-Blender.ps1` workflow also includes this builder. The exporter records the actual triangle count, bones, morph and clips in `AssetManifest.json`. Capture produces actual Unity URP views and 24 sampled crawl frames under `Screenshots/Creature`. Skeletal attack, recoil and death clips remain future campaign work.

Refresh skin only, keeping the existing geometry, rig, jaw and clips:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python Tools/Blender/skin_material.py -- --project C:\dev\UntilTheLastLight
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python Tools/Blender/export_assets.py -- --project C:\dev\UntilTheLastLight --asset walker_01
./Tools/Invoke-Unity.ps1 -Task Generate
./Tools/Invoke-Unity.ps1 -Task CreatureCapture
python Tools/Diagnostics/create_creature_preview.py
```

The focused skin refresh is deterministic from the checked-in source and needs
no image generation service or key. `05-human-skin-detail.png` and
`06-forearm-skin.png` add close material views to the actual Unity captures.
