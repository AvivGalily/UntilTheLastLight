using System;
using UnityEngine;
using UnityEngine.AI;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void PlaceNortonPlayer(GameSession g,Vector3 position)
        {g.player.Teleport(position,0);g.player.Simulate(g.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);Physics.SyncTransforms();}
        static void NortonArtShot(GameSession g,string name,bool beam)
        {g.hud.gameObject.SetActive(false);try{RigShot(g,name,beam);}finally{g.hud.gameObject.SetActive(true);}}
        static void RequireSeparateShotgunHands(GameSession g,Vector3 torchPosition,Quaternion torchRotation,Vector3 gripPosition,Quaternion gripRotation)
        {
            Require(g.shotgun.heldModel.gameObject.activeInHierarchy&&g.player.handLight.gameObject.activeInHierarchy&&g.presence.flashlightGrip.gameObject.activeInHierarchy,"shotgun selection hides the left-hand torch or grip");
            var arms=g.shotgun.heldModel.GetComponentsInChildren<FirstPersonArm>(true);Require(arms.Length==1,"shotgun has an extra support hand");
            var grip=g.player.view.WorldToViewportPoint(arms[0].transform.position);Require(grip.z>g.player.view.nearClipPlane&&grip.x>0&&grip.x<1&&grip.y>0&&grip.y<1,"right trigger grip leaves the view during recoil/reload");
            Require(g.player.handLight.localPosition==torchPosition&&Quaternion.Angle(g.player.handLight.localRotation,torchRotation)<.001f&&g.presence.flashlightGrip.localPosition==gripPosition&&Quaternion.Angle(g.presence.flashlightGrip.localRotation,gripRotation)<.001f,"gun recoil/reload moves the separate left-hand flashlight");
        }
        static void VerifyNorton(GameSession g)
        {
            g.Resume();var scene=g.norton;Require(scene&&g.shotgun,"Norton encounter not wired");scene.ResetEncounter();
            for(int n=0;n<40;n++)scene.Simulate(.05f,g);
            Require(scene.carEffects[0].particleCount>10&&scene.carEffects[2].particleCount>5,"burning wreck does not emit flames/smoke");
            var camera=g.player.view.transform;bool oldLanguage=g.Locale.Hebrew;
            g.player.hand.gameObject.SetActive(false);
            Require(Vector3.Distance(scene.wreck.position,ForestLayout.Research)<20&&Vector3.Distance(scene.wreck.position,ForestLayout.Cabin)>80,"wreck still beside ranger cabin");
            Require(NavMesh.SamplePosition(ForestLayout.NortonBody+Vector3.right*2,out var arrival,1.5f,NavMesh.AllAreas),"research wreck interaction area is not reachable");
            var arrivalPath=new NavMeshPath();Require(NavMesh.CalculatePath(g.player.transform.position,arrival.position,NavMesh.AllAreas,arrivalPath)&&arrivalPath.status==NavMeshPathStatus.PathComplete,"no route from ranger cabin to relocated wreck");
            camera.position=ForestLayout.Wreck+new Vector3(3.8f,2.1f,7.5f);camera.LookAt(ForestLayout.Wreck+new Vector3(-1.3f,.75f,0));NortonArtShot(g,"53-research-burning-wreck",false);
            camera.position=ForestLayout.NortonBody+new Vector3(.85f,1.6f,-1.0f);camera.LookAt(ForestLayout.NortonBody+new Vector3(0,.25f,-.9f));NortonArtShot(g,"46-norton-human-casualty",true);
            foreach(string id in new[]{"norton_casualty","norton_letter","norton_shotgun"})
            {
                var item=Array.Find(g.items,i=>i.persistentId==id);var target=item.GetComponent<Collider>().bounds.center;var from=target+Vector3.up*1.6f+Vector3.back*.25f;Physics.SyncTransforms();
                Require(Physics.Raycast(from,(target-from).normalized,out var hit,g.tuning.player.interactionRange,g.SolidMask|LayerMask.GetMask("Interactable"),QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<WorldItem>()==item,"new scene item cannot be targeted: "+id);
            }
            g.player.hand.gameObject.SetActive(true);PlaceNortonPlayer(g,ForestLayout.Cabin+new Vector3(0,0,7));
            Array.Find(g.items,i=>i.persistentId=="norton_shotgun").Use(g);Require(g.Inventory.HasShotgun&&g.Inventory.LoadedShells==2,"shotgun pickup/ammo");
            g.axe.Simulate(.02f,g);g.shotgun.Simulate(.02f,g);
            var torchPosition=g.player.handLight.localPosition;var torchRotation=g.player.handLight.localRotation;
            var gripPosition=g.presence.flashlightGrip.localPosition;var gripRotation=g.presence.flashlightGrip.localRotation;
            RequireSeparateShotgunHands(g,torchPosition,torchRotation,gripPosition,gripRotation);
            UIShot(g,"47-norton-held-shotgun",true);NortonArtShot(g,"59-left-flashlight-right-shotgun",true);
            VerifyEquipmentInput(g);
            g.ReadNote(NortonEncounter.Letter);scene.Simulate(15,g);Require(scene.Unlocked&&!scene.Introduced&&scene.Appearances==0,"head appeared while the letter menu was open");
            g.Locale.SetHebrew(false);g.hud.Invalidate();UIShot(g,"48-norton-letter-english",true);
            g.Locale.SetHebrew(true);g.hud.Invalidate();UIShot(g,"49-norton-letter-hebrew",true);g.Locale.SetHebrew(oldLanguage);
            g.Resume();scene.Simulate(.02f,g);Require(scene.Introduced&&scene.Appearances==1&&scene.introduction.isPlaying&&scene.NextAppearance==48,"first letter cry/appearance/original cadence missing");
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
            g.shotgun.Simulate(.02f,g);RequireSeparateShotgunHands(g,torchPosition,torchRotation,gripPosition,gripRotation);NortonArtShot(g,"60-shotgun-recoil-separate-flashlight",true);
            Require(g.shotgun.BeginReload(g),"shotgun cannot reload");float remaining=g.shotgun.ReloadRemaining;g.Pause();g.shotgun.Simulate(8,g);Require(g.shotgun.ReloadRemaining==remaining,"reload continues in pause");g.Resume();g.shotgun.Simulate(.85f,g);
            RequireSeparateShotgunHands(g,torchPosition,torchRotation,gripPosition,gripRotation);NortonArtShot(g,"61-shotgun-reload-separate-flashlight",true);
            g.shotgun.Simulate(.95f,g);Require(g.Inventory.LoadedShells==2&&g.Inventory.SpareShells==11,"reload duplicates/loses shells");
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
            foreach(var q in scene.projectiles)if(q.Flying)q.Simulate(.12f,g);g.damage.ResetFeedback();NortonArtShot(g,"51-head-fireball",true);
            foreach(var q in scene.projectiles)q.ResetProjectile();headPosition=new Vector3(-75,0,-86);head.ResetMonster(headPosition,Quaternion.identity);
            camera.position=headPosition+new Vector3(1,1.15f,2.8f);camera.LookAt(headPosition+Vector3.up*.64f);head.blood.amount=.25f;head.blood.Refresh();NortonArtShot(g,"50-six-legged-human-head",true);
            camera.position=headPosition+new Vector3(.48f,.98f,1.4f);camera.LookAt(headPosition+Vector3.up*.79f);NortonArtShot(g,"68-head-skin-mouth-detail",true);
            head.PoseFatal(.58f,headPosition,camera.position);NortonArtShot(g,"52-head-bite-jaw",true);g.player.hand.gameObject.SetActive(true);
            VerifyHeadLifecycle(g);
            scene.ResetEncounter(true,true);PlaceNortonPlayer(g,ForestLayout.Cabin);g.Condition.Restore(false);g.Condition.HitFire();g.Condition.Tick(3.5f,false,g.tuning.player);g.Inventory.Weapon=HeldWeapon.Shotgun;
            g.SaveCheckpoint("Verification Norton");int shells=g.Inventory.SpareShells;g.Continue();Require(g.norton.Unlocked&&g.norton.Introduced&&g.Inventory.HasShotgun&&g.Inventory.SpareShells==shells&&Mathf.Abs(g.Condition.FireRecoveryRemaining-6.5f)<.01f&&g.Documents.Contains(NortonEncounter.Letter),"new quest/weapon/burn checkpoint restore");
            g.Condition.Tick(10,true,g.tuning.player);g.SaveCheckpoint("Verification forest resumed");g.Pause();
            Debug.Log("SEPARATE WEAPON HANDS PASS: left flashlight and right shotgun visible, one trigger grip, independent torch through recoil/reload.");
            Debug.Log("NORTON SMOKE PASS: burning wreck/human viscera, bilingual letter, one-time cry, two lit-cabin/research and forest spawns, light-immune physical pursuit/bite, shotgun ammo/reload/pause/wall occlusion, timed axe kills, telegraphed spit, swept single-hit fireball, ten-second outdoor recovery, second-hit death menu and schema-five restore.");
        }
    }
}
