# Forest opening implementation

New Game now plays the 110-second first-person opening in `Assets/_Game/Generated/Scenes/ForestGame.unity`. Open that scene, enter Play Mode and choose **התחלת המסע / Begin the journey**. Continue loads the existing checkpoint without replaying the drive.

During the drive, use A/D, left/right arrows or the controller's horizontal left stick. Move the mouse or controller right stick to look around the cabin; head turns are bounded and lean toward the centre to see past the driver headrest. The wheel-grip arms remain anchored to the seated body while looking around. Forward motion follows the authored path automatically; steering is limited to 0.85 metres and smoothly centres at the entrance. Escape/controller Start pauses. After completing the opening once, hold Space/controller A for two seconds to skip. Settings include subtitles and a reduced-flash option. Both the complete sequence and a skip initialize the same original ranger-cabin state.

## Existing project audit

- Unity 6000.3.24f1; URP 17.3.0; Input System 1.14.2. These pinned local versions were retained.
- Entry point and ordered simulation: `Assets/_Game/Scripts/Core/GameSession.cs`, `StartNew`, `Continue` and `Update`.
- Existing scene and cabin: `Assets/_Game/Generated/Scenes/ForestGame.unity`, `GameSession.cabinSpawn`. The opening uses an isolated forest strip in this scene and the original FPS camera, controller, cabin and world.
- Save/load: `Assets/_Game/Scripts/Save/SaveService.cs`, existing `checkpoint.json`, `ForestGame` scene ID and `ranger_cabin` spawn ID. `introCompleted` is an optional additive schema-5 field. Older checkpoints still load; Continue always bypasses the opening.
- FPS, flashlight, equipment, inventory and objectives: existing PlayerMotor, FlashlightModel, InventoryService and GameHud. Original starting resources remain two batteries, a charged flashlight, the guard note and no weapons.
- Audio/localization: existing AudioListener volume and pause state, English/Hebrew JSON tables and RTL text formatting. Opening sound sources are independent layers; no AudioMixer existed to integrate.
- Input: a separate `Intro` map enables steering/look/skip; Gameplay stays disabled until the cabin handoff. The existing always-enabled UI map handles Back and fullscreen.

## Implementation and assets

`Assets/_Game/Scripts/Intro/` contains the configurable sequence, rail vehicle, wheel/forearm posing, dialogue, passenger posing, layered audio and transition bridge. `ForestIntroConfig.asset` contains timing and twelve distinct voice slots per language. Events and dialogue follow elapsed simulation time; pause stops sequence time and listener playback. Camera parenting and the disabled controller/hand state are restored on completion, skip or cancellation.

The main camera sits in the driver seat throughout. A separate 512×256 camera has no AudioListener and serves only the physical rear-view mirror. The original gameplay camera/listener/UI and save systems are reused. The environmental opening root is disabled after the blackout handoff. Ordinary AI, active play time and flashlight drain do not advance during the opening; autosave happens after gameplay control returns.

The retained Blender sources author an original 4×4 interior and separate steering wheel, plus three independently fitted seated CC0 MakeHuman characters, using the same macro build, age and clothes as the later forest bodies. Each retains a 163-bone rig; exact triangle counts are in the asset manifest. Vehicle and wheel have 16,168 and 3,440 triangles. The authored FBX assets, editable vehicle/hunter/gate prefabs, dirt-road mesh, PBR materials, forest LODs, working mirror and supplied opening audio are included. Original project hand anatomy and existing forest assets are reused.

The opening uses **all twelve user-supplied English MP3 takes**, including `VO_Jack_L02.mp3`. The supplied Runway manifest identifies Tyler as Chad, Jack as Clint and Ethan as Brodie. Complete takes are decoded to mono 24 kHz / 16-bit PCM WAVs in `Assets/_Game/Audio/VO/English`, preserving the existing Unity GUIDs. No words are trimmed and no playback speed or pitch is changed. They are preloaded and play through the existing three seat-positioned sources, without a runtime network dependency. Ambience/gate and threat ease down while a voice plays, then recover smoothly. English audio also plays with Hebrew subtitles; an assigned Hebrew take overrides that fallback. Windows include at least 150 ms after each take. L06 starts at 44.6 s, Jack's final warning at 86.25 s and Tyler's at 89.6 s; Tyler's full 2.08-second recording ends at 91.68 s, before the 92-second blast. The scene remains 110 seconds long.

[Listen to the supplied dialogue audition](Audio/ForestOpening_Dialogue_Preview.wav). It shortens gaps and omits the driving/SFX mix. [Voice manifest](Audio/ForestOpening_VoiceManifest.json) retains spoken text, supplied voice/task IDs, start, duration, levels and source/output SHA-256. Original MP3s and the unchanged supplied manifest are retained in `ArtSource/Audio/Runway`. Previous Kokoro takes and their manifest are archived outside Unity in `ArtSource/Audio/LegacyIntro`.

