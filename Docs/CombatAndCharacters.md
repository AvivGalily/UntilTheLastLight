# Recurring encounters, characters and melee — 0.5.0

The latest requested rules supersede the original GDD's invulnerable first encounter and point-blank flashlight cancellation. The flashlight repels the Walker outside a tunable **2.8 m horizontal radius**. Inside it, the beam no longer cancels a strike. Walls and powered amber shelters still block attacks. A warned, completed Walker strike kills immediately; wolf bites follow the existing wound / second-hit death rule and invulnerability interval.

`MonsterEncounters` schedules distant returns after 32–52 active seconds (34 for the first return). Placement requires a complete NavMesh path, an unprotected point at least 30 m away, six seconds out of sight, and no nearby pursuit. It never repositions a visible or close Walker. The Walker receives a snapshot of the player's position, then uses normal movement/senses. Returns may be delayed by visibility or shelter conditions.

## Characters and weapon

Four MakeHuman casualties replace the earlier procedural sculpt. They retain UV skin, fitted textured clothing, hair/eyes, relaxed limbs and three LODs. Two object-space wound fields add irregular bruising, blood and wet highlights to skin and clothes. Ground pools sit beneath bodies. Original notes, IDs and the surveyor's map remain discoverable.

Five undead wolves occupy the trails. Each uses a weighted NewDLC CC0 model, adapted fur, wounds and four Blender-authored clips. Wolves follow clear sightings, navigate around solids, stay out of shelters, telegraph bites, stagger on impact and die after two axe strikes. Dead wolves remain in the world; their identities are recorded at checkpoints.

The axe is beside the surveyor, the closest corpse to the starting cabin. **E** takes it; **left mouse** swings. A swing has windup, one damage moment and recovery; only the nearest living wolf within 2.65 m, the forward arc and an unobstructed line can be hit. The axe does not damage the Walker. The flashlight remains available in the other hand.

The Walker retains its human-skin atlas, eight-limb skeleton, grounded feet and opening jaw. Localized blood and restrained asymmetric head movement supplement that rig.

## Sound, damage and saves

Footsteps use actual controller displacement, grounded state and indoor/outdoor routing. Running increases cadence and volume; crouching is quieter. Breathing blends by exertion, stamina and injury, with gradual recovery. Recorded human scream/grunt and ground steps are CC0; breathing and combat effects are synthesized.

Walker clicks require actual movement. Proximity tension and creature breathing remain independent. Menus pause simulation and ordinary audio. The fatal scream uses `ignoreListenerPause` set at runtime; blood on the lens remains over the death menu. Continue restores the checkpoint; New Game resets the axe, wolves and blood.

Save schema 3 adds the axe and dead-wolf IDs. Versions 1/2 migrate without granting either. Backup recovery remains. Combat progress is checkpoint-based, like batteries and quest progress.

## Reproduce

With Unity closed, download sources using `python Tools/fetch_character_sources.py`. Run Blender in background with `--disable-autoexec --python Tools/Blender/licensed_humans.py`, then `--python Tools/Blender/combat_assets.py`. Run `python Tools/generate_combat_audio.py` and `python Tools/update_combat_localization.py`. Then run `Tools/Invoke-Unity.ps1` tasks Generate, Capture, Smoke, EditTests, PlayTests and Build in sequence.

Vendor inputs are retained/downloadable. Credits/licenses are in [ThirdPartyNotices.md](ThirdPartyNotices.md). Blender studio previews are under `Screenshots/Combat`; forest/body captures are actual Unity URP renders. The complete campaign and final cinematics remain outside this slice.

## Version 0.7 wolf and bite presentation

The axe points its cutting edge toward the camera's forward direction and swings in that plane. The wolf's full one-second bite opens during draw-back, closes at the 0.60-second damage moment and recovers. Movement out of range, occlusion and shelter can make a bite miss. A distant clear sighting causes a spatial howl with a short hesitation, followed by nearby growls; unseen time permits another encounter, with a howl cooldown.

The CC0 wolf retains its skeleton and UVs, with a smoother silhouette and original half-skull weighted to head/jaw. Fur uses an imagegen atlas (`WolfSkin_Realistic.png`, native 1254×1254) and a Blender-baked 2048×2048 tangent normal. Unity preserves atlas dimensions, uses mipmaps/high-quality compression and anisotropic filtering, and samples eye artwork rather than a flat color. The albedo is generated artwork, not a scan. Save schema remains 4. `generate_combat_audio.py` also rebuilds the additional approach howl and retrieves the credited CC0 growl.

Version 0.7.1 adds aged bone artwork to the exposed skull, mandible and teeth. Fine pores, restrained cracks, grime and dried blood use a native 1254×1254 albedo, a 2048×2048 Blender-baked tangent normal and varied AO/smoothness. Native texture dimensions, mipmaps and high-quality compression also apply to bone. The UVs and jaw weighting stay connected while biting. `refresh_wolf_bone.py` updates just this material; reproduction and the retained generation prompt are documented in `ArtSource/Textures/README.md`.

## Version 0.8 pursuit and voices

Approaching wolves bark on a separate spatial source, after a distant warning howl. Gain increases smoothly from distance, with a quicker cadence nearby and small pitch variations. Distance attenuation is applied once, through that gain; the source retains directional panning. Barking stops when movement stops, a bite starts, the wolf is staggered/dead or the player is sheltered. Ordinary menu audio pause also freezes playback.

The Walker remembers the player's current feet position throughout active forest pursuit, including without sight/hearing. It replans through the existing navigation/physical checks and does not drop into patrol or abandon the chase on its former hunt timeout. Shelters, beam repulsion, power-loss grace, attack warning and restore grace remain active.

After the first 34 active seconds, the director can disappear a creature that has been out of view for three seconds and remains at least 14 m away. Its object, attacks and voice deactivate for 3–6 seconds. It returns from a complete, unprotected NavMesh path at least 30 m away and outside the camera. Further opportunities are spaced 28–46 active seconds apart and can be deferred by close/visible pursuits or shelter. It cannot interrupt the authored ambush/escape. Checkpoint restoration and New Game reset this transient sequence; schema remains 4.

The Walker has a return roar and recurring guttural voices that grow louder nearby. Its voice stops while absent/indoors and yields to the explicit attack windup cue. New clips are processed CC0 recordings with original layers; source links and reproduction are in `ThirdPartyNotices.md`.
