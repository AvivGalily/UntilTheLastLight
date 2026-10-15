using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    // Renders the actual imported, animated FBX through the project's Unity URP pipeline.
    public static class CreatureVerification
    {
        const string Folder="Docs/Screenshots/Creature";
        static double started;
        public static void Capture()
        {
            started=EditorApplication.timeSinceStartup;EditorApplication.update+=Ready;
        }
        static void Ready()
        {
            if(EditorApplication.timeSinceStartup-started<3)return;EditorApplication.update-=Ready;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                ShaderUtil.allowAsyncCompilation=false;
                Directory.CreateDirectory(Folder);Directory.CreateDirectory(Folder+"/Crawl");
                ForestArt.Prepare();
                var model=ForestArt.Model("walker_01",Vector3.zero,Quaternion.identity);
                var animator=model.GetComponentInChildren<Animator>();
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
                int jaw=HeroTree.ShapeIndex(skin.sharedMesh,"Jaw_Open");
                AnimationClip crawl=null,idle=null;
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Meshes/walker_01.fbx"))if(asset is AnimationClip clip)
                {Debug.Log("CREATURE CLIP "+clip.name+" length="+clip.length);if(clip.name=="Crawler_Crawl")crawl=clip;if(clip.name=="Crawler_Idle")idle=clip;}
                if(!crawl||!idle||skin.bones.Length<28||jaw<0)throw new Exception("Crawler missing rig, clips or mouth");
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.30f,.31f,.33f);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.position=Vector3.down*.005f;floor.transform.localScale=Vector3.one*2;
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.19f,.20f,.22f);floor.GetComponent<Renderer>().sharedMaterial=material;
                LightAt("Key",new Vector3(-3,4,4),new Vector3(0,1,0),new Color(1,.88f,.81f),100);
                LightAt("Cool fill",new Vector3(3,2,2),Vector3.up,new Color(.72f,.82f,1),55);
                LightAt("Rim",new Vector3(1,3,-3),Vector3.up,new Color(.86f,.91f,1),110);
                var camera=new GameObject("Creature verification camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.18f,.20f);camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.fieldOfView=42;
                camera.GetUniversalAdditionalCameraData().renderShadows=true;
                idle.SampleAnimation(animator.gameObject,0);skin.SetBlendShapeWeight(jaw,100);
                // Prime the forward/shadow variants and new renderer buffers before saving.
                BakeShot(camera,model,"warmup",new Vector3(3.4f,2.2f,4.5f),new Vector3(0,.94f,.12f),1600,900,false);
                BakeShot(camera,model,"01-reference-three-quarter",new Vector3(3.4f,2.2f,4.5f),new Vector3(0,.94f,.12f),1600,900);
                BakeShot(camera,model,"02-anatomy-profile",new Vector3(4.8f,1.7f,.3f),new Vector3(0,.98f,.12f),1600,900);
                BakeShot(camera,model,"03-face-open",new Vector3(.75f,1.65f,2.05f),new Vector3(0,1.55f,.78f),1600,900);
                skin.SetBlendShapeWeight(jaw,0);
                BakeShot(camera,model,"04-face-resting",new Vector3(.75f,1.65f,2.05f),new Vector3(0,1.55f,.78f),1600,900);
                skin.SetBlendShapeWeight(jaw,100);BakeShot(camera,model,"07-decayed-mouth-open",new Vector3(.18f,1.49f,1.50f),new Vector3(0,1.44f,.93f),1600,900);
                skin.SetBlendShapeWeight(jaw,0);int bite=HeroTree.ShapeIndex(skin.sharedMesh,"Jaw_Bite");skin.SetBlendShapeWeight(bite,100);
                BakeShot(camera,model,"08-decayed-mouth-bite",new Vector3(.18f,1.49f,1.50f),new Vector3(0,1.44f,.93f),1600,900);skin.SetBlendShapeWeight(bite,0);
                BakeShot(camera,model,"05-human-skin-detail",new Vector3(.84f,1.83f,1.51f),new Vector3(.16f,1.65f,.82f),1600,900);
                BakeShot(camera,model,"06-forearm-skin",new Vector3(1.4f,.76f,1.75f),new Vector3(.61f,.53f,1.06f),1600,900);
                for(int n=0;n<24;n++)
                {
                    crawl.SampleAnimation(animator.gameObject,crawl.length*n/24);skin.SetBlendShapeWeight(jaw,65);
                    BakeShot(camera,model,"Crawl/frame-"+n.ToString("D2"),new Vector3(3.4f,2.2f,4.5f),new Vector3(0,.94f,.12f),960,540);
                }
                Debug.Log("CREATURE CAPTURE PASS: actual URP reference views, mouth states and 24 skeletal crawl frames; bones="+skin.bones.Length+" vertices="+skin.sharedMesh.vertexCount+" bounds="+skin.bounds);
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void LightAt(string name,Vector3 p,Vector3 target,Color color,float intensity)
        {
            var light=new GameObject(name,typeof(Light)).GetComponent<Light>();light.type=LightType.Spot;light.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
            light.color=color;light.intensity=intensity*.035f;light.range=12;light.spotAngle=75;light.innerSpotAngle=55;light.shadows=LightShadows.Soft;light.shadowBias=.015f;light.shadowNormalBias=.025f;
        }
        static void BakeShot(Camera camera,GameObject model,string name,Vector3 p,Vector3 target,int width,int height,bool save=true)
        {
            var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=new GameObject[renderers.Length];var meshes=new Mesh[renderers.Length];
            for(int i=0;i<renderers.Length;i++)
            {
                    var skin=renderers[i];meshes[i]=new Mesh();skin.BakeMesh(meshes[i]);
                    if(name=="01-reference-three-quarter")Debug.Log("BAKED CREATURE bounds="+meshes[i].bounds+" rendererScale="+skin.transform.lossyScale+" position="+skin.transform.position+" bone="+skin.bones[0].position);
                baked[i]=new GameObject("Synchronous verification skin",typeof(MeshFilter),typeof(MeshRenderer));baked[i].transform.SetParent(skin.transform,false);
                // CPU skin vertices already include the imported FBX's scale of 100.
                baked[i].transform.localScale=new Vector3(1/skin.transform.lossyScale.x,1/skin.transform.lossyScale.y,1/skin.transform.lossyScale.z);
                baked[i].GetComponent<MeshFilter>().sharedMesh=meshes[i];baked[i].GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(camera,request);
                var prior=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            if(save)File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
            for(int i=0;i<renderers.Length;i++){renderers[i].enabled=true;UnityEngine.Object.DestroyImmediate(baked[i]);UnityEngine.Object.DestroyImmediate(meshes[i]);}
        }
    }
}
