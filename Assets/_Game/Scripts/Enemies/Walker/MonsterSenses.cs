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

}
