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
        [Test] public void FingersFollowWeaponAndForearmCutsStayOutsideViewDuringSwing()
        {
            var go=Player();try
            {
                var arms=go.GetComponentsInChildren<FirstPersonArm>(true);Assert.That(arms.Length,Is.EqualTo(3));
                foreach(var arm in arms)
                {
                    var filter=arm.GetComponentInChildren<MeshFilter>();var rest=filter.sharedMesh.vertices;var normals=filter.sharedMesh.normals;
                    var local=rest.Select(v=>arm.transform.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();float far=local.Max(v=>v.z);
                    Assert.That(far,Is.GreaterThan(.25f));var fingers=Enumerable.Range(0,rest.Length).Where(i=>local[i].z<.065f).ToArray();var cut=Enumerable.Range(0,rest.Length).Where(i=>local[i].z>far-.025f).ToArray();
                    var parent=arm.transform.parent;var rotation=parent.localRotation;
                    foreach(float angle in new[]{0,-48f,85f,0})
                    {
                        parent.localRotation=Quaternion.Euler(angle,0,0)*rotation;arm.Pose();var posed=filter.sharedMesh.vertices;
                        foreach(int i in fingers)Assert.That((posed[i]-rest[i]).sqrMagnitude,Is.LessThan(.00000001f));
                        var posedNormals=filter.sharedMesh.normals;
                        foreach(int i in fingers)Assert.That(Vector3.Dot(normals[i],posedNormals[i]),Is.GreaterThan(.9999f),"finger UV seam lost authored smooth normal");
                        Vector3 centre=Vector3.zero;
                        foreach(int i in cut)
                        {
                            var world=filter.transform.TransformPoint(posed[i]);centre+=world;var viewport=arm.eyes.WorldToViewportPoint(world);
                            Assert.False(viewport.z>arm.eyes.nearClipPlane&&viewport.x>0&&viewport.x<1&&viewport.y>0&&viewport.y<1,"visible forearm cut");
                        }
                        Assert.That(Vector3.Distance(centre/cut.Length,arm.ForearmEnd),Is.LessThan(.002f));
                    }
                    parent.localRotation=rotation;
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
                    var filter=arm.GetComponentInChildren<MeshFilter>();var renderer=filter.GetComponent<Renderer>();
                    Assert.That(filter.sharedMesh.vertexCount,Is.GreaterThan(20000));
                    var skin=renderer.sharedMaterials.Single(m=>m.name=="PlayerSkin");var sleeve=renderer.sharedMaterials.Single(m=>m.name=="PlayerSleeve");
                    Assert.That(skin.GetTexture("_BaseMap").width,Is.GreaterThanOrEqualTo(2048));Assert.NotNull(skin.GetTexture("_BumpMap"));
                    Assert.NotNull(sleeve.GetTexture("_BumpMap"));Assert.AreNotSame(skin.GetTexture("_BaseMap"),sleeve.GetTexture("_BaseMap"));
                    Assert.Zero(skin.GetFloat("_CadaverDecay"));Assert.That(filter.sharedMesh.subMeshCount,Is.EqualTo(2));
                }
            }finally{UnityEngine.Object.DestroyImmediate(go);}
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
