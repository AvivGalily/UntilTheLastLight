using UnityEngine;
using UnityEngine.AI;
namespace Game
{
    // Real inactive intervals between unseen disappearances and distant returns.
    public sealed class MonsterEncounters:MonoBehaviour
    {
        System.Random rng;
        float remaining,unseen,hiddenRemaining;
        public float TimeUntilReturn=>Hidden?hiddenRemaining:remaining;
        public bool Hidden {get;private set;}
        public int Vanishes {get;private set;}
        public void ResetSequence(int seed){rng=new System.Random(seed+7501);remaining=34;unseen=hiddenRemaining=0;Hidden=false;Vanishes=0;}
        public static bool Visible(Camera view,Vector3 feet,LightProtectionService light)
        {
            Vector3 chest=feet+Vector3.up*1.1f;Vector3 p=view.WorldToViewportPoint(chest);
            return p.z>0&&p.x>-.08f&&p.x<1.08f&&p.y>-.1f&&p.y<1.1f&&light.Clear(view.transform.position,chest);
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||game.Completed||game.mechanicsTest)return;if(rng==null)ResetSequence(game.tuning.seed);
            var monster=game.monster;
            // The scripted ambush can explicitly restore the same hidden creature.
            if(Hidden&&monster.HasSpawned){Hidden=false;remaining=NextInterval(game);unseen=0;}
            if(game.forestEncounter&&game.forestEncounter.OwnsMonster)return;
            if(Hidden)
            {
                hiddenRemaining=Mathf.Max(0,hiddenRemaining-dt);
                if(hiddenRemaining<=0&&!game.IsSafe)
                {
                    if(TryReturn(game)){Hidden=false;remaining=NextInterval(game);unseen=0;}
                    else hiddenRemaining=1;
                }
                return;
            }
            remaining-=dt;
            unseen=monster.HasSpawned&&Visible(game.player.view,monster.transform.position,game.Protection)?0:unseen+dt;
            if(game.IsSafe||monster.Attack.WindingUp)return;
            if(!monster.HasSpawned)
            {if(TryReturn(game)){remaining=NextInterval(game);unseen=0;}return;}
            if(remaining>0||unseen<3||Vector3.Distance(monster.transform.position,game.player.transform.position)<game.tuning.monster.vanishMinDistance)return;
            monster.Disappear();Hidden=true;Vanishes++;
            hiddenRemaining=Mathf.Lerp(game.tuning.monster.hiddenMinSeconds,game.tuning.monster.hiddenMaxSeconds,(float)rng.NextDouble());
        }
        float NextInterval(GameSession game)=>Mathf.Lerp(game.tuning.monster.encounterMinSeconds,game.tuning.monster.encounterMaxSeconds,(float)rng.NextDouble());
        public bool TryReturn(GameSession game)
        {
            if(rng==null)ResetSequence(game.tuning.seed);
            var path=new NavMeshPath();var t=game.tuning.monster;Vector3 target=game.player.transform.position;
            for(int i=0;i<36;i++)
            {
                float angle=(float)rng.NextDouble()*Mathf.PI*2,range=33+(float)rng.NextDouble()*13;
                Vector3 candidate=target+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*range;
                if(!NavMesh.SamplePosition(candidate,out var nav,3,NavMesh.AllAreas)||Vector3.Distance(nav.position,target)<30)continue;
                if(!MonsterPathPlanner.StandingClear(nav.position,t,game.SolidMask))continue;
                if(game.Protection.Capsule(nav.position,t.radius,t.height)!=Protection.None)continue;
                if(Visible(game.player.view,nav.position,game.Protection))continue;
                if(!NavMesh.CalculatePath(nav.position,target,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                game.monster.BeginEncounter(nav.position,target);return true;
            }
            return false;
        }
    }
}
