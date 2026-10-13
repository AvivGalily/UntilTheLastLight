using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game
{
    public sealed class ForestIntroDirector : MonoBehaviour
    {
        public ForestIntroConfig config;
        public IntroVehicleController vehicle;
        public IntroDialogueController dialogue;
        public IntroAudioDirector audioDirector;
        public IntroTransitionBridge bridge;
        public Transform[] gateLeaves;
        public GameObject overlay;
        public Image fade;
        public TMP_Text controls,skip;
        public bool Active {get;private set;}
        public float Elapsed {get;private set;}
        public float SkipProgress {get;private set;}
        public ForestIntroPhase Phase=>config.PhaseAt(Elapsed);
        public bool CanSkip {get;private set;}
        public event Action GameplayReady;
        GameSession game;
        bool cabinEntered;
        float gateClosingAt=-1;
        public float GateCloseProgress {get;private set;}
        Vector2 gaze;
        public Vector2 Gaze=>gaze;
        public const string SeenKey="LastLight.ForestIntroSeen";
        public void Begin(GameSession session)
        {
            if(Active)return;
            game=session;Elapsed=SkipProgress=0;cabinEntered=false;gaze=Vector2.zero;gateClosingAt=-1;GateCloseProgress=0;
            CanSkip=PlayerPrefs.GetInt(SeenKey,0)!=0;Active=true;
            vehicle.ResetDrive();bridge.Attach(game,vehicle.driverEye);overlay.SetActive(true);
            dialogue.ResetDialogue();audioDirector.ResetAudio();game.State.Set(ScreenMode.Intro);
            vehicle.Simulate(.016f,0,0,config);RefreshPresentation();
            PoseGate();
        }
        public void Simulate(float dt,float steering,bool holdSkip,Vector2 look=default,bool accelerate=false)
        {
            if(!Active||game.State.Mode!=ScreenMode.Intro||dt<=0)return;
            SkipProgress=CanSkip&&holdSkip?SkipProgress+dt:0;
            if(SkipProgress>=config.skipHold){Finish();return;}
            Elapsed=Mathf.Min(config.duration,Elapsed+dt);
            if(Elapsed<config.blackout)
            {
                vehicle.Simulate(dt,Elapsed,steering,config,accelerate);
                var eyes=game.player.view.transform;
                float lookGain=game.player.sensitivity*config.headLookMultiplier;
                gaze.x=Mathf.Clamp(gaze.x+look.x*lookGain,-config.lookYawLimit,config.lookYawLimit);
                gaze.y=Mathf.Clamp(gaze.y+look.y*lookGain*(game.player.invertY?1:-1),-config.lookPitchLimit,config.lookPitchLimit);
                eyes.localPosition=config.HeadOffset(gaze.x);
                float shake=game.player.headBob?Mathf.Sin(Elapsed*13)*.22f:0;
                if(Elapsed>=config.blast-.25f&&game.player.headBob)shake+=Mathf.Sin(Elapsed*64)*2;
                eyes.localRotation=Quaternion.Euler(config.seatedViewPitch+gaze.y+shake,gaze.x+vehicle.Steering*config.headTilt,-vehicle.Steering*.65f);
                RenderSettings.fogDensity=Mathf.Lerp(.022f,.033f,Mathf.InverseLerp(config.gateApproach,config.silence,Elapsed));
            }
            if(Elapsed>=config.cabinWake)
            {
                cabinEntered=true;bridge.Cabin(Mathf.InverseLerp(config.cabinWake+2,config.duration-1,Elapsed));
            }
            if(gateClosingAt<0&&Elapsed>=config.gateCrossed+1&&vehicle.Distance>=config.gateDistance+config.gateClearance)
            {gateClosingAt=Elapsed;audioDirector.PlayGate();}
            GateCloseProgress=gateClosingAt<0?0:Mathf.SmoothStep(0,1,(Elapsed-gateClosingAt)/config.gateCloseDuration);
            PoseGate();
            audioDirector.Simulate(Elapsed,config,vehicle.transform);RefreshPresentation();audioDirector.MixDialogue(dt,dialogue.Speaking);
            if(Elapsed>=config.duration)Finish();
        }
        void PoseGate()
        {
            for(int n=0;n<gateLeaves.Length;n++)gateLeaves[n].localRotation=Quaternion.Euler(0,(n==0?-1:1)*Mathf.Lerp(config.gateOpenAngle,0,GateCloseProgress),0);
        }
        public void RefreshPresentation()
        {
            if(!Active)return;
            bool visible=game.State.Mode==ScreenMode.Intro;overlay.SetActive(visible);
            float black=Elapsed<config.fadeIn?1-Mathf.Clamp01(Elapsed/config.fadeIn):Elapsed>=config.blackout&&!cabinEntered?1:cabinEntered?1-Mathf.Clamp01((Elapsed-config.cabinWake)/config.cabinFade):0;
            bool flash=Elapsed>=config.blast&&Elapsed<config.blackout&&!game.reducedIntroFlash;
            fade.color=flash?new Color(.72f,.78f,.77f,.18f):new Color(0,0,0,black);
            controls.gameObject.SetActive(Elapsed<config.controlsHintDuration);game.Locale.Apply(controls,game.Text("intro_steer"));
            skip.gameObject.SetActive(CanSkip&&Elapsed<config.blackout);
            game.Locale.Apply(skip,game.Text("intro_skip")+(SkipProgress>0?$"  {Mathf.Clamp01(SkipProgress/config.skipHold)*100:0}%":""));
            dialogue.Simulate(Elapsed,config,game.Locale,visible&&Elapsed<config.blackout&&game.introSubtitles);
        }
        void Finish()
        {
            if(!Active)return;
            // Both paths use the same cabin initialization and exactly one checkpoint.
            bridge.Cabin(1);bridge.Restore();audioDirector.StopAudio();dialogue.ResetDialogue();overlay.SetActive(false);Active=false;
            PlayerPrefs.SetInt(SeenKey,1);PlayerPrefs.Save();
            game.CompleteOpening();GameplayReady?.Invoke();
        }
        public void Cancel()
        {
            if(!Active)return;
            bridge.Restore();audioDirector.StopAudio();dialogue.ResetDialogue();overlay.SetActive(false);Active=false;
        }
        void OnDestroy(){Cancel();}
    }
}
