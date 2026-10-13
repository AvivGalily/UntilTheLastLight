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
        public float cabinFade=3, skipHold=2, steeringLimit=1.6f, gateSteeringLimit=.08f;
        public float steeringResponse=4.5f, wheelDegrees=45, suspension=.009f, headTilt=2.2f;
        public float gateDistance=185, gateAssistDistance=20, driveDistance=265;
        public float boostMultiplier=1.6f, boostLead=12, gateOpenAngle=90, gateClearance=6, gateCloseDuration=1.1f;
        [Header("Seated head movement")]
        [Range(90,179)] public float lookYawLimit=175;
        [Range(20,70)] public float lookPitchLimit=65;
        [Range(.5f,2)] public float headLookMultiplier=1.35f;
        public float sideLean=.07f, rearLean=.32f, controlsHintDuration=12;
        public float seatedViewPitch=8;
        public IntroLine[] lines=new IntroLine[0];
        public Vector3 HeadOffset(float yaw)
        {
            float angle=Mathf.Abs(yaw);
            float rear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,140,angle));
            float side=Mathf.Sin(Mathf.Min(angle,180)*Mathf.Deg2Rad);
            // Lean slightly toward side windows; turn into the centre aisle
            // only for the rearward view, keeping the camera inside the cabin.
            return new Vector3(Mathf.Lerp(Mathf.Sign(yaw)*sideLean*side,rearLean,rear),
                Mathf.Lerp(.02f*side,.07f,rear),.02f*rear);
        }
        public float BoostEnvelope(float t)
        {
            if(t<gateCrossed)
                return Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,8,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(gateApproach,gateCrossed-1,t)));
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(gateCrossed+2,gateCrossed+8,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(blast-16,blast-1,t)));
        }
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
