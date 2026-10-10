# Verification report — 2026-10-10

## Current release: player presence, pursuit handoff and fatal interactions — 0.10.0

- **49/49 EditMode and 13/13 PlayMode tests rerun and passed**, zero failures, against the final assets/code. New tests check the fatal state's disabled gameplay actions with ongoing cinematic time/audio, textured human body/grip geometry and the wolf's articulated non-looping collapse. `Logs/EditMode.xml` and `Logs/PlayMode.xml` record the final 2026-10-10 runs.
- Both generated scenes pass compilation, wiring and art validation, including the shadow body, actual grip and fatal voice/particles. The manifest contains **35 models**. The clothed shadow body has **32,984 triangles**; the smoothed anatomical grip has **15,408 triangles**. The wolf retains **35,015 triangles and 36 bones**, its existing fur/bone maps and idle/run clips. `Logs/Generate.log`, `PlayerPresence.log`, `WolfMotion.log` and `AssetManifest.json` retain evidence.
- **PLAYER PRESENCE SMOKE PASS** checks human `ShadowsOnly` renderers and the grip attached to the swinging axe. **FATAL ANIMATION SMOKE PASS** exercises an actual fatal AI strike, disabled ordinary gameplay, camera drop, real close-mouth skeletal posing, defensive hands, repeated blood/tearing playback, zero-time freeze, delayed death menu and checkpoint camera/effect restoration. **WOLF ANIMATION SMOKE PASS** covers the revised bite/recovery and held articulated corpse pose, with zero-time collapse freeze. `Logs/Smoke.log`.
- **PURSUIT HANDOFF SMOKE PASS** verifies a clear connected navigation origin and actual movement toward the current player from **three post-feeding directions**. The original ambush smoke also requires progress immediately after the physical feeding sequence and checks the real motor/NavMesh escape into the cabin. Existing display, bilingual plaque, disappearance/return, bark, combat, web, forest, quest and save checks all pass. This establishes the tested regression cases; it is not a claim that every possible terrain corner has been manually played.
- Actual Unity captures **39–44** were inspected: anatomical axe grip, human ground shadow, fatal grab/devouring and collapsing/settled wolf. Visual review exposed omitted metacarpal palm groups in the first grip extraction; the complete forearm bone descendants are now retained and finger contours are smoothed. Restrained local fill makes the hand and decayed mouth readable without adding light protection. Publisher trailer frames from Amnesia: The Bunker and Resident Evil Village informed body/camera/contact composition; observations and primary links are in `PlayerAndFatalPresentation.md`.
- Windows **0.10.0** built successfully: **505,674,142 bytes**. A separate DX11 player on Intel Iris Xe logged no startup exceptions or shader errors, measured **1920×1080 FullScreenWindow → 1600×900 Windowed → 1920×1080 FullScreenWindow**, all passing, then exited with code 0. `Logs/Build.log`, `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` retain evidence. No standalone `-batchmode` was used. This is startup/display verification; the scripted gameplay is verified in editor integration smoke. Full campaign, manual playthrough, final animation production and reference-hardware performance acceptance remain pending.

## Previous release: webbed survivor and decayed creature mouth — 0.9.0

