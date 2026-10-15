using System;
namespace Game
{
    [Serializable] public sealed class FlashlightTuning
    {
        public float normalDurationSeconds = 420, minRange = 6.5f, maxRange = 18;
        public float innerHalfAngle = 18, outerHalfAngle = 24, edgeMeters = 1;
        public float boostRange = 1.25f, boostDrain = 3, boostSeconds = 3, boostRecovery = 5;
        public float swapSeconds = 2.5f, graceSeconds = 1.2f, graceRearmSeconds = 5;
        public float emergencyCharge = 35, emergencySeconds = 12;
    }
}
