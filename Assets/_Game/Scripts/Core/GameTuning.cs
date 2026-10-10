using System;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Last Light/Tuning")]
    public sealed class GameTuning : ScriptableObject
    {
        public FlashlightTuning flashlight = new FlashlightTuning();
        public PlayerTuning player = new PlayerTuning();
        public MonsterTuning monster = new MonsterTuning();
        public int seed = 1407;
    }
    [Serializable] public sealed class FlashlightTuning
    {
        public float normalDurationSeconds = 420, minRange = 6.5f, maxRange = 18;
        public float innerHalfAngle = 18, outerHalfAngle = 24, edgeMeters = 1;
        public float boostRange = 1.25f, boostDrain = 3, boostSeconds = 3, boostRecovery = 5;
        public float swapSeconds = 2.5f, graceSeconds = 1.2f, graceRearmSeconds = 5;
        public float emergencyCharge = 35, emergencySeconds = 12;
    }
    [Serializable] public sealed class PlayerTuning
    {
        public float walkSpeed = 3, sprintSpeed = 5.2f, crouchSpeed = 1.6f;
        public float sprintSeconds = 8, recoveryDelay = 1.5f, refillSeconds = 5;
        public float standingHeight = 1.7f, crouchHeight = 1.15f, interactionRange = 2.2f;
        public float hitInvulnerability = 6, woundRecovery = 15, gravity = 20;
    }
    [Serializable] public sealed class MonsterTuning
    {
        public float radius = 0.7f, height = 1.95f, stalkSpeed = 1.8f, approachSpeed = 2.8f;
        public float huntSpeed = 4.5f, retreatSpeed = 4, attackRange = 1.8f;
        public float windupSeconds = 1.2f, warningSeconds = 1.5f, warningRange = 8;
        public float huntSeconds = 10, planningHz = 4, sensesHz = 8, memorySeconds = 8;
        public float sightRange = 45, walkHearing = 10, sprintHearing = 24;
        public float spawnMin = 35, spawnMax = 50, encounterCooldown = 40;
        public float restoreGraceSeconds = 10;
        public float pointBlankRange = 2.8f, encounterMinSeconds = 32, encounterMaxSeconds = 52;
        public float hiddenMinSeconds=10,hiddenMaxSeconds=15,vanishMinDistance=14,fogRetreatDistance=32;
        public int pathCandidates = 12;
    }
}
