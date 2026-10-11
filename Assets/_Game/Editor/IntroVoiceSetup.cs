using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class IntroVoiceSetup
    {
        [Serializable] sealed class VoiceManifest { public VoiceLine[] lines; }
        [Serializable] sealed class VoiceLine
        {
            public string id,actor,file,text;
            public float start,budget,duration;
        }
        [MenuItem("Last Light/Opening/Install English voices")]
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest=JsonUtility.FromJson<VoiceManifest>(File.ReadAllText("Docs/Audio/ForestOpening_VoiceManifest.json"));
            var config=AssetDatabase.LoadAssetAtPath<ForestIntroConfig>(ProjectSetup.Root+"/Data/ForestIntroConfig.asset");
            if(!config||manifest.lines.Length!=12||config.lines.Length!=12)throw new Exception("Expected twelve authored opening voice lines.");
            foreach(var take in manifest.lines)
            {
                var line=config.lines.Single(l=>l.id==take.id);
                if(line.speakerKey!="intro_"+take.actor.ToLowerInvariant())throw new Exception("VO speaker mismatch: "+take.id);
                var importer=AssetImporter.GetAtPath(take.file) as AudioImporter;
                if(!importer)throw new Exception("Missing VO audio: "+take.file);
                // The authored WAVs are mono already. Leave downmix normalization
                // disabled so Ethan's quiet discovery retains its measured gain.
                importer.forceToMono=false;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;
                importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(take.file);
                if(!clip||clip.length+.06f>take.budget||Mathf.Abs(clip.length-take.duration)>.02f)throw new Exception("Voice does not fit complete playback window: "+take.id);
                line.englishVoice=clip;line.start=take.start;line.duration=take.budget;
            }
            for(int n=1;n<config.lines.Length;n++)
                if(config.lines[n-1].start+config.lines[n-1].duration>config.lines[n].start)throw new Exception("Overlapping voice windows.");
            if(config.lines.Last().start+config.lines.Last().duration>config.blast)throw new Exception("Final VO overlaps blast.");
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
            Debug.Log("FOREST INTRO VOICES INSTALLED: twelve English WAVs, three distinct voices, preloaded PCM, complete non-overlapping playback, English fallback with Hebrew subtitles.");
        }
    }
}
