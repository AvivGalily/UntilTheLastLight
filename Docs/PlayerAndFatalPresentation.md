# Player presence, pursuit handoff and fatal presentation — 0.10.0

The player now has a clothed anatomical body that casts a human shadow under the moon and nearby lights. It uses `ShadowsOnly`, preventing the first-person camera from entering its head or chest. The visible axe hand is extracted from the project's CC0 MakeHuman anatomy, including forearm, palm, nails and five posed fingers. The extraction retains the complete forearm descendant chain, including the metacarpal palm bones. It is attached to the axe's placement root, so the same swing moves both grip and blade. Existing skin/clothing UVs and textures are retained. Restrained local fill lights make the hand and fatal mouth readable; they are visual lights without shelter/protection components.

The ambush releases the Walker at a clear, connected navigation point beside the feeding contact. A cinematic contact pose can shift its wide capsule off the walkable surface; the handoff now checks standing collision clearance, a complete path to the player and a legal first step. If unavailable, the escape owner retries instead of leaving an airborne or blocked navigation origin. Initial and distant placements also require clear standing capsules. Ordinary path selection checks physical capsule sweeps as well as light protection; a blocked moving pursuer can take short legal lateral steps to clear a corner. This does not bypass solids, powered shelter or flashlight protection.

A successful lethal strike now enters `Dying` before `Dead`. Gameplay actions and ordinary AI stop while a single presentation owner controls the camera and attacker. The camera drops toward the ground, hands rise defensively, and the Walker braces its front limbs beside the player and brings its animated mouth close. Repeated bites combine jaw motion, small camera impacts, blood droplets, lens blood, a scream and new guttural/tearing layers. The death menu appears after 5.6 seconds. New Game, Continue and Title restore the camera/equipment and clear the cinematic sounds/particles. This is an authored interaction pose, not a general ragdoll solver.

The wolf's attack has a crouched anticipation, leg push-off, a short collision-checked committed lunge, jaw closure at the existing 0.60-second damage contact, and a longer recovery. Moving away or taking cover can still make it miss. Clip transitions blend rather than switching abruptly. Death includes a shock reaction, articulated side collapse, ground contact, damped limb/head movement and a held settled pose; dead checkpoint restoration samples that final pose. No new external character asset or skin atlas is required.

## Visual references inspected

These are publisher-posted promotional footage, reviewed through sampled frames for animation/composition. The samples are authoring references in `Logs/HorrorReferences`, outside game assets and the build.

- [Amnesia: The Bunker, Frictional Games' Steam page](https://store.steampowered.com/app/1944430/Amnesia_The_Bunker/), **Launch Trailer**: hand-held equipment and visible fingers in the gameplay shots; near-ground hands around the late trailer sequence. These informed a bodily first-person camera and visible defensive arms.
- [Resident Evil Village, Capcom's Steam page](https://store.steampowered.com/app/1196590/Resident_Evil_Village/), **Village_PV3_Multi_05_PEGI-EN_FullHD**: the close enemy reaching toward the armed camera and later face-filling character shot informed readable proximity and contact framing. The current predator feeding choreography is original project animation.

Source titles and exact publisher video URLs are retained in `Logs/HorrorReferences/sources.json`. No models, textures, animations or sound from those games were imported.

## Reproduction and verification

Run `Tools/Run-BlenderStep.ps1 -Script Tools/Blender/player_presence.py -LogName PlayerPresence`, then focused `export_assets.py` steps for `player_body_01` and `player_axe_hand_01`. `wolf_motion.py` refreshes the retained wolf's attack/death actions; focused export uses `--asset zombie_wolf_01`. Full authoring includes these changes. `python Tools/generate_death_audio.py` recreates the new mono clips from retained CC0 voices and deterministic original synthesis.

Generate the Unity scenes and run the EditMode/PlayMode suites and Smoke. Player presentation, fatal sequence, wolf collapse and actual post-feeding movement in three directions are checked in the integration smoke; captures 39–44 record the visible result. `TestReport.md` records the final results and practical limits.
