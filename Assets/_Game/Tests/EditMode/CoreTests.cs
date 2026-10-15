using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests
{
    public sealed class CoreTests
    {
        [Test] public void A03_BatteryUsesSpecifiedSquareRootCurve()
        {
            var t=new FlashlightTuning();Assert.That(FlashlightModel.CalculateRange(100,t),Is.EqualTo(18).Within(.001));
            Assert.That(FlashlightModel.CalculateRange(10,t),Is.EqualTo(10.136619f).Within(.001));
            Assert.That(FlashlightModel.CalculateRange(0,t),Is.EqualTo(6.5f).Within(.001));
        }
        [Test] public void NormalBatteryLastsSevenActiveMinutes()
        {var f=new FlashlightModel(new FlashlightTuning());f.Tick(420,false);Assert.That(f.Charge,Is.EqualTo(0));Assert.False(f.Active);Assert.Zero(f.Range);}
        [Test] public void BoostStopsAfterThreeSecondsAndHasRecovery()
        {var f=new FlashlightModel(new FlashlightTuning());for(int i=0;i<31;i++)f.Tick(.1f,true);Assert.False(f.Boosting);Assert.That(f.BoostCooldown,Is.GreaterThan(4.8));}
        [Test] public void A07_ToggleDoesNotRearmGraceWithoutFiveProtectedSeconds()
        {
            var f=new FlashlightModel(new FlashlightTuning());f.ObserveProtection(true,1);f.Toggle();f.ObserveProtection(false,.1f);Assert.That(f.GraceRemaining,Is.EqualTo(1.2f));
            f.Tick(2,false);f.Toggle();f.ObserveProtection(true,.1f);f.Toggle();f.ObserveProtection(false,.1f);Assert.Zero(f.GraceRemaining);
            f.Toggle();f.ObserveProtection(true,5);f.Toggle();f.ObserveProtection(false,.1f);Assert.That(f.GraceRemaining,Is.EqualTo(1.2f));
        }
        [Test] public void BatterySwapConsumesOneCellAndPreservesOffState()
        {
            var f=new FlashlightModel(new FlashlightTuning());var inventory=new InventoryService();f.Restore(10,false);
            Assert.True(f.BeginSwap(inventory));Assert.False(f.BeginSwap(inventory));Assert.That(inventory.Batteries,Is.EqualTo(1));Assert.False(f.Active);
            f.Tick(2.5f,false);Assert.That(f.Charge,Is.EqualTo(100));Assert.False(f.Active);
        }
        [Test] public void A09_EmergencyChargeNeverCreatesSpareCellsOrExceedsFloor()
        {var f=new FlashlightModel(new FlashlightTuning());f.Restore(0,true);f.EmergencyFill();f.EmergencyFill();Assert.That(f.Charge,Is.EqualTo(35));f.Restore(70,true);f.EmergencyFill();Assert.That(f.Charge,Is.EqualTo(70));}
        [Test] public void A10_PartialStackLeavesRemainder()
        {var inventory=new InventoryService();inventory.AddBatteries(3);int pile=3;pile-=inventory.AddBatteries(pile);Assert.That(pile,Is.EqualTo(2));Assert.That(inventory.Batteries,Is.EqualTo(6));Assert.Zero(inventory.AddBatteries(pile));}
        [Test] public void ContinuousContactKillsAtThreeSecondsWithoutAnExtraWarningDelay()
        {
            var a=new MonsterAttack();var t=new MonsterTuning();Assert.False(a.Tick(2.99f,true,true,true,t));Assert.True(a.WindingUp);
            Assert.True(a.Tick(.011f,true,true,true,t));Assert.False(a.Tick(5,true,true,true,t));
        }
        [Test] public void A06_IlluminationCancelsWindupBeforeHit()
        {var a=new MonsterAttack();var t=new MonsterTuning();a.Tick(2,true,true,true,t);a.Tick(.1f,true,true,true,t);a.Tick(.6f,true,true,true,t);Assert.True(a.WindingUp);Assert.False(a.Tick(1,true,false,true,t));Assert.False(a.WindingUp);}
        [Test] public void A08_OneAttackCannotInflictDoubleDamage()
        {var a=new MonsterAttack();var t=new MonsterTuning();a.Tick(2,true,true,true,t);a.Tick(.1f,true,true,true,t);Assert.True(a.Tick(1.2f,true,true,true,t));for(int i=0;i<60;i++)Assert.False(a.Tick(.033f,true,true,true,t));}
        [Test] public void WoundDoesNotReduceMovementAndRecoversInShelter()
        {var condition=new PlayerCondition();var t=new PlayerTuning();Assert.True(condition.Hit(t));Assert.True(condition.Wounded);Assert.False(condition.Hit(t));condition.Tick(15,true,t);Assert.False(condition.Wounded);Assert.False(condition.Dead);}
        [Test] public void A12_ZeroTimeFreezesBatteryAndWindup()
        {var f=new FlashlightModel(new FlashlightTuning());var a=new MonsterAttack();var t=new MonsterTuning();a.Tick(2,true,true,true,t);a.Tick(.1f,true,true,true,t);float elapsed=a.WindupElapsed;for(int i=0;i<600;i++){f.Tick(0,true);a.Tick(0,true,true,true,t);}Assert.That(f.Charge,Is.EqualTo(100));Assert.That(a.WindupElapsed,Is.EqualTo(elapsed));}
        [Test] public void LeavingContactRangeResetsTheEntireThreeSeconds()
        {var a=new MonsterAttack();var t=new MonsterTuning();Assert.False(a.Tick(2.9f,true,true,true,t));Assert.False(a.Tick(.01f,true,true,false,t));Assert.Zero(a.WindupElapsed);Assert.False(a.Tick(2.9f,true,true,true,t));Assert.True(a.Tick(.11f,true,true,true,t));}
        [Test] public void LethalWalkerContactBypassesTemporaryBiteInvulnerability()
        {var condition=new PlayerCondition();condition.Restore(false);Assert.True(condition.Hit(new PlayerTuning(),true));Assert.True(condition.Dead);}
        static SaveData Snapshot()=>new SaveData{checkpoint="Ranger cabin",charge=23,batteries=5,documents=new[]{"note_guard_rules"},pickups=new[]{new ItemRecord{id="cabin_batteries",remaining=1}}};
        [Test] public void SaveRoundTripPreservesPartialPickups()
        {var data=Snapshot();var copy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));SaveService.Validate(copy);Assert.That(copy.pickups[0].remaining,Is.EqualTo(1));Assert.That(copy.charge,Is.EqualTo(23));}
        [Test] public void SaveRejectsImpossibleQuestState()
        {var data=Snapshot();data.completed=true;Assert.Throws<InvalidDataException>(()=>SaveService.Validate(data));data.powerOn=true;Assert.Throws<InvalidDataException>(()=>SaveService.Validate(data));data.gateKey=data.gateUnlocked=true;data.research.phase=5;data.research.health=0;SaveService.Validate(data);data.fuse=true;Assert.Throws<InvalidDataException>(()=>SaveService.Validate(data));}
        [TestCase("שלום 35% עולם","םלוע 35% םולש")]
        [TestCase("לחץ E לפתיחה","החיתפל E ץחל")]
        [TestCase("מסמך (12)","(12) ךמסמ")]
        [TestCase("שלום\nעולם","םולש\nםלוע")]
        public void HebrewKeepsNumbersControlsBracketsAndParagraphs(string logical,string visual)
        {Assert.That(HebrewText.Visual(logical),Is.EqualTo(visual));}
        [Test] public void LegacySaveMigratesWithoutGrantingMap()
        {
            string dir=Path.Combine(Path.GetTempPath(),"LastLightLegacy-"+Guid.NewGuid());string path=Path.Combine(dir,"save.json");
            try{Directory.CreateDirectory(dir);var data=Snapshot();data.schemaVersion=1;File.WriteAllText(path,JsonUtility.ToJson(data));var loaded=new SaveService(path).Load(out _);Assert.That(loaded.schemaVersion,Is.EqualTo(6));Assert.False(loaded.mapAcquired);Assert.False(loaded.axe);Assert.IsEmpty(loaded.deadWolves);Assert.That(loaded.charge,Is.EqualTo(23));}
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void MapAndRecoveredDocumentsSurviveSaveLoad()
        {
            var data=Snapshot();data.mapAcquired=true;data.documents=new[]{"note_guard_rules","note_cartographer","note_research"};
            var copy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));SaveService.Validate(copy);Assert.True(copy.mapAcquired);Assert.That(copy.documents,Does.Contain("note_cartographer"));
        }
        [Test] public void ThreatAndJawIncreaseWithRealProximity()
        {
            Assert.Zero(MonsterPresentation.DesiredOpening(30,false));Assert.That(MonsterPresentation.DesiredOpening(5,false),Is.GreaterThan(MonsterPresentation.DesiredOpening(14,false)));Assert.That(MonsterPresentation.DesiredOpening(30,true),Is.EqualTo(1));
            Assert.Zero(HorrorAudio.Proximity(40,false));Assert.That(HorrorAudio.Proximity(5,false),Is.GreaterThan(HorrorAudio.Proximity(20,false)));Assert.Zero(HorrorAudio.Proximity(3,true));
        }
        [Test] public void A14_CorruptPrimaryRecoversBackupWithoutOverwritingIt()
        {
            string dir=Path.Combine(Path.GetTempPath(),"LastLightTest-"+Guid.NewGuid());string path=Path.Combine(dir,"save.json");
            try
            {
                var service=new SaveService(path);var a=Snapshot();service.Write(a);a.charge=40;service.Write(a);
                File.WriteAllText(path,"{truncated");var recovered=service.Load(out string message);Assert.That(recovered.charge,Is.EqualTo(23));Assert.That(message,Does.Contain("backup"));
                string backup=File.ReadAllText(path+".bak");a.charge=60;service.Write(a);Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(backup));
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
    }
}
