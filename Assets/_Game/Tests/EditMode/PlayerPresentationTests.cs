using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Game.Tests
{
    public sealed class PlayerPresentationTests
    {
        static GameObject Player()=>UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/Player.prefab"));
        static Bounds BakedBody(PlayerPresence p)
        {
            var mesh=new Mesh();p.bodyMorph.BakeMesh(mesh);
            var points=mesh.vertices.Select(v=>p.transform.InverseTransformPoint(p.bodyMorph.transform.TransformPoint(v))).ToArray();
            var b=new Bounds(points[0],Vector3.zero);foreach(var v in points)b.Encapsulate(v);UnityEngine.Object.DestroyImmediate(mesh);return b;
        }
        [Test] public void ShadowBodyMatchesCapsuleAndCrouchesWithBentLegs()
        {
            var go=Player();try
            {
                var p=go.GetComponent<PlayerPresence>();var motor=go.GetComponent<PlayerMotor>();p.ResetPresence();var standing=BakedBody(p);
                Assert.That(standing.size.y,Is.EqualTo(motor.controller.height).Within(.025f));Assert.That(standing.min.y,Is.EqualTo(0).Within(.025f));
                Assert.That(standing.size.x,Is.InRange(.50f,.72f));Assert.That(p.crouchShape,Is.GreaterThanOrEqualTo(0));
                var scale=p.shadowBody.localScale;p.bodyMorph.SetBlendShapeWeight(p.crouchShape,100);var crouched=BakedBody(p);
                Assert.That(crouched.size.y,Is.InRange(1.25f,1.42f));Assert.That(crouched.min.y,Is.InRange(-.035f,.07f));
                Assert.That(p.shadowBody.localScale,Is.EqualTo(scale));Assert.That(crouched.size.z,Is.GreaterThan(standing.size.z+.10f));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void PersonalBeamExcludesBodyWhileEquipmentDoesNotCastShadows()
        {
            var go=Player();try
            {
                var p=go.GetComponent<PlayerPresence>();var motor=go.GetComponent<PlayerMotor>();var beam=motor.view.GetComponentsInChildren<Light>().First(l=>l.type==LightType.Spot);
                var data=beam.GetUniversalAdditionalLightData();Assert.True(data.customShadowLayers);
                Assert.Zero(((uint)data.shadowRenderingLayers)&PlayerPresence.BodyShadowLayer);
                Assert.That(((uint)data.shadowRenderingLayers)&PlayerPresence.WorldShadowLayer,Is.Not.Zero);
                Assert.True(p.bodyRenderers.All(r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly&&r.renderingLayerMask==PlayerPresence.BodyShadowLayer));
                Assert.True(motor.hand.GetComponentsInChildren<Renderer>(true).All(r=>r.shadowCastingMode==ShadowCastingMode.Off));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void WeightedArmsKeepBoneLengthsAndGripTargetsDuringSwing()
        {
            var go=Player();try
            {
                var arms=go.GetComponent<PlayerMotor>().hand.GetComponentsInChildren<FirstPersonArm>(true);Assert.That(arms.Length,Is.EqualTo(3));
                foreach(var arm in arms)
                {
                    var skin=arm.GetComponentInChildren<SkinnedMeshRenderer>();var rest=skin.sharedMesh.vertices;var normals=skin.sharedMesh.normals;
                    Assert.NotNull(arm.upper);Assert.NotNull(arm.lower);Assert.NotNull(arm.wrist);Assert.That(skin.bones.Length,Is.GreaterThan(10));
                    Assert.That(arm.wristOffset.magnitude,Is.LessThan(.17f),"FBX must export evaluated grip-frame bones, not the old whole-character positions");
                    Assert.That(arm.upperLength,Is.InRange(.15f,.4f));Assert.That(arm.lowerLength,Is.InRange(.15f,.4f));
                    var parent=arm.transform.parent;var rotation=parent.localRotation;
                    foreach(float angle in new[]{0,-48f,85f,0})
                    {
                        parent.localRotation=Quaternion.Euler(angle,0,0)*rotation;arm.Pose();
                        Assert.That(Vector3.Distance(arm.upper.position,arm.lower.position),Is.EqualTo(arm.upperLength).Within(.002f));
                        Assert.That(Vector3.Distance(arm.lower.position,arm.wrist.position),Is.EqualTo(arm.lowerLength).Within(.002f));
                        Assert.That(Vector3.Distance(arm.wrist.position,arm.transform.TransformPoint(arm.wristOffset)),Is.LessThan(.01f),"wrist detached from authored equipment grip");
                        Assert.That(skin.sharedMesh.vertices,Is.EqualTo(rest),"IK must not shear or stretch shared geometry");
                        Assert.That(skin.sharedMesh.normals,Is.EqualTo(normals));
                    }
                    parent.localRotation=rotation;
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void EquipmentGripsRestAboveTheHandleWithoutReversedWrists()
        {
            var go=Player();try
            {
                var presence=go.GetComponent<PlayerPresence>();var gun=go.GetComponent<ShotgunCombat>();
                foreach(var arm in go.GetComponent<PlayerMotor>().hand.GetComponentsInChildren<FirstPersonArm>(true))
                {
                    arm.Pose();string side=arm.bodyAnchor.x<0?"L":"R";
                    var bones=arm.GetComponentsInChildren<Transform>();
                    Vector3 knuckles=Enumerable.Range(2,4).Select(i=>bones.First(t=>t.name=="finger"+i+"-1."+side).position).Aggregate(Vector3.zero,(a,b)=>a+b)/4;
                    Assert.That(Vector3.Angle(knuckles-arm.wrist.position,arm.transform.TransformDirection(arm.handForward)),Is.LessThan(1),"elbow solver must use this hand, not other retained source-rig bones");
                    var palm=arm.transform.InverseTransformPoint(knuckles);var wrist=arm.transform.InverseTransformPoint(arm.wrist.position);
                    if(side=="L")
                    {Assert.That(palm.y,Is.GreaterThan(.025f),"flashlight palm must sit above the barrel");Assert.That(wrist.x,Is.LessThan(-.06f),"left wrist must remain outside the handle");}
                    else
                    {Assert.That(wrist.y,Is.GreaterThan(palm.y+.005f),"weapon hand must enter the grip from above");Assert.That(wrist.z,Is.LessThan(palm.z-.06f),"weapon wrist must stay behind its knuckles");}
                    Assert.That(Vector3.Angle(arm.wrist.position-arm.lower.position,arm.transform.TransformDirection(arm.handForward)),Is.LessThan(75),"forearm must not fold backwards at the wrist");
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ShotgunHasOneRightGripAndSeparateLeftFlashlight()
        {
            var go=Player();try
            {
                var motor=go.GetComponent<PlayerMotor>();var presence=go.GetComponent<PlayerPresence>();var gun=go.GetComponent<ShotgunCombat>();
                var gunArms=gun.heldModel.GetComponentsInChildren<FirstPersonArm>(true);
                Assert.That(gunArms.Length,Is.EqualTo(1));Assert.That(gunArms[0].bodyAnchor.x,Is.GreaterThan(0));
                Assert.That(presence.flashlightGrip.GetComponent<FirstPersonArm>().bodyAnchor.x,Is.LessThan(0));
                Assert.That(motor.handLight.parent,Is.EqualTo(motor.hand));Assert.That(presence.flashlightGrip.parent,Is.EqualTo(motor.hand));
                Assert.That(motor.handLight.localPosition.x,Is.LessThan(0));Assert.That(gun.heldModel.localPosition.x,Is.GreaterThan(0));
                Assert.True(motor.handLight.gameObject.activeSelf);Assert.True(presence.flashlightGrip.gameObject.activeSelf);
                Assert.False(gun.heldModel.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("field_flashlight")||t.name.Contains("flashlight clamp")));
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void VisibleHandsUseContinuousAnatomyAndSeparateSkinAndFabricMaps()
        {
            var go=Player();try
            {
                foreach(var arm in go.GetComponentsInChildren<FirstPersonArm>(true))
                {
                    var renderer=arm.GetComponentInChildren<SkinnedMeshRenderer>();
                    Assert.That(renderer.sharedMesh.vertexCount,Is.GreaterThan(20000));
                    var skin=renderer.sharedMaterials.Single(m=>m.name=="PlayerSkin");var sleeve=renderer.sharedMaterials.Single(m=>m.name=="PlayerSleeve");
                    Assert.That(skin.GetTexture("_BaseMap").width,Is.GreaterThanOrEqualTo(2048));Assert.NotNull(skin.GetTexture("_BumpMap"));
                    Assert.NotNull(sleeve.GetTexture("_BumpMap"));Assert.AreNotSame(skin.GetTexture("_BaseMap"),sleeve.GetTexture("_BaseMap"));
                    Assert.Zero(skin.GetFloat("_CadaverDecay"));Assert.That(renderer.sharedMesh.subMeshCount,Is.EqualTo(2));
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void WalkAndRunPosesMoveTheLegsWhileKeepingThePlantedBootOnTheGround()
        {
            var go=Player();try
            {
                var p=go.GetComponent<PlayerPresence>();p.ResetPresence();var mesh=p.bodyMorph.sharedMesh;var deltas=new Vector3[mesh.vertexCount];
                Assert.That(p.walkShapes.All(s=>s>=0)&&p.runShapes.All(s=>s>=0),Is.True);
                foreach(int shape in p.walkShapes.Concat(p.runShapes))
                {
                    p.bodyMorph.SetBlendShapeWeight(shape,100);var bounds=BakedBody(p);
                    Assert.That(bounds.min.y,Is.InRange(-.04f,.055f),"Planted boot floats or sinks");
                    Assert.That(bounds.size.y,Is.InRange(1.5f,1.95f));
                    mesh.GetBlendShapeFrameVertices(shape,mesh.GetBlendShapeFrameCount(shape)-1,deltas,null,null);
                    Assert.That(deltas.Count(v=>p.bodyMorph.transform.TransformVector(v).sqrMagnitude>.0025f),Is.GreaterThan(1000),"Gait does not articulate the body: "+mesh.GetBlendShapeName(shape)+"; raw="+deltas.Max(v=>v.magnitude)+"; world="+deltas.Max(v=>p.bodyMorph.transform.TransformVector(v).magnitude));
                    p.bodyMorph.SetBlendShapeWeight(shape,0);
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void EquipmentUsesNativeLeftFlashlightAndRightWeaponMeshesWithoutMirroring()
        {
            var go=Player();try
            {
                var p=go.GetComponent<PlayerPresence>();var gun=go.GetComponent<ShotgunCombat>();
                Assert.That(p.flashlightGrip.name,Is.EqualTo("player_flashlight_hand_01"));
                Assert.That(p.axeGrip.name,Is.EqualTo("player_axe_hand_01"));
                foreach(var arm in go.GetComponent<PlayerMotor>().hand.GetComponentsInChildren<FirstPersonArm>(true))
                {
                    Assert.That(arm.transform.localScale.x,Is.GreaterThan(0));
                    Assert.That(arm.forearmAxis,Is.EqualTo(arm.transform==p.flashlightGrip?Vector3.left:arm.transform.IsChildOf(gun.heldModel)?Vector3.right:Vector3.back));
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void FallenPassengerPrefabsRetainTheLivingCastHairAndOutfitMaps()
        {
            foreach(string name in new[]{"jack","tyler","ethan"})
            {
                var fallen=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/arrival_"+name+"_01.prefab");
                var alive=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/intro_"+name+"_01.prefab");
                Assert.That(fallen.GetComponent<HunterIdentity>().characterId,Is.EqualTo(name));Assert.That(fallen.GetComponent<HunterIdentity>().deceased,Is.EqualTo(name!="jack"));
                Assert.That(fallen.GetComponent<HunterIdentity>().encounterRole,Is.EqualTo(name=="tyler"?"first-body":name=="jack"?"web-victim":"wreck-body"));
                Assert.That(alive.GetComponent<HunterIdentity>().characterId,Is.EqualTo(name));
                var liveMaterials=alive.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                var deadMaterials=fallen.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).ToArray();
                foreach(var material in liveMaterials.Where(m=>m.name.StartsWith("IntroHair")||m.name.StartsWith("IntroOutfit")))Assert.That(deadMaterials.Contains(material),Is.True,"Cast hair or clothing differs");
                Assert.That(deadMaterials.First(m=>m.name.StartsWith("ArrivalSkin")).GetTexture("_BaseMap"),Is.SameAs(liveMaterials.First(m=>m.name.StartsWith("IntroSkin")).GetTexture("_BaseMap")));
            }
        }
        [Test] public void HoldingEquipmentCannotEnablePlayerFillLight()
        {
            var go=Player();try{var p=go.GetComponent<PlayerPresence>();p.RefreshEquipment(true);Assert.False(p.gripLight.enabled);Assert.Zero(p.gripLight.intensity);}finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ExposedIntestinesHaveDetailedMappedWetTissueMaterial()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/arrival_ethan_01.prefab");
            var renderer=prefab.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterials.Any(m=>m.name=="IntestinalSerosa"));
            var material=renderer.sharedMaterials.First(m=>m.name=="IntestinalSerosa");
            Assert.That(material.GetTexture("_BaseMap").width,Is.GreaterThanOrEqualTo(1024));Assert.That(material.GetFloat("_Smoothness"),Is.GreaterThan(.5f));
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.uv.Max(uv=>uv.y),Is.GreaterThan(1));
        }
        [Test] public void WreckAndAllFindsAreOutsideResearchStationInsteadOfRangerCabin()
        {
            Assert.That(Vector3.Distance(ForestLayout.Wreck,ForestLayout.Research),Is.LessThan(20));
            Assert.That(Vector3.Distance(ForestLayout.Wreck,ForestLayout.Cabin),Is.GreaterThan(80));
            Assert.False(ForestLayout.InBuilding(ForestLayout.Wreck));Assert.False(ForestLayout.InBuilding(ForestLayout.NortonBody));
            Assert.True(ForestLayout.Clearing(ForestLayout.NortonBody));Assert.That(Vector3.Distance(ForestLayout.NortonBody,ForestLayout.Wreck),Is.LessThan(7));
        }
    }
}
