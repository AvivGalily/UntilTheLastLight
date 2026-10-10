# Development progress

Current result: **0.10.0**, the 280×280m forest slice with a human player shadow, an anatomical axe grip, clear navigation handoff after feeding, a close fatal feeding sequence and improved wolf bite/collapse animation. The wounded webbed survivor, decayed Walker mouth, bilingual wooden plaques, persistent pursuit/hidden returns, approaching barks, half-skull wolves, axe combat, human casualties/documents, scanned moving forest/crows, night wildlife and fog remain connected. There are **35 exported Blender models**. All **49 EditMode and 13 PlayMode tests** were rerun and passed, alongside scene validation, full gameplay smoke and inspected Unity captures. Windows 0.10.0 built and passed separate startup/display verification. The full campaign remains pending. Earlier entries below are historical; current presentation and asset sources are in `PlayerAndFatalPresentation.md`, `CombatAndCharacters.md` and `ThirdPartyNotices.md`.

## 2026-10-08 — original implementation notes (historical)

- Original supplied GDD is preserved beside this file.
- Installed and pinned Unity 6000.3.24f1 LTS; discovered Blender 5.2.2 LTS.
- M0/M1 sources written: explicit ordered simulation, CharacterController, Input System maps, shared flashlight geometry, raycast occlusion, swept capsule checks, retreat, senses/memory, warning/windup and wound model.
- M2 sources staged: checkpoint recovery, inventory, fuse route, paused menus, shelter charging, map/journal, forest generation.
- Reproducible scene/prefab generation is confined to Assets/_Game/Generated; no hand-authored scene YAML or invented asset GUIDs.
- Current verification: import/compilation pending. No milestone is yet accepted.
- Next: compile; generate MechanicsTest; run EditMode and PlayMode tests; only then generate and inspect the playable forest.

All art and sound are explicitly placeholders. The full M3–M6 story, 60–90 minute duration, final combat, final art and release QA are not claimed complete.

## 2026-10-08 — forest art and verified playable slice

The project now contains a working M2-style forest slice with a first Blender art pass. This is not acceptance of the complete campaign or of its target duration. The original GDD's design rules remain unchanged.

### Implemented and connected

- Locked Unity 6000.3.24f1 / URP 17.3.0 / Blender 5.2.2, reproducible scene generation, a title/pause/settings interface, MechanicsTest and ForestGame, and a Windows build.
- Directional flashlight protection, battery/boost/swap, occlusion, swept capsule checks, retreat, senses/memory, warning/windup, wounds and shelter recovery. A native `NavMeshPath` constructor error is fixed through main-thread lazy initialization.
- The slice's ranger cabin, east storehouse, two routes, service fuse, circuit, lamps and temporary exit gate. Inventory, journal/map, checkpoint persistence, backup recovery and emergency charging are connected.
- Nineteen original Blender assets: two hero trees, four regular tree families with three LODs, three rocks, fern, grass, two furnished cabin layouts, flashlight, shelter lantern, Walker body, gate, fence and fuse panel. The regular pine LOD0 budget is below 8,000 triangles. A connected-island reduction step makes the distant LODs materially smaller.
- Nine original PBR texture sets, alpha-clipped needle/broadleaf silhouettes, explicit URP material mapping, fine normal detail, instancing and stable simple collision. Imported FBX unit/axis transforms are preserved; a validator checks cabin scale, orientation and the pickup/tabletop alignment.
- Cold exterior light, warmer shelters, star/moon sky, fixed-exposure ACES grading, restrained bloom/vignette, irregular dirt trails, ground vegetation and a bounded set of depth-faded mist wisps. Mist is presentation only and grants no protection.
- Both hero tree shapes are connected to Dread, including FBX-qualified shape names. Imported mesh bounds are expanded in their actual local coordinates; trunk colliders remain static. Resetting Dread clears its shader and fog state.
- The north fence has an actual gap for the sliding leaf rather than a solid fence segment behind it. The leaf uses dynamic NavMesh carving; the scene smoke checks that its open passage is physically clear.

### Verification achieved

