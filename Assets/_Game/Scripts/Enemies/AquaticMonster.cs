using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game
{
    public sealed class AquaticMonster:MonoBehaviour
    {
        public ResearchSpecimen containment;
        public Animator animator;
        public AnimationClip idle,escape,run,attack,death,hit;
        public AudioSource voice;
        public AudioClip growlClip,attackClip,deathClip;
        public Collider hitCollider;
        public int Health {get;private set;}=4;
        public bool Alive=>Health>0;
        public bool CanBeHit=>Alive&&containment.Phase==ResearchPhase.Hunting;
        public bool DeathSettled=>!Alive&&deathTime>=death.length;
        public int Strikes {get;private set;}
        public Vector3 AimPoint=>transform.position+Vector3.up*1.2f;
        public float ActualSpeed {get;private set;}
        float clock,deathTime,planning,cooldown,windup=-1,stagger,growl,speed,gaitTime;
        bool committed;
        int corner;
        NavMeshPath path;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] poses;
        AnimationClip[] clips;
        double[] poseTimes;
        void Awake(){path=new NavMeshPath();EnsureGraph();}
        void EnsureGraph()
        {
            if(graph.IsValid()||!animator||!idle||!escape||!run||!attack||!death||!hit)return;
            graph=PlayableGraph.Create("R-09 research creature");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);mixer=AnimationMixerPlayable.Create(graph,6);poses=new AnimationClipPlayable[6];
            clips=new[]{idle,escape,run,attack,death,hit};poseTimes=new double[6];
            for(int i=0;i<clips.Length;i++){poses[i]=AnimationClipPlayable.Create(graph,clips[i]);poses[i].SetSpeed(0);graph.Connect(poses[i],0,mixer,i);}
            AnimationPlayableOutput.Create(graph,"R-09 skeleton",animator).SetSourcePlayable(mixer);graph.Play();
        }
        public void Pose(int state,float time,float dt)
        {
            EnsureGraph();if(!graph.IsValid())return;
            float sum=0,blend=dt<=0?1:1-Mathf.Exp(-dt*12);
            for(int i=0;i<6;i++)
            {
                float weight=Mathf.Lerp(mixer.GetInputWeight(i),i==state?1:0,blend);mixer.SetInputWeight(i,weight);sum+=weight;
                poseTimes[i]=i==state?time:poseTimes[i]+dt;
                bool loop=i==0||i==2;double length=clips[i].length;
                poses[i].SetTime(loop?poseTimes[i]%length:System.Math.Min(poseTimes[i],length));
            }
            if(sum>0)for(int i=0;i<6;i++)mixer.SetInputWeight(i,mixer.GetInputWeight(i)/sum);
            graph.Evaluate(0);
        }
        public void ResetMonster(int health=4,bool dead=false)
        {
            Health=dead?0:health;clock=deathTime=planning=stagger=growl=speed=gaitTime=ActualSpeed=0;cooldown=1;windup=-1;committed=false;Strikes=0;corner=0;
            if(poseTimes!=null)System.Array.Clear(poseTimes,0,poseTimes.Length);
            if(path==null)path=new NavMeshPath();else path.ClearCorners();voice.Stop();hitCollider.enabled=!dead;Pose(dead?4:0,dead?death.length:0,0);
        }
        public void BeginHunt()
        {
            if(NavMesh.SamplePosition(transform.position,out var ground,1.5f,NavMesh.AllAreas))transform.position=ground.position;
            planning=0;cooldown=1;speed=0;voice.PlayOneShot(growlClip,.8f);growl=4;
        }
        public void SetPaused(bool value){if(value)voice.Pause();else voice.UnPause();}
        public bool Hit()
        {
            if(!CanBeHit)return false;
            Health--;stagger=.44f;windup=-1;cooldown=.85f;speed=0;
            if(!Alive){deathTime=0;hitCollider.enabled=false;voice.Stop();voice.PlayOneShot(deathClip,.8f);containment.Killed();}
            else voice.PlayOneShot(attackClip,.45f);
            return true;
        }
        public void AdvanceDeath(float dt){deathTime+=dt;Pose(4,deathTime,dt);}
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!Alive||!game.State.Running)return;
            clock+=dt;cooldown=Mathf.Max(0,cooldown-dt);stagger=Mathf.Max(0,stagger-dt);growl-=dt;ActualSpeed=0;
            Vector3 feet=game.player.transform.position,delta=feet-transform.position;delta.y=0;float distance=delta.magnitude;
            bool safe=game.IsSafe||game.Completed;
            if(!safe&&distance<70&&stagger<=0)
            {
                if(windup<0&&distance<1.6f&&cooldown<=0&&game.Protection.Clear(AimPoint,feet+Vector3.up*1.2f)){windup=0;committed=false;}
                if(windup<0&&distance>1.35f)Move(dt,feet,game,distance);
                if(growl<=0&&distance<18){voice.PlayOneShot(growlClip,.65f);growl=4.5f;}
            }
            if(windup>=0)
            {
                windup+=dt;
                if(windup>=.55f&&!committed)
                {
                    committed=true;voice.PlayOneShot(attackClip,.7f);cooldown=1.7f;
                    if(!safe&&distance<1.95f&&game.Protection.Clear(AimPoint,feet+Vector3.up*1.2f)&&game.ReceiveAquaticAttack())Strikes++;
                }
                if(windup>=attack.length)windup=-1;
            }
            if(ActualSpeed<.05f)
            {
                speed=Mathf.MoveTowards(speed,0,dt*12);
                if(!safe&&delta.sqrMagnitude>.01f)Face(delta,dt);
            }
            gaitTime=Mathf.Repeat(gaitTime+ActualSpeed*dt/2.3f*run.length,run.length);
            int state=stagger>0?5:windup>=0?3:ActualSpeed>.05f?2:0;
            float time=state==5?.44f-stagger:state==3?windup:state==2?gaitTime:clock;
            Pose(state,time,dt);
        }
        void Face(Vector3 direction,float dt)
        {
            direction.y=0;if(direction.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-dt*11));
        }
        void Move(float dt,Vector3 destination,GameSession game,float distance)
        {
            planning-=dt;
            if(planning<=0)
            {
                planning=.35f;corner=1;
                if(!NavMesh.SamplePosition(transform.position,out var start,1.5f,NavMesh.AllAreas)||!NavMesh.SamplePosition(destination,out var end,1.5f,NavMesh.AllAreas)
                    ||!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){path.ClearCorners();return;}
            }
            // Follow the complete path all the way to each corner before turning.
            // A separate navmesh raycast at an edge can reject the path's own next
            // segment; sample its surface and retain the physical collision sweep.
            var points=path.corners;
            speed=Mathf.MoveTowards(speed,Mathf.Min(3.9f,Mathf.Max(0,distance-1.35f)*7),dt*9);
            float remaining=speed*dt,travel=0;Vector3 origin=transform.position;
            // Consume the remaining distance after a corner in the same frame.
            // Stopping there for a whole frame produced a visible hitch at each turn.
            for(int n=0;n<12&&remaining>.00001f;n++)
            {
                while(corner<points.Length&&(transform.position-points[corner]).sqrMagnitude<.000001f)corner++;
                if(corner>=points.Length)break;
                Vector3 from=transform.position,next=Vector3.MoveTowards(from,points[corner],remaining);
                if(!NavMesh.SamplePosition(next,out var surface,.1f,NavMesh.AllAreas))break;
                next=surface.position;Vector3 delta=next-from;float step=delta.magnitude;
                if(step<.00001f||game.Protection.InShelter(next+Vector3.up)
                    ||Physics.SphereCast(from+Vector3.up*.75f,.34f,delta.normalized,out _,step,game.SolidMask,QueryTriggerInteraction.Ignore))break;
                transform.position=next;travel+=step;remaining=Mathf.Max(0,remaining-step);
            }
            ActualSpeed=travel/dt;if(travel>0)Face(transform.position-origin,dt);
        }
        public void DisposeAnimation(){if(graph.IsValid())graph.Destroy();}
        void OnDestroy()=>DisposeAnimation();
    }
}
