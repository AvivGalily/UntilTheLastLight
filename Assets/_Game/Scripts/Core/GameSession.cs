using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public sealed class GameSession : MonoBehaviour
    {
        #if UNITY_EDITOR
        // Automated scene verification uses an isolated save directory.
        public static string VerificationSaveDirectory;
        #endif
        public GameTuning tuning;
        public InputActionAsset inputTemplate;
        public PlayerMotor player;
        public MonsterBrain monster;
        public Light flashlightVisual;
        public SafeLight[] shelters;
        public SafeLight[] circuitLights;
        public WorldItem[] items;
        public GameHud hud;
        public Transform cabinSpawn;
        public Transform[] monsterSpawns;
        public Transform gateLeaf;
        public TextAsset strings;
        public TextAsset hebrewStrings;
        public HorrorAudio horrorAudio;
        public ForestAmbience ambience;
        public ForestAtmosphere atmosphere;
        public MonsterPresentation creature;
        public MonsterEncounters encounters;
        public ForestEncounter forestEncounter;
        public ForestWildlife wildlife;
        public PlayerFoley foley;
        public DamageFeedback damage;
        public AxeCombat axe;
        public PlayerPresence presence;
        public PlayerDeathSequence deathSequence;
        public NortonEncounter norton;
        public ShotgunCombat shotgun;
        public ZombieWolf[] wolves=new ZombieWolf[0];
        public Localization Locale { get; private set; }
        public DisplaySettings Display {get;private set;}
        public bool HasMap { get; private set; }
        public string CheckpointLabel => Text(LastCheckpoint == "Ranger cabin" ? "checkpoint_cabin" : LastCheckpoint == "Cabin power restored" ? "checkpoint_power" : LastCheckpoint == "Prototype completed" ? "checkpoint_complete" : LastCheckpoint == "No checkpoint yet" ? "checkpoint_none" : LastCheckpoint);
        public bool mechanicsTest;
        public GameStateService State { get; private set; }
        public FlashlightModel Flashlight { get; private set; }
        public LightProtectionService Protection { get; private set; }
        public InventoryService Inventory { get; private set; }
        public PlayerCondition Condition { get; private set; }
        public DreadController Dread { get; private set; }
        public SaveService Save { get; private set; }
        public bool PowerOn { get; private set; }
        public bool Completed { get; set; }
        public bool ThreatEnabled => mechanicsTest || (started&&!Completed);
        public bool IsSafe => Protection.InShelter(player.view.transform.position);
        public int SolidMask { get; private set; }
        public float ActiveSeconds { get; private set; }
        public string Message { get; private set; }
        public string InteractionPrompt { get; private set; }
        public string LastCheckpoint { get; private set; } = "No checkpoint yet";
        public readonly HashSet<string> Documents = new HashSet<string>();
        public bool visualWarnings = true;
        public float textScale = 1;
        public string OpenDocument { get; private set; }
        public float HoldProgress { get; private set; }
        InputActionAsset actions;
        readonly Dictionary<string, InputAction> inputs = new Dictionary<string, InputAction>();
        Dictionary<string, int> initialItems;
        float messageTimer, charging;
        WorldItem heldItem;
        HeldWeapon? queuedWeapon;
        public HeldWeapon RequestedWeapon=>queuedWeapon??Inventory.Weapon;
        bool started;
        ScreenMode settingsReturn=ScreenMode.Title;
        Vector3 gateClosed;

        void Awake()
        {
            actions = Instantiate(inputTemplate);
            foreach (var action in actions) inputs[action.name] = action;
            actions.FindActionMap("UI").Enable();
            State = new GameStateService(actions);
            Flashlight = new FlashlightModel(tuning.flashlight); Inventory = new InventoryService();
            Condition = new PlayerCondition(); Dread = new DreadController();
            SolidMask = LayerMask.GetMask("WorldSolid", "LightBlocker");
            Protection = new LightProtectionService(Flashlight, player.view.transform, shelters, SolidMask);
            Protection.MinimumRepelDistance=tuning.monster.pointBlankRange;
            string saveDirectory=Application.persistentDataPath;
            #if UNITY_EDITOR
            if(!string.IsNullOrEmpty(VerificationSaveDirectory))saveDirectory=VerificationSaveDirectory;
            #endif
            Save = new SaveService(Path.Combine(saveDirectory, mechanicsTest ? "mechanics-checkpoint.json" : "checkpoint.json"));
            initialItems = new Dictionary<string, int>();
            foreach (var item in items) initialItems.Add(item.persistentId, item.remaining);
            Locale = new Localization(strings, hebrewStrings);
            Display=new DisplaySettings();
            if (gateLeaf) gateClosed = gateLeaf.localPosition;
            hud.Bind(this);
            State.Set(ScreenMode.Title);
            monster.gameObject.SetActive(false);
            if (mechanicsTest) StartNew();
        }
        public string Text(string id, params object[] args) => Locale.Get(id,args);
        public void ToggleLanguage() { Locale.SetHebrew(!Locale.Hebrew); Message=""; hud.Invalidate(); }
        public void ToggleFullscreen(){Display.Toggle();hud.Invalidate();}
        public void OpenSettings() { settingsReturn=State.Mode;State.Set(ScreenMode.Settings); }
        public void LeaveSettings() { State.Set(settingsReturn); }
        public void OpenMap() { if(HasMap) State.Set(ScreenMode.Map); else Notify(Text("map_missing"),7); }
        public void AcquireMap() { HasMap=true;Notify(Text("map_acquired"),8); }
        public bool Pressed(string name) => inputs.TryGetValue(name, out var input) && input.WasPressedThisFrame();
        bool Held(string name) => inputs.TryGetValue(name, out var input) && input.IsPressed();
        public bool SelectWeapon(HeldWeapon weapon)
        {
            if(!started||(!State.Running&&State.Mode!=ScreenMode.Inventory)||Condition.Dead||!Inventory.OwnsWeapon(weapon))return false;
            queuedWeapon=weapon;ApplyWeaponSelection();hud.Invalidate();return true;
        }
        void ApplyWeaponSelection()
        {
            if(!queuedWeapon.HasValue|| (axe&&axe.Swinging)||(shotgun&&shotgun.ReloadRemaining>0))return;
            if(Inventory.OwnsWeapon(queuedWeapon.Value))Inventory.Weapon=queuedWeapon.Value;
            queuedWeapon=null;
            // Inventory selection runs while simulation is paused: update both
            // models immediately without consuming ammunition or advancing time.
            axe?.Simulate(0,this);shotgun?.Simulate(0,this);
        }
        public void StartNew()
        {
            queuedWeapon=null;
            deathSequence?.ResetSequence();presence?.ResetPresence();
            Inventory.Restore(2, false); Flashlight.Restore(100, true); Condition.Restore(false);
            HasMap=false;horrorAudio?.ResetSequence(tuning.seed);ambience?.ResetSequence(tuning.seed);
            encounters?.ResetSequence(tuning.seed);foley?.ResetSequence(tuning.seed);damage?.ResetFeedback();axe?.ResetCombat();
            foreach(var wolf in wolves)wolf.ResetWolf(false);
            norton?.ResetEncounter();shotgun?.ResetCombat();
            ActiveSeconds = 0; Documents.Clear(); Documents.Add("note_guard_rules"); OpenDocument = "note_guard_rules";
            Completed = false; SetPower(false); Dread.Reset();
            monster.RestoreTutorial(false);
            foreach (var item in items) { item.remaining = initialItems[item.persistentId]; item.gameObject.SetActive(true); }
            player.Teleport(cabinSpawn.position, cabinSpawn.eulerAngles.y);
            atmosphere?.ResetSequence(tuning.seed,player.view.transform.position);
            SpawnMonster(false); started = true; State.Set(ScreenMode.Playing);
            forestEncounter?.ResetEncounter(false,false);wildlife?.ResetForest();
            SaveCheckpoint("Ranger cabin");
            Notify(Text("new_hint"),9);
        }
        void SpawnMonster(bool restore)
        {
            var points = new Vector3[monsterSpawns.Length];
            for (int i = 0; i < points.Length; i++) points[i] = monsterSpawns[i].position;
            Physics.SyncTransforms(); monster.Initialize(points, player, Protection, tuning.monster, restore);
        }
        public void SetPower(bool on)
        {
            PowerOn = on;
            foreach (var light in circuitLights) light.SetPowered(on);
            if (gateLeaf) gateLeaf.localPosition = gateClosed + (on ? Vector3.right * 5 : Vector3.zero);
        }
        public void ReadNote(string id)
        { Documents.Add(id); OpenDocument = id; State.Set(ScreenMode.Journal);if(id==NortonEncounter.Letter&&norton&&!norton.Unlocked){norton.ReadLetter();SaveCheckpoint("checkpoint_norton");} }
        public void Notify(string message, float seconds = 4) { Message = message; messageTimer = seconds; }
        public void Pause() { if (started && State.Running) State.Set(ScreenMode.Pause); }
        public void Resume() { if (started&&!Condition.Dead) State.Set(ScreenMode.Playing); }
        public void ReturnToTitle() { queuedWeapon=null;deathSequence?.ResetSequence();foley?.StopInjury();damage?.ResetFeedback();State.Set(ScreenMode.Title); }
        public bool ReceiveAttack(bool lethal,ZombieWolf attackingWolf=null)
        {
            if(!State.Running||IsSafe||!Condition.Hit(tuning.player,lethal))return false;
            damage?.Trigger();foley?.Hit(Condition.Dead);
            if(Condition.Dead){if(deathSequence)deathSequence.Begin(this,attackingWolf);else {State.Set(ScreenMode.Dead);Notify(Text("last_light"));}}
            else Notify(Text("struck"),5);
            return true;
        }
        public bool ReceiveHeadAttack(bool fire,CinderHead attacker)
        {
            if(!State.Running||!(fire?Condition.HitFire():Condition.Hit(tuning.player)))return false;
            damage?.Trigger();foley?.Hit(Condition.Dead);
            if(Condition.Dead)
            {
                if(attacker&&deathSequence&&(!fire||Vector3.Distance(attacker.transform.position,player.transform.position)<3))deathSequence.Begin(this,null,attacker);
                else{norton?.SilenceEnemies();State.Set(ScreenMode.Dead);Notify(Text("fire_death"));}
            }
            else Notify(Text(fire?"fire_hit":"struck"),7);
            return true;
        }
        public void SwapFromInventory()
        { Notify(Text(Flashlight.Faulted?"repair_house_only":Flashlight.BeginSwap(Inventory, true) ? "fresh" : "no_spares")); }
        public void SaveCheckpoint(string name)
        {
            if (mechanicsTest) return;
            var pickupData = new List<ItemRecord>();
            foreach (var item in items)
                if (item.kind == ItemKind.Batteries || item.kind == ItemKind.Fuse || item.kind==ItemKind.Axe || item.kind==ItemKind.Shotgun || item.kind==ItemKind.Shells)
                    pickupData.Add(new ItemRecord { id = item.persistentId, remaining = item.remaining });
            var data = new SaveData {
                checkpoint = name, activeSeconds = ActiveSeconds, charge = Flashlight.Charge,
                flashlightOn = Flashlight.SwitchedOn, batteries = Inventory.Batteries, fuse = Inventory.HasFuse,
                powerOn = PowerOn, wounded = Condition.PhysicalWound, completed = Completed,fireRecovery=Condition.FireRecoveryRemaining,
                shotgun=Inventory.HasShotgun,loadedShells=Inventory.LoadedShells,spareShells=Inventory.SpareShells,weapon=(int)Inventory.Weapon,
                headUnlocked=norton&&norton.Unlocked,headIntroduced=norton&&norton.Introduced,
                mapAcquired = HasMap,axe=Inventory.HasAxe,
                forestEncounterSeen=forestEncounter&&forestEncounter.Seen,flashlightFaulted=Flashlight.Faulted,
                deadWolves=Array.ConvertAll(Array.FindAll(wolves,w=>!w.Alive),w=>w.persistentId),
                documents = new List<string>(Documents).ToArray(), pickups = pickupData.ToArray(), tutorialRepelled = monster.EverRepelled
            };
            try { Save.Write(data); LastCheckpoint = name; }
            catch (Exception e) { Notify(Text("save_error"),10); Debug.LogException(e); }
        }
        public void Continue()
        {
            try
            {
                var data = Save.Load(out string message);
                // Validate every ID before applying anything to the live scene.
                foreach (var record in data.pickups)
                    if (!initialItems.ContainsKey(record.id) || record.remaining > initialItems[record.id]) throw new InvalidDataException("Unknown pickup in checkpoint.");
                foreach(var id in data.deadWolves)if(!Array.Exists(wolves,w=>w.persistentId==id))throw new InvalidDataException("Unknown wolf in checkpoint.");
                State.Set(ScreenMode.Pause);
                queuedWeapon=null;
                deathSequence?.ResetSequence();presence?.ResetPresence();
                Inventory.Restore(data.batteries, data.fuse,data.axe); Flashlight.Restore(data.charge, data.flashlightOn);
                Inventory.RestoreWeapon(data.shotgun,data.loadedShells,data.spareShells,(HeldWeapon)data.weapon);
                Condition.Restore(data.wounded,data.fireRecovery); SetPower(data.powerOn); Completed = data.completed;
                norton?.ResetEncounter(data.headUnlocked,data.headIntroduced);shotgun?.ResetCombat();
                HasMap=data.mapAcquired;horrorAudio?.ResetSequence(tuning.seed);ambience?.ResetSequence(tuning.seed);
                encounters?.ResetSequence(tuning.seed);foley?.ResetSequence(tuning.seed);damage?.ResetFeedback();axe?.ResetCombat();
                foreach(var wolf in wolves)wolf.ResetWolf(Array.IndexOf(data.deadWolves,wolf.persistentId)>=0);
                ActiveSeconds = data.activeSeconds; LastCheckpoint = data.checkpoint; Dread.Reset();
                Documents.Clear(); foreach (var id in data.documents) Documents.Add(id);
                if(!Documents.Contains(OpenDocument??""))OpenDocument=Documents.Contains("note_guard_rules")?"note_guard_rules":null;
                foreach (var item in items)
                {
                    item.remaining = initialItems[item.persistentId]; item.gameObject.SetActive(true);
                    foreach (var record in data.pickups) if (record.id == item.persistentId) { item.remaining = record.remaining; item.gameObject.SetActive(record.remaining > 0); }
                }
                player.Teleport(cabinSpawn.position, cabinSpawn.eulerAngles.y);
                atmosphere?.ResetSequence(tuning.seed,player.view.transform.position);
                monster.RestoreTutorial(data.tutorialRepelled);
                if (!Completed) SpawnMonster(true); else monster.gameObject.SetActive(false);
                if(data.flashlightFaulted)Flashlight.FailCircuit();
                forestEncounter?.ResetEncounter(data.forestEncounterSeen,data.flashlightFaulted);wildlife?.ResetForest();
                started = true; State.Set(Completed ? ScreenMode.Complete : ScreenMode.Playing); Notify(Text(message.Contains("backup") ? "load_backup" : "load_ok"),6);
            }
            catch (Exception e) { Notify(Text("load_error"),12);Debug.LogWarning(e.Message); }
        }
        void Update()
        {
            if (State == null) return;
            if(Pressed("Fullscreen"))ToggleFullscreen();
            Display.Tick();
            if(State.Mode==ScreenMode.Dying){deathSequence.Simulate(Time.deltaTime);hud.Refresh();return;}
            if(Pressed("Inventory")&&started&&!Condition.Dead)
            {
                if(State.Running)State.Set(ScreenMode.Inventory);
                else if(State.Mode==ScreenMode.Inventory)Resume();
                hud.Refresh();return;
            }
            if (Pressed("Back"))
            {
                if (State.Running) Pause();
                else if (State.Mode != ScreenMode.Title && State.Mode != ScreenMode.Dead && State.Mode != ScreenMode.Complete) Resume();
            }
            if (!State.Running) { hud.Refresh(); return; }
            if (Pressed("Journal")) State.Set(ScreenMode.Journal);
            else if (Pressed("Map")) OpenMap();
            if (!State.Running) { hud.Refresh(); return; }
            // One ordered simulation coordinator. Light snapshot follows movement; AI follows light.
            float dt = Time.deltaTime;
            ActiveSeconds += dt; messageTimer -= dt; if (messageTimer <= 0) Message = "";
            player.Simulate(tuning.player, inputs["Move"].ReadValue<Vector2>(), inputs["Look"].ReadValue<Vector2>(), Held("Sprint"), Held("Crouch"), dt);
            presence?.Simulate(dt,player);
            Physics.SyncTransforms();
            if (Pressed("Flashlight")) Flashlight.Toggle();
            if(inputs.TryGetValue("CycleWeapon",out var wheel)&&Mathf.Abs(wheel.ReadValue<float>())>.01f)
                SelectWeapon(Inventory.NextWeapon(RequestedWeapon));
            if(Pressed("EquipAxe"))SelectWeapon(HeldWeapon.Axe);
            if(Pressed("EquipShotgun"))SelectWeapon(HeldWeapon.Shotgun);
            ApplyWeaponSelection();
            if(Pressed("Reload")&&Inventory.ShotgunEquipped)shotgun.BeginReload(this);
            if(Pressed("BatterySwap")&&!Flashlight.BeginSwap(Inventory))Notify(Text(Flashlight.Faulted?"repair_house_only":"no_spares"));
            Flashlight.Tick(dt, Held("Boost"));
            flashlightVisual.enabled = Flashlight.Active;
            flashlightVisual.range = Flashlight.Range;
            flashlightVisual.spotAngle = tuning.flashlight.outerHalfAngle * 2;
            flashlightVisual.innerSpotAngle = tuning.flashlight.innerHalfAngle * 2;
            flashlightVisual.intensity = Flashlight.Intensity * 3;
            Condition.Tick(dt, IsSafe, tuning.player);
            damage?.Simulate(dt);foley?.Simulate(dt,this);
            if(Pressed("Attack")){if(Inventory.ShotgunEquipped)shotgun?.Fire(this);else axe?.BeginSwing(this);}axe?.Simulate(dt,this);shotgun?.Simulate(dt,this);
            Interact(dt);
            if (!State.Running) { hud.Refresh(); return; }
            forestEncounter?.Simulate(dt,this);wildlife?.Simulate(dt,this);
            encounters?.Simulate(dt,this);if(!forestEncounter||!forestEncounter.Choreographing)monster.Tick(dt, this);
            if(!State.Running){hud.Refresh();return;}
            if(!forestEncounter||!forestEncounter.Choreographing)
                foreach(var wolf in wolves){wolf.Simulate(dt,this);if(!State.Running){hud.Refresh();return;}}
            norton?.Simulate(dt,this);if(!State.Running){hud.Refresh();return;}
            Dread.Tick(dt, this);
            if(!forestEncounter||!forestEncounter.Choreographing)creature?.Simulate(dt,monster);
            if(!forestEncounter||(!forestEncounter.Choreographing&&forestEncounter.Phase!=ForestEncounterPhase.Refuge))horrorAudio?.Simulate(dt,this);
            ambience?.Simulate(dt,this);atmosphere?.Simulate(dt,this);
            if (Condition.Dead) { State.Set(ScreenMode.Dead); Notify(Text("last_light")); }
            hud.Refresh();
        }
        void Interact(float dt)
        {
            WorldItem target = null;
            if (Physics.Raycast(player.view.transform.position, player.view.transform.forward, out var hit, tuning.player.interactionRange,
                SolidMask | LayerMask.GetMask("Interactable"), QueryTriggerInteraction.Collide)) target = hit.collider.GetComponentInParent<WorldItem>();
            InteractionPrompt = target ? target.Prompt(this) : "";
            if (target && Pressed("Interact") && target.kind != ItemKind.EmergencyCharger) target.Use(this);
            if (target && target.kind == ItemKind.EmergencyCharger && Held("Interact") && IsSafe && Flashlight.Charge < tuning.flashlight.emergencyCharge
                &&(!Flashlight.Faulted||(target.houseCharger&&ForestLayout.InRefugeHouse(player.transform.position))))
            {
                if (heldItem != target) charging = 0;
                heldItem = target; charging += dt; HoldProgress = charging / tuning.flashlight.emergencySeconds;
                if (charging >= tuning.flashlight.emergencySeconds)
                {
                    if(Flashlight.Faulted)Flashlight.RepairAtHouse(target.houseCharger&&ForestLayout.InRefugeHouse(player.transform.position));else Flashlight.EmergencyFill();
                    charging=0;Notify(Text("charge_complete"));
                }
            }
            else { heldItem = null; charging = HoldProgress = 0; }
        }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
        void OnDestroy() { State?.Dispose(); if (actions) Destroy(actions); }
    }
}
