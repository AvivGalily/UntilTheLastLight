using System;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void CombatMaterials()
        {
            string path=Root+"/Resources/BloodScreen.mat";
            var blood=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!blood){blood=new Material(Shader.Find("Last Light/Blood on lens"));AssetDatabase.CreateAsset(blood,path);}
            else blood.shader=Shader.Find("Last Light/Blood on lens");
            EditorUtility.SetDirty(blood);
        }
        static AudioSource PlayerVoice(Transform parent,string name,AudioClip clip=null,bool loop=false)
        {
            var go=new GameObject(name,typeof(AudioSource));go.transform.SetParent(parent,false);
            var source=go.GetComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.clip=clip;source.loop=loop;return source;
        }
        static void WireCombat(GameSession game,bool mechanics)
        {
            game.encounters=game.gameObject.AddComponent<MonsterEncounters>();game.damage=game.player.gameObject.AddComponent<DamageFeedback>();
            var foley=game.player.gameObject.AddComponent<PlayerFoley>();game.foley=foley;
            foley.footsteps=PlayerVoice(game.player.transform,"Boots on the ground");
            foley.calmBreath=PlayerVoice(game.player.transform,"Quiet breathing",Clip("player_breath_calm"),true);
            foley.hardBreath=PlayerVoice(game.player.transform,"Exertion breathing",Clip("player_breath_hard"),true);
            foley.injury=PlayerVoice(game.player.transform,"Human injury voice");foley.injury.ignoreListenerPause=true;
            foley.scream=Clip("player_scream");foley.hurt=Clip("player_hurt");
            foley.forestSteps=new[]{Clip("step_forest_1"),Clip("step_forest_2"),Clip("step_forest_3"),Clip("step_forest_4")};
            foley.woodSteps=new[]{Clip("step_wood_1"),Clip("step_wood_2"),Clip("step_wood_3")};
            game.axe=game.player.gameObject.AddComponent<AxeCombat>();
            // Blender +X cutting edge imports as -X: yaw 90 points it along camera +Z.
            game.axe.heldModel=ForestArt.Model("field_axe_01",new Vector3(.34f,-.50f,.66f),Quaternion.Euler(-10,90,8),game.player.hand,.62f).transform;
            game.axe.audioSource=PlayerVoice(game.player.transform,"Axe movement and impact");game.axe.swingClip=Clip("axe_swing");game.axe.hitClip=Clip("axe_impact");
            game.axe.heldModel.gameObject.SetActive(false);
            WirePlayerPresentation(game);
            if(mechanics)return;
            ConfigureWolfImport();
            Vector3[] places={new Vector3(-38,0,-55),new Vector3(15,0,-38),new Vector3(83,0,25),new Vector3(-58,0,9),new Vector3(-5,0,96)};
            game.wolves=new ZombieWolf[places.Length];
            for(int i=0;i<places.Length;i++)
            {
                var root=new GameObject("Undead wolf "+(i+1),typeof(ZombieWolf));root.layer=LayerMask.NameToLayer("Monster");root.transform.position=places[i];root.transform.rotation=Quaternion.Euler(0,57*i,0);
                var wolf=root.GetComponent<ZombieWolf>();game.wolves[i]=wolf;wolf.persistentId="undead_wolf_"+i;
                var model=ForestArt.Model("zombie_wolf_01",Vector3.zero,Quaternion.identity,root.transform);
                wolf.animator=model.GetComponentInChildren<Animator>();wolf.animator.applyRootMotion=false;wolf.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Meshes/zombie_wolf_01.fbx"))if(asset is AnimationClip clip)
                {if(clip.name=="Wolf_Idle")wolf.idle=clip;else if(clip.name=="Wolf_Run")wolf.run=clip;else if(clip.name=="Wolf_Bite")wolf.bite=clip;else if(clip.name=="Wolf_Death")wolf.death=clip;}
                if(!wolf.idle||!wolf.run||!wolf.bite||!wolf.death)throw new Exception("Wolf needs four skeletal animation clips.");
                var col=root.AddComponent<CapsuleCollider>();col.center=new Vector3(0,.53f,0);col.height=1.6f;col.radius=.28f;col.direction=2;col.isTrigger=true;wolf.hitCollider=col;
                wolf.voice=root.AddComponent<AudioSource>();wolf.voice.playOnAwake=false;wolf.voice.spatialBlend=1;wolf.voice.minDistance=3;wolf.voice.maxDistance=27;wolf.voice.dopplerLevel=0;
                wolf.growlClip=Clip("wolf_monster");wolf.howlClip=Clip("wolf_approach_howl");wolf.biteClip=Clip("zombie_bite");wolf.deathClip=Clip("zombie_death");
                wolf.barkVoice=root.AddComponent<AudioSource>();wolf.barkVoice.playOnAwake=false;wolf.barkVoice.spatialBlend=1;wolf.barkVoice.dopplerLevel=0;
                wolf.barkVoice.minDistance=2;wolf.barkVoice.maxDistance=30;wolf.barkVoice.rolloffMode=AudioRolloffMode.Custom;
                wolf.barkVoice.SetCustomCurve(AudioSourceCurveType.CustomRolloff,AnimationCurve.Linear(0,1,1,1));wolf.barkClip=Clip("wolf_bark");
                wolf.blood=root.AddComponent<BloodySurface>();wolf.blood.surfaces=model.GetComponentsInChildren<Renderer>();wolf.blood.wound0=new Vector4(.12f,.69f,.05f,.36f);wolf.blood.wound1=new Vector4(0,.87f,.63f,.22f);wolf.blood.amount=.65f;wolf.blood.Refresh();
                foreach(var bone in model.GetComponentsInChildren<Transform>()){if(bone.name=="b_Spine02")wolf.blood.anchor0=bone;else if(bone.name=="b_Head")wolf.blood.anchor1=bone;}wolf.blood.Refresh();
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;var bounds=skin.localBounds;bounds.Expand(1.6f);skin.localBounds=bounds;}
            }
        }
        static void ConfigureWolfImport()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Meshes/zombie_wolf_01.fbx");
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;
            importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)
            {
                clip.name=clip.takeName.Contains("Idle")?"Wolf_Idle":clip.takeName.Contains("Run")?"Wolf_Run":clip.takeName.Contains("Bite")?"Wolf_Bite":"Wolf_Death";
                clip.loopTime=clip.name=="Wolf_Idle"||clip.name=="Wolf_Run";clip.loopPose=clip.loopTime;
                clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        static void BloodOnBody(GameObject root,GameObject model,int index)
        {
            var blood=root.AddComponent<BloodySurface>();blood.surfaces=model.GetComponentsInChildren<Renderer>();
            blood.wound0=new Vector4(index%2==0?.09f:-.13f,.25f,.12f,.33f);blood.wound1=new Vector4(-.075f,.30f,.72f,.13f);blood.amount=1;blood.Refresh();
            // Flat irregular puddles remain beneath the body and do not obstruct navigation.
            var mat=Material("Dried blood on the forest floor",new Color(.065f,.0025f,.004f));mat.shader=Shader.Find("Last Light/Blood soaked soil");EditorUtility.SetDirty(mat);
            var mesh=new Mesh{name="Irregular blood seep"};var vertices=new Vector3[26];var uv=new Vector2[26];var tris=new int[25*3];
            vertices[0]=new Vector3(.08f,.017f,.18f);uv[0]=Vector2.one*.5f;
            for(int n=0;n<25;n++)
            {
                float a=n/25f*Mathf.PI*2,r=.50f+.03f*Mathf.Sin(a*3+index)+.025f*Mathf.Sin(a*7);
                vertices[n+1]=vertices[0]+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r*1.35f);uv[n+1]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.5f+Vector2.one*.5f;
                tris[n*3]=0;tris[n*3+1]=(n+1)%25+1;tris[n*3+2]=n+1;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();
            string path=Root+"/Data/BodyBlood_"+index+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var pool=new GameObject("Blood soaked ground",typeof(MeshFilter),typeof(MeshRenderer));pool.transform.SetParent(root.transform,false);pool.layer=LayerMask.NameToLayer("Decor");pool.GetComponent<MeshFilter>().sharedMesh=mesh;pool.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
    }
}
