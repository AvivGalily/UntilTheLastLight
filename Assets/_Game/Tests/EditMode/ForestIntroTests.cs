using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tests
{
    public sealed class ForestIntroTests
    {
        [Test] public void LivingEyeMaterialsExposeTheIrisThroughTheTransparentCornea()
        {
            for(int n=0;n<3;n++)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Generated/Materials/IntroEyes"+n+".mat");
                Assert.That(material,Is.Not.Null);Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"),Is.True,"Opaque cornea makes the living eyes black");
                Assert.That(material.GetFloat("_Cutoff"),Is.InRange(.1f,.8f));
            }
        }
        [Test] public void SuppliedEffectsContainAudibleUnclippedAudioAndCompleteTails()
        {
            foreach(string name in new[]{"SFX_ForestVehicleAmbience","SFX_ForestGate","SFX_CreatureApproach","SFX_VehicleExplosion"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/Opening/"+name+".wav");
                Assert.That(clip,Is.Not.Null);Assert.That(clip.channels,Is.EqualTo(name=="SFX_CreatureApproach"?1:2));Assert.That(clip.LoadAudioData(),Is.True);
                var samples=new float[clip.samples*clip.channels];Assert.True(clip.GetData(samples,0));float peak=0;double energy=0;
                foreach(float sample in samples){peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;}
                Assert.That(peak,Is.InRange(.1f,.99f));Assert.That(Math.Sqrt(energy/samples.Length),Is.GreaterThan(.02));
                if(name=="SFX_VehicleExplosion"||name=="SFX_CreatureApproach")Assert.That(clip.length,Is.EqualTo(6).Within(.01f),"Supplied effect tail would be cut short");
            }
        }
        [Test] public void EnglishVoicesAreAudibleAndFitTheEntireAuthoredPlaybackWindows()
        {
            var config=AssetDatabase.LoadAssetAtPath<ForestIntroConfig>("Assets/_Game/Generated/Data/ForestIntroConfig.asset");
            foreach(var line in config.lines)
            {
                var clip=line.englishVoice;Assert.That(clip,Is.Not.Null,line.id);
                Assert.That(clip.length+.06f,Is.LessThanOrEqualTo(line.duration),line.id+" would cut off speech");
                Assert.That(clip.channels,Is.EqualTo(1));
                Assert.That(clip.LoadAudioData(),Is.True);
                var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);
                float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
                Assert.That(peak,Is.InRange(.05f,.99f),line.id+" is silent or clipped");
                Assert.That(line.VoiceFor(false),Is.SameAs(clip));
                if(!line.hebrewVoice)Assert.That(line.VoiceFor(true),Is.SameAs(clip),"Hebrew subtitles must still have English audio");
            }
            var last=config.lines[config.lines.Length-1];
            Assert.That(last.start+last.duration,Is.LessThan(config.blast),"Explosion would interrupt Tyler's warning");
        }
        [Test] public void RecordedHebrewTakesOverrideEnglishFallbackAndMissingTakesStaySafe()
        {
            var english=AudioClip.Create("English",100,1,24000,false);var hebrew=AudioClip.Create("Hebrew",100,1,24000,false);
            try
            {
                var line=new IntroLine{englishVoice=english,hebrewVoice=hebrew};
                Assert.That(line.VoiceFor(true),Is.SameAs(hebrew));Assert.That(line.VoiceFor(false),Is.SameAs(english));
                line.hebrewVoice=null;Assert.That(line.VoiceFor(true),Is.SameAs(english));
                line.englishVoice=null;Assert.That(line.VoiceFor(true),Is.Null);
            }
            finally{UnityEngine.Object.DestroyImmediate(english);UnityEngine.Object.DestroyImmediate(hebrew);}
        }
        [Test] public void OpeningScheduleHasTwelveNonOverlappingBilingualLinesAndThreatAfterGate()
        {
            var c=AssetDatabase.LoadAssetAtPath<ForestIntroConfig>("Assets/_Game/Generated/Data/ForestIntroConfig.asset");
            Assert.That(c,Is.Not.Null);Assert.That(c.duration,Is.InRange(90,115));Assert.That(c.lines.Length,Is.EqualTo(12));
            Assert.That(c.gateCrossed,Is.LessThan(c.silence));Assert.That(c.silence,Is.LessThan(c.discovery));Assert.That(c.discovery,Is.LessThan(c.threat));
            Assert.That(c.threat,Is.LessThan(c.blast));Assert.That(c.blackout,Is.GreaterThan(c.blast));Assert.That(c.cabinWake,Is.GreaterThan(c.blackout));
            var locale=new Localization(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/English.json"),AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/Hebrew.json"));
            bool previous=locale.Hebrew;
            try
            {
                for(int n=0;n<c.lines.Length;n++)
                {
                    var line=c.lines[n];Assert.That(line.passenger,Is.InRange(0,2));
                    Assert.That(line.start+line.duration,Is.LessThanOrEqualTo(c.blackout));
                    if(n>0)Assert.That(line.start,Is.GreaterThanOrEqualTo(c.lines[n-1].start+c.lines[n-1].duration));
                    string key="intro_"+line.id;locale.SetHebrew(true);Assert.True(HebrewText.ContainsHebrew(locale.Get(key)));locale.SetHebrew(false);Assert.That(locale.Get(key),Is.Not.EqualTo(key));
                }
            }
            finally{locale.SetHebrew(previous);}
        }
        [Test] public void IntroMapRestrictsInputAndSupportsKeyboardArrowsAndController()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            var actions=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Game/Generated/Data/Player.inputactions"));
            var state=new GameStateService(actions);
            try
            {
                actions.FindActionMap("UI").Enable();state.Set(ScreenMode.Intro);
                var steer=actions.FindAction("Intro/Steer",true);
                foreach(var control in new UnityEngine.InputSystem.InputControl[]{keyboard.aKey,keyboard.dKey,keyboard.leftArrowKey,keyboard.rightArrowKey,pad.leftStick.x})Assert.That(steer.controls,Does.Contain(control));
                Assert.That(actions.FindAction("Intro/SkipIntro").controls,Does.Contain(pad.buttonSouth));Assert.False(actions.FindActionMap("Gameplay").enabled);
                Assert.That(actions.FindAction("Intro/LookStick").controls,Does.Contain(pad.rightStick));
                Assert.False(state.Running);Assert.That(Time.timeScale,Is.EqualTo(1));
                state.Set(ScreenMode.Pause);Assert.False(steer.enabled);Assert.That(Time.timeScale,Is.Zero);
                state.Set(ScreenMode.Playing);Assert.True(actions.FindActionMap("Gameplay").enabled);Assert.False(actions.FindActionMap("Intro").enabled);
            }
            finally{state.Dispose();UnityEngine.Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);}
        }
        [TestCase(1f)] [TestCase(-1f)] public void ConstantExtremeSteeringStaysOnRoadAndCentresAtGate(float input)
        {
            var go=new GameObject("route test");var config=ScriptableObject.CreateInstance<ForestIntroConfig>();
            var drive=go.AddComponent<IntroVehicleController>();var wheel=go.AddComponent<SteeringWheelHands>();drive.hands=wheel;
            wheel.wheel=new GameObject("wheel").transform;wheel.wheel.SetParent(go.transform);wheel.arms=new FirstPersonArm[0];drive.headlights=new Light[0];
            drive.waypoints=new Transform[3];
            for(int n=0;n<3;n++){var p=new GameObject("waypoint").transform;p.SetParent(go.transform);p.position=new Vector3(0,0,n*160);drive.waypoints[n]=p;}
            // Waypoints belong to the world, not the moving vehicle.
            var holder=new GameObject("world route");foreach(var p in drive.waypoints)p.SetParent(holder.transform,true);
            try
            {
                drive.ResetDrive();
                for(float t=0;t<config.blast;t+=1/30f)
                {
                    drive.Simulate(1/30f,t,input,config);Assert.That(Mathf.Abs(drive.Offset),Is.LessThanOrEqualTo(config.steeringLimit+.001f));
                    drive.Sample(drive.Distance,out _,out var forward);
                    Assert.That(Quaternion.Angle(drive.transform.rotation,forward),Is.LessThan(.01f),"Lateral input turned the car away from the forward route");
                    if(Mathf.Abs(t-10)<.04f)Assert.That(Mathf.Abs(drive.Offset),Is.GreaterThan(1.5f),"Wider steering range was not available");
                    if(Mathf.Abs(t-config.gateCrossed)<.04f)Assert.That(Mathf.Abs(drive.Offset),Is.LessThan(.12f));
                }
                Assert.That(drive.Distance,Is.GreaterThan(260));
                float before=drive.Offset;
                for(int n=0;n<60;n++)drive.Simulate(1/30f,30+n/30f,0,config);
                Assert.That(Mathf.Abs(drive.Offset),Is.LessThan(.01f),"Released steering did not return to the forward centre line");
                Assert.That(Mathf.Abs(before),Is.GreaterThan(1.5f));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(config);}
        }
        [Test] public void SideAndRearHeadTurnsStayInsideTheSeatWithoutDraggingTheBody()
        {
            var c=AssetDatabase.LoadAssetAtPath<ForestIntroConfig>("Assets/_Game/Generated/Data/ForestIntroConfig.asset");
            Assert.That(c.lookYawLimit,Is.GreaterThanOrEqualTo(175));Assert.That(c.lookPitchLimit,Is.GreaterThanOrEqualTo(50));
            Assert.That(c.HeadOffset(-90).x,Is.LessThan(0),"Left view should lean toward the driver's window");
            Assert.That(c.HeadOffset(90).x,Is.GreaterThan(0));
            Assert.That(c.HeadOffset(0),Is.EqualTo(Vector3.zero));
            for(int yaw=-175;yaw<=175;yaw++)
            {
                var offset=c.HeadOffset(yaw);
                Assert.That(offset.x,Is.InRange(-.08f,.33f));Assert.That(offset.y,Is.InRange(0,.071f));
                Assert.That(offset.z,Is.InRange(0,.021f));
            }
            Assert.That(c.HeadOffset(-175).x,Is.GreaterThan(.3f),"Rear view did not clear the driver headrest");
        }
        [Test] public void DriverHasWeightedBodySleevesAndArticulatedWheelGrips()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/HunterVehicle.prefab");
            var go=UnityEngine.Object.Instantiate(source);
            try
            {
                var hands=go.GetComponent<IntroVehicleController>().hands;
                Assert.That(hands.driver,Is.Not.Null);Assert.That(hands.arms,Is.Empty,"Rigid equipment hands are still used in the car");
                Assert.That(hands.driver.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.GreaterThanOrEqualTo(3));
                foreach(float steering in new[]{-1f,0,1f})
                {
                    hands.Pose(steering,45);
                    foreach(var arm in hands.driver.arms)
                    {
                        Assert.That(arm.upperLength,Is.InRange(.18f,.4f));Assert.That(arm.lowerLength,Is.InRange(.18f,.4f));
                        Assert.That(Vector3.Distance(arm.wrist.position,arm.grip.TransformPoint(arm.wristOffset)),Is.LessThan(.065f),"Wrist leaves the rim");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void WheelHandsHaveOpposedThumbsAndMirroredAnatomicalFrames()
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/HunterVehicle.prefab"));
            try
            {
                var hands=go.GetComponent<IntroVehicleController>().hands;
                Assert.That(hands.driver.arms[0].wrist,Is.Not.SameAs(hands.driver.arms[1].wrist));
                foreach(float steering in new[]{-1f,0,1f})
                {
                    hands.Pose(steering,45);
                    foreach(var arm in hands.driver.arms)
                    {
                        string side=arm.wrist.name.Substring(arm.wrist.name.Length-1);
                        Transform Bone(string name)=>arm.wrist.GetComponentsInChildren<Transform>().First(t=>t.name==name+"."+side);
                        Vector3 Local(Transform bone)=>hands.wheel.InverseTransformPoint(bone.position);
                        Vector3 origin=Vector3.zero;for(int finger=2;finger<=5;finger++)origin+=Local(Bone("finger"+finger+"-1"))/4;
                        var row=(Local(Bone("finger5-1"))-Local(Bone("finger2-1"))).normalized;
                        var forward=Vector3.ProjectOnPlane(origin-Local(arm.wrist),row).normalized;
                        float sign=Mathf.Sign(arm.grip.localPosition.x);
                        var radial=new Vector3(sign*.211f,.102f,0).normalized;
                        var tangent=new Vector3(sign*.102f,-.211f,0).normalized;
                        Assert.That(Vector3.Dot(row,tangent),Is.GreaterThan(.98f),"Finger row faces the wrong side of the wheel");
                        Assert.That(Vector3.Dot(forward,radial),Is.GreaterThan(.98f),"Palm is rolled backwards");
                        Assert.That(origin.z,Is.InRange(-.036f,-.019f),"Palm does not rest behind the rim");
                        var thumb=Local(Bone("finger1-2"));
                        Assert.That(Vector3.Dot(thumb-origin,row),Is.LessThan(-.025f),"Thumb is on the pinky side");
                        Assert.That(Vector3.Dot(thumb-arm.grip.localPosition,radial),Is.LessThan(-.032f),"Thumb does not oppose the fingers from inside the wheel");
                        // Distal joints curl onto the forward face of the rim.
                        for(int finger=2;finger<=5;finger++)Assert.That(Local(Bone("finger"+finger+"-3")).z-origin.z,Is.GreaterThan(.025f),"Finger curls away from the rim");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void StationaryFenceMeshesLeaveTheOpeningClear()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Generated/Prefabs/ForestArrivalGate.prefab");
            var go=UnityEngine.Object.Instantiate(source);int sections=0;
            try
            {
                foreach(Transform child in go.transform)
                {
                    if(!child.name.StartsWith("forest_fence_01"))continue;sections++;
                    foreach(var mesh in child.GetComponentsInChildren<MeshFilter>())
                    {
                        var bounds=mesh.sharedMesh.bounds;float min=float.PositiveInfinity,max=float.NegativeInfinity;
                        for(int corner=0;corner<8;corner++)
                        {
                            var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                            float x=go.transform.InverseTransformPoint(mesh.transform.TransformPoint(p)).x;min=Mathf.Min(min,x);max=Mathf.Max(max,x);
                        }
                        Assert.That(min>=2.8f||max<=-2.8f,Is.True,"A fixed fence still crosses the vehicle opening");
                    }
                }
                Assert.That(sections,Is.EqualTo(10));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void InteriorSurfacesHaveGrainWeaveAndNormalMaps()
        {
            foreach(string name in new[]{"IntroDash","IntroDoor","IntroSeat","IntroFloor","IntroWheelLeather"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Generated/Materials/"+name+".mat");
                Assert.That(material,Is.Not.Null);Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(material.GetTexture("_BumpMap"),Is.Not.Null);
            }
        }
        [Test] public void AccelerationPreservesGateAndBlastRouteAnchorsAndNeverReverses()
        {
            var world=new GameObject("boost route");var slow=new GameObject("normal drive");var fast=new GameObject("accelerated drive");
            var c=ScriptableObject.CreateInstance<ForestIntroConfig>();var path=new Transform[2];
            for(int n=0;n<2;n++){path[n]=new GameObject("point").transform;path[n].SetParent(world.transform);path[n].position=Vector3.forward*n*320;}
            IntroVehicleController Make(GameObject root)
            {
                var drive=root.AddComponent<IntroVehicleController>();drive.waypoints=path;drive.headlights=new Light[0];drive.hands=root.AddComponent<SteeringWheelHands>();
                drive.hands.wheel=new GameObject("wheel").transform;drive.hands.wheel.SetParent(root.transform);drive.hands.arms=new FirstPersonArm[0];drive.ResetDrive();return drive;
            }
            var ordinary=Make(slow);var boosted=Make(fast);
            try
            {
                float previous=0;
                for(int frame=1;frame<=2760;frame++)
                {
                    float t=frame/30f;ordinary.Simulate(1/30f,t,0,c);boosted.Simulate(1/30f,t,0,c,true);
                    Assert.That(boosted.Distance,Is.GreaterThanOrEqualTo(previous));previous=boosted.Distance;
                    if(frame==300)Assert.That(boosted.Distance,Is.GreaterThan(ordinary.Distance+5));
                    if(frame==1920||frame==2760)Assert.That(boosted.Distance,Is.EqualTo(ordinary.Distance).Within(.02f),"Acceleration moved a timed route landmark");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(slow);UnityEngine.Object.DestroyImmediate(fast);UnityEngine.Object.DestroyImmediate(c);}
        }
    }
}
