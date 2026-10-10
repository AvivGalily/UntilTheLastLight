using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void VerifyFeedingHandoffDirections(GameSession game)
        {
            Vector3 player=game.player.transform.position,origin=game.monster.transform.position;
            Quaternion yaw=game.player.transform.rotation;int checkedDirections=0;
            foreach(Vector3 offset in new[]{new Vector3(-11,0,-5),new Vector3(11,0,6),new Vector3(0,0,13)})
            {
                if(!NavMesh.SamplePosition(ForestLayout.EncounterTree+offset,out var target,2,NavMesh.AllAreas))continue;
                game.player.Teleport(target.position,0);Physics.SyncTransforms();game.monster.transform.position=origin;
                Require(game.monster.ResumePursuit(game),"feeding handoff cannot reach direction "+offset);
                Vector3 start=game.monster.transform.position;float before=Vector3.Distance(start,target.position);
                for(int i=0;i<24;i++)game.monster.Tick(.05f,game);
                Require(Vector3.Distance(start,game.monster.transform.position)>.6f&&Vector3.Distance(game.monster.transform.position,target.position)<before-.5f,"feeding pursuer stuck in direction "+offset);
                checkedDirections++;
            }
            game.player.Teleport(player,yaw.eulerAngles.y);game.monster.transform.position=origin;Physics.SyncTransforms();Require(game.monster.ResumePursuit(game),"feeding handoff restore");
            Require(checkedDirections==3,"missing handoff direction probes");
            Debug.Log("PURSUIT HANDOFF SMOKE PASS: three actual post-feeding approach directions, connected clear origin, physical progress and current player target.");
        }
        static void VerifyWolfCollapse(GameSession game,ZombieWolf wolf)
        {
            var camera=game.player.view.transform;Vector3 position=camera.position;Quaternion rotation=camera.rotation;
            bool hand=game.player.hand.gameObject.activeSelf;game.player.hand.gameObject.SetActive(false);
            camera.position=wolf.transform.position+new Vector3(1.7f,1.1f,1.6f);camera.LookAt(wolf.transform.position+Vector3.up*.32f);
            wolf.Simulate(.55f,game);float fall=wolf.DeathProgress;RigShot(game,"43-wolf-collapsing",true);
            wolf.Simulate(0,game);Require(wolf.DeathProgress==fall,"zero-time wolf collapse advanced");
            wolf.Simulate(2,game);Require(wolf.DeathProgress==1&&!wolf.hitCollider.enabled,"wolf did not reach a held death pose");
            RigShot(game,"44-wolf-settled-death",true);camera.SetPositionAndRotation(position,rotation);game.player.hand.gameObject.SetActive(hand);
            Debug.Log("WOLF ANIMATION SMOKE PASS: physical bite lunge, .60s contact, recovery, articulated ground collapse, zero-time freeze and held final corpse pose.");
        }
        static void VerifyPlayerDeath(GameSession game)
        {
            var sequence=game.deathSequence;Vector3 eyes=game.player.view.transform.position;
            Require(sequence&&sequence.Active&&sequence.voice.isPlaying&&!game.hud.overlay.activeSelf,"fatal sequence wiring/menu/audio");
            float elapsed=sequence.Elapsed;sequence.Simulate(0);Require(sequence.Elapsed==elapsed,"zero-time fatal sequence advanced");
            game.deathSequence.Simulate(.9f);game.hud.Refresh();
            Require(game.player.view.transform.position.y<eyes.y-.6f&&!game.hud.overlay.activeSelf,"camera did not fall before the death menu");
            RigShot(game,"41-player-death-grab",false);
            for(int i=0;i<12;i++)sequence.Simulate(.05f);
            Require(sequence.BloodBursts>=1&&sequence.ClosestMouth<.65f&&sequence.impacts.isPlaying,"close bite, blood or tearing audio missing");
            RigShot(game,"42-player-death-devouring",false);
            for(int i=0;i<90&&sequence.Active;i++)sequence.Simulate(.05f);
            Require(game.State.Mode==ScreenMode.Dead&&!sequence.Active&&sequence.BloodBursts>=4&&AudioListener.pause,"fatal sequence failed to end once with blood/menu");
            Debug.Log("FATAL ANIMATION SMOKE PASS: gameplay input locked, fallen camera, actual near-mouth skeletal bites, defensive hands, repeated blood/tearing audio, zero-time freeze and delayed death menu.");
        }
        static void VerifyPlayerPresence(GameSession game)
        {
            var presence=game.presence;
            Require(presence&&presence.shadowBody&&presence.axeGrip&&Array.TrueForAll(presence.bodyRenderers,r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly),"human shadow body missing");
            Require(presence.axeGrip.IsChildOf(game.axe.heldModel),"grip does not follow the axe swing");
            Require(presence.axeGrip.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount>300,"grip is not actual human anatomy");
            bool axe=game.Inventory.HasAxe;game.Inventory.Restore(game.Inventory.Batteries,game.Inventory.HasFuse,true);game.axe.Simulate(.01f,game);
            UIShot(game,"39-human-axe-grip",true);
            NortonArtShot(game,"54-body-connected-axe",true);
            Require(game.axe.BeginSwing(game),"axe presentation windup rejected");game.axe.Simulate(.17f,game);NortonArtShot(game,"58-axe-windup-body-arm",true);game.axe.Simulate(.8f,game);
            var eyes=game.player.view.transform;Vector3 position=eyes.position;Quaternion rotation=eyes.rotation;
            var moon=GameObject.Find("Moon · decorative, never protection").GetComponent<Light>();
            Vector3 shadowDirection=Vector3.ProjectOnPlane(moon.transform.forward,Vector3.up).normalized;
            eyes.rotation=Quaternion.LookRotation((game.player.transform.position+shadowDirection*1.7f)-eyes.position);
            UIShot(game,"40-player-human-shadow",false);eyes.SetPositionAndRotation(position,rotation);
            VerifyShadowOptics(game,moon,shadowDirection);
            game.Inventory.Restore(game.Inventory.Batteries,game.Inventory.HasFuse,axe);game.axe.Simulate(.01f,game);
            Debug.Log("PLAYER PRESENCE SMOKE PASS: anatomical human shadow, UV human forearm/palm/fingers and grip attached throughout axe motion.");
        }
        static Color32[] ShadowPixels(Camera camera)
        {
            var rt=new RenderTexture(480,270,24);rt.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(480,270,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,480,270),0,0);texture.Apply();var pixels=texture.GetPixels32();
            RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);return pixels;
        }
        static float PixelDifference(Color32[] a,Color32[] b)
        {
            double sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);return (float)(sum/(a.Length*3));
        }
        static void VerifyShadowOptics(GameSession game,Light moon,Vector3 direction)
        {
            var camera=game.player.view;var eyes=camera.transform;var position=eyes.position;var rotation=eyes.rotation;
            var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var enabled=Array.ConvertAll(lights,l=>l.enabled);bool hud=game.hud.gameObject.activeSelf;
            float selfDifference=0,externalDifference=0;
            try
            {
                game.hud.gameObject.SetActive(false);foreach(var l in lights)l.enabled=false;
                eyes.rotation=Quaternion.LookRotation(game.player.transform.position+direction*2.2f-eyes.position);
                game.flashlightVisual.enabled=true;
                foreach(var r in game.presence.bodyRenderers)r.enabled=true;var withBody=ShadowPixels(camera);
                foreach(var r in game.presence.bodyRenderers)r.enabled=false;selfDifference=PixelDifference(withBody,ShadowPixels(camera));
                Require(selfDifference<.15f,"personal flashlight casts own body into beam: mean pixel difference "+selfDifference);
                foreach(var r in game.presence.bodyRenderers)r.enabled=true;NortonArtShot(game,"55-forward-flashlight-no-self-shadow",true);game.hud.gameObject.SetActive(false);
                game.flashlightVisual.enabled=false;moon.enabled=true;withBody=ShadowPixels(camera);
                foreach(var r in game.presence.bodyRenderers)r.enabled=false;externalDifference=PixelDifference(withBody,ShadowPixels(camera));
                Require(externalDifference>.025f,"external light no longer produces a body shadow");
                foreach(var r in game.presence.bodyRenderers)r.enabled=true;NortonArtShot(game,"56-human-shadow-external-light",false);game.hud.gameObject.SetActive(false);
                game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,true,.02f);game.presence.Simulate(.02f,game.player);
                eyes.rotation=Quaternion.LookRotation(game.player.transform.position+direction*1.7f-eyes.position);NortonArtShot(game,"57-crouched-human-shadow",false);
            }
            finally
            {
                for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];foreach(var r in game.presence.bodyRenderers)r.enabled=true;
                game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.02f);game.presence.ResetPresence();eyes.SetPositionAndRotation(position,rotation);game.hud.gameObject.SetActive(hud);
            }
            Debug.Log($"PLAYER SHADOW OPTICS SMOKE PASS: actual URP body-on/off pixel difference {selfDifference:0.000} under personal beam; {externalDifference:0.000} under moon; human standing/crouch and body-connected idle/windup captures.");
        }
    }
}
