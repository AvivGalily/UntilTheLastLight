using UnityEngine;
namespace Game
{
    public sealed class DamageFeedback:MonoBehaviour
    {
        public float Blood{get;private set;}
        public void Trigger(){Blood=1;}
        public void ResetFeedback(){Blood=0;}
        public void Simulate(float dt){if(dt>0)Blood=Mathf.MoveTowards(Blood,0,dt*.18f);}
    }
}
