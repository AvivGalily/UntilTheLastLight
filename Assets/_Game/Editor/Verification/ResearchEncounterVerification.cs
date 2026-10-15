using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Editor
{
    public static class ResearchEncounterVerification
    {
        static void Require(bool value,string message){if(!value)throw new Exception("Research encounter: "+message);}
        public static void Run()
        {
            SessionState.SetBool("LastLight.ResearchEncounterVerify",true);SessionState.SetString("LastLight.ResearchEncounterSave",Path.Combine(Path.GetTempPath(),"ResearchEncounter-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");Attach();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("LastLight.ResearchEncounterVerify",false))Attach();}
        static void Attach(){GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.ResearchEncounterSave",null);EditorApplication.update-=Step;EditorApplication.update+=Step;}
        static void Step()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!game||game.State==null)return;
            EditorApplication.update-=Step;int exit=0;
            try{game.enabled=false;game.intro.enabled=false;game.StartNew();game.hud.gameObject.SetActive(false);Verify(game);}
            catch(Exception e){Debug.LogException(e);exit=1;}
            finally{SessionState.SetBool("LastLight.ResearchEncounterVerify",false);GameSession.VerificationSaveDirectory=null;if(Application.isBatchMode)EditorApplication.Exit(exit);else EditorApplication.isPlaying=false;}
        }
        static void Place(GameSession g,Vector3 p,Vector3 target)
        {g.player.Teleport(p,0);g.player.Simulate(g.tuning.player,Vector2.zero,Vector2.zero,false,false,.01f);g.player.view.transform.LookAt(target);Physics.SyncTransforms();}
        static void PressE(GameSession g)
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();var routing=InputSystem.settings.editorInputBehaviorInPlayMode;var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var update=typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null);
            try
            {
                update.Invoke(null,new object[]{InputUpdateType.Dynamic});InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));update.Invoke(null,new object[]{InputUpdateType.Dynamic});
                typeof(GameSession).GetMethod("Interact",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{.02f});
            }
            finally{InputSystem.QueueStateEvent(keyboard,new KeyboardState());update.Invoke(null,new object[]{InputUpdateType.Dynamic});InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=routing;InputSystem.settings.backgroundBehavior=background;}
        }
        public static void Verify(GameSession g)
        {
            var lab=g.researchSpecimen;var beast=lab.monster;var release=g.items.First(i=>i.kind==ItemKind.ResearchRelease);var keyLock=g.items.First(i=>i.kind==ItemKind.GateLock);
            Require(lab.Phase==ResearchPhase.Sealed&&beast.Health==4&&!g.Inventory.HasGateKey&&!g.GateUnlocked,"fresh encounter/key state");
            Require(lab.keyVisual.gameObject.activeSelf&&lab.KeyGlow==0&&lab.keyVisual.parent.name=="Neck"&&lab.keyChain.parent==lab.keyVisual.parent,"key/chain not attached visibly to neck while alive");
            Require(lab.attachedHoses.All(t=>t.IsChildOf(lab.lifeSupport)&&!t.IsChildOf(beast.transform)&&t.gameObject.activeSelf),"life support is not owned by tank");
            var ankle=beast.animator.GetComponentsInChildren<Transform>().First(t=>t.name=="Foot_L");
            beast.Pose(2,.12f,0);Vector3 firstAnkle=ankle.position;beast.Pose(2,.62f,0);
            Require(Vector3.Distance(firstAnkle,ankle.position)>.15f,"runtime playable stride does not animate ankle: "+firstAnkle+" / "+ankle.position);
            lab.Restore();
            Require(!beast.Hit()&&!lab.TakeKey(g),"sealed specimen can be killed or looted");
            Require(Mathf.Abs(lab.alarmClip.length-5)<.01f&&!lab.GetComponent<SafeLight>(),"alarm length or tank shelter");
            Require(GameObject.Find("Research laboratory equipment").GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("Archive specimen jar "))==28,"missing archive specimens");
            Place(g,release.transform.position+new Vector3(0,-release.transform.position.y+ForestLayout.Research.y,1),release.GetComponent<Collider>().bounds.center);PressE(g);
            Require(lab.Phase==ResearchPhase.Draining,"actual E does not activate release button: "+g.InteractionPrompt);Require(!lab.Release(g),"button can retrigger");
            lab.Simulate(2.5f,g);Require(Mathf.Abs(lab.WaterFraction-.5f)<.001f&&lab.alarmLight.enabled,"alarm/drain not synchronized");
            g.Pause();float elapsed=lab.PhaseTime;var pose=lab.suspended.localPosition;lab.Simulate(20,g);Require(lab.PhaseTime==elapsed&&lab.suspended.localPosition==pose,"release advances in a menu");g.Resume();lab.SetPaused(false);
            g.SaveCheckpoint("Midway containment drain");g.Continue();Require(lab.Phase==ResearchPhase.Draining&&Mathf.Abs(lab.PhaseTime-2.5f)<.001f&&Mathf.Abs(lab.WaterFraction-.5f)<.001f,"mid-drain checkpoint restarted or skipped the sequence");
            lab.Simulate(2.49f,g);Require(lab.Phase==ResearchPhase.Draining&&lab.alarmLight.enabled,"siren ended before five active seconds");
            lab.Simulate(.02f,g);Require(lab.Phase==ResearchPhase.Opening&&!lab.alarm.isPlaying&&!lab.alarmLight.enabled&&!lab.water.gameObject.activeSelf,"five-second release boundary");
            lab.Simulate(1.21f,g);Require(lab.Phase==ResearchPhase.Emerging&&!lab.tankSolid.enabled&&Quaternion.Angle(lab.leftDoor.localRotation,Quaternion.identity)>90,"hatches remain closed");
            lab.Simulate(4.05f,g);Require(lab.Phase==ResearchPhase.Hunting&&beast.CanBeHit,"escape does not hand over to pursuit");
            Require(lab.attachedHoses.All(t=>!t.gameObject.activeInHierarchy)&&lab.keyVisual.gameObject.activeInHierarchy&&lab.KeyGlow==0,"released creature retained hoses or lost necklace");
            Vector3 exit=lab.ExitPoint;Place(g,ForestLayout.Research+new Vector3(-2.8f,0,1.5f),beast.AimPoint);float distance=Vector3.Distance(g.player.transform.position,beast.transform.position);
            for(int i=0;i<12;i++)lab.Simulate(.05f,g);
            Require(Vector3.Distance(g.player.transform.position,beast.transform.position)<distance-.3f,"released creature does not move along the room path");
            var hunting=new ResearchSave{phase=4,health=4,x=exit.x,y=exit.y,z=exit.z};lab.Restore(hunting);
            g.Inventory.AcquireShotgun();g.shotgun.ResetCombat();
            Place(g,ForestLayout.Research+new Vector3(0,0,-1.35f),beast.AimPoint);Require(g.shotgun.Fire(g)&&beast.Health==4,"shot passes through corridor wall");
            g.Inventory.RestoreWeapon(true,2,12,HeldWeapon.Shotgun);g.shotgun.ResetCombat();Place(g,exit+new Vector3(1.8f,0,1.7f),beast.AimPoint);
            for(int i=0;i<4;i++)
            {
                if(g.Inventory.LoadedShells==0){Require(g.shotgun.BeginReload(g),"test shotgun reload");g.shotgun.Simulate(1.8f,g);}
                g.player.view.transform.LookAt(beast.transform.position+Vector3.up*(i%2==0?1.95f:1.2f));
                Require(g.shotgun.Fire(g),"shotgun did not fire");Require(beast.Health==3-i,"shotgun does not require exactly four hits");g.shotgun.Simulate(.9f,g);
            }
            Require(lab.Phase==ResearchPhase.Dead&&!lab.TakeKey(g),"death/early loot");
            lab.Restore(hunting);g.Inventory.HasAxe=true;g.Inventory.Weapon=HeldWeapon.Axe;g.axe.ResetCombat();Place(g,exit+Vector3.forward*1.75f,beast.AimPoint);
            for(int i=0;i<4;i++){Require(g.axe.BeginSwing(g),"axe did not swing");g.axe.Simulate(.27f,g);Require(beast.Health==3-i,"axe does not require exactly four strikes");g.axe.Simulate(.51f,g);}
            Require(lab.Phase==ResearchPhase.Dead,"four axe strikes do not kill");
            lab.Restore(hunting);g.Inventory.RestoreWeapon(true,2,12,HeldWeapon.Shotgun);g.shotgun.ResetCombat();Place(g,exit+Vector3.forward*1.75f,beast.AimPoint);
            for(int i=0;i<2;i++){Require(g.shotgun.Fire(g),"mixed weapon shot");g.shotgun.Simulate(.9f,g);}
            g.Inventory.Weapon=HeldWeapon.Axe;g.axe.ResetCombat();
            for(int i=0;i<2;i++){Require(g.axe.BeginSwing(g),"mixed weapon swing");g.axe.Simulate(.27f,g);g.axe.Simulate(.51f,g);}
            Require(lab.Phase==ResearchPhase.Dead&&beast.Health==0,"two shots plus two axe strikes do not kill");
            lab.Restore(hunting);g.Condition.Restore(false);g.State.Set(ScreenMode.Playing);Place(g,exit+Vector3.forward*1.3f,beast.AimPoint);
            float active=0;while(!g.Condition.Dead&&active<5){g.Condition.Tick(.05f,false,g.tuning.player);lab.Simulate(.05f,g);active+=.05f;}
            Require(g.Condition.Dead&&beast.Strikes==2&&active<5,"two actual contact attacks fail to kill within five seconds");
            g.Condition.Restore(false);g.State.Set(ScreenMode.Playing);lab.Restore(new ResearchSave{phase=5,health=0,x=exit.x,z=exit.z});lab.Simulate(2.3f,g);
            Require(lab.lootCollider.enabled&&lab.keyVisual.gameObject.activeSelf,"settled body is not searchable");
            float glow=lab.KeyGlow;lab.Simulate(.27f,g);Require(Mathf.Abs(lab.KeyGlow-glow)>.05f,"dead creature key does not blink");
            var keyPose=lab.keyVisual.position;g.Pause();float pausedGlow=lab.KeyGlow;lab.Simulate(2,g);Require(lab.KeyGlow==pausedGlow&&lab.keyVisual.position==keyPose,"paused key pulse or necklace pose advances");g.Resume();
            g.SaveCheckpoint("Necklace key still on corpse");g.Continue();Require(!lab.KeyTaken&&lab.keyVisual.gameObject.activeInHierarchy&&lab.keyVisual.parent.name=="Neck"&&lab.KeyGlow>0,"loaded corpse lost blinking neck key");
            Place(g,exit+Vector3.forward*1.05f,lab.lootCollider.bounds.center);PressE(g);Require(g.Inventory.HasGateKey&&lab.KeyTaken&&!lab.lootCollider.enabled,"actual E did not recover hidden key: "+g.InteractionPrompt);Require(!lab.TakeKey(g),"duplicate key recovery");
            Require(!lab.keyVisual.gameObject.activeSelf&&lab.KeyGlow==0&&lab.keyChain.gameObject.activeInHierarchy,"pickup retained glowing key or removed chain");
            Require(!g.TryUnlockGate()&&!g.GateOpen,"key alone opens an unpowered gate");
            g.items.First(i=>i.persistentId=="service_fuse").Use(g);g.items.First(i=>i.persistentId=="cabin_circuit").Use(g);Require(g.PowerOn&&!g.GateOpen,"fuse alone opens the locked gate");
            Physics.SyncTransforms();Require(Physics.Raycast(ForestLayout.Gate+new Vector3(0,1,-3),Vector3.forward,6,g.SolidMask,QueryTriggerInteraction.Ignore),"closed powered gate collider missing");
            Place(g,ForestLayout.Cabin+new Vector3(2.14f,0,-2.2f),keyLock.GetComponent<Collider>().bounds.center);PressE(g);Require(g.GateOpen&&g.GateUnlocked,"actual E did not turn cabin lock: "+g.InteractionPrompt);
            typeof(GameSession).GetMethod("GatePresentation",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{3f});Physics.SyncTransforms();
            Require(Quaternion.Angle(g.cabinKeySwitch.localRotation,Quaternion.identity)>85&&!Physics.Raycast(ForestLayout.Gate+new Vector3(0,1,-3),Vector3.forward,6,g.SolidMask,QueryTriggerInteraction.Ignore),"key turn/gate opening presentation");
            g.SaveCheckpoint("Research verification");g.Continue();Require(g.Inventory.HasGateKey&&g.GateOpen&&lab.Phase==ResearchPhase.Dead&&!lab.lootCollider.enabled,"saved kill/key/lock not restored");Require(!lab.Release(g),"saved encounter can release twice");
            g.StartNew();Require(!g.Inventory.HasGateKey&&!g.GateOpen&&lab.Phase==ResearchPhase.Sealed&&beast.Health==4&&lab.WaterFraction==1&&!lab.lootCollider.enabled,"new game retained released specimen or key");
            Require(lab.keyVisual.gameObject.activeInHierarchy&&lab.KeyGlow==0&&lab.attachedHoses.All(t=>t.gameObject.activeInHierarchy),"new game did not restore necklace and life support");
            Debug.Log("RESEARCH ENCOUNTER PASS: 28 archive jars; actual E release; five active seconds siren/drain with pause; opening doors and skeletal escape; navigation pursuit; opaque wall occlusion; exactly four shotgun hits and four axe strikes; two real contact attacks within five seconds; actual E corpse key and cabin lock; fuse+key gate; save/load and new-game reset.");
        }
    }
}
