using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public sealed class PlayerBreathingTests
    {
        GameObject root,floor,session;
        GameTuning tuning;
        GameSession game;
        PlayerMotor player;
        PlayerFoley foley;
        AudioClip clip;
        bool paused;
        float timeScale;

        [SetUp] public void Setup()
        {
            paused=AudioListener.pause;timeScale=Time.timeScale;AudioListener.pause=false;Time.timeScale=1;
            floor=new GameObject("breathing test floor");floor.transform.position=Vector3.down*.5f;floor.AddComponent<BoxCollider>().size=new Vector3(200,1,200);
            root=new GameObject("breathing test player");player=root.AddComponent<PlayerMotor>();player.controller=root.AddComponent<CharacterController>();
            var eyes=new GameObject("eyes",typeof(Camera));eyes.transform.SetParent(root.transform);player.view=eyes.GetComponent<Camera>();player.view.enabled=false;
            foley=root.AddComponent<PlayerFoley>();clip=AudioClip.Create("test breath and footfall",24000,1,24000,false);
            AudioSource Source(){var node=new GameObject("test source");node.transform.SetParent(root.transform);var source=node.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;return source;}
            foley.footsteps=Source();foley.calmBreath=Source();foley.hardBreath=Source();foley.injury=Source();foley.forestSteps=foley.woodSteps=new[]{clip};
            session=new GameObject("inactive foley coordinator");session.SetActive(false);game=session.AddComponent<GameSession>();
            tuning=ScriptableObject.CreateInstance<GameTuning>();game.tuning=tuning;game.player=player;
            player.Teleport(new Vector3(0,.01f,0),0);Physics.SyncTransforms();foley.ResetSequence(1407);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root);Object.DestroyImmediate(floor);Object.DestroyImmediate(session);Object.DestroyImmediate(clip);Object.DestroyImmediate(tuning);
            AudioListener.pause=paused;Time.timeScale=timeScale;
        }
        void Tick(bool moving,bool sprint,int frames=1)
        {
            for(int n=0;n<frames;n++){player.Simulate(tuning.player,moving?Vector2.up:Vector2.zero,Vector2.zero,sprint,false,.05f);foley.Simulate(.05f,game);}
        }
        void Silent()
        {
            Assert.That(foley.BreathingMode,Is.EqualTo(PlayerBreathingMode.Silent));
            Assert.False(foley.calmBreath.isPlaying);Assert.False(foley.hardBreath.isPlaying);
            Assert.Zero(foley.calmBreath.volume);Assert.Zero(foley.hardBreath.volume);
        }
        [UnityTest] public IEnumerator FullStaminaIdleAndWalkingHaveNoBreathing()
        {
            yield return null;Silent();Tick(false,false,20);yield return null;Silent();
            Tick(true,false,40);yield return null;Assert.True(player.Moving);Assert.That(player.Stamina,Is.EqualTo(1));Silent();
        }
        [UnityTest] public IEnumerator SprintAndStationaryRecoveryUseDifferentQuietClipsAndStopAtFullStamina()
        {
            Tick(true,true,40);yield return null;
            Assert.True(player.Sprinting);Assert.That(player.Stamina,Is.LessThan(.9f));
            Assert.That(foley.BreathingMode,Is.EqualTo(PlayerBreathingMode.Sprinting));Assert.True(foley.hardBreath.isPlaying);Assert.False(foley.calmBreath.isPlaying);
            Assert.That(foley.hardBreath.volume,Is.InRange(.025f,.05f));
            Tick(false,false);yield return null;
            Assert.That(foley.BreathingMode,Is.EqualTo(PlayerBreathingMode.Recovering));Assert.True(foley.calmBreath.isPlaying);Assert.False(foley.hardBreath.isPlaying);
            Assert.That(foley.calmBreath.volume,Is.InRange(.0001f,.018f));Assert.That(foley.calmBreath.pitch,Is.LessThan(1));float initialVolume=foley.calmBreath.volume;
            Tick(true,false);yield return null;Assert.True(player.Moving);Assert.That(player.Stamina,Is.LessThan(1));Silent();
            Tick(false,false,34);yield return null;Assert.True(foley.calmBreath.isPlaying);Assert.That(foley.calmBreath.volume,Is.LessThan(initialVolume));
            Tick(false,false,100);yield return null;Assert.That(player.Stamina,Is.EqualTo(1));Silent();
        }
        [UnityTest] public IEnumerator StopResetAndDisableLeaveNoBreathingLoop()
        {
            Tick(true,true,20);yield return null;Assert.True(foley.hardBreath.isPlaying);
            AudioListener.pause=true;foley.StopMovementAudio();AudioListener.pause=false;yield return null;Silent();
            Tick(false,false);yield return null;Assert.True(foley.calmBreath.isPlaying);
            foley.ResetSequence(1407);yield return null;Silent();
            Tick(true,true);yield return null;Assert.True(foley.hardBreath.isPlaying);
            foley.enabled=false;yield return null;Silent();
        }
    }
}
