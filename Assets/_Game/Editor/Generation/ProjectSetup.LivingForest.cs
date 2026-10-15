using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void WireLivingForest(GameSession game)
        {
            game.wildlife=game.gameObject.AddComponent<ForestWildlife>();
            var trees=UnityEngine.Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("blackpine")||c.name.StartsWith("birch")||c.name.StartsWith("snag")).OrderBy(c=>c.transform.position.x).ToArray();
            game.wildlife.treeVisuals=trees.Select(c=>c.transform.GetChild(0)).ToArray();
            game.wildlife.crows=new CrowFlight[24];
            // Low branches beside the trails make the approaching flocks visible.
            for(int n=0;n<24;n++)
            {
                var trail=ForestLayout.Patrol[n/3];
                var tree=trees.OrderBy(c=>Vector3.Distance(c.transform.position,trail)).First();
                Vector3 perch=tree.transform.position+new Vector3((n%3-1)*.42f,2.1f+(n%3)*.13f,-.45f);
                var bird=ForestArt.Model("crow_01",perch,Quaternion.Euler(0,n*47,0),null,1.45f);
                var crow=bird.AddComponent<CrowFlight>();crow.seed=37+n*13;game.wildlife.crows[n]=crow;
                foreach(var t in bird.GetComponentsInChildren<Transform>()){if(t.name=="CrowWing_L")crow.leftWing=t;if(t.name=="CrowWing_R")crow.rightWing=t;}
                crow.voice=bird.AddComponent<AudioSource>();crow.voice.playOnAwake=false;crow.voice.spatialBlend=1;crow.voice.minDistance=3;crow.voice.maxDistance=40;crow.voice.dopplerLevel=.25f;crow.caw=Clip("crow_caw");crow.flutter=Clip("crow_wings");
                // Weathered branch under the feet. Its collider is decorative only.
                ForestArt.Model("fallen_log_01",perch-Vector3.up*.12f,Quaternion.Euler(0,0,-4),null,.4f).name="Raven roost branch";
            }
            string path="Assets/_Game/Art/Meshes/web_jack_01.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.name=clip.takeName.Contains("Overpowered")?"Victim_Overpowered":clip.takeName.Contains("Dying")?"Victim_Dying":"Victim_Hanging";clip.loopTime=clip.name=="Victim_Hanging";clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
            var root=new GameObject("Old oak ambush",typeof(ForestEncounter));root.transform.position=ForestLayout.EncounterTree;
            var encounter=root.GetComponent<ForestEncounter>();game.forestEncounter=encounter;
            // FBX mirrors Blender's X axis; turn the long overhang toward the victim.
            var treeArt=ForestArt.Model("gallows_oak_01",Vector3.zero,Quaternion.Euler(0,180,0),root.transform);
            treeArt.layer=LayerMask.NameToLayer("WorldSolid");var trunk=treeArt.AddComponent<CapsuleCollider>();trunk.radius=.45f;trunk.height=6;trunk.center=Vector3.up*3;
            var victim=ForestArt.Model("web_jack_01",new Vector3(2.25f,.85f,0),Quaternion.Euler(0,180,0),root.transform);
            encounter.victim=victim.transform;encounter.victimAnimator=victim.GetComponentInChildren<Animator>();encounter.victimAnimator.applyRootMotion=false;encounter.victimAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            encounter.hanging=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Victim_Hanging");
            encounter.overpowered=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Victim_Overpowered");
            encounter.dying=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Victim_Dying");
            encounter.chest=victim.GetComponentsInChildren<Transform>().First(t=>t.name=="spine03");
            foreach(var skin in victim.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;var bounds=skin.localBounds;bounds.Expand(4);skin.localBounds=bounds;}
            encounter.wounds=victim.AddComponent<BloodySurface>();encounter.wounds.surfaces=victim.GetComponentsInChildren<Renderer>();encounter.wounds.amount=.75f;encounter.wounds.lacerations=.9f;
            encounter.wounds.anchor0=encounter.chest;encounter.wounds.anchor1=victim.GetComponentsInChildren<Transform>().First(t=>t.name=="head");
            var torsoWound=victim.transform.InverseTransformPoint(encounter.chest.position+Vector3.back*.14f);
            var faceWound=victim.transform.InverseTransformPoint(encounter.wounds.anchor1.position+new Vector3(.055f,.04f,-.13f));
            encounter.wounds.wound0=new Vector4(torsoWound.x,torsoWound.y,torsoWound.z,.31f);encounter.wounds.wound1=new Vector4(faceWound.x,faceWound.y,faceWound.z,.16f);encounter.wounds.Refresh();
            encounter.web=CreateVictimWeb(root.transform,victim);
            var poolRoot=new GameObject("Ambush ground blood");poolRoot.transform.position=root.transform.position+new Vector3(2.3f,0,.15f);
            BloodOnBody(poolRoot,victim,5);UnityEngine.Object.DestroyImmediate(poolRoot.GetComponent<BloodySurface>());encounter.pool=poolRoot.transform;poolRoot.SetActive(false);
            var blood=new GameObject("Impact blood droplets",typeof(ParticleSystem));blood.transform.SetParent(root.transform,false);encounter.spray=blood.GetComponent<ParticleSystem>();encounter.spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=encounter.spray.main;main.playOnAwake=false;main.loop=false;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.85f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.2f,4.6f);main.startSize=new ParticleSystem.MinMaxCurve(.018f,.065f);main.startColor=new Color(.24f,.003f,.008f,1);main.gravityModifier=1.3f;main.maxParticles=240;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=encounter.spray.emission;emission.enabled=false;var shape=encounter.spray.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.16f;
            var dummy=GameObject.CreatePrimitive(PrimitiveType.Sphere);var mesh=dummy.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(dummy);
            var renderer=blood.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;
            renderer.sharedMaterial=Material("Fresh ambush blood",new Color(.17f,.001f,.005f));renderer.sharedMaterial.SetFloat("_Smoothness",.52f);renderer.shadowCastingMode=ShadowCastingMode.Off;
            encounter.voice=root.AddComponent<AudioSource>();encounter.voice.playOnAwake=false;encounter.voice.spatialBlend=1;encounter.voice.minDistance=7;encounter.voice.maxDistance=42;
            encounter.impact=PlayerVoice(root.transform,"Ambush impacts");
            encounter.score=PlayerVoice(root.transform,"Escape score",Clip("escape_score"),true);
            encounter.heartbeat=PlayerVoice(root.transform,"Panic heartbeat",Clip("panic_heartbeat"));
            encounter.helpClip=Clip("victim_help");encounter.screamClip=Clip("player_scream");encounter.stingClip=Clip("gallows_sting");encounter.biteClip=Clip("feeding_impact");encounter.powerClip=Clip("flashlight_short");
        }
    }
}
