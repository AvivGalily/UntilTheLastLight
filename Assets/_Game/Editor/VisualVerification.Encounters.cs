using System;
using UnityEngine;
using UnityEngine.AI;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void VerifyFogRetreat(GameSession g,Vector3 clearing)
        {
            g.Resume();PlaceNortonPlayer(g,clearing);g.Flashlight.Restore(100,true);g.encounters.ResetSequence(g.tuning.seed);
            Require(!g.forestEncounter.OwnsMonster,"fog fixture overlaps scripted ambush");
            Vector3 point=clearing+Vector3.forward*8;
            Require(NavMesh.SamplePosition(point,out var nav,1,NavMesh.AllAreas)&&MonsterPathPlanner.StandingClear(nav.position,g.tuning.monster,g.SolidMask),"fog retreat fixture not clear");
            g.monster.BeginEncounter(nav.position,clearing);g.player.view.transform.LookAt(nav.position+Vector3.up*1.1f);Physics.SyncTransforms();
            Require(g.Protection.Capsule(nav.position,g.tuning.monster.radius,g.tuning.monster.height)==Protection.Full,"fog fixture not illuminated");
            g.monster.Tick(.1f,g);Require(g.monster.RetreatingToFog,"light did not start persistent retreat");
            // Chasing it into point-blank range still makes the light powerless.
            Vector3 near=g.monster.transform.position-Vector3.forward*1.4f;PlaceNortonPlayer(g,near);
            g.player.view.transform.LookAt(g.monster.transform.position+Vector3.up*1.1f);g.monster.Tick(.1f,g);
            Require(!g.monster.RetreatingToFog&&g.Protection.PointBlank(g.monster.transform.position),"fleeing creature grants point-blank immunity");
            PlaceNortonPlayer(g,clearing);g.monster.BeginEncounter(nav.position,clearing);g.player.view.transform.LookAt(nav.position+Vector3.up*1.1f);g.monster.Tick(.1f,g);
            g.creature.Simulate(.1f,g.monster);bool hand=g.player.hand.gameObject.activeSelf;g.player.hand.gameObject.SetActive(false);NortonArtShot(g,"69-walker-repelled",true);
            float lastDistance=Vector3.Distance(g.monster.transform.position,clearing);bool outsideBeam=false;
            bool mistShot=false;
            for(int n=0;n<300&&!g.encounters.Hidden;n++)
            {
                g.player.view.transform.LookAt(g.monster.transform.position+Vector3.up*1.1f);
                g.encounters.Simulate(.1f,g);if(g.encounters.Hidden)break;
                Vector3 before=g.monster.transform.position;g.monster.Tick(.1f,g);
                Require(Vector3.Distance(before,g.monster.transform.position)<=g.tuning.monster.retreatSpeed*.1f+.05f,"retreat teleported");
                float distance=Vector3.Distance(g.monster.transform.position,clearing);
                g.creature.Simulate(.1f,g.monster);
                if(distance>=28&&!mistShot){NortonArtShot(g,"70-walker-retreating-into-mist",true);mistShot=true;}
                if(g.Protection.Capsule(g.monster.transform.position,g.tuning.monster.radius,g.tuning.monster.height)==Protection.None)outsideBeam=true;
                Require(g.monster.RetreatingToFog,"monster returned to pursuit instead of escaping");lastDistance=distance;
            }
            Require(outsideBeam&&g.encounters.Hidden&&!g.monster.HasSpawned&&lastDistance>=g.tuning.monster.fogRetreatDistance,"retreat never reached fog/hid");
            float remaining=g.encounters.TimeUntilReturn;Require(remaining>=10&&remaining<=15,"fog respite outside ten to fifteen seconds");
            g.Pause();g.encounters.Simulate(6,g);Require(g.encounters.TimeUntilReturn==remaining,"fog respite advances in pause");g.Resume();g.encounters.Simulate(0,g);
            Require(g.encounters.TimeUntilReturn==remaining,"fog respite advances at zero time");
            g.encounters.Simulate(remaining-.1f,g);Require(!g.monster.HasSpawned,"fog return too early");
            g.encounters.Simulate(.11f,g);Require(g.monster.HasSpawned&&!g.encounters.Hidden&&Vector3.Distance(g.monster.transform.position,clearing)>=30,"fog return missing/too close");
            Require(!MonsterEncounters.Visible(g.player.view,g.monster.transform.position,g.Protection)&&!g.monster.RetreatingToFog,"fog return materialized on screen/still fleeing");
            g.Flashlight.Restore(100,false);float distant=Vector3.Distance(g.monster.transform.position,clearing);
            for(int n=0;n<300;n++)
            {
                g.monster.Tick(.1f,g);g.creature.Simulate(.1f,g.monster);
                if(Vector3.Distance(g.monster.transform.position,clearing)<15&&g.Protection.Clear(g.player.view.transform.position,g.creature.MouthPosition))break;
            }
            Require(Vector3.Distance(g.monster.transform.position,clearing)<15&&g.monster.FogFade<.1f&&g.Protection.Clear(g.player.view.transform.position,g.creature.MouthPosition),"return fails to physically emerge from mist/occlusion");
            var fogBlock=new MaterialPropertyBlock();g.creature.mouth.GetPropertyBlock(fogBlock);Require(fogBlock.GetFloat("_FogFade")<.1f&&g.creature.mouth.enabled&&Vector3.Distance(g.creature.MouthPosition,g.monster.transform.position)<3.5f,"return retains hidden/misplaced visual body");
            Debug.Log($"FOG RETURN VISUAL: feet {g.monster.transform.position}, mouth {g.creature.MouthPosition}, range {Vector3.Distance(g.monster.transform.position,clearing):F1}, fade {fogBlock.GetFloat("_FogFade"):F3}.");
            g.player.view.transform.LookAt(g.creature.MouthPosition);NortonArtShot(g,"71-walker-returning-from-mist",false);g.player.hand.gameObject.SetActive(hand);
            g.Flashlight.Restore(100,false);g.player.view.transform.localRotation=Quaternion.identity;g.monster.Disappear();g.encounters.ResetSequence(g.tuning.seed);
            Debug.Log("FOG RETREAT PASS: illuminated physical escape beyond beam to 32m; mist concealment; ten-to-fifteen-second respite, pause/zero-time freeze and distant offscreen return.");
        }
        static void VerifyHeadLifecycle(GameSession g)
        {
            var encounter=g.norton;encounter.ResetEncounter(true,true);PlaceNortonPlayer(g,new Vector3(-75,0,-86));g.Condition.Restore(false);
            Require(encounter.NextAppearance==18,"loaded head encounter does not restore its original delay");
            Require(encounter.TrySpawn(g),"lifecycle head cannot spawn");var head=Array.Find(encounter.enemies,e=>e.gameObject.activeSelf);
            Require(encounter.TrySpawn(g)&&!encounter.TrySpawn(g),"original two-head pool limit not restored");
            var second=Array.Find(encounter.enemies,e=>e!=head&&e.gameObject.activeSelf);
            float wait=encounter.NextAppearance;encounter.Simulate(.05f,g);
            Require(encounter.NextAppearance<wait,"recurrence freezes while heads are alive");
            Require(encounter.MusicEngaged&&encounter.approachMusic.isPlaying&&encounter.approachMusic.volume>0&&encounter.approachMusic.volume<=.13f,"quiet approach score missing");
            // Retreating out of earshot must not reset the already engaged score.
            Vector3 headPosition=head.transform.position;PlaceNortonPlayer(g,headPosition+Vector3.forward*35);encounter.Simulate(.05f,g);
            Require(encounter.MusicEngaged&&encounter.approachMusic.isPlaying,"score stopped before head death");
            float volume=encounter.approachMusic.volume;g.Pause();encounter.Simulate(8,g);Require(encounter.approachMusic.volume==volume&&AudioListener.pause&&!encounter.approachMusic.ignoreListenerPause,"score ignores pause");g.Resume();
            head.ResetMonster(headPosition,Quaternion.identity);head.Hit(2);encounter.Simulate(.01f,g);
            Require(encounter.MusicEngaged&&encounter.approachMusic.isPlaying,"one death silences the surviving head's score");second.Retire();encounter.Simulate(.01f,g);
            Require(!head.Alive&&!head.hitCollider.enabled&&!encounter.MusicEngaged&&!encounter.approachMusic.isPlaying,"death leaves damage collider or score active");
            var camera=g.player.view.transform;g.player.hand.gameObject.SetActive(false);camera.position=headPosition+new Vector3(1.8f,1.3f,2.3f);camera.LookAt(headPosition+Vector3.up*.42f);
            head.Simulate(1.5f,g);Require(!head.Burning&&!head.burnLight.enabled&&head.deathEffects[0].particleCount==0,"fire precedes collapse");NortonArtShot(g,"64-head-collapsed",true);
            float deathTime=head.DeathTime;g.Pause();head.Simulate(3,g);Require(head.DeathTime==deathTime,"death animation advances in pause");g.Resume();head.Simulate(0,g);Require(head.DeathTime==deathTime,"zero time advances death");
            head.Simulate(2.5f,g);Require(head.Burning&&head.deathEffects[0].particleCount>5&&head.burnLight.enabled&&head.burnVoice.isPlaying,"collapsed head never ignites");NortonArtShot(g,"65-head-burning",false);
            head.Simulate(1.7f,g);Require(head.BurnAway>.4f,"head never chars/dissolves");NortonArtShot(g,"66-head-burning-to-ash",false);
            Require(encounter.TrySpawn(g)&&!head.Alive&&head.gameObject.activeSelf,"second slot unavailable or burning body reused early");
            Require(!encounter.TrySpawn(g),"pool exceeded two heads while remains burn");second.Retire();float beforeCleanup=encounter.NextAppearance;
            encounter.Simulate(2,g);Require(!head.gameObject.activeSelf&&!head.burnVoice.isPlaying&&!head.burnLight.enabled&&head.deathEffects[0].particleCount==0,"head remains standing or fire persists after disappearance");
            Require(Mathf.Abs(encounter.NextAppearance-(beforeCleanup-2))<.001f,"cleanup unexpectedly postpones the original recurrence");NortonArtShot(g,"67-head-disappeared",false);
            // Pool reuse must restore opaque skin, original shadow and collider.
            head.gameObject.SetActive(true);head.ResetMonster(headPosition,Quaternion.identity);Require(head.Alive&&head.hitCollider.enabled&&head.BurnAway==0&&!head.Burning&&!head.burnLight.enabled,"pooled head retains death/burn state");head.Retire();
            encounter.ResetEncounter(true,true);PlaceNortonPlayer(g,new Vector3(-75,0,-86));float timer=encounter.NextAppearance;
            encounter.Simulate(timer-.1f,g);Require(encounter.Appearances==0,"recurrence too frequent");encounter.Simulate(.11f,g);Require(encounter.Appearances==1,"recurrence after respite missing");
            Require(encounter.NextAppearance>=42&&encounter.NextAppearance<=60,"original 42-to-60-second cadence not restored");
            // Leave the first slot occupied; cadence should still spawn the second.
            Array.Find(encounter.enemies,e=>e.gameObject.activeSelf).Hit(2);
            timer=encounter.NextAppearance;encounter.Simulate(timer-.1f,g);
            // The first body has burned away, so keep one real living head present.
            Require(encounter.TrySpawn(g),"cannot occupy first slot for recurrence");encounter.Simulate(.11f,g);
            Require(encounter.Appearances==3&&Array.FindAll(encounter.enemies,e=>e.gameObject.activeSelf&&e.Alive).Length==2,"cadence cannot add a second living head");
            encounter.ResetEncounter(true,true);g.player.hand.gameObject.SetActive(true);
            Debug.Log("HEAD LIFECYCLE PASS: restored two-head pool and 42-to-60-second cadence (48 first/18 load); closest living head score survives one death; imported collapse, flame/smoke/embers/char/dissolve, pause/zero time, complete disappearance and clean reuse.");
        }
    }
}