- Blender generation and FBX export executed successfully; meshes, UVs, finite coordinates, plausible units and required hero shapes were validated.
- Unity generated and opened both scenes, rebuilt navigation and passed validation for session wiring, IDs, missing scripts and art/LOD constraints.
- EditMode: **15/15 passed**. PlayMode: **10/10 passed**. Results are in `Logs/EditMode.xml` and `Logs/PlayMode.xml`.
- Automated play-mode scene smoke passed: legal monster spawn, actual player movement, paused battery, pickup, power activation, save/load, gate completion and restoration of a completed save without a monster. Verification saves are isolated from the user's saves.
- Five real 1600×900 Unity URP images were captured and inspected. The review caught and corrected FBX axis/unit handling, mirrored tabletop placement, inappropriate texture scale and overly geometric foliage. See `Docs/Screenshots`.
- A Windows x64 build was produced. See `TestReport.md` for its launch verification and the limits of the automated checks.

### Still missing from the GDD

- **M0/M1 acceptance:** dedicated Boot/MainMenu scenes; a documented manual light/AI playtest, full FSM warning/recovery scenarios and low-FPS tests of the complete moving AI. The existing tests cover core geometry and attack state, not every end-to-end acceptance case.
- **M2 acceptance:** a new-player walkthrough and a measured 10–15 minute duration. The scripted smoke test uses public interactions and does not prove navigation/discovery by a person.
- **M3 campaign:** the ~450×450m map and 1.8–2.4km route; hunter/research/radio cabin layouts and watchtower; P01–P05; service/tower keys, knife, medallion, three typed fuses, radio tuning/codes, quest dependencies, additional safe checkpoints, full documents and recordings.
- **M4 finale:** revolver/aim/reload, dark/close-range vulnerability, three confirmed hits, warning-safe fight, emergency ammunition, death animation, persistent monster defeat and the dawn escape sequence.
- **M5 production art/audio:** Generic Walker rig and movement/recoil/attack/death clips, proper first-person hands/equipment animation, production props and modular architectural kit, footstep surface routing, richer ambience and recordings with subtitles. The current body is static and hand/some pickups/chargers still use prototype geometry.
- **M6 release:** remapping, persistent settings, full audio controls, text-scale/brightness UI, Low/Medium/High profiles, instrumented CPU/GPU measurements, reference hardware profiling, complete A01–A26 acceptance, full save migrations, player timing/balance and release packaging.

### Reproduction and next step

See `QuickStart.md` and `Tools/Toolchain.md`. Use `Tools/Invoke-Blender.ps1`, then `Tools/Invoke-Unity.ps1 -Task Generate`; Capture, Smoke, EditTests, PlayTests and Build are separate tasks. The scene generator is limited to `Assets/_Game/Generated` and preserves generated asset GUIDs.

Next milestone: a manual M2 light/AI/discovery playtest, then the M3 quest state and connected four-cabin greybox route. Expand story content before calling this a 60–90 minute game. Do not mark M3–M6 or final art complete from the current slice.

## 2026-10-08 — expanded forest / Hebrew / spider-human request

- Enlarged the bounded playable area from 120×120m to 280×280m: **5.44 times the area**. Landmarks and branch/loop trails now share `ForestLayout` data with the paper sketch. Forest density, understory, mist, LODs and navigation were regenerated.
- Replaced the static human Walker model with an original Blender creature: pale human torso and arms above a dark arachnid abdomen, eight pivoted legs, a featureless crown with no eyes/nose, thirty individual fangs and an opening-jaw morph. Unity animates the gait from actual movement and opens the mouth with real proximity/attack windup. These are procedural presentation animations, not a completed cinematic rig.
- Added four clothed bodies, satchels and folded papers. Each supplies a different document; the surveyor beside the first trail bend also grants the field map. The map is unavailable until discovery, shows all buildings/bodies/lantern/gate and supply locations approximately, and has no GPS marker. Found documents can be reread in a paged journal.
- Added a Blender research station with a 3m corridor, three furnished rooms, 2.5m openings and matching physical/occluding walls. Its dim bulb is a decorative Unity Light only, excluded from `SafeLight` registration. All three rooms have complete creature NavMesh paths.
- Added 126 matching English/Hebrew string entries covering menus, HUD, prompts, signs, documents, hints, warning captions and map labels. Hebrew is the default; language selection is persisted. Baked Liberation Sans/Noto Sans Hebrew SDF atlases avoid incomplete glyphs on first render. Logical-line wrapping precedes Hebrew bidi ordering, preserving Latin control names, numeric runs, percent signs and brackets.
- Added original synthesized directional clicks, proximity drone/pulse and occasional spatial wolf howls (20–35 active seconds initially, then 45–90). Threat layers grow with actual distance and recede in protective shelter. Pause freezes the audio director and listener. The explicit attack cue remains separate.
- Added physical trail patrol when sensory memory expires, allowing the creature to search the larger forest without teleporting to an unseen player.
- Save schema 2 persists map acquisition and recovered documents. Version 1 checkpoints migrate while preserving resources/quest progress and allowing map discovery. Backup recovery stays intact.
- Validation now checks creature pivots/jaw, body/map counts, Hebrew glyphs/documents, audio references and absence of research shelter protection. Smoke verifies actual jaw weights, zero-time freeze, click/tension playback, scheduled wolf playback, discovery, room paths and map/document checkpoint restore.
- New edit tests cover Hebrew mixed text/paragraphs, old-save migration, map/document persistence and proximity behavior. A new PlayMode test confirms an ordinary lit bulb does not block an attack or repel the creature. **22 EditMode + 11 PlayMode tests pass.** Real engine captures include Hebrew/English maps, a Hebrew document/title, both mouth states, bodies and the new rooms.