- **46/46 EditMode and 13/13 PlayMode tests rerun and passed**, zero failures. New checks cover bone-following silk, pause freeze, idempotent release and checkpoint restoration, plus native oral textures and nondegenerate imported primary UVs. `Logs/EditMode.xml` and `Logs/PlayMode.xml` record the final 2026-10-09 runs.
- Both generated scenes pass compilation, art and reference validation. The manifest contains **33 models**, including the original **4,020-triangle** spider web. The Walker retains **97,652 triangles and 28 bones**; Unity imports **60,674 vertices** after material/UV splits. All 26 existing tooth groups have sharper tips; geometry, weights and crawl clips remain connected. `Logs/Generate.log` and `AssetManifest.json` retain evidence.
- **WEB AMBUSH SMOKE PASS** verifies visible pre-existing wounds, silk following the victim's bones, frozen bindings during pause, exactly one tear at the pounce impact, a physical fall, feeding/blood and restored torn-web state. Existing **DISPLAY UI**, **CABIN SIGN**, **PURSUIT**, **WOLF BARK**, **COMBAT**, **LIVING FOREST** and **RUNTIME SMOKE PASS** checks all pass, including the flashlight failure, escape and indoor repair. `Logs/Smoke.log`.
- The built-in imagegen source is retained at its native **1254×1254** resolution with its exact prompt and SHA-256. Three **1254×418** crops supply enamel, gum/lip and throat color, with derived normal and AO/smoothness maps. A separate **4096×4096** Blender anatomy bake blends the lip tissue into the existing skin; its resolution does not imply a native 4K generated source. The prior skin atlas and Blender source are preserved. `ArtSource/Textures/CrawlerMouth_Imagegen.source.json`, `CreatureArt.md` and `ThirdPartyNotices.md` document provenance and reproduction.
- Eight actual Unity creature views and 24 crawl frames were refreshed, including `Screenshots/Creature/07-decayed-mouth-open.png` and `08-decayed-mouth-bite.png`. The ambush captures were refreshed, with `Screenshots/38-webbed-injured-victim.png` showing the bound, wounded survivor and the feeding view showing the released body. Visual review corrected wound placement, feathered the lips into skin and replaced folded cheek deformation with smooth closure while keeping each tooth rigid. These are rendered Unity URP frames.
- Windows **0.9.0** built successfully: **504,139,142 bytes**. A separate DX11 player on Intel Iris Xe logged no startup exceptions or shader errors, measured **1920×1080 FullScreenWindow → 1600×900 Windowed → 1920×1080 FullScreenWindow**, all passing, then exited with code 0. `Logs/Build.log`, `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` retain evidence. No standalone `-batchmode` was used. Full campaign, manual playthrough and reference-hardware performance acceptance remain pending.

## Previous release: wooden cabin name plaques — 0.8.1

- The focused Blender authoring/export step passes and adds one **604-triangle** plaque, bringing the manifest to **32 models**. The source wood panel/frame UVs are retained, and the wall-plaque adaptation includes four mounting studs. Unity explicitly maps the 1024px source wood color/normal and packed AO/smoothness textures. `Logs/CabinSignBlender.log`, `Logs/CabinSignExport.log`, `AssetManifest.json` and the credited source archive retain evidence.
- Both scenes regenerated; final **ART VALIDATION PASS** and **VALIDATION PASS** include all three physical plaques and yellow text. `Logs/Generate.log` and `Logs/Validate.log`.
- **CABIN SIGN SMOKE PASS** exercises three actual wooden meshes, lettering attached to each plaque, both name-only language strings, actual single-line mesh layout without overflow and live language changes. The world label avoids the HUD's pixel wrap margin. Existing **DISPLAY UI**, **PURSUIT**, **WOLF BARK**, **COMBAT**, **LIVING FOREST** and **RUNTIME SMOKE PASS** checks all pass. `Logs/Smoke.log`.
- **Six actual 1600×900 Unity URP views** were captured and inspected: `32-sign_cabin-hebrew.png` / `33-sign_cabin-english.png`, `34-sign_research-hebrew.png` / `35-sign_research-english.png`, and `36-sign_store-hebrew.png` / `37-sign_store-english.png`. Each shows a textured plaque above its entrance with only the yellow name, fitting inside the wood. `Logs/SignCapture.log` records the capture pass.
- The earlier **44/44 EditMode and 13/13 PlayMode tests** passed during 0.8.0; they were not rerun for this plaque/localization presentation change. The current release is verified through scene validation, actual rendering and the full gameplay integration smoke.
- Windows **0.8.1** built successfully: **478,602,030 bytes**. A separate DX11 player on Intel Iris Xe logged no startup exceptions or shader errors, measured **1920×1080 FullScreenWindow → 1600×900 Windowed → 1920×1080 FullScreenWindow**, all passing, then exited with code 0. `Logs/Build.log`, `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` retain evidence. No standalone `-batchmode` was used. This is startup/display verification; full campaign, manual playthrough and performance acceptance remain pending.

