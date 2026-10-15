using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game
{
    // The AI owns movement and protection. This rig only poses the weighted skin.
    public sealed class MonsterPresentation:MonoBehaviour
    {
        public SkinnedMeshRenderer mouth;
        public Transform[] legs,shins,feet;
        public Transform body;
        public Transform head;
        public BloodySurface blood;
        public Animator animator;
        public AnimationClip idle,crawl;
        Vector3[] restFeet,restBends,anchors,swingStarts;
        Quaternion[] restFootRotations;
        Vector3 mouthOffset;
        float[] upperLengths,lowerLengths;
        bool[] swinging;
        Vector3 previous;
        float phase,opening,blend,idleTime;
        int jaw=-1,biteJaw=-1,solid;
        PlayableGraph graph;
        AnimationClipPlayable idlePose,crawlPose;
        AnimationMixerPlayable mixer;
        public float Opening=>opening;
        public float GaitPhase=>phase;
        public Vector3 MouthPosition=>head?head.TransformPoint(mouthOffset):transform.position;
        public bool HasRig=>mouth&&animator&&idle&&crawl&&legs!=null&&shins!=null&&feet!=null
            &&legs.Length==8&&shins.Length==8&&feet.Length==8
            &&Array.TrueForAll(legs,t=>t)&&Array.TrueForAll(shins,t=>t)&&Array.TrueForAll(feet,t=>t);
        public static float DesiredOpening(float distance,bool windup)=>windup?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,3,distance));
        public static float BiteOpening(float progress)
        {
            progress=Mathf.Clamp01(progress);
            return progress<.62f?Mathf.Lerp(.65f,1,Mathf.SmoothStep(0,1,progress/.62f)):
                Mathf.Lerp(1,.06f,Mathf.SmoothStep(0,1,(progress-.62f)/.38f));
        }
        public void CloseOnContact()
        {
            opening=.06f;ApplyMouth(true);
        }
        void ApplyMouth(bool biting)
        {
            if(jaw>=0&&mouth)mouth.SetBlendShapeWeight(jaw,opening*100);
            if(biteJaw>=0&&mouth)mouth.SetBlendShapeWeight(biteJaw,biting?(1-opening)*100:0);
        }

        void Awake(){if(HasRig)InitializeRig();}
        void OnEnable(){previous=transform.position;phase=opening=blend=idleTime=0;ResetContacts();}
        public void ReleaseRig(){if(graph.IsValid())graph.Destroy();}
        void OnDisable()=>ReleaseRig();
        void OnDestroy()=>ReleaseRig();
        void InitializeRig()
        {
            if(restFeet==null)
            {
                restFeet=new Vector3[8];restBends=new Vector3[8];restFootRotations=new Quaternion[8];
                upperLengths=new float[8];lowerLengths=new float[8];anchors=new Vector3[8];swingStarts=new Vector3[8];swinging=new bool[8];
                for(int i=0;i<8;i++)
                {
                    restFeet[i]=transform.InverseTransformPoint(feet[i].position);
                    restBends[i]=transform.InverseTransformVector(shins[i].position-legs[i].position);
                    restFootRotations[i]=Quaternion.Inverse(transform.rotation)*feet[i].rotation;
                    upperLengths[i]=Vector3.Distance(legs[i].position,shins[i].position);
                    lowerLengths[i]=Vector3.Distance(shins[i].position,feet[i].position);
                }
                for(int i=0;i<mouth.sharedMesh.blendShapeCount;i++)if(mouth.sharedMesh.GetBlendShapeName(i).EndsWith("Jaw_Open"))jaw=i;
                for(int i=0;i<mouth.sharedMesh.blendShapeCount;i++)if(mouth.sharedMesh.GetBlendShapeName(i).EndsWith("Jaw_Bite"))biteJaw=i;
                solid=LayerMask.GetMask("WorldSolid","LightBlocker");ResetContacts();
                mouthOffset=head.InverseTransformPoint(transform.TransformPoint(new Vector3(0,1.43f,.94f)));
            }
            if(graph.IsValid())return;
            graph=PlayableGraph.Create("Eight limb crawler · manual simulation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            idlePose=AnimationClipPlayable.Create(graph,idle);crawlPose=AnimationClipPlayable.Create(graph,crawl);
            idlePose.SetApplyFootIK(false);crawlPose.SetApplyFootIK(false);
            mixer=AnimationMixerPlayable.Create(graph,2);graph.Connect(idlePose,0,mixer,0);graph.Connect(crawlPose,0,mixer,1);
            var output=AnimationPlayableOutput.Create(graph,"Weighted anatomy",animator);output.SetSourcePlayable(mixer);
            mixer.SetInputWeight(0,1);graph.Play();previous=transform.position;
        }
        void ResetContacts()
        {
            if(restFeet==null)return;
            for(int i=0;i<8;i++){anchors[i]=Ground(transform.TransformPoint(restFeet[i]));swingStarts[i]=anchors[i];swinging[i]=false;}
        }
        Vector3 Ground(Vector3 p)
        {
            if(Physics.Raycast(p+Vector3.up*.65f,Vector3.down,out var hit,1.4f,solid,QueryTriggerInteraction.Ignore)
                &&hit.normal.y>.55f)p.y=hit.point.y+.075f;
            return p;
        }
        public void PoseEncounter(float dt,MonsterBrain brain,float t,bool feeding,Vector3? contact=null)
        {
            if(!HasRig)return;InitializeRig();idlePose.SetTime(0);mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);graph.Evaluate(0);
            float cycle=Mathf.Repeat(t/1.3f,1);
            float pulse=feeding?(cycle<.46f?Mathf.SmoothStep(0,1,(cycle-.28f)/.18f):1-Mathf.SmoothStep(0,1,(cycle-.62f)/.38f)):1;
            if(body)
            {
                body.localPosition=feeding?new Vector3(0,-.12f-.08f*pulse,0):Vector3.zero;
                body.localRotation=feeding?Quaternion.Euler(43+6*pulse,0,0):Quaternion.identity;
            }
            opening=feeding?(cycle<.28f?Mathf.Lerp(.28f,1,cycle/.28f):cycle<.46f?Mathf.Lerp(1,.08f,(cycle-.28f)/.18f):cycle<.72f?.08f:Mathf.Lerp(.08f,.28f,(cycle-.72f)/.28f)):1;
            ApplyMouth(feeding);
            if(head)head.localRotation*=Quaternion.Euler(feeding?-4-6*pulse:-18,feeding?Mathf.Sin(t*5)*4:0,feeding?Mathf.Sin(t*8)*3:0);
            if(feeding&&contact.HasValue)
            {
                float settle=Mathf.SmoothStep(0,1,t/.85f);Vector3 delta=contact.Value-MouthPosition;
                transform.position+=Vector3.ProjectOnPlane(delta,Vector3.up)*settle;
                body.localPosition+=Vector3.up*Mathf.Clamp(delta.y*settle,-.42f,.25f);
            }
            for(int i=0;i<8;i++)
            {
                Vector3 target=transform.TransformPoint(restFeet[i]);
                if(!feeding)target+=transform.up*(i<2?.8f:.4f)+transform.forward*(i<2?.55f:-.35f);
                else{target=Ground(target);if(i<2)target=contact.HasValue?contact.Value+transform.right*(i==0?-.29f:.29f)+Vector3.forward*.18f-Vector3.up*.05f:transform.position+transform.right*(i==0?-.45f:.45f)+transform.forward*1.1f+Vector3.up*.28f;}
                var hip=legs[i].position;var knee=SolveJoint(hip,target,transform.TransformVector(restBends[i]),upperLengths[i],lowerLengths[i]);
                legs[i].rotation=Quaternion.FromToRotation(shins[i].position-hip,knee-hip)*legs[i].rotation;
                shins[i].rotation=Quaternion.FromToRotation(feet[i].position-shins[i].position,target-shins[i].position)*shins[i].rotation;
            }
            previous=transform.position;ResetContacts();if(blood)blood.Refresh();
        }
        public void PoseDevouring(float dt,MonsterBrain brain,float time,Vector3 contact)
        {
            PoseEncounter(dt,brain,Mathf.Max(0,time-.40f),true,contact);
            float near=Mathf.SmoothStep(0,1,time/.85f);
            body.localPosition+=Vector3.up*(contact.y-MouthPosition.y)*near;
            float cycle=Mathf.Repeat(Mathf.Max(0,time-.85f)/1.12f,1);
            opening=cycle<.30f?Mathf.Lerp(.22f,1,cycle/.30f):cycle<.52f?Mathf.Lerp(1,.05f,(cycle-.30f)/.22f):cycle<.78f?.05f:Mathf.Lerp(.05f,.22f,(cycle-.78f)/.22f);
            ApplyMouth(true);
            // The front limbs brace beside the player's shoulders during the close bite.
            for(int i=0;i<2;i++)
            {
                Vector3 target=contact+transform.right*(i==0?-.36f:.36f)-Vector3.up*.18f;
                var hip=legs[i].position;var knee=SolveJoint(hip,target,transform.TransformVector(restBends[i]),upperLengths[i],lowerLengths[i]);
                legs[i].rotation=Quaternion.FromToRotation(shins[i].position-hip,knee-hip)*legs[i].rotation;
                shins[i].rotation=Quaternion.FromToRotation(feet[i].position-shins[i].position,target-shins[i].position)*shins[i].rotation;
            }
            if(blood)blood.Refresh();
        }
        // Exact two-segment geometry: fixed bone lengths and a stable anatomical bend plane.
        public static Vector3 SolveJoint(Vector3 hip,Vector3 foot,Vector3 bend,float upper,float lower)
        {
            Vector3 delta=foot-hip;Vector3 axis=delta.sqrMagnitude>.000001f?delta.normalized:Vector3.down;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.0001f,upper+lower-.0001f);
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 plane=Vector3.ProjectOnPlane(bend,axis);
            if(plane.sqrMagnitude<.000001f)plane=Vector3.ProjectOnPlane(Vector3.forward,axis);
            if(plane.sqrMagnitude<.000001f)plane=Vector3.ProjectOnPlane(Vector3.right,axis);
            return hip+axis*along+plane.normalized*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        }
        public void Simulate(float dt,MonsterBrain brain)
        {
            if(dt<=0||!HasRig)return;
            InitializeRig();
            if(body)body.localPosition=Vector3.zero;
            Vector3 movement=transform.position-previous;previous=transform.position;
            // Spawn/restore placement must never become a giant animation velocity.
            if(movement.magnitude>3){movement=Vector3.zero;ResetContacts();}
            float speed=Mathf.Min(6,movement.magnitude/dt);
            blend=Mathf.MoveTowards(blend,Mathf.Clamp01(speed/.3f),dt*5);
            float stride=Mathf.Lerp(.52f,.95f,Mathf.Clamp01(speed/4.5f));
            float frequency=speed/stride;
            phase=Mathf.Repeat(phase+dt*frequency,1);idleTime+=dt;
            idlePose.SetTime(Mathf.Repeat(idleTime,idle.length));crawlPose.SetTime(phase*crawl.length);
            mixer.SetInputWeight(0,1-blend);mixer.SetInputWeight(1,blend);graph.Evaluate(0);
            if(head)head.localRotation*=Quaternion.Euler(2*Mathf.Sin(idleTime*1.3f),0,Mathf.Sin(idleTime*.61f)*(brain.Distance<12?9:3));
            opening=brain.Attack.WindingUp?BiteOpening(brain.Attack.WindupElapsed/brain.WindupDuration):
                Mathf.MoveTowards(opening,DesiredOpening(brain.Distance,false),dt*1.6f);
            ApplyMouth(brain.Attack.WindingUp);
            for(int i=0;i<8;i++)
            {
                float cycle=Mathf.Repeat(phase+(i%2==0?0:.5f)+(i>=4?.5f:0),1);
                bool swing=speed>.05f&&cycle>=.62f;
                Vector3 resting=Ground(transform.TransformPoint(restFeet[i]));
                if(speed<.05f)
                {
                    // Gentle settling stops abrupt snapping when the AI becomes watchful.
                    anchors[i]=Vector3.MoveTowards(anchors[i],resting,dt*.35f);swing=false;
                }
                if(swing&&!swinging[i])swingStarts[i]=anchors[i];
                Vector3 target;
                if(swing)
                {
                    float t=(cycle-.62f)/.38f;
                    Vector3 landing=resting+transform.forward*stride*.26f;
                    target=Vector3.Lerp(swingStarts[i],landing,Mathf.SmoothStep(0,1,t));
                    target.y+=Mathf.Sin(t*Mathf.PI)*(i%4==0?.13f:.18f);
                }
                else
                {
                    if(swinging[i])anchors[i]=Ground(resting+transform.forward*stride*.26f);
                    target=anchors[i];
                }
                swinging[i]=swing;
                // Pull wide feet in beside walls and trunks, without changing navigation.
                Vector3 hip=legs[i].position;Vector3 reach=target-hip;
                if(Physics.SphereCast(hip,.025f,reach.normalized,out var obstruction,reach.magnitude,solid,QueryTriggerInteraction.Ignore)
                    &&obstruction.normal.y<.55f)target=obstruction.point+obstruction.normal*.055f;
                float maxReach=upperLengths[i]+lowerLengths[i]-.001f;
                if(Vector3.Distance(hip,target)>maxReach)target=hip+(target-hip).normalized*maxReach;
                Vector3 knee=SolveJoint(hip,target,transform.TransformVector(restBends[i]),upperLengths[i],lowerLengths[i]);
                legs[i].rotation=Quaternion.FromToRotation(shins[i].position-hip,knee-hip)*legs[i].rotation;
                shins[i].rotation=Quaternion.FromToRotation(feet[i].position-shins[i].position,target-shins[i].position)*shins[i].rotation;
                feet[i].rotation=transform.rotation*restFootRotations[i];
            }
            if(blood)blood.Refresh();
        }
    }
}
