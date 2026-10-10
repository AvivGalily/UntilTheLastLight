using UnityEngine;
namespace Game
{
    // World body casts a human silhouette; the visible grip is parented to the axe.
    public sealed class PlayerPresence:MonoBehaviour
    {
        public const uint WorldShadowLayer=1,BodyShadowLayer=2;
        public Transform shadowBody,axeGrip,flashlightGrip;
        public Renderer[] bodyRenderers;
        public SkinnedMeshRenderer bodyMorph;
        public Vector3 standingScale=Vector3.one,standingOffset;
        public int crouchShape=-1;
        public Light gripLight;
        float phase;
        public void ResetPresence(){phase=0;shadowBody.localPosition=standingOffset;shadowBody.localRotation=Quaternion.identity;shadowBody.localScale=standingScale;if(bodyMorph&&crouchShape>=0)bodyMorph.SetBlendShapeWeight(crouchShape,0);}
        public void Simulate(float dt,PlayerMotor player)
        {
            if(dt<=0)return;
            phase+=player.TravelledThisStep*4;
            float sway=player.Moving?Mathf.Sin(phase):0;
            shadowBody.localPosition=standingOffset+Vector3.up*Mathf.Abs(sway)*.009f;
            shadowBody.localScale=standingScale;
            if(bodyMorph&&crouchShape>=0)bodyMorph.SetBlendShapeWeight(crouchShape,player.Crouching?100:0);
            shadowBody.localRotation=Quaternion.Euler(player.Sprinting?9:0,0,sway*(player.Sprinting?2.2f:1));
        }
        public void RefreshEquipment(bool holding){if(gripLight)gripLight.enabled=holding;}
    }
}
