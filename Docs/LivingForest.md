# Living forest and ambush — 0.9.0

In 0.9.0 the adult is stuck in an original modeled silk web, with 14 bone-attached tethers and seven visible cocoon wraps. Chest/head bruises, blood and cuts are present before the encounter. At the existing pounce impact the inner web tears once, the tethers snap over 0.65 active seconds, and the victim falls through the existing 0.85-second choreography. The torn outer rim stays on the tree; feeding, blood, blackout, pursuit and repair remain connected. Pause freezes the bindings and snap timer; loading a completed-event checkpoint keeps the core torn, and New Game restores the intact web and initial injuries.

The forest now uses two Poly Haven fir variants, three scanned mossy rocks, a
scanned fallen trunk and stump, and photographed bark, soil, trail, weathered
wood and rust surfaces. Source UVs and alpha foliage are preserved. The fir
source stores its UVs as a three-component corner attribute; the Blender adapter
explicitly converts it to an FBX UV layer. Each environmental model has three
distance LODs. Credits and source metadata are in `ThirdPartyNotices.md` and
`ArtSource/Vendor/polyhaven`.

`ForestWildlife` applies phase-offset, gentle wind to the visual tree children.
Trunk colliders remain stationary. Twenty-four original feathered crows occupy
branches beside the trails. They stagger their takeoff when approached within
8.5 metres, flap their wings, caw, climb away and disappear beyond the local area.
New Game/Continue resets the wildlife. Pausing freezes wind and flight.

## The old oak

The oak stands beside the first trail, near (-56, -70). A clothed adult is
bound in spider silk beside the branch. A first approach within 10.5 metres,
looking toward the victim with clear sight, triggers the sequence. It waits if
the ordinary monster is currently visible, avoiding an on-screen relocation.
The event owns the same monster used by normal gameplay; there is no duplicate.

1. An English synthetic male voice calls for help; Hebrew/English subtitles
   appear. The weighted victim skeleton moves while suspended.
2. After 2.2 seconds the creature leaps along a 0.85-second arcing trajectory.
   The scare sting and a recorded human scream coincide with the leap.
3. The silk tears, the victim drops, the creature braces its limbs and feeds
   for 6.5 seconds. Its head and jaw move with repeated impact sounds, blood
   droplets, surface wounds and a growing ground pool. Player look/movement
   remain available. Wolves do not interrupt this short choreography.
4. The flashlight shorts out and the creature resumes real NavMesh pursuit.
   Ordinary encounter respawns and hunt timeouts cannot interrupt this chase.
   The score rises with elapsed time/proximity; the heartbeat accelerates.
5. Enter the **ranger cabin or east storehouse**. Music and heartbeat stop on
   crossing into the house interior. The amber shelter protects the player.
   Hold **E for 12 seconds** at the indoor charger to restore 35% charge and
   operation. Spare cells, F, boost and the outdoor charger cannot clear this
   circuit fault. The research building retains its nonprotective weak bulb.

The monster can kill a player who fails to escape using the existing warned
attack, scream, blood overlay and death window. A player who runs early can
reach shelter before feeding ends. No forced camera turn or movement lock is
used. The full campaign and final cinematic/sound production remain separate.

## Persistence and reproduction

Schema 4 adds `forestEncounterSeen` and `flashlightFaulted`; saves 1–3 migrate.
The ambush completion, refuge arrival and repaired lamp create checkpoints.
Continuing a blackout checkpoint starts safely in the cabin with the victim
already dead and the flashlight still requiring its indoor repair. The scene
does not replay after that checkpoint. New Game restores the living victim and
the working lamp. Quitting before the feeding sequence finishes rewinds to the
previous checkpoint, as with the rest of the checkpoint system.

Run `python Tools/download_forest_assets.py` once to retrieve/check the CC0
inputs. Run `Tools/Invoke-Blender.ps1` to regenerate/export all assets; its final
step restores the photographed PBR surfaces after the original base generator.
`Tools/generate_encounter_voice.ps1` reproduces the David SAPI line on Windows;
`python Tools/generate_encounter_audio.py` produces the original music/effects.
`python Tools/generate_strings.py` includes the new matching HE/EN strings.
Then use `Tools/Invoke-Unity.ps1 -Task Generate` and the existing verification
tasks. The checked-in WAVs and FBXs require neither Blender nor speech services
at game runtime.

The scene smoke tests wind/flight, the actual cinematic phases, pause, blackout,
exclusive monster ownership, a motor-driven escape with NavMesh pursuit,
immediate score silence, persistent repair, an idle player's death and recovery.
Snapshots 23–28 in `Docs/Screenshots` are rendered in Unity.

## Version 0.7 contact and body motion

The victim has three authored skeletal clips: `Victim_Hanging`, `Victim_Overpowered` and `Victim_Dying`. Silk release blends raised wrists into asymmetric arms and bent legs; the root accelerates through a 0.85-second fall, settles on the ground and briefly convulses. Feeding lowers the creature into a brace and aligns its mouth with the moving chest bone. The mouth opens in anticipation, closes with the `Jaw_Bite` morph, and pulls back before the next bite. Blood and impact sound occur at this chest contact; wounds follow the chest/head skeleton as it falls. This remains an authored sequence with manual IK. Pause, player movement, one-time persistence, blackout, pursuit and repair keep their previous behavior.
