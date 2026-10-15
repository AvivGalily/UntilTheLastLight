using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        const string LabTextureRoot="Assets/_Game/Art/Textures/";
        static void LabPbr(Material mat,string key,float density=1,float bump=.35f,Color? tint=null)
        {
            var colour=AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+key+"_BaseColor.png");
            if(colour)mat.SetTexture("_BaseMap",colour);
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+key+"_Normal.png");
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+key+"_Mask.png");
            if(normal){mat.SetTexture("_BumpMap",normal);mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",bump);}
            if(mask)
            {
                mat.SetTexture("_MetallicGlossMap",mask);mat.SetTexture("_OcclusionMap",mask);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");
                mat.SetFloat("_Smoothness",1);mat.SetFloat("_OcclusionStrength",.6f);
            }
            mat.SetColor("_BaseColor",tint??Color.white);
            mat.SetTextureScale("_BaseMap",Vector2.one*density);
            EditorUtility.SetDirty(mat);
        }
        static void TextureLabMaterial(Material mat,string name)
        {
            switch(name)
            {
                case "Laboratory brushed steel":LabPbr(mat,"LabSteel",2,.14f,new Color(.68f,.73f,.76f));break;
                case "Aged laboratory enamel":LabPbr(mat,"LabEnamel",1.1f,.19f);break;
                case "Containment rubber hoses":LabPbr(mat,"LabRubber",6,.42f);break;
                case "Containment clear glass":
                case "Broken laboratory glass":
                    LabPbr(mat,"LabGlass",1,.16f,new Color(1,1,1,name.StartsWith("Broken")?1:.85f));
                    mat.SetFloat("_SpecularHighlights",1);mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    // The broad default sky reflection makes a thin cylinder look opaque.
                    // Keep local light glints and alpha-attenuate the optical film.
                    mat.SetFloat("_BlendModePreserveSpecular",0);mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                    mat.SetFloat("_EnvironmentReflections",1);mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    break;
                case "Containment blue fluid":LabPbr(mat,"LabFluid",1,.06f,new Color(1,1,1,.65f));break;
                case "Spilled specimen reagent":LabPbr(mat,"LabReagent",3,.25f);break;
                case "Walker scientific study poster":LabPbr(mat,"LabPaper",1,.5f);break;
                case "Laboratory phosphor screen":
                    mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");break;
            }
        }
        [MenuItem("Last Light/Research/Install textured laboratory")]
        public static void InstallResearchSurfaces()
        {
            InstallResearchEncounter();
            Debug.Log("RESEARCH SURFACES INSTALLED: worn steel/enamel/rubber PBR, optical glass films, physical texture scale, rounded equipment edges, instrument panels and sample labels.");
        }
        static Mesh SaveLabSurfaceMesh(string name,Mesh mesh)
        {
            Directory.CreateDirectory(Root+"/Research/SurfaceMeshes");
            string path=Root+"/Research/SurfaceMeshes/"+name+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }
        static Mesh RoundedLabBox(string name,Vector3 size)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            float radius=Mathf.Min(.012f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.14f);
            Vector3 half=size*.5f,core=half-Vector3.one*radius;
            foreach(Vector3 n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                Vector3 u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n).normalized,v=Vector3.Cross(n,u);
                float hu=Vector3.Dot(half,new Vector3(Mathf.Abs(u.x),Mathf.Abs(u.y),Mathf.Abs(u.z))),hv=Vector3.Dot(half,new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z)));
                float hn=Vector3.Dot(half,new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z)));
                float[] U={-hu,-hu+radius*.15f,-hu+radius*.45f,-hu+radius,0,hu-radius,hu-radius*.45f,hu-radius*.15f,hu};
                float[] V={-hv,-hv+radius*.15f,-hv+radius*.45f,-hv+radius,0,hv-radius,hv-radius*.45f,hv-radius*.15f,hv};
                int start=vertices.Count;
                for(int y=0;y<9;y++)for(int x=0;x<9;x++)
                {
                    Vector3 p=n*hn+u*U[x]+v*V[y],q=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                    Vector3 normal=(p-q).normalized,r=q+normal*radius;
                    vertices.Add(new Vector3(r.x/size.x,r.y/size.y,r.z/size.z));normals.Add(Vector3.Scale(normal,size).normalized);uv.Add(new Vector2(U[x]+hu,V[y]+hv));
                    if(x<8&&y<8){int a=start+y*9+x;triangles.AddRange(new[]{a,a+1,a+9,a+1,a+10,a+9});}
                }
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
        static void FinishLabSurfaces(Transform root)
        {
            int index=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<Renderer>();var material=renderer?renderer.sharedMaterial:null;
                if(!material||!material.HasProperty("_BumpMap")||!material.GetTexture("_BumpMap"))continue;
                string source=AssetDatabase.GetAssetPath(filter.sharedMesh);
                if(source.Contains("Art/Meshes/"))continue;
                var scale=filter.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
                string name="Surface_"+(index++).ToString("D3")+"_"+filter.name;
                if(filter.sharedMesh.name=="Cube")filter.sharedMesh=SaveLabSurfaceMesh(name,RoundedLabBox(name,scale));
                else if(filter.sharedMesh.name=="Cylinder"&&!material.name.Contains("glass")&&!material.name.Contains("fluid"))
                {
                    var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=name;
                    var coordinates=mesh.uv;var points=mesh.vertices;var normals=mesh.normals;
                    for(int i=0;i<coordinates.Length;i++)
                        coordinates[i]=Mathf.Abs(normals[i].y)>.9f?new Vector2(points[i].x*scale.x,points[i].z*scale.z):Vector2.Scale(coordinates[i],new Vector2(Mathf.PI*scale.x,2*scale.y));
                    mesh.uv=coordinates;mesh.RecalculateTangents();filter.sharedMesh=SaveLabSurfaceMesh(name,mesh);
                }
            }
            AddLabMarkings(root);
        }
        static void LabCard(string name,Vector3 position,Vector2 size,Material material,Transform parent,int label=-1)
        {
            var vertices=new List<Vector3>{new Vector3(-size.x/2,-size.y/2,0),new Vector3(size.x/2,-size.y/2,0),new Vector3(-size.x/2,size.y/2,0),new Vector3(size.x/2,size.y/2,0)};
            float l=label<0?0:label*.25f,r=label<0?1:l+.25f;
            // From +Z the viewer's right points toward -X: keep the printed text readable.
            var card=LabMesh(name,vertices,new List<int>{0,1,2,1,3,2},new List<Vector2>{new Vector2(r,0),new Vector2(l,0),new Vector2(r,1),new Vector2(l,1)},material,parent);
            card.transform.localPosition=position;card.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        static void AddLabMarkings(Transform root)
        {
            var labels=LabMaterial("Printed research equipment labels",Color.white,.2f);
            labels.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+"LabLabels_BaseColor.png"));LabPbr(labels,"LabPaper",1,.3f);
            var panel=LabMaterial("Etched scientific instrument face",Color.white,.35f,.45f);
            panel.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+"LabInstrumentPanel_BaseColor.png"));LabPbr(panel,"LabPaper",1,.12f);
            LabCard("Etched oscilloscope face",new Vector3(-3,1.65f,-5.575f),new Vector2(.94f,.54f),panel,root);
            // Keep the live phosphor trace in front of its etched metal bezel.
            var screen=root.Find("Oscilloscope glowing screen");screen.localPosition+=Vector3.forward*.026f;
            var trace=root.Find("Live containment readout");trace.localPosition+=Vector3.forward*.026f;
            LabCard("Console maintenance label",new Vector3(-3,.83f,-5.465f),new Vector2(.53f,.26f),labels,root,1);
            LabCard("Containment caution plate",new Vector3(-4.45f,2.63f,-2.705f),new Vector2(.43f,.22f),labels,root,0);
            LabCard("Biohazard case label",new Vector3(7.8f,.43f,-5.452f),new Vector2(.47f,.24f),labels,root,3);
            for(int i=0;i<5;i++)LabCard("Sample jar identification "+i,new Vector3(4.6f+i*.32f,1.14f,-3.62f),new Vector2(.086f,.085f),labels,root,2);
            var steel=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Laboratory brushed steel.mat");
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var bolt=LabCylinder("Containment flange bolt "+i,new Vector3(-4.45f+Mathf.Cos(a)*1.015f,3.097f,-3.75f+Mathf.Sin(a)*1.015f),.026f,.018f,steel,root);
            }
            for(int i=0;i<7;i++)Box("Instrument cabinet ventilation slot "+i,new Vector3(-3.24f+i*.08f,.33f,-5.464f),new Vector3(.046f,.12f,.004f),AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Containment rubber hoses.mat"),root,false);
            EditorUtility.SetDirty(labels);EditorUtility.SetDirty(panel);
        }
    }
}
