# Until the Last Light — playable forest prototype

**פתיחת היער:** ״התחלת המסע״ מפעילה כעת נסיעה בגוף ראשון באורך 110 שניות עם שלושה ציידים, מעבר בשער, איום ופיצוץ, ואז התעוררות בבקתה הקיימת. A/D, חצים או ציר שמאל בבקר שולטים בהיגוי; Escape / Start משהים. לאחר צפייה מלאה אפשר לדלג בהחזקת רווח / A בבקר למשך שתי שניות. ״המשך״ טוען את השמירה ישירות. בהגדרות ניתן לכבות כתוביות ולהפחית את ההבזק. כל 12 המשפטים מדובבים כעת באנגלית בשלושה קולות עצביים שונים, גם כאשר הכתוביות בעברית. [האזנה לשיחה](Audio/ForestOpening_Dialogue_Preview.wav). ראו [ForestOpening.md](ForestOpening.md) לפרטי חיבור, נכסים ובדיקות.

**0.13.0:** Use the **mouse wheel** to switch axe/shotgun, or **TAB** to choose an owned weapon in the pack. TAB also closes it. The original fire-head frequency is restored: up to two active, with 42–60-second recurrence (48 after the introduction, 18 after load). The forest now contains **nine wolves**, up from five. First-person hands have smoother anatomy, a separate trigger pose, fitted cuffs/sleeves, detailed living skin and continuous UV-seam lighting. The Walker still retreats into mist and returns after 10–15 seconds; head music, collapse/burn/disappearance remain active.

**עברית — 0.13.0:** הגלגלת מחליפה בין גרזן לרובה ציד שנאספו. TAB פותח וסוגר את התיק, ובו כפתור לבחירת כל נשק. תדירות היצור שיורק אש הוחזרה: עד שניים פעילים ומרווחים של 42–60 שניות. נוספו ארבעה זאבים, וביער תשעה בסך הכול. הידיים שופרו עם אצבעות חלקות יותר, אחיזת הדק נפרדת, עור חי ושרוולים עם חפתים וקפלים.

In **0.13.0**, use **1** for the axe and **2** for the shotgun; both the number row and numpad work. A selection made during an axe swing or reload waits until that action finishes. **R or B** changes the flashlight battery with either weapon, including during a shotgun reload; **T** reloads the shotgun. The flashlight stays in the left hand and the shotgun in the right. Find the burning car beside the **research station**, with the wounded human casualty, shotgun and letter to Mr. Norton. Closing the letter introduces a dead human head crawling on six articulated legs. These creatures pursue you in the forest and in lit buildings; the flashlight does not repel them. An aimed shotgun blast or two axe hits kill one. A first fireball causes a burn that heals after ten seconds of active play; a second fireball during recovery is fatal. The HUD displays the recovery timer. The player's shadow matches the standing/crouching body, and the personal flashlight does not cast that body into its beam. Anatomical equipment arms continue into the body below the view.

**עברית — אחיזה בנשק:** כשבוחרים ברובה הציד, הפנס נשאר ביד שמאל והרובה מוחזק ביד ימין. שתי הידיים נראות גם בזמן ירי וטעינה.

**עברית — מקשי הנשק והפנס:** **1** לגרזן, **2** לרובה הציד, בשורת המספרים או בלוח הספרות. לחיצה במהלך מכה או טעינה נשמרת ומתבצעת בסיום הפעולה. **R או B** מחליפים סוללת פנס עם כל נשק, גם במהלך טעינת הרובה. **T** טוען את הרובה.

**עברית:** לצד **תחנת המחקר** חפשו את הרכב הבוער, הגופה, רובה הציד והמכתב לנורטון. הסצנה הועברה במלואה מבקתת השומר. סגירת המכתב מפעילה את הופעת המפלצת החדשה. האור אינו מגן מפניה, גם בבתים. ירייה מכוונת או שתי מכות גרזן הורגות אותה. אחרי כדור אש ראשון צריך לשרוד עשר שניות עד להחלמה; פגיעה נוספת בזמן הזה הורגת. הצל מותאם למידות הגוף ולתנוחת הכריעה; הפנס האישי אינו מטיל אותו קדימה, והזרועות ממשיכות אל הגוף מחוץ לשדה הראייה.

In **0.10.0**, the player casts a human shadow and holds the axe with a textured anatomical hand. The Walker resumes navigation after feeding instead of remaining stuck at the victim. A lethal attack now shows a 5.6-second close feeding sequence with camera fall, defensive hands, blood and new creature/tearing sounds before the death menu. Wolf bites include a short committed lunge and recovery; deaths show an articulated side collapse and settling.

In **0.9.0**, the injured adult by the old oak is caught in spider silk. When the creature pounces, the silk tears and he falls into the existing feeding sequence; the subsequent flashlight failure and escape remain. The creature has new decayed-enamel and wounded-lip textures with pointed teeth and a smoother bite closure.

