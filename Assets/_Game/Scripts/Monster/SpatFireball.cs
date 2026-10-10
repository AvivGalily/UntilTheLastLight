using UnityEngine;
namespace Game
{
    public sealed class SpatFireball:MonoBehaviour
    {
        public ParticleSystem flame,impact;
        public Renderer core;
        public Light glow;
        public AudioSource voice;
        public AudioClip impactClip;
        public bool Flying {get;private set;}
        public int PlayerImpacts {get;private set;}
        Vector3 velocity;
        CinderHead owner;
        float age,fade;
        public void ResetProjectile(){Flying=false;age=fade=0;PlayerImpacts=0;flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);impact.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);voice.Stop();gameObject.SetActive(false);}
        public void Launch(Vector3 origin,Vector3 target,CinderHead shooter)
        {
            gameObject.SetActive(true);transform.position=origin;owner=shooter;age=fade=0;Flying=true;PlayerImpacts=0;
            velocity=(target-origin).normalized*8.5f;core.enabled=true;glow.enabled=true;
            flame.Clear();flame.Play();flame.Pause();impact.Clear();
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!game.State.Running||!gameObject.activeSelf)return;
            if(!Flying){fade-=dt;impact.Simulate(dt,true,false,false);impact.Pause();if(fade<=0)gameObject.SetActive(false);return;}
            age+=dt;Vector3 delta=velocity*dt;
            int mask=game.SolidMask|LayerMask.GetMask("Player");
            if(Physics.SphereCast(transform.position,.16f,delta.normalized,out var hit,delta.magnitude,mask,QueryTriggerInteraction.Ignore))
            {
                transform.position=hit.point-hit.normal*.02f;
                if(hit.collider.GetComponentInParent<PlayerMotor>()==game.player&&game.ReceiveHeadAttack(true,owner))PlayerImpacts++;
                Impact();return;
            }
            transform.position+=delta;flame.Simulate(dt,true,false,false);flame.Pause();
            glow.intensity=2.4f+Mathf.Sin(age*31)*.4f;
            if(age>7||transform.position.y<-.2f)Impact();
        }
        void Impact(){Flying=false;fade=.8f;core.enabled=false;glow.enabled=false;flame.Stop();impact.Emit(32);voice.PlayOneShot(impactClip,.65f);}
    }
}
