using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public sealed class IntroDialogueTests
    {
        [UnityTest] public IEnumerator SubtitleWindowEndingDoesNotCutOffAnAlreadyPlayingTake()
        {
            var root=new GameObject("intro voice continuity test");
            var config=ScriptableObject.CreateInstance<ForestIntroConfig>();
            var clip=AudioClip.Create("complete supplied take",48000,1,24000,false);
            var english=new TextAsset("{\"entries\":[]}");var hebrew=new TextAsset("{\"entries\":[]}");
            var locale=new Localization(english,hebrew);bool previous=locale.Hebrew;
            try
            {
                var dialogue=root.AddComponent<IntroDialogueController>();
                var label=new GameObject("subtitle",typeof(RectTransform));label.transform.SetParent(root.transform);
                dialogue.subtitle=label.AddComponent<TextMeshProUGUI>();dialogue.subtitlePanel=label;
                var source=root.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=0;
                dialogue.voices=new[]{source};dialogue.passengers=new IntroPassenger[0];
                config.lines=new[]{new IntroLine{id="L01",speakerKey="intro_tyler",passenger=0,start=1,duration=2.15f,englishVoice=clip}};
                dialogue.Simulate(1.1f,config,locale,true);yield return null;
                Assert.That(source.isPlaying,Is.True,"Take did not start");
                // A delayed frame crosses the subtitle end before wall-clock
                // playback has completed. Its remaining words must survive.
                dialogue.Simulate(3.2f,config,locale,true);
                Assert.That(label.activeSelf,Is.False,"Expired subtitle stayed visible");
                Assert.That(source.isPlaying,Is.True,"Subtitle expiry interrupted the complete take");
                dialogue.ResetDialogue();
                Assert.That(source.isPlaying,Is.False,"Cancellation left voice playing");
            }
            finally
            {
                locale.SetHebrew(previous);
                Object.DestroyImmediate(root);Object.DestroyImmediate(config);Object.DestroyImmediate(clip);Object.DestroyImmediate(english);Object.DestroyImmediate(hebrew);
            }
        }
    }
}
