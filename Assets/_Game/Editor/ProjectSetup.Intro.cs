using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        [MenuItem("Last Light/Opening/Install forest opening")]
        public static void InstallForestIntro()
        {
            EditorSceneManager.OpenScene(Root+"/Scenes/ForestGame.unity");
            ForestArt.Prepare();CreateInput();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/FieldSans.asset");
            var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if(game.ambience&&game.ambience.insects){game.ambience.insects.Stop();game.ambience.insects.volume=0;game.ambience.insects.playOnAwake=false;EditorUtility.SetDirty(game.ambience.insects);}
            UnityEngine.Object.DestroyImmediate(game.hud.transform.Find("HUD").gameObject);
            UnityEngine.Object.DestroyImmediate(game.hud.transform.Find("Menu").gameObject);
            UnityEngine.Object.DestroyImmediate(game.hud.blood.gameObject);
            game.hud.Construct();
            game.inputTemplate=inputs;
            if(game.intro)UnityEngine.Object.DestroyImmediate(game.intro.gameObject);
            WireForestIntro(game);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("FOREST INTRO INSTALLED: New Game -> 110 second first person drive -> existing cabin; Continue bypasses opening.");
        }
        static Material IntroMaterial(string name,Color color,string source=null)
        {
            string path=Root+"/Materials/Intro "+name+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!result){var template=source==null?null:AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+source+".mat");result=template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,path);}
            result.color=color;result.enableInstancing=true;EditorUtility.SetDirty(result);return result;
        }
        static Transform IntroNode(string name,Transform parent,Vector3 position)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
        static GameObject IntroBox(string name,Transform parent,Vector3 pos,Vector3 size,Material material)
        {return Box(name,pos,size,material,parent,false);}
        static AudioSource IntroSource(Transform parent,string name,AudioClip clip,bool loop,float spatial=0)
        {
            var go=IntroNode(name,parent,Vector3.zero).gameObject;var source=go.AddComponent<AudioSource>();
            source.clip=clip;source.loop=loop;source.playOnAwake=false;source.volume=0;source.spatialBlend=spatial;source.minDistance=1;source.maxDistance=45;source.rolloffMode=AudioRolloffMode.Logarithmic;
            if(spatial>0){var filter=go.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=6500;}
            return source;
        }
        static AudioClip IntroClip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+name+".wav");
        static void WireForestIntro(GameSession game)
        {
            var root=new GameObject("Forest opening sequence",typeof(ForestIntroDirector));var intro=root.GetComponent<ForestIntroDirector>();game.intro=intro;
            intro.config=Asset<ForestIntroConfig>(Root+"/Data/ForestIntroConfig.asset");
            var c=intro.config;
            // Preserve Inspector tuning and VO assignments when reinstalling.
            if(c.lines.Length==0)
            {
                float[] times={8,14,20,34,40,44,50,57,64.5f,80,88,90};float[] durations={5,5,7,5,3,5,5.5f,6,4,3,2,1.8f};
                int[] speakers={1,0,1,2,0,2,1,0,1,2,0,1};string[] keys={"intro_jack","intro_tyler","intro_ethan"};
                c.lines=Enumerable.Range(0,12).Select(n=>new IntroLine{id="L"+(n+1).ToString("00"),speakerKey=keys[speakers[n]],passenger=speakers[n],start=times[n],duration=durations[n]}).ToArray();
                EditorUtility.SetDirty(c);
            }
            var world=IntroNode("Opening forest and vehicle",root.transform,Vector3.zero);
            var path=IntroNode("Driving path",world,Vector3.zero);var route=new Transform[41];
            for(int n=0;n<route.Length;n++)
            {
                float z=n*8;
                route[n]=IntroNode("Path "+n,path,new Vector3(800+Mathf.Sin(z/56)*9+Mathf.Sin(z/25)*2,0,z));
            }
            var vehicle=ForestArt.Model("intro_vehicle_01",route[0].position,Quaternion.identity,world);
            intro.vehicle=vehicle.AddComponent<IntroVehicleController>();var drive=intro.vehicle;
            var dash=IntroMaterial("dashboard",new Color(.028f,.040f,.036f));var iron=IntroMaterial("rusted iron",new Color(.16f,.20f,.18f),"Oxidized steel");
            var cloth=IntroMaterial("seat fabric",new Color(.14f,.19f,.15f),"PlayerSleeve");
            var paint=IntroMaterial("vehicle body paint",new Color(.12f,.17f,.14f),"Oxidized steel");paint.SetTexture("_BaseMap",null);paint.SetFloat("_Metallic",.45f);paint.SetFloat("_Smoothness",.25f);
            foreach(var renderer in vehicle.GetComponentsInChildren<Renderer>())
            {
                var mats=renderer.sharedMaterials;
                for(int n=0;n<mats.Length;n++){if(mats[n].name=="Ranger canvas")mats[n]=dash;else if(mats[n].name=="Oxidized steel")mats[n]=paint;else if(mats[n].name=="PlayerSleeve")mats[n]=cloth;}
                renderer.sharedMaterials=mats;
            }
            drive.driverEye=IntroNode("Driver eye anchor",vehicle.transform,new Vector3(-.44f,1.44f,.12f));
            var wheel=IntroNode("Steering pivot",vehicle.transform,new Vector3(-.44f,1.12f,.50f));
            ForestArt.Model("intro_wheel_01",Vector3.zero,Quaternion.identity,wheel);
            drive.hands=vehicle.AddComponent<SteeringWheelHands>();drive.hands.wheel=wheel;
            drive.hands.grips=new Transform[2];drive.hands.arms=new FirstPersonArm[0];
            wheel.localRotation=Quaternion.Euler(18,0,0);
            var driver=ForestArt.Model("intro_driver_01",new Vector3(-.44f,.07f,.04f),Quaternion.identity,vehicle.transform);
            driver.name="Seated driver body and anatomical wheel hands";
            drive.hands.driver=driver.AddComponent<IntroDriverPose>();var driverPose=drive.hands.driver;driverPose.arms=new IntroDriverPose.Arm[2];
            Transform DriverBone(string name)=>driver.GetComponentsInChildren<Transform>().First(t=>t.name==name);
            var driverOutfit=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/IntroOutfit0.mat"));driverOutfit.name="Driver field jacket";driverOutfit.color=new Color(.64f,.78f,.68f);
            string driverMaterial=Root+"/Materials/Intro driver clothing.mat";var previousDriver=AssetDatabase.LoadAssetAtPath<Material>(driverMaterial);
            if(previousDriver){EditorUtility.CopySerialized(driverOutfit,previousDriver);UnityEngine.Object.DestroyImmediate(driverOutfit);driverOutfit=previousDriver;}else AssetDatabase.CreateAsset(driverOutfit,driverMaterial);
            foreach(var renderer in driver.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.updateWhenOffscreen=true;renderer.receiveShadows=true;
                var mats=renderer.sharedMaterials;for(int n=0;n<mats.Length;n++)if(mats[n].name=="IntroOutfit0")mats[n]=driverOutfit;renderer.sharedMaterials=mats;
            }
            for(int n=0;n<2;n++)
            {
                float sign=n==0?-1:1;
                var grip=IntroNode(n==0?"Left rim grip":"Right rim grip",wheel,new Vector3(sign*.211f,.102f,0));drive.hands.grips[n]=grip;
                string side=driver.transform.InverseTransformPoint(DriverBone("wrist.L").position).x*sign>0?"L":"R";
                driverPose.arms[n]=new IntroDriverPose.Arm{upper=DriverBone("upperarm01."+side),lower=DriverBone("lowerarm01."+side),wrist=DriverBone("wrist."+side),grip=grip,pole=new Vector3(sign*.28f,.76f,.18f)};
            }
            driverPose.Bind();driverPose.Pose();
            drive.headlights=new Light[2];
            for(int n=0;n<2;n++)
            {
                var head=IntroNode("Headlight "+n,vehicle.transform,new Vector3(n==0?-.65f:.65f,.94f,2.31f));head.localRotation=Quaternion.Euler(9,0,0);
                var light=head.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.range=65;light.spotAngle=68;light.innerSpotAngle=38;light.intensity=4.5f;light.color=new Color(.91f,.87f,.69f);light.shadows=n==0?LightShadows.Soft:LightShadows.None;
                drive.headlights[n]=light;
            }
            var glow=IntroNode("Dashboard amber glow",vehicle.transform,new Vector3(-.24f,1.40f,.67f)).gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.range=2.3f;glow.intensity=.40f;glow.color=new Color(.72f,.76f,.65f);
            var cabinFill=IntroNode("Soft cabin dome light",vehicle.transform,new Vector3(0,1.78f,-.35f)).gameObject.AddComponent<Light>();
            cabinFill.type=LightType.Point;cabinFill.range=3.2f;cabinFill.intensity=.65f;cabinFill.color=new Color(1,.84f,.65f);cabinFill.shadows=LightShadows.None;
            var gaugeMat=IntroMaterial("instrument glow",new Color(.34f,.49f,.39f));gaugeMat.EnableKeyword("_EMISSION");gaugeMat.SetColor("_EmissionColor",new Color(.19f,.35f,.22f)*.5f);
            for(int n=0;n<2;n++)
            {
                var face=Primitive("Instrument face",PrimitiveType.Cylinder,new Vector3(-.57f+n*.24f,1.13f,.625f),new Vector3(.16f,.004f,.16f),dash,vehicle.transform,false);face.transform.localRotation=Quaternion.Euler(90,0,0);
                for(int tick=0;tick<10;tick++)
                {float a=(-130+tick*28)*Mathf.Deg2Rad;IntroBox("Gauge marking",vehicle.transform,new Vector3(-.57f+n*.24f+Mathf.Sin(a)*.065f,1.13f+Mathf.Cos(a)*.065f,.618f),new Vector3(.003f,.010f,.005f),gaugeMat);}
                var needle=IntroBox("Gauge needle",vehicle.transform,new Vector3(-.57f+n*.24f,1.13f,.613f),new Vector3(.006f,.10f,.005f),gaugeMat);needle.transform.localRotation=Quaternion.Euler(0,0,n==0?-28:40);
            }
            var bridge=root.AddComponent<IntroTransitionBridge>();intro.bridge=bridge;bridge.environment=world.gameObject;bridge.cabinAnchor=game.cabinSpawn;
            // A wide auxiliary camera keeps both back-seat hunters in the mirror.
            var mirror=IntroNode("Rear view mirror",vehicle.transform,new Vector3(.04f,1.75f,.78f));
            IntroBox("Mirror frame",mirror,Vector3.zero,new Vector3(.52f,.25f,.036f),dash);
            var surface=Primitive("Rear view glass",PrimitiveType.Quad,new Vector3(0,0,-.023f),new Vector3(.49f,.225f,1),dash,mirror,false);
            var target=AssetDatabase.LoadAssetAtPath<RenderTexture>(Root+"/Data/IntroMirror.renderTexture");
            if(!target){target=new RenderTexture(512,256,16);target.name="Opening rear view";AssetDatabase.CreateAsset(target,Root+"/Data/IntroMirror.renderTexture");}
            target.Release();target.width=512;target.height=256;EditorUtility.SetDirty(target);
            var reflective=IntroMaterial("rear view",Color.white);reflective.shader=Shader.Find("Universal Render Pipeline/Unlit");reflective.mainTexture=target;reflective.mainTextureScale=new Vector2(-1,1);reflective.mainTextureOffset=new Vector2(1,0);surface.GetComponent<Renderer>().sharedMaterial=reflective;
            var mirrorEye=IntroNode("Mirror reflection camera",vehicle.transform,new Vector3(0,1.50f,-.25f));mirrorEye.localRotation=Quaternion.LookRotation(new Vector3(0,1.36f,-.91f)-mirrorEye.localPosition);
            bridge.mirrorCamera=mirrorEye.gameObject.AddComponent<Camera>();bridge.mirrorCamera.fieldOfView=50;bridge.mirrorCamera.aspect=2;bridge.mirrorCamera.targetTexture=target;bridge.mirrorCamera.nearClipPlane=.06f;bridge.mirrorCamera.farClipPlane=35;bridge.mirrorCamera.enabled=false;bridge.mirrorCamera.depth=-1;
            intro.dialogue=root.AddComponent<IntroDialogueController>();var dialogue=intro.dialogue;
            dialogue.passengers=new IntroPassenger[3];dialogue.voices=new AudioSource[3];
            string[] models={"intro_jack_01","intro_tyler_01","intro_ethan_01"};Vector3[] seats={new Vector3(.44f,.08f,.04f),new Vector3(.44f,.07f,-.95f),new Vector3(-.44f,.12f,-.95f)};
            for(int n=0;n<3;n++)
            {
                var npc=ForestArt.Model(models[n],seats[n],Quaternion.identity,vehicle.transform);npc.name=new[]{"Jack front passenger","Tyler rear right","Ethan rear left"}[n];
                npc.AddComponent<HunterIdentity>().characterId=new[]{"jack","tyler","ethan"}[n];
                foreach(var r in npc.GetComponentsInChildren<Renderer>())r.receiveShadows=true;
                Transform Bone(string name)=>npc.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name);
                var passenger=npc.AddComponent<IntroPassenger>();passenger.head=Bone("head");passenger.spine=Bone("spine01");passenger.jaw=Bone("jaw");passenger.gesture=Bone("lowerarm01.R");dialogue.passengers[n]=passenger;
                if(!passenger.head||!passenger.spine)throw new Exception("Missing seated hunter rig: "+models[n]);
                dialogue.voices[n]=IntroSource(npc.transform,"Hunter voice",null,false,.35f);dialogue.voices[n].volume=.5f;
                // Inspector-friendly editable equipment and shoulder strap.
                var strap=IntroBox("Field equipment strap",npc.transform,new Vector3(.15f,1.04f,.08f),new Vector3(.037f,.44f,.024f),iron);strap.transform.localRotation=Quaternion.Euler(0,0,-21);
                PrefabUtility.SaveAsPrefabAsset(npc,Root+"/Prefabs/"+models[n]+".prefab");
            }
            // Save vehicle before assigning scene-only waypoint references.
            PrefabUtility.SaveAsPrefabAsset(vehicle,Root+"/Prefabs/HunterVehicle.prefab");drive.waypoints=route;drive.ResetDrive();
            CreateIntroRoad(world,drive,iron);
            drive.Sample(c.gateDistance,out var gatePosition,out var gateRotation);
            var gate=IntroNode("Old iron entrance",world,gatePosition);gate.rotation=gateRotation;intro.gateLeaves=new Transform[2];
            var concrete=IntroMaterial("gate concrete",new Color(.28f,.30f,.26f),"Damp stone");
            for(int n=0;n<2;n++)
            {
                float sign=n==0?-1:1;
                IntroBox("Concrete gatepost",gate,new Vector3(sign*3.3f,1.7f,0),new Vector3(.55f,3.4f,.55f),concrete);
                var leaf=IntroNode("Hinged gate leaf "+n,gate,new Vector3(sign*3,0,0));intro.gateLeaves[n]=leaf;
                for(int row=0;row<3;row++)IntroBox("Gate cross rail",leaf,new Vector3(-sign*1.5f,.35f+row*1.10f,0),new Vector3(3,.065f,.075f),iron);
                for(int bar=0;bar<=12;bar++)IntroBox("Gate upright",leaf,new Vector3(-sign*bar*.25f,1.48f,0),new Vector3(.035f,2.65f,.035f),iron);
                IntroBox("Heavy latch",leaf,new Vector3(-sign*2.85f,1.30f,-.03f),new Vector3(.30f,.09f,.08f),iron);
                for(int bolt=0;bolt<4;bolt++)IntroBox("Gate hinge bolt",leaf,new Vector3(0,.6f+bolt*.55f,-.05f),new Vector3(.085f,.06f,.04f),iron);
                leaf.localRotation=Quaternion.Euler(0,sign*c.gateOpenAngle,0);
                // The retained fence FBX is ten metres wide. Unscaled copies
                // overlap the entrance even when the hinged leaves are open.
                for(int fence=0;fence<5;fence++)
                {var section=ForestArt.Model("forest_fence_01",new Vector3(sign*(5.4f+fence*4.1f),0,0),Quaternion.identity,gate);section.transform.localScale=new Vector3(.41f,1,1);}
            }
            var signBoard=IntroBox("Faded warning plate",gate,new Vector3(-3.3f,2.4f,-.31f),new Vector3(1.05f,.53f,.035f),iron);
            var lettering=IntroNode("Faded sign lettering",signBoard.transform,new Vector3(0,0,-.58f)).gameObject.AddComponent<TextMeshPro>();lettering.font=font;lettering.text="PRIVATE\nNO ENTRY";lettering.fontSize=1.4f;lettering.alignment=TextAlignmentOptions.Center;lettering.color=new Color(.50f,.48f,.36f);lettering.rectTransform.sizeDelta=new Vector2(.9f,.45f);lettering.transform.localScale=new Vector3(1/1.05f,1/.53f,1/.035f);
            PrefabUtility.SaveAsPrefabAsset(gate.gameObject,Root+"/Prefabs/ForestArrivalGate.prefab");
            var audio=root.AddComponent<IntroAudioDirector>();intro.audioDirector=audio;
            IntroAudioSetup.Wire(audio);
            CreateIntroUI(intro,game.hud.font);
            intro.overlay.SetActive(false);world.gameObject.SetActive(false);EditorUtility.SetDirty(game);
        }
        static void CreateIntroRoad(Transform world,IntroVehicleController drive,Material iron)
        {
            var groundMaterial=IntroMaterial("forest soil",new Color(.68f,.72f,.63f),"Forest floor");groundMaterial.mainTextureScale=new Vector2(40,150);
            IntroBox("Opening forest floor",world,new Vector3(800,-.07f,150),new Vector3(120,.10f,350),groundMaterial);
            var mesh=new Mesh{name="Opening winding dirt road"};var vertices=new Vector3[162];var uv=new Vector2[162];var triangles=new int[80*6];
            for(int n=0;n<=80;n++)
            {
                drive.Sample(n*4,out var p,out var q);var right=q*Vector3.right;
                vertices[n*2]=p-right*3+Vector3.up*.018f;vertices[n*2+1]=p+right*3+Vector3.up*.018f;
                uv[n*2]=new Vector2(0,n*.4f);uv[n*2+1]=new Vector2(1,n*.4f);
                if(n<80){int t=n*6,v=n*2;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;}
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Data/IntroRoad.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var road=IntroNode("Winding dirt road",world,Vector3.zero).gameObject;road.AddComponent<MeshFilter>().sharedMesh=mesh;road.AddComponent<MeshRenderer>().sharedMaterial=IntroMaterial("dirt road",new Color(.68f,.61f,.49f),"Service trail soil");
            var rutMaterial=IntroMaterial("wheel rut",new Color(.12f,.11f,.085f),"Service trail soil");
            var forest=IntroNode("Roadside forest",world,Vector3.zero);var rng=new System.Random(1407);
            for(int n=0;n<80;n++)
            {
                float distance=n*4;drive.Sample(distance,out var p,out var q);
                for(int side=0;side<2;side++)
                {
                    float sign=side==0?-1:1;
                    for(int row=0;row<2;row++)
                    {
                        float spread=5.5f+row*10+(float)rng.NextDouble()*6;var location=p+q*Vector3.right*spread*sign;
                        ForestArt.Model(n%3==0?"blackpine_02":"blackpine_01",location,Quaternion.Euler(0,(float)rng.NextDouble()*360,0),forest,.8f+(float)rng.NextDouble()*.4f);
                    }
                    if(n%3==0)ForestArt.Model("fern_01",p+q*Vector3.right*(4+(float)rng.NextDouble())*sign,Quaternion.Euler(0,n*67,0),forest,1.2f);
                    if(n%4==0)ForestArt.Model("moss_rock_01",p+q*Vector3.right*4.2f*sign,Quaternion.Euler(0,n*49,0),forest,.6f);
                }
                // Small inset paired tracks and gravel stay safely outside the vehicle envelope.
                if(n%2==0)for(int side=0;side<2;side++)
                {var rut=IntroBox("Wheel rut",world,p+q*Vector3.right*(side==0?-.70f:.70f)+Vector3.up*.023f,new Vector3(.28f,.006f,8.1f),rutMaterial);rut.transform.rotation=q;}
            }
        }
        static void CreateIntroUI(ForestIntroDirector intro,TMP_FontAsset typeface)
        {
            var canvas=new GameObject("Opening subtitles and fade",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(intro.transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=80;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            intro.overlay=canvas;
            GameObject Panel(string name,Vector2 min,Vector2 max)
            {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(canvas.transform,false);var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return go;}
            var fade=Panel("Full black transition",Vector2.zero,Vector2.one);intro.fade=fade.AddComponent<Image>();intro.fade.raycastTarget=false;intro.fade.color=Color.black;
            var panel=Panel("Subtitle safe area",new Vector2(.15f,.075f),new Vector2(.85f,.205f));panel.AddComponent<Image>().color=new Color(.012f,.019f,.019f,.78f);intro.dialogue.subtitlePanel=panel;
            TMP_Text Label(GameObject parent,string name,float size)
            {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent.transform,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(20,8);r.offsetMax=new Vector2(-20,-8);var text=go.AddComponent<TextMeshProUGUI>();text.font=typeface;text.fontSize=size;text.color=new Color(.91f,.89f,.80f);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
            intro.dialogue.subtitle=Label(panel,"Speaker and line",28);
            intro.controls=Label(Panel("Steering hint",new Vector2(.16f,.24f),new Vector2(.84f,.29f)),"Steering controls",23);
            intro.skip=Label(Panel("Hold to skip",new Vector2(.30f,.015f),new Vector2(.70f,.06f)),"Skip hint",19);
        }
    }
}
