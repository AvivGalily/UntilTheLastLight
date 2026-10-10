using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Game.Tests
{
    public sealed class NortonTests
    {
        [Test] public void TwoSeparateFireballsAreFatalDuringRecovery()
        {var c=new PlayerCondition();c.Restore(false);Assert.True(c.HitFire());Assert.True(c.Wounded);Assert.False(c.Dead);c.Tick(1,false,new PlayerTuning());Assert.True(c.HitFire());Assert.True(c.Dead);}
        [Test] public void FireRecoversAfterTenSecondsOutsideShelter()
        {var c=new PlayerCondition();c.Restore(false);c.HitFire();c.Tick(9.99f,false,new PlayerTuning());Assert.True(c.Wounded);c.Tick(.011f,false,new PlayerTuning());Assert.False(c.Wounded);Assert.True(c.HitFire());Assert.False(c.Dead);}
        [Test] public void SameContactCannotDoubleHitAndZeroTimeDoesNotHeal()
        {var c=new PlayerCondition();c.HitFire();Assert.False(c.HitFire());c.Tick(0,true,new PlayerTuning());Assert.That(c.FireRecoveryRemaining,Is.EqualTo(10));Assert.False(c.Dead);}
        [Test] public void FireHealingDoesNotRemoveAnOrdinaryBiteWound()
        {var c=new PlayerCondition();var t=new PlayerTuning();c.Hit(t);c.HitFire();c.Tick(10,false,t);Assert.True(c.PhysicalWound);Assert.True(c.Wounded);Assert.That(c.FireRecoveryRemaining,Is.Zero);}
        [Test] public void ShotgunUsesOneShellAndReloadPreservesTotalAmmo()
        {var i=new InventoryService();i.AcquireShotgun();Assert.That(i.LoadedShells+i.SpareShells,Is.EqualTo(14));Assert.True(i.FireShell());Assert.True(i.FireShell());Assert.False(i.FireShell());Assert.That(i.ReloadShotgun(),Is.EqualTo(2));Assert.That(i.LoadedShells+i.SpareShells,Is.EqualTo(12));}
        [Test] public void StowedShotgunCannotFireAndDuplicatePickupDoesNotRefill()
        {var i=new InventoryService();i.AcquireShotgun();i.FireShell();i.AcquireShotgun();Assert.That(i.LoadedShells,Is.EqualTo(1));i.Weapon=HeldWeapon.Axe;Assert.False(i.FireShell());}
        [Test] public void HeadHasSixArticulatedLegsAndFiveInPlaceClips()
        {
            string path="Assets/_Game/Art/Meshes/cinder_head_01.fbx";var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.NotNull(model);
            var bones=model.GetComponentsInChildren<Transform>();Assert.That(bones.Count(t=>t.name.StartsWith("Leg_")&&t.name.EndsWith("_0")),Is.EqualTo(6));
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();Assert.That(clips.Length,Is.EqualTo(5));Assert.That(clips.Single(c=>c.name=="Head_Spit").length,Is.EqualTo(1.6f).Within(.04f));
            Assert.False(model.GetComponentInChildren<Animator>().applyRootMotion);
        }
        [Test] public void VersionFourMigrationRetainsForestProgressWithoutInventingNewQuest()
        {
            string directory=Path.Combine(Path.GetTempPath(),"norton-migration-"+Guid.NewGuid());Directory.CreateDirectory(directory);
            try{var s=new SaveData{schemaVersion=4,checkpoint="Ranger cabin",charge=60,axe=true,mapAcquired=true,forestEncounterSeen=true,documents=new[]{"note_cartographer"},pickups=new ItemRecord[0]};string path=Path.Combine(directory,"save.json");File.WriteAllText(path,JsonUtility.ToJson(s));var loaded=new SaveService(path).Load(out _);Assert.That(loaded.schemaVersion,Is.EqualTo(5));Assert.True(loaded.axe&&loaded.mapAcquired&&loaded.forestEncounterSeen);Assert.False(loaded.shotgun||loaded.headUnlocked);}
            finally{Directory.Delete(directory,true);}
        }
        [Test] public void NewWeaponAndBurnStateRoundTrip()
        {
            string directory=Path.Combine(Path.GetTempPath(),"norton-save-"+Guid.NewGuid());
            try{var service=new SaveService(Path.Combine(directory,"save.json"));service.Write(new SaveData{checkpoint="checkpoint_norton",charge=43,shotgun=true,loadedShells=1,spareShells=7,weapon=1,headUnlocked=true,headIntroduced=true,fireRecovery=6.2f,documents=new[]{NortonEncounter.Letter},pickups=new[]{new ItemRecord{id="norton_shotgun",remaining=0}}});var s=service.Load(out _);Assert.True(s.headIntroduced&&s.shotgun);Assert.That(s.loadedShells,Is.EqualTo(1));Assert.That(s.spareShells,Is.EqualTo(7));Assert.That(s.fireRecovery,Is.EqualTo(6.2f));}
            finally{Directory.Delete(directory,true);}
        }
        [TestCase(-1,2,0)] [TestCase(3,2,0)] [TestCase(1,31,0)] [TestCase(1,2,11)]
        public void InvalidAmmoAndFireRecoveryAreRejected(int loaded,int spare,float recovery)
        {Assert.Throws<InvalidDataException>(()=>SaveService.Validate(new SaveData{checkpoint="Ranger cabin",shotgun=true,loadedShells=loaded,spareShells=spare,fireRecovery=recovery,documents=new string[0],pickups=new ItemRecord[0]}));}
    }
}
