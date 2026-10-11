using UnityEngine;
namespace Game
{
    public sealed class IntroPassenger : MonoBehaviour
    {
        public Transform head,spine,jaw,gesture;
        Quaternion headRest,spineRest,jawRest,gestureRest;
        bool ready;
        public void Pose(float time,bool talking,int identity,bool threat,bool blast)
        {
            if(!ready){headRest=head.localRotation;spineRest=spine.localRotation;if(jaw)jawRest=jaw.localRotation;if(gesture)gestureRest=gesture.localRotation;ready=true;}
            float breath=Mathf.Sin(time*1.6f+identity)*.5f;
            float turn=threat?(identity==2?-34:identity==0?12:-18):talking?(identity==0?-14:identity==1?-20:8):Mathf.Sin(time*.22f+identity)*4;
            head.localRotation=headRest*Quaternion.Euler(breath+ (blast?-16:0),turn, talking?Mathf.Sin(time*2.3f)*2:0);
            spine.localRotation=spineRest*Quaternion.Euler(breath+(talking&&identity==1?8:0)+(blast?-14:0),0,0);
            if(jaw)jaw.localRotation=jawRest*Quaternion.Euler(talking?Mathf.Abs(Mathf.Sin(time*13))*4:0,0,0);
            if(gesture)gesture.localRotation=gestureRest*Quaternion.Euler(talking&&identity==1?Mathf.Sin(time*2.1f)*9:0,0,0);
        }
    }
}
