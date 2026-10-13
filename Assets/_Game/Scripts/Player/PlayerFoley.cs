using UnityEngine;
namespace Game
{
    public enum PlayerBreathingMode { Silent, Sprinting, Recovering }
    public sealed class PlayerFoley:MonoBehaviour
    {
        public AudioSource footsteps,calmBreath,hardBreath,injury;
        public AudioClip[] forestSteps,woodSteps;
        public AudioClip scream,hurt;
        float effort;
        int last=-1;
        System.Random rng;
        public int StepsPlayed{get;private set;}
        public float Effort=>effort;
        public PlayerBreathingMode BreathingMode{get;private set;}
        void Awake(){if(injury)injury.ignoreListenerPause=true;StopBreathing();}
        public void ResetSequence(int seed)
        {
            rng=new System.Random(seed+811);effort=0;last=-1;StepsPlayed=0;
            foreach(var source in new[]{footsteps,calmBreath,hardBreath,injury})if(source)source.Stop();
            footsteps.loop=false;footsteps.playOnAwake=false;footsteps.clip=null;
            StopBreathing();
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;if(rng==null)ResetSequence(game.tuning.seed);
            var player=game.player;
            // Walking is silent even while stamina refills. Recovery breathing
            // only belongs to a stationary, winded player, not wounds or idle.
            float deficit=1-player.Stamina;
            if(player.Sprinting)
            {
                BreathingMode=PlayerBreathingMode.Sprinting;
                effort=Mathf.MoveTowards(effort,Mathf.Max(.85f,deficit),dt*.6f);
                Silence(calmBreath);PlayBreath(hardBreath,Mathf.Lerp(.025f,.05f,effort),Mathf.Lerp(.95f,1.08f,effort));
            }
            else if(!player.Moving&&player.Stamina<.995f)
            {
                BreathingMode=PlayerBreathingMode.Recovering;effort=deficit;
                Silence(hardBreath);PlayBreath(calmBreath,.018f*Mathf.Sqrt(deficit),.95f);
            }
            else StopBreathing();
            if(!player.Moving||!player.Grounded){StopFootsteps();return;}
            if(player.FootfallsThisStep==0)return;
            var clips=ForestLayout.InBuilding(player.transform.position)?woodSteps:forestSteps;
            int index=rng.Next(clips.Length);if(index==last)index=(index+1)%clips.Length;last=index;
            footsteps.pitch=.92f+(float)rng.NextDouble()*.16f;
            footsteps.volume=player.Crouching?.12f:player.Sprinting?.45f:.28f;
            // Keep a single footfall active; silence its tail on stopping.
            footsteps.clip=clips[index];footsteps.Play();StepsPlayed++;
        }
        public void StopFootsteps(){if(footsteps){footsteps.Stop();footsteps.clip=null;}}
        static void Silence(AudioSource source){if(!source)return;source.volume=0;source.Stop();}
        static void PlayBreath(AudioSource source,float volume,float pitch)
        {
            if(!source)return;source.playOnAwake=false;source.loop=true;source.volume=volume;source.pitch=pitch;
            if(!source.isPlaying&&source.clip)source.Play();
        }
        public void StopBreathing(){BreathingMode=PlayerBreathingMode.Silent;effort=0;Silence(calmBreath);Silence(hardBreath);}
        public void StopMovementAudio(){StopFootsteps();StopBreathing();}
        void OnDisable(){StopMovementAudio();}
        public void Hit(bool lethal){injury.Stop();injury.volume=lethal?.85f:.7f;injury.PlayOneShot(lethal?scream:hurt);}
        public void StopInjury(){if(injury)injury.Stop();}
    }
}
