using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    // Presentation lives beside the existing, stable gameplay colliders and item IDs.
    public static class ForestArt
    {
        const string MeshRoot = "Assets/_Game/Art/Meshes/";
        const string TextureRoot = "Assets/_Game/Art/Textures/";
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();

        public static void Prepare()
        {
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[]{TextureRoot.TrimEnd('/')}))
            {
                string assetPath=AssetDatabase.GUIDToAssetPath(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(assetPath);
                bool normal=assetPath.Contains("_Normal"), mask=assetPath.Contains("_Mask");
                bool foliage=assetPath.EndsWith("Needles_BaseColor.png")||assetPath.EndsWith("PH_FirTwig_BaseColor.png")||assetPath.EndsWith("Broadleaf_BaseColor.png")||assetPath.EndsWith("HumanHair_BaseColor.png")||assetPath.EndsWith("HumanBrows_BaseColor.png")||assetPath.Contains("IntroHair");
                bool wolf=assetPath.Contains("WolfSkin_")||assetPath.Contains("WolfBone_");
                bool oral=assetPath.Contains("CrawlerTeeth_")||assetPath.Contains("CrawlerGums_")||assetPath.Contains("CrawlerThroat_");
                bool skin=assetPath.Contains("CrawlerSkin_")||assetPath.Contains("CrawlerMouth_")||assetPath.Contains("HumanSkin")||wolf||oral;
                int size=assetPath.EndsWith("_Anatomy.png")?4096:2048;
                bool changed=importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)
                    || importer.sRGBTexture== (normal||mask) || importer.wrapMode!=TextureWrapMode.Repeat
                    || importer.mipMapsPreserveCoverage!=foliage || importer.maxTextureSize!=size
                    || (skin&&(importer.anisoLevel!=8||importer.textureCompression!=TextureImporterCompression.CompressedHQ))
                    || ((wolf||oral)&&importer.npotScale!=TextureImporterNPOTScale.None);
                if(changed)
                {
                    importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                    importer.sRGBTexture=!(normal||mask);importer.wrapMode=TextureWrapMode.Repeat;
                    importer.maxTextureSize=size;importer.mipmapEnabled=true;importer.anisoLevel=skin?8:4;
                    if(skin)importer.textureCompression=TextureImporterCompression.CompressedHQ;
                    if(wolf||oral)importer.npotScale=TextureImporterNPOTScale.None;
                    importer.mipMapsPreserveCoverage=foliage;importer.alphaTestReferenceValue=.42f;
                    importer.SaveAndReimport();
                }
            }
            palette.Clear();
            Pbr("Timber","Weathered timber",new Vector2(6,6));Pbr("Bark","Tree bark",new Vector2(3,3));Pbr("Needles","Needles");
            var signWood=Pbr("CabinSignWood","Weathered cabin sign wood");signWood.SetFloat("_BumpScale",.55f);EditorUtility.SetDirty(signWood);
            Plain("CabinSignIron","Cabin sign iron frame",new Color(.07f,.065f,.05f),.16f,.65f);
            Pbr("Broadleaf","Birch leaves");
            foreach(string key in new[]{"Needles","Broadleaf"})
            {
                var foliage=palette[key];foliage.SetFloat("_AlphaClip",1);foliage.SetFloat("_Cutoff",.42f);
                foliage.EnableKeyword("_ALPHATEST_ON");foliage.SetOverrideTag("RenderType","TransparentCutout");foliage.renderQueue=2450;
            }
            Plain("Grass","Forest grass",new Color(.11f,.19f,.10f),.08f,0);
            foreach(string key in new[]{"PH_MossRock","PH_FallenWood","PH_Stump","PH_FirBark","PH_FirTrunkB","PH_FirTrunkC","PH_FirTwig"})
            {
                var scan=Pbr(key,key);scan.SetFloat("_BumpScale",.65f);
                if(key=="PH_FirTwig")
                {
                    scan.SetFloat("_AlphaClip",1);scan.SetFloat("_Cutoff",.38f);scan.SetFloat("_Cull",0);scan.EnableKeyword("_ALPHATEST_ON");scan.SetOverrideTag("RenderType","TransparentCutout");scan.renderQueue=2450;
                }
                EditorUtility.SetDirty(scan);
            }
            Plain("CrowFeathers","Blue black raven feathers",new Color(.018f,.023f,.030f),.08f,0);
            Plain("CrowBeak","Worn raven beak",new Color(.032f,.035f,.036f),.22f,0);
            Plain("CrowEye","Raven eyes",new Color(.008f,.009f,.011f),.72f,0);
            var silk=Plain("WebSilk","Ancient spider silk",new Color(.48f,.52f,.48f),.28f,0);silk.EnableKeyword("_EMISSION");silk.SetColor("_EmissionColor",new Color(.016f,.020f,.016f));EditorUtility.SetDirty(silk);
            Pbr("ForestFloor","Forest floor",new Vector2(84,84));Pbr("Trail","Service trail soil");
            palette["ForestFloor"].SetFloat("_Smoothness",.35f);palette["Trail"].SetFloat("_Smoothness",.4f);
            Pbr("Steel","Oxidized steel");Pbr("Stone","Damp stone");Pbr("PaleBark","Walker bark");
            Plain("Glass","Night glass",new Color(.09f,.16f,.18f),.72f,.5f);
            Plain("Fabric","Ranger canvas",new Color(.15f,.18f,.14f),.05f,0);
            Plain("Skin","Creature skin",new Color(.23f,.25f,.24f),.16f,0);
            var crawlerSkin=Pbr("CrawlerSkin","Crawler living skin");crawlerSkin.shader=Shader.Find("Last Light/Crawler skin");
            crawlerSkin.SetFloat("_BumpScale",.65f);crawlerSkin.SetFloat("_HighlightRolloff",1.4f);
            crawlerSkin.SetTexture("_AnatomyMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+"CrawlerMouth_Anatomy.png"));
            crawlerSkin.SetFloat("_UseSkinAtlas",1);crawlerSkin.SetFloat("_SkinSpecular",.028f);crawlerSkin.SetFloat("_Subsurface",.20f);
            EditorUtility.SetDirty(crawlerSkin);
            var bodySkin=Pbr("CrawlerSkin","Human body skin");bodySkin.shader=crawlerSkin.shader;
            bodySkin.SetFloat("_UseSkinAtlas",0);bodySkin.SetFloat("_BumpScale",.15f);bodySkin.SetFloat("_SkinSpecular",.028f);bodySkin.SetFloat("_Subsurface",.1f);bodySkin.SetFloat("_HighlightRolloff",1.8f);
            bodySkin.SetFloat("_WorldSkin",1);bodySkin.SetFloat("_SkinTileScale",3.2f);
            bodySkin.SetFloat("_IlluminanceRolloff",1.2f);
            bodySkin.SetColor("_BaseColor",new Color(.78f,.82f,.86f));palette["BodySkin"]=bodySkin;EditorUtility.SetDirty(bodySkin);
            // Pbr writes its texture key into the palette; restore the crawler's
            // own atlas material after reusing its epidermis on human skin.
            palette["CrawlerSkin"]=crawlerSkin;
            foreach(string key in new[]{"HumanSkin0","HumanSkin1","HumanOutfit0","HumanOutfit1","HumanOutfit2","HumanOutfit3","HumanBoots","HumanHair","HumanEyes","HumanBrows","WolfSkin"})
            {
                var surface=Pbr(key,key);
                if(key=="HumanHair"||key=="HumanBrows")
                {
                    surface.shader=crawlerSkin.shader;surface.SetFloat("_AlphaClip",1);surface.SetFloat("_Cutoff",.3f);surface.SetFloat("_Cull",0);
                    surface.SetFloat("_WorldSkin",0);surface.SetFloat("_UseSkinAtlas",0);surface.SetFloat("_Subsurface",0);surface.SetFloat("_SkinSpecular",.012f);
                    surface.SetFloat("_IlluminanceRolloff",1.25f);surface.SetFloat("_HighlightRolloff",2);surface.SetFloat("_BumpScale",.2f);
                    surface.EnableKeyword("_ALPHATEST_ON");surface.SetOverrideTag("RenderType","TransparentCutout");surface.renderQueue=2450;
                }
                else
                {
                    surface.shader=crawlerSkin.shader;surface.SetFloat("_WorldSkin",0);surface.SetFloat("_UseSkinAtlas",0);
                    surface.SetFloat("_Subsurface",key.StartsWith("HumanSkin")?.08f:0);surface.SetFloat("_SkinSpecular",.028f);
                    surface.SetFloat("_IlluminanceRolloff",.45f);surface.SetFloat("_HighlightRolloff",1.6f);surface.SetFloat("_BumpScale",key=="WolfSkin"?.48f:.4f);
                }
                EditorUtility.SetDirty(surface);
            }
            var wolfEyes=Pbr("WolfSkin","Textured wolf eyes");wolfEyes.SetFloat("_Smoothness",.55f);palette["WolfEyes"]=wolfEyes;
            wolfEyes.shader=crawlerSkin.shader;wolfEyes.SetFloat("_WorldSkin",0);wolfEyes.SetFloat("_UseSkinAtlas",0);wolfEyes.SetFloat("_Subsurface",0);
            wolfEyes.SetFloat("_SkinSpecular",.025f);wolfEyes.SetFloat("_HighlightRolloff",1.8f);wolfEyes.SetFloat("_IlluminanceRolloff",.9f);EditorUtility.SetDirty(wolfEyes);
            // Pbr also writes its texture key. Keep the coat on its matte wound shader.
            palette["WolfSkin"]=AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.Root+"/Materials/WolfSkin.mat");
            var wolfBone=Pbr("WolfBone","Exposed wolf bone");wolfBone.shader=crawlerSkin.shader;
            wolfBone.SetFloat("_WorldSkin",0);wolfBone.SetFloat("_UseSkinAtlas",0);wolfBone.SetFloat("_Subsurface",0);wolfBone.SetFloat("_SkinSpecular",.025f);wolfBone.SetFloat("_BumpScale",.6f);wolfBone.SetFloat("_HighlightRolloff",1.6f);wolfBone.SetFloat("_IlluminanceRolloff",1.15f);EditorUtility.SetDirty(wolfBone);
            Plain("WolfTissue","Torn wolf tissue",new Color(.20f,.021f,.028f),.42f,0);
            Plain("WolfSocket","Empty wolf orbit",new Color(.008f,.003f,.005f),.05f,0);
            foreach(var key in new[]{"BodyCoatOlive","BodyCoatBrown","BodyCoatBlue","BodyCoatGrey","BodyPants"})
            {
                var clothing=Pbr("BodyFabric",key);palette[key]=clothing;
                clothing.SetColor("_BaseColor",key=="BodyCoatOlive"?new Color(.23f,.30f,.20f):key=="BodyCoatBrown"?new Color(.32f,.23f,.16f):key=="BodyCoatBlue"?new Color(.17f,.24f,.30f):key=="BodyCoatGrey"?new Color(.27f,.29f,.28f):new Color(.18f,.19f,.16f));
                clothing.SetFloat("_BumpScale",.5f);EditorUtility.SetDirty(clothing);
            }
            Pbr("BodyLeather","Worn body leather");
            Plain("BodyHair","Human hair",new Color(.055f,.041f,.031f),.1f,0);
            Plain("BodyCrease","Human closed eye and mouth creases",new Color(.08f,.052f,.044f),.1f,0);
            Plain("BodyLips","Human lips",new Color(.25f,.13f,.13f),.42f,0);
            Plain("BodyNails","Human nails",new Color(.42f,.39f,.36f),.52f,0);
            Plain("BodySole","Boot sole",new Color(.035f,.038f,.035f),.15f,0);
            Plain("BodyMetal","Body zips and buckles",new Color(.23f,.25f,.24f),.6f,.65f);
            Plain("BodyStitch","Clothing stitches",new Color(.28f,.27f,.21f),.2f,0);
            Plain("BodyPaper","Body documents",new Color(.70f,.66f,.54f),.05f,0);
            Plain("BodyInk","Document handwriting",new Color(.055f,.051f,.047f),0,0);
            foreach(string key in new[]{"BodyHair","BodyCrease","BodyLips","BodyNails","BodyStitch","BodySole","BodyCoatOlive","BodyCoatBrown","BodyCoatBlue","BodyCoatGrey","BodyPants","BodyLeather"})
            {
                var surface=palette[key];surface.shader=crawlerSkin.shader;surface.SetFloat("_IlluminanceRolloff",1.2f);surface.SetFloat("_HighlightRolloff",1.8f);
                surface.SetFloat("_WorldSkin",0);surface.SetFloat("_UseSkinAtlas",0);surface.SetFloat("_Subsurface",0);surface.SetFloat("_SkinSpecular",.028f);EditorUtility.SetDirty(surface);
            }
            Pbr("CrawlerGums","Crawler wet gums");Pbr("CrawlerTeeth","Crawler irregular teeth");Pbr("CrawlerThroat","Crawler throat");
            Plain("CrawlerSaliva","Crawler saliva",new Color(.29f,.19f,.17f),.96f,0);
            foreach(string key in new[]{"CrawlerGums","CrawlerTeeth","CrawlerThroat","CrawlerSaliva"})
            {palette[key].shader=crawlerSkin.shader;palette[key].SetFloat("_HighlightRolloff",1.4f);
                palette[key].SetFloat("_WorldSkin",0);palette[key].SetFloat("_UseSkinAtlas",0);palette[key].SetFloat("_Subsurface",0);palette[key].SetFloat("_SkinSpecular",.025f);palette[key].SetFloat("_IlluminanceRolloff",.55f);palette[key].SetFloat("_BumpScale",.55f);EditorUtility.SetDirty(palette[key]);}
            palette["CrawlerThroat"].SetColor("_BaseColor",new Color(.24f,.18f,.20f));EditorUtility.SetDirty(palette["CrawlerThroat"]);
            Plain("Chitin","Creature chitin",new Color(.045f,.055f,.05f),.36f,0);Plain("Mouth","Mouth interior",new Color(.06f,.012f,.014f),.24f,0);Plain("Teeth","Old ivory",new Color(.72f,.64f,.43f),.28f,0);Plain("Coat","Weathered field coat",new Color(.16f,.19f,.18f),.06f,0);
            palette["Amber"]=AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.Root+"/Materials/Shelter glass.mat");
            palette["Mouth"].shader=Shader.Find("Universal Render Pipeline/Unlit");palette["Mouth"].color=new Color(.035f,.008f,.009f);
            palette["Teeth"].color=new Color(.55f,.52f,.44f);
            NortonMaterials();
            PlayerMaterials();
            foreach(string key in new[]{"IntroSkin0","IntroSkin1","IntroOutfit0","IntroOutfit1","IntroOutfit2","IntroEyes0","IntroEyes1","IntroEyes2","IntroHair0","IntroHair1","IntroHair2"})
            {
                var surface=Pbr(key,key);surface.shader=Shader.Find("Universal Render Pipeline/Lit");
                surface.SetColor("_BaseColor",Color.white);surface.SetFloat("_Metallic",0);
                surface.SetFloat("_Smoothness",key.StartsWith("IntroEyes")?.52f:key.StartsWith("IntroSkin")?.24f:.12f);
                surface.SetFloat("_BumpScale",.18f);
                // Living skin has its own clean albedo, without corpse shaders/maps.
                if(key.StartsWith("IntroSkin")){surface.DisableKeyword("_NORMALMAP");surface.DisableKeyword("_METALLICSPECGLOSSMAP");surface.DisableKeyword("_OCCLUSIONMAP");}
                if(key.StartsWith("IntroEyes"))
                {
                    // The CC0 high-poly cornea uses a transparent atlas region.
                    // Opaque rendering covers the sclera/iris with a black shell.
                    surface.SetFloat("_AlphaClip",1);surface.SetFloat("_Cutoff",.42f);surface.EnableKeyword("_ALPHATEST_ON");
                    surface.SetOverrideTag("RenderType","TransparentCutout");surface.renderQueue=2450;
                    surface.DisableKeyword("_METALLICSPECGLOSSMAP");surface.DisableKeyword("_OCCLUSIONMAP");
                }
                if(key.StartsWith("IntroHair"))
                {
                    int identity=key[key.Length-1]-'0';
                    surface.SetColor("_BaseColor",identity==0?new Color(.58f,.60f,.56f):identity==1?new Color(.75f,.60f,.46f):new Color(.24f,.25f,.27f));
                    surface.SetFloat("_AlphaClip",1);surface.SetFloat("_Cutoff",.3f);surface.SetFloat("_Cull",0);surface.EnableKeyword("_ALPHATEST_ON");
                    surface.SetOverrideTag("RenderType","TransparentCutout");surface.renderQueue=2450;
                }
                EditorUtility.SetDirty(surface);
            }
            for(int n=0;n<2;n++)
            {
                string key="ArrivalSkin"+n,path=ProjectSetup.Root+"/Materials/"+key+".mat";
                var skin=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!skin){skin=new Material(palette["HumanSkin"+n]);AssetDatabase.CreateAsset(skin,path);}
                skin.SetTexture("_BaseMap",palette["IntroSkin"+n].GetTexture("_BaseMap"));
                skin.SetColor("_BaseColor",new Color(.80f,.83f,.82f));skin.SetFloat("_CadaverDecay",.25f);
                palette[key]=skin;EditorUtility.SetDirty(skin);
            }
            var originalLeather=palette["BodyLeather"];var originalFabric=palette["BodyFabric"];
            string[] interiorKeys={"IntroDash","IntroDoor","IntroSeat","IntroFloor","IntroWheelLeather"};
            foreach(var key in interiorKeys)
            {
                bool fabric=key=="IntroSeat"||key=="IntroFloor";
                var surface=Pbr(fabric?"BodyFabric":"BodyLeather",key,fabric?new Vector2(4,4):new Vector2(2,2));palette[key]=surface;
                surface.shader=Shader.Find("Last Light/Intro interior");surface.SetFloat("_Metallic",0);surface.SetFloat("_Smoothness",key=="IntroWheelLeather"?.24f:.12f);surface.SetFloat("_BumpScale",.8f);
                surface.SetFloat("_Pattern",fabric?1:0);surface.SetFloat("_GrainScale",fabric?65:140);surface.SetFloat("_GrainDepth",fabric?.09f:.18f);
                surface.SetColor("_BaseColor",key=="IntroSeat"?new Color(.46f,.50f,.41f):key=="IntroDoor"?new Color(.38f,.40f,.34f):new Color(.20f,.22f,.20f));
                EditorUtility.SetDirty(surface);
            }
            palette["BodyLeather"]=originalLeather;palette["BodyFabric"]=originalFabric;
        }
        static void PlayerMaterials()
        {
            string path=ProjectSetup.Root+"/Materials/PlayerSkin.mat";
            var skin=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!skin){skin=new Material(palette["HumanSkin0"]);AssetDatabase.CreateAsset(skin,path);}else skin.CopyPropertiesFromMaterial(palette["HumanSkin0"]);
            skin.SetColor("_BaseColor",new Color(.96f,.87f,.81f));skin.SetFloat("_BumpScale",1.05f);skin.SetFloat("_Smoothness",.65f);
            skin.SetFloat("_Subsurface",.14f);skin.SetFloat("_OcclusionStrength",.35f);skin.SetFloat("_IlluminanceRolloff",.22f);
            skin.SetFloat("_HighlightRolloff",1.5f);skin.SetFloat("_CadaverDecay",0);skin.SetFloat("_CinderDecay",0);
            palette["PlayerSkin"]=skin;EditorUtility.SetDirty(skin);
            var cloth=Pbr("BodyFabric","PlayerSleeve",new Vector2(2,3));palette["PlayerSleeve"]=cloth;
            cloth.SetColor("_BaseColor",new Color(.17f,.23f,.20f));cloth.SetFloat("_BumpScale",.65f);cloth.SetFloat("_Smoothness",.3f);
            EditorUtility.SetDirty(cloth);
        }
        static void NortonMaterials()
        {
            string organPath=ProjectSetup.Root+"/Materials/IntestinalSerosa.mat";var organ=AssetDatabase.LoadAssetAtPath<Material>(organPath);
            if(!organ){organ=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(organ,organPath);}
            organ.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+"IntestinalSerosa_BaseColor.png"));organ.SetColor("_BaseColor",Color.white);organ.SetFloat("_Smoothness",.72f);organ.SetFloat("_Metallic",0);palette["IntestinalSerosa"]=organ;EditorUtility.SetDirty(organ);
            string[] keys={"CadaverSkin","CadaverLimb","CadaverEyes","Viscera","BurnedSteel","BurnedRubber","BurnedGlass","ShotgunSteel","ShotgunWood"};
            string[] sources={"HumanSkin0","CrawlerSkin","HumanEyes","CrawlerGums","Steel","Steel","Steel","Steel","Timber"};
            Color[] colors={new Color(.76f,.80f,.72f),new Color(.60f,.61f,.51f),new Color(.65f,.65f,.58f),new Color(.76f,.42f,.47f),new Color(.10f,.095f,.08f),new Color(.026f,.024f,.021f),new Color(.07f,.09f,.08f),new Color(.20f,.22f,.23f),new Color(.38f,.24f,.12f)};
            for(int i=0;i<keys.Length;i++)
            {
                string path=ProjectSetup.Root+"/Materials/"+keys[i]+".mat";var surface=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!surface){surface=new Material(palette[sources[i]]);AssetDatabase.CreateAsset(surface,path);}else surface.CopyPropertiesFromMaterial(palette[sources[i]]);
                surface.SetColor("_BaseColor",colors[i]);surface.SetFloat("_Smoothness",keys[i]=="Viscera"?.63f:.13f);
                if(keys[i]=="CadaverLimb"){surface.SetFloat("_WorldSkin",0);surface.SetFloat("_UseSkinAtlas",0);surface.SetTextureScale("_BaseMap",new Vector2(2,2));}
                if(keys[i].StartsWith("Cadaver"))surface.SetFloat("_CadaverDecay",keys[i]=="CadaverEyes"?.9f:.8f);
                if(keys[i]=="BurnedRubber")surface.SetFloat("_Metallic",0);
                palette[keys[i]]=surface;EditorUtility.SetDirty(surface);
            }
            string[] headKeys={"CinderSkin","CinderLimb","CinderEyes","CinderHair","CinderGums","CinderTeeth","CinderThroat","CinderBone"};
            string[] headSources={"HumanSkin0","CrawlerSkin","HumanEyes","HumanHair","CrawlerGums","CrawlerTeeth","CrawlerThroat","WolfBone"};
            for(int i=0;i<headKeys.Length;i++)
            {
                string path=ProjectSetup.Root+"/Materials/"+headKeys[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(palette[headSources[i]]);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(palette[headSources[i]]);
                m.shader=palette["CrawlerSkin"].shader;m.SetFloat("_WorldSkin",0);m.SetFloat("_UseSkinAtlas",0);m.SetFloat("_CharAmount",0);m.SetFloat("_BurnAway",0);
                m.SetFloat("_CadaverDecay",i<2?.52f:0);m.SetFloat("_CinderDecay",i<2?1:0);m.SetFloat("_SkinSpecular",.032f);m.SetFloat("_Subsurface",i<2?.10f:0);
                m.SetFloat("_BumpScale",i<2?.8f:.6f);m.SetFloat("_Smoothness",i<2?.72f:i==2?.85f:i==4||i==6?.8f:.5f);
                m.SetFloat("_IlluminanceRolloff",.7f);m.SetFloat("_HighlightRolloff",1.3f);
                m.SetColor("_BaseColor",i<2?new Color(.94f,.82f,.84f):i==2?new Color(.66f,.56f,.49f):i==4?new Color(.48f,.18f,.22f):i==5?new Color(.76f,.70f,.52f):i==7?new Color(.64f,.61f,.5f):Color.white);
                if(i==1)m.SetTextureScale("_BaseMap",new Vector2(2,2));
                palette[headKeys[i]]=m;EditorUtility.SetDirty(m);
            }
        }
        static Material Pbr(string key,string name,Vector2? tile=null)
        {
            var mat=Plain(key,name,Color.white,.25f,key=="Steel"?.7f:0);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+key+"_BaseColor.png");
            if(!texture)throw new InvalidOperationException("Generate Blender art before the scene: "+key);
            mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",tile??Vector2.one);
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+key+"_Normal.png"));
            mat.SetFloat("_BumpScale",.18f);mat.EnableKeyword("_NORMALMAP");
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+key+"_Mask.png");
            mat.SetTexture("_MetallicGlossMap",mask);mat.SetTexture("_OcclusionMap",mask);
            mat.SetFloat("_Smoothness",1);mat.SetFloat("_OcclusionStrength",.55f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(mat);return mat;
        }
        static Material Plain(string key,string name,Color color,float smoothness,float metallic)
        {
            string path=ProjectSetup.Root+"/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;mat.SetFloat("_Smoothness",smoothness);mat.SetFloat("_Metallic",metallic);
            mat.enableInstancing=true;palette[key]=mat;return mat;
        }
        public static GameObject Model(string id,Vector3 position,Quaternion rotation,Transform parent=null,float scale=1)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(MeshRoot+id+".fbx");
            if(!source)throw new InvalidOperationException("Missing Blender export: "+id);
            // Preserve FBX's axis/unit conversion on its imported root transform.
            // A separate placement root applies the game's metres/yaw/scale.
            var instance=new GameObject(id);
            instance.transform.SetParent(parent,false);
            UnityEngine.Object.Instantiate(source,instance.transform,false);
            instance.name=id;instance.transform.localPosition=position;instance.transform.localRotation=rotation;
            instance.transform.localScale=Vector3.one*scale;
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.gameObject.layer=LayerMask.NameToLayer("Decor");
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    string key=mats[i]?mats[i].name:"Bark";
                    int suffix=key.IndexOf('.');if(suffix>=0)key=key.Substring(0,suffix);
                    if(!palette.TryGetValue(key,out var replacement))throw new InvalidOperationException("Unmapped Blender material: "+key);
                    mats[i]=replacement;
                }
                renderer.sharedMaterials=mats;
            }
            // FBX may create its own LODGroup. Rebuild explicit groups to avoid rendering all copies.
            var renderers=instance.GetComponentsInChildren<Renderer>();
            bool hasLods=Array.Exists(renderers,r=>r.name.EndsWith("_LOD0"));
            if(hasLods)
            {
                foreach(var existing in instance.GetComponentsInChildren<LODGroup>())UnityEngine.Object.DestroyImmediate(existing);
                var lod=instance.AddComponent<LODGroup>();var levels=new LOD[3];
                for(int n=0;n<3;n++)levels[n]=new LOD(n==0?.20f:n==1?.08f:.012f,Array.FindAll(renderers,r=>r.name.EndsWith("_LOD"+n)));
                lod.SetLODs(levels);lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();
            }
            return instance;
        }
        public static void Lighting()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProjectSetup.Root+"/Data/ForestLook.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProjectSetup.Root+"/Data/ForestLook.asset");}
            var bloom=Override<Bloom>(profile);bloom.intensity.Override(.20f);bloom.threshold.Override(.95f);bloom.scatter.Override(.65f);
            var tone=Override<Tonemapping>(profile);tone.mode.Override(TonemappingMode.ACES);
            var grade=Override<ColorAdjustments>(profile);grade.postExposure.Override(.75f);grade.contrast.Override(12);grade.saturation.Override(-9);
            var vignette=Override<Vignette>(profile);vignette.intensity.Override(.16f);vignette.smoothness.Override(.55f);
            EditorUtility.SetDirty(profile);
            var volume=new GameObject("Forest color and lens",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            string path=ProjectSetup.Root+"/Materials/Blackpine sky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!sky){sky=new Material(Shader.Find("Last Light/Blackpine night sky"));AssetDatabase.CreateAsset(sky,path);}
            var moon=GameObject.Find("Moon · decorative, never protection").GetComponent<Light>();
            sky.SetVector("_MoonDirection",-moon.transform.forward);RenderSettings.skybox=sky;RenderSettings.sun=moon;
            sky.SetColor("_Horizon",new Color(.105f,.127f,.132f));sky.SetColor("_Zenith",new Color(.027f,.039f,.048f));EditorUtility.SetDirty(sky);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.12f,.17f,.23f);
            RenderSettings.ambientEquatorColor=new Color(.075f,.095f,.10f);
            RenderSettings.ambientGroundColor=new Color(.035f,.043f,.035f);
            RenderSettings.fogColor=new Color(.105f,.127f,.132f);
            RenderSettings.fogDensity=ForestAtmosphere.BaseDensity;
            string mistPath=ProjectSetup.Root+"/Materials/Low forest mist.mat";
            var mist=AssetDatabase.LoadAssetAtPath<Material>(mistPath);
            if(!mist){mist=new Material(Shader.Find("Last Light/Low forest mist"));AssetDatabase.CreateAsset(mist,mistPath);}
            mist.SetColor("_Tint",new Color(.22f,.26f,.27f,.4f));EditorUtility.SetDirty(mist);
            for(int n=0;n<50;n++)
            {
                var wisp=GameObject.CreatePrimitive(PrimitiveType.Quad);wisp.name="Low forest mist · presentation only";
                UnityEngine.Object.DestroyImmediate(wisp.GetComponent<Collider>());wisp.layer=LayerMask.NameToLayer("Decor");
                wisp.transform.position=new Vector3(-110+(n%10)*24,.85f,-100+(n/10)*44);
                wisp.transform.rotation=Quaternion.Euler(0,n*47,0);wisp.transform.localScale=new Vector3(16,2.6f,1);
                var renderer=wisp.GetComponent<Renderer>();renderer.sharedMaterial=mist;renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            foreach(var wall in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if(wall.name=="Forest boundary")wall.enabled=false;
        }
        static T Override<T>(VolumeProfile profile) where T:VolumeComponent
        {
            if(profile.TryGet<T>(out var component))return component;
            component=profile.Add<T>();AssetDatabase.AddObjectToAsset(component,profile);return component;
        }
        public static void Trail(Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a,side=Vector3.Cross(Vector3.up,delta.normalized);
            int sections=Mathf.CeilToInt(delta.magnitude/1.5f);var vertices=new Vector3[(sections+1)*2];
            var uv=new Vector2[vertices.Length];var tris=new int[sections*6];
            for(int n=0;n<=sections;n++)
            {
                float t=(float)n/sections;Vector3 center=Vector3.Lerp(a,b,t)+Vector3.up*.012f;
                float width=1.72f+.13f*Mathf.Sin(n*1.77f+a.x);
                vertices[n*2]=center-side*width;vertices[n*2+1]=center+side*width;
                uv[n*2]=new Vector2(0,t*delta.magnitude/3);uv[n*2+1]=new Vector2(1,t*delta.magnitude/3);
                if(n<sections){int k=n*6,v=n*2;tris[k]=v;tris[k+1]=v+2;tris[k+2]=v+1;tris[k+3]=v+1;tris[k+4]=v+2;tris[k+5]=v+3;}
            }
            var mesh=new Mesh{name="Irregular service trail"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateTangents();
            string path=ProjectSetup.Root+"/Data/Trail_"+a.x+"_"+a.z+"_"+b.x+"_"+b.z+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var root=new GameObject("Worn service trail",typeof(MeshFilter),typeof(MeshRenderer));root.layer=LayerMask.NameToLayer("Decor");
            root.GetComponent<MeshFilter>().sharedMesh=mesh;root.GetComponent<MeshRenderer>().sharedMaterial=palette["Trail"];
            root.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        public static void Understory(Vector3[][] paths,Vector3 start,Vector3 store)
        {
            var root=new GameObject("Original forest understory").transform;var rng=new System.Random(1408);
            for(int n=0;n<4100;n++)
            {
                var p=new Vector3((float)rng.NextDouble()*270-135,0,(float)rng.NextDouble()*270-135);
                if(ForestLayout.Clearing(p))continue;
                float nearest=100;foreach(var path in paths)for(int j=1;j<path.Length;j++)nearest=Mathf.Min(nearest,Distance(p,path[j-1],path[j]));
                if(nearest<2.2f)continue;
                string id=n%47==0?"fallen_log_01":n%31==0?"scanned_stump_01":n%13==0?"moss_rock_0"+(n%3+1):n%3==0?"fern_01":"grass_01";
                float scale=id.StartsWith("moss")?.45f+(float)rng.NextDouble()*.75f:.65f+(float)rng.NextDouble()*.7f;
                var obj=Model(id,p,Quaternion.Euler(0,(float)rng.NextDouble()*360,0),root,scale);
                foreach(var r in obj.GetComponentsInChildren<Renderer>())r.shadowCastingMode=id.StartsWith("moss")?ShadowCastingMode.On:ShadowCastingMode.Off;
            }
            // A perimeter of visible rocks conceals the level's stable invisible boundary.
            for(int n=0;n<200;n++)
            {
                float angle=n*Mathf.PI*2/200;var p=new Vector3(Mathf.Sin(angle)*138,-.7f,Mathf.Cos(angle)*138);
                Model("moss_rock_0"+(n%3+1),p,Quaternion.Euler(0,n*43,0),root,3.8f+n%3);
            }
        }
        static bool NearCabin(Vector3 p,Vector3 cabin)=>Mathf.Abs(p.x-cabin.x)<6.2f&&Mathf.Abs(p.z-cabin.z)<6.5f;
        static float Distance(Vector3 p,Vector3 a,Vector3 b){Vector3 d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
    }
}
