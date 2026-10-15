using UnityEngine;
namespace Game
{
    public sealed class HorrorAudio:MonoBehaviour
    {
        public AudioSource clicks,tension,pulse,wolf,monsterVoice;
        public AudioClip clickClip,pulseClip,howlClip,growlClip,roarClip;
        System.Random rng;
        float nextClick,nextPulse,nextHowl;
        float nextVoice;int knownEncounter;
        public int RoarsPlayed {get;private set;}
        public int GrowlsPlayed {get;private set;}
        public float Threat {get;private set;}
        public void ResetSequence(int seed)
        {rng=new System.Random(seed);nextClick=.4f;nextPulse=.6f;nextHowl=20+(float)rng.NextDouble()*15;nextVoice=2;knownEncounter=RoarsPlayed=GrowlsPlayed=0;Threat=0;foreach(var s in new[]{clicks,tension,pulse,wolf,monsterVoice})if(s){s.Stop();s.volume=0;}if(tension)tension.Play();}
        public static float Proximity(float distance,bool safe)=>safe?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(35,3,distance));
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;if(rng==null)ResetSequence(game.tuning.seed);
            var monster=game.monster;
            float target=monster.HasSpawned&&!game.Completed?Proximity(monster.Distance,game.IsSafe):0;
            Threat=Mathf.MoveTowards(Threat,target,dt*.7f);
            tension.volume=Threat*Threat*.38f;tension.pitch=Mathf.Lerp(.82f,1.12f,Threat);
            nextClick-=dt;nextPulse-=dt;nextHowl-=dt;
            if(nextClick<=0&&target>.015f&&monster.ActualSpeed>.08f)
            {
                clicks.volume=Mathf.Lerp(.07f,.8f,target);clicks.pitch=Mathf.Lerp(.8f,1.25f,target)+(float)rng.NextDouble()*.08f;
                clicks.PlayOneShot(clickClip);nextClick=Mathf.Lerp(2.2f,.32f,target)*(.82f+(float)rng.NextDouble()*.35f);
            }
            if(nextPulse<=0&&Threat>.35f)
            {pulse.volume=Mathf.InverseLerp(.35f,1,Threat)*.43f;pulse.pitch=Mathf.Lerp(.85f,1.3f,Threat);pulse.PlayOneShot(pulseClip);nextPulse=Mathf.Lerp(1.15f,.52f,Threat);}
            // Keep the explicit pre-attack cue clear. Howls are occasional, with randomized distant positions.
            if(nextHowl<=0&&(!monster.HasSpawned||monster.Distance>12||game.IsSafe))
            {
                float angle=(float)rng.NextDouble()*Mathf.PI*2;
                wolf.transform.position=game.player.transform.position+new Vector3(Mathf.Sin(angle)*60,8,Mathf.Cos(angle)*60);
                wolf.volume=.48f;wolf.pitch=.86f+(float)rng.NextDouble()*.23f;wolf.PlayOneShot(howlClip);
                if(game.visualWarnings)game.Notify(game.Text("wolf_caption"),4);
                nextHowl=45+(float)rng.NextDouble()*45;
            }
            if(target==0){clicks.volume=0;pulse.volume=0;}
            if(monster.ActualSpeed<=.08f){clicks.Stop();nextClick=0;}
            if(monsterVoice)
            {
                nextVoice-=dt;
                if(!monster.HasSpawned||!monster.gameObject.activeInHierarchy||game.Completed||game.IsSafe)
                {monsterVoice.Stop();monsterVoice.volume=0;}
                else if(monster.Attack.WindingUp)
                {monsterVoice.Stop();nextVoice=Mathf.Max(nextVoice,1.5f);}
                else
                {
                    float proximity=Mathf.SmoothStep(0,1,Mathf.InverseLerp(60,2,monster.Distance));
                    monsterVoice.volume=Mathf.Lerp(.06f,.78f,proximity);monsterVoice.pitch=.82f;
                    bool returned=knownEncounter!=monster.EncounterCount;
                    if(returned||nextVoice<=0&&monster.Distance<38)
                    {
                        var clip=returned?roarClip:growlClip;
                        if(clip){monsterVoice.PlayOneShot(clip);if(returned)RoarsPlayed++;else GrowlsPlayed++;nextVoice=clip.length/monsterVoice.pitch+Mathf.Lerp(8,3,proximity);}
                        knownEncounter=monster.EncounterCount;
                    }
                }
            }
        }
    }
}
