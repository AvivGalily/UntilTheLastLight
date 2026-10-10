using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class CreatureTests
    {
        const string Model="Assets/_Game/Art/Meshes/walker_01.fbx";
        [Test] public void OralMaterialsHaveNativeTexturesAndNondegeneratePrimaryUvs()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/Walker.prefab");var renderer=prefab.GetComponent<MonsterPresentation>().mouth;var mesh=renderer.sharedMesh;
            foreach(string name in new[]{"Crawler wet gums","Crawler irregular teeth","Crawler throat"})
            {
                int index=Array.FindIndex(renderer.sharedMaterials,m=>m.name==name);Assert.That(index,Is.GreaterThanOrEqualTo(0));var material=renderer.sharedMaterials[index];
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                {var map=material.GetTexture(property);Assert.That(map,Is.Not.Null);Assert.That(map.width,Is.EqualTo(1254));Assert.That(map.height,Is.EqualTo(418));}
                Assert.Zero(material.GetFloat("_UseSkinAtlas"));Assert.Zero(material.GetFloat("_WorldSkin"));
                var points=mesh.GetTriangles(index).Select(v=>mesh.uv[v]).ToArray();
                Assert.That(points.Max(p=>p.x)-points.Min(p=>p.x),Is.GreaterThan(.01f));Assert.That(points.Max(p=>p.y)-points.Min(p=>p.y),Is.GreaterThan(.01f));
            }
        }
        [Test] public void JointSolverKeepsSegmentLengthsAndBendSide()
        {
            Vector3 hip=new Vector3(.3f,1.1f,.2f),foot=new Vector3(1.1f,.08f,.8f),bend=Vector3.up+Vector3.right;
            Vector3 knee=MonsterPresentation.SolveJoint(hip,foot,bend,.85f,.92f);
            Assert.That(Vector3.Distance(knee,hip),Is.EqualTo(.85f).Within(.0001f));
            Assert.That(Vector3.Distance(knee,foot),Is.EqualTo(.92f).Within(.0001f));
            Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(knee-hip,foot-hip),bend),Is.GreaterThan(0));
        }
        [Test] public void JointSolverHandlesStraightAndCollapsedTargetsWithoutNaN()
        {
            foreach(Vector3 foot in new[]{Vector3.zero,Vector3.down*8,Vector3.down})
            {
                Vector3 knee=MonsterPresentation.SolveJoint(Vector3.zero,foot,Vector3.down,.7f,.7f);
                Assert.True(float.IsFinite(knee.x)&&float.IsFinite(knee.y)&&float.IsFinite(knee.z));
                Assert.That(knee.magnitude,Is.EqualTo(.7f).Within(.0001f));
            }
        }
        [Test] public void ExportedCreatureHasWeightedLimbsTexturesAndLoopingClips()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/Walker.prefab");var p=prefab.GetComponent<MonsterPresentation>();
            Assert.True(p.HasRig);Assert.False(p.animator.applyRootMotion);Assert.That(p.mouth.sharedMesh.vertexCount,Is.LessThan(90000));
            Assert.That(p.mouth.bones.Length,Is.GreaterThanOrEqualTo(27));
            foreach(var t in p.legs.Concat(p.shins).Concat(p.feet))Assert.That(t,Is.Not.Null);
            Assert.That(p.idle.isLooping&&p.crawl.isLooping,Is.True);
            var skin=p.mouth.sharedMaterials.First(m=>m.name=="Crawler living skin");
            Assert.That(skin.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(skin.GetTexture("_BumpMap"),Is.Not.Null);
            foreach(var w in p.mouth.sharedMesh.boneWeights)Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1).Within(.001f));
        }
        [Test] public void CrawlMovesArticulatedLimbsStaysInPlaceAndLoops()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            try
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().First(c=>c.name=="Crawler_Crawl");
                var bones=go.GetComponentsInChildren<Transform>();var knee=bones.First(t=>t.name=="Limb_0_Lower");
                var foot=bones.First(t=>t.name=="Limb_0_Foot");var rig=bones.First(t=>t.name=="Root");
                clip.SampleAnimation(go,0);Vector3 first=foot.position,root=rig.position;Quaternion rest=knee.rotation;
                clip.SampleAnimation(go,clip.length*.25f);
                Assert.That(Quaternion.Angle(knee.rotation,rest),Is.GreaterThan(4));Assert.That(Vector3.Distance(rig.position,root),Is.LessThan(.001f));
                clip.SampleAnimation(go,clip.length);Assert.That(Vector3.Distance(foot.position,first),Is.LessThan(.02f));
                var mesh=new Mesh();go.GetComponentInChildren<SkinnedMeshRenderer>().BakeMesh(mesh);mesh.RecalculateBounds();
                var size=mesh.bounds.size;Assert.That(Mathf.Max(size.x,size.y,size.z),Is.LessThan(4.1f));
                foreach(var t in bones.Where(t=>t.name.EndsWith("_Foot")))
                {Assert.That(t.position.y,Is.GreaterThan(.04f));Assert.That(t.position.y,Is.LessThan(.32f));}
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void RuntimeCrawlFreezesWithZeroTimeAndNeverMovesAiRoot()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/Walker.prefab"));
            try
            {
                var p=go.GetComponent<MonsterPresentation>();var brain=go.GetComponent<MonsterBrain>();
                p.Simulate(.02f,brain);Vector3 root=go.transform.position;
                for(int i=0;i<30;i++){go.transform.position+=Vector3.forward*.02f;Vector3 moved=go.transform.position;p.Simulate(.02f,brain);Assert.That(go.transform.position,Is.EqualTo(moved));}
                Assert.That(p.GaitPhase,Is.GreaterThan(0));var feet=p.feet.Select(t=>t.position).ToArray();float phase=p.GaitPhase;
                p.Simulate(0,brain);Assert.That(p.GaitPhase,Is.EqualTo(phase));
                for(int i=0;i<8;i++)Assert.That(p.feet[i].position,Is.EqualTo(feet[i]));
            }
            finally{go.GetComponent<MonsterPresentation>().ReleaseRig();UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
