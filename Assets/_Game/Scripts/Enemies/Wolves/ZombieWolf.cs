using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Game
{
    public sealed class ZombieWolf:MonoBehaviour
    {
        public string persistentId;
        public Animator animator;
        public AnimationClip idle,run,bite,death;
        public AudioSource voice,barkVoice;
        public AudioClip growlClip,howlClip,biteClip,deathClip,barkClip;
        public Collider hitCollider;
        public BloodySurface blood;
        public int Health{get;private set;}=2;
        public bool Alive=>Health>0;
        public int Bites{get;private set;}
        public int Howls{get;private set;}
        public int Growls{get;private set;}
        public int Barks{get;private set;}
        public static float BarkGain(float distance)=>Mathf.Lerp(.08f,.95f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,2,distance)));
        public static float BarkInterval(float distance)=>Mathf.Lerp(2.4f,.85f,Mathf.InverseLerp(25,2,distance));
        public float BiteProgress=>windup<0?-1:Mathf.Clamp01(windup/bite.length);
        public Vector3 MouthPosition=>mouthBone?mouthBone.position:transform.position+Vector3.up*.75f;
        Transform mouthBone;
        Vector3 biteStart,biteDirection;
        float lungeProgress;
        public float DeathProgress=>Mathf.Clamp01(deathTime/death.length);
        Vector3 spawn;
        Quaternion spawnRotation;
        NavMeshPath path;
        int corner;
        float planning,cooldown=3,windup=-1,stagger,clock,deathTime,growlTimer,actualSpeed,alertDelay,unseen,howlCooldown;
        bool engaged,biteCommitted;
        float nextBark;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] poses;
        void Awake(){spawn=transform.position;spawnRotation=transform.rotation;path=new NavMeshPath();foreach(var bone in animator.GetComponentsInChildren<Transform>())if(bone.name=="b_Jaw")mouthBone=bone;EnsureGraph();}
        void EnsureGraph()
        {
            if(graph.IsValid()||!animator||!idle||!run||!bite||!death)return;
            graph=PlayableGraph.Create("Undead wolf "+persistentId);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,4);poses=new AnimationClipPlayable[4];var clips=new[]{idle,run,bite,death};
            for(int i=0;i<4;i++){poses[i]=AnimationClipPlayable.Create(graph,clips[i]);graph.Connect(poses[i],0,mixer,i);}
            AnimationPlayableOutput.Create(graph,"Wolf skin",animator).SetSourcePlayable(mixer);graph.Play();
        }
        public void ResetWolf(bool dead)
        {
            Health=dead?0:2;Bites=Howls=Growls=Barks=0;engaged=biteCommitted=false;unseen=alertDelay=howlCooldown=growlTimer=0;nextBark=0;
            transform.SetPositionAndRotation(spawn,spawnRotation);windup=-1;lungeProgress=0;deathTime=dead?death.length:0;
            planning=clock=stagger=actualSpeed=0;cooldown=5;corner=0;if(path!=null)path.ClearCorners();
            hitCollider.enabled=!dead;voice.Stop();if(blood)blood.amount=dead?1:.55f;Pose(0);
            if(barkVoice){barkVoice.Stop();barkVoice.volume=0;}
        }
        public void Hit()
        {
            if(!Alive)return;Health--;stagger=.65f;windup=-1;cooldown=1.4f;
            if(barkVoice)barkVoice.Stop();
            if(blood)blood.amount=1;
            if(!Alive){deathTime=0;hitCollider.enabled=false;voice.Stop();voice.PlayOneShot(deathClip,.75f);}
            else voice.PlayOneShot(deathClip,.42f);
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;clock+=dt;
            if(!Alive){deathTime+=dt;Pose(dt);if(blood)blood.Refresh();return;}
            cooldown=Mathf.Max(0,cooldown-dt);stagger=Mathf.Max(0,stagger-dt);growlTimer-=dt;
            alertDelay=Mathf.Max(0,alertDelay-dt);howlCooldown=Mathf.Max(0,howlCooldown-dt);
            Vector3 feet=game.player.transform.position,chest=feet+Vector3.up*1.2f;
            float distance=Vector3.Distance(transform.position,feet);
            bool sees=distance<25&&game.Protection.Clear(transform.position+Vector3.up*.8f,chest);
            bool safe=game.IsSafe||game.Completed;
            actualSpeed=0;
            unseen=sees&&!safe?0:unseen+dt;if(unseen>8)engaged=false;
            if(stagger<=0&&!safe&&sees)
            {
                Vector3 direction=feet-transform.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),dt*160);
                if(!engaged)
                {
                    engaged=true;
                    if(distance>=8&&howlCooldown<=0){voice.PlayOneShot(howlClip,.72f);Howls++;howlCooldown=30;alertDelay=1.5f;growlTimer=howlClip.length+.2f;nextBark=howlClip.length;}
                }
                if(growlTimer<=0&&distance<15){voice.PlayOneShot(growlClip,.65f);Growls++;growlTimer=4.5f+(persistentId.GetHashCode()&3);}
                if(windup<0&&distance<1.5f&&cooldown<=0&&alertDelay<=0){windup=0;biteCommitted=false;biteStart=transform.position;biteDirection=direction.normalized;lungeProgress=0;}
                if(windup<0&&alertDelay<=0&&distance>1.35f)Move(dt,feet,game);
            }
            // Finish the entire bite, including closure/recovery; damage happens at contact.
            // A retreating player or a wall can make the committed bite miss.
            if(windup>=0)
            {
                windup+=dt;
                float reach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.55f,windup))*.32f;
                float increment=reach-lungeProgress;
                if(increment>0&&!safe)
                {
                    Vector3 from=transform.position,to=from+biteDirection*increment,delta=to-from;
                    if(!NavMesh.Raycast(from,to,out _,NavMesh.AllAreas)&&!game.Protection.InShelter(to+Vector3.up*.7f)
                        &&!Physics.SphereCast(from+Vector3.up*.5f,.32f,delta.normalized,out _,delta.magnitude,game.SolidMask,QueryTriggerInteraction.Ignore))transform.position=to;
                }
                lungeProgress=reach;distance=Vector3.Distance(transform.position,feet);
                if(windup>=.6f&&!biteCommitted)
                {
                    biteCommitted=true;voice.PlayOneShot(biteClip,.7f);
                    if(!safe&&distance<1.8f&&game.Protection.Clear(transform.position+Vector3.up*.7f,chest)&&game.ReceiveAttack(false,this))Bites++;
                    cooldown=2.8f;
                }
                if(windup>=bite.length)windup=-1;
            }
            if(barkVoice&&barkClip)
            {
                nextBark-=dt;
                if(actualSpeed>.08f&&!safe&&windup<0)
                {
                    barkVoice.volume=BarkGain(distance);
                    if(nextBark<=0){barkVoice.pitch=.86f+(Barks%3)*.04f;barkVoice.PlayOneShot(barkClip);Barks++;nextBark=Mathf.Max(BarkInterval(distance),barkClip.length/barkVoice.pitch+.15f);}
                }
                else {barkVoice.Stop();barkVoice.volume=0;}
            }
            Pose(dt);if(blood)blood.Refresh();
        }
        void Move(float dt,Vector3 target,GameSession game)
        {
            planning-=dt;
            if(planning<=0)
            {
                planning=.55f;corner=1;
                if(!NavMesh.CalculatePath(transform.position,target,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){path.ClearCorners();return;}
            }
            var points=path.corners;while(corner<points.Length&&Vector3.Distance(transform.position,points[corner])<.2f)corner++;
            if(corner>=points.Length)return;
            Vector3 from=transform.position,next=Vector3.MoveTowards(from,points[corner],dt*3.4f),delta=next-from;
            if(game.Protection.InShelter(next+Vector3.up*.7f)||NavMesh.Raycast(from,next,out _,NavMesh.AllAreas))return;
            if(Physics.SphereCast(from+Vector3.up*.5f,.32f,delta.normalized,out _,delta.magnitude,game.SolidMask,QueryTriggerInteraction.Ignore))return;
            transform.position=next;actualSpeed=delta.magnitude/dt;
        }
        void Pose(float dt)
        {
            EnsureGraph();if(!graph.IsValid())return;
            int state=!Alive?3:windup>=0?2:actualSpeed>.05f?1:0;
            float sum=0;for(int i=0;i<4;i++){float weight=dt<=0?(i==state?1:0):Mathf.MoveTowards(mixer.GetInputWeight(i),i==state?1:0,dt*9);mixer.SetInputWeight(i,weight);sum+=weight;}
            if(sum>0)for(int i=0;i<4;i++)mixer.SetInputWeight(i,mixer.GetInputWeight(i)/sum);
            poses[0].SetTime(Mathf.Repeat(clock,idle.length));poses[1].SetTime(Mathf.Repeat(clock*1.65f,run.length));
            poses[2].SetTime(Mathf.Max(0,windup));poses[3].SetTime(Mathf.Min(deathTime,death.length));graph.Evaluate(0);
        }
        public void PoseFatal(float seconds,Vector3 position,Vector3 camera)
        {
            EnsureGraph();transform.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.ProjectOnPlane(camera-position,Vector3.up)));
            for(int i=0;i<4;i++)mixer.SetInputWeight(i,i==2?1:0);
            poses[2].SetTime(Mathf.Repeat(seconds,bite.length));graph.Evaluate(0);
            if(blood)blood.Refresh();
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
