using UnityEngine;
namespace Game
{
    // Imported shoulder/elbow/wrist weights preserve the grip's anatomy.
    // No mesh vertex is stretched or sheared toward the camera.
    public sealed class FirstPersonArm:MonoBehaviour
    {
        public Camera eyes;
        public Transform bodyFrame;
        public Vector3 bodyAnchor=new Vector3(.26f,-.50f,.025f);
        public Vector3 forearmAxis=Vector3.forward;
        public Transform upper,lower,wrist;
        public Vector3 wristOffset,elbowPole;
        public Quaternion upperRest,lowerRest,wristRest,wristRotation;
        public Vector3 upperPosition,lowerPosition,wristPosition;
        public float upperLength,lowerLength;
        public Vector3 ForearmEnd=>bodyFrame?bodyFrame.TransformPoint(bodyAnchor):eyes?eyes.transform.TransformPoint(bodyAnchor):Vector3.zero;
        public void Bind()
        {
            upperRest=upper.localRotation;lowerRest=lower.localRotation;wristRest=wrist.localRotation;
            upperPosition=upper.localPosition;lowerPosition=lower.localPosition;wristPosition=wrist.localPosition;
            upperLength=Vector3.Distance(upper.position,lower.position);lowerLength=Vector3.Distance(lower.position,wrist.position);
            wristOffset=transform.InverseTransformPoint(wrist.position);wristRotation=Quaternion.Inverse(transform.rotation)*wrist.rotation;
            elbowPole=new Vector3(bodyAnchor.x*1.3f,-.60f,.21f);
        }
        public void Pose()
        {
            if(!eyes||!upper||!lower||!wrist)return;
            upper.localPosition=upperPosition;lower.localPosition=lowerPosition;wrist.localPosition=wristPosition;
            upper.localRotation=upperRest;lower.localRotation=lowerRest;wrist.localRotation=wristRest;
            Vector3 target=transform.TransformPoint(wristOffset),shoulder=ForearmEnd,line=target-shoulder;
            float reach=upperLength+lowerLength-.003f;
            // The shoulder can follow a large swing; bone lengths never change.
            if(line.magnitude>reach)shoulder+=line.normalized*(line.magnitude-reach);
            upper.position=shoulder;line=target-shoulder;
            float distance=Mathf.Clamp(line.magnitude,.02f,reach);Vector3 direction=line.normalized;
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            Vector3 pole=eyes.transform.TransformPoint(elbowPole)-shoulder;pole-=direction*Vector3.Dot(pole,direction);pole.Normalize();
            Vector3 elbow=shoulder+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-shoulder,elbow-shoulder)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(wrist.position-lower.position,target-lower.position)*lower.rotation;
            wrist.rotation=transform.rotation*wristRotation;
        }
        void LateUpdate(){Pose();}
    }
}
