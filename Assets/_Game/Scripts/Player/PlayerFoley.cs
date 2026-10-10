using UnityEngine;
namespace Game
{
    public sealed class PlayerFoley:MonoBehaviour
    {
        public AudioSource footsteps,calmBreath,hardBreath,injury;
        public AudioClip[] forestSteps,woodSteps;
        public AudioClip scream,hurt;
        float travel,effort;
        int last=-1;
        System.Random rng;
        public int StepsPlayed{get;private set;}
        public float Effort=>effort;
        void Awake(){if(injury)injury.ignoreListenerPause=true;}
        public void ResetSequence(int seed)
        {
            rng=new System.Random(seed+811);travel=.55f;effort=0;last=-1;StepsPlayed=0;
            foreach(var source in new[]{footsteps,calmBreath,hardBreath,injury})if(source)source.Stop();
            if(calmBreath){calmBreath.volume=.045f;calmBreath.Play();}if(hardBreath){hardBreath.volume=0;hardBreath.Play();}
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;if(rng==null)ResetSequence(game.tuning.seed);
            var player=game.player;
            float desired=Mathf.Max(1-player.Stamina,player.Sprinting?.85f:player.Moving?.15f:0,game.Condition.Wounded?.65f:0);
            effort=Mathf.MoveTowards(effort,desired,dt*(desired>effort?.6f:.13f));
            calmBreath.volume=Mathf.Lerp(.045f,.08f,effort)*(1-effort);hardBreath.volume=effort*.24f;
            hardBreath.pitch=Mathf.Lerp(.85f,1.18f,effort);
            if(!player.Moving||!player.controller.isGrounded){travel=Mathf.Min(travel,.55f);return;}
            travel+=Mathf.Min(player.TravelledThisStep,2);
            float stride=player.Sprinting?1.25f:player.Crouching?1.12f:.95f;
            if(travel<stride)return;travel%=stride;
            var clips=ForestLayout.InBuilding(player.transform.position)?woodSteps:forestSteps;
            int index=rng.Next(clips.Length);if(index==last)index=(index+1)%clips.Length;last=index;
            footsteps.pitch=.92f+(float)rng.NextDouble()*.16f;
            footsteps.volume=player.Crouching?.12f:player.Sprinting?.45f:.28f;
            footsteps.PlayOneShot(clips[index]);StepsPlayed++;
        }
        public void Hit(bool lethal){injury.Stop();injury.volume=lethal?.85f:.7f;injury.PlayOneShot(lethal?scream:hurt);}
        public void StopInjury(){if(injury)injury.Stop();}
    }
}
