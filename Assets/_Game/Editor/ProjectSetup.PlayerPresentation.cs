using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void WirePlayerPresentation(GameSession game)
        {
            foreach(string id in new[]{"player_axe_hand_01","player_shotgun_hand_01","player_body_01"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Meshes/"+id+".fbx");
                importer.isReadable=true;importer.importBlendShapes=true;importer.SaveAndReimport();
            }
            var presence=game.player.gameObject.AddComponent<PlayerPresence>();game.presence=presence;
            presence.shadowBody=ForestArt.Model("player_body_01",Vector3.zero,Quaternion.identity,game.player.transform).transform;
            presence.bodyRenderers=presence.shadowBody.GetComponentsInChildren<Renderer>();
            var bounds=presence.bodyRenderers[0].bounds;foreach(var r in presence.bodyRenderers)bounds.Encapsulate(r.bounds);
            float bodyScale=game.player.controller.height/bounds.size.y;presence.standingScale=Vector3.one*bodyScale;
            presence.standingOffset=Vector3.up*(game.player.transform.position.y-bounds.min.y)*bodyScale;
            presence.bodyMorph=presence.shadowBody.GetComponentInChildren<SkinnedMeshRenderer>();
            if(presence.bodyMorph)presence.bodyMorph.updateWhenOffscreen=true;
            if(presence.bodyMorph)for(int i=0;i<presence.bodyMorph.sharedMesh.blendShapeCount;i++)if(presence.bodyMorph.sharedMesh.GetBlendShapeName(i).EndsWith("Crouch"))presence.crouchShape=i;
            foreach(var r in presence.bodyRenderers){r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;r.receiveShadows=false;r.renderingLayerMask=PlayerPresence.BodyShadowLayer;}
            presence.ResetPresence();
            presence.axeGrip=ForestArt.Model("player_axe_hand_01",new Vector3(-.008f,.22f,0),Quaternion.Euler(0,60,0),game.axe.heldModel,1/.62f).transform;
            AttachBodyArm(presence.axeGrip,game.player.view,false);
            presence.flashlightGrip=ForestArt.Model("player_axe_hand_01",new Vector3(-.22f,-.195f,.405f),Quaternion.Euler(90,0,0),game.player.hand).transform;
            presence.flashlightGrip.localScale=new Vector3(-1,1,1);AttachBodyArm(presence.flashlightGrip,game.player.view,true);
            game.player.hand.Find("Gloved hand").GetComponent<Renderer>().enabled=false;
            var fill=new GameObject("Subtle first person grip fill",typeof(Light));fill.transform.SetParent(game.player.view.transform,false);fill.transform.localPosition=new Vector3(0,-.14f,.22f);
            presence.gripLight=fill.GetComponent<Light>();presence.gripLight.type=LightType.Point;presence.gripLight.range=1;presence.gripLight.intensity=.34f;presence.gripLight.color=new Color(.84f,.80f,.73f);presence.gripLight.shadows=LightShadows.None;presence.gripLight.enabled=false;
            var sequence=game.player.gameObject.AddComponent<PlayerDeathSequence>();game.deathSequence=sequence;
            sequence.voice=PlayerVoice(game.player.transform,"Fatal close creature voice");sequence.impacts=PlayerVoice(game.player.transform,"Fatal tearing and impact");
            sequence.devourClip=Clip("walker_devour");sequence.tearClip=Clip("flesh_tear");sequence.stingClip=Clip("fatal_sting");
            var closeLight=new GameObject("Fatal contact detail fill",typeof(Light));closeLight.transform.SetParent(game.player.view.transform,false);closeLight.transform.localPosition=new Vector3(.08f,.16f,.03f);
            sequence.contactLight=closeLight.GetComponent<Light>();sequence.contactLight.type=LightType.Point;sequence.contactLight.range=2.2f;sequence.contactLight.intensity=.26f;sequence.contactLight.color=new Color(.65f,.53f,.48f);sequence.contactLight.shadows=LightShadows.None;sequence.contactLight.enabled=false;
            sequence.defensiveHands=new Transform[2];
            for(int i=0;i<2;i++)
            {
                var hand=ForestArt.Model("player_axe_hand_01",new Vector3(i==0?-.3f:.3f,-.25f,.48f),Quaternion.Euler(35,i==0?-15:15,i==0?-22:22),game.player.view.transform);
                if(i==0)hand.transform.localScale=new Vector3(-1,1,1);
                sequence.defensiveHands[i]=hand.transform;hand.SetActive(false);
            }
            var go=new GameObject("Fatal contact blood spray",typeof(ParticleSystem));go.transform.SetParent(game.player.transform,false);
            sequence.spray=go.GetComponent<ParticleSystem>();var main=sequence.spray.main;
            main.playOnAwake=false;main.loop=false;main.duration=sequence.Duration;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.70f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,3.4f);main.startSize=new ParticleSystem.MinMaxCurve(.010f,.052f);main.startColor=new Color(.28f,.003f,.007f,.92f);
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=.8f;main.maxParticles=240;
            var emission=sequence.spray.emission;emission.rateOverTime=0;
            var shape=sequence.spray.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=21;shape.radius=.065f;
            var renderer=sequence.spray.GetComponent<ParticleSystemRenderer>();
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            renderer.sharedMaterial=Material("Fatal fresh blood drops",new Color(.23f,.001f,.004f));renderer.sharedMaterial.SetFloat("_Smoothness",.4f);renderer.shadowCastingMode=ShadowCastingMode.Off;
            sequence.spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            PrefabUtility.SaveAsPrefabAsset(game.player.gameObject,Root+"/Prefabs/Player.prefab");
        }
        static void AttachBodyArm(Transform grip,Camera eyes,bool left)
        {
            var arm=grip.gameObject.AddComponent<FirstPersonArm>();arm.eyes=eyes;arm.bodyAnchor=new Vector3(left?-.26f:.26f,-.50f,.025f);
        }
        static void ConfigurePlayerShadowLights(GameSession game)
        {
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var data=light.GetUniversalAdditionalLightData();data.customShadowLayers=true;
                data.shadowRenderingLayers=light==game.flashlightVisual?PlayerPresence.WorldShadowLayer:PlayerPresence.WorldShadowLayer|PlayerPresence.BodyShadowLayer;
                if(light.type==LightType.Directional){light.shadowBias=.008f;light.shadowNormalBias=.01f;}
            }
            foreach(var r in game.player.hand.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        }
    }
}
