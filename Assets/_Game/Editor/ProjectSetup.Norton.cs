using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static Material EmberMaterial(bool smoke)
        {
            var m=Material(smoke?"Wreck drifting soot":"Living orange flame",Color.white);m.shader=Shader.Find("Last Light/Embers and smoke");m.SetFloat("_Mode",smoke?1:0);m.SetFloat("_DstBlend",smoke?10:1);EditorUtility.SetDirty(m);return m;
        }
        static ParticleSystem FireParticles(Transform parent,string name,Vector3 local,Color color,float size,float speed,float life,float rate,bool smoke=false)
        {
            var root=new GameObject(name,typeof(ParticleSystem));root.transform.SetParent(parent,false);root.transform.localPosition=local;root.transform.localRotation=Quaternion.Euler(-90,0,0);
            var p=root.GetComponent<ParticleSystem>();var m=p.main;m.playOnAwake=false;m.loop=true;m.startLifetime=new ParticleSystem.MinMaxCurve(life*.6f,life);m.startSpeed=new ParticleSystem.MinMaxCurve(speed*.7f,speed);
            m.startSize=new ParticleSystem.MinMaxCurve(size*.65f,size);m.startColor=color;m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=300;m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var e=p.emission;e.rateOverTime=rate;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=smoke?13:20;shape.radius=size*.25f;
            var over=p.colorOverLifetime;over.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(smoke?color:new Color(.65f,.045f,.001f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(color.a,.13f),new GradientAlphaKey(0,1)});over.color=gradient;
            var sizes=p.sizeOverLifetime;sizes.enabled=true;sizes.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,smoke?.7f:1,1,smoke?1.8f:.18f));
            var r=p.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=EmberMaterial(smoke);r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return p;
        }
        static void WireNorton(GameSession game,bool mechanics)
        {
            var gun=game.player.gameObject.AddComponent<ShotgunCombat>();game.shotgun=gun;
            gun.heldModel=ForestArt.Model("norton_shotgun_01",new Vector3(.24f,-.24f,.43f),Quaternion.Euler(-2,-3,0),game.player.hand,.68f).transform;
            ForestArt.Model("player_axe_hand_01",new Vector3(.035f,-.03f,-.04f),Quaternion.Euler(85,0,20),gun.heldModel,1.2f);
            ForestArt.Model("player_axe_hand_01",new Vector3(-.02f,-.035f,.38f),Quaternion.Euler(90,0,175),gun.heldModel,1.12f);
            gun.voice=PlayerVoice(game.player.transform,"Shotgun report and reload");gun.fireClip=Clip("shotgun_fire");gun.reloadClip=Clip("shotgun_reload");gun.emptyClip=Clip("shotgun_empty");
            var flash=new GameObject("Shotgun muzzle flash",typeof(Light));flash.transform.SetParent(gun.heldModel,false);flash.transform.localPosition=new Vector3(0,0,.86f);
            gun.muzzleLight=flash.GetComponent<Light>();gun.muzzleLight.type=LightType.Point;gun.muzzleLight.range=6;gun.muzzleLight.intensity=5;gun.muzzleLight.color=new Color(1,.54f,.12f);gun.muzzleLight.enabled=false;
            gun.muzzleSmoke=FireParticles(gun.heldModel,"Shotgun smoke",new Vector3(0,0,.86f),new Color(.4f,.39f,.34f,.25f),.15f,.5f,.4f,0,true);gun.heldModel.gameObject.SetActive(false);
            if(mechanics)return;
            var scene=new GameObject("Norton wreck and lightless heads",typeof(NortonEncounter));game.norton=scene.GetComponent<NortonEncounter>();var encounter=game.norton;
            var wreckRoot=new GameObject("Ranger burning car");wreckRoot.transform.position=ForestLayout.Wreck;wreckRoot.transform.rotation=Quaternion.Euler(0,-25,0);encounter.wreck=wreckRoot.transform;
            ForestArt.Model("burning_car_01",Vector3.zero,Quaternion.identity,wreckRoot.transform);
            wreckRoot.layer=LayerMask.NameToLayer("WorldSolid");var carCollider=wreckRoot.AddComponent<BoxCollider>();carCollider.center=new Vector3(0,.52f,0);carCollider.size=new Vector3(2.15f,1.1f,4.5f);
            encounter.carEffects=new[]{FireParticles(wreckRoot.transform,"Engine fire",new Vector3(0,.6f,1.3f),new Color(1,.22f,.025f,.78f),1.25f,1.1f,1.25f,36),FireParticles(wreckRoot.transform,"Cabin fire",new Vector3(0,.55f,0),new Color(1,.32f,.025f,.58f),1.15f,.8f,1.5f,28),FireParticles(wreckRoot.transform,"Heavy rising smoke",new Vector3(0,1.0f,.65f),new Color(.095f,.09f,.085f,.43f),2.4f,.75f,5.5f,11,true),FireParticles(wreckRoot.transform,"Floating embers",new Vector3(0,.7f,.8f),new Color(1,.36f,.04f,.8f),.07f,2.6f,2.5f,14)};
            var glow=new GameObject("Fire illuminates the wreck but grants no shelter",typeof(Light));glow.transform.SetParent(wreckRoot.transform,false);glow.transform.localPosition=new Vector3(0,1.4f,.6f);encounter.carLight=glow.GetComponent<Light>();encounter.carLight.type=LightType.Point;encounter.carLight.range=12;encounter.carLight.intensity=3.3f;encounter.carLight.color=new Color(1,.32f,.08f);encounter.carLight.shadows=LightShadows.Soft;
            encounter.fireVoice=wreckRoot.AddComponent<AudioSource>();encounter.fireVoice.playOnAwake=false;encounter.fireVoice.clip=Clip("wreck_fire");encounter.fireVoice.loop=true;encounter.fireVoice.spatialBlend=1;encounter.fireVoice.volume=.58f;encounter.fireVoice.minDistance=3;encounter.fireVoice.maxDistance=25;
            var corpse=new GameObject("Norton eviscerated casualty",typeof(WorldItem));corpse.transform.position=ForestLayout.NortonBody;corpse.layer=LayerMask.NameToLayer("Interactable");encounter.casualty=corpse.transform;
            var body=ForestArt.Model("eviscerated_casualty_01",Vector3.zero,Quaternion.identity,corpse.transform);BloodOnBody(corpse,body,4);
            var bodyCol=corpse.AddComponent<BoxCollider>();bodyCol.center=new Vector3(0,.22f,.85f);bodyCol.size=new Vector3(1.0f,.45f,1.9f);
            var document=corpse.GetComponent<WorldItem>();document.persistentId="norton_casualty";document.kind=ItemKind.Corpse;document.noteId=NortonEncounter.Letter;items.Add(document);
            var letter=Item("norton_letter",ItemKind.Note,ForestLayout.NortonBody+new Vector3(.62f,.055f,1.1f),"Bloodstained letter to Norton",paper,1,new Vector3(.32f,.018f,.23f));letter.noteId=NortonEncounter.Letter;
            var weapon=Item("norton_shotgun",ItemKind.Shotgun,ForestLayout.NortonBody+new Vector3(1.25f,.10f,.45f),"Norton's shotgun",metal,1,new Vector3(.30f,.18f,1.4f));weapon.GetComponent<Renderer>().enabled=false;ForestArt.Model("norton_shotgun_01",Vector3.zero,Quaternion.Euler(0,25,90),weapon.transform);
            foreach(var place in new[]{ForestLayout.Cabin+new Vector3(-2.2f,1.15f,1),ForestLayout.Store+new Vector3(-1.4f,1.15f,1),ForestLayout.Research+new Vector3(-4.6f,1.15f,3)})
            {var box=Item("norton_shells_"+items.Count,ItemKind.Shells,place,"Shotgun cartridges",paper,3,new Vector3(.24f,.12f,.18f));}
            encounter.introduction=PlayerVoice(scene.transform,"First lightless head cry");encounter.introductionClip=Clip("head_introduction");
            ConfigureHeadImport();encounter.enemies=new CinderHead[2];
            for(int i=0;i<2;i++)
            {
                var root=new GameObject("Lightless head "+i,typeof(CinderHead));root.transform.SetParent(scene.transform);root.layer=LayerMask.NameToLayer("Monster");var enemy=root.GetComponent<CinderHead>();encounter.enemies[i]=enemy;
                var model=ForestArt.Model("cinder_head_01",Vector3.zero,Quaternion.identity,root.transform);enemy.animator=model.GetComponentInChildren<Animator>();enemy.animator.applyRootMotion=false;enemy.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var a in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Meshes/cinder_head_01.fbx"))if(a is AnimationClip c){if(c.name=="Head_Idle")enemy.idle=c;else if(c.name=="Head_Run")enemy.run=c;else if(c.name=="Head_Bite")enemy.bite=c;else if(c.name=="Head_Spit")enemy.spit=c;else if(c.name=="Head_Death")enemy.death=c;}
                if(!enemy.idle||!enemy.run||!enemy.bite||!enemy.spit||!enemy.death)throw new Exception("Cinder head needs five skeletal clips.");
                var col=root.AddComponent<CapsuleCollider>();col.center=new Vector3(0,.6f,0);col.height=1.1f;col.radius=.34f;col.isTrigger=true;enemy.hitCollider=col;
                enemy.voice=root.AddComponent<AudioSource>();enemy.voice.playOnAwake=false;enemy.voice.spatialBlend=1;enemy.voice.minDistance=2;enemy.voice.maxDistance=25;enemy.voice.dopplerLevel=0;
                enemy.growlClip=Clip("head_growl");enemy.biteClip=Clip("head_bite");enemy.spitClip=Clip("head_spit");enemy.deathClip=Clip("head_death");
                enemy.blood=root.AddComponent<BloodySurface>();enemy.blood.surfaces=model.GetComponentsInChildren<Renderer>();enemy.blood.wound0=new Vector4(.13f,.9f,.14f,.24f);enemy.blood.wound1=new Vector4(0,.48f,0,.19f);enemy.blood.amount=.55f;enemy.blood.Refresh();
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;var b=skin.localBounds;b.Expand(2);skin.localBounds=b;}
                root.SetActive(false);
            }
            encounter.projectiles=new SpatFireball[10];
            for(int i=0;i<10;i++)
            {
                var root=new GameObject("Spat fireball "+i,typeof(SpatFireball));root.transform.SetParent(scene.transform);var p=root.GetComponent<SpatFireball>();encounter.projectiles[i]=p;
                var orb=Primitive("Incandescent fire core",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.24f,amber,root.transform,false);p.core=orb.GetComponent<Renderer>();
                p.flame=FireParticles(root.transform,"Fire trail",Vector3.zero,new Color(1,.22f,.02f,.7f),.42f,.12f,.33f,45);
                p.impact=FireParticles(root.transform,"Fire impact",Vector3.zero,new Color(1,.35f,.035f,.7f),.4f,2,.55f,0);p.voice=root.AddComponent<AudioSource>();p.voice.playOnAwake=false;p.voice.spatialBlend=1;p.voice.minDistance=2;p.voice.maxDistance=25;p.impactClip=Clip("fireball_impact");
                p.glow=root.AddComponent<Light>();p.glow.type=LightType.Point;p.glow.range=4;p.glow.intensity=2.4f;p.glow.color=new Color(1,.22f,.02f);root.SetActive(false);
            }
        }
        static void ConfigureHeadImport()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/_Game/Art/Meshes/cinder_head_01.fbx");importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;foreach(var c in clips){c.name=c.takeName.Substring(c.takeName.IndexOf("Head_",StringComparison.Ordinal));c.loopTime=c.name=="Head_Idle"||c.name=="Head_Run";c.loopPose=c.loopTime;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.lockRootRotation=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
        }
    }
}
