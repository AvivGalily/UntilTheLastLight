using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Game.Tests
{
    public sealed class EncounterLifecycleTests
    {
        [Test] public void ImportedDeathActuallyLowersTheHeadAndFoldsGroundedLegs()
        {
            const string path="Assets/_Game/Art/Meshes/cinder_head_01.fbx";
            var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var mesh=new Mesh();
            try
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name=="Head_Death");
                Assert.False(clip.isLooping);Assert.That(clip.length,Is.EqualTo(3.2f).Within(.04f));
                var skull=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head");
                var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();
                clip.SampleAnimation(instance,0);var start=skull.position;var rotation=skull.rotation;skin.BakeMesh(mesh);
                float top=mesh.vertices.Max(v=>skin.transform.TransformPoint(v).y);
                clip.SampleAnimation(instance,clip.length);skin.BakeMesh(mesh);
                Assert.That(skull.position.y,Is.LessThan(start.y-.12f),"death leaves the head standing");
                Assert.That(Quaternion.Angle(rotation,skull.rotation),Is.GreaterThan(60));
                Assert.That(mesh.vertices.Max(v=>skin.transform.TransformPoint(v).y),Is.LessThan(top-.25f));
                Assert.That(mesh.vertices.Min(v=>skin.transform.TransformPoint(v).y),Is.GreaterThan(-.10f),"collapse sinks through the ground");
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(instance);}
        }
        [Test] public void CadaverSkinHasHumanDetailAndEveryHeadSurfaceCanBurn()
        {
            foreach(string id in new[]{"CinderSkin","CinderLimb","CinderEyes","CinderGums","CinderTeeth","CinderThroat","CinderBone"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Generated/Materials/"+id+".mat");Assert.NotNull(m,id);
                Assert.NotNull(m.GetTexture("_BaseMap"),id);Assert.NotNull(m.GetTexture("_BumpMap"),id);
                Assert.True(m.HasProperty("_BurnAway"),id);Assert.True(m.HasProperty("_CharAmount"),id);
            }
            var skin=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Generated/Materials/CinderSkin.mat");
            Assert.That(skin.GetTexture("_BaseMap").name,Is.EqualTo("HumanSkin0_BaseColor"));Assert.That(skin.GetFloat("_CinderDecay"),Is.GreaterThan(.8f));
        }
        [Test] public void HeadScoreIsASeamlessOriginalLoop()
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/head_score.wav");Assert.NotNull(clip);Assert.That(clip.length,Is.EqualTo(12).Within(.001f));
            var samples=new float[clip.samples*clip.channels];Assert.True(clip.GetData(samples,0));
            Assert.That(Mathf.Abs(samples[0]-samples[samples.Length-1]),Is.LessThan(.03f));
            Assert.That(samples.Max(Mathf.Abs),Is.GreaterThan(.5f));
        }
    }
}
