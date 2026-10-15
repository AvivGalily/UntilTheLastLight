using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests
{
    public sealed class ForestEncounterTests
    {
        [Test] public void SilkFollowsBonesReleasesOnceFreezesAndRestores()
        {
            var root=new GameObject("Web test");
            try
            {
                var web=root.AddComponent<VictimWeb>();var core=new GameObject("Core",typeof(MeshRenderer));core.transform.SetParent(root.transform);web.intactCore=core.GetComponent<Renderer>();
                var bone=new GameObject("Bone");bone.transform.SetParent(root.transform);var strand=new GameObject("Strand",typeof(LineRenderer));strand.transform.SetParent(root.transform);var line=strand.GetComponent<LineRenderer>();line.positionCount=3;
                web.supports=new[]{line};web.supportBones=new[]{bone.transform};web.boneOffsets=new[]{Vector3.zero};web.fixedAnchors=new[]{Vector3.up};
                web.bodyBands=Array.Empty<LineRenderer>();web.bandBones=Array.Empty<Transform>();web.bandOffsets=Array.Empty<Vector3>();web.bandHalfWidths=Array.Empty<float>();
                web.ResetWeb(false);Vector3 initial=line.GetPosition(2);bone.transform.position=Vector3.right;web.Simulate(0);Assert.That(line.GetPosition(2),Is.EqualTo(initial));
                web.Simulate(.1f);Assert.That(line.GetPosition(2),Is.EqualTo(Vector3.right));web.Release();web.Release();Assert.That(web.TearCount,Is.EqualTo(1));Assert.False(web.intactCore.enabled);
                web.Simulate(0);Assert.Zero(web.TearSeconds);web.Simulate(.7f);Assert.False(line.enabled);
                web.ResetWeb(true);Assert.True(web.Released);Assert.False(line.enabled);web.ResetWeb(false);Assert.False(web.Released);Assert.True(line.enabled);Assert.True(web.intactCore.enabled);Assert.Zero(web.TearCount);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void FaultCannotBeBypassedBySwitchBoostCellOrOutdoorCharger()
        {
            var light=new FlashlightModel(new FlashlightTuning());var pack=new InventoryService();pack.Restore(3,false);
            light.BeginSwap(pack);light.FailCircuit();int cells=pack.Batteries;
            light.Toggle();Assert.False(light.BeginSwap(pack,true));light.EmergencyFill();light.Tick(10,true);
            Assert.False(light.Active);Assert.False(light.Boosting);Assert.Zero(light.Charge);Assert.That(pack.Batteries,Is.EqualTo(cells));
            Assert.False(light.RepairAtHouse(false));Assert.True(light.Faulted);
            Assert.True(light.RepairAtHouse(true));Assert.True(light.Active);Assert.That(light.Charge,Is.EqualTo(35));
        }
        [Test] public void NewGameRestoreClearsCircuitFailure()
        {var light=new FlashlightModel(new FlashlightTuning());light.FailCircuit();light.Restore(100,true);Assert.False(light.Faulted);Assert.True(light.Active);}
        [Test] public void OnlyHouseInteriorsEndEscape()
        {
            Assert.True(ForestLayout.InRefugeHouse(ForestLayout.Cabin));Assert.True(ForestLayout.InRefugeHouse(ForestLayout.Store));
            Assert.False(ForestLayout.InRefugeHouse(ForestLayout.Research));Assert.False(ForestLayout.InRefugeHouse(ForestLayout.Lantern));
            Assert.False(ForestLayout.InRefugeHouse(ForestLayout.Cabin+Vector3.forward*4.2f));
        }
        [Test] public void EncounterAndFaultRoundTripWhileVersionThreeMigrates()
        {
            string dir=Path.Combine(Path.GetTempPath(),"ForestEncounterTest-"+Guid.NewGuid());string path=Path.Combine(dir,"save.json");
            try
            {
                var service=new SaveService(path);var data=new SaveData{checkpoint="checkpoint_blackout",charge=0,flashlightOn=false,forestEncounterSeen=true,flashlightFaulted=true,documents=new string[0],pickups=new ItemRecord[0]};
                service.Write(data);var loaded=service.Load(out _);Assert.True(loaded.forestEncounterSeen);Assert.True(loaded.flashlightFaulted);
                data.schemaVersion=3;data.charge=52;data.flashlightOn=true;service.Write(data);loaded=service.Load(out _);
                Assert.That(loaded.schemaVersion,Is.EqualTo(6));Assert.False(loaded.forestEncounterSeen);Assert.False(loaded.flashlightFaulted);Assert.That(loaded.charge,Is.EqualTo(52));
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void CorruptFaultStateIsRejected()
        {
            var data=new SaveData{checkpoint="test",charge=70,flashlightFaulted=true,documents=new string[0],pickups=new ItemRecord[0]};
            Assert.Throws<InvalidDataException>(()=>SaveService.Validate(data));
        }
    }
}
