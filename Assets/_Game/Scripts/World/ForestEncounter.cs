using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game
{
    public enum ForestEncounterPhase { Waiting, Call, Leap, Feeding, Escape, Refuge, Resolved }
    // A single owner coordinates the existing creature, sound, victim and circuit fault.
    // No coroutine or wall clock: menus freeze the entire sequence.
    public sealed class ForestEncounter:MonoBehaviour
    {
        public Transform victim,pool;
        public VictimWeb web;
        public Animator victimAnimator;
        public AnimationClip hanging,overpowered,dying;
        public Transform chest;
        public BloodySurface wounds;
        public ParticleSystem spray;
        public AudioSource voice,impact,score,heartbeat;
        public AudioClip helpClip,screamClip,stingClip,biteClip,powerClip;
        public ForestEncounterPhase Phase {get;private set;}
        public bool Seen {get;private set;}
        public bool Choreographing=>Phase==ForestEncounterPhase.Call||Phase==ForestEncounterPhase.Leap||Phase==ForestEncounterPhase.Feeding;
        public bool OwnsMonster=>Choreographing||Phase==ForestEncounterPhase.Escape;
        public bool Escaping=>Phase==ForestEncounterPhase.Escape;
        public float Elapsed {get;private set;}
        public float ScoreIntensity {get;private set;}
        public int TriggerCount {get;private set;}
        public int BloodBursts {get;private set;}
        public Vector3 BiteTarget=>chest.position+Vector3.up*.08f;
        Vector3 hangPosition,leapStart,feedingPoint;
        Quaternion hangRotation;
        bool initialized;
        float heartbeatTime,escapeTime;
        int bite;
        PlayableGraph graph;
        AnimationClipPlayable pose,fallPose,dyingPose;
        AnimationMixerPlayable mixer;
        void Initialize()
        {
            if(initialized)return;initialized=true;hangPosition=victim.position;hangRotation=victim.rotation;
            feedingPoint=transform.position+new Vector3(2.3f,0,-.4f);
            leapStart=transform.position+new Vector3(-4.5f,0,7);
            graph=PlayableGraph.Create("Suspended survivor");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            pose=AnimationClipPlayable.Create(graph,hanging);pose.SetApplyFootIK(false);
            fallPose=AnimationClipPlayable.Create(graph,overpowered);dyingPose=AnimationClipPlayable.Create(graph,dying);
            mixer=AnimationMixerPlayable.Create(graph,3);graph.Connect(pose,0,mixer,0);graph.Connect(fallPose,0,mixer,1);graph.Connect(dyingPose,0,mixer,2);mixer.SetInputWeight(0,1);
            AnimationPlayableOutput.Create(graph,"Victim anatomy",victimAnimator).SetSourcePlayable(mixer);graph.Play();
        }
        public void ResetEncounter(bool seen,bool faulted)
        {
            Initialize();Seen=seen;TriggerCount=seen?1:0;Elapsed=escapeTime=heartbeatTime=ScoreIntensity=0;bite=BloodBursts=0;
            Phase=seen?(faulted?ForestEncounterPhase.Refuge:ForestEncounterPhase.Resolved):ForestEncounterPhase.Waiting;
            StopSound();voice.Stop();impact.Stop();spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            victim.position=hangPosition;victim.rotation=hangRotation;wounds.amount=seen?1:.75f;
            mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);mixer.SetInputWeight(2,0);pose.SetTime(0);graph.Evaluate(0);web.ResetWeb(seen);pool.gameObject.SetActive(seen);
            if(seen)PoseFallen(1);wounds.Refresh();
        }
        void StopSound(){score.Stop();heartbeat.Stop();ScoreIntensity=0;}
        public void StopForPlayerDeath(){StopSound();voice.Stop();impact.Stop();}
        public bool TryTrigger(GameSession game)
        {
            if(Seen||game.Completed||!game.State.Running||game.IsSafe)return false;
            Vector3 chest=hangPosition+Vector3.up*1.1f;
            if(Vector3.Distance(game.player.transform.position,transform.position)>10.5f)return false;
            Vector3 viewport=game.player.view.WorldToViewportPoint(chest);
            if(viewport.z<0||viewport.x<.05f||viewport.x>.95f||viewport.y<0||viewport.y>1||!game.Protection.Clear(game.player.view.transform.position,chest))return false;
            // Do not replace a creature that is already being watched.
            if(game.monster.HasSpawned&&MonsterEncounters.Visible(game.player.view,game.monster.transform.position,game.Protection))return false;
            Seen=true;TriggerCount++;Phase=ForestEncounterPhase.Call;Elapsed=0;
            game.monster.gameObject.SetActive(false);game.monster.Relentless=false;game.horrorAudio.ResetSequence(game.tuning.seed);
            voice.clip=helpClip;voice.pitch=1;voice.Play();game.Notify(game.Text("survivor_help"),3.4f);
            return true;
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!game.State.Running)return;Initialize();
            if(Phase==ForestEncounterPhase.Waiting)TryTrigger(game);
            Elapsed+=dt;
            if(Phase==ForestEncounterPhase.Waiting||Phase==ForestEncounterPhase.Call)
            {
                mixer.SetInputWeight(0,1);pose.SetTime(Mathf.Repeat(Elapsed,hanging.length));graph.Evaluate(0);
                victim.rotation=hangRotation*Quaternion.Euler(1.5f*Mathf.Sin(Elapsed*1.7f),0,2*Mathf.Sin(Elapsed*1.1f));
            }
            if(Phase==ForestEncounterPhase.Call&&Elapsed>=2.2f)
            {
                Phase=ForestEncounterPhase.Leap;Elapsed=0;
                game.monster.BeginEncounter(leapStart,game.player.transform.position);
                game.monster.transform.rotation=Quaternion.LookRotation(feedingPoint-leapStart);
                impact.PlayOneShot(stingClip,.9f);voice.Stop();voice.pitch=.94f;voice.PlayOneShot(screamClip,.9f);
            }
            if(Phase==ForestEncounterPhase.Leap)
            {
                float t=Mathf.Clamp01(Elapsed/.85f);
                Vector3 end=feedingPoint+Vector3.back*.9f;
                game.monster.transform.position=Vector3.Lerp(leapStart,end,t)+Vector3.up*(4*t*(1-t))*2.7f;
                var airborne=Quaternion.LookRotation((feedingPoint-leapStart).normalized)*Quaternion.Euler(Mathf.Lerp(-24,12,t),0,0);
                game.monster.transform.rotation=Quaternion.Slerp(airborne,Quaternion.identity,Mathf.SmoothStep(0,1,(t-.55f)/.45f));
                game.creature.PoseEncounter(dt,game.monster,t,false);
                if(t>=1){Phase=ForestEncounterPhase.Feeding;Elapsed=0;web.Release();PoseFallen(0);EmitBlood(45);impact.PlayOneShot(biteClip,.9f);}
            }
            if(Phase==ForestEncounterPhase.Feeding)
            {
                float fall=Mathf.Clamp01(Elapsed/.85f);PoseFallen(fall);
                game.monster.transform.position=feedingPoint+new Vector3(0,0,-.9f);
                game.monster.transform.rotation=Quaternion.LookRotation(Vector3.forward);
                game.creature.PoseEncounter(dt,game.monster,Elapsed,true,BiteTarget);
                wounds.amount=Mathf.Clamp01(.75f+Elapsed*.10f);wounds.Refresh();pool.gameObject.SetActive(true);
                pool.localScale=Vector3.one*Mathf.Lerp(.3f,1,Mathf.Clamp01(Elapsed/6));
                int next=Mathf.FloorToInt((Elapsed+.7f)/1.3f);
                if(next>bite&&Elapsed<6){bite=next;EmitBlood(32);impact.PlayOneShot(biteClip,.75f);}
                if(Elapsed>=6.5f)
                {
                    Phase=ForestEncounterPhase.Escape;Elapsed=0;escapeTime=0;
                    game.Flashlight.FailCircuit();game.flashlightVisual.enabled=false;impact.PlayOneShot(powerClip,.8f);
                    game.monster.ResumePursuit(game);
                    game.monster.Relentless=true;game.Notify(game.Text("blackout_run"),10);
                    score.volume=.15f;score.Play();game.SaveCheckpoint("checkpoint_blackout");
                }
            }
            if(Phase==ForestEncounterPhase.Escape)
            {
                if(!game.monster.NavigationReady)game.monster.ResumePursuit(game);
                escapeTime+=dt;float proximity=1-Mathf.InverseLerp(3,28,Vector3.Distance(game.monster.transform.position,game.player.transform.position));
                ScoreIntensity=Mathf.Clamp01(.2f+escapeTime/45+proximity*.35f);score.volume=ScoreIntensity*.6f;score.pitch=1+ScoreIntensity*.12f;
                heartbeatTime-=dt;
                if(heartbeatTime<=0){heartbeatTime=Mathf.Lerp(.72f,.43f,ScoreIntensity);heartbeat.PlayOneShot(heartbeat.clip,Mathf.Lerp(.35f,.8f,ScoreIntensity));}
                if(ForestLayout.InRefugeHouse(game.player.transform.position))
                {
                    Phase=ForestEncounterPhase.Refuge;Elapsed=0;StopSound();game.monster.Relentless=false;game.horrorAudio.ResetSequence(game.tuning.seed);
                    game.encounters.ResetSequence(game.tuning.seed);game.Notify(game.Text("blackout_refuge"),10);game.SaveCheckpoint("checkpoint_refuge");
                }
            }
            if(Phase==ForestEncounterPhase.Refuge&&!game.Flashlight.Faulted){Phase=ForestEncounterPhase.Resolved;game.SaveCheckpoint("checkpoint_repaired");}
            web.Simulate(dt);wounds.Refresh();
        }
        void PoseFallen(float t)
        {
            float blend=Mathf.SmoothStep(0,1,(t-.72f)/.28f);
            mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,1-blend);mixer.SetInputWeight(2,blend);
            fallPose.SetTime(t*overpowered.length);dyingPose.SetTime(Phase==ForestEncounterPhase.Feeding?Mathf.Clamp(Elapsed-.85f,0,dying.length):dying.length);graph.Evaluate(0);
            float gravity=t*t,settle=Phase==ForestEncounterPhase.Feeding&&Elapsed>.85f?Mathf.Sin((Elapsed-.85f)*18)*.025f*Mathf.Exp(-(Elapsed-.85f)*6):0;
            victim.position=Vector3.Lerp(hangPosition,feedingPoint+new Vector3(0,.13f,.9f),gravity)+Vector3.up*settle;
            victim.rotation=Quaternion.Slerp(hangRotation,Quaternion.Euler(82,180,-12),Mathf.SmoothStep(0,1,t));
        }
        void EmitBlood(int count)
        {
            BloodBursts++;spray.transform.position=BiteTarget;spray.transform.rotation=Quaternion.LookRotation((Vector3.up+Vector3.back*.6f).normalized);if(!spray.isPlaying)spray.Play();spray.Emit(count);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
