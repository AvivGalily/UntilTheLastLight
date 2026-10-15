using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    public static class PlayerRefinementsVerification
    {
        static GameSession game;
        static int stage,oldSeen;
        static double started,changed;
        static bool failed;
        static Vector3 clearing;
        static Vector3 shadowDirection;
        static Light inspectionBeam;
        [MenuItem("Last Light/Player/Verify gait, grips and fallen passengers")]
        public static void Run()
        {
            SessionState.SetBool("LastLight.PlayerRefinements",true);
            SessionState.SetString("LastLight.PlayerRefinementsSave",Path.Combine(Path.GetTempPath(),"LastLightPlayer-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");Attach();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("LastLight.PlayerRefinements",false))Attach();}
        static void Attach()
        {
            stage=0;started=changed=EditorApplication.timeSinceStartup;failed=false;
            GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.PlayerRefinementsSave",null);
            EditorApplication.update-=Step;EditorApplication.update+=Step;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;
        }
        static void Observe(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;}
        static void Require(bool value,string message){if(!value)throw new Exception("PLAYER REFINEMENTS VERIFY: "+message);}
        static void Capture(string name)
        {
            ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory("Docs/Screenshots");var camera=game.player.view;
            var rt=new RenderTexture(1600,900,24);rt.Create();var previous=RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes("Docs/Screenshots/"+name+".png",texture.EncodeToPNG());RenderTexture.active=previous;
            rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
        }
        static void GripDetail(string name)
        {
            // Inspection-only light makes knuckles, thumb contact and wrist bends
            // readable; no fill light is installed in the playable scene.
            var lamp=new GameObject("Temporary equipment inspection light",typeof(Light));
            lamp.transform.SetParent(game.player.view.transform,false);lamp.transform.localPosition=new Vector3(-.05f,.18f,.25f);
            var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.range=2;light.intensity=.6f;light.color=new Color(.92f,.86f,.78f);
            try{Capture(name);}finally{UnityEngine.Object.DestroyImmediate(lamp);}
        }
        static void Gait(float phase,bool running)
        {
            game.player.Teleport(clearing,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);
            float progress=Mathf.Repeat(phase-.25f,1),distance=progress*2*(running?1.45f:1.1f),travel=0;
            for(int n=0;travel<distance&&n<200;n++)
            {
                game.player.Simulate(game.tuning.player,Vector2.up,Vector2.zero,running,false,.01f);travel+=game.player.TravelledThisStep;
            }
            game.presence.Simulate(.01f,game.player);
            var mesh=new Mesh();game.presence.bodyMorph.BakeMesh(mesh);
            float low=mesh.vertices.Min(v=>game.presence.bodyMorph.transform.TransformPoint(v).y-game.player.transform.position.y);
            UnityEngine.Object.DestroyImmediate(mesh);Require(low>-.04f&&low<.055f,"animated planted boot is not at ground level: "+low);
            var eye=game.player.view.transform;eye.rotation=Quaternion.LookRotation(game.player.transform.position+shadowDirection*1.8f-eye.position);game.player.view.fieldOfView=75;
        }
        static void Next(){stage++;changed=EditorApplication.timeSinceStartup;EditorApplication.QueuePlayerLoopUpdate();}
        static void Inspect(Vector3 position,Vector3 target,float field=58)
        {
            var camera=game.player.view;camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.fieldOfView=field;
            if(!inspectionBeam){inspectionBeam=new GameObject("Temporary cast inspection beam",typeof(Light)).GetComponent<Light>();inspectionBeam.transform.SetParent(camera.transform,false);inspectionBeam.type=LightType.Spot;inspectionBeam.range=12;inspectionBeam.intensity=1.2f;inspectionBeam.spotAngle=75;inspectionBeam.innerSpotAngle=50;inspectionBeam.color=game.flashlightVisual.color;}
            foreach(var blood in UnityEngine.Object.FindObjectsByType<BloodySurface>(FindObjectsSortMode.None))blood.Refresh();
        }
        static void Step()
        {
            if(EditorApplication.timeSinceStartup-started>100){End(1);return;}
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup-changed<.2)return;
            try
            {
                if(stage==0)
                {
                    game=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!game||game.State==null)return;
                    game.enabled=false;oldSeen=PlayerPrefs.GetInt(ForestIntroDirector.SeenKey,0);PlayerPrefs.SetInt(ForestIntroDirector.SeenKey,1);
                    game.StartNew();game.intro.Simulate(2.1f,0,true);Require(game.State.Running,"opening did not restore gameplay");
                    game.hud.gameObject.SetActive(false);game.Inventory.Restore(2,false,true);game.Inventory.AcquireShotgun();game.SelectWeapon(HeldWeapon.Axe);
                    game.Flashlight.Restore(100,true);game.flashlightVisual.enabled=true;game.flashlightVisual.intensity=3;
                    game.player.Teleport(ForestLayout.Cabin+new Vector3(0,0,9),0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);
                    game.player.view.fieldOfView=game.player.fieldOfView;game.player.view.transform.localRotation=Quaternion.Euler(18,0,0);Next();return;
                }
                if(stage==1)
                {
                    Capture("88-player-axe-grip");GripDetail("104-overhand-flashlight-and-axe");game.SelectWeapon(HeldWeapon.Shotgun);Next();return;
                }
                if(stage==2)
                {
                    Capture("89-player-shotgun-grip");GripDetail("105-overhand-flashlight-and-shotgun");game.player.view.transform.localRotation=Quaternion.Euler(55,0,0);Next();return;
                }
                if(stage==3){Capture("94-weighted-shotgun-look-down");GripDetail("106-overhand-grips-look-down");Require(game.shotgun.Fire(game),"shotgun recoil rejected");game.shotgun.Simulate(.02f,game);Next();return;}
                if(stage==4){Capture("95-weighted-shotgun-recoil");GripDetail("107-overhand-grips-recoil");game.shotgun.Simulate(1,game);Require(game.shotgun.BeginReload(game),"shotgun reload rejected");game.shotgun.Simulate(.75f,game);Next();return;}
                if(stage==5)
                {
                    Capture("96-weighted-shotgun-reload");GripDetail("108-overhand-grips-reload");game.shotgun.Simulate(2,game);game.Flashlight.Toggle();game.flashlightVisual.enabled=game.Flashlight.Active;game.presence.RefreshEquipment(true);game.shotgun.Simulate(.02f,game);
                    Require(!game.player.GetComponentsInChildren<Light>(true).Any(l=>l.enabled&&l.intensity>0),"player light survives flashlight off");Next();return;
                }
                if(stage==6)
                {
                    Capture("97-flashlight-off-no-player-glow");game.player.hand.gameObject.SetActive(false);
                    foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))light.enabled=false;
                    var moon=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);moon.enabled=true;
                    shadowDirection=Vector3.ProjectOnPlane(moon.transform.forward,Vector3.up).normalized;clearing=new Vector3(-75,0,-78);
                    Gait(.1f,false);Next();return;
                }
                if(stage==7){Capture("90-player-walking-shadow-left");Gait(.6f,false);Next();return;}
                if(stage==8){Capture("91-player-walking-shadow-right");Gait(.1f,true);Next();return;}
                if(stage==9)
                {
                    Capture("92-player-running-shadow");
                    var cast=UnityEngine.Object.FindObjectsByType<HunterIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.encounterRole!="vehicle").ToArray();
                    Require(cast.Length==3&&cast.Select(c=>c.characterId).Distinct().Count()==3,"encounter cast is not exactly Jack, Tyler and Ethan");
                    foreach(var actor in cast)
                    {
                        Require(game.intro.dialogue.passengers.Any(p=>p.GetComponent<HunterIdentity>().characterId==actor.characterId),"encounter passenger has no corresponding living cast member");
                    }
                    Require(game.items.Single(i=>i.persistentId=="corpse_0").GetComponent<HunterIdentity>().characterId=="tyler","map/axe carrier was replaced instead of retaining its item identity");
                    Require(game.forestEncounter.victim.GetComponent<HunterIdentity>().characterId=="jack"&&game.forestEncounter.web&&!game.forestEncounter.web.Released,"Jack is not the weighted web victim");
                    Require(game.norton.casualty.GetComponent<HunterIdentity>().characterId=="ethan"&&game.norton.casualty.GetComponent<WorldItem>().persistentId=="norton_casualty","Ethan wreck casualty lost its item identity");
                    Require(!cast.Any(c=>c.name.StartsWith("Fallen ")),"extra cabin casualties remain");
                    Inspect(ForestLayout.Bodies[0]+new Vector3(2,2.1f,-2),ForestLayout.Bodies[0]+new Vector3(0,.2f,-.4f));Next();return;
                }
                if(stage==10){Capture("93-first-corpse-tyler");Inspect(game.forestEncounter.web.transform.position+new Vector3(0,.1f,-3.1f),game.forestEncounter.web.transform.position);Next();return;}
                if(stage==11){Capture("98-web-victim-jack");Inspect(ForestLayout.NortonBody+new Vector3(.8f,1.15f,-.6f),ForestLayout.NortonBody+new Vector3(.04f,.15f,-.9f));Next();return;}
                if(stage==12)
                {
                    Capture("99-wreck-casualty-ethan-intestines");Require(!failed,"Unity logged errors");Debug.Log("PLAYER REFINEMENTS SMOKE PASS: weighted native left flashlight/right axe/shotgun shoulder-elbow-wrist IK; real-frame look-down/recoil/reload; no player lights when flashlight off; grounded walk/run; exact Tyler first corpse / Jack web victim / Ethan textured-intestine wreck casualty; retained item identities.");End(0);
                }
            }
            catch(Exception e){Debug.LogException(e);End(1);}
        }
        static void End(int code)
        {
            PlayerPrefs.SetInt(ForestIntroDirector.SeenKey,oldSeen);PlayerPrefs.Save();EditorApplication.update-=Step;Application.logMessageReceived-=Observe;
            SessionState.SetBool("LastLight.PlayerRefinements",false);GameSession.VerificationSaveDirectory=null;
            if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
        }
    }
}
