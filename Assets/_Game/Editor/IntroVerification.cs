using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    public static class IntroVerification
    {
        static double started,lastUpdate;
        static int stage,readyEvents;
        static bool failed,oldLanguage;
        static int oldSeen;
        static GameSession game;
        static Keyboard keyboard;static Gamepad pad;static Mouse mouse;
        static InputSettings.EditorInputBehaviorInPlayMode routing;
        static InputSettings.BackgroundBehavior background;
        static float paused;
        [MenuItem("Last Light/Opening/Verify opening flow")]
        public static void Run()
        {
            SessionState.SetBool("LastLight.IntroVerification",true);
            SessionState.SetString("LastLight.IntroSave",Path.Combine(Path.GetTempPath(),"LastLightIntro-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");Attach();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("LastLight.IntroVerification",false))Attach();}
        static void Attach()
        {
            GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.IntroSave",null);started=lastUpdate=EditorApplication.timeSinceStartup;stage=readyEvents=0;failed=false;
            EditorApplication.update-=Step;EditorApplication.update+=Step;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;
        }
        static void Observe(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;}
        static void Require(bool value,string label){if(!value)throw new Exception("INTRO VERIFY: "+label);}
        static void InputTick()
        {
            var update=typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null);
            update.Invoke(null,new object[]{InputUpdateType.Dynamic});typeof(GameSession).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        }
        static void Advance(float destination)
        {
            while(game.intro.Active&&game.intro.Elapsed<destination)game.intro.Simulate(Mathf.Min(.25f,destination-game.intro.Elapsed),0,false);
            game.hud.Refresh();
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory("Docs/Screenshots");ShaderUtil.allowAsyncCompilation=false;
            var camera=game.player.view;var canvas=game.intro.overlay.GetComponent<Canvas>();
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.065f;Canvas.ForceUpdateCanvases();
            foreach(var arm in game.intro.vehicle.hands.arms)arm.Pose();
            var mirror=game.intro.bridge.mirrorCamera;
            if(mirror&&mirror.enabled)RenderPipeline.SubmitRenderRequest(mirror,new UniversalRenderPipeline.SingleCameraRequest{destination=mirror.targetTexture});
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes("Docs/Screenshots/"+name+".png",texture.EncodeToPNG());RenderTexture.active=previous;
            camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
        }
        static void Step()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                game=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!game||game.State==null)return;
                if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Opening verification timed out");
                if(stage==0)
                {
                    oldSeen=PlayerPrefs.GetInt(ForestIntroDirector.SeenKey,0);PlayerPrefs.SetInt(ForestIntroDirector.SeenKey,0);oldLanguage=game.Locale.Hebrew;
                    routing=InputSystem.settings.editorInputBehaviorInPlayMode;background=InputSystem.settings.backgroundBehavior;
                    InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                    keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();mouse=InputSystem.AddDevice<Mouse>();game.intro.GameplayReady+=()=>readyEvents++;
                    game.Locale.SetHebrew(true);game.StartNew();Require(game.State.Mode==ScreenMode.Intro&&game.intro.Active,"New Game did not enter opening");
                    Require(!game.Save.Exists&&game.ActiveSeconds==0&&!game.ThreatEnabled,"opening wrote checkpoint or ran ordinary simulation");
                    Require(!game.player.controller.enabled&&!game.player.hand.gameObject.activeSelf,"FPS/equipment active in drive");
                    Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"duplicate AudioListener");
                    Require(game.intro.dialogue.passengers.Length==3,"three physical hunters missing");Advance(10);
                    Require(game.intro.dialogue.voices[1].isPlaying,"English voice fallback did not play with Hebrew subtitles");
                    game.intro.RefreshPresentation();Capture("72-intro-drive-hebrew");
                    game.Locale.SetHebrew(false);game.intro.RefreshPresentation();Capture("73-intro-drive-english");
                    Require(!game.ambience.insects.isPlaying&&game.ambience.insects.volume==0&&game.intro.audioDirector.vehicleAmbience.clip.name=="SFX_ForestVehicleAmbience","building crickets or previous opening bed audible during intro");
                    Require(game.intro.audioDirector.vehicleAmbience.loop&&game.intro.audioDirector.vehicleAmbience.isPlaying&&!game.intro.audioDirector.gate.loop&&!game.intro.audioDirector.threat.loop&&!game.intro.audioDirector.effects.loop,"supplied loop modes ignored");
                    InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(720,0)});InputTick();
                    Require(game.intro.Gaze.x>50,"mouse does not look around the car");
                    game.intro.Simulate(.01f,0,false,new Vector2((90-game.intro.Gaze.x)/game.player.sensitivity,0));Capture("78-intro-jack-alive");
                    game.intro.Simulate(.01f,0,false,new Vector2(10000,0));Require(game.intro.Gaze.x==160,"head turn exceeds seat limits");Capture("79-intro-back-seat-hunters");
                    game.intro.Simulate(.01f,0,false,-game.intro.Gaze/game.player.sensitivity);
                    game.introSubtitles=false;game.intro.RefreshPresentation();Require(!game.intro.dialogue.subtitlePanel.activeSelf,"subtitle off preference ignored");game.introSubtitles=true;
                    float time=game.intro.Elapsed;game.intro.Simulate(2.1f,0,true);Require(game.intro.Active&&game.intro.Elapsed>time,"unseen opening could be skipped");
                    game.Pause();paused=game.intro.Elapsed;lastUpdate=EditorApplication.timeSinceStartup;stage=1;return;
                }
                if(stage==1)
                {
                    if(EditorApplication.timeSinceStartup-lastUpdate<.6)return;
                    game.intro.Simulate(1,1,true);Require(game.intro.Elapsed==paused&&AudioListener.pause,"pause advanced intro/audio");game.Resume();
                    var initial=game.intro.vehicle.Offset;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));InputTick();
                    var input=(InputActionAsset)typeof(GameSession).GetField("actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
                    Require(input.FindAction("Intro/Steer").ReadValue<float>()>.9f,"real D event not read by Intro action map");
                    for(int n=0;n<30;n++)game.intro.Simulate(1/30f,1,false);
                    Require(game.intro.vehicle.Steering>0&&game.intro.vehicle.Offset>initial,"D does not steer");
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(-1,0)});InputTick();
                    Require(input.FindAction("Intro/Steer").ReadValue<float>()<-.9f,"real controller event not read by Intro action map");
                    for(int n=0;n<30;n++)game.intro.Simulate(1/30f,-1,false);
                    Require(game.intro.vehicle.Steering<0,"controller left stick does not steer");
                    InputSystem.QueueStateEvent(pad,new GamepadState());InputTick();Advance(60);game.Locale.SetHebrew(true);game.intro.RefreshPresentation();Capture("74-intro-gate");
                    game.introSubtitles=false;Advance(65);Require(game.intro.dialogue.voices[1].isPlaying&&!game.intro.dialogue.subtitlePanel.activeSelf,"subtitles-off muted dialogue");game.introSubtitles=true;
                    Require(game.intro.audioDirector.GateStarted&&game.intro.audioDirector.gate.isPlaying,"supplied gate cue did not play with closing leaves");
                    Advance(80.1f);Require(game.intro.audioDirector.ThreatStarted&&game.intro.audioDirector.threat.isPlaying,"supplied approaching creature did not play before discovery");
                    Advance(85);Capture("75-intro-silence");Advance(87.1f);Require(game.intro.dialogue.voices[0].isPlaying,"Jack's final warning did not play");Require(game.intro.audioDirector.threat.volume<.3f,"threat not ducked underneath warning");
                    Advance(90.1f);Require(game.intro.dialogue.voices[1].isPlaying,"Tyler's final warning did not play");
                    game.reducedIntroFlash=true;Advance(92.05f);Require(game.intro.fade.color.a==0,"reduced flash preference ignored");Require(game.intro.audioDirector.BlastStarted&&game.intro.audioDirector.effects.isPlaying,"impact sound did not play at the blast");game.reducedIntroFlash=false;Advance(95);Require(game.intro.fade.color.a==1&&!game.Save.Exists,"blackout/save timing");Capture("76-intro-blackout");
                    Advance(100);Capture("77-intro-cabin-wake");Advance(111);
                    Require(!game.intro.Active&&game.State.Running&&readyEvents==1,"full opening handoff not exactly once");
                    Require(game.Save.Exists&&game.IntroCompleted&&game.player.controller.enabled&&game.player.hand.gameObject.activeSelf,"wake did not restore FPS/save");
                    Require(game.Inventory.Batteries==2&&!game.Inventory.HasShotgun&&!game.Inventory.HasAxe&&game.Documents.Count==1,"initial inventory/objectives changed");
                    Require(Vector3.Distance(game.player.transform.position,game.cabinSpawn.position)<.1f,"wrong cabin spawn");
                    var saved=game.Save.Load(out _);Require(saved.introCompleted,"checkpoint missing completion flag");
                    game.ReturnToTitle();game.Continue();Require(game.State.Running&&!game.intro.Active&&readyEvents==1,"Continue replayed opening");
                    game.ReturnToTitle();game.StartNew();Require(game.intro.CanSkip,"repeat viewing cannot skip");game.intro.Simulate(1.9f,0,true);Require(game.intro.Active,"skip accepted too early");game.intro.Simulate(.11f,0,true);
                    Require(game.State.Running&&!game.intro.Active&&readyEvents==2,"skip did not use cabin handoff exactly once");
                    game.StartNew();Advance(91.9f);game.intro.Simulate(2.1f,0,true);Require(game.State.Running&&readyEvents==3,"skip near blast transition");
                    game.StartNew();Advance(101);game.ReturnToTitle();Require(!game.intro.Active&&game.player.view.transform.parent==game.player.transform,"cancel during wake left camera attached");
                    game.Continue();Require(game.State.Running&&!game.intro.Active,"load after cancelling wake");
                    Require(!failed,"Unity logged errors");
                    Debug.Log("FOREST INTRO SMOKE PASS: complete 110s sequence, keyboard/gamepad, unseen/repeat/transition skip, pause, supplied English VO with Hebrew subtitles, subtitles-off keeps VO, complete final warnings before blast, supplied ambience/gate/creature/explosion cues, bilingual 16:9 captures, blackout, cabin inventory, checkpoint, Continue and cancellation. Isolated save: "+GameSession.VerificationSaveDirectory);
                    End(0);stage=2;
                }
            }
            catch(Exception e){Debug.LogException(e);End(1);}
        }
        static void End(int code)
        {
            if(game&&game.Locale!=null)game.Locale.SetHebrew(oldLanguage);PlayerPrefs.SetInt(ForestIntroDirector.SeenKey,oldSeen);PlayerPrefs.Save();
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pad!=null)InputSystem.RemoveDevice(pad);if(mouse!=null)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode=routing;InputSystem.settings.backgroundBehavior=background;
            EditorApplication.update-=Step;Application.logMessageReceived-=Observe;SessionState.SetBool("LastLight.IntroVerification",false);GameSession.VerificationSaveDirectory=null;
            if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
        }
    }
}
