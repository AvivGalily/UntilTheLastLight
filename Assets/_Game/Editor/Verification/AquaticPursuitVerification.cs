using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Editor
{
    // Advance one simulation step per player frame, allowing navigation carving,
    // animation and physics transforms to update between pursuit decisions.
    public static class AquaticPursuitVerification
    {
        static IEnumerator routine;
        static int frame=-1;
        static double deadline;
        const float Dt=.05f;
        static void Require(bool value,string message){if(!value)throw new Exception("Aquatic pursuit: "+message);}
        public static void Run()
        {
            if(File.Exists("Logs/AquaticPursuitVerification.json"))File.Delete("Logs/AquaticPursuitVerification.json");
            SessionState.SetBool("LastLight.AquaticPursuitVerify",true);
            SessionState.SetString("LastLight.AquaticPursuitSave",Path.Combine(Path.GetTempPath(),"AquaticPursuit-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");Attach();EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("LastLight.AquaticPursuitVerify",false))Attach();}
        static void Attach()
        {
            GameSession.VerificationSaveDirectory=SessionState.GetString("LastLight.AquaticPursuitSave",null);
            deadline=EditorApplication.timeSinceStartup+180;frame=-1;routine=null;EditorApplication.update-=Step;EditorApplication.update+=Step;
        }
        static void Step()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"verification timed out");
                if(frame==Time.frameCount)return;frame=Time.frameCount;
                if(routine==null)
                {
                    var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();if(!game||game.State==null)return;
                    game.enabled=false;game.intro.enabled=false;game.StartNew();game.hud.gameObject.SetActive(false);routine=Verify(game);
                }
                if(!routine.MoveNext())Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int exit)
        {
            EditorApplication.update-=Step;SessionState.SetBool("LastLight.AquaticPursuitVerify",false);GameSession.VerificationSaveDirectory=null;
            if(Application.isBatchMode)EditorApplication.Exit(exit);else EditorApplication.isPlaying=false;
        }
        static void Place(GameSession game,Vector3 position)
        {
            game.player.Teleport(position,0);game.player.Simulate(game.tuning.player,Vector2.zero,Vector2.zero,false,false,.01f);Physics.SyncTransforms();
        }
        static string Navigation(GameSession game)
        {
            var beast=game.researchSpecimen.monster;var p=beast.transform.position;var route=new NavMeshPath();
            bool start=NavMesh.SamplePosition(p,out var a,1.5f,NavMesh.AllAreas),end=NavMesh.SamplePosition(game.player.transform.position,out var b,1.5f,NavMesh.AllAreas);
            bool found=start&&end&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,route);
            string points=string.Join(";",Array.ConvertAll(route.corners,v=>v.ToString("F3")));
            string block="none";
            if(route.corners.Length>1)
            {
                Vector3 delta=route.corners[1]-p;
                if(Physics.SphereCast(p+Vector3.up*.75f,.34f,delta.normalized,out var hit,delta.magnitude,game.SolidMask,QueryTriggerInteraction.Ignore))block=hit.collider.name+" @ "+hit.distance;
            }
            return $"actor={p:F3}, target={game.player.transform.position:F3}, speed={beast.ActualSpeed}, safe={game.IsSafe}, start={start} ({a.position:F3}), end={end}, path={found}/{route.status}, corners={points}, blocker={block}";
        }
        static IEnumerator Verify(GameSession game)
        {
            var lab=game.researchSpecimen;var beast=lab.monster;
            Place(game,ForestLayout.Research+new Vector3(0,0,10));Require(lab.Release(game),"release failed");
            for(int i=0;i<206;i++){lab.Simulate(Dt,game);yield return null;}
            Require(lab.Phase==ResearchPhase.Hunting,"escape did not finish");
            var stride=VerifyWeightedStride(beast);while(stride.MoveNext())yield return stride.Current;
            Debug.Log("PURSUIT handoff: "+Navigation(game));
            Vector3 released=beast.transform.position;int stalledCruiseFrames=0;
            for(int i=0;i<240&&beast.Strikes==0;i++)
            {
                game.Condition.Tick(Dt,false,game.tuning.player);lab.Simulate(Dt,game);Physics.SyncTransforms();
                Require(beast.ActualSpeed<3.95f,"pursuit exceeded speed cap");
                if(i>12&&Vector3.Distance(beast.transform.position,game.player.transform.position)>3&&beast.ActualSpeed<3.5f)stalledCruiseFrames++;
                if(i%40==39)Debug.Log("PURSUIT outside "+((i+1)*Dt)+": "+Navigation(game));
                yield return null;
            }
            Require(Vector3.Distance(released,beast.transform.position)>8,"did not leave laboratory: "+Navigation(game));
            Require(beast.Strikes>0,"did not reach player through room door and station entrance: "+Navigation(game));
            Require(stalledCruiseFrames==0,"motion pauses at room corners: "+stalledCruiseFrames);
            // Retreat while the first attack is still playing, then walk along the
            // trail. Its recovery must give movement back to the pursuing AI.
            game.Condition.Restore(false);
            Place(game,ForestLayout.Research+new Vector3(0,0,16));
            Vector3 chaseStart=beast.transform.position,walkingStart=game.player.transform.position;
            var trail=new NavMeshPath();Vector3 trailEnd=new Vector3(-21,0,52);
            Require(NavMesh.CalculatePath(walkingStart,trailEnd,NavMesh.AllAreas,trail)&&trail.status==NavMeshPathStatus.PathComplete,"walking trail disconnected");
            int waypoint=1;
            for(int i=0;i<60;i++)
            {
                Vector3 current=game.player.transform.position;
                while(waypoint<trail.corners.Length&&Vector3.Distance(current,trail.corners[waypoint])<.01f)waypoint++;
                if(waypoint<trail.corners.Length)Place(game,Vector3.MoveTowards(current,trail.corners[waypoint],3*Dt));
                game.Condition.Tick(Dt,false,game.tuning.player);lab.Simulate(Dt,game);Physics.SyncTransforms();yield return null;
            }
            Require(!game.Condition.Dead&&Vector3.Distance(walkingStart,game.player.transform.position)>6,"walking player setup");
            Require(Vector3.Distance(chaseStart,beast.transform.position)>5,"stopped following walking target after attack: "+Navigation(game));
            // A save/load during the chase must preserve a navigable origin.
            var saved=lab.Capture();Vector3 beforeLoad=beast.transform.position;lab.Restore(saved);yield return null;
            Require(Vector3.Distance(beforeLoad,beast.transform.position)<.1f,"load moved chasing creature");
            int previousStrikes=beast.Strikes;
            for(int i=0;i<200&&beast.Strikes==previousStrikes;i++)
            {game.Condition.Tick(Dt,false,game.tuning.player);lab.Simulate(Dt,game);Physics.SyncTransforms();yield return null;}
            Require(beast.Strikes>previousStrikes,"did not catch up after moving target and save/load: "+Navigation(game));
            game.Pause();Vector3 paused=beast.transform.position;for(int i=0;i<10;i++){lab.Simulate(Dt,game);yield return null;}
            Require(beast.transform.position==paused,"moves while paused");game.Resume();
            for(int i=0;i<4;i++)Require(beast.Hit(),"combat hit failed");Vector3 dead=beast.transform.position;
            for(int i=0;i<30;i++){lab.Simulate(Dt,game);yield return null;}
            Require(beast.transform.position==dead&&lab.Phase==ResearchPhase.Dead,"dead creature pursues player");
            Vector3 reference=Vector3.zero;float largestFrameRateDifference=0;
            foreach(float frameDt in new[]{1/30f,1/60f,1/120f})
            {
                game.Condition.Restore(false);game.State.Set(ScreenMode.Playing);Place(game,ForestLayout.Research+new Vector3(0,0,10));
                Vector3 exit=lab.ExitPoint;lab.Restore(new ResearchSave{phase=4,health=4,x=exit.x,z=exit.z});yield return null;
                int frames=Mathf.RoundToInt(4/frameDt);
                for(int i=0;i<frames;i++)
                {
                    lab.Simulate(frameDt,game);Physics.SyncTransforms();
                    Require(beast.ActualSpeed<=3.96f,"speed spike at "+frameDt);
                    if(i*frameDt>.6f)Require(beast.ActualSpeed>3.5f,"corner hitch at "+frameDt+": "+Navigation(game));
                    yield return null;
                }
                if(reference==Vector3.zero)reference=beast.transform.position;
                else largestFrameRateDifference=Mathf.Max(largestFrameRateDifference,Vector3.Distance(reference,beast.transform.position));
            }
            Require(largestFrameRateDifference<.16f,"frame rate changes pursuit distance: "+largestFrameRateDifference);
            game.StartNew();ResearchEncounterVerification.Verify(game);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/AquaticPursuitVerification.json",JsonUtility.ToJson(new Receipt{passed=true,checkedAt=DateTime.UtcNow.ToString("o"),frameRatePositionDifference=largestFrameRateDifference,verification="Release; room corners without cruise hitches; laboratory and station doors; walking target after attack; save/load and catch-up; pause/death; 30/60/120fps motion; neck key visibility, death pulse, pause/load and E pickup; tank-owned hoses detach and reset"},true));
            Debug.Log("AQUATIC PURSUIT PASS: released creature routes through laboratory door and station entrance, follows walking player after attack, resumes from save, catches up and attacks; pause and death stop movement. Actual player frames between all AI ticks.");
        }
        [Serializable] sealed class Receipt {public bool passed;public string checkedAt,verification;public float frameRatePositionDifference;}
        static IEnumerator VerifyWeightedStride(AquaticMonster beast)
        {
            var skin=beast.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            beast.Pose(2,.12f,0);yield return null;skin.BakeMesh(mesh,true);var first=mesh.vertices;
            beast.Pose(2,.62f,0);yield return null;skin.BakeMesh(mesh,true);var second=mesh.vertices;
            int selected=0;float motion=0,anyMotion=0;var bounds=new Bounds();
            for(int i=0;i<first.Length;i++)
            {
                Vector3 p=beast.transform.InverseTransformPoint(skin.transform.TransformPoint(first[i]));
                if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);
                float moved=Vector3.Distance(skin.transform.TransformPoint(first[i]),skin.transform.TransformPoint(second[i]));anyMotion=Mathf.Max(anyMotion,moved);
                if(p.x<-.1f&&p.y<.4f&&p.z>-.1f){selected++;motion=Mathf.Max(motion,moved);}
            }
            float edgeChange=0;Vector3 edgeA=Vector3.zero,edgeB=Vector3.zero;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)for(int edge=0;edge<3;edge++)
            {
                int a=triangles[i+edge],b=triangles[i+(edge+1)%3];
                float before=skin.transform.TransformVector(first[a]-first[b]).magnitude,after=skin.transform.TransformVector(second[a]-second[b]).magnitude;
                if(Mathf.Abs(after-before)>edgeChange){edgeChange=Mathf.Abs(after-before);edgeA=beast.transform.InverseTransformPoint(skin.transform.TransformPoint(first[a]));edgeB=beast.transform.InverseTransformPoint(skin.transform.TransformPoint(first[b]));}
            }
            UnityEngine.Object.DestroyImmediate(mesh);
            Debug.Log($"R09 WEIGHTED STRIDE: foot vertices={selected}, motion={motion}, any motion={anyMotion}, edge length change={edgeChange}, edge={edgeA:F3}/{edgeB:F3}, actor bounds={bounds}");
            Require(selected>0&&motion>.15f,"weighted foot mesh does not follow stride: "+motion+" / selected "+selected+" / "+bounds);
            Require(edgeChange<.08f,"stride stretches tail or body into a sheet: "+edgeChange);
        }
    }
}
