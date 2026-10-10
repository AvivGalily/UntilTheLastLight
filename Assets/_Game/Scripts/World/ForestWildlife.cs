using UnityEngine;
namespace Game
{
    public sealed class ForestWildlife:MonoBehaviour
    {
        public Transform[] treeVisuals;
        public CrowFlight[] crows;
        Quaternion[] rests;
        float clock;
        public float WindClock=>clock;
        public void ResetForest()
        {
            if(rests==null){rests=new Quaternion[treeVisuals.Length];for(int i=0;i<rests.Length;i++)rests[i]=treeVisuals[i].localRotation;}
            clock=0;for(int i=0;i<rests.Length;i++)treeVisuals[i].localRotation=rests[i];
            foreach(var crow in crows)crow.ResetBird();
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0||!game.State.Running)return;if(rests==null)ResetForest();clock+=dt;
            for(int i=0;i<treeVisuals.Length;i++)
            {
                var tree=treeVisuals[i];float offset=tree.position.x*.13f+tree.position.z*.23f;
                float gust=.6f+.4f*Mathf.Sin(clock*.21f+offset*.4f);
                tree.localRotation=Quaternion.Euler(Mathf.Sin(clock*.73f+offset)*.55f*gust,0,Mathf.Sin(clock*.52f+offset)*.85f*gust)*rests[i];
            }
            foreach(var crow in crows)crow.Simulate(dt,game.player.transform.position,clock);
        }
    }
}
