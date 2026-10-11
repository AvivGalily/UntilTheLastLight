# Asset and tool provenance — 0.11.3

## Supplied forest opening audio — 2026-10-11

The active opening uses twelve English dialogue files and four effects supplied by the user, with the original `Forest_Opening_Audio_Manifest.json`. Original MP3s and that manifest are retained unchanged in `ArtSource/Audio/Runway`; it identifies Runway generation tasks and Chad (Tyler), Clint (Jack) and Brodie (Ethan). No new generation or download service was invoked to integrate them. Complete speech is decoded to mono 24 kHz / 16-bit PCM without trimming or speed/pitch changes. Effects retain complete tails; only the ambience receives a 150 ms loop overlap. These user-supplied recordings are not labelled CC0. Source/output hashes and measured processing are in `Docs/Audio/ForestOpening_VoiceManifest.json` and `Docs/Audio/ForestOpening_SuppliedAudio.json`. Runtime playback makes no synthesis/network calls. Existing credited tinnitus/breathing/wake foley supplements the supplied effects.

## Previous forest opening English neural dialogue (archived)

The previous twelve locally generated lines from [hexgrad/Kokoro-82M v1.0](https://huggingface.co/hexgrad/Kokoro-82M), whose model card specifies **Apache-2.0**, are archived in `ArtSource/Audio/LegacyIntro`, outside Unity assets. The stock voice packs were `am_michael` (Jack), `am_puck` (Tyler), and `am_fenrir` (Ethan), listed in the author's [voice catalogue](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md). Original project dialogue, per-line pacing/gain, signal processing and captions remain in `ArtSource/Audio/ForestIntroVoice.json` and `Tools/generate_intro_voices.py`. The archived `ForestOpening_VoiceManifest.json` records their source-model/voice-pack and clip hashes. They are no longer the active opening takes.

Authoring uses [kokoro-onnx](https://github.com/thewh1teagle/kokoro-onnx) 0.6.1 (MIT inference wrapper), ONNX Runtime CPU and its espeak-ng/phonemizer authoring dependencies. Model/voice downloads come from that project's `model-files-v1.0` release. Model weights, tools, eSpeak code and intermediate WAVs remain outside the Unity runtime/build in ignored `Logs`; the game ships the finished audio only and makes no synthesis/network calls. [Kokoro's model card](https://huggingface.co/hexgrad/Kokoro-82M) retains its training-data acknowledgements. This dialogue is generated speech and is not marked as a CC0 actor recording. English audio falls back under Hebrew subtitles until Hebrew takes are supplied.

## Player presence and fatal presentation

`player_body_01` and `player_axe_hand_01` are derived in Blender from the project's retained CC0 MakeHuman/MPFB graphics and clothing, using the already credited Mindfront skin. They preserve source skin/clothing UVs; the anatomy is posed/extracted by `Tools/Blender/player_presence.py`. The shadow body, grounded bent-leg crouch morph, shaft grip, body-connected first-person forearm deformation, defensive hand poses and runtime camera/contact choreography are project authored. The existing hand also supplies the mirrored flashlight grip. GPL authoring addon code remains outside the Unity game assets.

`wolf_motion.py` replaces the project's bite/death actions on the same credited CC0 NewDLC wolf, retaining its weighted anatomy, fur/bone maps and idle/run actions. The 1.3-second bite and 2.4-second death are original animation, not imported game clips.

`walker_devour.wav` layers the retained CC0 Ogrebane Ghost and HaelDB human yell with original processing. `flesh_tear.wav` and `fatal_sting.wav` are original deterministic synthesis. `Tools/generate_death_audio.py` reproduces all three mono clips locally at 22,050 Hz with a restrained 0.74 peak; underlying recording credits and source hashes are retained below. Runtime has no network or authoring-tool dependency.

Publisher footage from Amnesia: The Bunker and Resident Evil Village was inspected as visual reference; titles, primary links and observations are in `PlayerAndFatalPresentation.md`. Reference frames remain in `Logs`, outside Unity assets and the build. No assets or recordings from those games are shipped.

## Crawler oral surfaces and spider silk — original project art

`ArtSource/Textures/CrawlerMouth_Imagegen.png` is original fictional surface artwork generated with the built-in **image_gen** tool, at its native **1254×1254** resolution. The exact final prompt and source hash/crop manifest are retained in adjacent `.prompt.txt` and `.source.json` files. The three native **1254×418** bands supply decayed enamel, wounded lip/gum tissue and dark mucosa. `prepare_crawler_mouth.py` splits the atlas without upscaling; `crawler_mouth_material.py` derives separate normal/roughness/AO maps, maps the existing oral geometry and bakes a feathered lip edge into a new **4096px** rest-space skin atlas. The original epidermis atlas is retained. This is generated art and derived game maps, not a scanned mouth or an extracted game asset.

The original 26 weighted teeth were sharpened while retaining the rig, clip data and jaw shapes. `crawler_bite.py` uses a continuous mouth closure and moves teeth as rigid pieces to avoid folded cheek triangles. `ArtSource/Previous/walker_01-0.8.1.blend` preserves the preceding authoring asset.

`Tools/Blender/victim_web.py` authors the original 22-radial silk scaffold, five sagging rings and branch/trunk/ground attachment strands. The intact center and torn rim are separate meshes. Unity adds original bone-attached tethers and cocoon wraps; the impact releases them, and checkpoint restoration retains the torn web. Human sources remain the credited CC0 MakeHuman/MPFB assets. The victim's initial bruises, blood and cuts use the project injury shader, with chest/head attachment points and stronger bleeding during feeding.

## Wooden cabin name plaques — CC0

[Textured Medieval Sign](https://opengameart.org/content/textured-medieval-sign), **allinonemovie**, CC0. The original archive, FBX and wood color/normal/AO/specular maps are retained under `ArtSource/Vendor/wooden-sign-cc0`; `source.json` records the primary page, download URL, SHA-256 and adaptation. The project removes the perpendicular shop bracket, fits the panel and frame to a 3m × 0.82m wall plaque, preserves its source UVs and adds four original iron mounting studs. The 1024px source color/normal maps and a packed AO/smoothness mask drive the Unity material. Yellow, name-only Hebrew or English lettering is attached to the plaque and follows the language setting. No assets were extracted from another game.

`Tools/prepare_cabin_sign.py`, `Tools/Blender/cabin_sign.py` and the focused FBX exporter reproduce this asset from the retained archive. The complete Blender wrapper includes these steps. The game requires no asset download or Blender installation.

## Enemy voice recordings — CC0

- [Dog barking mono](https://opengameart.org/content/dog-barking-mono), Brandon Morris / HaelDB: the CC0 license option is used. `wolf_bark.wav` isolates a bark with attack/tail padding, downmixes/resamples and lowers its pitch slightly. It is an adapted recorded dog bark for the zombie wolf, not a field recording of a wolf.
- [Ghost](https://opengameart.org/content/ghost), Ogrebane, CC0: pitch-shifted and layered with the already credited HaelDB human grunt/yell and a restrained original low-frequency layer for `walker_growl.wav` and `walker_roar.wav`. These are designed creature effects.

Original downloads, primary URLs and SHA-256 hashes are retained in `ArtSource/Vendor/enemy-voices-cc0/sources.json`. `Tools/generate_enemy_audio.py` reproduces the three mono runtime WAV files locally; `generate_combat_audio.py` includes that step. The game makes no downloads.

## Wolf bone surface — original artwork

`ArtSource/Textures/WolfBone_Realistic.png` is original surface artwork created with the built-in **imagegen** tool, not a photographed scan or a downloaded game texture. Its prompt and previous bone texture are retained alongside it. The runtime albedo is an exact copy at its native 1254×1254 resolution. `Tools/Blender/wolf_bone_material.py` bakes a 2048×2048 tangent normal and derives a linear AO/smoothness mask for fine bone pores, restrained cracks, grime and dried blood. The material is applied to the original project-authored skull/mandible/teeth. The retained CC0 wolf body, fur artwork, skeleton and animations are unchanged by this surface refresh.

## Forest scans and photographed surfaces — CC0

Powered by [Poly Haven](https://polyhaven.com). The offline authoring downloader uses its public API with identification and verifies every file against the published MD5. The game makes no API calls. Asset license: [CC0](https://polyhaven.com/license).

- [Fir Tree 01](https://polyhaven.com/a/fir_tree_01): two adapted fir variants, scaled to 10 metres, using source UVs and alpha foliage. Game LOD budgets: 16,000 / 5,000 / 1,400 triangles.
- [Rock Moss Set 01](https://polyhaven.com/a/rock_moss_set_01): three scanned rocks, 8,000 / 2,400 / 650 triangle budgets.
- [Dead Tree Trunk](https://polyhaven.com/a/dead_tree_trunk) and [Tree Stump 02](https://polyhaven.com/a/tree_stump_02): forest debris and roost branches, 8,000 / 2,600 / 650 triangle budgets.
- [Pine Bark](https://polyhaven.com/a/pine_bark), [Forest Ground 04](https://polyhaven.com/a/forest_ground_04), [Rocky Trail](https://polyhaven.com/a/rocky_trail), [Wood Planks Dirt](https://polyhaven.com/a/wood_planks_dirt), [Rust Coarse 01](https://polyhaven.com/a/rust_coarse_01): 2K photographed color/normal/roughness/AO; Unity mask packing is project-authored.

Original metadata, download hashes and source links are retained under `ArtSource/Vendor/polyhaven`. `licensed_forest.py` performs the mesh adaptation. The high-resolution authoring library is not included in the Windows build.

The crow, feather fans, claws and old oak are original geometry from `encounter_assets.py`. Crow flight and tree wind are project-authored runtime animation. The suspended adult uses the same CC0 MakeHuman sources below and an original weighted struggle clip. The scene uses the existing original crawler, with a new pounce/brace/feed pose controller.

The help line is an **English synthetic voice**, generated locally using the installed Windows Microsoft David SAPI voice, with Hebrew/English subtitles. It is not a human actor recording or Hebrew dubbing. The impact scream uses the credited HaelDB recording below. Jump sting, pursuit score, heartbeat, feeding sounds, crow calls and wing sounds are original synthesis in `generate_encounter_audio.py`.

## Human characters — CC0

The four clothed casualties are derived from MakeHuman/MPFB anatomy, fitted system clothing, hair, eyes and skin assets. Their UVs are retained. Project-authored poses, LODs, wound rendering and floor blood are added by `Tools/Blender/licensed_humans.py` and Unity. The older procedural bodies have been replaced.

- [MakeHuman core graphics license](https://static.makehumancommunity.org/about/license.html): CC0.
- [MakeHuman system assets](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html): the **CC0 archive only**, not the separately offered AGPL archive. Credits in source material files include Data Collection AB, Joel Palmius and Jonas Hauquier.
- [Male skins 02](https://static.makehumancommunity.org/assets/assetpacks/skins02.html): **CC0 archive only**. Used skins: Mindfront's Aksel skin and Jartur69's Old Slavic Male skin. The latter includes beard detail. Runtime models are fully clothed.
- [MPFB v2.0.17](https://github.com/makehumancommunity/mpfb2/tree/v2.0.17): the authoring add-on code is GPLv3, its graphics assets are CC0 as specified in `LICENSE.ASSETS.md`. The add-on code is not bundled in the Windows game. Original license files remain in `ArtSource/Vendor/mpfb-2.0.17`.

## Wolf — CC0

[3D Wolf by NewDLC](https://opengameart.org/content/3d-wolf), released by its author under CC0. The downloaded `3dwolf_blend.zip` provides the mesh, 36-bone skeleton and texture maps. `Tools/Blender/combat_assets.py` adapts its scale/materials and authors four clips: idle, run, bite and death. The archive contains no animation clips. This is not a model extracted from a third-party commercial game.

## Recorded audio — CC0

- [Male Grunt/Yelling Sounds by HaelDB](https://opengameart.org/content/male-gruntyelling-sounds), using its offered **CC0** license. `3yell13.wav` and `3grunt1.wav` supply the fatal scream and injury voice. They are trimmed, downmixed, resampled and normalized by `Tools/generate_combat_audio.py`.
- [Different steps on wood, stone, leaves, gravel and mud by TinyWorlds](https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud), CC0; based on pdsounds.org recordings and edited for Minetest. The game uses leaves01/02, gravel, mud02 and wood01/02/03 as Ogg files.

Download URLs, SHA-256 hashes and byte sizes are retained in `ArtSource/Vendor/*.source.json`. Original archives and upstream notices remain with the authoring inputs. [CC0 terms](https://creativecommons.org/publicdomain/zero/1.0/).

## Original project assets

The remaining forest models/PBR maps are original procedural assets from `Tools/Blender/forest_art.py`. The field axe is original geometry from `combat_assets.py`. The crawler is an original reference-guided model from `reference_creature.py`, with 28 bones, idle/crawl clips and a jaw morph. The supplied reference image is not redistributed in the game.

The crawler skin albedo was generated with imagegen. Its source and prompt remain in `ArtSource/Textures`; `skin_material.py` derives pore/roughness maps and the 4K anatomical atlas. This is not a stock scan or a photographed person.

Night wildlife, distant/approach howls, clicks, threat drone/pulse, breaths, axe effects and wolf bite/death voices are original synthesized audio. The approach growl uses [Wolf Monster Sound by CaveboyTup](https://opengameart.org/content/wolf-monster-sound), **CC0**, from the creator's published file. The creator made this fantasy sound from processed CC0 horse snorting; it is not a recording of a real wolf. Source URL and SHA-256 are in `ArtSource/Vendor/wolf-audio-cc0/source.json`. No film or commercial-game sounds are included. See `generate_audio.py`, `generate_night_audio.py`, `generate_combat_audio.py` and `generate_wolf_audio.py`.

The wolf's exposed half-skull, orbit, mandible, teeth and tissue are original geometry in `zombie_anatomy.py`, weighted to the CC0 NewDLC skeleton. The new fur albedo is an original imagegen UV-atlas edit in `ArtSource/Textures/WolfSkin_Realistic.png`, with its prompt alongside. `combat_assets.py` bakes strand relief into a tangent normal using native Blender tools. The albedo is generated artwork, not a photographed/scanned animal. No third-party zombie-wolf or skull model was imported.

Asset research considered the [Fab Zombie Wolf](https://www.fab.com/listings/3af4d914-b493-4005-97c0-8f7ea98a2513) (a paid anthropomorphic character rather than the required quadruped), [tylercwilson's wolf-skull scan](https://sketchfab.com/3d-models/wolf-skull-a2bc88557be8455fa0a46f8cd20d11cd), and tabletop STL options. The downloadable [NewDLC CC0 wolf](https://opengameart.org/content/3d-wolf) remains the licensed base. Horror presentation reference: [Dead Space lighting development talk by Motive](https://www.gdcvault.com/play/1029020/-Dead-Space-Harnessing-the). These research references supply no redistributed assets.

## Norton wreck and lightless heads — 0.11.0

The new vehicle adapts [Car by Creomoto](https://opengameart.org/content/car-0), licensed **CC0** by its creator. The original source and download SHA-256 are retained in `ArtSource/Vendor/creomoto`. MakeHuman/MPFB CC0 anatomy, clothing and Mindfront epidermis are reused for the dead human head, eviscerated adult casualty and shotgun hands. The six articulated legs, exposed intestines, shotgun, animation clips and Unity flame shader are original project work. Existing licensed PBR skin, fabric, wood and steel maps are reused; no new imagegen output is claimed for this release.

`head_introduction`, `head_growl`, `head_bite`, `head_spit` and `head_death` are original processed layers from the previously credited CC0 ghost recording. `shotgun_fire`, `shotgun_reload`, `shotgun_empty`, `wreck_fire` and `fireball_impact` are original deterministic synthesis. `Tools/generate_norton_audio.py` reproduces them. [Nightdive's official The Thing: Remastered page](https://nightdivestudios.com/the-thing-remastered/) supplies the head-spider design reference; its commercial assets are not included. See `NortonEncounter.md` for implementation and source details.

## Encounter art and score revision — 0.12.0

The revised Cinder head retains the credited CC0 human anatomy, epidermis, pore/roughness and oral/bone maps. Sunken facial contours, torn lips, pointed teeth, tendons, claws, grounded death animation and char/ash shader are original project adaptations. `head_score.wav` is an original seamless twelve-second oscillator composition; it uses no additional recording or sample license. Source and deterministic reproduction remain in `Tools/Blender/norton_assets.py` and `Tools/generate_norton_audio.py`.

## Engine, authoring tools and fonts

Unity Engine and packages retain their applicable Unity/package licenses. Unity runtime is included in the Windows build. [Blender](https://www.blender.org/about/license/) is a separate authoring tool and is not bundled.

TextMesh Pro includes Liberation Sans under SIL OFL 1.1. Hebrew uses [Noto Sans Hebrew](https://github.com/notofonts/noto-fonts/tree/main/hinted/ttf/NotoSansHebrew), also SIL OFL 1.1. Their notices are stored with the fonts and copied into the Windows build.

## Equipment hand revision — 0.13.0

Both first-person hand grips retain the credited CC0 MakeHuman/MPFB anatomical surface and Mindfront skin albedo/normal maps. Subdivision, a separate shotgun index pose, fitted rounded cuffs/sleeves, cloth folds and UVs are project-authored adaptations. Dedicated PlayerSkin and PlayerSleeve materials reuse the licensed human skin and existing project fabric maps. Authored smooth UV-seam normals are retained during body-connected arm deformation. No new downloaded character, generated image or recording is included in this revision.

Previous opening revision: the layered CC0 Ogrebane Ghost and HaelDB presence is superseded by the supplied creature effect. Its source/derived hashes and levels remain in `ArtSource/Audio/ForestIntroSFX.json`. Living hunters reuse the matching forest-body MPFB macro builds and system outfits; the clean Mindfront skin derivative was edited with the built-in imagegen tool (retained exact prompt and hashes in `ArtSource/Textures/IntroHealthySkin_Imagegen.*`). Jartur69 bearded skin is retained for Jack. Corpse albedos were not replaced.
