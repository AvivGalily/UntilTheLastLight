using UnityEngine;
namespace Game
{
    // Imported shoulder/elbow/wrist weights preserve the grip's anatomy.
    // No mesh vertex is stretched or sheared toward the camera.
    public sealed class FirstPersonArm:MonoBehaviour
    {
        public Camera eyes;
        public Transform bodyFrame;
        public Vector3 bodyAnchor=new Vector3(.32f,-.32f,.10f);
        public Vector3 forearmAxis=Vector3.forward;
        public Transform upper,lower,wrist,lowerTwist;
        public Vector3 wristOffset,elbowPole;
        public Vector3 handForward=Vector3.forward;
        public Quaternion upperRest,lowerRest,wristRest,wristRotation,lowerTwistRest;
        public Vector3 upperPosition,lowerPosition,wristPosition;
        public float upperLength,lowerLength;
        public Vector3 ForearmEnd=>bodyFrame?bodyFrame.TransformPoint(bodyAnchor):eyes?eyes.transform.TransformPoint(bodyAnchor):Vector3.zero;
        public void Bind()
        {
            upperRest=upper.localRotation;lowerRest=lower.localRotation;wristRest=wrist.localRotation;
            upperPosition=upper.localPosition;lowerPosition=lower.localPosition;wristPosition=wrist.localPosition;
            upperLength=Vector3.Distance(upper.position,lower.position);lowerLength=Vector3.Distance(lower.position,wrist.position);
            wristOffset=transform.InverseTransformPoint(wrist.position);wristRotation=Quaternion.Inverse(transform.rotation)*wrist.rotation;
            Vector3 knuckles=Vector3.zero;int count=0;string side=wrist.name.EndsWith(".L")?".L":".R";
            foreach(var bone in GetComponentsInChildren<Transform>())if(bone.name=="lowerarm02"+side)lowerTwist=bone;
            if(lowerTwist)lowerTwistRest=lowerTwist.localRotation;
            foreach(var bone in GetComponentsInChildren<Transform>())
                if(bone.name.StartsWith("finger")&&bone.name.Contains("-1.")&&!bone.name.StartsWith("finger1-")&&bone.name.EndsWith(side)){knuckles+=bone.position;count++;}
            if(count>0)handForward=transform.InverseTransformDirection(knuckles/count-wrist.position).normalized;
            elbowPole=new Vector3(bodyAnchor.x*1.3f,-.60f,.21f);
        }
        public void Pose()
        {
            if(!eyes||!upper||!lower||!wrist)return;
            upper.localPosition=upperPosition;lower.localPosition=lowerPosition;wrist.localPosition=wristPosition;
            upper.localRotation=upperRest;lower.localRotation=lowerRest;wrist.localRotation=wristRest;
            if(lowerTwist)lowerTwist.localRotation=lowerTwistRest;
            Vector3 target=transform.TransformPoint(wristOffset),shoulder=ForearmEnd,line=target-shoulder;
            float reach=upperLength+lowerLength-.003f;
            // The shoulder can follow a large swing; bone lengths never change.
            if(line.magnitude>reach)shoulder+=line.normalized*(line.magnitude-reach);
            upper.position=shoulder;line=target-shoulder;
            float distance=Mathf.Clamp(line.magnitude,.02f,reach);Vector3 direction=line.normalized;
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            // Choose the elbow on its reach circle behind the palm. A fixed low
            // screen-space elbow makes an overhand wrist fold back on itself.
            Vector3 bodyPole=Vector3.ProjectOnPlane(eyes.transform.TransformPoint(elbowPole)-shoulder,direction).normalized;
            Vector3 pole=Vector3.ProjectOnPlane(-transform.TransformDirection(handForward),direction);
            if(pole.sqrMagnitude<.001f)pole=bodyPole;
            else pole=Vector3.Slerp(bodyPole,pole.normalized,.7f);
            pole.Normalize();
            Vector3 elbow=shoulder+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-shoulder,elbow-shoulder)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(wrist.position-lower.position,target-lower.position)*lower.rotation;
            Quaternion gripRotation=transform.rotation*wristRotation;
            Vector3 forearm=(target-lower.position).normalized;
            Vector3 reference=Mathf.Abs(Vector3.Dot(gripRotation*Vector3.forward,forearm))<.8f?Vector3.forward:Vector3.right;
            Vector3 from=Vector3.ProjectOnPlane(wrist.rotation*reference,forearm),to=Vector3.ProjectOnPlane(gripRotation*reference,forearm);
            if(from.sqrMagnitude>.001f&&to.sqrMagnitude>.001f)
            {
                // Spread pronation along both native forearm bones instead of
                // winding the entire change into the small wrist skin weights.
                float roll=Vector3.SignedAngle(from,to,forearm);
                lower.rotation=Quaternion.AngleAxis(roll*(lowerTwist?.5f:1),forearm)*lower.rotation;
                if(lowerTwist)lowerTwist.rotation=Quaternion.AngleAxis(roll*.5f,(target-lowerTwist.position).normalized)*lowerTwist.rotation;
            }
            wrist.rotation=gripRotation;
        }
        void LateUpdate(){Pose();}
    }
}
