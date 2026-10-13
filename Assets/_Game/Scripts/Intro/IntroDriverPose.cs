using System;
using UnityEngine;

namespace Game
{
    // The wheel moves actual skeletal shoulders/elbows/wrists. Skin and cloth
    // follow their imported weights instead of stretching rigid forearm meshes.
    public sealed class IntroDriverPose : MonoBehaviour
    {
        [Serializable] public sealed class Arm
        {
            public Transform upper,lower,wrist,grip;
            public Vector3 wristOffset,pole;
            public Quaternion upperRest,lowerRest,wristRest,wristRotation;
            public float upperLength,lowerLength;
        }
        public Arm[] arms=new Arm[0];
        public void Bind()
        {
            foreach(var a in arms)
            {
                a.upperRest=a.upper.localRotation;a.lowerRest=a.lower.localRotation;a.wristRest=a.wrist.localRotation;
                a.upperLength=Vector3.Distance(a.upper.position,a.lower.position);a.lowerLength=Vector3.Distance(a.lower.position,a.wrist.position);
                a.wristOffset=a.grip.InverseTransformPoint(a.wrist.position);
                a.wristRotation=Quaternion.Inverse(a.grip.rotation)*a.wrist.rotation;
            }
        }
        public void Pose()
        {
            foreach(var a in arms)
            {
                if(!a.upper||!a.lower||!a.wrist||!a.grip)continue;
                a.upper.localRotation=a.upperRest;a.lower.localRotation=a.lowerRest;a.wrist.localRotation=a.wristRest;
                Vector3 shoulder=a.upper.position,target=a.grip.TransformPoint(a.wristOffset),line=target-shoulder;
                float distance=Mathf.Clamp(line.magnitude,.02f,a.upperLength+a.lowerLength-.001f);
                Vector3 direction=line.normalized;
                float along=(a.upperLength*a.upperLength-a.lowerLength*a.lowerLength+distance*distance)/(2*distance);
                Vector3 bend=transform.TransformPoint(a.pole)-shoulder;bend-=direction*Vector3.Dot(bend,direction);bend.Normalize();
                Vector3 elbow=shoulder+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a.upperLength*a.upperLength-along*along));
                a.upper.rotation=Quaternion.FromToRotation(a.lower.position-shoulder,elbow-shoulder)*a.upper.rotation;
                a.lower.rotation=Quaternion.FromToRotation(a.wrist.position-a.lower.position,target-a.lower.position)*a.lower.rotation;
                a.wrist.rotation=a.grip.rotation*a.wristRotation;
            }
        }
        void LateUpdate(){Pose();}
    }
}
