using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Game.Tests
{
    public sealed class CombatTests
    {
        [Test] public void FatalAnimationLocksGameplayButKeepsCameraEffectsAndAudioRunning()
        {
            var actions=ScriptableObject.CreateInstance<InputActionAsset>();actions.AddActionMap("Gameplay").AddAction("Move");
            var state=new GameStateService(actions);
            try
            {
                state.Set(ScreenMode.Playing);Assert.True(actions.FindActionMap("Gameplay").enabled);
                state.Set(ScreenMode.Dying);Assert.False(state.Running);Assert.False(actions.FindActionMap("Gameplay").enabled);
                Assert.That(Time.timeScale,Is.EqualTo(1));Assert.False(AudioListener.pause);
                state.Set(ScreenMode.Dead);Assert.That(Time.timeScale,Is.Zero);Assert.True(AudioListener.pause);
            }
            finally{state.Dispose();UnityEngine.Object.DestroyImmediate(actions);}
        }
        [Test] public void PlayerShadowAndGripContainTexturedHumanAnatomy()
        {
            foreach(string id in new[]{"player_body_01","player_axe_hand_01","player_shotgun_hand_01"})
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Meshes/"+id+".fbx");Assert.That(source,Is.Not.Null);
                var meshes=source.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).Concat(source.GetComponentsInChildren<SkinnedMeshRenderer>().Select(m=>m.sharedMesh)).ToArray();Assert.That(meshes.Sum(m=>m.vertexCount),Is.GreaterThan(id.Contains("hand")?300:8000));
                Assert.True(meshes.All(m=>m.uv.Length==m.vertexCount));
                Assert.True(source.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Any(m=>id.Contains("hand")?m.name=="PlayerSkin":m.name.StartsWith("HumanSkin")));
            }
        }
        [Test] public void WolfDeathHasArticulatedCollapseAndFiniteSettledPose()
        {
            const string path="Assets/_Game/Art/Meshes/zombie_wolf_01.fbx";
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var death=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Wolf_Death");
                var root=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="b_Root");
                var foreleg=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="b_LeftForeArm");
                death.SampleAnimation(instance,0);var upright=root.rotation;var leg=foreleg.localRotation;
                death.SampleAnimation(instance,.8f);Assert.That(Quaternion.Angle(upright,root.rotation),Is.GreaterThan(70));Assert.That(Quaternion.Angle(leg,foreleg.localRotation),Is.GreaterThan(30));
                death.SampleAnimation(instance,death.length);foreach(var bone in instance.GetComponentsInChildren<Transform>())Assert.False(float.IsNaN(bone.position.x)||float.IsInfinity(bone.position.y));
                Assert.That(death.length,Is.GreaterThan(2));Assert.False(death.isLooping);
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        [Test] public void ApproachingBarksGetLouderAndMoreFrequentWithinValidLimits()
        {
            float far=ZombieWolf.BarkGain(25),middle=ZombieWolf.BarkGain(12),close=ZombieWolf.BarkGain(2);
            Assert.That(middle,Is.GreaterThan(far));Assert.That(close,Is.GreaterThan(middle));
            Assert.That(ZombieWolf.BarkGain(0),Is.InRange(0f,1f));Assert.That(ZombieWolf.BarkGain(100),Is.InRange(0f,1f));
            Assert.That(ZombieWolf.BarkInterval(2),Is.LessThan(ZombieWolf.BarkInterval(25)));
        }
        [Test] public void EnemyVoiceRecordingsAreMonoAndPlayable()
        {
            foreach(string name in new[]{"wolf_bark","walker_growl","walker_roar"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+name+".wav");
                Assert.That(clip,Is.Not.Null);Assert.That(clip.channels,Is.EqualTo(1));Assert.That(clip.length,Is.GreaterThan(.2f));
            }
        }
        [Test] public void FreshPlayerDiesToWalkerButNotToOneWolfBite()
        {
            var tuning=new PlayerTuning();var person=new PlayerCondition();Assert.True(person.Hit(tuning,true));Assert.True(person.Dead);Assert.False(person.Hit(tuning,true));
            person=new PlayerCondition();Assert.True(person.Hit(tuning));Assert.True(person.Wounded);Assert.False(person.Dead);person.Tick(7,false,tuning);Assert.True(person.Hit(tuning));Assert.True(person.Dead);
        }
        [Test] public void SchemaTwoPreservesMapWhileAddingEmptyCombatProgress()
        {
            string dir=Path.Combine(Path.GetTempPath(),"LastLightV2-"+Guid.NewGuid());Directory.CreateDirectory(dir);
            try
            {
                var data=new SaveData{schemaVersion=2,checkpoint="Ranger cabin",charge=37,mapAcquired=true,documents=new[]{"note_cartographer"},pickups=new ItemRecord[0]};
                string path=Path.Combine(dir,"save.json");File.WriteAllText(path,JsonUtility.ToJson(data));var loaded=new SaveService(path).Load(out _);
                Assert.That(loaded.schemaVersion,Is.EqualTo(6));Assert.True(loaded.mapAcquired);Assert.False(loaded.axe);Assert.IsEmpty(loaded.deadWolves);Assert.That(loaded.charge,Is.EqualTo(37));
            }
            finally{Directory.Delete(dir,true);}
        }
        [Test] public void CombatSaveRoundTripRejectsDuplicateKills()
        {
            var data=new SaveData{checkpoint="Ranger cabin",charge=17,axe=true,deadWolves=new[]{"undead_wolf_0","undead_wolf_3"},documents=new string[0],pickups=new[]{new ItemRecord{id="surveyor_axe",remaining=0}}};
            var copy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));SaveService.Validate(copy);Assert.True(copy.axe);Assert.That(copy.deadWolves,Is.EqualTo(data.deadWolves));
            copy.deadWolves=new[]{"undead_wolf_0","undead_wolf_0"};Assert.Throws<InvalidDataException>(()=>SaveService.Validate(copy));
        }
        [Test] public void AllNineWolfKillsSurviveTheExistingSaveSchema()
        {
            var data=new SaveData{checkpoint="Ranger cabin",charge=17,axe=true,deadWolves=Enumerable.Range(0,9).Select(n=>"undead_wolf_"+n).ToArray(),documents=new string[0],pickups=new ItemRecord[0]};
            var copy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));SaveService.Validate(copy);
            Assert.That(copy.deadWolves,Is.EqualTo(data.deadWolves));Assert.That(copy.deadWolves[8],Is.EqualTo("undead_wolf_8"));
        }
        [Test] public void WolfClipsContainSkeletalMotionAndProperLoopFlags()
        {
            const string path="Assets/_Game/Art/Meshes/zombie_wolf_01.fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=UnityEngine.Object.Instantiate(model);
            try
            {
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();Assert.That(clips.Length,Is.EqualTo(4));
                var run=clips.First(c=>c.name=="Wolf_Run");Assert.True(run.isLooping);Assert.False(clips.First(c=>c.name=="Wolf_Death").isLooping);
                var leg=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="b_LeftForeArm");run.SampleAnimation(instance,0);var start=leg.rotation;
                run.SampleAnimation(instance,run.length*.25f);Assert.That(Quaternion.Angle(start,leg.rotation),Is.GreaterThan(10));
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        [Test] public void InjuryAudioAndSurfaceStepsAreImported()
        {
            foreach(string id in new[]{"player_scream","player_hurt","player_breath_calm","player_breath_hard","axe_swing","axe_impact"})
            {var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/"+id+".wav");Assert.That(clip,Is.Not.Null);Assert.That(clip.length,Is.GreaterThan(.1f));}
            Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/step_forest_1.ogg"),Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/step_wood_1.ogg"),Is.Not.Null);
        }
        [Test] public void WolfBiteClosesJawAtDamageTimeAndKeepsSkullWeightedAndTextured()
        {
            const string path="Assets/_Game/Art/Meshes/zombie_wolf_01.fbx";
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var bite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name=="Wolf_Bite");
                var jaw=instance.GetComponentsInChildren<Transform>().First(t=>t.name=="b_Jaw");
                bite.SampleAnimation(instance,.34f);var open=jaw.localRotation;
                bite.SampleAnimation(instance,.60f);Assert.That(Quaternion.Angle(open,jaw.localRotation),Is.GreaterThan(30));
                var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.True(skin.sharedMaterials.Any(m=>m.name.StartsWith("WolfBone")));Assert.True(skin.sharedMaterials.Any(m=>m.name.StartsWith("WolfSocket")));
                int boneSlot=Array.FindIndex(skin.sharedMaterials,m=>m.name.StartsWith("WolfBone"));
                var mesh=skin.sharedMesh;var uvs=mesh.uv;
                var boneUVs=mesh.GetIndices(boneSlot).Distinct().Select(index=>uvs[index]).ToArray();
                Assert.That(boneUVs.Max(uv=>uv.x)-boneUVs.Min(uv=>uv.x),Is.GreaterThan(.25f),"Skull primary UVs collapse to a single texture sample");
                Assert.That(boneUVs.Max(uv=>uv.y)-boneUVs.Min(uv=>uv.y),Is.GreaterThan(.25f),"Skull primary UVs collapse to a single texture sample");
                Assert.That(skin.sharedMesh.boneWeights.Count(w=>w.weight0>.9f&&skin.bones[w.boneIndex0].name=="b_Jaw"),Is.GreaterThan(40));
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        [Test] public void WalkerBiteOpensThenClosesBeforeFatalContact()
        {
            Assert.That(MonsterPresentation.BiteOpening(.62f),Is.GreaterThan(.98f));
            Assert.That(MonsterPresentation.BiteOpening(.94f),Is.LessThan(.15f));
            Assert.That(MonsterPresentation.BiteOpening(1),Is.LessThan(.07f));
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Meshes/walker_01.fbx");
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;int index=HeroTree.ShapeIndex(mesh,"Jaw_Bite");Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var delta=new Vector3[mesh.vertexCount];int frame=mesh.GetBlendShapeFrameCount(index)-1;mesh.GetBlendShapeFrameVertices(index,frame,delta,null,null);
            Assert.That(delta.Count(v=>skin.transform.TransformVector(v).magnitude>.08f),Is.GreaterThan(200));
        }
    }
}
