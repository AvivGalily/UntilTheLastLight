using UnityEngine;
namespace Game
{
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
            if (warningAudible) WarningElapsed += dt;
            if (!allowed || !inRange) { Cancel(); return false; }
            // Continuous close contact counts while either actor is moving.
            if (!WindingUp) { WindingUp = true; WindupElapsed = 0; committed = false; }
            WindupElapsed += dt;
            if (WindupElapsed < tuning.windupSeconds || committed) return false;
            committed = true; return true;
        }
    }
}
