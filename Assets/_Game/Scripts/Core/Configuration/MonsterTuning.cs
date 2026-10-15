using System;
namespace Game
{
    [Serializable] public sealed class MonsterTuning
    {
        public float radius = 0.7f, height = 1.95f, stalkSpeed = 1.8f, approachSpeed = 3.5f;
        public float huntSpeed = 3.5f, retreatSpeed = 4, attackRange = 1.5f;
        public float windupSeconds = 3, warningSeconds = 1.5f, warningRange = 8;
        public float flashlightMinSeconds = 4, flashlightMaxSeconds = 7;
        public float huntSeconds = 10, planningHz = 4, sensesHz = 8, memorySeconds = 8;
        public float sightRange = 45, walkHearing = 10, sprintHearing = 24;
        public float spawnMin = 35, spawnMax = 50, encounterCooldown = 40;
        public float restoreGraceSeconds = 10;
        public float pointBlankRange = 1.5f, encounterMinSeconds = 32, encounterMaxSeconds = 52;
        public float hiddenMinSeconds=10,hiddenMaxSeconds=15,vanishMinDistance=14,fogRetreatDistance=32;
        public int pathCandidates = 12;
    }
}
