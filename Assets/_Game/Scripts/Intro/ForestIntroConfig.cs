using System;
using UnityEngine;

namespace Game
{
    public enum ForestIntroPhase { FadeIn, DriveDialogue, GateApproach, GateCrossed, Silence, Threat, Blast, Blackout, CabinWake, Gameplay }
    [Serializable] public sealed class IntroLine
    {
        public string id, speakerKey;
        public int passenger;
        public float start, duration;
        public AudioClip hebrewVoice, englishVoice;
        public AudioClip VoiceFor(bool hebrew)=>hebrew&&hebrewVoice?hebrewVoice:englishVoice;
    }
    [CreateAssetMenu(menuName="Last Light/Forest opening")]
    public sealed class ForestIntroConfig : ScriptableObject
    {
        public float fadeIn=3, dialogueStart=8, gateApproach=50, gateCrossed=64, silence=69;
        public float discovery=80, threat=84, blast=92, blackout=92.18f, cabinWake=98, duration=110;
        public float cabinFade=3, skipHold=2, steeringLimit=.85f, gateSteeringLimit=.08f;
        public float steeringResponse=3, wheelDegrees=35, suspension=.009f, headTilt=2.2f;
        public float gateDistance=185, gateAssistDistance=20, driveDistance=265;
        public IntroLine[] lines=new IntroLine[0];
        public ForestIntroPhase PhaseAt(float t)
        {
            if(t>=duration)return ForestIntroPhase.Gameplay;
            if(t>=cabinWake)return ForestIntroPhase.CabinWake;
            if(t>=blackout)return ForestIntroPhase.Blackout;
            if(t>=blast)return ForestIntroPhase.Blast;
            if(t>=discovery)return ForestIntroPhase.Threat;
            if(t>=silence)return ForestIntroPhase.Silence;
            if(t>=gateCrossed)return ForestIntroPhase.GateCrossed;
            if(t>=gateApproach)return ForestIntroPhase.GateApproach;
            if(t>=dialogueStart)return ForestIntroPhase.DriveDialogue;
            return ForestIntroPhase.FadeIn;
        }
        public float DistanceAt(float t)
        {
            t=Mathf.Clamp(t,0,blast);
            // Smoothly slow at the narrow gate; timing stays independent of input.
            if(t<=gateApproach)return Mathf.Lerp(0,gateDistance-40,t/gateApproach);
            if(t<=gateCrossed)return Mathf.Lerp(gateDistance-40,gateDistance,(t-gateApproach)/(gateCrossed-gateApproach));
            return Mathf.Lerp(gateDistance,driveDistance,(t-gateCrossed)/(blast-gateCrossed));
        }
    }
}
