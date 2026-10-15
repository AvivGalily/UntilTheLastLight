using UnityEngine;
namespace Game
{
    // Only the two marked hero trees animate. Static trunk colliders never move.
    public sealed class HeroTree : MonoBehaviour
    {
        public SkinnedMeshRenderer[] crowns;
        int[] stage1,stage2;
        void Awake()
        {
            stage1=new int[crowns.Length];stage2=new int[crowns.Length];
            for(int n=0;n<crowns.Length;n++)
            { stage1[n]=ShapeIndex(crowns[n].sharedMesh,"Dread_Stage1");stage2[n]=ShapeIndex(crowns[n].sharedMesh,"Dread_Stage2"); }
        }
        public static int ShapeIndex(Mesh mesh,string name)
        {
            for(int n=0;n<mesh.blendShapeCount;n++)
            { string value=mesh.GetBlendShapeName(n);if(value==name||value.EndsWith("."+name))return n; }
            return -1;
        }
        void LateUpdate()
        {
            float dread=Shader.GetGlobalFloat("_Dread");
            for(int n=0;n<crowns.Length;n++)
            {
                var crown=crowns[n];
                int a=stage1[n],b=stage2[n];
                if(a>=0)crown.SetBlendShapeWeight(a,Mathf.Clamp01(dread*2)*100);
                if(b>=0)crown.SetBlendShapeWeight(b,Mathf.Clamp01(dread*2-1)*100);
            }
        }
    }
}
