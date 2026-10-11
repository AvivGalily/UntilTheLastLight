using UnityEngine;
namespace Game
{
    public sealed class IntroVehicleController : MonoBehaviour
    {
        public Transform[] waypoints;
        public Transform driverEye;
        public SteeringWheelHands hands;
        public Light[] headlights;
        float[] lengths;
        float total;
        public float Offset {get;private set;}
        public float Steering {get;private set;}
        public float Distance {get;private set;}
        public void ResetDrive(){Offset=Steering=Distance=0;BuildPath();}
        void BuildPath()
        {
            lengths=new float[waypoints.Length];total=0;
            for(int n=1;n<waypoints.Length;n++){total+=Vector3.Distance(waypoints[n-1].position,waypoints[n].position);lengths[n]=total;}
        }
        public void Sample(float distance,out Vector3 centre,out Quaternion rotation)
        {
            if(lengths==null)BuildPath();distance=Mathf.Clamp(distance,0,total-.001f);
            int n=1;while(n<lengths.Length-1&&lengths[n]<distance)n++;
            float fraction=Mathf.InverseLerp(lengths[n-1],lengths[n],distance);
            centre=Vector3.Lerp(waypoints[n-1].position,waypoints[n].position,fraction);
            Vector3 before=(waypoints[n].position-waypoints[Mathf.Max(0,n-2)].position).normalized;
            Vector3 after=(waypoints[Mathf.Min(waypoints.Length-1,n+1)].position-waypoints[n-1].position).normalized;
            rotation=Quaternion.LookRotation(Vector3.Slerp(before,after,fraction),Vector3.up);
        }
        public float AllowedOffset(float distance,ForestIntroConfig config)
        {
            float gap=Mathf.Abs(distance-config.gateDistance);
            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,config.gateAssistDistance,gap));
            return Mathf.Lerp(config.gateSteeringLimit,config.steeringLimit,blend);
        }
        public void Simulate(float dt,float time,float input,ForestIntroConfig config)
        {
            if(dt<=0)return;
            Distance=config.DistanceAt(time);
            float blend=1-Mathf.Exp(-dt*config.steeringResponse);
            Steering=Mathf.Lerp(Steering,Mathf.Clamp(input,-1,1),blend);
            Offset=Mathf.Lerp(Offset,Steering*AllowedOffset(Distance,config),blend);
            Sample(Distance,out var centre,out var rotation);
            float bounce=(Mathf.Sin(time*14)+Mathf.Sin(time*21)*.4f)*config.suspension;
            transform.SetPositionAndRotation(centre+rotation*Vector3.right*Offset+Vector3.up*bounce,rotation);
            hands.Pose(Steering,config.wheelDegrees);
            float flicker=time>=config.blast-.25f&&time<config.blackout?.25f:1;
            foreach(var light in headlights)light.intensity=4.5f*flicker;
        }
    }
}