Still pending from the full GDD: the full ~450×450m campaign route, remaining hunter/radio locations and tower, P01–P05, their inventory/quest dependencies, revolver/finale, authored attack/death clips, first-person hands, final sound mastering and measured player duration/performance. The research building, gait, map discovery and bilingual UI above supersede the corresponding gaps in the older audit.

## 2026-10-08 - reference crawler, prototype 0.3

Replaced the upright chitin/pivot creature with an original fleshy, horizontal humanoid crawler guided by the user supplied image. Fused anatomy, pale vein/crease maps, an actual mouth recess with irregular teeth, gums, tongue and saliva, long muscular limbs and five-fingered front hands are authored in Blender. The mesh uses a 28-bone weighted rig, two looping idle/crawl actions, a shared jaw morph and explicit Generic FBX import.

Unity blends clips by real AI movement and solves eight two-bone chains after animation, keeping world-space stance anchors and raised recovery arcs. Ground rays and wall casts position the feet. Simulation pause freezes animation; the AI retains root motion ownership. Navigation and protection use a 1.95m height for the low posture. A dedicated URP material preserves detail under close flashlight highlights.

Verification: 27 EditMode and 11 PlayMode tests passed; both generated scenes validated and the expanded smoke passed with the new rig/capsule and all three room routes. Four model views and 24 crawl frames were rendered through Unity URP, along with updated scene images. A Windows x64 build was produced and launched successfully. See CreatureArt.md for sources and reproduction, and TestReport.md for the limits of these checks. The full campaign and skeletal attack/recoil/death clips remain pending.

## 2026-10-08 — human skin, prototype 0.3.1

Replaced the flat procedural skin and drawn vein lines with an original imagegen
human epidermis texture, retaining its native 1254px source and exact prompt.
Blender bakes a 4096px rest-space anatomical atlas on the second UV channel,
keeping skin pigment continuous across UV seams while following the rig and jaw.
Separate 2048px pore relief and variable roughness maps provide fine detail;
broad pigment and veins are excluded from relief. The maps are shared with the
Blender Principled material and Unity URP skin shader. Lower dielectric
reflectance and subtle, shadow-aware warm diffusion soften the plastic finish.

The focused `skin_material.py` tool refreshes skin without rebuilding the sculpt,
weights, clips or jaw. The full creature pipeline calls the same tool. The
exporter can refresh one asset while retaining all 22 manifest records. Source
provenance and reproduction are documented in `CreatureArt.md` and
`ArtSource/Textures/README.md`.

Verification: both scenes regenerated/validated, all 27 EditMode tests rerun and
passed, and the expanded animated scene smoke passed. Six studio views, ten
forest views and 24 crawl frames were rendered in Unity; the preview GIF was
refreshed. Windows 0.3.1 was built successfully. The earlier 11 PlayMode results
remain recorded separately in `TestReport.md`.

## 2026-10-08 — night atmosphere and human bodies, prototype 0.4.0

Added an original stereo cricket bed, owl hoots, nightjar calls, frogs and leaf
rustles. Calls use separate spatial sources and seeded irregular schedules.
Wildlife becomes quieter and muffled indoors, and ducks near the creature or
its attack warning. The game simulation controls the timers; paused menus also
pause audio playback.