## Previous release: approaching barks and persistent pursuit — 0.8.0

- **44/44 EditMode and 13/13 PlayMode tests rerun and passed**, zero failures. New checks cover distance-based bark gain/cadence, clamping and imported mono bark/growl/roar clips. `Logs/EditMode.xml` and `Logs/PlayMode.xml` record this release's runs.
- Both generated scenes pass compilation, wiring and asset validation, including the added voice/bark references. `Logs/Generate.log`. No character geometry or texture was changed in this release.
- **PURSUIT SMOKE PASS** exercises actual root deactivation, silence while hidden, a frozen zero-time hidden countdown, an unseen reachable return, return roar, deep growl, menu pause and continuous retargeting to the player without sight/hearing. The simulated pursuit closes distance instead of expiring into patrol. **WOLF BARK SMOKE PASS** exercises actual approaching playback, gain **0.600 at 12m < 0.909 at 5m**, zero-time freeze and immediate shelter silence. Death also stops the bark source. `Logs/Smoke.log`.
- Existing **DISPLAY UI**, **COMBAT**, **LIVING FOREST** and **RUNTIME SMOKE PASS** checks were rerun and pass, including close-range fatal attack, axe kills, movement-only footsteps, warnings/bite animation, exclusive ambush ownership, real escape/repair, Hebrew, documents/map, pause, save/load and gate completion.
- The recorded dog bark and source creature layers have CC0 credits, download hashes and a reproducible offline-capable generator. Audio generation creates a lowered bark and processed growl/roar; these are designed game effects. `Tools/generate_enemy_audio.py`, `ArtSource/Vendor/enemy-voices-cc0/sources.json` and `ThirdPartyNotices.md` retain provenance. Runtime playback requires no download.
- Windows **0.8.0** built successfully: **475,080,514 bytes** reported by Unity. A separately launched DX11 player on Intel Iris Xe logged no startup exceptions or shader errors, measured **1920×1080 FullScreenWindow → 1600×900 Windowed → 1920×1080 FullScreenWindow**, all passing, then exited with code 0. `Logs/Build.log`, `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` record this release. No standalone `-batchmode` was used. This confirms startup/display behavior; audio playback and pursuit are checked in the editor integration smoke, not a manual standalone playthrough or listening review.

## Previous release: wolf bone texture — 0.7.1

- Added original imagegen aged-bone artwork with pores, restrained cracks, dirt and dried blood. The exact native **1254×1254** albedo is retained, with a **2048×2048** tangent normal baked in Blender and a linear AO/smoothness mask. The earlier surface and exact generation prompt are preserved. Bone uses mipmaps, anisotropic filtering, high-quality compression and native NPOT dimensions.
- Visual verification exposed a real mapping defect: appended skull meshes used `UVMap`, while the imported wolf shader read `UVChannel_1`. Their primary UV coordinates were zero, so only one texel appeared across the entire skull. The focused refresh migrates the skull/tissue coordinates into the primary channel; new anatomy uses the matching UV name before joining. Original coat coordinates, geometry, weights and bite clips are retained. Blender export and the existing imported-asset test now reject degenerate skull primary UVs.
- **42/42 EditMode tests rerun and passed**, including the strengthened imported skull UV/bite test. Final scene generation/validation, display UI, combat, living forest and runtime smoke all pass. The 13 PlayMode tests from 0.7.0 were not rerun for this surface update. `Logs/EditMode.xml`, `Logs/Generate.log`, `Logs/Smoke.log`, `Logs/WolfBoneBake.log` and `Logs/WolfBoneExport.log`.
- Actual Unity views `29-wolf-bite-open.png` and `30-wolf-bite-contact.png` were refreshed and inspected after the mapping correction. Bone pores, cracks and stains are visible under the flashlight in both jaw poses. The geometry remains the authored stylized half-skull; this material refresh is not a new scanned model.
- Windows **0.7.1** built successfully: **475,035,822 bytes** reported by Unity. A separately launched DX11 player on Intel Iris Xe logged no startup exceptions/shader errors, measured 1920×1080 full screen, 1600×900 window and 1920×1080 full screen again, then exited with code 0. `Logs/Build.log`, `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` record the final pass. This is startup/display verification, not a frame-rate or full-playthrough acceptance claim.

