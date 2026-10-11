using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class AtmosphereTests
    {
        [Test] public void WildlifeRetainsDirectionalMonoVoices()
        {
            foreach(string name in new[]{"night_owl","nightjar_calls","forest_frogs","leaf_rustle"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+name+".wav");
                Assert.That(clip,Is.Not.Null);Assert.That(clip.channels,Is.EqualTo(1));Assert.That(clip.length,Is.GreaterThan(1));
            }
        }
        [Test] public void ResetKeepsCricketClipButDoesNotStartItBeforeGameplay()
        {
            var go=new GameObject("Legacy insect ambience");
            try
            {
                var ambience=go.AddComponent<ForestAmbience>();ambience.insects=go.AddComponent<AudioSource>();
                ambience.insects.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/night_crickets.wav");
                ambience.insects.volume=.14f;ambience.insects.playOnAwake=true;
                ambience.ResetSequence(1407);
                Assert.That(ambience.insects.clip.name,Is.EqualTo("night_crickets"));Assert.Zero(ambience.insects.volume);
                Assert.That(ambience.cricketVolume,Is.LessThanOrEqualTo(.015f));Assert.True(ambience.insects.loop);
                Assert.False(ambience.insects.playOnAwake);Assert.False(ambience.insects.isPlaying);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void CricketsFadeBeyondWallsAndStaySilentOnForestTrails()
        {
            foreach(var building in new[]{ForestLayout.Cabin,ForestLayout.Store,ForestLayout.Research})
            {
                float wall=building==ForestLayout.Research?7:5;
                Assert.That(ForestAmbience.CricketZone(building+Vector3.up*1.7f),Is.EqualTo(1));
                float near=ForestAmbience.CricketZone(building+Vector3.forward*(wall+3));
                float far=ForestAmbience.CricketZone(building+Vector3.forward*(wall+8));
                Assert.That(near,Is.GreaterThan(far));Assert.That(far,Is.GreaterThan(0));
                Assert.Zero(ForestAmbience.CricketZone(building+Vector3.forward*(wall+10)));
            }
            foreach(var point in new[]{ForestLayout.Lantern,ForestLayout.Gate,ForestLayout.EncounterTree,new Vector3(15,0,-38),new Vector3(800,0,0)})
                Assert.Zero(ForestAmbience.CricketZone(point),point.ToString());
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
