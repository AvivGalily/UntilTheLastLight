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



}