## Previous release: wolf surfaces, bites and display — 0.7.0

- **42/42 EditMode and 13/13 PlayMode passed**, zero failures. The new exported-asset checks cover the jaw-weighted half-skull, timed wolf bite closure and the Walker's world-scale `Jaw_Bite` deformation. Results: `Logs/EditMode.xml` and `Logs/PlayMode.xml`. The final surface/light-response and deferred window-resize changes were followed by another scene generation, smoke and Windows build; those changes did not require another unit-suite run.
- Generated scenes and asset validation pass. The wolf retains 36 source bones and four clips, with **35,015 triangles** after silhouette smoothing and added anatomy. The survivor exports three separate struggle/fall/dying clips; the Walker exports both jaw shapes. `AssetManifest.json` records actual geometry and sources.
- **DISPLAY UI SMOKE PASS** exercises an actual injected F11 through the Input System while Settings is open, preference persistence, visible localized display/back controls and the display button. It restores the original preference and input routing afterward. The held axe's imported cutting edge points forward and its swing uses that plane.
- **COMBAT SMOKE PASS** additionally exercises a warning howl before engagement, nearby growling, a full one-second bite with closure at the 0.60-second contact, recovery and the Walker's closing jaw at a fatal strike. Existing ownership, range, wall, two-hit wolf kill, checkpoint, blood/scream and New Game checks still pass.
- **LIVING FOREST SMOKE PASS** verifies the revised fall/dying blend, a curved pounce, the mouth coming within 18 cm of the victim's chest, braced feeding and contact blood. The one-time activation, menu pause, flashlight fault, real motor/NavMesh escape, immediate indoor silence, E-held indoor repair, fatal idle pursuit and persistence checks remain passing. **RUNTIME SMOKE PASS** covers the existing wildlife/fog, Hebrew, map/documents, navigation, weak bulb, power/gate and saves. `Logs/Smoke.log`.
- Actual Unity views `29-wolf-bite-open.png`, `30-wolf-bite-contact.png` and `31-fullscreen-settings-hebrew.png` were added, and the combat/ambush views refreshed. Visual review corrected eye highlights and close-light bleaching of exposed bone. Fur uses the retained imagegen UV atlas at its native **1254×1254** resolution, a **2048×2048** Blender tangent-normal bake, mipmaps and high-quality texture compression. It is generated surface artwork, not photographed fur or hair geometry.
- Windows **0.7.0** built successfully: **456,341,182 bytes** reported by Unity. A separately launched DX11 player on Intel Iris Xe loaded without startup exceptions or shader errors. Its opt-in verification measured **1920×1080 FullScreenWindow → 1600×900 Windowed → 1920×1080 FullScreenWindow**, all passing, then exited with code 0. The first check exposed a Windows mode-transition resize issue; dimensions are now confirmed two frames after the transition. Evidence: `Logs/DisplayCheck.json`, `Logs/WindowsLaunch.log`, `Logs/WindowsLaunchCheck.json` and `Logs/Build.log`. No standalone `-batchmode` was used for this display check.

These are automated gameplay/asset checks and a standalone startup/display check, not a full manual playthrough or a performance acceptance test. The ambush uses authored skeletal clips and procedural contact poses, not a general ragdoll/interaction solver. The model and materials are an improved prototype; full campaign, final character production and reference-hardware performance acceptance remain pending.

## Previous release: living forest and ambush — 0.6.0

