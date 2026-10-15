using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static Material LabMaterial(string name,Color color,float smoothness=.3f,float metallic=0,bool transparent=false,Color? emission=null)
        {
            var mat=Material(name,color,metallic);mat.SetFloat("_Smoothness",smoothness);
            if(transparent)
            {
                mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;
                mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            if(emission.HasValue){mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",emission.Value);}
            TextureLabMaterial(mat,name);EditorUtility.SetDirty(mat);return mat;
        }
        static GameObject LabCylinder(string name,Vector3 p,float radius,float height,Material mat,Transform parent,bool collision=false)
            =>Primitive(name,PrimitiveType.Cylinder,p,new Vector3(radius*2,height*.5f,radius*2),mat,parent,collision);
        static GameObject LabSphere(string name,Vector3 p,Vector3 scale,Material mat,Transform parent)
            =>Primitive(name,PrimitiveType.Sphere,p,scale,mat,parent,false);
        static void LabText(string name,string content,Vector3 p,Quaternion rotation,float size,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=rotation;
            var label=go.GetComponent<TextMeshPro>();label.font=font;label.text=content;label.fontSize=size;label.color=color;
            label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1.4f,.35f);label.textWrappingMode=TextWrappingModes.NoWrap;
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;go.layer=LayerMask.NameToLayer("Decor");
        }
        static GameObject LabMesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uvs,Material mat,Transform parent)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Root+"/Research/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.layer=LayerMask.NameToLayer("Decor");
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }
        static void LabHose(string name,Vector3[] points,float radius,Material mat,Transform parent)
        {
            if(points.Length<=8)
            {
                var curved=new List<Vector3>();
                for(int k=0;k<points.Length-1;k++)for(int step=0;step<10;step++)
                {
                    float t=step/10f;Vector3 p0=points[Mathf.Max(0,k-1)],p1=points[k],p2=points[k+1],p3=points[Mathf.Min(points.Length-1,k+2)];
                    curved.Add(.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));
                }
                curved.Add(points[points.Length-1]);points=curved.ToArray();
            }
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int sides=20;float travelled=0;
            for(int i=0;i<points.Length;i++)
            {
                if(i>0)travelled+=Vector3.Distance(points[i],points[i-1]);
                Vector3 tangent=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
                Vector3 a=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized,b=Vector3.Cross(tangent,a);
                for(int j=0;j<=sides;j++)
                {
                    float angle=j*Mathf.PI*2/sides;vertices.Add(points[i]+(a*Mathf.Cos(angle)+b*Mathf.Sin(angle))*radius);uv.Add(new Vector2(j/(float)sides*Mathf.PI*2*radius,travelled));
                    if(i>0&&j<sides){int k=i*(sides+1)+j;triangles.AddRange(new[]{k,k-sides-1,k+1,k+1,k-sides-1,k-sides});}
                }
            }
            LabMesh(name,vertices,triangles,uv,mat,parent);
        }
        // Open shell, with a jagged upper edge for broken glassware.
        static GameObject LabShell(string name,float radius,float height,Material mat,Transform parent,bool broken=false)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int sides=64;
            float[] fracture={.73f,.85f,.46f,.58f,.92f,.66f,.61f,.78f,.73f};
            for(int y=0;y<2;y++)for(int j=0;j<=sides;j++)
            {
                float angle=j*Mathf.PI*2/sides;
                // A few contiguous fractures, rather than a sawtooth around every vertex.
                float sector=j*8f/sides;int edge=Mathf.Min(7,(int)sector);
                float top=broken?height*Mathf.Lerp(fracture[edge],fracture[edge+1],sector-edge):height;
                vertices.Add(new Vector3(Mathf.Cos(angle)*radius,y==0?0:top,Mathf.Sin(angle)*radius));uv.Add(new Vector2(j/(float)sides,y));
                if(y==1&&j<sides){int k=sides+1+j;triangles.AddRange(new[]{j,k,j+1,j+1,k,k+1});}
            }
            var go=LabMesh(name,vertices,triangles,uv,mat,parent);go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return go;
        }
        static void LabRing(string name,float height,Material material,Transform parent)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<=96;i++)for(int j=0;j<=8;j++)
            {
                float a=i*Mathf.PI*2/96,b=j*Mathf.PI*2/8,r=1.012f+Mathf.Cos(b)*.014f;
                vertices.Add(new Vector3(Mathf.Cos(a)*r,height+Mathf.Sin(b)*.014f,Mathf.Sin(a)*r));uv.Add(new Vector2(i/96f,j/8f));
                if(i<96&&j<8){int k=i*9+j;triangles.AddRange(new[]{k,k+1,k+9,k+1,k+10,k+9});}
            }
            LabMesh(name,vertices,triangles,uv,material,parent);
        }
        static void ResearchLaboratory(Transform building)
        {
            System.IO.Directory.CreateDirectory(Root+"/Research");
            var root=new GameObject("Research laboratory equipment").transform;root.SetParent(building,false);
            var steel=LabMaterial("Laboratory brushed steel",new Color(.18f,.24f,.25f),.55f,.75f);
            var enamel=LabMaterial("Aged laboratory enamel",new Color(.49f,.54f,.47f),.35f,.15f);
            var rubber=LabMaterial("Containment rubber hoses",new Color(.025f,.044f,.043f),.35f);
            var glow=LabMaterial("Containment cyan strips",new Color(.03f,.38f,.55f),.7f,0,false,new Color(.02f,.7f,1.1f));
            var glass=LabMaterial("Containment clear glass",new Color(.38f,.72f,.78f,.055f),.88f,0,true);
            var water=LabMaterial("Containment blue fluid",new Color(.015f,.22f,.39f,.045f),.7f,0,true,new Color(.003f,.025f,.045f));
            var vialGlass=LabMaterial("Broken laboratory glass",new Color(.42f,.73f,.72f,.29f),.9f,0,true);
            var sample=LabMaterial("Spilled specimen reagent",new Color(.12f,.26f,.11f),.88f);
            var red=LabMaterial("Laboratory warning red",new Color(.45f,.09f,.035f),.3f);
            var screen=LabMaterial("Laboratory phosphor screen",new Color(.008f,.04f,.038f),.6f,0,false,new Color(.015f,.27f,.19f));
            // The west room remains connected through its original wide doorway.
            var tank=new GameObject("Cylindrical blue specimen tank",typeof(ResearchSpecimen)).transform;tank.SetParent(root,false);tank.localPosition=new Vector3(-4.45f,0,-3.75f);
            var specimen=tank.GetComponent<ResearchSpecimen>();
            LabCylinder("Containment foot",new Vector3(0,.18f,0),1.13f,.30f,steel,tank);
            LabCylinder("Containment upper lid",new Vector3(0,2.97f,0),1.08f,.22f,steel,tank);
            ContainmentDoors(specimen,glass,tank);
            var liquid=LabShell("Containment blue water",.985f,2.51f,water,tank);liquid.transform.localPosition=Vector3.up*.34f;
            specimen.water=liquid.transform;
            var waterline=LabMaterial("Containment water surface",new Color(.025f,.20f,.26f,.28f),.7f,0,true,new Color(.002f,.04f,.06f));
            waterline.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(LabTextureRoot+"LabFluid_Normal.png"));waterline.EnableKeyword("_NORMALMAP");waterline.SetFloat("_BumpScale",.06f);EditorUtility.SetDirty(waterline);
            specimen.waterSurface=LabCylinder("Containment water meniscus",new Vector3(0,2.85f,0),.98f,.008f,waterline,tank).transform;
            foreach(float y in new[]{.36f,2.86f})LabRing("Cyan illuminated rim "+y,y,glow,tank);
            for(int i=0;i<4;i++)
            {
                float a=(45+i*90)*Mathf.Deg2Rad;Vector3 p=new Vector3(Mathf.Sin(a)*1.015f,1.6f,Mathf.Cos(a)*1.015f);
                Box("Tank vertical reinforcement",p,new Vector3(.045f,2.5f,.045f),steel,tank,false);
            }
            var solid=tank.gameObject.AddComponent<CapsuleCollider>();solid.radius=1.08f;solid.height=3.1f;solid.center=Vector3.up*1.55f;tank.gameObject.layer=LayerMask.NameToLayer("WorldSolid");
            specimen.tankSolid=solid;
            tank.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild=true;
            var obstacle=tank.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Capsule;obstacle.radius=1.08f;obstacle.height=3.1f;obstacle.center=Vector3.up*1.55f;obstacle.carving=true;specimen.tankObstacle=obstacle;
            var suspended=new GameObject("Floating humanoid").transform;suspended.SetParent(tank,false);suspended.localPosition=Vector3.up*.52f;specimen.suspended=suspended;
            CreateAquaticMonster(specimen,suspended);
            var support=new GameObject("Tank-mounted detachable life support").transform;support.SetParent(tank,false);support.localPosition=suspended.localPosition;specimen.lifeSupport=support;
            LabHose("Respirator to muzzle",new[]{new Vector3(0,2.4f,-.08f),new Vector3(.38f,2.24f,.10f),new Vector3(.37f,1.96f,.38f),new Vector3(0,1.91f,.31f)},.035f,rubber,support);
            LabHose("Chest perfusion tube",new[]{new Vector3(-.68f,2.4f,-.1f),new Vector3(-.72f,1.8f,.02f),new Vector3(-.58f,1.42f,.25f),new Vector3(-.20f,1.43f,.18f)},.028f,rubber,support);
            LabHose("Umbilical return tube",new[]{new Vector3(.7f,2.4f,-.15f),new Vector3(.75f,1.25f,.04f),new Vector3(.60f,.80f,.25f),new Vector3(.12f,1.06f,.18f)},.031f,rubber,support);
            specimen.attachedHoses=new[]{support.Find("Respirator to muzzle"),support.Find("Chest perfusion tube"),support.Find("Umbilical return tube")};
            LabHose("External coolant feed",new[]{new Vector3(-.8f,3.08f,0),new Vector3(-1.5f,3.08f,0),new Vector3(-1.65f,1.8f,0),new Vector3(-1.65f,.18f,-1.6f)},.055f,rubber,tank);
            specimen.bubbles=new Transform[18];
            for(int i=0;i<specimen.bubbles.Length;i++)specimen.bubbles[i]=LabSphere("Suspension bubble "+i,new Vector3(Mathf.Sin(i*9.2f)*.53f,.34f+i*.13f,Mathf.Cos(i*5.7f)*.53f),Vector3.one*(.017f+i%4*.007f),vialGlass,tank).transform;
            foreach(float y in new[]{.68f,2.6f})
            {
                var light=new GameObject("Internal blue containment light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(tank,false);light.transform.localPosition=new Vector3(0,y,.38f);
                light.type=LightType.Point;light.color=new Color(.10f,.55f,1);light.intensity=1.25f;light.range=5;light.shadows=LightShadows.None;
            }
            Box("Tank identification plate",new Vector3(0,.18f,1.12f),new Vector3(1.2f,.20f,.015f),rubber,tank,false);
            LabText("Specimen designation","R-09 / BIO-CONTAINMENT",new Vector3(0,.18f,1.14f),Quaternion.Euler(0,180,0),.70f,tank,new Color(.42f,.86f,.88f));
            // A large anatomical wall plate with four views of the actual Walker.
            var poster=LabMaterial("Walker scientific study poster",Color.white,.12f);
            poster.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Textures/WalkerResearch_BaseColor.png"));EditorUtility.SetDirty(poster);
            Box("Scientific poster frame",new Vector3(-8.78f,1.97f,2.7f),new Vector3(.055f,2.25f,3.37f),steel,root,false);
            var sheet=Primitive("Walker anatomy - four scientific angles",PrimitiveType.Quad,new Vector3(-8.735f,1.97f,2.7f),new Vector3(3.30f,2.2f,1),poster,root,false);sheet.transform.localRotation=Quaternion.Euler(0,-90,0);
            var posterLight=new GameObject("Archive inspection light",typeof(Light)).GetComponent<Light>();posterLight.transform.SetParent(root,false);posterLight.transform.localPosition=new Vector3(-7.65f,2.5f,2.7f);posterLight.type=LightType.Point;posterLight.color=new Color(.63f,.76f,.72f);posterLight.intensity=.7f;posterLight.range=4;
            // Tabletop instruments and broken, hollow test tubes with scattered shards.
            Box("Microscope base",new Vector3(-5.45f,1.09f,3.08f),new Vector3(.43f,.08f,.34f),steel,root,false);
            LabHose("Microscope curved arm",new[]{new Vector3(-5.57f,1.12f,3.08f),new Vector3(-5.58f,1.4f,3.08f),new Vector3(-5.38f,1.59f,3.08f)},.045f,steel,root);
            Box("Microscope stage",new Vector3(-5.38f,1.28f,3.08f),new Vector3(.28f,.025f,.24f),rubber,root,false);
            var scope=LabCylinder("Microscope eyepiece",new Vector3(-5.32f,1.53f,3.08f),.037f,.24f,rubber,root);scope.transform.localRotation=Quaternion.Euler(0,0,25);
            Box("Test tube rack base",new Vector3(-4.2f,1.10f,3.03f),new Vector3(.68f,.05f,.34f),steel,root,false);
            for(int i=0;i<6;i++)
            {
                var tube=LabShell("Fractured test tube "+i,.035f,.20f+i%3*.025f,vialGlass,root,true);tube.transform.localPosition=new Vector3(-4.44f+i*.095f,1.13f,3.03f);
                if(i>3)tube.transform.localRotation=Quaternion.Euler(65,15*i,20);
                LabCylinder("Reagent in broken tube "+i,new Vector3(0,.03f,0),.029f,.06f,sample,tube.transform);
            }
            var rng=new System.Random(923);
            for(int i=0;i<27;i++)
            {
                Vector3 p=i<14?new Vector3(-4.7f+(float)rng.NextDouble()*1.6f,1.059f,2.65f+(float)rng.NextDouble()*.65f):new Vector3(-3.5f+(float)rng.NextDouble()*.8f,.075f,2.4f+(float)rng.NextDouble()*1.4f);
                float size=.02f+(float)rng.NextDouble()*.055f;
                var shard=LabMesh("Broken test tube shard "+i,new List<Vector3>{Vector3.zero,new Vector3(size,.008f,.01f),new Vector3(size*.35f,.003f,size*.75f)},new List<int>{0,1,2},new List<Vector2>{Vector2.zero,Vector2.right,Vector2.up},vialGlass,root);shard.transform.localPosition=p;shard.transform.localRotation=Quaternion.Euler(0,i*137,0);
            }
            LabSphere("Spilled reagent on bench",new Vector3(-3.7f,1.052f,3.2f),new Vector3(.53f,.009f,.27f),sample,root);
            for(int i=0;i<5;i++)
            {
                var jar=LabShell("Sealed research sample jar "+i,.075f,.22f,vialGlass,root);jar.transform.localPosition=new Vector3(4.6f+i*.32f,1.04f,-3.7f);
                LabCylinder("Sample jar lid "+i,new Vector3(4.6f+i*.32f,1.27f,-3.7f),.082f,.035f,steel,root);
                LabSphere("Preserved tissue sample "+i,new Vector3(4.6f+i*.32f,1.12f,-3.7f),new Vector3(.07f,.11f,.065f),sample,root);
            }
            Box("Containment instrument console",new Vector3(-3.0f,.7f,-5.8f),new Vector3(1.1f,1.3f,.65f),enamel,root);
            Box("Oscilloscope casing",new Vector3(-3,1.65f,-5.8f),new Vector3(.95f,.55f,.4f),steel,root,false);
            Box("Oscilloscope glowing screen",new Vector3(-2.88f,1.65f,-5.59f),new Vector3(.57f,.36f,.008f),screen,root,false);
            var trace=new List<Vector3>();for(int i=0;i<60;i++)trace.Add(new Vector3(-3.14f+i*.0088f,1.65f+Mathf.Sin(i*.55f)*.055f+(i%23==0?.09f:0),-5.578f));LabHose("Live containment readout",trace.ToArray(),.003f,glow,root);
            for(int i=0;i<4;i++)LabSphere("Instrument dial "+i,new Vector3(-3.24f,1.50f+i*.09f,-5.56f),Vector3.one*.038f,rubber,root);
            LabText("Containment status","R-09 / VITALS",new Vector3(-3,1.22f,-5.46f),Quaternion.Euler(0,180,0),.72f,root,new Color(.16f,.28f,.24f));
            Box("Biohazard case",new Vector3(7.8f,.35f,-5.8f),new Vector3(.8f,.6f,.65f),enamel,root);
            Box("Biohazard case stripe",new Vector3(7.8f,.35f,-5.46f),new Vector3(.65f,.09f,.012f),red,root,false);
            ResearchShelvedSpecimens(root,steel,glass,water);
            ResearchReleaseControl(root,specimen,steel,rubber);
            FinishLabSurfaces(root);
        }
    }
}