Reduced visibility with denser grey distance fog, a muted sky, 42 drifting
airborne smoke wisps and 50 wider ground mist banks. Noise and drift follow
simulation time and freeze on pause. Interior density eases and airborne wisps
avoid buildings. This remains a bounded real-time effect.

Replaced both repeated corpse proxies with four separately posed Blender human
models. They have continuous facial anatomy, closed eyes, ears, lips, hair,
hands with fingers/thumbs, tailored coats, trouser folds, boots and document
satchels. Human skin reuses the original epidermis texture; new fabric/leather
PBR maps add fine surface detail. Each body has three reduced distance LODs.
Flashlight brightness and shadow allocation were calibrated for close inspection.
The four existing document/map pickups remain connected to these figures.

Sources, reproduction and visual limits are documented in
`AtmosphereAndBodies.md`; verification results are in `TestReport.md`.

Verification: both scenes regenerated/validated; all 30 EditMode and 11 PlayMode
tests passed. The scene smoke confirms wildlife playback, actual fog drift,
zero-time freeze and the paused menu's frozen audio/atmosphere, alongside the
existing map/document, rig, navigation, light and checkpoint flow. Fifteen
scene/model images and four UI views were refreshed in Unity. Windows 0.4.0
built successfully and passed a separate Direct3D 11 startup check.

## 2026-10-08 — characters, recurring pursuit and axe combat, 0.5.0

- Replaced four casualties with CC0 MakeHuman anatomy, original UV skin, fitted outfits, hair and eyes. Three LODs provide approximately 76k / 32k / 10k triangles per body. Added localized bruises/blood and soft-edged pools. Notes, IDs and map discovery remain intact.
- Added an original Blender field axe beside the closest body and five CC0 NewDLC wolves. Each wolf has 36 bones, 5,358 triangles and project-authored idle/run/bite/death clips. It navigates, respects walls/shelters, staggers and dies after two axe hits. Range, forward arc, one damage moment per swing and occlusion are enforced.
- Removed the Walker's fuse/tutorial dependency. The director schedules unseen, reachable distant returns while preserving close or visible pursuits. Within 2.8 m the flashlight cannot repel it; a completed Walker strike is fatal. Walls and shelters still protect.
- Real movement drives walking/running/crouching ground steps. Breathing follows exertion, stamina and wounds. Walker clicks require movement. Human scream/grunt, screen blood and a Hebrew/English death menu are connected.
- The crawler retains its original rig, skin and jaw, with added bone-attached blood and head asymmetry. Wolf wounds follow the falling skeleton. Visual review corrected hair highlights, angular pool edges and granular wound artifacts under the Unity flashlight.
- Schema 3 preserves axe ownership and dead wolves; versions 1/2 migrate. Checkpoints and New Game resets were exercised. Source credits are retained, and the full asset/audio/localization pipeline includes this extension.
- **35 EditMode + 13 PlayMode passed.** The extended runtime smoke also passes repeated approaches, movement-based audio, exertion, an interaction ray reaching the axe, wolf chase, melee range/wall/single-hit/kill rules, combat save/load, an actual fatal AI strike despite a full beam, continuing scream, blood/death UI and reset. Existing map/room/quest/pause checks still pass. Windows 0.5.0 built and passed standalone DX11 startup verification.

The larger campaign, revolver finale, professional sound mastering, first-person
hand animation and reference-hardware performance acceptance remain pending.
This is an improved playable prototype, not acceptance of final AAA character
production or the full 60–90 minute campaign.


## 2026-10-08 ? scanned forest, wildlife and ambush, 0.6.0

Photographed Poly Haven CC0 surfaces and three-LOD scans replace the two principal tree variants and three rock variants, and add fallen timber/stumps. The source Geometry Nodes UV attribute is converted explicitly to an FBX UV layer. Wind animates visual tree children while collision remains fixed. Twenty-four feathered crows flush from trail-side branches with animated wings and local sounds.

A one-time, visible approach to the old oak triggers an articulated adult's help call, a crawler leap, a falling victim and braced feeding with jaw/head movement, blood particles, wounds and an expanding pool. The same crawler then pursues the player with the flashlight circuit failed, rising score and heartbeat. Movement/look remain available. Entry into either amber house silences the chase audio; only an indoor charger can repair the light. Schema 4 persists the event and fault and migrates older checkpoints. Source, voice limitations and reproduction: `LivingForest.md`, `ThirdPartyNotices.md`.

