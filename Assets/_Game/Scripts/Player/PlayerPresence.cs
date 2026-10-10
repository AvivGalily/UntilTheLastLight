using UnityEngine;
namespace Game
{
    // World body casts a human silhouette; the visible grip is parented to the axe.
    public sealed class PlayerPresence:MonoBehaviour
    {
        public Transform shadowBody,axeGrip;
        public Renderer[] bodyRenderers;
        public Light gripLight;
        float phase;
        public void ResetPresence(){phase=0;shadowBody.localPosition=Vector3.zero;shadowBody.localRotation=Quaternion.identity;shadowBody.localScale=Vector3.one;}
        public void Simulate(float dt,PlayerMotor player)
        {
            if(dt<=0)return;
            phase+=player.TravelledThisStep*4;
            float sway=player.Moving?Mathf.Sin(phase):0;
            shadowBody.localPosition=Vector3.up*(player.Crouching?-.03f:Mathf.Abs(sway)*.009f);
            shadowBody.localScale=new Vector3(1,player.Crouching?.72f:1,1);
            shadowBody.localRotation=Quaternion.Euler(player.Sprinting?9:0,0,sway*(player.Sprinting?2.2f:1));
        }
        public void RefreshEquipment(bool holding){if(gripLight)gripLight.enabled=holding;}
    }
}