- **40/40 EditMode and 13/13 PlayMode passed**, zero failures. Added checks cover the circuit fault blocking F/boost/cells/outdoor charging, house-only repair, New Game reset, safe-house bounds, schema 3→4 migration, event/fault round-trip and corrupt fault rejection.
- Blender validates **31 exported models**. Poly Haven source downloads are verified against published MD5 values. Both scene generation and asset validation pass, including actual UV layers on the scanned trees, reduced distance LODs, existing human/crawler/wolf rigs and all material/shader checks.
- The extended runtime smoke passes **LIVING FOREST SMOKE PASS**, alongside the existing combat and quest smoke. It exercises wind on over 700 trees, approach-triggered crow takeoff/flight/reset, visible one-time ambush activation, audible help, menu pause, a real aerial pounce, feeding impacts/blood, an inoperable flashlight, exclusive creature ownership, playing score/heartbeat, a real character-motor escape while NavMesh AI pursues, silence inside the cabin, persisted indoor repair, an idle player's fatal pursuit and recovery.
- The escape is verified after expiring the player's restore invulnerability. An injected E key goes through the existing input action and interaction ray; holding it at the physical indoor charger clears the fault and restores 35% charge. The batch verifier temporarily routes unfocused editor input to the play-mode buffer and restores those settings afterward. The final enhanced smoke passed on 2026-10-09.
- Actual Unity screenshots 23–28 show flight, help, leap, feeding, blackout and refuge. Visual review corrected the source's nonstandard UV attribute, gallows tube twisting, mirrored branch placement, wrist-rope attachment, the imported wing hinge, feeding head height and ground-blood highlights. The main forest no longer uses the old triangular broadleaf placeholder. Crow body parts are combined to avoid a renderer per feather.
- The English help voice is SAPI synthesis with localized subtitles. The scare score, heartbeat and crow sounds are original synthesis; the scream is the credited CC0 recording. No final actor performance or Hebrew dubbing is claimed.

- Windows **0.6.0** built successfully: **434,565,133 bytes** reported by Unity. A separate DX11 process loaded on Intel Iris Xe, remained responsive, and logged no startup exceptions or shader errors. The verification process was stopped afterward. `Logs/Build.log`, `Logs/WindowsLaunch.log` and `Logs/WindowsLaunchCheck.json` retain the evidence. This is a startup check; the scripted full sequence is verified in Unity play mode.

## Previous release: combat and characters — 0.5.0

- **35/35 EditMode and 13/13 PlayMode passed**, zero failures. New checks cover fatal Walker versus wolf wounds, v2 migration, persisted axe/kills, duplicate kill rejection, actual wolf-bone motion/loop flags, imported injury/step clips, point-blank beam immunity and retained wall/shelter protection.
- Generated scenes pass wiring, IDs, navigation, shader/material, human bounds/LOD, five animated wolves, nearby axe, input and blood-overlay validation. Blender export validates 26 models. There are 23 texture sets, including retained older sets.
- `Logs/Smoke.log` records **COMBAT SMOKE PASS** and **RUNTIME SMOKE PASS**. These exercise recurring unseen reachable approaches, protection against repositioning a close pursuit, movement-only player/Walker steps, exertion breathing, a real interaction ray reaching the axe, wolf chase, melee ownership/range/walls/single-hit/two-hit death, axe/dead-wolf restore, actual warned fatal AI attack with the beam on, blood and a scream continuing through the death pause, and New Game reset. Existing Hebrew/map/documents, rooms, weak bulb, wildlife/fog pause, circuit/gate/completion checks also pass.
- Fifteen refreshed Unity URP character/forest views and six UI/combat views were inspected. `15-body-1.png` through `19-human-face.png` show the new casualties; `20-axe-and-defeated-wolf.png` and `21-death-blood-hebrew.png` show actual play-mode combat. Blender previews under `Screenshots/Combat` inspect anatomy separately. Review corrected hair overexposure, angular pool edges, granular wound artifacts and blood detaching from a falling wolf.
- Windows **0.5.0** built successfully: **311,378,825 bytes** reported by Unity. The standalone executable launched using Direct3D 11 on Intel Iris Xe, remained responsive, and logged no startup exceptions or shader errors. The verification instance was stopped after observation. `Logs/Build.log` and `Logs/WindowsLaunch.log` retain the evidence.

