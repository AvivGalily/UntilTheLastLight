using System;
using UnityEngine;
using UnityEngine.AI;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void PlaceNortonPlayer(GameSession g,Vector3 position)
        {g.player.Teleport(position,0);g.player.Simulate(g.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);Physics.SyncTransforms();}
        static void VerifyNorton(GameSession g)
        {
            g.Resume();var scene=g.norton;Require(scene&&g.shotgun,"Norton encounter not wired");scene.ResetEncounter();
            for(int n=0;n<40;n++)scene.Simulate(.05f,g);
            var camera=g.player.view.transform;bool oldLanguage=g.Locale.Hebrew;
            g.player.hand.gameObject.SetActive(false);
            camera.position=ForestLayout.Wreck+new Vector3(3.8f,2.1f,5.7f);camera.LookAt(ForestLayout.Wreck+Vector3.up*.75f);UIShot(g,"45-ranger-burning-wreck",false);
            camera.position=ForestLayout.NortonBody+new Vector3(.85f,1.6f,2.0f);camera.LookAt(ForestLayout.NortonBody+new Vector3(0,.25f,.9f));UIShot(g,"46-norton-human-casualty",true);
            g.player.hand.gameObject.SetActive(true);PlaceNortonPlayer(g,ForestLayout.Cabin+new Vector3(0,0,7));
            Array.Find(g.items,i=>i.persistentId=="norton_shotgun").Use(g);Require(g.Inventory.HasShotgun&&g.Inventory.LoadedShells==2,"shotgun pickup/ammo");
            g.shotgun.Simulate(.02f,g);UIShot(g,"47-norton-held-shotgun",true);
            g.ReadNote(NortonEncounter.Letter);scene.Simulate(15,g);Require(scene.Unlocked&&!scene.Introduced&&scene.Appearances==0,"head appeared while the letter menu was open");
            g.Locale.SetHebrew(false);g.hud.Invalidate();UIShot(g,"48-norton-letter-english",true);
            g.Locale.SetHebrew(true);g.hud.Invalidate();UIShot(g,"49-norton-letter-hebrew",true);g.Locale.SetHebrew(oldLanguage);
            g.Resume();scene.Simulate(.02f,g);Require(scene.Introduced&&scene.Appearances==1&&scene.introduction.isPlaying,"first letter cry/appearance missing");
            int appearances=scene.Appearances;g.ReadNote(NortonEncounter.Letter);g.Resume();scene.Simulate(.02f,g);Require(scene.Appearances==appearances,"rereading replays the first appearance");
            // A real head follows and bites inside an illuminated protected cabin.
            PlaceNortonPlayer(g,ForestLayout.Cabin+Vector3.forward*2);g.Flashlight.Restore(100,true);g.Condition.Restore(false);g.Condition.Tick(11,true,g.tuning.player);
            var head=scene.enemies[0];head.gameObject.SetActive(true);head.ResetMonster(ForestLayout.Cabin-Vector3.forward*2,Quaternion.identity);scene.enemies[1].gameObject.SetActive(false);Physics.SyncTransforms();
            float distance=Vector3.Distance(head.transform.position,g.player.transform.position);
            head.Simulate(.3f,g);Require(g.IsSafe&&g.Flashlight.Active&&Vector3.Distance(head.transform.position,g.player.transform.position)<distance-.4f,"lit cabin repels the head");
            for(int n=0;n<100&&head.Bites==0;n++){g.Condition.Tick(.05f,g.IsSafe,g.tuning.player);head.Simulate(.05f,g);}
            Require(head.Bites==1&&g.Condition.Wounded&&g.State.Running,"head bite cannot injure a sheltered player");
            g.Condition.Restore(false);head.ResetMonster(ForestLayout.Cabin-Vector3.forward,Quaternion.identity);camera.LookAt(head.AimPoint);Physics.SyncTransforms();
            Require(g.shotgun.Fire(g)&&!head.Alive&&g.Inventory.LoadedShells==1,"aimed shotgun blast failed to kill head");
            Require(g.shotgun.BeginReload(g),"shotgun cannot reload");float remaining=g.shotgun.ReloadRemaining;g.Pause();g.shotgun.Simulate(8,g);Require(g.shotgun.ReloadRemaining==remaining,"reload continues in pause");g.Resume();g.shotgun.Simulate(1.8f,g);Require(g.Inventory.LoadedShells==2&&g.Inventory.SpareShells==11,"reload duplicates/loses shells");
            // A wall stops a blast; removing it makes the same shot hit.
            head.ResetMonster(ForestLayout.Cabin-Vector3.forward,Quaternion.identity);camera.LookAt(head.AimPoint);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LayerMask.NameToLayer("WorldSolid");wall.transform.position=Vector3.Lerp(camera.position,head.AimPoint,.5f);wall.transform.localScale=new Vector3(1.5f,2,.2f);Physics.SyncTransforms();
            Require(g.shotgun.Fire(g)&&head.Health==2,"shotgun passes through a wall");UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();g.shotgun.Simulate(1,g);Require(g.shotgun.Fire(g)&&!head.Alive,"unobstructed second shot misses");
            // Two actual timed axe swings, not a direct damage shortcut.
            head.ResetMonster(g.player.transform.position+Vector3.forward*1.2f,Quaternion.Euler(0,180,0));g.Inventory.HasAxe=true;g.Inventory.Weapon=HeldWeapon.Axe;g.axe.ResetCombat();camera.LookAt(head.AimPoint);Physics.SyncTransforms();
            Require(g.axe.BeginSwing(g),"cannot swing axe at head");g.axe.Simulate(.28f,g);Require(head.Health==1,"axe contact missed the head");g.axe.Simulate(.55f,g);Require(g.axe.BeginSwing(g),"second axe swing rejected");g.axe.Simulate(.28f,g);Require(!head.Alive,"head cannot be killed with axe");g.axe.Simulate(.55f,g);
            // Swept collision gives exactly one hit; the lit house offers no fire immunity.
            g.Condition.Restore(false);var p=scene.projectiles[0];Vector3 chest=g.player.controller.bounds.center;
            p.Launch(chest+Vector3.right*2.2f,chest,head);
            for(int n=0;n<40&&p.Flying;n++)p.Simulate(.02f,g);
            Require(p.PlayerImpacts==1&&g.Condition.FireRecoveryRemaining==10&&g.IsSafe,"fireball did not strike the protected player exactly once");p.Simulate(.3f,g);Require(p.PlayerImpacts==1,"one projectile hit twice");
            g.Pause();float recovery=g.Condition.FireRecoveryRemaining;scene.Simulate(12,g);Require(g.Condition.FireRecoveryRemaining==recovery,"burn healed while paused");g.Resume();
            g.Condition.Tick(9.99f,false,g.tuning.player);Require(g.Condition.Wounded,"burn healed too early");g.Condition.Tick(.02f,false,g.tuning.player);Require(!g.Condition.Wounded,"burn did not recover after ten seconds outdoors");
            p.Launch(chest+Vector3.right*2.2f,chest,head);for(int n=0;n<40&&p.Flying;n++)p.Simulate(.02f,g);Require(!g.Condition.Dead,"first hit after healing was lethal");g.Condition.Tick(1,false,g.tuning.player);
            p.Launch(chest+Vector3.right*2.2f,chest,head);for(int n=0;n<40&&p.Flying;n++)p.Simulate(.02f,g);Require(g.Condition.Dead&&!g.State.Running,"two fireballs failed to kill");
            if(g.State.Mode==ScreenMode.Dying)g.deathSequence.Simulate(g.deathSequence.Duration+.1f);Require(g.State.Mode==ScreenMode.Dead,"fire fatal menu missing");g.deathSequence.ResetSequence();g.Condition.Restore(false);g.State.Set(ScreenMode.Playing);
            // Both strong-lit cabins and the weak-lit research station admit recurring spawns.
            foreach(var house in new[]{ForestLayout.Cabin,ForestLayout.Store,ForestLayout.Research})
            {
                scene.ResetEncounter(true,true);PlaceNortonPlayer(g,house);Require(scene.TrySpawn(g),"no recurring head inside building "+house);
                Require(Array.Exists(scene.enemies,e=>e.gameObject.activeSelf&&ForestLayout.InBuilding(e.transform.position)),"building appearance spawned outside");
            }
            scene.ResetEncounter(true,true);PlaceNortonPlayer(g,ForestLayout.Cabin+Vector3.forward*16);Require(scene.TrySpawn(g),"no recurring forest head");
            head=Array.Find(scene.enemies,e=>e.gameObject.activeSelf);
            for(int n=0;n<130&&head.Spits==0;n++)head.Simulate(.05f,g);Require(head.Spits>0&&Array.Exists(scene.projectiles,q=>q.Flying),"live head never telegraphs/spits a fireball");
            Vector3 headPosition=head.transform.position;g.player.hand.gameObject.SetActive(false);camera.position=headPosition+head.transform.forward*2.7f+head.transform.right*.9f+Vector3.up*1.3f;camera.LookAt(headPosition+Vector3.up*.67f);
            RigShot(g,"50-six-legged-human-head",true);
            foreach(var q in scene.projectiles)if(q.Flying)q.Simulate(.12f,g);RigShot(g,"51-head-fireball",true);g.player.hand.gameObject.SetActive(true);
            scene.ResetEncounter(true,true);PlaceNortonPlayer(g,ForestLayout.Cabin);g.Condition.Restore(false);g.Condition.HitFire();g.Condition.Tick(3.5f,false,g.tuning.player);g.Inventory.Weapon=HeldWeapon.Shotgun;
            g.SaveCheckpoint("Verification Norton");int shells=g.Inventory.SpareShells;g.Continue();Require(g.norton.Unlocked&&g.norton.Introduced&&g.Inventory.HasShotgun&&g.Inventory.SpareShells==shells&&Mathf.Abs(g.Condition.FireRecoveryRemaining-6.5f)<.01f&&g.Documents.Contains(NortonEncounter.Letter),"new quest/weapon/burn checkpoint restore");
            g.Condition.Tick(10,true,g.tuning.player);g.SaveCheckpoint("Verification forest resumed");g.Pause();
            Debug.Log("NORTON SMOKE PASS: burning wreck/human viscera, bilingual letter, one-time cry, two lit-cabin/research and forest spawns, light-immune physical pursuit/bite, shotgun ammo/reload/pause/wall occlusion, timed axe kills, telegraphed spit, swept single-hit fireball, ten-second outdoor recovery, second-hit death menu and schema-five restore.");
        }
    }
}