In **0.8.1**, all three buildings have modeled wooden entrance plaques with yellow name-only lettering. Each plaque displays Hebrew or English according to the chosen language: **בקתת השומר / RANGER CABIN**, **מחסן מזרחי / EAST STOREHOUSE**, **תחנת מחקר / RESEARCH STATION**. The languages are shown separately and update immediately in Settings.

Open `Builds/Windows/UntilTheLastLight.exe` and choose **התחלת המסע / Begin the journey**. Hebrew is the default; change language on the title screen or in Settings. The choice is remembered.
Keep the executable alongside `UntilTheLastLight_Data`, `UnityPlayer.dll` and the other build files.

This is the **0.12.0** M2 forest slice. Restore the ranger cabin's circuit with the fuse from the east storehouse, then use the north gate. The four-cabin campaign and revolver finale are still pending.

The playable area is now **280×280m**, compared with 120×120m previously. Search the surveyor's body beside the first bend north of the starting cabin with **E** to acquire the map. **M** opens it only after discovery. The four trail bodies and the new wreck casualty carry documents that remain available in **J**. The sketch marks the existing forest landmarks, protective lantern and exit, with no live position marker.

The abandoned research station has three rooms and a corridor. Its dim bulb **does not repel the creature**. The spider-human creature opens its tooth-filled jaw as it approaches; its eight legs move with its gait. Directional clicks and a rising drone/pulse follow real proximity. Distant wolf howls occur occasionally; visual warnings include a translated caption.

**עברית:** חפשו את גופת המודד ליד הפנייה הראשונה מצפון לבקתה ולחצו **E**. לאחר איסוף המפה אפשר לפתוח אותה עם **M**. המסמכים שנמצאו זמינים ביומן עם **J**. האור החלש בתחנת המחקר אינו מקום בטוח. שינוי השפה זמין בתפריט הראשי ובהגדרות.

## Controls

- WASD / mouse: move / look. Shift: run. Ctrl: crouch.
- F: flashlight. Hold right mouse: boost. R or B: replace a flashlight battery with either weapon.
- 1: axe. 2: shotgun (number row or numpad). Left mouse: attack. T: reload the shotgun's two barrels in 1.7 seconds. A busy weapon finishes its swing/reload before a queued selection takes effect. Initial ammunition is 2 loaded + 12 spare; extra cartridge boxes are in the buildings.
- E: interact. Hold E at an emergency charger: recover to 35% in 12 seconds.
- E beside the first corpse: take the axe. Left mouse: swing; two hits kill an undead wolf.
- Tab: inventory. M: map. J: journal. Esc: pause.

Inventory, map, journal and menus pause the simulation. The beam protects its direction **only beyond 2.8 metres**. Close Walker strikes are fatal, including during the first encounter. Keep your distance and retreat to amber shelters. Five undead wolves can be killed with the axe; the axe does not damage the Walker. A wolf bite wounds you; a second bite before recovery kills. The research bulb remains nonprotective.

**עברית — הקרב:** הגרזן נמצא לצד גופת המודד הקרובה לבקתה. **E** לאיסוף, **לחצן עכבר שמאלי** למכה. שתי פגיעות הורגות זאב. כשהמפלצת נמצאת פחות מ־2.8 מטרים ממך, הפנס לא מרחיק אותה. תקיפה שלה מפעילה צעקה, דם על המסך ומסך סוף משחק עם אפשרות להמשיך מנקודת שמירה או להתחיל מחדש.

Checkpoint files live under `%USERPROFILE%\AppData\LocalLow\Blackpine Studio\Until the Last Light`. The automated editor smoke test uses a separate temporary directory.

## Open in Unity

Use Unity **6000.3.24f1** and open the project folder. Open `Assets/_Game/Generated/Scenes/ForestGame.unity`, then press Play. `MechanicsTest.unity` remains the isolated light/occlusion test scene.

Source models are in `ArtSource/Blender`. Unity imports the explicit FBX exports in `Assets/_Game/Art/Meshes`; Blender is not required to play the build.

## Reproduce the art, scenes and build

Run PowerShell from the project folder with the Unity editor closed:

```powershell
python .\Tools\fetch_character_sources.py
.\Tools\Invoke-Blender.ps1
python .\Tools\generate_strings.py
python .\Tools\generate_audio.py
python .\Tools\generate_night_audio.py
python .\Tools\generate_combat_audio.py
.\Tools\Invoke-Unity.ps1 -Task Generate
.\Tools\Invoke-Unity.ps1 -Task Capture
.\Tools\Invoke-Unity.ps1 -Task CreatureCapture
.\Tools\Invoke-Unity.ps1 -Task Smoke
.\Tools\Invoke-Unity.ps1 -Task EditTests
.\Tools\Invoke-Unity.ps1 -Task PlayTests
.\Tools\Invoke-Unity.ps1 -Task Build
```

