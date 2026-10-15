using UnityEngine;
namespace Game
{
    public sealed class DreadController
    {
        public float Value { get; private set; }
        public void Tick(float dt, GameSession game)
        {
            float distance = game.monster.HasSpawned ? game.monster.Distance : 100;
            float proximity = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 32, distance));
            float battery = game.Flashlight.Active ? 1 - game.Flashlight.Charge / 100 : 1;
            float state = game.monster.State == MonsterState.Hunting ? 1 : game.monster.State == MonsterState.Approaching ? .5f : 0;
            float target = Mathf.Clamp01(.7f * proximity + .15f * battery + .1f * state + .05f * (game.Inventory.HasFuse ? 1 : 0));
            if (!game.monster.HasSpawned || game.monster.State == MonsterState.Dead || game.Completed) target = 0;
            if (game.IsSafe) target = Mathf.Min(.2f, target);
            Value = Mathf.MoveTowards(Value, target, dt / (target > Value ? 2 : 6));
            RenderSettings.fogDensity = Mathf.Lerp(ForestAtmosphere.BaseDensity, ForestAtmosphere.ThreatDensity, Value);
            Shader.SetGlobalFloat("_Dread", Value);
            if (game.monster.breath)
            {
                game.monster.breath.volume = Mathf.Clamp01((32 - distance) / 28) * .7f;
                // The source uses a flat distance curve: attenuation is applied exactly once above.
                game.monster.breath.pitch = Mathf.Lerp(.7f, 1.1f, Value);
            }
        }
        public void Reset() { Value = 0; Shader.SetGlobalFloat("_Dread",0);RenderSettings.fogDensity=ForestAtmosphere.BaseDensity; }
    }
}
