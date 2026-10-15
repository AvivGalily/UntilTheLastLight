using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Game.Editor
{
    public static class ProjectValidator
    {
        [MenuItem("Last Light/Validate generated scenes")]
        public static void ValidateAll()
        {
            if(!GraphicsSettings.defaultRenderPipeline)throw new Exception("URP is not assigned.");
            foreach(string name in new[]{"MechanicsTest","ForestGame"})
            {
                string path=ProjectSetup.Root+"/Scenes/"+name+".unity";if(!File.Exists(path))continue;
                EditorSceneManager.OpenScene(path);
                var games=UnityEngine.Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None);
                if(games.Length!=1)throw new Exception(name+": expected one simulation root.");var game=games[0];
                if(!game.tuning||!game.player||!game.monster||!game.hud||!game.inputTemplate||!game.flashlightVisual)throw new Exception(name+": unwired session.");
                if(!game.presence||!game.deathSequence||!game.presence.shadowBody||!game.presence.axeGrip||!game.deathSequence.devourClip||!game.deathSequence.tearClip||!game.deathSequence.spray)throw new Exception(name+": player presentation/fatal sequence not wired.");
                foreach(var r in game.presence.bodyRenderers)if(r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly)throw new Exception("Player requires a human shadow caster.");
                var ids=new HashSet<string>();foreach(var item in game.items)if(!item||string.IsNullOrEmpty(item.persistentId)||!ids.Add(item.persistentId))throw new Exception("Duplicate/missing persistent item ID.");
                if(!NavMesh.SamplePosition(game.cabinSpawn.position,out _,2,NavMesh.AllAreas))throw new Exception("Checkpoint is outside navigation.");
                foreach(var root in game.gameObject.scene.GetRootGameObjects())foreach(var transform in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)throw new Exception("Missing script: "+transform.name);
                if(name=="ForestGame"){ValidateArt(game);ValidateIntro(game);}
            }
            Debug.Log("VALIDATION PASS: wired scenes, unique IDs, navigation, URP, no missing scripts.");
        }
        static void ValidateIntro(GameSession game)
        {
            var intro=game.intro;
            if(!intro||!intro.config||!intro.bridge||!intro.vehicle||!intro.audioDirector||!intro.dialogue||!intro.fade||!intro.overlay)throw new Exception("Opening sequence not wired.");
            if(intro.bridge.cabinAnchor!=game.cabinSpawn||intro.vehicle.waypoints.Length<3||intro.config.duration<90||intro.config.duration>115||intro.config.lines.Length!=12)throw new Exception("Opening cabin/path/timing configuration invalid.");
            if(intro.vehicle.hands.grips.Length!=2||!intro.vehicle.hands.driver||intro.vehicle.hands.driver.arms.Length!=2||intro.dialogue.passengers.Length!=3||intro.dialogue.voices.Length!=3||intro.gateLeaves.Length!=2)throw new Exception("Opening driver/hunters/gate incomplete.");
            foreach(var arm in intro.vehicle.hands.driver.arms)if(!arm.upper||!arm.lower||!arm.wrist||!arm.grip||arm.upperLength<.15f||arm.lowerLength<.15f)throw new Exception("Anatomical driver arm rig missing.");
            foreach(var renderer in intro.vehicle.hands.driver.GetComponentsInChildren<SkinnedMeshRenderer>())if(renderer.bones.Length<20)throw new Exception("Driver body lacks weighted anatomical bones.");
            if(game.inputTemplate.FindActionMap("Intro")==null||game.inputTemplate.FindAction("Intro/Steer")==null||game.inputTemplate.FindAction("Intro/SkipIntro")==null||game.inputTemplate.FindAction("Intro/LookAround")==null||game.inputTemplate.FindAction("Intro/LookStick")==null)throw new Exception("Opening input map missing.");
            if(intro.bridge.mirrorCamera.GetComponent<AudioListener>()||intro.bridge.mirrorCamera.targetTexture==null)throw new Exception("Opening mirror/listener wiring invalid.");
            foreach(var passenger in intro.dialogue.passengers)if(!passenger||!passenger.head||!passenger.spine)throw new Exception("Opening passenger skeleton missing.");
            var audio=intro.audioDirector;
            if(!audio.vehicleAmbience||!audio.vehicleAmbience.clip||audio.vehicleAmbience.clip.name!="SFX_ForestVehicleAmbience"||!audio.vehicleAmbience.loop||
                !audio.gate||audio.gate.loop||!audio.gateClip||audio.gateClip.name!="SFX_ForestGate"||
                !audio.threat||audio.threat.loop||!audio.threatClip||audio.threatClip.name!="SFX_CreatureApproach"||
                !audio.effects||audio.effects.loop||!audio.blastClip||audio.blastClip.name!="SFX_VehicleExplosion"||!audio.wakeCreak)
                throw new Exception("Supplied opening SFX/loop modes missing.");
            var sound=audio.transform.Find("Opening sound layers");
            if(!sound||sound.GetComponentsInChildren<AudioSource>(true).Length!=6)throw new Exception("Previous opening layers still present or supplied sources missing.");
            foreach(var clip in new[]{audio.vehicleAmbience.clip,audio.gateClip,audio.threatClip,audio.blastClip})
                if(!clip.preloadAudioData)throw new Exception("Supplied effect not preloaded: "+clip.name);
            foreach(var line in intro.config.lines)
                if(!line.englishVoice||line.englishVoice.channels!=1||!line.englishVoice.preloadAudioData||line.englishVoice.length+.06f>line.duration)throw new Exception("Opening VO missing, unprepared or cut off: "+line.id);
            foreach(var renderer in intro.bridge.environment.GetComponentsInChildren<Renderer>(true))
                foreach(var material in renderer.sharedMaterials)if(!material||!material.shader||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Missing opening material: "+renderer.name);
        }
        static void ValidateArt(GameSession game)
        {
            if(!game.norton||!game.shotgun||game.norton.enemies.Length!=2||game.norton.projectiles.Length!=10||!game.norton.introductionClip||!game.norton.fireVoice.clip||!game.shotgun.fireClip||!game.shotgun.reloadClip)throw new Exception("Norton wreck, shotgun or head creature missing.");
            foreach(var h in game.norton.enemies)if(!h.idle||!h.run||!h.bite||!h.spit||!h.death||!h.hitCollider||!h.animator)throw new Exception("Six-legged head animations missing.");
            if(!game.hebrewStrings||!game.creature||game.creature.legs.Length!=8||!game.horrorAudio||!game.horrorAudio.clickClip||!game.horrorAudio.howlClip||!game.horrorAudio.pulseClip)throw new Exception("Expansion is unwired.");
            if(!game.horrorAudio.monsterVoice||!game.horrorAudio.growlClip||!game.horrorAudio.roarClip)throw new Exception("Walker voices missing.");
            if(!game.forestEncounter||!game.forestEncounter.web||!game.forestEncounter.web.intactCore||game.forestEncounter.web.supports.Length!=14||game.forestEncounter.web.bodyBands.Length!=7)
                throw new Exception("Victim silk scaffold/bone bindings missing.");
            foreach(string name in new[]{"Crawler wet gums","Crawler irregular teeth","Crawler throat"})
            {
                var oral=AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.Root+"/Materials/"+name+".mat");
                if(!oral||!oral.GetTexture("_BaseMap")||!oral.GetTexture("_BumpMap")||!oral.GetTexture("_MetallicGlossMap"))throw new Exception("Mouth PBR maps missing: "+name);
            }
            foreach(var wolf in game.wolves)if(!wolf.barkVoice||!wolf.barkClip)throw new Exception("Wolf barking missing.");
            var cabinSigns=Array.FindAll(UnityEngine.Object.FindObjectsByType<LocalizedSign>(FindObjectsSortMode.None),s=>s.singleLine);
            if(cabinSigns.Length!=3)throw new Exception("Expected three wooden cabin name signs.");
            foreach(var sign in cabinSigns)
            {
                var plaque=sign.transform.parent.GetComponentsInChildren<MeshFilter>();
                if(plaque.Length==0||!sign.transform.parent.name.StartsWith("Cabin name sign"))throw new Exception("Cabin lettering has no physical plaque.");
                var text=sign.GetComponent<TMPro.TMP_Text>();
                if(!text||text.color.r<.8f||text.color.g<.6f||text.color.b>.2f)throw new Exception("Cabin name must be yellow.");
            }
            if(!game.creature.mouth||HeroTree.ShapeIndex(game.creature.mouth.sharedMesh,"Jaw_Open")<0)throw new Exception("Creature has no opening mouth.");
            if(HeroTree.ShapeIndex(game.creature.mouth.sharedMesh,"Jaw_Bite")<0)throw new Exception("Creature has no jaw closure morph.");
            if(!game.creature.HasRig||game.creature.mouth.bones.Length<28||game.creature.animator.applyRootMotion)
                throw new Exception("Crawler requires a complete weighted rig, in-place clips and AI-owned movement.");
            if(game.creature.mouth.sharedMesh.vertexCount>90000)throw new Exception("Crawler exceeds the single-creature skinning budget.");
            if(game.creature.mouth.sharedMesh.uv2.Length!=game.creature.mouth.sharedMesh.vertexCount)
                throw new Exception("Crawler human-skin atlas UV channel is missing.");
            var skinMaterial=AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.Root+"/Materials/Crawler living skin.mat");
            var skinAtlas=skinMaterial?skinMaterial.GetTexture("_AnatomyMap"):null;
            if(!skinAtlas||skinAtlas.width!=4096||skinMaterial.GetFloat("_UseSkinAtlas")!=1)
                throw new Exception("Crawler human-skin 4K atlas is missing or disconnected.");
            int bodies=0,maps=0;foreach(var item in game.items){if(item.kind==ItemKind.Corpse)bodies++;if(item.kind==ItemKind.MapCorpse)maps++;}
            if(bodies!=4||maps!=1)throw new Exception("Expected five bodies, one carrying the field map.");
            if(!game.ambience||!game.ambience.owlClip||!game.ambience.nightjarClip||!game.ambience.frogClip||!game.ambience.leafClip)
                throw new Exception("Night wildlife soundscape is missing clips.");
            if(!game.ambience.insects||!game.ambience.insects.clip||game.ambience.insects.clip.name!="night_crickets"||
                !game.ambience.insects.loop||game.ambience.insects.playOnAwake||game.ambience.insects.volume!=0||game.ambience.cricketVolume>.015f)
                throw new Exception("Quiet gameplay-only building crickets are not configured.");
            if(!game.atmosphere||game.atmosphere.wisps.Length!=42||!RenderSettings.fog||RenderSettings.fogDensity<ForestAtmosphere.BaseDensity)
                throw new Exception("Dense fog/airborne wisps are not connected.");
            for(int n=0;n<4;n++)
            {
                var body=GameObject.Find("Forest body "+n);var group=body.GetComponentInChildren<LODGroup>();
                if(!group||group.GetLODs().Length!=3)throw new Exception("Human body distance LODs missing.");
                var human=Array.Find(group.GetLODs()[0].renderers,r=>Array.Exists(r.sharedMaterials,m=>m&&(m.name.StartsWith("HumanSkin")||m.name.StartsWith("ArrivalSkin"))));
                if(!human||human.bounds.size.y>.65f||Mathf.Max(human.bounds.size.x,human.bounds.size.z)<1.55f||!body.GetComponent<BloodySurface>())throw new Exception("Human corpse axes/anatomy/blood are wrong.");
            }
            if(!game.encounters||!game.damage||!game.axe||!game.foley||!game.foley.scream||!game.foley.hurt||!game.foley.injury)throw new Exception("Combat/foley feedback is unwired.");
            if(game.wolves.Length!=9||!Array.TrueForAll(game.wolves,w=>w&&w.animator&&w.idle&&w.run&&w.bite&&w.death&&w.blood))throw new Exception("Nine animated undead wolves are required.");
            var axe=Array.Find(game.items,i=>i.kind==ItemKind.Axe);
            if(!axe||Vector3.Distance(axe.transform.position,ForestLayout.Bodies[0])>2)throw new Exception("Axe must be beside the closest body.");
            var attack=game.inputTemplate.FindAction("Attack");
            if(attack==null||attack.bindings.Count==0||!attack.bindings[0].path.Contains("leftButton"))throw new Exception("Missing left mouse axe binding.");
            var bloodScreen=AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.Root+"/Resources/BloodScreen.mat");
            if(!bloodScreen||ShaderUtil.ShaderHasError(bloodScreen.shader))throw new Exception("Blood screen shader missing/broken.");
            var weak=GameObject.Find("Weak research bulb - NO PROTECTION");
            if(!weak||!weak.GetComponent<Light>()||weak.GetComponent<SafeLight>())throw new Exception("Research weak light incorrectly wired.");
            foreach(var shelter in game.shelters)if(Vector3.Distance(shelter.transform.position,ForestLayout.Research)<15)throw new Exception("Research station incorrectly grants shelter.");
            if(game.hud.font.atlasPopulationMode!=TMPro.AtlasPopulationMode.Static||game.hud.font.fallbackFontAssetTable.Count==0||!game.hud.font.HasCharacter('א',true,false))throw new Exception("Hebrew font not baked/wired.");
            var locale=new Localization(game.strings,game.hebrewStrings);bool language=locale.Hebrew;locale.SetHebrew(true);
            foreach(var doc in ForestLayout.Documents)if(!HebrewText.ContainsHebrew(locale.Get(doc)))throw new Exception("Missing Hebrew document: "+doc);
            locale.SetHebrew(language);
            if(!game.player.view.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing)throw new Exception("Forest camera has no post processing.");
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(!material||!material.shader||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Missing/error material on "+renderer.name);
            var heroes=UnityEngine.Object.FindObjectsByType<HeroTree>(FindObjectsSortMode.None);
            if(heroes.Length!=2)throw new Exception("Expected two deforming hero trees.");
            foreach(var hero in heroes)
            {
                if(hero.crowns.Length==0)throw new Exception("Hero tree has no imported blend shapes.");
                foreach(var crown in hero.crowns)
                    if(HeroTree.ShapeIndex(crown.sharedMesh,"Dread_Stage1")<0||HeroTree.ShapeIndex(crown.sharedMesh,"Dread_Stage2")<0)throw new Exception("Missing hero shape: "+crown.name);
            }
            var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            if(groups.Length<100)throw new Exception("Forest distance LODs are missing.");
            foreach(var group in groups)
            {
                var lods=group.GetLODs();if(lods.Length!=3)throw new Exception("Expected three LODs: "+group.name);
                foreach(var lod in lods)if(lod.renderers.Length==0)throw new Exception("Empty LOD: "+group.name);
                if(group.name.StartsWith("blackpine")||group.name.StartsWith("birch"))
                {
                    long near=Triangles(lods[0]),far=Triangles(lods[2]);
                    if(far>=near*.5f)throw new Exception("Far tree LOD does not reduce geometry: "+group.name);
                    if(group.name.StartsWith("blackpine"))foreach(var renderer in lods[0].renderers)
                    {
                        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                        if(mesh.uv.Length!=mesh.vertexCount)throw new Exception("Scanned tree source UV attribute was not converted: "+group.name);
                    }
                }
            }
            foreach(string cabin in new[]{"ranger_cabin_01","storehouse_01"})
            {
                var root=GameObject.Find(cabin);var renderer=root.GetComponentInChildren<Renderer>();
                if(renderer.bounds.size.y<4||renderer.bounds.size.y>7||renderer.bounds.size.x<8||renderer.bounds.size.z<7)throw new Exception("FBX axes/units are wrong: "+cabin);
                var item=Array.Find(game.items,i=>i.persistentId==(cabin=="ranger_cabin_01"?"note_guard_rules":"service_fuse"));
                bool desktop=false;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 world=filter.transform.TransformPoint(vertex),relative=world-item.transform.position;
                    if(Mathf.Abs(relative.x)<1.3f&&Mathf.Abs(relative.z)<.7f&&Mathf.Abs(world.y-1.05f)<.025f)desktop=true;
                }
                if(!desktop)throw new Exception("Artwork tabletop does not support pickup positions: "+cabin);
            }
            Debug.Log("ART VALIDATION PASS: FBX metres/axes, pickup tabletops, materials, hero shapes and three distance LODs.");
        }
        static long Triangles(LOD lod)
        {
            long count=0;
            foreach(var renderer in lod.renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;
                var mesh=filter.sharedMesh;for(int n=0;n<mesh.subMeshCount;n++)count+=(long)mesh.GetIndexCount(n)/3;
            }
            return count;
        }
    }
    public static class BuildPipeline
    {
        [MenuItem("Last Light/Build Windows")]
        public static void BuildWindows()
        {
            ProjectValidator.ValidateAll();Directory.CreateDirectory("Builds/Windows");
            var options=new BuildPlayerOptions{scenes=new[]{ProjectSetup.Root+"/Scenes/ForestGame.unity"},locationPathName="Builds/Windows/UntilTheLastLight.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
            var report=UnityEditor.BuildPipeline.BuildPlayer(options);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            File.Copy("Docs/ThirdPartyNotices.md","Builds/Windows/ThirdPartyNotices.md",true);
            File.Copy("Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt","Builds/Windows/LiberationSans-OFL.txt",true);
            File.Copy("Assets/_Game/Art/Fonts/Noto-OFL.txt","Builds/Windows/Noto-OFL.txt",true);
            Debug.Log("WINDOWS BUILD SUCCESS: "+report.summary.totalSize+" bytes");
        }
    }
}
