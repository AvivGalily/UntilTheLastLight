using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        public const string Root = "Assets/_Game/Generated";
        static Material timber, ground, bark, leaves, metal, paper, amber, stone, pale;
        static GameTuning tuning;
        static InputActionAsset inputs;
        static TMP_FontAsset font;
        static readonly List<WorldItem> items = new List<WorldItem>();
        static readonly List<SafeLight> lights = new List<SafeLight>();
        static readonly List<SafeLight> circuit = new List<SafeLight>();

        [MenuItem("Last Light/1 Generate mechanics test")]
        public static void GenerateMechanics() { Setup(); CreateWorld(true); AssetDatabase.SaveAssets(); Debug.Log("M0 mechanics scene generated."); }
        [MenuItem("Last Light/2 Generate playable prototype")]
        public static void GeneratePrototype()
        {
            Setup(); CreateWorld(true); CreateWorld(false);
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(Root+"/Scenes/ForestGame.unity",true)};
            AssetDatabase.SaveAssets(); ProjectValidator.ValidateAll(); Debug.Log("M2 prototype generated.");
        }
        static void Setup()
        {
            foreach(string dir in new[]{"Scenes","Materials","Data","Prefabs","Navigation","Audio","Fonts","Resources"}) Directory.CreateDirectory(Root+"/"+dir);
            Directory.CreateDirectory("Docs");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            string[] layerNames={"WorldSolid","Player","Monster","Interactable","LightBlocker","Trigger","Decor"};
            var renderingNames=tags.FindProperty("m_RenderingLayers");if(renderingNames.arraySize<2)renderingNames.arraySize=2;renderingNames.GetArrayElementAtIndex(1).stringValue="Player body shadow";
            for(int n=0;n<layerNames.Length;n++) tags.FindProperty("layers").GetArrayElementAtIndex(8+n).stringValue=layerNames[n];
            tags.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.companyName="Blackpine Studio";PlayerSettings.productName="Until the Last Light";
            PlayerSettings.bundleVersion="0.13.0";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            EditorSettings.serializationMode=SerializationMode.ForceText;
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var active=settings.FindProperty("activeInputHandler"); if(active!=null){active.intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();}
            var renderer=Asset<UniversalRendererData>(Root+"/Data/Renderer.asset");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Data/UniversalPipeline.asset");
            if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Root+"/Data/UniversalPipeline.asset");}
            pipeline.supportsHDR=true;pipeline.msaaSampleCount=2;pipeline.shadowDistance=40;
            pipeline.supportsCameraDepthTexture=true;
            pipeline.mainLightShadowmapResolution=4096;pipeline.additionalLightsShadowmapResolution=2048;pipeline.shadowCascadeCount=2;pipeline.cascade2Split=.18f;
            var pipelineSettings=new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_MainLightRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
            pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue=true;
            pipelineSettings.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
            pipelineSettings.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
            pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=true;
            pipelineSettings.FindProperty("m_SupportsLightLayers").boolValue=true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();pipeline.maxAdditionalLightsCount=8;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            QualitySettings.vSyncCount=1;
            tuning=Asset<GameTuning>(Root+"/Data/BalancedPrototype.asset");
            tuning.monster.radius=.7f;tuning.monster.height=1.95f;tuning.monster.pointBlankRange=2.8f;
            tuning.monster.hiddenMinSeconds=10;tuning.monster.hiddenMaxSeconds=15;tuning.monster.vanishMinDistance=14;tuning.monster.fogRetreatDistance=32;
            tuning.monster.encounterMinSeconds=32;tuning.monster.encounterMaxSeconds=52;tuning.monster.encounterCooldown=14;tuning.monster.huntSeconds=14;
            EditorUtility.SetDirty(tuning);CreateInput();font=CreateFont();
            timber=Material("Weathered timber",new Color(.21f,.17f,.13f)); ground=Material("Forest floor",new Color(.09f,.12f,.115f));
            bark=Material("Tree bark",new Color(.16f,.17f,.15f));leaves=Material("Needles",new Color(.035f,.085f,.073f));
            metal=Material("Oxidized steel",new Color(.21f,.27f,.27f),.7f);paper=Material("Ranger paper",new Color(.72f,.64f,.43f));
            stone=Material("Damp stone",new Color(.18f,.23f,.22f));pale=Material("Walker bark",new Color(.42f,.44f,.40f));
            amber=Material("Shelter glass",new Color(.85f,.48f,.1f));amber.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;amber.EnableKeyword("_EMISSION");amber.SetColor("_EmissionColor",new Color(1,.43f,.09f)*2);EditorUtility.SetDirty(amber);
            ForestArt.Prepare();CombatMaterials();
            AssetDatabase.SaveAssets();
        }
        static T Asset<T>(string path) where T:ScriptableObject
        { var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset; }
        static Material Material(string name,Color color,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.2f);m.enableInstancing=true;return m;
        }
        static TMP_FontAsset CreateFont()
        {
            string path=Root+"/Fonts/FieldSans.asset";
            var result=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if(!source)
            {
                string package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly).resolvedPath;
                AssetDatabase.ImportPackage(package+"/Package Resources/TMP Essential Resources.unitypackage",false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            }
            if(!source)throw new Exception("TextMeshPro essential font import failed.");
            if(!result){result=TMP_FontAsset.CreateFontAsset(source,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            result.name="Field Sans";AssetDatabase.CreateAsset(result,path);}
            result.atlasPopulationMode=AtlasPopulationMode.Dynamic;result.ClearFontAssetData();
            result.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?'-/()%[]·…+°=><_ —’");
            string hebrewPath=Root+"/Fonts/FieldHebrew.asset";
            var hebrew=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(hebrewPath);
            if(hebrew&&(!hebrew.atlasTexture||!hebrew.material)){AssetDatabase.DeleteAsset(hebrewPath);hebrew=null;}
            if(!hebrew){hebrew=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/Fonts/NotoSansHebrew-Regular.ttf"),64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);AssetDatabase.CreateAsset(hebrew,hebrewPath);}
            hebrew.atlasPopulationMode=AtlasPopulationMode.Dynamic;hebrew.ClearFontAssetData();
            if(!hebrew.TryAddCharacters("אבגדהוזחטיכלמנסעפצקרשתךםןףץ־",out string missing))throw new Exception("Missing Hebrew glyphs: "+missing);
            result.atlasPopulationMode=AtlasPopulationMode.Static;hebrew.atlasPopulationMode=AtlasPopulationMode.Static;
            result.fallbackFontAssetTable=new List<TMP_FontAsset>{hebrew};
            foreach(var f in new[]{result,hebrew}){if(!AssetDatabase.Contains(f.material))AssetDatabase.AddObjectToAsset(f.material,f);foreach(var tex in f.atlasTextures)if(tex&&!AssetDatabase.Contains(tex))AssetDatabase.AddObjectToAsset(tex,f);EditorUtility.SetDirty(f);}
            EditorUtility.SetDirty(result);return result;
        }
        static void CreateInput()
        {
            string path=Root+"/Data/Player.inputactions";
            var asset=ScriptableObject.CreateInstance<InputActionAsset>();var map=asset.AddActionMap("Gameplay");
            var move=map.AddAction("Move",InputActionType.Value);move.expectedControlType="Vector2";
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            map.AddAction("Look",InputActionType.Value,"<Mouse>/delta").expectedControlType="Vector2";
            string[,] bindings={{"Sprint","<Keyboard>/leftShift"},{"Crouch","<Keyboard>/leftCtrl"},{"Interact","<Keyboard>/e"},{"Flashlight","<Keyboard>/f"},{"Boost","<Mouse>/rightButton"},{"Reload","<Keyboard>/t"},{"Journal","<Keyboard>/j"},{"Map","<Keyboard>/m"}};
            for(int i=0;i<bindings.GetLength(0);i++)map.AddAction(bindings[i,0],InputActionType.Button,bindings[i,1]);
            map.AddAction("Attack",InputActionType.Button,"<Mouse>/leftButton");
            map.AddAction("EquipAxe",InputActionType.Button,"<Keyboard>/1").AddBinding("<Keyboard>/numpad1");
            map.AddAction("EquipShotgun",InputActionType.Button,"<Keyboard>/2").AddBinding("<Keyboard>/numpad2");
            map.AddAction("BatterySwap",InputActionType.Button,"<Keyboard>/r").AddBinding("<Keyboard>/b");
            map.AddAction("CycleWeapon",InputActionType.Value,"<Mouse>/scroll/y").expectedControlType="Axis";
            var ui=asset.AddActionMap("UI");ui.AddAction("Back",InputActionType.Button,"<Keyboard>/escape");
            ui.AddAction("Inventory",InputActionType.Button,"<Keyboard>/tab");
            ui.AddAction("Fullscreen",InputActionType.Button,"<Keyboard>/f11");
            File.WriteAllText(path,asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);AssetDatabase.ImportAsset(path);inputs=AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        static GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
            go.layer=LayerMask.NameToLayer(collision?"WorldSolid":"Decor");go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null,bool collision=true)=>Primitive(name,PrimitiveType.Cube,pos,size,mat,parent,collision);
        static void CreateWorld(bool mechanics)
        {
            items.Clear();lights.Clear();circuit.Clear();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.075f,.10f,.13f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.105f,.127f,.132f);RenderSettings.fogDensity=ForestAtmosphere.BaseDensity;
            var moon=new GameObject("Moon · decorative, never protection",typeof(Light));var moonLight=moon.GetComponent<Light>();moonLight.type=LightType.Directional;moonLight.color=new Color(.43f,.60f,.78f);moonLight.intensity=.55f;moonLight.shadows=LightShadows.Soft;moon.transform.rotation=Quaternion.Euler(48,-32,0);
            moonLight.shadowBias=.015f;moonLight.shadowNormalBias=.025f;
            float size=mechanics?70:ForestLayout.Size;
            Box("Ground",new Vector3(0,-.5f,0),new Vector3(size,1,size),ground);
            for(int i=0;i<4;i++) {var wall=Box("Forest boundary",new Vector3(0,2,(i<2?-1:1)*(size/2)),new Vector3(size,5,2),bark);if(i%2==1){wall.transform.position=new Vector3((i<2?-1:1)*size/2,2,0);wall.transform.rotation=Quaternion.Euler(0,90,0);}}
            Vector3 start=mechanics?new Vector3(0,0,-25):ForestLayout.Cabin;
            if(mechanics)
            {
                Box("Opaque wall",new Vector3(-6,2,0),new Vector3(2,4,16),metal);
                Shelter("mechanics_shelter",start+Vector3.up*2,new Vector3(10,4,9),true);
                Box("Test marker",new Vector3(0,.02f,-5),new Vector3(12,.02f,.18f),paper,null,false);
            }
            else { Forest(start); ForestArt.Lighting(); }
            var player=CreatePlayer(start);var monster=CreateMonster();
            var hud=CreateHud();new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var session=new GameObject("Game · ordered simulation",typeof(GameSession)).GetComponent<GameSession>();
            session.tuning=tuning;session.inputTemplate=inputs;session.player=player;session.monster=monster;session.hud=hud;
            session.flashlightVisual=player.view.GetComponentInChildren<Light>();session.shelters=lights.ToArray();session.circuitLights=circuit.ToArray();session.items=items.ToArray();
            session.strings=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/English.json");session.hebrewStrings=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/Hebrew.json");session.mechanicsTest=mechanics;session.creature=monster.GetComponent<MonsterPresentation>();session.horrorAudio=CreateHorrorAudio(monster);
            if(!mechanics){session.ambience=CreateForestAmbience();session.atmosphere=CreateForestAtmosphere(player.view);}
            WireCombat(session,mechanics);
            if(!mechanics)WireLivingForest(session);
            WireNorton(session,mechanics);session.items=items.ToArray();ConfigurePlayerShadowLights(session);
            PrefabUtility.SaveAsPrefabAsset(player.gameObject,Root+"/Prefabs/Player.prefab");
            var spawn=new GameObject("checkpoint_ranger_cabin").transform;spawn.position=start;session.cabinSpawn=spawn;
            var points=new List<Transform>();
            foreach(var p in new[]{start+new Vector3(0,0,40),start+new Vector3(38,0,8),start+new Vector3(-8,0,40)}) {var t=new GameObject("monster_spawn").transform;t.position=p;points.Add(t);}
            session.monsterSpawns=points.ToArray();
            // Spawn candidate to the side of the player's initial view; within the required 35–50m.
            if(!mechanics){points[1].position=start+new Vector3(40,0,5);session.gateLeaf=GameObject.Find("Sliding gate leaf").transform;}
            var navigation=new GameObject("Navigation",typeof(NavMeshSurface)).GetComponent<NavMeshSurface>();
            navigation.collectObjects=CollectObjects.All;navigation.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            navigation.layerMask=LayerMask.GetMask("WorldSolid","LightBlocker");
            var navSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            var agent=navSettings.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue=tuning.monster.radius;
            agent.FindPropertyRelative("agentHeight").floatValue=tuning.monster.height;
            agent.FindPropertyRelative("agentClimb").floatValue=.3f;navSettings.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();navigation.BuildNavMesh();
            string sceneName=mechanics?"MechanicsTest":"ForestGame";
            string navPath=Root+"/Navigation/"+sceneName+".asset";
            var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(old){EditorUtility.CopySerialized(navigation.navMeshData,old);navigation.RemoveData();navigation.navMeshData=old;navigation.AddData();}
            else AssetDatabase.CreateAsset(navigation.navMeshData,navPath);
            if(!mechanics)PlaceWolvesOnNavigation(session);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/"+sceneName+".unity");
        }
        static PlayerMotor CreatePlayer(Vector3 position)
        {
            var root=new GameObject("Player",typeof(CharacterController),typeof(PlayerMotor));root.layer=LayerMask.NameToLayer("Player");root.transform.position=position;
            var motor=root.GetComponent<PlayerMotor>();motor.controller=root.GetComponent<CharacterController>();motor.controller.height=1.9f;motor.controller.radius=.3f;motor.controller.center=Vector3.up*.95f;motor.controller.stepOffset=.3f;
            var camera=new GameObject("Eyes",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.transform.SetParent(root.transform,false);camera.transform.localPosition=Vector3.up*1.7f;
            motor.view=camera.GetComponent<Camera>();motor.view.nearClipPlane=.06f;motor.view.farClipPlane=200;motor.view.fieldOfView=80;motor.view.clearFlags=CameraClearFlags.SolidColor;motor.view.backgroundColor=RenderSettings.fogColor;
            var data=camera.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.renderShadows=true;
            motor.view.clearFlags=CameraClearFlags.Skybox;
            var source=new GameObject("Flashlight · model driven",typeof(Light));source.transform.SetParent(camera.transform,false);
            var beam=source.GetComponent<Light>();beam.type=LightType.Spot;beam.range=18;beam.spotAngle=48;beam.innerSpotAngle=36;beam.intensity=7;beam.color=new Color(.87f,.91f,.81f);beam.shadows=LightShadows.Soft;beam.shadowBias=.02f;beam.shadowNormalBias=.02f;
            beam.intensity=3;beam.shadowBias=.004f;beam.shadowNormalBias=.003f;beam.shadowResolution=LightShadowResolution.High;
            ShadowTier(beam,UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh);
            var hand=new GameObject("Hand equipment").transform;hand.SetParent(camera.transform,false);motor.hand=hand;
            var flashlight=Primitive("Flashlight body",PrimitiveType.Cylinder,new Vector3(.24f,-.24f,.39f),new Vector3(.065f,.14f,.065f),metal,hand,false);flashlight.transform.localRotation=Quaternion.Euler(90,0,0);
            Primitive("Lens",PrimitiveType.Cylinder,new Vector3(.24f,-.24f,.54f),new Vector3(.077f,.012f,.077f),paper,hand,false).transform.localRotation=Quaternion.Euler(90,0,0);
            Primitive("Gloved hand",PrimitiveType.Capsule,new Vector3(.23f,-.30f,.30f),new Vector3(.09f,.10f,.13f),timber,hand,false).transform.localRotation=Quaternion.Euler(25,0,0);
            flashlight.GetComponent<Renderer>().enabled=false;
            foreach(var r in hand.GetComponentsInChildren<Renderer>())if(r.name=="Lens")r.enabled=false;
            motor.handLight=ForestArt.Model("field_flashlight_01",new Vector3(-.22f,-.18f,.43f),Quaternion.Euler(90,0,0),hand).transform;
            foreach(var r in hand.GetComponentsInChildren<Renderer>())if(r.name=="Gloved hand")r.transform.localPosition=new Vector3(-.23f,-.30f,.30f);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Player.prefab");return motor;
        }
        static MonsterBrain CreateMonster()
        {
            var root=new GameObject("The Walker",typeof(MonsterBrain));root.layer=LayerMask.NameToLayer("Monster");
            var brain=root.GetComponent<MonsterBrain>();var body=new GameObject("Spider-human visual body").transform;body.SetParent(root.transform,false);brain.body=body;
            ConfigureCrawlerImport();
            var model=ForestArt.Model("walker_01",Vector3.zero,Quaternion.identity,body);
            var presentation=root.AddComponent<MonsterPresentation>();presentation.body=body;
            presentation.mouth=model.GetComponentInChildren<SkinnedMeshRenderer>();
            presentation.animator=model.GetComponentInChildren<Animator>();
            presentation.legs=new Transform[8];presentation.shins=new Transform[8];presentation.feet=new Transform[8];
            foreach(var t in model.GetComponentsInChildren<Transform>())for(int i=0;i<8;i++)
            {
                if(t.name=="Limb_"+i+"_Upper")presentation.legs[i]=t;
                if(t.name=="Limb_"+i+"_Lower")presentation.shins[i]=t;
                if(t.name=="Limb_"+i+"_Foot")presentation.feet[i]=t;
            }
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Meshes/walker_01.fbx"))if(asset is AnimationClip clip)
            {if(clip.name=="Crawler_Idle")presentation.idle=clip;if(clip.name=="Crawler_Crawl")presentation.crawl=clip;}
            if(!presentation.HasRig)throw new Exception("Crawler export requires eight articulated limbs, skinned jaw and both skeletal clips.");
            presentation.animator.applyRootMotion=false;presentation.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            presentation.mouth.updateWhenOffscreen=true;
            presentation.blood=root.AddComponent<BloodySurface>();presentation.blood.surfaces=model.GetComponentsInChildren<Renderer>();
            presentation.blood.wound0=new Vector4(0,1.32f,1.03f,.29f);presentation.blood.wound1=new Vector4(.23f,1.17f,.08f,.36f);presentation.blood.amount=.6f;presentation.blood.Refresh();
            foreach(var t in model.GetComponentsInChildren<Transform>()){if(t.name=="Head"){presentation.head=t;presentation.blood.anchor0=t;}else if(t.name=="Thorax")presentation.blood.anchor1=t;}presentation.blood.Refresh();
            brain.breath=root.AddComponent<AudioSource>();brain.breath.spatialBlend=1;brain.breath.loop=true;brain.breath.playOnAwake=true;brain.breath.volume=0;
            brain.breath.rolloffMode=AudioRolloffMode.Custom;brain.breath.SetCustomCurve(AudioSourceCurveType.CustomRolloff,AnimationCurve.Linear(0,1,1,1));brain.breath.maxDistance=70;
            brain.breath.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/walker_breath.wav");
            brain.warningSource=root.AddComponent<AudioSource>();brain.warningSource.spatialBlend=1;brain.warningSource.minDistance=2;brain.warningSource.maxDistance=15;brain.warningSource.volume=.85f;
            brain.warningClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/attack_warning.wav");
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Walker.prefab");return brain;
        }
        static void ConfigureCrawlerImport()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Meshes/walker_01.fbx");
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.importBlendShapes=true;importer.optimizeGameObjects=false;
            importer.importNormals=ModelImporterNormals.Calculate;importer.importBlendShapeNormals=ModelImporterNormals.Calculate;
            importer.normalCalculationMode=ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            importer.normalSmoothingSource=ModelImporterNormalSmoothingSource.FromAngle;importer.normalSmoothingAngle=180;
            importer.skinWeights=ModelImporterSkinWeights.Standard;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)
            {
                clip.name=clip.takeName.Contains("Idle")?"Crawler_Idle":"Crawler_Crawl";
                clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
        }
        static GameHud CreateHud()
        {
            var root=new GameObject("Field interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameHud));
            var hud=root.GetComponent<GameHud>();hud.font=font;hud.Construct();PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Interface.prefab");return hud;
        }
        static SafeLight Shelter(string id,Vector3 position,Vector3 box,bool powered)
        {
            var root=new GameObject(id,typeof(SafeLight));root.transform.position=position;var shelter=root.GetComponent<SafeLight>();shelter.persistentId=id;shelter.boxSize=box;
            var bulb=Primitive("Amber shelter lamp",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.01f,amber,root.transform,false);
            var light=bulb.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.72f,.43f);light.intensity=4.5f;light.range=Mathf.Max(box.x,box.z);light.shadows=LightShadows.Soft;light.shadowBias=.015f;
            ShadowTier(light,UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow);
            shelter.lamp=light;
            var fixture=ForestArt.Model("shelter_lantern_01",Vector3.zero,Quaternion.identity,root.transform);
            shelter.fixtures=fixture.GetComponentsInChildren<Renderer>();
            shelter.SetPowered(powered);lights.Add(shelter);return shelter;
        }
        static void ShadowTier(Light light,int tier)
        {
            var data=new SerializedObject(light.GetUniversalAdditionalLightData());
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=tier;data.ApplyModifiedPropertiesWithoutUndo();
        }
        static WorldItem Item(string id,ItemKind kind,Vector3 position,string label,Material material,int count=1,Vector3? size=null)
        {
            var go=Box(label,position,size??new Vector3(.35f,.22f,.3f),material);go.layer=LayerMask.NameToLayer("Interactable");
            var item=go.AddComponent<WorldItem>();item.persistentId=id;item.kind=kind;item.label=label;item.remaining=count;items.Add(item);
            if(kind==ItemKind.PowerPanel)
            {
                go.GetComponent<Renderer>().enabled=false;
                var art=ForestArt.Model("field_power_panel_01",position,Quaternion.identity);art.transform.SetParent(go.transform,true);
            }
            return item;
        }
        static void Cabin(string id,Vector3 p,bool powered)
        {
            var root=new GameObject(id).transform;root.position=p;
            Box("Foundation",new Vector3(0,-.1f,0),new Vector3(9,.2f,8),stone,root);
            for(int i=0;i<12;i++)Box("Floorboard",new Vector3(-4.1f+i*.74f,.035f,0),new Vector3(.70f,.06f,7.8f),timber,root);
            Box("Back wall",new Vector3(0,1.6f,-4),new Vector3(9,3.2f,.22f),timber,root);
            Box("Left wall",new Vector3(-4.5f,1.6f,0),new Vector3(.22f,3.2f,8),timber,root);
            Box("Right wall",new Vector3(4.5f,1.6f,0),new Vector3(.22f,3.2f,8),timber,root);
            Box("Door wall left",new Vector3(-3.15f,1.6f,4),new Vector3(2.7f,3.2f,.22f),timber,root);
            Box("Door wall right",new Vector3(3.15f,1.6f,4),new Vector3(2.7f,3.2f,.22f),timber,root);
            Box("Lintel",new Vector3(0,3,4),new Vector3(3.6f,.4f,.22f),timber,root);
            var roof=Box("Pitched roof A",new Vector3(-2.5f,3.9f,0),new Vector3(5.5f,.20f,9),bark,root);roof.transform.localRotation=Quaternion.Euler(0,0,17);
            roof=Box("Pitched roof B",new Vector3(2.5f,3.9f,0),new Vector3(5.5f,.20f,9),bark,root);roof.transform.localRotation=Quaternion.Euler(0,0,-17);
            Box("Desk",new Vector3(-2.7f,.95f,1),new Vector3(2.2f,.2f,1.1f),timber,root);
            foreach(float x in new[]{-3.55f,-1.85f})Box("Desk leg",new Vector3(x,.45f,1),new Vector3(.12f,.9f,.7f),bark,root);
            foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
            ForestArt.Model(id=="Ranger cabin"?"ranger_cabin_01":"storehouse_01",Vector3.zero,Quaternion.identity,root);
            Shelter(id+"_shelter",p+new Vector3(0,2.6f,0),new Vector3(9,5.4f,9),powered);
            CabinSign(id=="Ranger cabin"?"sign_cabin":"sign_store",root,new Vector3(0,3.15f,4.23f),3.0f);
        }
        static void Sign(string words,Vector3 pos,float size)
        {
            Box("Signboard",pos,new Vector3(size, .8f,.08f),timber);
            var go=new GameObject("Sign lettering",typeof(TextMeshPro));go.transform.position=pos+new Vector3(0,0,.05f);go.transform.rotation=Quaternion.Euler(0,180,0);
            var text=go.GetComponent<TextMeshPro>();text.font=font;text.text=words;var localized=go.AddComponent<LocalizedSign>();localized.key=words;text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(size-.1f,.75f);text.color=new Color(.77f,.70f,.50f);
        }
        static void Forest(Vector3 start) { ForestExpanded(start); }
        static void Trail(Vector3 a,Vector3 b)
        {
            ForestArt.Trail(a,b);
        }
        static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b){Vector3 d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
        static void Tree(Vector3 p,float height,float yaw,bool hero=false)
        {
            if(!hero)
            {
                string[] ids={"blackpine_01","blackpine_02","blackpine_02","snag_01"};
                int variant=Mathf.FloorToInt(yaw)%10;
                string id=ids[variant<4?0:variant<8?1:variant==8?2:3];
                var tree=ForestArt.Model(id,p,Quaternion.Euler(0,yaw,0),null,height/10);
                tree.layer=LayerMask.NameToLayer("WorldSolid");
                var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.33f;trunk.height=8;trunk.center=Vector3.up*4;
                return;
            }
            var treeHero=ForestArt.Model(p.x<20?"pine_hero_01":"dead_hero_01",p,Quaternion.Euler(0,yaw,0),null,height/10);
            treeHero.name="Hero tree · visual deformation only";treeHero.layer=LayerMask.NameToLayer("WorldSolid");
            var collider=treeHero.AddComponent<CapsuleCollider>();collider.radius=.33f;collider.height=8;collider.center=Vector3.up*4;
            var component=treeHero.AddComponent<HeroTree>();component.crowns=treeHero.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(var crown in component.crowns){var bounds=crown.sharedMesh.bounds;bounds.Expand(2);crown.localBounds=bounds;}
        }
    }
}
