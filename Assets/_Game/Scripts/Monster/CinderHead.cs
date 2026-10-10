using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Game
{
    // A physical pursuer. This species deliberately never queries light or shelter protection.
    public sealed class CinderHead:MonoBehaviour
    {
        public Animator animator;
        public AnimationClip idle,run,bite,spit,death;
        public Collider hitCollider;
        public AudioSource voice;
        public AudioClip growlClip,biteClip,spitClip,deathClip;
        public BloodySurface blood;
        public int Health {get;private set;}=2;
        public bool Alive=>Health>0;
        public int Bites {get;private set;}
        public int Spits {get;private set;}
        public float DeathTime {get;private set;}
        public float Speed {get;private set;}
        public Vector3 AimPoint=>transform.position+Vector3.up*.76f;
        public Vector3 MouthPosition=>mouth?mouth.position:AimPoint+transform.forward*.2f;
        Transform mouth;
        NavMeshPath path;
        int corner,attack; // 0 pursuit, 1 bite, 2 spit
        float clock,planning,cooldown,attackTime,stagger;
        bool contact;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] poses;
        public void ResetMonster(Vector3 position,Quaternion rotation)
        {
            transform.SetPositionAndRotation(position,rotation);Health=2;Bites=Spits=0;DeathTime=Speed=clock=planning=attackTime=stagger=0;
            cooldown=3;attack=corner=0;contact=false;path=new NavMeshPath();hitCollider.enabled=true;
            if(voice){voice.Stop();voice.clip=growlClip;voice.loop=true;voice.volume=.4f;voice.Play();}
            if(blood){blood.amount=.55f;blood.Refresh();}
            Pose(0);
        }
        void Graph()
        {
            if(graph.IsValid()||!animator||!idle||!run||!bite||!spit||!death)return;
            foreach(var t in animator.GetComponentsInChildren<Transform>())if(t.name=="Jaw")mouth=t;
            graph=PlayableGraph.Create("Cinder head six-leg animation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,5);poses=new AnimationClipPlayable[5];var clips=new[]{idle,run,bite,spit,death};
            for(int i=0;i<5;i++){poses[i]=AnimationClipPlayable.Create(graph,clips[i]);graph.Connect(poses[i],0,mixer,i);}
            AnimationPlayableOutput.Create(graph,"Head skin",animator).SetSourcePlayable(mixer);graph.Play();
        }
        public void Hit(int amount)
        {
            if(!Alive||amount<=0)return;Health=Mathf.Max(0,Health-amount);stagger=.48f;attack=0;cooldown=1.2f;
            if(blood){blood.amount=1;blood.Refresh();}
            voice.PlayOneShot(deathClip,Alive?.35f:.8f);
            if(!Alive){DeathTime=0;hitCollider.enabled=false;voice.loop=false;voice.Stop();voice.PlayOneShot(deathClip,.8f);}
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!game.State.Running||!gameObject.activeSelf)return;
            clock+=dt;Speed=0;
            if(!Alive){DeathTime+=dt;Pose(dt);return;}
            cooldown-=dt;stagger-=dt;
            Vector3 target=game.player.transform.position,chest=game.player.controller.bounds.center;
            Vector3 toward=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);float distance=toward.magnitude;
            if(toward.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(toward),dt*190);
            bool clear=game.Protection.Clear(AimPoint,chest);
            voice.volume=Mathf.Lerp(.18f,.65f,Mathf.InverseLerp(22,2,distance));
            if(stagger>0){Pose(dt);return;}
            if(attack==0&&cooldown<=0&&clear)
            {
                if(distance<1.55f){attack=1;attackTime=0;contact=false;voice.PlayOneShot(biteClip,.65f);}
                else if(distance<18){attack=2;attackTime=0;contact=false;voice.PlayOneShot(spitClip,.7f);}
            }
            if(attack!=0)
            {
                attackTime+=dt;
                if(attack==1&&attackTime>=.52f&&!contact)
                {contact=true;if(distance<1.85f&&clear&&game.ReceiveHeadAttack(false,this))Bites++;}
                if(attack==2&&attackTime>=.8f&&!contact)
                {contact=true;if(clear&&game.norton.LaunchFireball(MouthPosition+transform.forward*.30f,chest,this))Spits++;}
                if(attackTime>=(attack==1?1.2f:1.6f)){cooldown=attack==1?2.1f:4.5f;attack=0;}
            }
            else if(distance>1.15f)Move(dt,target,game);
            Pose(dt);if(blood)blood.Refresh();
        }
        void Move(float dt,Vector3 target,GameSession game)
        {
            if(path==null)path=new NavMeshPath();planning-=dt;
            if(planning<=0)
            {
                planning=.4f;corner=1;
                if(!NavMesh.SamplePosition(target,out var destination,1.3f,NavMesh.AllAreas)||!NavMesh.CalculatePath(transform.position,destination.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){path.ClearCorners();return;}
            }
            var points=path.corners;while(corner<points.Length&&Vector3.Distance(transform.position,points[corner])<.22f)corner++;
            if(corner>=points.Length)return;
            Vector3 next=Vector3.MoveTowards(transform.position,points[corner],dt*2.65f),delta=next-transform.position;
            if(delta.sqrMagnitude<.000001f||NavMesh.Raycast(transform.position,next,out _,NavMesh.AllAreas))return;
            if(Physics.SphereCast(transform.position+Vector3.up*.55f,.32f,delta.normalized,out _,delta.magnitude,game.SolidMask,QueryTriggerInteraction.Ignore)){planning=0;return;}
            transform.position=next;Speed=delta.magnitude/dt;
        }
        void Pose(float dt)
        {
            Graph();if(!graph.IsValid())return;
            int state=!Alive?4:attack==1?2:attack==2?3:Speed>.03f?1:0;
            float sum=0;
            for(int i=0;i<5;i++){float w=dt<=0?(i==state?1:0):Mathf.MoveTowards(mixer.GetInputWeight(i),i==state?1:0,dt*12);mixer.SetInputWeight(i,w);sum+=w;}
            if(sum>0)for(int i=0;i<5;i++)mixer.SetInputWeight(i,mixer.GetInputWeight(i)/sum);
            poses[0].SetTime(Mathf.Repeat(clock,idle.length));poses[1].SetTime(Mathf.Repeat(clock*1.35f,run.length));
            poses[2].SetTime(attackTime);poses[3].SetTime(attackTime);poses[4].SetTime(Mathf.Min(DeathTime,death.length));graph.Evaluate(0);
        }
        public void PoseFatal(float seconds,Vector3 position,Vector3 camera)
        {
            Graph();transform.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(camera-position,Vector3.up)));
            for(int i=0;i<5;i++)mixer.SetInputWeight(i,i==2?1:0);poses[2].SetTime(Mathf.Repeat(seconds,bite.length));graph.Evaluate(0);blood?.Refresh();
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
