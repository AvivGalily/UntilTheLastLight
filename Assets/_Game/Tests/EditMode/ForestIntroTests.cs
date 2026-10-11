using System;
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
                    if(Mathf.Abs(t-config.gateCrossed)<.04f)Assert.That(Mathf.Abs(drive.Offset),Is.LessThan(.12f));
                }
                Assert.That(drive.Distance,Is.GreaterThan(260));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(config);}
        }
    }
}
