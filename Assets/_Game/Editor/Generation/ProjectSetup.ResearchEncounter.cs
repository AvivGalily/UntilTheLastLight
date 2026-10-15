using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void ConfigureAquaticImport()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Meshes/research_abomination_01.fbx");
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)
            {
                clip.name="Aquatic_"+new[]{"Idle","Escape","Run","Attack","Death","Hit"}.First(n=>clip.takeName.Contains(n));
                clip.loopTime=clip.name.EndsWith("Idle")||clip.name.EndsWith("Run");clip.loopPose=clip.loopTime;
                clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        static void CreateAquaticMonster(ResearchSpecimen specimen,Transform suspended)
        {
            ConfigureAquaticImport();
            var root=new GameObject("R-09 aquatic predator",typeof(AquaticMonster)).transform;root.SetParent(suspended,false);root.gameObject.layer=LayerMask.NameToLayer("Monster");
            var monster=root.GetComponent<AquaticMonster>();specimen.monster=monster;monster.containment=specimen;
            var model=ForestArt.Model("research_abomination_01",Vector3.zero,Quaternion.identity,root);
            monster.animator=model.GetComponentInChildren<Animator>();monster.animator.applyRootMotion=false;monster.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Meshes/research_abomination_01.fbx"))if(asset is AnimationClip clip)
            {
                switch(clip.name){case "Aquatic_Idle":monster.idle=clip;break;case "Aquatic_Escape":monster.escape=clip;break;case "Aquatic_Run":monster.run=clip;break;case "Aquatic_Attack":monster.attack=clip;break;case "Aquatic_Death":monster.death=clip;break;case "Aquatic_Hit":monster.hit=clip;break;}
            }
            if(!monster.idle||!monster.escape||!monster.run||!monster.attack||!monster.death||!monster.hit)throw new Exception("R-09 needs six skeletal animation clips.");
            var hit=root.gameObject.AddComponent<CapsuleCollider>();hit.radius=.35f;hit.height=2.15f;hit.center=Vector3.up*1.075f;hit.isTrigger=true;monster.hitCollider=hit;
            monster.voice=root.gameObject.AddComponent<AudioSource>();monster.voice.playOnAwake=false;monster.voice.spatialBlend=1;monster.voice.minDistance=2;monster.voice.maxDistance=34;monster.voice.dopplerLevel=0;
            monster.growlClip=Clip("research_growl");monster.attackClip=Clip("research_attack");monster.deathClip=Clip("research_death");
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;var bounds=skin.localBounds;bounds.Expand(2.5f);skin.localBounds=bounds;}
            CreateAquaticNecklace(specimen,root,model.transform);
        }
        static Transform CurvedGlass(string name,float from,float to,Material glass,Transform parent,Vector3 hinge)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int segments=48;
            for(int y=0;y<2;y++)for(int i=0;i<=segments;i++)
            {
                float a=Mathf.Lerp(from,to,i/(float)segments)*Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Cos(a)*1.01f,.31f+y*2.58f,Mathf.Sin(a)*1.01f)-hinge);uv.Add(new Vector2(i/(float)segments,y));
                if(y==1&&i<segments){int k=segments+1+i;triangles.AddRange(new[]{i,k,i+1,i+1,k,k+1});}
            }
            var panel=LabMesh(name,vertices,triangles,uv,glass,parent);panel.transform.localPosition=hinge;panel.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return panel.transform;
        }
        static void ContainmentDoors(ResearchSpecimen specimen,Material glass,Transform tank)
        {
            CurvedGlass("Fixed rear containment glass",180,360,glass,tank,Vector3.zero);
            specimen.leftDoor=CurvedGlass("Left opening containment glass",90,180,glass,tank,new Vector3(-1.01f,0,0));
            specimen.rightDoor=CurvedGlass("Right opening containment glass",0,90,glass,tank,new Vector3(1.01f,0,0));
        }
        static void ResearchReleaseControl(Transform root,ResearchSpecimen specimen,Material steel,Material rubber)
        {
            var pedestal=new GameObject("Containment release control").transform;pedestal.SetParent(root,false);pedestal.localPosition=new Vector3(-2.8f,0,-2.25f);
            LabCylinder("Release pedestal",new Vector3(0,.52f,0),.06f,1.04f,steel,pedestal);
            Box("Release control housing",new Vector3(0,1.11f,0),new Vector3(.37f,.23f,.24f),steel,pedestal,false);
            var red=LabMaterial("Containment emergency red",new Color(.28f,.008f,.005f),.35f,0,false,new Color(.04f,.001f,0));
            var button=LabCylinder("Press E - release R09",new Vector3(0,1.12f,.145f),.068f,.044f,red,pedestal);button.transform.localRotation=Quaternion.Euler(90,0,0);button.layer=LayerMask.NameToLayer("Interactable");
            var item=button.AddComponent<WorldItem>();item.kind=ItemKind.ResearchRelease;item.persistentId="research_release";items.Add(item);var trigger=button.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.65f;
            LabText("Release caution label","RELEASE / R-09",new Vector3(0,1.22f,.13f),Quaternion.Euler(0,180,0),.45f,pedestal,new Color(.80f,.70f,.40f));
            var tank=specimen.transform;
            LabCylinder("Alarm beacon base",new Vector3(0,3.14f,.82f),.18f,.08f,steel,tank);
            var lens=LabMaterial("Containment alarm red glass",new Color(.38f,.003f,.002f),.65f,0,false,new Color(.04f,0,0));
            LabPbr(lens,"LabGlass",1,.12f,new Color(.45f,.004f,.002f));
            specimen.beaconGlass=LabSphere("Red alarm dome",new Vector3(0,3.29f,.82f),new Vector3(.29f,.24f,.29f),lens,tank).GetComponent<Renderer>();
            var rotor=new GameObject("Rotating red siren").transform;rotor.SetParent(tank,false);rotor.localPosition=new Vector3(0,3.25f,.82f);specimen.beaconRotor=rotor;
            var light=new GameObject("Siren red sweep",typeof(Light)).GetComponent<Light>();light.transform.SetParent(rotor,false);light.transform.localRotation=Quaternion.Euler(18,0,0);light.type=LightType.Spot;light.spotAngle=105;light.innerSpotAngle=65;light.color=new Color(1,.008f,.002f);light.range=9;light.intensity=4;light.enabled=false;specimen.alarmLight=light;
            specimen.alarm=tank.gameObject.AddComponent<AudioSource>();specimen.alarm.playOnAwake=false;specimen.alarm.spatialBlend=1;specimen.alarm.minDistance=4;specimen.alarm.maxDistance=45;specimen.alarm.dopplerLevel=0;
            specimen.mechanism=tank.gameObject.AddComponent<AudioSource>();specimen.mechanism.playOnAwake=false;specimen.mechanism.spatialBlend=1;specimen.mechanism.minDistance=2;specimen.mechanism.maxDistance=18;
            specimen.alarmClip=Clip("research_alarm");specimen.openClip=Clip("research_hatch");specimen.drainClip=Clip("research_drain");
        }
        static void ResearchShelvedSpecimens(Transform root,Material steel,Material glass,Material fluid)
        {
            var preservative=LabMaterial("Amber specimen preservative",new Color(.20f,.22f,.08f,.08f),.5f,0,true);
            preservative.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+"LabFluid_BaseColor.png"));preservative.SetColor("_BaseColor",new Color(.65f,.50f,.16f,.8f));
            var labels=LabMaterial("Specimen archive jar labels",Color.white,.18f);labels.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+"LabLabels_BaseColor.png"));LabPbr(labels,"LabPaper",1,.2f);
            for(int row=0;row<4;row++)for(int column=0;column<7;column++)
            {
                int index=row*7+column,kind=(row+column)%4;
                float radius=kind==0?.14f:.22f,height=kind==0?.39f:.57f;
                var jar=new GameObject("Archive specimen jar "+index+" - "+new[]{"eyes","embryo","wolf head","deer head"}[kind]).transform;jar.SetParent(root,false);
                jar.localPosition=new Vector3(-7.65f,.39f+row*.68f,-5.55f+column*.79f);
                // Archive is approached from +X, so labels and faces point into the room.
                jar.localRotation=Quaternion.Euler(0,90,0);
                LabShell("Archive vessel glass "+index,radius,height,glass,jar);
                LabCylinder("Thick glass foot "+index,new Vector3(0,.008f,0),radius,.016f,glass,jar);
                LabShell("Preservative fluid "+index,radius-.007f,height-.055f,preservative,jar);
                LabCylinder("Archive screw lid "+index,new Vector3(0,height+.012f,0),radius+.012f,.035f,steel,jar);
                for(int ridge=0;ridge<3;ridge++)LabCylinder("Lid thread "+index+"_"+ridge,new Vector3(0,height-.01f+ridge*.009f,0),radius+.014f,.003f,steel,jar);
                string model=kind==0?"lab_eye_01":kind==1?"lab_embryo_01":kind==2?"lab_wolf_head_01":"lab_deer_head_01";
                var specimen=ForestArt.Model(model,new Vector3(0,.038f,0),Quaternion.Euler(0,(index%3-1)*16,kind==1?-12:0),jar,kind==0?1:1.06f);
                if(kind==0)ForestArt.Model(model,new Vector3(.025f,.205f,-.02f),Quaternion.Euler(10,-24,14),jar,.88f);
                LabCard("Archive identification "+index,new Vector3(0,height*.26f,radius+.003f),new Vector2(radius*1.2f,.095f),labels,jar,2);
                var full=jar.GetComponentsInChildren<Renderer>();
                var middle=full.Where(r=>!r.name.StartsWith("Lid thread")&&!r.name.StartsWith("Archive identification")).ToArray();
                var far=middle.Where(r=>!r.name.StartsWith("Preservative fluid")&&!r.name.StartsWith("Thick glass foot")).ToArray();
                var lod=jar.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,full),new LOD(.04f,middle),new LOD(.018f,far)});lod.RecalculateBounds();
            }
            var lamp=new GameObject("Archive specimen inspection lamp",typeof(Light)).GetComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(-6.8f,2.7f,-2.9f);lamp.type=LightType.Point;lamp.color=new Color(.54f,.62f,.49f);lamp.range=6;lamp.intensity=.7f;
        }
        static void AddCabinGateLock(GameSession game)
        {
            var old=GameObject.Find("Cabin gate key lock");if(old)UnityEngine.Object.DestroyImmediate(old);
            var parent=new GameObject("Cabin gate key lock").transform;parent.position=ForestLayout.Cabin+new Vector3(2.14f,1.48f,-3.65f);
            var steel=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Laboratory brushed steel.mat");
            Box("Gate lock escutcheon",Vector3.zero,new Vector3(.34f,.46f,.08f),steel,parent,false);
            var lockObject=LabCylinder("Gate key cylinder",new Vector3(0,0,.06f),.075f,.04f,steel,parent);lockObject.transform.localRotation=Quaternion.Euler(90,0,0);lockObject.layer=LayerMask.NameToLayer("Interactable");
            var item=lockObject.AddComponent<WorldItem>();item.persistentId="cabin_gate_lock";item.kind=ItemKind.GateLock;items.Add(item);var col=lockObject.AddComponent<SphereCollider>();col.isTrigger=true;col.radius=1.6f;
            var turn=new GameObject("Turn gate key").transform;turn.SetParent(parent,false);turn.localPosition=new Vector3(0,0,.085f);game.cabinKeySwitch=turn;
            var dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Containment rubber hoses.mat");
            Box("Keyway slot",Vector3.zero,new Vector3(.014f,.08f,.008f),dark,turn,false);
            Box("Lock turn indicator",new Vector3(0,.064f,0),new Vector3(.012f,.021f,.01f),dark,turn,false);
            LabText("Gate key lock caption","GATE / KEY",new Vector3(0,.16f,.047f),Quaternion.Euler(0,180,0),.47f,parent,new Color(.74f,.71f,.52f));
        }
        [MenuItem("Last Light/Research/Install release encounter and gate key")]
        public static void InstallResearchEncounter()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);ForestArt.Prepare();
            EditorSceneManager.OpenScene(Root+"/Scenes/ForestGame.unity");font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/FieldSans.asset");
            var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();var station=GameObject.Find("Abandoned research station").transform;
            var old=station.Find("Research laboratory equipment");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);ResearchLaboratory(station);
            game.researchSpecimen=station.GetComponentInChildren<ResearchSpecimen>();AddCabinGateLock(game);
            game.items=UnityEngine.Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(i=>i.persistentId).ToArray();
            Physics.SyncTransforms();var navigation=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();var previous=navigation.navMeshData;navigation.BuildNavMesh();
            EditorUtility.CopySerialized(navigation.navMeshData,previous);navigation.RemoveData();navigation.navMeshData=previous;navigation.AddData();EditorUtility.SetDirty(previous);
            ConfigurePlayerShadowLights(game);PlayerSettings.bundleVersion="0.14.2";
            EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();ProjectValidator.ValidateAll();
            Debug.Log("RESEARCH ENCOUNTER INSTALLED: 28 textured archive jars, rigged R-09, five-second drain/siren, opening hatches, release control, searchable carcass and cabin gate key lock.");
        }
    }
}
