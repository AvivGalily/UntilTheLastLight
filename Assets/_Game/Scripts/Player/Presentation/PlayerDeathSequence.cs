using UnityEngine;
namespace Game
{
    // A single fatal attack owns the camera and attacker until the death menu.
    public sealed class PlayerDeathSequence:MonoBehaviour
    {
        public ParticleSystem spray;
        public AudioSource voice,impacts;
        public AudioClip devourClip,tearClip,stingClip;
        public Transform[] defensiveHands;
        public Light contactLight;
        public float Duration=5.6f;
        public bool Active {get;private set;}
        public float Elapsed {get;private set;}
        public int BloodBursts {get;private set;}
        public float ClosestMouth {get;private set;}=100;
        GameSession game;
        ZombieWolf wolf;
        CinderHead head;
        Vector3 eyeStart,fallenEye,attackerStart,stagePoint,direction,localEyes;
        Quaternion viewStart,localView;
        float originalFov;
        int bite;
        public void ResetSequence()
        {
            if(game)
            {
                game.player.view.transform.localPosition=localEyes;game.player.view.transform.localRotation=localView;
                game.player.view.fieldOfView=originalFov;game.player.hand.gameObject.SetActive(true);
            }
            Active=false;Elapsed=0;BloodBursts=bite=0;ClosestMouth=100;
            if(voice)voice.Stop();if(impacts)impacts.Stop();if(spray)spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(contactLight)contactLight.enabled=false;
            if(defensiveHands!=null)foreach(var hand in defensiveHands)if(hand)hand.gameObject.SetActive(false);
            game=null;wolf=null;head=null;
        }
        public void Begin(GameSession session,ZombieWolf attackingWolf=null,CinderHead attackingHead=null)
        {
            if(Active)return;
            game=session;wolf=attackingWolf;head=attackingHead;Active=true;Elapsed=0;BloodBursts=bite=0;ClosestMouth=100;
            var camera=game.player.view;eyeStart=camera.transform.position;viewStart=camera.transform.rotation;
            localEyes=camera.transform.localPosition;localView=camera.transform.localRotation;originalFov=camera.fieldOfView;
            var attacker=head?head.transform:wolf?wolf.transform:game.monster.transform;attackerStart=attacker.position;
            direction=Vector3.ProjectOnPlane(attacker.position-game.player.transform.position,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            fallenEye=game.player.transform.position-direction*.25f+Vector3.up*.34f;
            stagePoint=game.player.transform.position+direction*(head?.65f:wolf?.95f:1.3f);
            game.player.hand.gameObject.SetActive(false);game.flashlightVisual.enabled=false;
            if(game.presence.gripLight)game.presence.gripLight.enabled=false;if(contactLight)contactLight.enabled=true;
            game.foley.footsteps.Stop();game.foley.calmBreath.Stop();game.foley.hardBreath.Stop();
            foreach(var source in game.monster.GetComponentsInChildren<AudioSource>())source.Stop();
            foreach(var enemy in game.wolves){enemy.voice.Stop();enemy.barkVoice.Stop();}
            game.norton?.SilenceEnemies();
            game.forestEncounter?.StopForPlayerDeath();
            if(game.horrorAudio)game.horrorAudio.ResetSequence(game.tuning.seed);
            voice.PlayOneShot(devourClip,.72f);impacts.PlayOneShot(stingClip,.80f);
            foreach(var hand in defensiveHands)hand.gameObject.SetActive(true);
            game.State.Set(ScreenMode.Dying);game.hud.Invalidate();
        }
        public void Simulate(float dt)
        {
            if(!Active||dt<=0||!game||game.State.Mode!=ScreenMode.Dying)return;
            Elapsed+=dt;var camera=game.player.view;
            float fall=Mathf.SmoothStep(0,1,Elapsed/.8f);
            camera.transform.position=Vector3.Lerp(eyeStart,fallenEye,fall);
            Vector3 contact=camera.transform.position+direction*.30f+Vector3.up*.055f;
            if(head)head.PoseFatal(Elapsed,Vector3.Lerp(attackerStart,stagePoint,fall),camera.transform.position);
            else if(wolf)wolf.PoseFatal(Elapsed,Vector3.Lerp(attackerStart,stagePoint,fall),camera.transform.position);
            else
            {
                game.monster.transform.SetPositionAndRotation(Vector3.Lerp(attackerStart,stagePoint,fall),Quaternion.LookRotation(-direction));
                game.creature.PoseDevouring(dt,game.monster,Elapsed,contact);
            }
            Vector3 mouth=head?head.MouthPosition:wolf?wolf.MouthPosition:game.creature.MouthPosition;
            ClosestMouth=Mathf.Min(ClosestMouth,Vector3.Distance(mouth,camera.transform.position));
            Quaternion face=Quaternion.LookRotation(mouth+Vector3.up*.035f-camera.transform.position,Vector3.up);
            float chewing=Mathf.Repeat(Mathf.Max(0,Elapsed-.85f)/1.12f,1);
            float jolt=Elapsed>.85f?Mathf.Exp(-chewing*18)*2.8f:0;
            camera.transform.rotation=Quaternion.Slerp(viewStart,face,Mathf.SmoothStep(0,1,Elapsed/.48f))*Quaternion.Euler(Mathf.Sin(Elapsed*29)*jolt,Mathf.Sin(Elapsed*17)*jolt*.4f,Mathf.Lerp(0,-12,fall)+Mathf.Sin(Elapsed*21)*jolt);
            camera.fieldOfView=Mathf.Lerp(originalFov,72,fall);
            for(int i=0;i<defensiveHands.Length;i++)
            {
                float sign=i==0?-1:1,drop=Mathf.SmoothStep(0,1,(Elapsed-1.4f)/1.1f);
                defensiveHands[i].localPosition=new Vector3(sign*(.26f+drop*.2f),-.23f-drop*.65f,.48f);
                defensiveHands[i].localRotation=Quaternion.Euler(35+drop*60,sign*15,sign*22);
            }
            int next=Elapsed<.88f?0:1+Mathf.FloorToInt((Elapsed-.88f)/1.12f);
            if(next>bite&&Elapsed<Duration-.5f)
            {
                bite=next;BloodBursts++;game.damage.Trigger();impacts.pitch=.86f+(bite%3)*.055f;impacts.PlayOneShot(tearClip,.85f);
                spray.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*.45f,Quaternion.LookRotation(-camera.transform.forward+Vector3.up*.2f));
                spray.Emit(32);game.foley.injury.PlayOneShot(game.foley.hurt,.35f);
            }
            game.damage.Simulate(dt*.15f);
            if(Elapsed>=Duration)
            {
                Active=false;voice.Stop();impacts.Stop();foreach(var hand in defensiveHands)hand.gameObject.SetActive(false);
                game.State.Set(ScreenMode.Dead);game.Notify(game.Text("last_light"));game.hud.Invalidate();
            }
        }
    }
}
