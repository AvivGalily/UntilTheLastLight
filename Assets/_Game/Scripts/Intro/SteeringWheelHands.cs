using UnityEngine;
namespace Game
{
    public sealed class SteeringWheelHands : MonoBehaviour
    {
        public Transform wheel;
        public Transform[] grips;
        public FirstPersonArm[] arms;
        public void Pose(float steering,float degrees)
        {
            wheel.localRotation=Quaternion.Euler(18,0,-Mathf.Clamp(steering,-1,1)*degrees);
            // Grip anchors are children of the wheel; the forearms deform toward
            // the shoulders without moving the palms relative to the rim.
            foreach(var arm in arms)arm.Pose();
        }
    }
}
