using UnityEngine;
namespace Game
{
    public sealed class CrowFlight:MonoBehaviour
    {
        public Transform leftWing,rightWing;
        public AudioSource voice;
        public AudioClip caw,flutter;
        public int seed;
        public bool Flying {get;private set;}
        public int FleeCount {get;private set;}
        Vector3 perch,start,away;
        Quaternion resting,leftRest,rightRest;
        float flight,delay;
        bool initialized;
        public void ResetBird()
        {
            if(!initialized){perch=transform.position;resting=transform.rotation;leftRest=leftWing.localRotation;rightRest=rightWing.localRotation;initialized=true;}
            transform.position=perch;transform.rotation=resting;flight=0;delay=-1;Flying=false;FleeCount=0;gameObject.SetActive(true);voice.Stop();Fold();
        }
        void Fold(){leftWing.localRotation=leftRest*Quaternion.Euler(0,-48,55);rightWing.localRotation=rightRest*Quaternion.Euler(0,48,-55);}
        public void Simulate(float dt,Vector3 player,float time)
        {
            if(dt<=0||!gameObject.activeSelf)return;if(!initialized)ResetBird();
            if(!Flying)
            {
                transform.position=perch+Vector3.right*Mathf.Sin(time*.73f+seed)*.035f;
                Vector3 horizontal=player-perch;horizontal.y=0;
                if(horizontal.magnitude<8.5f&&delay<0)delay=.05f+(seed%7)*.045f;
                if(delay>=0)
                {
                    delay-=dt;if(delay<=0)
                    {
                        Flying=true;FleeCount++;start=transform.position;away=Vector3.ProjectOnPlane(start-player,Vector3.up).normalized;
                        away=Quaternion.Euler(0,(seed%5-2)*14,0)*away;
                        if(away.sqrMagnitude<.1f)away=Vector3.forward;
                        voice.pitch=.90f+(seed%5)*.065f;voice.PlayOneShot(caw,.65f);voice.PlayOneShot(flutter,.5f);
                    }
                }
                return;
            }
            flight+=dt;float speed=Mathf.Lerp(2,9,Mathf.Clamp01(flight/1.3f));
            Vector3 direction=(away+Vector3.up*Mathf.Lerp(.9f,.16f,Mathf.Clamp01(flight/4))+Vector3.Cross(away,Vector3.up)*Mathf.Sin(flight*.9f+seed)*.16f).normalized;
            transform.position+=direction*speed*dt;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction),dt*7);
            float flap=Mathf.Sin(flight*29+seed)*62;
            // Imported mesh nodes retain Blender's local Y wing hinge.
            leftWing.localRotation=leftRest*Quaternion.Euler(0,flap,0);rightWing.localRotation=rightRest*Quaternion.Euler(0,-flap,0);
            if(flight>11)gameObject.SetActive(false);
        }
    }
}