40 EditMode and 13 PlayMode tests pass. The extended scene smoke passes the cinematic phases, physical escape and save/load flow alongside the earlier combat/quest checks. Windows 0.6.0 builds and passes separate DX11 startup verification (434,565,133 bytes). Screenshots, visual corrections and practical test limits are recorded in `TestReport.md`.

## 2026-10-09 — wolf surfaces, bites and display, 0.7.0

Replaced the wolf's flat coat with a detailed imagegen fur atlas, a native Blender tangent-normal bake and textured eyes. The silhouette is smoother; one side of the head has an original exposed skull, empty socket, teeth and a jaw-weighted mandible. The CC0 NewDLC body/rig is retained. Source artwork, its prompt, prior texture and authoring files are preserved. Bone and eye lighting preserve their detail under the flashlight.

The wolf howls before a distant engagement and growls nearby. Its full bite opens, closes at contact and recovers; the Walker's lower face and teeth now also close during a strike. The axe edge points forward and swings in that plane. F11 or the localized Settings display button selects borderless full screen or a resizable window, with the player's choice saved.

The ambush survivor now has separate struggle, overpowering and dying clips, a gravity-shaped fall and a settling body. The crawler follows a curved leap, braces its front limbs and brings its mouth to the chest for feeding/blood. The existing surprise, blackout, escape, shelter silence and repair mechanics remain connected. Schema 4 remains compatible.

42 EditMode and 13 PlayMode tests pass. Final generated-scene validation and display/combat/living-forest/runtime smoke pass. Windows 0.7.0 builds (456,341,182 bytes) and separately verifies actual full-screen/window/full-screen dimensions and clean DX11 startup. `TestReport.md` records evidence, native texture dimensions and the remaining production/performance limits. The root GDD and Docs copy both include this extension.

## 2026-10-09 — exposed wolf bone surface, 0.7.1

Added original imagegen bone artwork with pores, small cracks, dirt and dried blood, a Blender-baked tangent normal and variable roughness/AO. Fixed the appended skull's UV layer mismatch so the material displays across the skull and articulated mandible rather than sampling a single texel. Body geometry, coat coordinates and the four clips are retained. Both focused refresh and full authoring pipelines reproduce the material; source artwork, prompt and the previous surface remain in `ArtSource/Textures`.

All 42 EditMode tests rerun and pass, including an imported primary-UV check. Final scene validation, gameplay/display smoke and a separately launched Windows 0.7.1 player pass. Build size: 475,035,822 bytes. New bone detail is visible in the open/contact bite screenshots. `TestReport.md` records the current evidence and the preceding, unrepeated PlayMode suite.

## 2026-10-09 — approaching barks and persistent creature pursuit, 0.8.0

Wolves bark during actual approach movement, using an adapted CC0 dog recording. Spatial panning remains active; distance gain is applied once and updated throughout playback. Both volume and repetition increase at close range. Barks stop during a bite, stagger, death or shelter protection; the earlier warning howl and growl remain.

The Walker retains the current player position during active forest pursuit, including outside sight/hearing range, and no longer ends that hunt because its sensory memory or hunt timer expires. Existing walls, flashlight rules, warning/windup and shelter protection remain. When distant and out of view, it can deactivate for 3–6 active seconds before returning at a reachable unseen position at least 30m from the current player. This director yields to the scripted ambush. New spatial roars and deep growls use credited CC0 recordings and original processing; disappearance and shelter entry silence the voice, and attack warnings keep their own cue.

Both scenes regenerated and passed validation. All **44 EditMode and 13 PlayMode tests were rerun and passed**. Integration smoke verifies real disappearance/return, current-target pursuit without sight/hearing, pause, audible voices and wolf barks increasing from gain **0.600 at 12m** to **0.909 at 5m**, alongside the existing combat, ambush, save and quest flow. Windows **0.8.0** built successfully (**475,080,514 bytes**) and passed clean DX11 startup plus full-screen/window/full-screen verification. `TestReport.md` and `Logs/WindowsLaunchCheck.json` retain the current evidence.

## 2026-10-09 — wooden name plaques, 0.8.1

