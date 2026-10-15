using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Game.Tests
{
    public sealed class ResearchEncounterTests
    {
        [TestCase(4.99f,true)] [TestCase(5f,false)] [TestCase(5.1f,false)]
        public void AquaticHitsUseStrictFiveSecondWindow(float gap,bool fatal)
        {
            var p=new PlayerCondition();p.Restore(false);Assert.True(p.HitAquatic());Assert.False(p.Dead);
            p.Tick(gap,false,new PlayerTuning());Assert.True(p.HitAquatic());Assert.That(p.Dead,Is.EqualTo(fatal));
        }
        [Test] public void PausedAquaticWoundsDoNotExpireOrChangeOtherInjuries()
        {
            var p=new PlayerCondition();p.Restore(true,7,4);p.Tick(0,true,new PlayerTuning());Assert.That(p.AquaticRecoveryRemaining,Is.EqualTo(4));
            p.Tick(5,false,new PlayerTuning());Assert.Zero(p.AquaticRecoveryRemaining);Assert.True(p.PhysicalWound);Assert.That(p.FireRecoveryRemaining,Is.EqualTo(2));
        }
        [Test] public void ResearchProgressAndKeyRoundTripAndRejectImpossibleCompletion()
        {
            var s=new SaveData{checkpoint="Research",charge=40,documents=new string[0],pickups=new ItemRecord[0],powerOn=true,gateKey=true,gateUnlocked=true,aquaticRecovery=3,research=new ResearchSave{phase=5,health=0,x=-38,z=34}};
            var copy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));SaveService.Validate(copy);Assert.True(copy.gateKey&&copy.gateUnlocked);Assert.That(copy.research.x,Is.EqualTo(-38));Assert.That(copy.aquaticRecovery,Is.EqualTo(3));
            copy.gateKey=false;Assert.Throws<InvalidDataException>(()=>SaveService.Validate(copy));copy.gateKey=true;copy.research.health=1;Assert.Throws<InvalidDataException>(()=>SaveService.Validate(copy));
        }
        [TestCase(false)] [TestCase(true)] public void VersionFiveMigrationKeepsProgressWithoutOpeningAnUnfinishedGate(bool completed)
        {
            string folder=Path.Combine(Path.GetTempPath(),"LastLightResearchV5-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                var s=new SaveData{schemaVersion=5,checkpoint="Old circuit",charge=44,powerOn=true,completed=completed,documents=new string[0],pickups=new ItemRecord[0]};
                string path=Path.Combine(folder,"save.json");File.WriteAllText(path,JsonUtility.ToJson(s));var loaded=new SaveService(path).Load(out _);
                Assert.That(loaded.schemaVersion,Is.EqualTo(6));Assert.That(loaded.gateKey,Is.EqualTo(completed));Assert.That(loaded.gateUnlocked,Is.EqualTo(completed));Assert.That(loaded.research.phase,Is.EqualTo(completed?5:0));SaveService.Validate(loaded);
            }
            finally{Directory.Delete(folder,true);}
        }
        [Test] public void AquaticRigHasAnimatedEscapeLocomotionAndSettledDeath()
        {
            const string path="Assets/_Game/Art/Meshes/research_abomination_01.fbx";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=UnityEngine.Object.Instantiate(source);
            try
            {
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();Assert.That(clips.Length,Is.EqualTo(6));
                var escape=clips.First(c=>c.name=="Aquatic_Escape");Assert.That(escape.length,Is.EqualTo(4).Within(.04));Assert.False(escape.isLooping);
                var arm=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="UpperArm_L");escape.SampleAnimation(instance,0);var start=arm.rotation;escape.SampleAnimation(instance,1.3f);Assert.That(Quaternion.Angle(start,arm.rotation),Is.GreaterThan(20));
                var death=clips.First(c=>c.name=="Aquatic_Death");var root=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="AquaticRoot");death.SampleAnimation(instance,0);start=root.rotation;death.SampleAnimation(instance,death.length);Assert.That(Quaternion.Angle(start,root.rotation),Is.GreaterThan(70));
                var mesh=instance.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;Assert.That(mesh.boneWeights.Count(w=>w.weight0>0),Is.EqualTo(mesh.vertexCount));
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        [Test] public void AquaticAttackJawOpensAndRecoversWithoutSnapping()
        {
            const string path="Assets/_Game/Art/Meshes/research_abomination_01.fbx";
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Aquatic_Attack");
                var jaw=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="Jaw");
                clip.SampleAnimation(instance,0);Quaternion start=jaw.localRotation,previous=start;float opening=0;
                int steps=Mathf.CeilToInt(clip.length*120);
                for(int i=1;i<=steps;i++)
                {
                    clip.SampleAnimation(instance,clip.length*i/steps);
                    Assert.That(Quaternion.Angle(previous,jaw.localRotation),Is.LessThan(2),"jaw snaps during attack/recovery");
                    opening=Mathf.Max(opening,Quaternion.Angle(start,jaw.localRotation));previous=jaw.localRotation;
                }
                Assert.That(opening,Is.GreaterThan(35));Assert.That(Quaternion.Angle(start,jaw.localRotation),Is.LessThan(.15f));
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        [Test] public void AquaticStrideLoopsWithoutAPoseJump()
        {
            const string path="Assets/_Game/Art/Meshes/research_abomination_01.fbx";
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Aquatic_Run");
                var bones=instance.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Thigh_")||t.name.StartsWith("Shin_")||t.name.StartsWith("Foot_")||t.name=="Pelvis").ToArray();
                clip.SampleAnimation(instance,0);var start=bones.Select(t=>t.localRotation).ToArray();var positions=bones.Select(t=>t.position).ToArray();
                clip.SampleAnimation(instance,clip.length*.25f);
                Assert.That(bones.Select((t,i)=>Vector3.Distance(positions[i],t.position)).Max(),Is.GreaterThan(.15f),"stride must move the leg skeleton");
                clip.SampleAnimation(instance,clip.length-.00001f);
                for(int i=0;i<bones.Length;i++){Assert.That(Quaternion.Angle(start[i],bones[i].localRotation),Is.LessThan(.15f),bones[i].name);Assert.That(Vector3.Distance(positions[i],bones[i].position),Is.LessThan(.001f),bones[i].name);}
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
    }
}
