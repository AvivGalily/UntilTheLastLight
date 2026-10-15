using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Editor
{
    public static partial class VisualVerification
    {
        [MenuItem("Last Light/Verification/Capture research laboratory")]
        public static void CaptureResearch()
        {
            ShaderUtil.allowAsyncCompilation=false;
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");
            var g=UnityEngine.Object.FindFirstObjectByType<GameSession>();
            g.player.hand.gameObject.SetActive(false);g.hud.gameObject.SetActive(false);g.monster.gameObject.SetActive(false);
            Vector3 r=ForestLayout.Research;
            g.researchSpecimen.Restore();
            Shot(g.player.view,"100-research-laboratory",r+new Vector3(-2.25f,1.7f,4.5f),r+new Vector3(-5.4f,1.6f,-1.6f),true);
            Shot(g.player.view,"101-research-specimen-tank",r+new Vector3(-3.2f,1.65f,-.7f),r+new Vector3(-4.45f,1.6f,-3.75f),false);
            Shot(g.player.view,"102-walker-scientific-poster",r+new Vector3(-5.6f,1.97f,2.7f),r+new Vector3(-8.73f,1.97f,2.7f),true);
            Shot(g.player.view,"103-broken-laboratory-glass",r+new Vector3(-3.5f,1.65f,4.25f),r+new Vector3(-4.45f,1.12f,3.04f),true);
            Shot(g.player.view,"109-research-instrument-textures",r+new Vector3(-2.8f,1.65f,-4.1f),r+new Vector3(-3,1.5f,-5.65f),true);
            Shot(g.player.view,"110-research-sample-textures",r+new Vector3(5.2f,1.55f,-2.6f),r+new Vector3(5.2f,1.17f,-3.7f),true);
            Shot(g.player.view,"111-preserved-archive-specimens",r+new Vector3(-5.5f,1.65f,-.1f),r+new Vector3(-7.65f,1.55f,-2.7f),true);
            Shot(g.player.view,"112-aquatic-horror-portrait",r+new Vector3(-4.45f,2.13f,-1.8f),r+new Vector3(-4.45f,2.42f,-3.62f),true);
            g.researchSpecimen.Restore(new ResearchSave{phase=1,elapsed=2.5f});
            Shot(g.player.view,"113-red-siren-and-draining",r+new Vector3(-2.3f,1.9f,.1f),r+new Vector3(-4.2f,1.8f,-3.75f),true);
            g.researchSpecimen.Restore(new ResearchSave{phase=3,elapsed=2.6f});
            Shot(g.player.view,"114-aquatic-escape-animation",r+new Vector3(-2.6f,1.65f,.8f),r+new Vector3(-4.45f,1.3f,-2.1f),true);
            g.researchSpecimen.Restore(new ResearchSave{phase=5,health=0});
            Shot(g.player.view,"115-aquatic-carcass-key",r+new Vector3(-2.7f,1.65f,.05f),r+new Vector3(-4.1f,.45f,-1.35f),true);
            Shot(g.player.view,"116-cabin-gate-key-lock",ForestLayout.Cabin+new Vector3(2,1.55f,-2),ForestLayout.Cabin+new Vector3(2.14f,1.48f,-3.65f),true);
            var lab=g.researchSpecimen;Vector3 exit=lab.ExitPoint;
            lab.Restore(new ResearchSave{phase=4,health=4,x=exit.x,z=exit.z});lab.monster.Pose(0,.25f,0);
            AquaticShot(g,"117-aquatic-organic-anatomy",exit+new Vector3(1.05f,1.52f,2.9f),exit+new Vector3(0,1.12f,0));
            AquaticShot(g,"118-aquatic-neck-chain",exit+new Vector3(.32f,1.63f,1.08f),lab.keyVisual.position+Vector3.down*.035f);
            AquaticShot(g,"119-aquatic-dermis-and-gills",exit+new Vector3(.65f,1.94f,.88f),exit+new Vector3(.06f,1.93f,.08f));
            AquaticShot(g,"124-aquatic-mouth-front",exit+new Vector3(0,1.94f,.99f),exit+new Vector3(0,1.94f,.13f));
            AquaticShot(g,"125-aquatic-mouth-profile",exit+new Vector3(.73f,1.96f,.55f),exit+new Vector3(.03f,1.94f,.13f));
            lab.monster.Pose(3,.55f,0);
            var head=lab.monster.animator.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
            AquaticShot(g,"126-aquatic-mouth-attack",head.position+new Vector3(.46f,.05f,.93f),head.position+new Vector3(0,.02f,.17f));
            lab.monster.Pose(2,.18f,0);
            Debug.Log("R09 CAPTURE STRIDE .18: "+string.Join("; ",lab.monster.animator.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Foot_")).Select(t=>t.name+" "+t.position.ToString("F3"))));
            AquaticShot(g,"120-aquatic-stride-contact",exit+new Vector3(1.5f,.70f,2.1f),exit+new Vector3(0,.68f,.02f));
            lab.monster.Pose(2,.48f,0);
            Debug.Log("R09 CAPTURE STRIDE .48: "+string.Join("; ",lab.monster.animator.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Foot_")).Select(t=>t.name+" "+t.position.ToString("F3"))));
            AquaticShot(g,"121-aquatic-stride-recovery",exit+new Vector3(1.5f,.70f,2.1f),exit+new Vector3(0,.68f,.02f));
            lab.Restore(new ResearchSave{phase=5,health=0,x=exit.x,z=exit.z});
            Vector3 key=lab.keyVisual.position;
            AquaticShot(g,"122-neck-key-death-glint",key+new Vector3(.2f,.92f,.86f),key);
            lab.Simulate(.4f);
            AquaticShot(g,"123-neck-key-death-dim",key+new Vector3(.2f,.92f,.86f),key);
            g.researchSpecimen.monster.DisposeAnimation();
            Debug.Log("RESEARCH CAPTURE PASS: twenty-two Unity URP views including R-09 frontal/profile oral anatomy and articulated attack gape, realistic dermis and gill membranes, neck chain, two planted-ankle stride poses and bright/dim death-key glint.");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        public static void VerifyResearchChanges(GameSession g)
        {
            var specimen=g.researchSpecimen;Require(specimen&&specimen.suspended&&specimen.bubbles.Length==18,"research specimen missing");
            Require(!specimen.GetComponent<SafeLight>()&&!specimen.GetComponentInChildren<SafeLight>(),"tank grants shelter protection");
            specimen.Simulate(1);Vector3 floating=specimen.suspended.localPosition;specimen.Simulate(0);Require(specimen.suspended.localPosition==floating,"tank animation advances at zero time");
            var path=new NavMeshPath();
            foreach(Vector3 p in new[]{new Vector3(-3.8f,0,1),new Vector3(-2.8f,0,-4.2f),new Vector3(3.7f,0,1.8f),new Vector3(3.5f,0,-1.5f)})
                Require(NavMesh.CalculatePath(ForestLayout.Research+new Vector3(0,0,6),ForestLayout.Research+p,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"laboratory equipment blocks room route "+p);
            var t=g.tuning.monster;
            Require(t.approachSpeed>g.tuning.player.walkSpeed&&t.huntSpeed<g.tuning.player.sprintSpeed&&t.attackRange==1.5f&&t.windupSeconds==3,"Walker tuning mismatch");
            Vector3 clearing=new Vector3(-75,0,-86);PlaceNortonPlayer(g,clearing);g.Flashlight.Restore(100,true);g.Condition.Restore(false);
            // Hold an eight-metre fixture and measure each real approach step inside the beam.
            g.monster.BeginEncounter(clearing+Vector3.forward*8,clearing);
            float elapsed=0,travel=0;
            for(int i=0;i<300;i++)
            {
                g.monster.transform.position=clearing+Vector3.forward*8;g.player.view.transform.LookAt(g.monster.transform.position+Vector3.up*1.1f);
                Vector3 before=g.monster.transform.position;g.monster.Tick(.05f,g);elapsed+=.05f;
                if(g.monster.RetreatingToFog)break;
                travel+=Vector3.Distance(before,g.monster.transform.position);
                Require(g.monster.ActualSpeed<=t.huntSpeed+.01f,"pursuit exceeds requested speed");
                Require(elapsed<=7.1f,"Walker never retreats after sustained light");
            }
            Require(elapsed>=4&&elapsed<=7.1f&&g.monster.RetreatingToFog&&travel>2,"Walker does not advance during four-to-seven-second illumination");
            // Turning the lamp away must reset, rather than accumulate, exposure.
            PlaceNortonPlayer(g,clearing);g.monster.BeginEncounter(clearing+Vector3.forward*8,clearing);g.player.view.transform.LookAt(g.monster.transform.position+Vector3.up*1.1f);
            g.monster.Tick(1,g);g.player.view.transform.rotation=Quaternion.Euler(0,180,0);g.monster.Tick(.05f,g);Require(g.monster.FlashlightExposure==0,"interrupted beam retained exposure");
            float tolerance=g.monster.FlashlightTolerance;g.monster.Tick(0,g);Require(g.monster.FlashlightTolerance==tolerance&&g.monster.FlashlightExposure==0,"zero-time changes beam tolerance");
            // No letter read: crossing five metres starts a ten-second timer that survives walking away.
            var n=g.norton;n.ResetEncounter();g.Documents.Remove(NortonEncounter.Letter);
            PlaceNortonPlayer(g,n.wreck.position+Vector3.forward*5.2f);n.Simulate(.05f,g);Require(!n.ArrivalTriggered,"wreck triggered outside five metres");
            PlaceNortonPlayer(g,n.wreck.position+Vector3.forward*4.8f);n.Simulate(.05f,g);Require(n.ArrivalTriggered&&!n.Introduced,"wreck introduction is immediate/missing");
            PlaceNortonPlayer(g,clearing);g.Pause();n.Simulate(15,g);Require(n.IntroductionRemaining==10,"wreck timer advances in menu");g.Resume();
            n.Simulate(9.99f,g);Require(!n.Introduced,"head arrived early without a letter");n.Simulate(.02f,g);Require(n.Introduced&&n.Appearances==1&&!g.Documents.Contains(NortonEncounter.Letter),"head requires letter/staying at wreck");
            n.ResetEncounter();g.monster.Disappear();g.Flashlight.Restore(100,false);PlaceNortonPlayer(g,clearing);
            // Physical player walking and pursuer movement keep continuous close-contact time.
            g.monster.BeginEncounter(clearing-Vector3.forward*1.2f,clearing);
            for(int i=0;i<59;i++)
            {
                g.player.Simulate(g.tuning.player,Vector2.up,Vector2.zero,false,false,.05f);Physics.SyncTransforms();g.monster.Tick(.05f,g);
                Require(!g.Condition.Dead,"moving proximity killed before three seconds");
            }
            g.player.Simulate(g.tuning.player,Vector2.up,Vector2.zero,false,false,.051f);Physics.SyncTransforms();g.monster.Tick(.051f,g);
            Require(g.Condition.Dead,"three seconds of moving close contact did not kill");
            Debug.Log("RESEARCH GAMEPLAY PASS: connected lab rooms, paused suspension, 4-7 seconds continuous beam with advancing pursuit, interruption reset, speed between walk/run, five-metre wreck trigger without letter, ten active seconds after leaving, three-second lethal contact while walking.");
        }
    }
    public static class ResearchGameplayVerification
    {
        public static void Run()
        {
            SessionState.SetBool("LastLight.ResearchVerify",true);
            SessionState.SetString("LastLight.ResearchSave",Path.Combine(Path.GetTempPath(),"LastLightResearch-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");Attach();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("LastLight.ResearchVerify",false))Attach();}
        static void Attach()
        {
            GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.ResearchSave",null);
            EditorApplication.update-=Step;EditorApplication.update+=Step;
        }
        static void Step()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var g=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!g||g.State==null)return;
            EditorApplication.update-=Step;int code=0;
            try
            {
                g.enabled=false;g.intro.enabled=false;g.StartNew();g.hud.gameObject.SetActive(false);
                VisualVerification.VerifyResearchChanges(g);
            }
            catch(Exception e){Debug.LogException(e);code=1;}
            finally
            {
                SessionState.SetBool("LastLight.ResearchVerify",false);GameSession.VerificationSaveDirectory=null;
                if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
            }
        }
    }
}