The following entries are historical. Current user-requested close-range lethal
rules supersede the original invulnerable-first-encounter/beam-cancellation rules.
Asset and recorded/synthesized audio sources are distinguished in
`ThirdPartyNotices.md`. Automated checks do not establish new-player difficulty,
campaign duration or a sustained frame-rate target.

Executed on Windows with Unity 6000.3.24f1, URP 17.3.0, Blender 5.2.2 and an Intel Iris Xe graphics device. This report describes the forest slice and first art pass, not the complete GDD campaign.

## Automated verification

- Blender generation/export: **22 original model files** and ten PBR texture sets. The reference crawler has 97,652 triangles, 28 bones, eight articulated limbs, a jaw morph and two skeletal clips; Unity imports 60,596 skin vertices including UV/material splits. Export validation covers nonempty geometry, UVs, finite vertices, plausible metre dimensions and both required hero-tree shapes. `AssetManifest.json` records actual counts and source/export paths.
- Unity scene generation/validation: passed for MechanicsTest and ForestGame. One session per scene, complete references, unique pickup IDs, valid checkpoint navigation, no missing scripts, no shader-error materials, preserved FBX axes/metres, physical tabletop/pickup alignment, two hero shape sets and nonempty/reduced distance LODs.
- EditMode: **27 passed, 0 failed**. `Logs/EditMode.xml`. Includes Hebrew, migration/map persistence, joint lengths and degeneracy, actual exported rig/normalized weights/textures, in-place crawl movement/loop continuity/foot heights, and runtime root ownership/zero-time pose freeze.
- PlayMode: **11 passed, 0 failed**. `Logs/PlayMode.xml`. Includes the explicit ordinary-weak-bulb/no-protection test.
- Expanded scene smoke: **passed**. Checks map unavailable before discovery, surveyor map and four body documents, Hebrew HUD, the complete weighted crawler rig and disabled root motion, actual jaw opening and zero-time freeze, click/tension playback, scheduled wolf playback, nonprotective research bulb with the new 1.95m capsule, complete creature paths into all three rooms, player movement, pause, fuse/power, open gate, map/document checkpoint restore and completed-save restore. `Logs/Smoke.log`; verification saves use an isolated temporary directory.
- Unity visual capture: the original **14 scene/UI images** were refreshed, with four additional reference views and **24 actual skeletal crawl frames** under `Screenshots/Creature`. `crawl-preview.gif` assembles those frames into a 1.2-second loop. Review corrected bone roll twisting, shoulder weight transitions, jaw closure, FBX scale in CPU captures, async shader warmup and close-flashlight highlight clipping. The crawler skin shader compresses bright highlights while keeping ordinary lighting, texture detail and separate mouth materials. These are Unity URP renders, not painted concept images.
- Windows x64 build: produced under `Builds/Windows` using `Game.Editor.BuildPipeline.BuildWindows`. `Logs/Build.log`.
- Build packaging includes both font license notices. A final material review corrected the emission flag so strong shelter fixtures glow and the research bulb retains its faint glow without gaining gameplay protection.
- Standalone Windows startup: launched the built executable separately from the editor in batch mode with Direct3D 11 on Intel Iris Xe. Engine/assemblies and the initial scene loaded, the process remained responsive, and the launch log contained no startup exceptions or shader errors. The verification process was stopped after observation. `Logs/WindowsLaunch.log`. This is a startup check, not a manual standalone playthrough.

## Skin update — 0.3.1

- The existing sculpt was refreshed through `skin_material.py` and the focused
  Blender exporter. It retains 97,652 triangles, 60,596 Unity vertices, 28 bones,
  both skeletal clips and the jaw morph. A second UV channel and 4K anatomical
  atlas are now validated in the scene generator. The atlas is baked from an
  original generated 1254px skin source, not a 4K scan of a person.
- Both scenes regenerated and passed validation; shaders compiled without
  errors. **All 27 EditMode tests were rerun and passed.** The 11 PlayMode tests
  listed above passed during the preceding creature pass; they were not rerun
  for this material update.
- The expanded scene smoke was rerun and passed, including actual animated
  creature/jaw playback, pause, room paths and light protection.
