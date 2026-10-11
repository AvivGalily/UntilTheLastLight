using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static double started;
        static int stage;
        static float charge;
        static float pausedWildlife,pausedFog;
        static Vector3 pausedWisp;
        static bool failed;
        static readonly string Output="Docs/Screenshots";
        [MenuItem("Last Light/3 Capture forest art")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");
            started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=CaptureWhenReady;
        }
        static void CaptureWhenReady()
        {
            if(EditorApplication.timeSinceStartup-started<3)return;
            EditorApplication.update-=CaptureWhenReady;
            try
            {
                Directory.CreateDirectory(Output);
                ShaderUtil.allowAsyncCompilation=false;
                var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
                foreach(var wound in UnityEngine.Object.FindObjectsByType<BloodySurface>(FindObjectsSortMode.None))wound.Refresh();
                Shader.SetGlobalFloat("_AtmosphereTime",12);
                game.hud.gameObject.SetActive(false);var camera=game.player.view;
                camera.enabled=true;camera.clearFlags=CameraClearFlags.Skybox;
                Shot(camera,"01-ranger-shelter",ForestLayout.Cabin+new Vector3(0,2,14),ForestLayout.Cabin+Vector3.up*2,false);
                Shot(camera,"02-service-trail",new Vector3(-66,1.7f,-73),new Vector3(-40,2,-53),true);
                Shot(camera,"03-ranger-interior",ForestLayout.Cabin+Vector3.up*1.7f,ForestLayout.Cabin+new Vector3(-2.7f,1,1),true);
                Shot(camera,"04-east-storehouse",ForestLayout.Store+new Vector3(0,2,14),ForestLayout.Store+Vector3.up*2,true);
                Shot(camera,"06-surveyor-map",ForestLayout.Bodies[0]+new Vector3(1.3f,1.7f,1.2f),ForestLayout.Bodies[0]+Vector3.up*.25f,true);
                for(int n=0;n<4;n++)
                {
                    var body=GameObject.Find("Forest body "+n).transform;
                    var face=body.TransformPoint(new Vector3(0,.24f,.74f));
                    Shot(camera,"15-body-"+(n+1),body.TransformPoint(new Vector3(.85f,1.6f,1.1f)),body.TransformPoint(new Vector3(0,.18f,-.08f)),true);
                    if(n==0)
                    {
                        Shot(camera,"19-human-face",face+body.TransformDirection(new Vector3(.13f,.51f,.33f)),face,true);
                    }
                }
                Shot(camera,"07-research-corridor",ForestLayout.Research+new Vector3(0,1.7f,5.6f),ForestLayout.Research+new Vector3(0,1.6f,-3),false);
                Shot(camera,"08-research-room",ForestLayout.Research+new Vector3(-2.8f,1.7f,5.6f),ForestLayout.Research+new Vector3(-5,1,-1),true);
                game.monster.gameObject.SetActive(true);game.monster.transform.SetPositionAndRotation(ForestLayout.Cabin+new Vector3(0,0,18),Quaternion.Euler(0,180,0));
                var jaw=game.creature.mouth;int jawIndex=HeroTree.ShapeIndex(jaw.sharedMesh,"Jaw_Open");
                if(jawIndex<0)throw new Exception("Missing mouth shape");
                jaw.SetBlendShapeWeight(jawIndex,0);
                CreatureShot(camera,jaw,"09-creature-closed",ForestLayout.Cabin+new Vector3(0,1.7f,12),game.monster.transform.position+Vector3.up*1.0f);
                jaw.SetBlendShapeWeight(jawIndex,100);
                CreatureShot(camera,jaw,"10-creature-open",ForestLayout.Cabin+new Vector3(.3f,1.8f,15),game.monster.transform.position+Vector3.up*1.25f);
                game.monster.gameObject.SetActive(false);
                RenderSettings.fogDensity=ForestAtmosphere.ThreatDensity;Shader.SetGlobalFloat("_Dread",1);
                foreach(var hero in UnityEngine.Object.FindObjectsByType<HeroTree>(FindObjectsSortMode.None))
                    foreach(var crown in hero.crowns)
                        for(int n=0;n<crown.sharedMesh.blendShapeCount;n++)crown.SetBlendShapeWeight(n,100);
                Shot(camera,"05-dread-forest",new Vector3(-66,1.7f,-73),new Vector3(-40,2,-53),true);
                Debug.Log("VISUAL CAPTURE PASS: fifteen 1600x900 Unity URP images, four human bodies, face detail and dense fog at "+Output);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void Shot(Camera camera,string name,Vector3 position,Vector3 target,bool beam)
        {
            foreach(var arm in UnityEngine.Object.FindObjectsByType<FirstPersonArm>(FindObjectsSortMode.None))arm.Pose();
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            camera.fieldOfView=65;
            camera.GetComponentInChildren<Light>().enabled=beam;
            var atmosphere=UnityEngine.Object.FindFirstObjectByType<ForestAtmosphere>();
            if(atmosphere)
            {
                // The capture camera visits distant sites without running player movement.
                // Place the runtime smoke around each viewpoint for an accurate comparison.
                atmosphere.ResetSequence(821,position);Shader.SetGlobalFloat("_AtmosphereTime",12);
                foreach(var wisp in atmosphere.wisps)
                {
                    wisp.transform.rotation=Quaternion.LookRotation(wisp.transform.position-position,Vector3.up);
                    wisp.enabled=!ForestLayout.InBuilding(wisp.transform.position,1.5f);
                }
            }
            var light=camera.GetComponentInChildren<Light>();
            Shader.SetGlobalVector("_MistBeamPosition",light.transform.position);Shader.SetGlobalVector("_MistBeamDirection",light.transform.forward);
            Shader.SetGlobalVector("_MistBeamParams",new Vector4(light.range,Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad),beam?1:0,0));
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
            if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new Exception("URP render request unavailable");
            RenderPipeline.SubmitRenderRequest(camera,request);
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
        }
        static void CreatureShot(Camera camera,SkinnedMeshRenderer jaw,string name,Vector3 position,Vector3 target)
        {
            // Bake the selected morph synchronously; multiple editor shots otherwise share one skinning update.
            var mesh=new Mesh();jaw.BakeMesh(mesh);
            var go=new GameObject("Verification baked jaw",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(jaw.transform,false);
            go.transform.localScale=new Vector3(1/jaw.transform.lossyScale.x,1/jaw.transform.lossyScale.y,1/jaw.transform.lossyScale.z);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=jaw.sharedMaterials;
            jaw.enabled=false;Shot(camera,name,position,target,true);jaw.enabled=true;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);
        }
        static void UIShot(GameSession game,string name,bool beam=false)
        {
            Directory.CreateDirectory(Output);game.hud.Refresh();
            var camera=game.player.view;var sizing=new RenderTexture(1600,900,24);sizing.Create();camera.targetTexture=sizing;
            var canvas=game.hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.25f;canvas.sortingOrder=100;
            Canvas.ForceUpdateCanvases();
            Shot(camera,name,camera.transform.position,camera.transform.position+camera.transform.forward,beam);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;camera.targetTexture=null;sizing.Release();UnityEngine.Object.DestroyImmediate(sizing);
        }
        public static void Smoke()
        {
            SessionState.SetBool("LastLight.Smoke",true);
            SessionState.SetString("LastLight.SmokeSave",Path.Combine(Path.GetTempPath(),"LastLightSmoke-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");
            AttachSmoke();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void ResumeSmoke()
        {
            if(SessionState.GetBool("LastLight.Smoke",false))AttachSmoke();
        }
        static void AttachSmoke()
        {
            GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.SmokeSave",null);
            started=EditorApplication.timeSinceStartup;stage=0;failed=false;
            EditorApplication.update-=SmokeStep;EditorApplication.update+=SmokeStep;
            Application.logMessageReceived-=ObserveError;Application.logMessageReceived+=ObserveError;
        }
        static void ObserveError(string condition,string stack,LogType type)
        { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true; }
        static void Require(bool condition,string message){if(!condition)throw new Exception("SMOKE: "+message);}
        static void SmokeStep()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
                if(!game||game.State==null)return;
                double elapsed=EditorApplication.timeSinceStartup-started;
                if(stage==0)
                {
                    bool preferred=game.Locale.Hebrew;game.Locale.SetHebrew(true);game.State.Set(ScreenMode.Title);UIShot(game,"11-hebrew-title");game.Locale.SetHebrew(preferred);
                    // This suite exercises existing cabin/forest mechanics. The
                    // opening's complete route is covered by IntroVerification.
                    if(game.intro)game.intro.enabled=false;
                    game.StartNew();Require(game.monster.HasSpawned,"no legal monster spawn");
                    game.OpenMap();Require(game.State.Running&&!game.HasMap,"map granted before discovery");
                    Array.Find(game.items,item=>item.kind==ItemKind.MapCorpse).Use(game);
                    Require(game.HasMap&&game.Documents.Contains("note_cartographer"),"surveyor map/document pickup");
                    game.OpenMap();Require(game.State.Mode==ScreenMode.Map,"recovered map unavailable");game.Resume();
                    foreach(var item in game.items)if(item.kind==ItemKind.Corpse)item.Use(game);
                    Require(game.Documents.Contains("note_research"),"body documents");game.Resume();
                    bool language=game.Locale.Hebrew;game.Locale.SetHebrew(true);game.hud.Refresh();Require(HebrewText.ContainsHebrew(game.hud.objective.text),"Hebrew UI missing");
                    game.OpenMap();UIShot(game,"12-recovered-map-hebrew");game.ReadNote("note_research");UIShot(game,"13-hebrew-document");
                    game.Locale.SetHebrew(false);game.OpenMap();UIShot(game,"14-recovered-map-english");game.Locale.SetHebrew(language);game.Resume();
                    Require(game.creature.HasRig&&game.creature.mouth.bones.Length==28&&!game.creature.animator.applyRootMotion,"weighted crawler presentation");
                    game.Flashlight.Restore(100,false);game.player.Teleport(ForestLayout.Research,0);Physics.SyncTransforms();
                    Require(!game.IsSafe&&game.Protection.Capsule(ForestLayout.Research+Vector3.forward*2,game.tuning.monster.radius,game.tuning.monster.height)==Protection.None,"weak bulb grants protection");
                    var path=new UnityEngine.AI.NavMeshPath();
                    foreach(var point in new[]{new Vector3(-3.8f,0,1),new Vector3(3.7f,0,1.8f),new Vector3(3.5f,0,-1.5f)})
                        Require(UnityEngine.AI.NavMesh.CalculatePath(ForestLayout.Research+new Vector3(0,0,6),ForestLayout.Research+point,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"research room unreachable: "+point);
                    Vector3 original=game.monster.transform.position;game.monster.transform.position=game.player.transform.position+Vector3.right*4;
                    game.monster.Tick(.016f,game);game.creature.Simulate(1,game.monster);
                    int jaw=HeroTree.ShapeIndex(game.creature.mouth.sharedMesh,"Jaw_Open");
                    float weight=game.creature.mouth.GetBlendShapeWeight(jaw);Require(weight>90,"jaw did not open near player");
                    game.creature.Simulate(0,game.monster);Require(Mathf.Approximately(weight,game.creature.mouth.GetBlendShapeWeight(jaw)),"jaw advanced with zero time");
                    game.horrorAudio.Simulate(.6f,game);Require(game.monster.ActualSpeed>.08f||!game.horrorAudio.clicks.isPlaying,"stationary creature emits footsteps");
                    Require(game.horrorAudio.tension.volume>0,"proximity tension layer missing");
                    float threat=game.horrorAudio.Threat;game.horrorAudio.Simulate(0,game);Require(Mathf.Approximately(threat,game.horrorAudio.Threat),"audio director advanced with zero time");
                    game.monster.transform.position=original;
                    game.player.Teleport(game.cabinSpawn.position,0);game.Flashlight.Restore(100,true);
                    game.horrorAudio.Simulate(40,game);Require(game.horrorAudio.wolf.isPlaying,"scheduled wolf howl missing");
                    game.monster.warningSource.Stop();game.ambience.Simulate(11,game);
                    Require(game.ambience.insects.isPlaying&&game.ambience.insects.volume>0&&game.ambience.insects.volume<=.0121f&&game.ambience.BirdCalls>0&&game.ambience.birds.isPlaying,"quiet building crickets or intermittent night wildlife missing");
                    game.player.Teleport(ForestLayout.Cabin+Vector3.forward*30,0);game.ambience.Simulate(1,game);
                    Require(!game.ambience.insects.isPlaying&&game.ambience.insects.volume==0,"crickets leaked into open forest");
                    game.player.Teleport(ForestLayout.Cabin+Vector3.forward*6,0);game.ambience.Simulate(2,game);float outsideCrickets=game.ambience.insects.volume;
                    Require(outsideCrickets>0&&outsideCrickets<=.0121f,"building approach did not restore quiet crickets");
                    game.player.Teleport(game.cabinSpawn.position,0);game.ambience.Simulate(2,game);
                    Require(game.ambience.insects.volume>0&&game.ambience.insects.volume<=.00457f,"indoor cricket muffling missing");
                    Debug.Log("BUILDING CRICKETS PASS: quiet near buildings, silent in open forest, more muffled indoors, no autoplay.");
                    float wildlifeTime=game.ambience.ActiveTime;game.ambience.Simulate(0,game);Require(game.ambience.ActiveTime==wildlifeTime,"wildlife advanced with zero time");
                    var beforeFog=game.atmosphere.wisps[0].transform.position;
                    game.atmosphere.Simulate(1,game);float fogTime=game.atmosphere.ActiveTime;
                    var cloudPosition=game.atmosphere.wisps[0].transform.position;game.atmosphere.Simulate(0,game);
                    Require(cloudPosition!=beforeFog,"airborne fog did not drift");
                    Require(game.atmosphere.ActiveTime==fogTime&&game.atmosphere.wisps[0].transform.position==cloudPosition,"fog moved with zero time");
                    Require(game.atmosphere.wisps.Length==42&&RenderSettings.fogDensity>.015f,"dense atmosphere missing");
                    game.player.Simulate(game.tuning.player,Vector2.up,Vector2.zero,false,false,.5f);
                    Require(game.player.transform.position.z>game.cabinSpawn.position.z+.5f,"player movement blocked");
                    game.Pause();charge=game.Flashlight.Charge;
                    pausedWildlife=game.ambience.ActiveTime;pausedFog=game.atmosphere.ActiveTime;pausedWisp=game.atmosphere.wisps[0].transform.position;
                    stage=1;started=EditorApplication.timeSinceStartup;
                }
                else if(stage==1&&elapsed>.6)
                {
                    Require(Mathf.Approximately(charge,game.Flashlight.Charge),"battery advanced while paused");
                    Require(AudioListener.pause&&game.ambience.ActiveTime==pausedWildlife,"wildlife did not pause with menu");
                    Require(game.atmosphere.ActiveTime==pausedFog&&game.atmosphere.wisps[0].transform.position==pausedWisp,"atmosphere did not pause with menu");
                    VerifyCombat(game);
                    VerifyLivingForest(game);
                    VerifyNorton(game);
                    Array.Find(game.items,item=>item.persistentId=="service_fuse").Use(game);
                    Require(game.Inventory.HasFuse,"fuse pickup");
                    Array.Find(game.items,item=>item.persistentId=="cabin_circuit").Use(game);
                    Require(game.PowerOn,"circuit interaction");
                    Physics.SyncTransforms();
                    Require(!Physics.Raycast(ForestLayout.Gate+new Vector3(0,1,-3),Vector3.forward,6,game.SolidMask,QueryTriggerInteraction.Ignore),"open gate is blocked by a fence collider");
                    game.SaveCheckpoint("Verification circuit");game.Continue();
                    Require(game.PowerOn&&!game.Inventory.HasFuse,"checkpoint restore");
                    Require(game.HasMap&&game.Documents.Contains("note_research"),"map/documents lost on checkpoint restore");
                    Array.Find(game.items,item=>item.persistentId=="north_gate").Use(game);
                    Require(game.Completed,"gate completion");
                    game.Continue();Require(game.Completed&&!game.monster.gameObject.activeSelf,"completed checkpoint restored monster");
                    Require(!failed,"runtime errors were logged");
                    Debug.Log("RUNTIME SMOKE PASS: night wildlife playback, moving dense fog and zero-time freeze, map discovery, four body documents, Hebrew HUD, eight legs/jaw, nonprotective weak bulb, all three research room paths, spawn, pause, power, map save/load and gate.");
                    SessionState.SetBool("LastLight.Smoke",false);EditorApplication.update-=SmokeStep;
                    Application.logMessageReceived-=ObserveError;EditorApplication.Exit(0);
                }
                if(elapsed>40)throw new Exception("Smoke timed out");
            }
            catch(Exception e)
            {
                SessionState.SetBool("LastLight.Smoke",false);EditorApplication.update-=SmokeStep;
                Debug.LogException(e);EditorApplication.Exit(1);
            }
        }
    }
}
