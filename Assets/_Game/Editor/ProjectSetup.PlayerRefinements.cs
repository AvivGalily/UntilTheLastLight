using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        [MenuItem("Last Light/Player/Install locomotion, equipment grips and fallen passengers")]
        public static void InstallPlayerRefinements()
        {
            EditorSceneManager.OpenScene(Root+"/Scenes/ForestGame.unity");ForestArt.Prepare();
            var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
            var old=game.presence;
            foreach(var node in new[]{old.shadowBody,old.axeGrip,old.flashlightGrip,old.gripLight.transform})UnityEngine.Object.DestroyImmediate(node.gameObject);
            UnityEngine.Object.DestroyImmediate(old);
            var death=game.deathSequence;
            foreach(var node in death.defensiveHands)UnityEngine.Object.DestroyImmediate(node.gameObject);
            foreach(var node in new[]{death.voice.transform,death.impacts.transform,death.contactLight.transform,death.spray.transform})UnityEngine.Object.DestroyImmediate(node.gameObject);
            UnityEngine.Object.DestroyImmediate(death);
            foreach(var arm in game.shotgun.heldModel.GetComponentsInChildren<FirstPersonArm>(true))UnityEngine.Object.DestroyImmediate(arm.gameObject);
            game.axe.heldModel.localPosition=new Vector3(.30f,-.34f,.56f);
            WirePlayerPresentation(game);
            var grip=ForestArt.Model("player_shotgun_hand_01",new Vector3(0,-.014f,-.09f),Quaternion.identity,game.shotgun.heldModel,1/.68f);
            AttachBodyArm(grip.transform,game.player.view,false);grip.GetComponent<FirstPersonArm>().forearmAxis=Vector3.right;
            ConfigurePlayerShadowLights(game);
            game.foley.footsteps.Stop();game.foley.footsteps.loop=false;game.foley.footsteps.playOnAwake=false;game.foley.footsteps.clip=null;
            InstallArrivalHunters(game);
            PrefabUtility.SaveAsPrefabAsset(game.player.gameObject,Root+"/Prefabs/Player.prefab");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("PLAYER REFINEMENTS INSTALLED: weighted equipment arm IK, no player fill light, Tyler first corpse / Jack web victim / Ethan wreck casualty.");
        }
        static void InstallArrivalHunters(GameSession game)
        {
            foreach(var identity in UnityEngine.Object.FindObjectsByType<HunterIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(identity.gameObject.name.StartsWith("Fallen "))UnityEngine.Object.DestroyImmediate(identity.gameObject);
            string[] cast={"jack","tyler","ethan"};
            for(int n=0;n<3;n++)
            {
                var living=game.intro.dialogue.passengers[n].gameObject;
                var alive=living.GetComponent<HunterIdentity>();if(!alive)alive=living.AddComponent<HunterIdentity>();alive.characterId=cast[n];alive.deceased=false;alive.encounterRole="vehicle";
                PrefabUtility.SaveAsPrefabAsset(living,Root+"/Prefabs/intro_"+cast[n]+"_01.prefab");
                GameObject fallen;
                if(n==1)
                {
                    fallen=GameObject.Find("Forest body 0");
                    foreach(var lod in fallen.GetComponentsInChildren<LODGroup>())UnityEngine.Object.DestroyImmediate(lod.gameObject);
                    var model=ForestArt.Model("arrival_tyler_01",Vector3.zero,Quaternion.identity,fallen.transform);
                    var blood=fallen.GetComponent<BloodySurface>();blood.surfaces=model.GetComponentsInChildren<Renderer>();blood.Refresh();
                }
                else if(n==0)
                {
                    ReplaceWebHunter(game);fallen=game.forestEncounter.victim.gameObject;
                }
                else
                {
                    fallen=game.norton.casualty.gameObject;
                    foreach(Transform child in fallen.transform.Cast<Transform>().ToArray())if(child.name=="eviscerated_casualty_01")UnityEngine.Object.DestroyImmediate(child.gameObject);
                    var model=ForestArt.Model("eviscerated_casualty_01",Vector3.zero,Quaternion.identity,fallen.transform);
                    var blood=fallen.GetComponent<BloodySurface>();blood.surfaces=model.GetComponentsInChildren<Renderer>();blood.Refresh();
                }
                var dead=fallen.GetComponent<HunterIdentity>();if(!dead)dead=fallen.AddComponent<HunterIdentity>();dead.characterId=cast[n];dead.deceased=n!=0;dead.encounterRole=n==1?"first-body":n==0?"web-victim":"wreck-body";
                PrefabUtility.SaveAsPrefabAsset(fallen,Root+"/Prefabs/arrival_"+cast[n]+"_01.prefab");
            }
        }
        static void ReplaceWebHunter(GameSession game)
        {
            string path="Assets/_Game/Art/Meshes/web_jack_01.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.name=clip.takeName.Contains("Overpowered")?"Victim_Overpowered":clip.takeName.Contains("Dying")?"Victim_Dying":"Victim_Hanging";clip.loopTime=clip.name=="Victim_Hanging";clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
            var e=game.forestEncounter;var parent=e.victim.parent;var position=e.victim.localPosition;var rotation=e.victim.localRotation;
            UnityEngine.Object.DestroyImmediate(e.web.gameObject);UnityEngine.Object.DestroyImmediate(e.victim.gameObject);
            var victim=ForestArt.Model("web_jack_01",position,rotation,parent);e.victim=victim.transform;e.victimAnimator=victim.GetComponentInChildren<Animator>();e.victimAnimator.applyRootMotion=false;e.victimAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var animations=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>();e.hanging=animations.First(c=>c.name=="Victim_Hanging");e.overpowered=animations.First(c=>c.name=="Victim_Overpowered");e.dying=animations.First(c=>c.name=="Victim_Dying");
            var bones=victim.GetComponentsInChildren<Transform>();e.chest=bones.First(t=>t.name=="spine03");
            foreach(var skin in victim.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;var bounds=skin.localBounds;bounds.Expand(4);skin.localBounds=bounds;}
            e.wounds=victim.AddComponent<BloodySurface>();e.wounds.surfaces=victim.GetComponentsInChildren<Renderer>();e.wounds.amount=.75f;e.wounds.lacerations=.9f;e.wounds.anchor0=e.chest;e.wounds.anchor1=bones.First(t=>t.name=="head");
            var torso=victim.transform.InverseTransformPoint(e.chest.position+Vector3.back*.14f);var face=victim.transform.InverseTransformPoint(e.wounds.anchor1.position+new Vector3(.055f,.04f,-.13f));
            e.wounds.wound0=new Vector4(torso.x,torso.y,torso.z,.31f);e.wounds.wound1=new Vector4(face.x,face.y,face.z,.16f);e.wounds.Refresh();e.web=CreateVictimWeb(parent,victim);
        }
    }
}
