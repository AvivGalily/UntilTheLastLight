using UnityEngine;

namespace Game
{
    // Quiet wildlife stays separate from the monster's mandatory attack cue.
    public sealed class ForestAmbience : MonoBehaviour
    {
        public AudioSource insects, birds, frogs, leaves;
        public AudioClip owlClip, nightjarClip, frogClip, leafClip;
        public AudioLowPassFilter[] filters;
        [Range(0,.015f)] public float cricketVolume=.012f;
        System.Random rng;
        float nextBird, nextFrog, nextLeaves, indoor, gain=1;
        public float ActiveTime { get; private set; }
        public int BirdCalls { get; private set; }

        public void ResetSequence(int seed)
        {
            rng=new System.Random(seed+82147);ActiveTime=0;BirdCalls=0;indoor=0;gain=1;
            nextBird=5+(float)rng.NextDouble()*5;nextFrog=14+(float)rng.NextDouble()*12;nextLeaves=8+(float)rng.NextDouble()*7;
            foreach(var source in new[]{insects,birds,frogs,leaves})if(source)source.Stop();
            // Reset stays silent. The loop starts only from active gameplay
            // near a building, never during the opening or checkpoint setup.
            if(insects){insects.volume=0;insects.loop=true;insects.playOnAwake=false;}
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;
            if(!game.State.Running){if(insects){insects.Stop();insects.volume=0;}return;}
            if(rng==null)ResetSequence(game.tuning.seed);ActiveTime+=dt;
            Vector3 eyes=game.player.view.transform.position;
            indoor=Mathf.MoveTowards(indoor,ForestLayout.InBuilding(eyes)?1:0,dt*.8f);
            float threat=game.monster.HasSpawned?HorrorAudio.Proximity(game.monster.Distance,game.IsSafe):0;
            float target=(game.monster.warningSource&&game.monster.warningSource.isPlaying) ? .12f : Mathf.Lerp(1,.28f,threat);
            gain=Mathf.MoveTowards(gain,target,dt*3);
            float level=gain*Mathf.Lerp(1,.38f,indoor);
            float zone=CricketZone(eyes);
            if(insects)
            {
                if(zone<=0||!insects.clip){insects.Stop();insects.volume=0;}
                else
                {
                    insects.volume=Mathf.MoveTowards(insects.volume,Mathf.Clamp(cricketVolume,0,.015f)*zone*level,dt*.012f);
                    if(insects.volume>0&&!insects.isPlaying)insects.Play();
                }
            }
            birds.volume=.32f*level;frogs.volume=.16f*level;leaves.volume=.18f*level;
            foreach(var filter in filters)if(filter)filter.cutoffFrequency=Mathf.Lerp(8500,1800,indoor);
            nextBird-=dt;nextFrog-=dt;nextLeaves-=dt;
            if(nextBird<=0&&threat<.6f&&gain>.3f)
            {
                Position(birds,eyes,22,46,6);birds.pitch=.94f+(float)rng.NextDouble()*.13f;
                birds.PlayOneShot(rng.Next(2)==0?owlClip:nightjarClip);BirdCalls++;
                nextBird=18+(float)rng.NextDouble()*22;
            }
            if(nextFrog<=0&&threat<.5f)
            {Position(frogs,eyes,18,35,.25f);frogs.pitch=.9f+(float)rng.NextDouble()*.18f;frogs.PlayOneShot(frogClip);nextFrog=28+(float)rng.NextDouble()*28;}
            if(nextLeaves<=0&&threat<.65f)
            {Position(leaves,eyes,7,18,1);leaves.pitch=.88f+(float)rng.NextDouble()*.25f;leaves.PlayOneShot(leafClip);nextLeaves=12+(float)rng.NextDouble()*17;}
        }
        public static float CricketZone(Vector3 position)
        {
            // Horizontal distance from the walls, including the larger station.
            float distance=Mathf.Min(Mathf.Min(FootprintDistance(position,ForestLayout.Cabin,5,5),
                FootprintDistance(position,ForestLayout.Store,5,5)),FootprintDistance(position,ForestLayout.Research,9,7));
            return Mathf.SmoothStep(1,0,Mathf.InverseLerp(2,10,distance));
        }
        static float FootprintDistance(Vector3 p,Vector3 centre,float halfWidth,float halfDepth)
        {
            float x=Mathf.Max(0,Mathf.Abs(p.x-centre.x)-halfWidth),z=Mathf.Max(0,Mathf.Abs(p.z-centre.z)-halfDepth);
            return Mathf.Sqrt(x*x+z*z);
        }
        void Position(AudioSource source,Vector3 eyes,float min,float max,float height)
        {
            float angle=(float)rng.NextDouble()*Mathf.PI*2,distance=Mathf.Lerp(min,max,(float)rng.NextDouble());
            source.transform.position=new Vector3(eyes.x+Mathf.Sin(angle)*distance,height,eyes.z+Mathf.Cos(angle)*distance);
        }
    }
}
