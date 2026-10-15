using UnityEngine;
using UnityEngine.AI;
namespace Game
{
    public sealed class NortonEncounter:MonoBehaviour
    {
        public const string Letter="note_norton";
        public CinderHead[] enemies;
        public SpatFireball[] projectiles;
        public ParticleSystem[] carEffects;
        public Light carLight;
        public AudioSource introduction,fireVoice;
        public AudioClip introductionClip;
        public AudioSource approachMusic;
        public const float MinRespite=42,MaxRespite=60;
        public bool MusicEngaged {get;private set;}
        public Transform wreck,casualty;
        public bool Unlocked {get;private set;}
        public bool Introduced {get;private set;}
        public int Appearances {get;private set;}
        public float NextAppearance {get;private set;}
        public const float ArrivalDistance=5,IntroductionDelay=10;
        public bool ArrivalTriggered {get;private set;}
        public float IntroductionRemaining {get;private set;}
        float clock;
        System.Random random;
        public void ReadLetter(){Unlocked=true;}
        public void ResetEncounter(bool unlocked=false,bool introduced=false)
        {
            Unlocked=unlocked;Introduced=introduced;Appearances=0;clock=0;random=new System.Random(141818);NextAppearance=introduced?18:0;
            ArrivalTriggered=false;IntroductionRemaining=IntroductionDelay;
            foreach(var enemy in enemies)enemy.Retire();
            StopMusic();
            foreach(var p in projectiles)p.ResetProjectile();introduction.Stop();
            foreach(var p in carEffects){p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.Play();p.Pause();}
            if(!fireVoice.isPlaying)fireVoice.Play();
        }
        public void SilenceEnemies(){introduction.Stop();StopMusic();foreach(var enemy in enemies){enemy.voice.Stop();if(enemy.burnVoice)enemy.burnVoice.Stop();}}
        float NextRespite()=>Mathf.Lerp(MinRespite,MaxRespite,(float)random.NextDouble());
        void StopMusic(){MusicEngaged=false;if(approachMusic){approachMusic.Stop();approachMusic.volume=0;}}
        void Music(float dt,GameSession game)
        {
            float distance=float.PositiveInfinity;
            foreach(var e in enemies)if(e.gameObject.activeSelf&&e.Alive)distance=Mathf.Min(distance,Vector3.Distance(e.transform.position,game.player.transform.position));
            if(float.IsPositiveInfinity(distance)){StopMusic();return;}
            if(distance<24)MusicEngaged=true;
            if(!MusicEngaged||!approachMusic)return;
            if(!approachMusic.isPlaying)approachMusic.Play();
            approachMusic.volume=Mathf.MoveTowards(approachMusic.volume,Mathf.Lerp(.035f,.13f,Mathf.InverseLerp(24,2,distance)),dt*.12f);
        }
        public bool LaunchFireball(Vector3 origin,Vector3 target,CinderHead shooter)
        {
            foreach(var projectile in projectiles)if(!projectile.gameObject.activeSelf){projectile.Launch(origin,target,shooter);return true;}return false;
        }
        public bool TrySpawn(GameSession game,bool initial=false)
        {
            CinderHead available=null;
            // Each pool slot stays occupied until its collapse/burn has finished.
            // A second living head may join the encounter on the normal cadence.
            foreach(var e in enemies)if(!e.gameObject.activeSelf){available=e;break;}
            if(!available)return false;
            Vector3 player=game.player.transform.position;
            Vector3 building=Vector3.Distance(player,ForestLayout.Cabin)<8?ForestLayout.Cabin:Vector3.Distance(player,ForestLayout.Store)<8?ForestLayout.Store:ForestLayout.Research;
            bool indoors=ForestLayout.InBuilding(player);
            for(int n=0;n<36;n++)
            {
                float a=n*Mathf.PI*2/36+random.Next(0,20)*.03f;
                Vector3 candidate=indoors?building+new Vector3(Mathf.Cos(a)*3,0,Mathf.Sin(a)*3):player+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(initial?12:17+random.Next(0,6));
                if(!NavMesh.SamplePosition(candidate,out var point,indoors?.55f:1.5f,NavMesh.AllAreas)||Vector3.Distance(point.position,player)<(indoors?3:9))continue;
                if(indoors&&!ForestLayout.InBuilding(point.position))continue;
                if(Physics.CheckCapsule(point.position+Vector3.up*.4f,point.position+Vector3.up*.85f,.34f,game.SolidMask,QueryTriggerInteraction.Ignore))continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(point.position,player,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                if(System.Array.Exists(enemies,e=>e!=available&&e.gameObject.activeSelf&&Vector3.Distance(e.transform.position,point.position)<3))continue;
                available.gameObject.SetActive(true);available.ResetMonster(point.position,Quaternion.LookRotation(Vector3.ProjectOnPlane(player-point.position,Vector3.up)));Appearances++;return true;
            }
            return false;
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!game.State.Running)return;clock+=dt;
            Shader.SetGlobalFloat("_NortonTime",clock);
            foreach(var p in carEffects){p.Play();p.Simulate(dt,true,false,false);p.Pause();}
            carLight.intensity=3.3f+Mathf.Sin(clock*13)*.45f+Mathf.Sin(clock*23)*.35f;
            if(game.Completed)return;
            if(!Introduced)
            {
                if(!ArrivalTriggered&&wreck&&Vector3.ProjectOnPlane(game.player.transform.position-wreck.position,Vector3.up).sqrMagnitude<=ArrivalDistance*ArrivalDistance)
                {ArrivalTriggered=true;Unlocked=true;return;}
                if(!ArrivalTriggered)return;
                IntroductionRemaining=Mathf.Max(0,IntroductionRemaining-dt);
                if(IntroductionRemaining>0)return;
            }
            if(game.forestEncounter&&game.forestEncounter.Choreographing)return;
            if(!Introduced)
            {
                if(TrySpawn(game,true)){Introduced=true;introduction.PlayOneShot(introductionClip,.9f);game.Notify(game.Text("head_arrival"),9);NextAppearance=48;game.SaveCheckpoint("checkpoint_norton");}
            }
            else
            {
                NextAppearance-=dt;
                if(NextAppearance<=0){if(TrySpawn(game))NextAppearance=NextRespite();else NextAppearance=5;}
            }
            foreach(var e in enemies)
            {
                if(!e.gameObject.activeSelf)continue;
                if(e.Alive&&Vector3.Distance(e.transform.position,game.player.transform.position)>80){e.Retire();continue;}
                e.Simulate(dt,game);if(!game.State.Running)return;
            }
            Music(dt,game);
            foreach(var p in projectiles){p.Simulate(dt,game);if(!game.State.Running)return;}
        }
    }
}
