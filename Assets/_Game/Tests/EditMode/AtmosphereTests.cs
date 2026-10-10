using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class AtmosphereTests
    {
        [Test] public void WildlifeAudioHasStereoLoopAndDirectionalMonoVoices()
        {
            var insects=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/night_crickets.wav");
            Assert.That(insects.channels,Is.EqualTo(2));Assert.That(insects.length,Is.EqualTo(18).Within(.01f));
            foreach(string name in new[]{"night_owl","nightjar_calls","forest_frogs","leaf_rustle"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+name+".wav");
                Assert.That(clip,Is.Not.Null);Assert.That(clip.channels,Is.EqualTo(1));Assert.That(clip.length,Is.GreaterThan(1));
            }
        }
        [Test] public void BuildingInteriorMaskLeavesTrailsAndDoorApproachOutside()
        {
            Assert.True(ForestLayout.InBuilding(ForestLayout.Cabin+Vector3.up*1.7f));
            Assert.True(ForestLayout.InBuilding(ForestLayout.Research+new Vector3(7,1.7f,-5)));
            Assert.False(ForestLayout.InBuilding(ForestLayout.Research+Vector3.forward*9));
            Assert.False(ForestLayout.InBuilding(ForestLayout.Lantern));
        }
        [Test] public void PausedDirectorsDoNotInitializeOrAdvance()
        {
            var go=new GameObject("Frozen directors");
            try
            {
                var wildlife=go.AddComponent<ForestAmbience>();var fog=go.AddComponent<ForestAtmosphere>();
                wildlife.Simulate(0,null);fog.Simulate(0,null);
                Assert.Zero(wildlife.ActiveTime);Assert.Zero(wildlife.BirdCalls);Assert.Zero(fog.ActiveTime);
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
