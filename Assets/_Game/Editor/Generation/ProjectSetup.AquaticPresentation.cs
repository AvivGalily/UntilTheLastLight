using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void CreateAquaticNecklace(ResearchSpecimen specimen,Transform actor,Transform model)
        {
            var neck=model.GetComponentsInChildren<Transform>().First(t=>t.name=="Neck");
            var chain=new GameObject("R-09 neck chain").transform;chain.SetParent(actor,false);specimen.keyChain=chain;
            var steel=LabMaterial("R09 tarnished necklace steel",new Color(.34f,.38f,.37f),.47f,.85f);
            LabPbr(steel,"LabSteel",4,.08f,new Color(.55f,.62f,.58f));
            var anchors=new[]{new Vector3(0,1.76f,-.16f),new Vector3(-.14f,1.74f,-.075f),new Vector3(-.17f,1.71f,.045f),new Vector3(-.135f,1.65f,.17f),new Vector3(-.095f,1.55f,.24f),new Vector3(0,1.45f,.265f),new Vector3(.095f,1.55f,.24f),new Vector3(.135f,1.65f,.17f),new Vector3(.17f,1.71f,.045f),new Vector3(.14f,1.74f,-.075f)};
            var points=new List<Vector3>();
            for(int k=0;k<anchors.Length;k++)for(int j=0;j<12;j++)
            {
                float t=j/12f;Vector3 p0=anchors[(k+anchors.Length-1)%anchors.Length],p1=anchors[k],p2=anchors[(k+1)%anchors.Length],p3=anchors[(k+2)%anchors.Length];
                points.Add(.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));
            }
            points.Add(points[0]);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            float spacing=.014f,remaining=0;int link=0;
            for(int k=1;k<points.Count;k++)
            {
                Vector3 segment=points[k]-points[k-1];float length=segment.magnitude;
                while(remaining<length)
                {
                    Vector3 centre=points[k-1]+segment.normalized*remaining,tangent=segment.normalized;
                    Vector3 side=Vector3.Cross(tangent,Mathf.Abs(tangent.z)>.9f?Vector3.up:Vector3.forward).normalized;
                    if(link%2==1)side=Vector3.Cross(tangent,side).normalized;
                    Vector3 normal=Vector3.Cross(tangent,side).normalized;int start=vertices.Count;
                    const int around=14,tube=6;
                    for(int a=0;a<=around;a++)for(int b=0;b<=tube;b++)
                    {
                        float angle=a*Mathf.PI*2/around,cross=b*Mathf.PI*2/tube;
                        Vector3 radial=(tangent*Mathf.Cos(angle)/.010f+side*Mathf.Sin(angle)/.006f).normalized;
                        vertices.Add(centre+tangent*(Mathf.Cos(angle)*.010f)+side*(Mathf.Sin(angle)*.006f)+(radial*Mathf.Cos(cross)+normal*Mathf.Sin(cross))*.0016f);
                        uv.Add(new Vector2(a/(float)around,b/(float)tube));
                        if(a<around&&b<tube){int n=start+a*(tube+1)+b;triangles.AddRange(new[]{n,n+1,n+tube+1,n+1,n+tube+2,n+tube+1});}
                    }
                    link++;remaining+=spacing;
                }
                remaining-=length;
            }
            LabMesh("R09 interlocking neck chain",vertices,triangles,uv,steel,chain);
            var key=new GameObject("Gate key on neck chain").transform;key.SetParent(actor,false);key.localPosition=new Vector3(0,1.45f,.27f);specimen.keyVisual=key;
            var brass=LabMaterial("Tarnished gate key brass",new Color(.49f,.33f,.09f),.55f,.82f,false,Color.black);
            LabPbr(brass,"LabSteel",5,.09f,new Color(.73f,.48f,.12f));
            var bow=new Vector3[33];for(int i=0;i<bow.Length;i++){float a=i*Mathf.PI*2/(bow.Length-1);bow[i]=new Vector3(Mathf.Sin(a)*.024f,Mathf.Cos(a)*.027f,0);}
            LabHose("Hollow key bow",bow,.004f,brass,key);
            Box("Key shaft",new Vector3(0,-.075f,0),new Vector3(.012f,.105f,.008f),brass,key,false);
            Box("Key shoulder",new Vector3(0,-.030f,0),new Vector3(.024f,.009f,.01f),brass,key,false);
            for(int i=0;i<3;i++)Box("Key cut "+i,new Vector3(.010f+i%2*.003f,-.103f+i*.014f,0),new Vector3(.024f,.008f,.009f),brass,key,false);
            var pickup=new GameObject("Recover key from neck chain",typeof(WorldItem),typeof(BoxCollider));pickup.transform.SetParent(key,false);pickup.layer=LayerMask.NameToLayer("Interactable");
            var item=pickup.GetComponent<WorldItem>();item.kind=ItemKind.ResearchCarcass;item.persistentId="research_carcass";items.Add(item);
            var box=pickup.GetComponent<BoxCollider>();box.center=new Vector3(0,-.05f,0);box.size=new Vector3(.46f,.46f,.34f);box.isTrigger=true;box.enabled=false;specimen.lootCollider=box;
            // Author placement in actor metres, then preserve that placement on
            // the actual neck bone, including the FBX axis/unit conversion.
            chain.SetParent(neck,true);key.SetParent(neck,true);
        }
    }
}
