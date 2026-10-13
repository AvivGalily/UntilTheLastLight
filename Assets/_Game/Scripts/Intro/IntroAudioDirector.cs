using UnityEngine;
namespace Game
{
    public sealed class IntroAudioDirector : MonoBehaviour
    {
        public AudioSource vehicleAmbience,gate,threat,effects,ringing,breath;
        public AudioClip gateClip,threatClip,blastClip,wakeCreak;
        bool gated,threatened,blasted,woke;
        public bool GateStarted=>gated;
        public bool ThreatStarted=>threatened;
        public bool BlastStarted=>blasted;
        float speechDuck;
        public void PlayGate(){if(!gated){gated=true;gate.PlayOneShot(gateClip);}}
        public void ResetAudio()
        {
            StopAudio();gated=threatened=blasted=woke=false;speechDuck=0;
            vehicleAmbience.Play();
        }
        public void StopAudio(){foreach(var source in GetComponentsInChildren<AudioSource>(true))source.Stop();}
        public void MixDialogue(float dt,bool speaking)
        {
            speechDuck=Mathf.MoveTowards(speechDuck,speaking?1:0,dt*(speaking?12:3));
            float bed=Mathf.Lerp(1,.65f,speechDuck);
            vehicleAmbience.volume*=bed;gate.volume*=bed;
            threat.volume*=Mathf.Lerp(1,.4f,speechDuck);
        }
        public void Simulate(float t,ForestIntroConfig c,Transform vehicle)
        {
            bool drive=t<c.blackout;
            float fade=1-Mathf.InverseLerp(c.silence,c.silence+4,t);
            vehicleAmbience.volume=drive?.18f*fade:0;
            gate.volume=.28f;
            // Use the supplied approach once, just before Ethan hears it. Its
            // movement reaches the vehicle at the authored threat beat.
            float approach=c.discovery-.25f;
            if(!threatened&&t>=approach){threatened=true;threat.PlayOneShot(threatClip);}
            float close=Mathf.SmoothStep(0,1,Mathf.InverseLerp(approach,c.threat,t));
            threat.transform.position=vehicle.TransformPoint(Vector3.Lerp(new Vector3(-14,2,-12),new Vector3(-2,1.6f,1),close));
            threat.volume=drive?Mathf.Lerp(.18f,.70f,close):0;
            if(!blasted&&t>=c.blast){blasted=true;effects.PlayOneShot(blastClip,.8f);ringing.Play();breath.Play();}
            ringing.volume=.065f*(1-Mathf.InverseLerp(c.cabinWake,c.duration,t));
            breath.volume=t>=c.blackout?.10f:0;
            if(!woke&&t>=c.cabinWake){woke=true;effects.PlayOneShot(wakeCreak,.13f);}
        }
    }
}
