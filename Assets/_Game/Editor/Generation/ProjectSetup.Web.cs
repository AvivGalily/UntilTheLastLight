using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static VictimWeb CreateVictimWeb(Transform parent,GameObject victim)
        {
            var root=new GameObject("Victim spider silk suspension",typeof(VictimWeb));root.transform.SetParent(parent,false);root.transform.localPosition=new Vector3(2.25f,2.2f,.16f);
            var web=root.GetComponent<VictimWeb>();var scaffold=ForestArt.Model("victim_web_01",Vector3.zero,Quaternion.identity,root.transform);
            web.intactCore=scaffold.GetComponentsInChildren<Renderer>().First(r=>r.name=="Web_Intact_Core");
            var silk=Material("Bone-attached spider silk",new Color(.52f,.56f,.50f));silk.SetFloat("_Smoothness",.3f);
            silk.EnableKeyword("_EMISSION");silk.SetColor("_EmissionColor",new Color(.022f,.027f,.022f));
            var bones=victim.GetComponentsInChildren<Transform>();Transform Bone(string name)=>bones.First(t=>t.name==name);
            LineRenderer Line(string name,float width)
            {
                var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(root.transform,false);
                line.gameObject.layer=LayerMask.NameToLayer("Decor");line.useWorldSpace=true;line.positionCount=3;line.startWidth=line.endWidth=width;
                line.numCornerVertices=2;line.numCapVertices=2;line.sharedMaterial=silk;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=true;return line;
            }
            string[] rows={"wrist","lowerarm01","upperarm01","spine03","spine02","upperleg01","lowerleg01"};int count=rows.Length*2;
            web.supports=new LineRenderer[count];web.supportBones=new Transform[count];web.boneOffsets=new Vector3[count];web.fixedAnchors=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                int row=i/2;float side=i%2==0?-1:1;string name=rows[row]+(row==3||row==4?"":"."+(i%2==0?"L":"R"));
                var bone=Bone(name);web.supportBones[i]=bone;web.boneOffsets[i]=bone.InverseTransformPoint(bone.position+new Vector3(side*.045f,0,-.10f));
                float angle=row==0?Mathf.PI*.5f+side*.30f:Mathf.Lerp(2.5f,4.4f,(row-1)/5f);
                web.fixedAnchors[i]=new Vector3(side*Mathf.Abs(Mathf.Cos(angle))*1.65f,Mathf.Sin(angle)*1.86f,-.02f);
                web.supports[i]=Line("Silk body tether "+i,.008f);
            }
            string[] bandNames={"spine03","spine03","spine03","spine02","spine02","upperleg01.L","upperleg01.R"};
            web.bodyBands=new LineRenderer[bandNames.Length];web.bandBones=new Transform[bandNames.Length];web.bandOffsets=new Vector3[bandNames.Length];web.bandHalfWidths=new float[bandNames.Length];
            for(int i=0;i<bandNames.Length;i++)
            {
                var bone=Bone(bandNames[i]);web.bandBones[i]=bone;
                var offset=new Vector3(0,(i%3-1)*.065f,-.19f);web.bandOffsets[i]=bone.InverseTransformPoint(bone.position+offset);
                web.bandHalfWidths[i]=i<5?.20f:.105f;web.bodyBands[i]=Line("Cocoon silk wrap "+i,.012f);
            }
            web.ResetWeb(false);return web;
        }
    }
}
