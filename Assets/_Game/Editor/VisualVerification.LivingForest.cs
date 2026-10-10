using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void RigShot(GameSession game,string name,bool beam)
        {
            var skins=new List<SkinnedMeshRenderer>();skins.AddRange(game.forestEncounter.victim.GetComponentsInChildren<SkinnedMeshRenderer>());
            skins.AddRange(game.monster.GetComponentsInChildren<SkinnedMeshRenderer>());
            foreach(var wolf in game.wolves)skins.AddRange(wolf.GetComponentsInChildren<SkinnedMeshRenderer>());
            if(game.norton)foreach(var head in game.norton.enemies)if(head.gameObject.activeSelf)skins.AddRange(head.GetComponentsInChildren<SkinnedMeshRenderer>());
            var baked=new List<GameObject>();var meshes=new List<Mesh>();var states=new List<bool>();
            foreach(var skin in skins)
            {
                states.Add(skin.enabled);if(!skin.enabled)continue;
                var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                var go=new GameObject("Verification animated surface",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(skin.transform,false);
                go.transform.localScale=new Vector3(1/skin.transform.lossyScale.x,1/skin.transform.lossyScale.y,1/skin.transform.lossyScale.z);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=skin.sharedMaterials;
                r.shadowCastingMode=skin.shadowCastingMode;r.receiveShadows=skin.receiveShadows;r.renderingLayerMask=skin.renderingLayerMask;
                var properties=new MaterialPropertyBlock();skin.GetPropertyBlock(properties);r.SetPropertyBlock(properties);skin.enabled=false;baked.Add(go);
            }
            UIShot(game,name,beam);
            for(int i=0;i<skins.Count;i++)skins[i].enabled=states[i];
            foreach(var go in baked)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
        }
        static void VerifyLivingForest(GameSession game)
        {
            game.Resume();var sequence=game.forestEncounter;Require(sequence&&game.wildlife,"living forest wiring");
            Require(sequence.web&&!sequence.web.Released&&sequence.web.intactCore.enabled&&sequence.wounds.amount>=.6f,"victim must start wounded and bound in an intact web");
            game.wildlife.ResetForest();var tree=game.wildlife.treeVisuals[0];Quaternion rest=tree.localRotation;
            game.wildlife.Simulate(2,game);Require(Quaternion.Angle(rest,tree.localRotation)>.01f,"forest wind does not move trees");
            Require(game.wildlife.treeVisuals.Length>700,"only a few trees have wind");
            var crow=game.wildlife.crows[0];Vector3 perch=crow.transform.position;
            crow.Simulate(.5f,new Vector3(perch.x,0,perch.z),0);Require(crow.Flying&&crow.FleeCount==1,"crow did not flush on approach");
            crow.Simulate(.5f,new Vector3(perch.x,0,perch.z),0);Require(Vector3.Distance(perch,crow.transform.position)>.5f,"crow has no flight displacement");
            Shot(game.player.view,"23-crow-taking-flight",crow.transform.position+new Vector3(1,1,2),crow.transform.position,true);
            game.wildlife.ResetForest();Require(!crow.Flying&&crow.FleeCount==0,"crow reset");
            game.Flashlight.Restore(100,true);game.Condition.Restore(false);game.Condition.Tick(11,false,game.tuning.player);
            Vector3 watching=ForestLayout.EncounterTree+new Vector3(-5,0,-5);
            game.player.Teleport(watching,45);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.01f);game.axe.Simulate(.01f,game);
            game.player.view.transform.rotation=Quaternion.LookRotation(sequence.victim.position+Vector3.up*1.1f-game.player.view.transform.position);Physics.SyncTransforms();
            game.monster.BeginEncounter(watching+new Vector3(-35,0,0),watching);
            Require(sequence.TryTrigger(game),"first visible approach did not trigger the ambush");
            Require(sequence.TriggerCount==1&&sequence.voice.isPlaying,"missing help voice");
            sequence.Simulate(.01f,game);
            RigShot(game,"24-ambush-help",true);
            var camera=game.player.view;Vector3 oldCamera=camera.transform.position;Quaternion oldView=camera.transform.rotation;
            camera.transform.SetPositionAndRotation(sequence.web.transform.position+new Vector3(0,.05f,-4.4f),Quaternion.LookRotation(sequence.web.transform.position-(sequence.web.transform.position+new Vector3(0,.05f,-4.4f))));
            RigShot(game,"38-webbed-injured-victim",true);camera.transform.SetPositionAndRotation(oldCamera,oldView);
            Vector3 suspended=sequence.victim.position,thread=sequence.web.supports[0].GetPosition(2);
            float elapsed=sequence.Elapsed;var wing=crow.leftWing.localRotation;game.Pause();sequence.Simulate(5,game);game.wildlife.Simulate(5,game);
            Require(sequence.Elapsed==elapsed&&crow.leftWing.localRotation==wing&&sequence.web.supports[0].GetPosition(2)==thread,"cutscene, silk or wildlife runs during pause");game.Resume();
            sequence.Simulate(2.21f,game);sequence.Simulate(.38f,game);
            Require(sequence.Phase==ForestEncounterPhase.Leap&&game.monster.transform.position.y>2,"creature did not leap");
            RigShot(game,"25-ambush-pounce",true);
            for(int i=0;i<25;i++)sequence.Simulate(.1f,game);
            Require(sequence.Phase==ForestEncounterPhase.Feeding&&sequence.BloodBursts>=2&&sequence.wounds.amount>.4f,"feeding animation/blood impacts missing");
            Require(sequence.web.Released&&sequence.web.TearCount==1&&!sequence.web.intactCore.enabled&&sequence.victim.position.y<suspended.y-.5f,"web did not tear/release the falling body");
            Require(Array.TrueForAll(sequence.web.supports,l=>!l.enabled)&&sequence.web.transform.GetComponentsInChildren<Renderer>().Length>0,"loose strands did not finish snapping");
            Require(Vector3.Distance(game.creature.MouthPosition,sequence.BiteTarget)<.18f,"feeding mouth does not contact the victim chest");
            RigShot(game,"26-ambush-feeding",true);
            for(int i=0;i<60&&!sequence.Escaping;i++)sequence.Simulate(.1f,game);
            Require(sequence.Escaping&&game.Flashlight.Faulted&&!game.Flashlight.Active&&game.monster.Relentless,"ambush did not release a blackout pursuit");
            Require(game.monster.NavigationReady,"feeding handoff did not find clear connected navigation");
            VerifyFeedingHandoffDirections(game);
            Vector3 releasePosition=game.monster.transform.position;float releaseDistance=Vector3.Distance(releasePosition,game.player.transform.position);
            for(int i=0;i<16;i++){game.monster.Tick(.05f,game);game.creature.Simulate(.05f,game.monster);}
            Require(Vector3.Distance(releasePosition,game.monster.transform.position)>.7f&&Vector3.Distance(game.monster.transform.position,game.player.transform.position)<releaseDistance-.5f,"creature stuck at victim after feeding");
            Require(sequence.score.isPlaying&&sequence.ScoreIntensity>0&&sequence.heartbeat.isPlaying,"escape score or heartbeat missing");
            int encounters=game.monster.EncounterCount;game.encounters.Simulate(100,game);Require(game.monster.EncounterCount==encounters,"encounter director stole the scripted pursuit");
            Require(!sequence.TryTrigger(game)&&sequence.TriggerCount==1,"ambush repeats");
            bool language=game.Locale.Hebrew;game.Locale.SetHebrew(true);game.hud.Invalidate();RigShot(game,"27-blackout-escape",false);game.Locale.SetHebrew(language);game.hud.Invalidate();
            // Run the actual character motor and monster planner along the trail.
            Vector3[] route={new Vector3(-69,0,-77),new Vector3(-75,0,-86),ForestLayout.Cabin+Vector3.forward*2};
            for(int leg=0;leg<route.Length;leg++)
            {
                int steps=0;
                while(Vector3.Distance(Vector3.ProjectOnPlane(game.player.transform.position-route[leg],Vector3.up),Vector3.zero)>.45f&&steps++<300&&game.State.Running)
                {
                    Vector3 delta=route[leg]-game.player.transform.position;delta.y=0;game.player.transform.rotation=Quaternion.LookRotation(delta);
                    game.player.Simulate(game.tuning.player,Vector2.up,Vector2.zero,true,false,.05f);Physics.SyncTransforms();
                    game.Condition.Tick(.05f,game.IsSafe,game.tuning.player);sequence.Simulate(.05f,game);game.monster.Tick(.05f,game);game.creature.Simulate(.05f,game.monster);
                }
                Require(steps<300&&game.State.Running,"escape route blocked or impossible at normal running speed, leg "+leg+" at "+game.player.transform.position);
            }
            Require(sequence.Phase==ForestEncounterPhase.Refuge&&!sequence.score.isPlaying&&!sequence.heartbeat.isPlaying&&game.Flashlight.Faulted,"reaching cabin must silence chase but keep repair required");
            Require(game.IsSafe,"cabin no longer protects player");
            RigShot(game,"28-refuge-recharge",false);
            game.Continue();Require(sequence.Seen&&sequence.Phase==ForestEncounterPhase.Refuge&&game.Flashlight.Faulted,"blackout checkpoint lost one-time event/fault");
            Require(sequence.web.Released&&!sequence.web.intactCore.enabled,"checkpoint restore rebuilt the intact web");
            var charger=Array.Find(game.items,item=>item.persistentId=="emergency_charger");
            game.player.Teleport(ForestLayout.Cabin+new Vector3(2.4f,0,2.8f),180);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.01f);
            game.player.view.transform.rotation=Quaternion.LookRotation(charger.transform.position-game.player.view.transform.position);Physics.SyncTransforms();
            var keyboard=Keyboard.current;bool added=keyboard==null;if(added)keyboard=InputSystem.AddDevice<Keyboard>();
            var priorInputRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var priorBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var playerInputUpdate=typeof(InputSystem).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null);
            void AdvanceInput()=>playerInputUpdate.Invoke(null,new object[]{InputUpdateType.Dynamic});
            try
            {
                // This verifier runs from EditorApplication.update; explicitly
                // advance the play-mode input buffer, not the editor UI buffer.
                AdvanceInput();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));AdvanceInput();
                Debug.Log("CHARGER CHECK: mode="+game.State.Mode+" key="+keyboard.eKey.isPressed+" safe="+game.IsSafe+" eyes="+game.player.view.transform.position+" target="+charger.transform.position);
                var interact=typeof(GameSession).GetMethod("Interact",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                for(int i=0;i<121;i++)interact.Invoke(game,new object[]{.1f});
                Debug.Log("CHARGER RESULT: prompt="+game.InteractionPrompt+" hold="+game.HoldProgress+" fault="+game.Flashlight.Faulted+" charge="+game.Flashlight.Charge+" mode="+game.State.Mode);
            }
            finally{InputSystem.QueueStateEvent(keyboard,new KeyboardState());AdvanceInput();InputSystem.settings.editorInputBehaviorInPlayMode=priorInputRouting;InputSystem.settings.backgroundBehavior=priorBackground;if(added)InputSystem.RemoveDevice(keyboard);}
            Require(!game.Flashlight.Faulted&&game.Flashlight.Active,"real E hold at the charger did not restore the lamp");sequence.Simulate(.01f,game);
            game.Continue();Require(sequence.Phase==ForestEncounterPhase.Resolved&&game.Flashlight.Active&&!game.Flashlight.Faulted,"repaired checkpoint failed");
            // A player who stands still during the pursuit must still be catchable.
            game.player.Teleport(watching,45);game.Condition.Restore(false);game.Condition.Tick(11,false,game.tuning.player);game.Flashlight.FailCircuit();game.monster.BeginEncounter(watching+Vector3.forward*7,watching);game.monster.Relentless=true;
            for(int i=0;i<200&&game.State.Running;i++)game.monster.Tick(.1f,game);
            Require(game.State.Mode==ScreenMode.Dying,"blackout pursuer cannot catch an idle player");
            game.deathSequence.Simulate(game.deathSequence.Duration+.1f);Require(game.State.Mode==ScreenMode.Dead,"fatal animation never completes");
            game.Continue();Require(game.State.Running&&!game.Flashlight.Faulted,"death recovery");game.Pause();
            Debug.Log("WEB AMBUSH SMOKE PASS: pre-existing wounds, bone-attached silk, pause freeze, one impact tear, physical fall, feeding/blood and torn-web checkpoint restoration.");
            Debug.Log("LIVING FOREST SMOKE PASS: wind on all forest trees, crow approach/flight/reset, one-time visible trigger, help voice, pause, aerial pounce, feeding/blood, circuit failure, locked encounter ownership, score/heartbeat, real motor+NavMesh escape to cabin, immediate silence, persistent repair, lethal idle pursuit and recovery.");
        }
    }
}
