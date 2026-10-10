using UnityEngine;
namespace Game
{
    public sealed class BloodySurface:MonoBehaviour
    {
        public Renderer[] surfaces;
        public Transform anchor0,anchor1;
        public Vector4 wound0=new Vector4(0,.2f,.35f,.3f),wound1=new Vector4(-.06f,.2f,.76f,.12f);
        [Range(0,1)] public float amount=1;
        [Range(0,1)] public float lacerations;
        MaterialPropertyBlock block;
        Transform cached0,cached1;
        Vector3 offset0,offset1;
        void OnEnable()=>Refresh();
        public void Refresh()
        {
            if(surfaces==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            var a=transform.TransformPoint(new Vector3(wound0.x,wound0.y,wound0.z));var b=transform.TransformPoint(new Vector3(wound1.x,wound1.y,wound1.z));
            // Cache the rest-pose point in bone space so wounds follow a falling
            // wolf or moving head instead of staying suspended in world space.
            if(anchor0){if(cached0!=anchor0){offset0=anchor0.InverseTransformPoint(a);cached0=anchor0;}a=anchor0.TransformPoint(offset0);}
            if(anchor1){if(cached1!=anchor1){offset1=anchor1.InverseTransformPoint(b);cached1=anchor1;}b=anchor1.TransformPoint(offset1);}
            foreach(var surface in surfaces)
            {
                if(!surface)continue;surface.GetPropertyBlock(block);
                block.SetVector("_Wound0",new Vector4(a.x,a.y,a.z,wound0.w));block.SetVector("_Wound1",new Vector4(b.x,b.y,b.z,wound1.w));
                block.SetFloat("_BloodAmount",amount);block.SetFloat("_Lacerations",lacerations);surface.SetPropertyBlock(block);
            }
        }
    }
}
