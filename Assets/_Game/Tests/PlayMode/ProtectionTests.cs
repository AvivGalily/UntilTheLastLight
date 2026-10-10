using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public sealed class ProtectionTests
    {
        readonly List<GameObject> objects=new List<GameObject>();
        FlashlightModel flashlight;LightProtectionService query;Transform beam;
        GameObject Object(string name){var go=new GameObject(name);objects.Add(go);return go;}
        [SetUp] public void Setup()
        {
            beam=Object("test beam").transform;beam.position=new Vector3(0,1.5f,0);
            flashlight=new FlashlightModel(new FlashlightTuning());query=new LightProtectionService(flashlight,beam,new SafeLight[0],1<<0);
        }
        [TearDown] public void Teardown(){foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);objects.Clear();Time.timeScale=1;}
        [Test] public void A01_FullBeamStopsInwardStepAtTenMeters()
        {
            Assert.That(query.Capsule(new Vector3(0,0,10),.4f,2.5f),Is.EqualTo(Protection.Full));
            Assert.False(query.StepAllowed(new Vector3(0,0,20),new Vector3(0,0,10),.4f,2.5f,Vector3.zero,false));
        }
        [Test] public void A02_SidesAndBackRemainAvailable()
        {
            Assert.True(query.StepAllowed(new Vector3(0,0,-10),new Vector3(0,0,-2),.4f,2.5f,Vector3.zero,false));
            Assert.True(query.StepAllowed(new Vector3(10,0,0),new Vector3(2,0,0),.4f,2.5f,Vector3.zero,false));
        }
        [UnityTest] public IEnumerator A04_WallOccludesProtection()
        {
            var wall=Object("opaque wall");wall.AddComponent<BoxCollider>().size=new Vector3(8,5,1);wall.transform.position=new Vector3(0,2,5);Physics.SyncTransforms();yield return null;
            Assert.That(query.Sample(new Vector3(0,1.5f,10)),Is.EqualTo(Protection.None));
        }
        [Test] public void A05_A08_SweptCapsuleCannotTunnelThroughCone()
        {
            Vector3 from=new Vector3(-8,0,8),to=new Vector3(8,0,8);
            Assert.That(query.Capsule(from,.4f,2.5f),Is.EqualTo(Protection.None));Assert.That(query.Capsule(to,.4f,2.5f),Is.EqualTo(Protection.None));
            Assert.False(query.StepAllowed(from,to,.4f,2.5f,Vector3.zero,false));
            foreach(float frame in new[]{1/60f,1/30f,.25f,2f})Assert.False(query.StepAllowed(new Vector3(-.1f,0,9),new Vector3(-.1f,0,9-frame*4.5f),.4f,2.5f,Vector3.zero,false));
        }
        [Test] public void A06_LightRotatedOntoWindupPreventsStrike()
        {
            beam.rotation=Quaternion.Euler(0,180,0);var attack=new MonsterAttack();var tuning=new MonsterTuning();Vector3 monster=new Vector3(0,0,1.7f);
            Assert.False(query.AttackBlocked(monster,.4f,2.5f,new Vector3(0,1.3f,0)));
            attack.Tick(2,true,true,true,tuning);attack.Tick(.1f,true,true,true,tuning);Assert.True(attack.WindingUp);
            beam.rotation=Quaternion.identity;bool blocked=query.AttackBlocked(monster,.4f,2.5f,new Vector3(0,1.3f,0));Assert.True(blocked);
            Assert.False(attack.Tick(1.5f,true,!blocked,true,tuning));Assert.False(attack.WindingUp);
        }
        [UnityTest] public IEnumerator A20_ShelterLightDoesNotProtectThroughClosedDoor()
        {
            flashlight.Toggle();var shelter=Object("safe light").AddComponent<SafeLight>();shelter.transform.position=new Vector3(0,2,0);shelter.boxSize=new Vector3(12,6,12);query.Shelters=new[]{shelter};
            var wall=Object("closed door");wall.transform.position=new Vector3(0,2,2);wall.AddComponent<BoxCollider>().size=new Vector3(6,5,.2f);Physics.SyncTransforms();yield return null;
            Assert.True(query.InShelter(new Vector3(0,1.5f,1)));Assert.False(query.InShelter(new Vector3(0,1.5f,4)));
        }
        [Test] public void A25_IlluminatedMonsterCanRetreatButNeverApproach()
        {
            Vector3 from=new Vector3(0,0,10);
            Assert.True(query.StepAllowed(from,new Vector3(0,0,11),.4f,2.5f,Vector3.zero,true));
            Assert.False(query.StepAllowed(from,new Vector3(0,0,9),.4f,2.5f,Vector3.zero,true));
        }
        [Test] public void A26_FlashlightOriginDoesNotBlockRearAttack()
        {
            Assert.That(query.Sample(beam.position),Is.EqualTo(Protection.None));
            Assert.False(query.AttackBlocked(new Vector3(0,0,-1.5f),.4f,2.5f,new Vector3(0,1.3f,0)));
            Assert.True(query.AttackBlocked(new Vector3(0,0,1.5f),.4f,2.5f,new Vector3(0,1.3f,0)));
        }
        [Test] public void EdgeLightBlocksAttackWithoutCreatingOmnidirectionalShield()
        {
            Vector3 point=Quaternion.Euler(0,22,0)*new Vector3(0,0,10)+Vector3.up*1.5f;
            Assert.That(query.Sample(point),Is.EqualTo(Protection.Edge));Assert.True(query.AttackBlocked(point-Vector3.up*1.5f,.4f,2.5f,new Vector3(0,1.3f,0)));
        }
        [Test] public void WeakBatteryShrinksOnlyItsOwnProtection()
        {flashlight.Restore(10,true);Assert.That(query.Sample(new Vector3(0,1.5f,12)),Is.EqualTo(Protection.None));Assert.That(query.Sample(new Vector3(0,1.5f,7)),Is.EqualTo(Protection.Full));}
        [Test] public void OrdinaryWeakBulbNeverRepelsCreature()
        {
            flashlight.Toggle();var lamp=Object("research weak bulb").AddComponent<Light>();lamp.type=LightType.Point;lamp.intensity=.6f;lamp.range=7;lamp.transform.position=new Vector3(0,2,0);
            Assert.False(query.InShelter(new Vector3(0,1.7f,0)));Assert.That(query.Capsule(Vector3.zero,.7f,2.5f),Is.EqualTo(Protection.None));Assert.False(query.AttackBlocked(new Vector3(0,0,1.7f),.7f,2.5f,new Vector3(0,1.3f,0)));
        }
        [Test] public void PointBlankRuleDisablesBeamButRetainsDistanceProtection()
        {
            query.MinimumRepelDistance=2.8f;Vector3 close=new Vector3(0,0,1.5f);
            Assert.That(query.Capsule(close,.7f,1.95f),Is.EqualTo(Protection.None));Assert.False(query.AttackBlocked(close,.7f,1.95f,new Vector3(0,1.3f,0)));
            Assert.That(query.Capsule(new Vector3(0,0,8),.7f,1.95f),Is.EqualTo(Protection.Full));
            Assert.False(query.StepAllowed(new Vector3(0,0,22),close,.7f,1.95f,Vector3.zero,false));
        }
        [Test] public void PointBlankStillCannotAttackThroughWallOrShelter()
        {
            query.MinimumRepelDistance=2.8f;Vector3 close=new Vector3(0,0,1.5f),chest=new Vector3(0,1.3f,0);
            var wall=Object("close wall");wall.transform.position=new Vector3(0,1,.75f);wall.AddComponent<BoxCollider>().size=new Vector3(3,3,.15f);Physics.SyncTransforms();
            Assert.True(query.AttackBlocked(close,.7f,1.95f,chest));wall.SetActive(false);Physics.SyncTransforms();
            var shelter=Object("safe lamp").AddComponent<SafeLight>();shelter.transform.position=Vector3.up*2;shelter.boxSize=new Vector3(8,5,8);query.Shelters=new[]{shelter};
            Assert.True(query.AttackBlocked(close,.7f,1.95f,chest));Assert.That(query.Capsule(close,.7f,1.95f),Is.EqualTo(Protection.Full));
        }
    }
}
