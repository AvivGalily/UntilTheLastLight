using System;
using UnityEngine;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void VerifyCombat(GameSession game)
        {
            game.Resume();game.Flashlight.Restore(100,false);
            VerifyDisplayUI(game);
            VerifyCabinSigns(game);
            Vector3 clearing=new Vector3(-75,0,-86);
            game.player.Teleport(clearing,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);Physics.SyncTransforms();
            VerifyPlayerPresence(game);
            Require(game.ThreatEnabled&&!game.Inventory.HasFuse,"threat still waits for fuse tutorial");
            int encounters=game.monster.EncounterCount;
            Require(game.encounters.TryReturn(game),"no reachable distant return candidate");
            Require(game.monster.EncounterCount==encounters+1&&Vector3.Distance(game.monster.transform.position,clearing)>=30,"return too close or not counted");
            Require(!MonsterEncounters.Visible(game.player.view,game.monster.transform.position,game.Protection),"return materialized on screen");
            float before=Vector3.Distance(game.monster.transform.position,clearing);
            for(int i=0;i<90;i++)game.monster.Tick(.1f,game);
            Require(Vector3.Distance(game.monster.transform.position,clearing)<before-1,"returned monster did not approach");
            // Place a visible close encounter: the director must leave it untouched.
            game.monster.BeginEncounter(clearing+Vector3.forward*8,clearing);encounters=game.monster.EncounterCount;
            game.encounters.Simulate(70,game);Require(game.monster.EncounterCount==encounters,"director teleported a close pursuit");
            game.encounters.TryReturn(game);encounters=game.monster.EncounterCount;
            game.encounters.ResetSequence(game.tuning.seed);game.encounters.Simulate(35,game);
            Require(game.encounters.Hidden&&game.encounters.Vanishes==1&&!game.monster.HasSpawned&&!game.monster.gameObject.activeSelf,"scheduled disappearance did not deactivate creature");
            game.horrorAudio.Simulate(.1f,game);Require(!game.horrorAudio.monsterVoice.isPlaying,"hidden creature voice still playing");
            float hiddenTime=game.encounters.TimeUntilReturn;game.encounters.Simulate(0,game);
            Require(game.encounters.TimeUntilReturn==hiddenTime,"zero-time disappearance timer advanced");
            game.encounters.Simulate(hiddenTime+.1f,game);
            Require(game.monster.EncounterCount==encounters+1&&!game.encounters.Hidden&&game.monster.HasSpawned,"scheduled return after hidden interval failed");
            game.horrorAudio.Simulate(.1f,game);Require(game.horrorAudio.RoarsPlayed>0&&game.horrorAudio.monsterVoice.isPlaying,"return roar missing");
            // No sight/hearing: the live pursuit must still retarget and make progress.
            float originalSight=game.tuning.monster.sightRange;
            try
            {
                game.tuning.monster.sightRange=0;game.monster.Senses.Reset();
                game.player.Teleport(clearing+Vector3.right*2,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);Physics.SyncTransforms();
                float distant=Vector3.Distance(game.monster.transform.position,game.player.transform.position);
                for(int i=0;i<80;i++)game.monster.Tick(.1f,game);
                Require(Vector3.Distance(game.monster.Senses.LastKnown,game.player.transform.position)<.01f&&game.monster.Senses.HasMemory,"pursuit lost current player target");
                Require(Vector3.Distance(game.monster.transform.position,game.player.transform.position)<distant-1,"unseen creature no longer approaches");
            }
            finally{game.tuning.monster.sightRange=originalSight;}
            game.monster.BeginEncounter(clearing+Vector3.forward*10,clearing);game.horrorAudio.Simulate(.1f,game);game.horrorAudio.Simulate(20,game);
            Require(game.horrorAudio.GrowlsPlayed>0&&game.horrorAudio.monsterVoice.isPlaying,"creature growl missing");
            game.Pause();int pausedRoars=game.horrorAudio.RoarsPlayed;game.horrorAudio.Simulate(0,game);
            Require(AudioListener.pause&&!game.horrorAudio.monsterVoice.ignoreListenerPause&&game.horrorAudio.RoarsPlayed==pausedRoars,"voice fails menu pause");game.Resume();
            Debug.Log("PURSUIT SMOKE PASS: inactive disappearance, zero-time freeze, distant reachable return, current moving target without sight/hearing, roar/growl playback and menu pause.");
            // Foley follows real displacement, including a stopped controller.
            game.foley.ResetSequence(game.tuning.seed);
            game.player.Teleport(clearing,0);
            for(int i=0;i<30;i++){game.player.Simulate(game.tuning.player,Vector2.up,Vector2.zero,false,false,.05f);game.foley.Simulate(.05f,game);}
            Require(game.foley.StepsPlayed>=3,"walking footsteps missing");
            int walking=game.foley.StepsPlayed;
            for(int i=0;i<25;i++){game.player.Simulate(game.tuning.player,Vector2.up,Vector2.zero,true,false,.05f);game.foley.Simulate(.05f,game);}
            Require(game.foley.StepsPlayed>walking&&game.foley.Effort>.5f&&game.foley.hardBreath.volume>.1f,"running/exertion audio missing");
            int running=game.foley.StepsPlayed;
            for(int i=0;i<20;i++){game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.05f);game.foley.Simulate(.05f,game);}
            Require(game.foley.StepsPlayed==running,"idle player emits steps");
            // A real moving monster emits clicks; its subsequent shelter stop does not.
            game.player.Teleport(clearing,0);game.monster.BeginEncounter(clearing+Vector3.forward*6,clearing);
            for(int i=0;i<23;i++)game.monster.Tick(.1f,game);
            Require(game.monster.ActualSpeed>.08f,"click test monster did not move");
            game.horrorAudio.Simulate(.6f,game);Require(game.horrorAudio.clicks.isPlaying,"moving creature clicks missing");
            game.player.Teleport(game.cabinSpawn.position,0);game.monster.Tick(.1f,game);game.horrorAudio.Simulate(.1f,game);
            Require(game.monster.ActualSpeed<.01f&&!game.horrorAudio.clicks.isPlaying,"stationary creature clicks continued");
            // Axe ownership, distance, occlusion and exactly one hit per swing.
            game.player.Teleport(clearing,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);
            Require(!game.axe.BeginSwing(game),"axe can swing before pickup");
            var pickup=Array.Find(game.items,item=>item.kind==ItemKind.Axe);Require(Vector3.Distance(pickup.transform.position,ForestLayout.Bodies[0])<2,"axe not by nearest body");
            var pickupCenter=pickup.GetComponent<Collider>().bounds.center;var inspecting=pickupCenter+new Vector3(0,1.7f,-.6f);
            Require(Physics.Raycast(inspecting,(pickupCenter-inspecting).normalized,out var pickupHit,game.tuning.player.interactionRange,game.SolidMask|LayerMask.GetMask("Interactable"),QueryTriggerInteraction.Collide)&&pickupHit.collider.GetComponentInParent<WorldItem>()==pickup,"axe cannot be targeted beside corpse");pickup.Use(game);
            var wolf=game.wolves[0];wolf.ResetWolf(false);wolf.transform.SetPositionAndRotation(clearing+Vector3.forward*5,Quaternion.Euler(0,180,0));
            Vector3 wolfStart=wolf.transform.position;
            for(int i=0;i<8;i++)wolf.Simulate(.05f,game);
            Require(Vector3.Distance(wolfStart,wolf.transform.position)>.5f,"wolf did not chase on navigation");
            Require(wolf.Barks>0&&wolf.barkVoice.isPlaying,"approaching wolf does not bark");
            wolf.ResetWolf(false);wolf.transform.SetPositionAndRotation(clearing+Vector3.forward*12,Quaternion.Euler(0,180,0));Physics.SyncTransforms();
            wolf.Simulate(.05f,game);Require(wolf.Howls==1&&wolf.voice.isPlaying,"approaching wolf did not howl before pursuit");
            for(int i=0;i<40;i++)wolf.Simulate(.1f,game);
            Require(wolf.Growls>0,"nearby wolf growl missing");
            Require(wolf.Barks>0,"wolf did not bark after approach howl");
            // Compare explicit distances; the first chase has already moved the wolf close.
            wolf.transform.position=clearing+Vector3.forward*12;wolf.Simulate(.05f,game);float farBark=wolf.barkVoice.volume;
            wolf.transform.position=clearing+Vector3.forward*5;wolf.Simulate(.05f,game);
            Require(wolf.barkVoice.volume>farBark,"nearby barking did not get louder");
            int barks=wolf.Barks;wolf.Simulate(0,game);Require(wolf.Barks==barks,"zero-time wolf bark advanced");
            game.player.Teleport(game.cabinSpawn.position,0);wolf.Simulate(.1f,game);Require(!wolf.barkVoice.isPlaying,"wolf keeps barking at safe player");
            Debug.Log($"WOLF BARK SMOKE PASS: approaching recorded bark, 12m gain {farBark:F3} < 5m gain {ZombieWolf.BarkGain(5):F3}, zero-time freeze and shelter silence.");
            game.player.Teleport(clearing,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);Physics.SyncTransforms();
            wolf.ResetWolf(false);wolf.transform.SetPositionAndRotation(clearing+Vector3.forward*1.3f,Quaternion.Euler(0,180,0));Physics.SyncTransforms();
            game.Condition.Restore(false);game.Condition.Tick(11,false,game.tuning.player);
            for(int i=0;i<52;i++)wolf.Simulate(.1f,game);
            Require(wolf.BiteProgress>=0,"wolf never started a bite");
            for(int i=0;i<2;i++)wolf.Simulate(.1f,game);
            game.player.view.transform.LookAt(wolf.transform.position+Vector3.up*.65f);RigShot(game,"29-wolf-bite-open",true);
            for(int i=0;i<3;i++)wolf.Simulate(.1f,game);
            Require(wolf.Bites==1,"wolf bite did not hit at jaw closure");RigShot(game,"30-wolf-bite-contact",true);
            wolf.Simulate(.6f,game);Require(wolf.BiteProgress<0,"wolf bite recovery never ended");
            game.Condition.Restore(false);wolf.ResetWolf(false);game.player.view.transform.rotation=Quaternion.identity;
            wolf.transform.position=clearing+Vector3.forward*5;Physics.SyncTransforms();
            game.axe.BeginSwing(game);game.axe.Simulate(.3f,game);Require(wolf.Health==2,"axe hit beyond reach");game.axe.Simulate(.6f,game);
            wolf.transform.position=clearing+Vector3.forward*1.5f;
            var wall=new GameObject("Combat verification wall");wall.layer=LayerMask.NameToLayer("WorldSolid");wall.transform.position=clearing+new Vector3(0,1,.7f);wall.AddComponent<BoxCollider>().size=new Vector3(3,3,.2f);Physics.SyncTransforms();
            game.axe.BeginSwing(game);game.axe.Simulate(.3f,game);Require(wolf.Health==2,"axe struck through wall");game.axe.Simulate(.6f,game);UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
            game.axe.BeginSwing(game);game.axe.Simulate(.3f,game);Require(wolf.Health==1,"first axe hit failed");game.axe.Simulate(.1f,game);Require(wolf.Health==1,"one swing struck twice");game.axe.Simulate(.5f,game);
            game.axe.BeginSwing(game);game.axe.Simulate(.3f,game);Require(!wolf.Alive&&!wolf.hitCollider.enabled,"second axe hit did not kill");game.axe.Simulate(.6f,game);
            VerifyWolfCollapse(game,wolf);
            Require(!wolf.barkVoice.isPlaying,"dead wolf still barking");
            game.monster.gameObject.SetActive(false);game.Flashlight.Restore(100,true);
            game.player.view.transform.rotation=Quaternion.LookRotation(wolf.transform.position+Vector3.up*.3f-game.player.view.transform.position);
            UIShot(game,"20-axe-and-defeated-wolf",true);
            game.SaveCheckpoint("Combat verification");game.Continue();
            Require(game.State.Running&&game.Inventory.HasAxe&&!wolf.Alive&&!pickup.gameObject.activeSelf,"axe/dead wolf checkpoint failed");
            // The actual AI must finish a fatal strike even with a full beam aimed at it.
            game.player.Teleport(clearing,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);game.Condition.Restore(false);game.Condition.Tick(11,false,game.tuning.player);
            game.Flashlight.Restore(100,true);game.monster.BeginEncounter(clearing+Vector3.forward*1.5f,clearing);Physics.SyncTransforms();
            Require(game.Protection.PointBlank(game.monster.transform.position)&&!game.Protection.AttackBlocked(game.monster.transform.position,game.tuning.monster.radius,game.tuning.monster.height,clearing+Vector3.up*1.3f),"beam blocks point-blank attack");
            for(int i=0;i<70&&game.State.Running;i++){game.monster.Tick(.1f,game);if(game.State.Running)game.creature.Simulate(.1f,game.monster);}
            Require(game.State.Mode==ScreenMode.Dying&&game.Condition.Dead&&game.damage.Blood>.9f,"actual Walker attack did not start the fatal close-up");
            Require(game.creature.mouth.GetBlendShapeWeight(HeroTree.ShapeIndex(game.creature.mouth.sharedMesh,"Jaw_Bite"))>90,"Walker did not close its real jaw on fatal contact");
            Require(!AudioListener.pause&&game.foley.injury.isPlaying,"fatal scream interrupted by animation");
            game.hud.Refresh();VerifyPlayerDeath(game);
            bool language=game.Locale.Hebrew;game.Locale.SetHebrew(true);game.hud.Invalidate();UIShot(game,"21-death-blood-hebrew",true);game.Locale.SetHebrew(language);game.hud.Invalidate();
            game.Continue();Require(game.State.Running&&!game.Condition.Dead&&game.damage.Blood==0,"death checkpoint recovery failed");
            // Starting over resets equipment, all enemies and visible feedback.
            game.StartNew();Require(!game.Inventory.HasAxe&&Array.TrueForAll(game.wolves,w=>w.Alive)&&game.damage.Blood==0,"new game retained combat state");
            Array.Find(game.items,item=>item.kind==ItemKind.MapCorpse).Use(game);foreach(var item in game.items)if(item.kind==ItemKind.Corpse)item.Use(game);game.Resume();
            game.player.Teleport(game.cabinSpawn.position,0);game.Flashlight.Restore(100,true);game.Pause();
            Debug.Log("COMBAT SMOKE PASS: recurring unseen reachable approaches, visible pursuit guard, movement-only player/monster footsteps, exertion breath, axe ownership/range/wall/single-hit/two-hit kill, wolf pursuit, combat checkpoint, actual point-blank fatal AI strike with full beam, continuing scream, blood/death UI and new-game reset.");
        }
    }
}
