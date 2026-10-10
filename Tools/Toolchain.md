# Locked toolchain

- Unity Editor: 6000.3.24f1 (`4e7b9b5b6244`), Windows, Direct3D 11.
- Installed editor: `C:\dev\UnityEditors\6000.3.24f1\Editor\Unity.exe`.
- Blender: 5.2.2 LTS (`d13f752e3b9c`), installed at `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
- URP: 17.3.0. Input System: 1.14.2. AI Navigation: 2.0.9. Test Framework: 1.4.6. uGUI: 2.0.0.
- Blender's bundled Python and NumPy create the meshes and most texture maps. The cabin-sign source-map packing step uses system Python 3.12 and Pillow, already installed on this authoring machine; gameplay has no Python dependency.
- Source assets use metres. FBX preset: forward `-Z`, up `Y`, unit conversion enabled. Unity preserves each imported FBX's conversion transform under a separate placement root. Cabin axes and tabletop/pickup alignment are checked by the editor validator.
- PBR texture maps are explicitly remapped to URP Lit materials. Normal/packed mask textures import as linear data. Metallic is R, occlusion is G, smoothness is A in the mask.
- Alpha-clipped foliage imports with coverage-preserving mipmaps at the material's 0.42 cutoff so thin needles stay visible at distance.
- Visual capture uses `RenderPipeline.SubmitRenderRequest` with a URP `SingleCameraRequest` and an sRGB render target. HDR lighting, fixed exposure, ACES, restrained bloom and vignette are enabled in the forest profile.

The local verification machine reports an Intel Iris Xe GPU. The 1080p/60 FPS target in the GDD has not been benchmarked or accepted on reference hardware.

For the 0.10 presentation refresh, `Tools/Run-BlenderStep.ps1` wraps a hidden Blender process and retains stdout/stderr logs. Run `player_presence.py`, then focused `export_assets.py` steps for `player_body_01` and `player_axe_hand_01`. `wolf_motion.py` refreshes the retained wolf clips before exporting `zombie_wolf_01`. `python Tools/generate_death_audio.py` reproduces the new fatal voice, tearing and sting from retained sources. Full art authoring includes the player and revised wolf steps. Gameplay requires none of these tools. Smoke captures the player grip/shadow, fatal mouth contact and wolf collapse.

Cabin plaques can be refreshed independently: run `python Tools/prepare_cabin_sign.py`, Blender with `--background --disable-autoexec --python-exit-code 1 --python Tools/Blender/cabin_sign.py -- --project C:\dev\UntilTheLastLight`, then the same Blender arguments with `export_assets.py` and `--asset cabin_sign_01`. Generate/Validate the Unity scenes. `Tools/Invoke-Unity.ps1 -Task SignCapture` produces six actual Unity views, one per building/language, and `-Task Smoke` verifies their mounting, one-line fit and live language changes.

Primary API references: [Unity URP tonemapping](https://docs.unity.cn/Packages/com.unity.render-pipelines.universal%4017.0/manual/post-processing-tonemapping.html), [Blender command-line arguments](https://docs.blender.org/manual/en/latest/advanced/command_line/arguments.html). The installed package source was used to check the exact pinned URP API.

The 0.9 mouth refresh first runs `python Tools/prepare_crawler_mouth.py`, then Blender `crawler_mouth_material.py -- --project C:\dev\UntilTheLastLight`; the focused exporter uses `--asset walker_01`. `victim_web.py` and `--asset victim_web_01` independently reproduce the web. Use the same Blender background/disable-autoexec/python-exit-code flags described above. The full art wrapper includes both authoring steps. Scene Smoke validates web release and checkpoint state; CreatureCapture renders opening/contact views and the weighted crawl with the new mouth maps. The retained source artwork means reproduction needs no image service or API key.