The four supplied effects replace the previous engine, tyres, wind, score, gate, branch, creature and impact layers. `SFX_ForestVehicleAmbience` is the only supplied loop; a 150 ms tail/head overlap gives its 8-second source a continuous 7.85-second loop, fading into the forest silence at 69–73 s. `SFX_ForestGate` plays once at 65 s, aligned with the closing gate leaves. `SFX_CreatureApproach` plays once at 79.75 s just before Ethan hears it, moving from rear left toward the vehicle at the 84-second threat beat. `SFX_VehicleExplosion` plays once at 92 s with its complete six-second tail through blackout. These preloaded PCM files are in `Assets/_Game/Audio/Opening`. Existing tinnitus, breathing and wake rustle remain as separate aftermath foley, as allowed by the supplied manifest. [Effect receipt](Audio/ForestOpening_SuppliedAudio.json) records source hashes, loop flags, channel layout, attenuation and timing; [dry threat/impact audition](Audio/ForestOpening_ThreatImpact_Preview.wav) contains the supplied clips.

## Reproduction

1. Run `Tools/Run-BlenderStep.ps1 -Script Tools/Blender/forest_intro.py -LogName ForestIntroArt` to reproduce the five source models/FBX files. Focused `export_assets.py --asset intro_<name>_01` exports update the retained asset manifest.
2. Run `python Tools/import_intro_audio.py --source ArtSource/Audio/Runway` with ffmpeg available (or install `imageio-ffmpeg`). This uses the retained supplied MP3s and manifest; it does not generate speech. `--source` may also name a directory containing the original downloaded files.
3. Run `Tools/Invoke-Unity.ps1 -Task Intro` to install/update the opening in the existing scene. This preserves configured timing and VO references. Full `Generate` also wires the opening after creating the existing world/navigation.
4. Run `Tools/Invoke-Unity.ps1 -Task IntroAudio` to import all sixteen supplied files, update VO timing and replace just the opening effects group. This focused installer preserves scene art, the vehicle and the three passenger voice sources.
5. Run `-Task IntroSmoke`, `-Task EditTests`, `-Task PlayTests`, `-Task Validate`, `-Task Smoke` and `-Task Build` as appropriate. Intro verification saves to a unique temporary directory and restores language/seen preferences. The existing forest smoke suite bypasses the opening deliberately; the dedicated intro suite covers the drive and transition.

`generate_intro_audio.py`, `generate_intro_voices.py` and `ArtSource/Audio/ForestIntroVoice.json` retain the previous procedural/Kokoro authoring approach for reference. Running the legacy VO generator replaces the supplied voice WAVs; use `import_intro_audio.py` and `IntroAudio` to restore the current supplied-audio version.

## Remaining production work

- Supplied English VO is complete for L01–L12. Hebrew recordings are optional production work; current Hebrew mode uses English audio with translated subtitles.
- The vehicle is a functional procedural art pass. Further authored body/interior detail, windshield condensation and dust polish remain; this is not claimed as a finished production vehicle.
- Hunters have distinct age/build, actual clothing styles, iris maps and seated head/spine/arm reactions, with clean living skin. Bespoke facial acting, precise lip sync and recorded laughter remain unfinished.
- No new focus-blur effect or production cabin-breath recording was added. The existing cabin receives a low camera wake/rise, slow fade and retained breathing with a rustling sound substitute. Purpose-recorded cabin wood/ambience still needs an audio pass.
- The monster and source of the blast remain unseen. Reference-hardware frame-rate benchmarking and human comfort/audio playtesting are not complete.

Verification evidence and current results are recorded in `Docs/ForestOpeningVerification.md`; 1600×900 Unity captures are `Docs/Screenshots/72` through `79`.

## Living hunters and revised sound pass ? 2026-10-11

Jack in the front seat matches `forest_body_02` (older, bearded, blue casual outfit); Tyler in the rear right matches `forest_body_01` (middle-aged, slimmer casual outfit); Ethan in the rear left matches `forest_body_03` (younger, sturdier, work clothes). Their macro build and clothes use the retained CC0 sources. Healthy albedo and separate living materials avoid the baked bruises/wounds and corpse treatment. The bodies on the ground retain their existing injury maps and gameplay item IDs. Rear seating height is calibrated to the actual cushion/hip landmarks. A warm cabin light, larger mirror and head-turn lean keep both rear hunters visible.

The clean Mindfront skin derivative was edited with the **built-in image_gen.imagegen tool** and saved to `ArtSource/Textures/IntroHealthySkin_Imagegen.png`, with the game copy at `Assets/_Game/Art/Textures/IntroSkin0_BaseColor.png`. The exact final prompt is in [the prompt file](../ArtSource/Textures/IntroHealthySkin_Imagegen.prompt.txt); [the receipt](../ArtSource/Textures/IntroHealthySkin_Imagegen.json) records reference, full prompt, dimensions, hashes and character/body mapping. Jack uses the separate healthy bearded CC0 Jartur69 skin.

`night_crickets.wav` is silent during the opening and on open forest trails. In gameplay it is limited to the cabin, storehouse and research station: source volume is at most 0.012, about 9% of the old 0.14 setting, with further indoor/threat attenuation. Distance is measured from each building's walls; it fades between 2 and 10 metres and stops outside that zone. Reset/Continue retain the clip but stop/mute it until gameplay reaches a building; autoplay is disabled. Sporadic birds/frogs/leaves remain. The opening's core sound comes from the four supplied files described above. Use `Tools/Invoke-Unity.ps1 -Task Ambience` to install this focused scene setting. Dialogue ducking, volume control, pause and cancellation continue to apply.

The previous layered CC0 threat/impact sources remain documented in [legacy SFX provenance](../ArtSource/Audio/ForestIntroSFX.json), but are no longer assigned to opening playback. Source peaks for the new supplied effect WAVs are capped at 0.80 before Unity mixing.
