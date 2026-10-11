using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void ForestExpanded(Vector3 start)
        {
            Cabin("Ranger cabin",start,true);Vector3 store=ForestLayout.Store;Cabin("East storehouse",store,true);
            Item("note_guard_rules",ItemKind.Note,start+new Vector3(-2.7f,1.10f,1),"Ranger note",paper,1,new Vector3(.5f,.03f,.36f)).noteId="note_guard_rules";
            Item("note_route",ItemKind.Note,start+new Vector3(-1.9f,1.10f,1),"Route letter",paper,1,new Vector3(.4f,.03f,.3f)).noteId="note_route";
            Item("cabin_batteries",ItemKind.Batteries,start+new Vector3(-3.3f,1.17f,1),"Spare batteries",metal,2);
            Item("emergency_charger",ItemKind.EmergencyCharger,start+new Vector3(3,1.1f,1),"Hand-crank charger",amber,1,new Vector3(.6f,.5f,.45f)).houseCharger=true;
            Item("store_charger",ItemKind.EmergencyCharger,store+new Vector3(3,1.1f,1),"Storehouse charger",amber,1,new Vector3(.6f,.5f,.45f)).houseCharger=true;
            Box("Store charger stand",store+new Vector3(3,.4f,1),new Vector3(.6f,.8f,.6f),timber);
            Box("Charger stand",start+new Vector3(3,.4f,1),new Vector3(.6f,.8f,.6f),timber);
            Item("cabin_circuit",ItemKind.PowerPanel,start+new Vector3(1.5f,1.5f,-3.8f),"Cabin circuit",metal,1,new Vector3(.8f,.9f,.18f));
            Item("service_fuse",ItemKind.Fuse,store+new Vector3(-2.7f,1.20f,1),"Service fuse",paper,1,new Vector3(.18f,.3f,.18f));
            Item("store_batteries",ItemKind.Batteries,store+new Vector3(-3.35f,1.20f,1),"Spare batteries",metal,2);
            Item("note_storehouse",ItemKind.Note,store+new Vector3(-1.95f,1.10f,1),"Storekeeper note",paper,1,new Vector3(.4f,.03f,.3f)).noteId="note_storehouse";
            Shelter("old_lantern",ForestLayout.Lantern+Vector3.up*2,new Vector3(7,4.4f,7),true);
            Item("trail_charger",ItemKind.EmergencyCharger,ForestLayout.Lantern+Vector3.up,"Trail charger",amber);
            circuit.Add(Shelter("restored_gate_lamp",ForestLayout.Gate+new Vector3(0,2.6f,-6),new Vector3(12,6,12),false));
            // Two symmetric fence runs flank a genuine 5m opening; the movable gate alone blocks it.
            for(int side=-1;side<=1;side+=2)
            {
                float from=side<0?-ForestLayout.Size/2:ForestLayout.Gate.x+2.5f;
                float to=side<0?ForestLayout.Gate.x-2.5f:ForestLayout.Size/2;
                for(float x=from;x<to;x+=10)
                {
                    float width=Mathf.Min(10,to-x),center=x+width/2;
                    var wall=Box("Northern fence",new Vector3(center,1.8f,ForestLayout.Gate.z),new Vector3(width,3.6f,.22f),metal);wall.GetComponent<Renderer>().enabled=false;
                    var art=ForestArt.Model("forest_fence_01",new Vector3(center,0,ForestLayout.Gate.z),Quaternion.identity);art.transform.localScale=new Vector3(width/10,1,1);
                }
            }
            var gate=Box("Sliding gate leaf",ForestLayout.Gate+Vector3.up*1.8f,new Vector3(5,3.6f,.25f),metal);gate.GetComponent<Renderer>().enabled=false;
            gate.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
            var obstacle=gate.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;obstacle.carvingMoveThreshold=.05f;
            ForestArt.Model("forest_gate_01",ForestLayout.Gate,Quaternion.identity).transform.SetParent(gate.transform,true);
            Item("north_gate",ItemKind.ExitGate,ForestLayout.Gate+new Vector3(-3.1f,1.3f,-1.7f),"Gate switch",amber,1,new Vector3(.3f,.4f,.2f));
            Sign("sign_gate",ForestLayout.Gate+new Vector3(0,3.5f,-2),3.8f);
            ResearchBuilding();
            for(int i=0;i<ForestLayout.Bodies.Length;i++)
            {
                var root=new GameObject("Forest body "+i,typeof(WorldItem));root.transform.position=ForestLayout.Bodies[i];root.transform.rotation=Quaternion.Euler(0,new[]{25,155,280,75}[i],0);root.layer=LayerMask.NameToLayer("Interactable");
                var item=root.GetComponent<WorldItem>();item.persistentId="corpse_"+i;item.kind=i==0?ItemKind.MapCorpse:ItemKind.Corpse;item.noteId=ForestLayout.BodyNotes[i];items.Add(item);
                var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(1.35f,.52f,1.95f);collider.center=new Vector3(0,.25f,0);
                var human=ForestArt.Model("forest_body_0"+(i+1),Vector3.zero,Quaternion.identity,root.transform);BloodOnBody(root,human,i);
                // A visible, folded map on the surveyor's satchel. It remains with the body for re-reading.
                if(i==0)
                {
                    var sheet=Box("Surveyor folded map",new Vector3(.58f,.026f,.2f),new Vector3(.22f,.003f,.28f),paper,root.transform,false);
                    for(int n=0;n<4;n++){var line=Box("Map pencil line",new Vector3(.52f+n*.035f,.028f,.2f),new Vector3(.002f,.001f,.20f),bark,root.transform,false);line.transform.localRotation=Quaternion.Euler(0,n*17,0);}
                    var pickup=Item("surveyor_axe",ItemKind.Axe,root.transform.position+new Vector3(1.25f,.06f,-.55f),"Surveyor field axe",metal,1,Vector3.one);pickup.GetComponent<Renderer>().enabled=false;
                    var axe=ForestArt.Model("field_axe_01",pickup.transform.position,Quaternion.Euler(90,35,0));axe.transform.SetParent(pickup.transform,true);
                    var renderers=axe.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    var pickupCollider=pickup.GetComponent<BoxCollider>();pickupCollider.center=pickup.transform.InverseTransformPoint(bounds.center);pickupCollider.size=Vector3.Max(bounds.size+Vector3.one*.08f,new Vector3(.16f,.18f,.16f));
                }
            }
            foreach(var path in ForestLayout.Paths)for(int i=1;i<path.Length;i++)Trail(path[i-1],path[i]);
            var rng=new System.Random(tuning.seed);
            // Keep the previous forest density rather than stretching a sparse collection of trees.
            for(int i=0;i<1600;i++)
            {
                var p=new Vector3((float)rng.NextDouble()*(ForestLayout.Size-10)-(ForestLayout.Size-10)/2,0,(float)rng.NextDouble()*(ForestLayout.Size-10)-(ForestLayout.Size-10)/2);
                if(ForestLayout.Clearing(p))continue;
                bool near=false;foreach(var path in ForestLayout.Paths)for(int j=1;j<path.Length;j++)if(DistanceToSegment(p,path[j-1],path[j])<3.2f)near=true;
                if(!near)Tree(p,7+(float)rng.NextDouble()*6,(float)rng.NextDouble()*360);
            }
            Tree(new Vector3(-34,0,-46),11,35,true);Tree(new Vector3(76,0,28),10,180,true);
            ForestArt.Understory(ForestLayout.Paths,start,store);
            foreach(var p in new[]{ForestLayout.Lantern,ForestLayout.Gate-Vector3.forward*6})Box("Shelter lamp post",p+Vector3.up,new Vector3(.12f,2,.12f),metal);
            var wind=new GameObject("Forest wind",typeof(AudioSource)).GetComponent<AudioSource>();wind.clip=Clip("forest_wind");wind.loop=true;wind.playOnAwake=true;wind.volume=.3f;
        }
        static void ResearchBuilding()
        {
            var root=new GameObject("Abandoned research station").transform;root.position=ForestLayout.Research;
            Box("Foundation",new Vector3(0,-.1f,0),new Vector3(18.2f,.2f,14.2f),stone,root);
            Box("Back wall",new Vector3(0,1.6f,-7),new Vector3(18,3.2f,.25f),timber,root);
            foreach(float x in new[]{-9f,9f})Box("Side wall",new Vector3(x,1.6f,0),new Vector3(.25f,3.2f,14),timber,root);
            foreach(float x in new[]{-5.25f,5.25f})Box("Front wall",new Vector3(x,1.6f,7),new Vector3(7.5f,3.2f,.25f),timber,root);
            Box("Entrance lintel",new Vector3(0,2.98f,7),new Vector3(3,.44f,.25f),timber,root);
            foreach(float x in new[]{-1.5f,1.5f})
            {
                foreach(var span in new[]{new Vector2(-7,-4.75f),new Vector2(-2.25f,2.25f),new Vector2(4.75f,7)})Box("Corridor partition",new Vector3(x,1.6f,(span.x+span.y)/2),new Vector3(.2f,3.2f,span.y-span.x),timber,root);
                foreach(float z in new[]{-3.5f,3.5f})Box("Room lintel",new Vector3(x,2.98f,z),new Vector3(.2f,.44f,2.5f),timber,root);
            }
            Box("West closed door",new Vector3(-1.5f,1.6f,-3.5f),new Vector3(.2f,3.2f,2.5f),timber,root);
            Box("East room divider",new Vector3(5.25f,1.6f,0),new Vector3(7.5f,3.2f,.2f),timber,root);
            Box("Research desk",new Vector3(-4.6f,.96f,3),new Vector3(3,.18f,1.3f),timber,root);
            Box("Observation desk",new Vector3(5.5f,.95f,-3.7f),new Vector3(2.8f,.15f,1.4f),timber,root);
            foreach(float z in new[]{2.2f,4.9f})Box("Bunk",new Vector3(6,.5f,z),new Vector3(1.4f,.7f,2.2f),metal,root);
            Box("Archive shelves",new Vector3(-7.9f,1.4f,-3),new Vector3(1.5f,2.8f,6),timber,root);
            Box("Roof collision",new Vector3(0,3.55f,0),new Vector3(18,.2f,14),metal,root);
            foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
            ForestArt.Model("research_station_01",Vector3.zero,Quaternion.identity,root);
            // Deliberately a Unity Light only. It is never registered with the protection service.
            var bulb=new GameObject("Weak research bulb - NO PROTECTION",typeof(Light));bulb.transform.SetParent(root,false);bulb.transform.localPosition=new Vector3(0,2.65f,1);
            var light=bulb.GetComponent<Light>();light.type=LightType.Point;light.intensity=.6f;light.range=7;light.color=new Color(.71f,.63f,.38f);light.shadows=LightShadows.Soft;
            ShadowTier(light,UnityEngine.Rendering.Universal.UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow);
            var fixture=ForestArt.Model("shelter_lantern_01",bulb.transform.localPosition,Quaternion.identity,root);
            var weak=new Material(amber);weak.name="Weak research glass";weak.SetColor("_EmissionColor",new Color(.16f,.12f,.045f));weak.color=new Color(.22f,.18f,.09f);
            string weakPath=Root+"/Materials/Weak research glass.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(weakPath);
            if(old){EditorUtility.CopySerialized(weak,old);UnityEngine.Object.DestroyImmediate(weak);weak=old;}else AssetDatabase.CreateAsset(weak,weakPath);
            foreach(var r in fixture.GetComponentsInChildren<Renderer>()){var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]==amber)materials[i]=weak;r.sharedMaterials=materials;}
            CabinSign("sign_research",root,new Vector3(0,3.15f,7.23f),3.4f);
        }
        static AudioClip Clip(string id)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+id+".wav")??AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+id+".ogg");
        static ForestAmbience CreateForestAmbience()
        {
            var root=new GameObject("Night wildlife",typeof(ForestAmbience));var ambience=root.GetComponent<ForestAmbience>();
            ambience.insects=root.AddComponent<AudioSource>();ambience.insects.clip=Clip("night_crickets");ambience.insects.loop=true;ambience.insects.volume=0;ambience.insects.playOnAwake=false;
            AudioSource Voice(string name,float min,float max)
            {
                var source=new GameObject(name,typeof(AudioSource),typeof(AudioLowPassFilter)).GetComponent<AudioSource>();source.transform.SetParent(root.transform,false);
                source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=min;source.maxDistance=max;source.playOnAwake=false;source.dopplerLevel=0;
                return source;
            }
            ambience.birds=Voice("Night birds in the canopy",8,90);ambience.frogs=Voice("Distant frogs",6,75);ambience.leaves=Voice("Leaves rustling",3,35);
            root.AddComponent<AudioLowPassFilter>();ambience.filters=root.GetComponentsInChildren<AudioLowPassFilter>();
            ambience.owlClip=Clip("night_owl");ambience.nightjarClip=Clip("nightjar_calls");ambience.frogClip=Clip("forest_frogs");ambience.leafClip=Clip("leaf_rustle");
            return ambience;
        }
        static ForestAtmosphere CreateForestAtmosphere(Camera camera)
        {
            var root=new GameObject("Drifting airborne fog",typeof(ForestAtmosphere));var atmosphere=root.GetComponent<ForestAtmosphere>();
            atmosphere.wisps=new Renderer[42];var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Low forest mist.mat");
            for(int n=0;n<atmosphere.wisps.Length;n++)
            {
                var cloud=GameObject.CreatePrimitive(PrimitiveType.Quad);cloud.name="Airborne fog "+n;cloud.transform.SetParent(root.transform,false);
                UnityEngine.Object.DestroyImmediate(cloud.GetComponent<Collider>());cloud.layer=LayerMask.NameToLayer("Decor");
                cloud.transform.localScale=new Vector3(13+n%5*2,5+n%3*1.5f,1);
                var renderer=cloud.GetComponent<Renderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                atmosphere.wisps[n]=renderer;
            }
            atmosphere.ResetSequence(tuning.seed,camera.transform.position);
            foreach(var wisp in atmosphere.wisps)wisp.transform.rotation=Quaternion.LookRotation(wisp.transform.position-camera.transform.position,Vector3.up);
            return atmosphere;
        }
        static HorrorAudio CreateHorrorAudio(MonsterBrain monster)
        {
            var root=new GameObject("Original horror soundscape",typeof(HorrorAudio));var audio=root.GetComponent<HorrorAudio>();
            audio.clicks=monster.gameObject.AddComponent<AudioSource>();audio.clicks.spatialBlend=1;audio.clicks.rolloffMode=AudioRolloffMode.Custom;audio.clicks.SetCustomCurve(AudioSourceCurveType.CustomRolloff,AnimationCurve.Linear(0,1,1,1));audio.clicks.maxDistance=70;audio.clicks.playOnAwake=false;
            audio.tension=root.AddComponent<AudioSource>();audio.tension.clip=Clip("proximity_tension");audio.tension.loop=true;audio.tension.playOnAwake=false;
            audio.pulse=root.AddComponent<AudioSource>();audio.pulse.playOnAwake=false;
            audio.wolf=new GameObject("Distant wolf",typeof(AudioSource)).GetComponent<AudioSource>();audio.wolf.spatialBlend=1;audio.wolf.minDistance=35;audio.wolf.maxDistance=150;audio.wolf.playOnAwake=false;
            audio.monsterVoice=monster.gameObject.AddComponent<AudioSource>();audio.monsterVoice.spatialBlend=1;audio.monsterVoice.playOnAwake=false;audio.monsterVoice.dopplerLevel=0;audio.monsterVoice.maxDistance=70;
            audio.monsterVoice.rolloffMode=AudioRolloffMode.Custom;audio.monsterVoice.SetCustomCurve(AudioSourceCurveType.CustomRolloff,AnimationCurve.Linear(0,1,1,1));
            audio.growlClip=Clip("walker_growl");audio.roarClip=Clip("walker_roar");
            audio.clickClip=Clip("creature_click");audio.pulseClip=Clip("threat_pulse");audio.howlClip=Clip("wolf_howl");return audio;
        }
    }
}
