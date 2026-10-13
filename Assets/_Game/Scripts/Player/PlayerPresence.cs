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
        public int[] walkShapes=new int[4]{-1,-1,-1,-1},runShapes=new int[4]{-1,-1,-1,-1};
        public Light gripLight;
        public void BindGaitShapes()
        {
            for(int i=0;i<bodyMorph.sharedMesh.blendShapeCount;i++)
            {
                string name=bodyMorph.sharedMesh.GetBlendShapeName(i);
                for(int n=0;n<4;n++){if(name.EndsWith("Walk"+n))walkShapes[n]=i;if(name.EndsWith("Run"+n))runShapes[n]=i;}
            }
        }
        void ClearGait()
        {
            if(!bodyMorph)return;
            foreach(int shape in walkShapes)if(shape>=0)bodyMorph.SetBlendShapeWeight(shape,0);
            foreach(int shape in runShapes)if(shape>=0)bodyMorph.SetBlendShapeWeight(shape,0);
        }
        public void ResetPresence(){ClearGait();shadowBody.localPosition=standingOffset;shadowBody.localRotation=Quaternion.identity;shadowBody.localScale=standingScale;if(bodyMorph&&crouchShape>=0)bodyMorph.SetBlendShapeWeight(crouchShape,0);}
        public void Simulate(float dt,PlayerMotor player)
        {
            if(dt<=0)return;
            ClearGait();shadowBody.localPosition=standingOffset;
            shadowBody.localScale=standingScale;
            if(bodyMorph&&crouchShape>=0)bodyMorph.SetBlendShapeWeight(crouchShape,player.Crouching?100:0);
            // The imported leg/arm poses provide swing and foot contact. Keep
            // the placement root level so boots are not tipped off the ground.
            shadowBody.localRotation=Quaternion.identity;
            if(bodyMorph&&player.Moving&&player.Grounded&&!player.Crouching)
            {
                var shapes=player.Sprinting?runShapes:walkShapes;
                float sample=player.GaitPhase*4;int first=Mathf.FloorToInt(sample)%4;float blend=sample-first;
                if(shapes[first]>=0)bodyMorph.SetBlendShapeWeight(shapes[first],100*(1-blend));
                if(shapes[(first+1)%4]>=0)bodyMorph.SetBlendShapeWeight(shapes[(first+1)%4],100*blend);
            }
        }
        public void RefreshEquipment(bool holding){if(gripLight){gripLight.enabled=false;gripLight.intensity=0;}}
    }
}