- Six studio model/material images, including head and forearm detail, and 24
  crawl frames were rendered with the actual Unity shader. Ten forest views
  were refreshed and inspected, including the close flashlight view. The GIF
  now uses the new skin. Previous face/overview captures are retained under
  `Screenshots/Creature/PreviousSkin` for comparison.
- The normal map derives fine epidermal relief, excluding broad veins/pigment.
  Unity uses a bounded warm diffusion approximation with real light/shadow
  attenuation; it does not implement full volumetric subsurface scattering.
- Windows **0.3.1** was built successfully with the new maps and shader. Sources
  are included in the authoring project; the game loads the explicit FBX and
  imported runtime textures, with no Blender or image generation dependency.
  The standalone build was launched with Direct3D 11, remained responsive and
  reported no startup exceptions or shader errors. The verification process
  was stopped after observation. Build size reported by Unity: 233,915,837 bytes.

## Night atmosphere and human bodies — 0.4.0

This section supersedes the preceding historical asset/test totals.

- The manifest now contains **24 original Blender models** and twelve PBR sets.
  Four posed human casualties replace the two repeated proxies, with approximately
  65,000 / 24,700 / 8,400 triangles in three LODs. The Blender exporter validates
  their geometry, UVs, metre scale and texture references.
- Both scenes regenerated and passed validation with all five wildlife clips,
  42 airborne wisps, four human skin/LOD bindings and the existing quest/rig references.
- **30 EditMode and 11 PlayMode tests passed, zero failed.** The three new
  EditMode tests check stereo/mono clip duration, building interior boundaries
  and zero-time director behavior. Results: `Logs/EditMode.xml`, `Logs/PlayMode.xml`.
- The expanded runtime smoke passed wildlife playback, fog/voice timer freeze,
  actual AudioListener pause and frozen smoke positions across the pause menu,
  alongside map/documents, Hebrew, room paths, creature jaw/audio, weak-light
  protection, power, gate and checkpoint restoration. `Logs/Smoke.log`.
- Fifteen 1600×900 Unity URP scene/model captures and four UI views were inspected.
  Continuous face anatomy, physically scaled skin, restrained illumination and
  smooth shadow envelopes remove the previous sphere seams and oversized
  micro-detail shadows. Small point lights use 256px shadows so the 1024px
  flashlight shadow fits without reducing the 2048px additional-light atlas.
  `15-body-1.png` through `18-body-4.png` and `19-human-face.png` show the figures;
  `02-service-trail.png` / `05-dread-forest.png` compare normal and threat fog.
- The wildlife is original synthesis, not recorded animals. Smoke uses bounded
  noise billboards plus distance fog, not volumetric ray marching. The figures
  are authored game humans, not photogrammetry. Sources: `AtmosphereAndBodies.md`.
- Windows **0.4.0** built successfully: **273,918,417 bytes** reported by Unity,
  with the new imported bodies, sound clips, fog and materials. The separate
  executable was launched with Direct3D 11 on Intel Iris Xe, remained responsive
  and logged no startup exceptions or shader errors. The verification process
  was stopped afterward. `Logs/Build.log`, `Logs/WindowsLaunch.log`.

## What these checks do not establish

The scene smoke scripts interactions; it does not prove that a new player discovers the route, finishes in 10–15 minutes, hears every warning or experiences balanced pursuit. The 60–90 minute campaign, its puzzles and combat do not exist yet.

The geometric PlayMode tests do not cover every live AI FSM transition. Low-FPS complete-AI testing, sustained CPU/GPU/VRAM profiling, reference hardware 1080p/60 FPS and the full A01–A26 manual matrix remain pending. No target frame rate is claimed from the presence of LODs.

The creature now has authored weighted breathing/crawl clips, a jaw morph and runtime foot placement. Skeletal attack/recoil/death clips remain pending. The model is a stylized interpretation of the supplied reference, not a photogrammetric copy. Player hands, some interaction objects and synthesized sound still need production polish. No full campaign completion, final sound quality or M5 acceptance is claimed.