Adapted the CC0 Textured Medieval Sign model by allinonemovie in Blender: removed the shop bracket, retained the wood panel/frame and source UVs, resized it as a wall plaque, and added four mounting studs. Its 1024px source albedo/normal and packed AO/smoothness maps are explicitly mapped to Unity URP. The three buildings now have real modeled plaques above their entrances, with attached yellow letters that show only the building name in the selected language. Hebrew world labels use a single line rather than the HUD's pixel-based wrap margin; automatic sizing keeps English and Hebrew inside the plaque.

Both scenes regenerated, final validation and the complete runtime smoke pass. **CABIN SIGN SMOKE PASS** verifies the actual wooden meshes, attached yellow lettering, one-line names without overflow and live language switching. Six Unity URP views (32–37) were captured and inspected, covering all three buildings separately in Hebrew and English. No unit tests were added or rerun for this presentation change; the preceding 44 EditMode and 13 PlayMode suite remains documented under 0.8.0. Windows **0.8.1** built (**478,602,030 bytes**) and separately passed clean DX11 startup plus full-screen/window/full-screen verification, exiting with code 0. The GDD mirrors, asset manifest and source/license notices were updated.

## 2026-10-09 — webbed survivor and decayed creature mouth, 0.9.0

The ambush survivor now starts wounded and trapped in a modeled spider web attached to the old oak. Fourteen animated support strands follow the skeleton, with seven silk bands across the body. At the existing pounce impact the core and bindings release once, the loose strands snap back, and the survivor falls into the existing feeding sequence. The remaining rim stays on the tree. Pause and seen-event checkpoint restoration preserve the correct silk state. The flashlight failure, chase, indoor silence and repair flow remain connected.

Original built-in imagegen artwork supplies decayed enamel, bruised/scarred gum and lip tissue, and dark inner-mouth surfaces. Native material crops have explicit primary UV mapping and derived normal/roughness detail. A separate Blender anatomy bake feathers the lip edge into the skin. All 26 tooth groups were sharpened across the jaw shapes. Visual review corrected a folded cheek at bite closure; the revised face compresses smoothly while each tooth moves rigidly. Exact artwork, prompt, hashes and the prior Blender source are retained.

All **46 EditMode and 13 PlayMode tests** were rerun and passed. Final scene validation and gameplay smoke pass, including the new **WEB AMBUSH SMOKE PASS**. Actual Unity captures show the bound injured survivor, released feeding pose and open/closing textured mouth; eight creature views and 24 crawl frames were refreshed. Windows **0.9.0** built (**504,139,142 bytes**) and separately passed clean DX11 startup plus full-screen/window/full-screen verification, exiting with code 0. `TestReport.md` records the current evidence and practical limits. The GDD mirrors and asset/source documentation are updated.

## 2026-10-10 — human player presence and fatal interactions, 0.10.0

Added a clothed anatomical shadow body and a textured five-finger axe grip from the retained CC0 human anatomy. The grip follows the actual weapon swing; palm extraction includes the metacarpals and the finger contours are smoothed. Restrained visual fill improves readability. The shadow adapts to crouching and movement without rendering the body inside the camera.

The Walker's cinematic-to-navigation handoff resolves to a clear capsule position beside the feeding contact, with a complete path and legal first step. Initial/distant placement also checks collision clearance. Paths reject physically blocked segments, and ordinary pursuit can use legal lateral steps around a corner. The final smoke confirms actual progress toward the player from three post-feeding directions and through the existing escape route.

A 5.6-second fatal sequence now owns the player camera and attacker before the death menu. The camera falls, defensive hands rise and drop, the Walker braces beside the player and bites near the lens with blood, a scream and new guttural/tearing/sting layers. Gameplay input is disabled, while the cinematic and its audio continue. New Game and Continue restore the camera/equipment and clear effects. Wolf attacks now include anticipation, push-off, a collision-checked lunge and recovery; their articulated death includes a shock, ground-contact side collapse and damped settling.

Publisher trailer frames from Amnesia: The Bunker and Resident Evil Village informed camera/hand/contact composition. The shipped assets and choreography are project work and the retained credited CC0 sources. All **49 EditMode and 13 PlayMode tests** were rerun and passed. Final generated-scene validation and the full runtime smoke pass; Unity captures 39–44 show the grip, shadow, fatal interaction and wolf collapse. Windows **0.10.0** built (**505,674,142 bytes**). `TestReport.md` records standalone verification and practical limits. The GDD mirrors and source/license documentation are updated.
