using UnityEngine;
namespace Game
{
    public sealed class MonsterSenses
    {
        public bool ObservedNow { get; private set; }
        public bool HasMemory => MemoryRemaining > 0;
        public Vector3 LastKnown { get; private set; }
        public float MemoryRemaining { get; private set; }
        float nextSense;
        public void Tick(float dt, Vector3 feet, PlayerMotor player, MonsterTuning tuning, LightProtectionService light)
        {
            MemoryRemaining = Mathf.Max(0, MemoryRemaining - dt);
            nextSense -= dt;
            if (nextSense > 0) return;
            nextSense = 1 / tuning.sensesHz;
            float distance = Vector3.Distance(feet, player.transform.position);
            bool sees = distance <= tuning.sightRange && light.Clear(feet + Vector3.up * 2.05f, player.view.transform.position);
            bool hears = player.Moving && !player.Crouching && distance <= (player.Sprinting ? tuning.sprintHearing : tuning.walkHearing);
            ObservedNow = sees || hears;
            if (ObservedNow) { LastKnown = player.transform.position; MemoryRemaining = tuning.memorySeconds; }
        }
        public void Reset() { ObservedNow = false; MemoryRemaining = nextSense = 0; }
        public void Remember(Vector3 position,float seconds){LastKnown=position;MemoryRemaining=seconds;}
    }
    public sealed class MonsterAttack
    {
        public float WarningElapsed { get; private set; }
        public float WindupElapsed { get; private set; }
        public bool WindingUp { get; private set; }
        bool committed;
        public void ResetEncounter() { WarningElapsed = 0; Cancel(); }
        public void Cancel() { WindingUp = false; WindupElapsed = 0; committed = false; }
        public bool Tick(float dt, bool warningAudible, bool allowed, bool inRange, MonsterTuning tuning)
        {
            if (dt <= 0) return false;
            // Even on a very long frame, warning and windup cannot collapse into one update.
            bool warnedBeforeFrame = WarningElapsed >= tuning.warningSeconds;
            if (warningAudible) WarningElapsed += dt;
            if (!allowed || !inRange) { Cancel(); return false; }
            if (!warnedBeforeFrame) return false;
            if (!WindingUp) { WindingUp = true; WindupElapsed = 0; committed = false; return false; }
            WindupElapsed += dt;
            if (WindupElapsed < tuning.windupSeconds || committed) return false;
            committed = true; return true;
        }
    }
}