Pass `-Unity` or `-Blender` when the installed executable is in another folder. Scene/prefab regeneration writes only under `Assets/_Game/Generated`. Keep authored scene changes outside that folder. Existing generated asset GUIDs are preserved by the editor scripts.

`Capture` produces fifteen actual Unity URP views in `Docs/Screenshots`, including the creature with closed/open jaws, four detailed bodies and a face close-up, research rooms and dense forest fog. It requires a graphics device. `Smoke` also captures four Hebrew/English title/document/map views and checks map discovery/persistence, all room paths, weak-light safety, near-creature animation/audio, wolf/wildlife playback, frozen fog on pause and the existing quest/save flow. It does not establish a new player's completion time.

See `Progress.md` for implemented and missing GDD requirements, `TestReport.md` for verification, and `AssetManifest.json` for asset provenance and geometry budgets.

The monster was rebuilt from the supplied visual reference as a fleshy, low crawling humanoid with a weighted Blender skeleton. `CreatureCapture` produces model views and sampled crawl frames in `Docs/Screenshots/Creature`; see `CreatureArt.md` for the source rig, clips and regeneration instructions.

Version 0.3.1 adds a human epidermis texture, a 4K rest-pose skin atlas, fine pore
normals and variable roughness. `CreatureArt.md` also documents the focused skin
refresh command; it uses the retained source image and needs no API key.

Version 0.4.0 adds night birds, a cricket bed, frogs and rustling leaves, with
quieter/muffled indoor ambience and reduced wildlife volume during danger.
Dense grey fog and slowly drifting airborne wisps reduce distant visibility.
Four detailed dressed human models replace the corpse proxies; map/document
controls are unchanged. See `AtmosphereAndBodies.md` for sources and reproduction.

Version 0.5.0 replaces those bodies with licensed MakeHuman anatomy and UV skin,
clothing and hair, adds blood, recurring distant Walker encounters, movement-based
steps and exertion breathing, point-blank lethal attacks and a blood/death screen.
Five animated undead wolves and the recoverable axe are connected to combat and
checkpoints. See `CombatAndCharacters.md` and `ThirdPartyNotices.md` for sources,
rules and the focused rebuild. `Smoke` also verifies the entire combat flow.


Version 0.6.0 adds scanned forest art, wind and fleeing crows. The first trail contains a surprise encounter. After the light fails, **Shift + WASD** runs to the ranger cabin or east storehouse. Enter the house to stop the chase music, then **hold E for 12 seconds** at its charger. F, R and the outdoor charger cannot reset a failed circuit. The research station's weak bulb remains unsafe. The new scene is available on a fresh game or an older checkpoint that has not played it; an already completed encounter does not repeat. See `LivingForest.md` for the complete reproduction and persistence behavior.

Version 0.7.0 starts in borderless full screen. **F11**, or **Settings → Display**, switches between full screen and a resizable window; the choice survives restarting. The axe cutting edge faces forward. Wolves howl before a distant approach, growl closer, and finish a jaw-closing bite before recovery. Their head is half exposed skull, with textured fur, eyes and bone. The Walker closes its lips/teeth on a bite; the ambush victim has separate struggle/fall/dying animation, chest contact and blood emitted at that contact. Start a new game to watch the revised one-time scene if it already occurred in your checkpoint.

Version 0.7.1 gives the wolf's exposed skull and jaw a detailed aged-bone surface with pores, small cracks, dirt and dried blood, plus baked normal and variable roughness maps. The texture also appears when continuing an existing checkpoint.

Version 0.8.0 adds approaching wolf barks that become louder and quicker nearby, plus a return roar and guttural Walker voices. The Walker follows your current position outside shelter, can disappear out of sight for a few seconds and return from a distant reachable point. Menus freeze the sequence. The existing safe houses, flashlight repulsion and close-range danger still apply.

The revised opening uses all twelve supplied English dialogue recordings and the supplied vehicle ambience, gate, approaching creature and explosion, replacing the previous opening audio. Complete final warnings finish before the blast. `night_crickets.wav` plays very quietly only around the cabin, storehouse and research station during gameplay, fading to silence 10m from the walls and becoming quieter indoors. It never plays during the opening. Healthy distinct hunters match later forest bodies. **Mouse / controller right stick** looks around the car; both rear passengers are visible in the enlarged mirror. Start a new game to watch it. To reinstall supplied opening audio, run `python Tools/import_intro_audio.py --source ArtSource/Audio/Runway`, then `Tools/Invoke-Unity.ps1 -Task IntroAudio`. Use `-Task Ambience` for the quiet building cricket setup. See `ForestOpening.md`.
