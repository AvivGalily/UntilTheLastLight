using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class IntroAudioSetup
    {
        const string Opening="Assets/_Game/Audio/Opening/";
        [Serializable] sealed class EffectManifest { public Effect[] effects; }
        [Serializable] sealed class Effect
        {
            public string assetName,file,role;
            public bool loop;
            public int channels;
            public float duration;
        }
        [MenuItem("Last Light/Opening/Install supplied audio")]
        public static void InstallSupplied()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest=JsonUtility.FromJson<EffectManifest>(File.ReadAllText("Docs/Audio/ForestOpening_SuppliedAudio.json"));
            if(manifest.effects.Length!=4)throw new Exception("Expected four supplied opening effects.");
            foreach(var take in manifest.effects)
            {
                if(take.file!=Opening+take.assetName+".wav"||take.loop!=(take.role=="vehicle"))throw new Exception("Unexpected effect mapping: "+take.assetName);
                var importer=AssetImporter.GetAtPath(take.file) as AudioImporter;
                if(!importer)throw new Exception("Missing supplied effect: "+take.file);
                importer.forceToMono=false;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(take.file);
                if(!clip||clip.channels!=take.channels||Mathf.Abs(clip.length-take.duration)>.02f)throw new Exception("Supplied effect import changed channels or duration: "+take.assetName);
            }
            IntroVoiceSetup.Install();
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");
            var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if(!game||!game.intro||!game.intro.audioDirector)throw new Exception("Install forest opening before its supplied audio.");
            Wire(game.intro.audioDirector);
            EditorUtility.SetDirty(game.intro.audioDirector);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("FOREST INTRO SUPPLIED AUDIO INSTALLED: twelve complete English takes, four supplied effects, one ambience loop, single gate/approach/explosion cues, no previous core layers.");
        }
        public static void Wire(IntroAudioDirector audio)
        {
            // Only replace the effects group; passenger voice sources live in
            // the vehicle and must retain their spatial/speaker assignments.
            var previous=audio.transform.Find("Opening sound layers");
            if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root=new GameObject("Opening sound layers").transform;root.SetParent(audio.transform,false);
            AudioClip Supplied(string name)=>Load(Opening+name+".wav");
            AudioClip Foley(string name)=>Load("Assets/_Game/Audio/"+name+".wav");
            AudioSource Source(string name,AudioClip clip,bool loop,float spatial=0)
            {
                var go=new GameObject(name);go.transform.SetParent(root,false);
                var source=go.AddComponent<AudioSource>();source.clip=clip;source.loop=loop;source.playOnAwake=false;source.volume=0;
                source.spatialBlend=spatial;source.minDistance=3;source.maxDistance=45;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Logarithmic;
                if(spatial>0)go.AddComponent<AudioLowPassFilter>().cutoffFrequency=6500;
                return source;
            }
            audio.vehicleAmbience=Source("Supplied forest vehicle ambience",Supplied("SFX_ForestVehicleAmbience"),true);
            audio.gate=Source("Supplied gate closing",null,false);audio.gate.volume=.28f;
            audio.threat=Source("Supplied approaching creature",null,false,.85f);
            audio.effects=Source("Supplied vehicle explosion and wake foley",null,false);audio.effects.volume=.9f;
            audio.ringing=Source("Muffled tinnitus",Foley("intro_tinnitus"),true);
            audio.breath=Source("Interrupted breathing",Foley("player_breath_hard"),true);
            audio.gateClip=Supplied("SFX_ForestGate");audio.threatClip=Supplied("SFX_CreatureApproach");audio.blastClip=Supplied("SFX_VehicleExplosion");audio.wakeCreak=Foley("leaf_rustle");
        }
        static AudioClip Load(string path)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(!clip)throw new Exception("Opening audio missing: "+path);
            return clip;
        }
    }
}
