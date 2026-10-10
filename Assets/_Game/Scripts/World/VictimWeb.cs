using UnityEngine;
namespace Game
{
    // Fixed silk scaffold with bone-attached bindings; the impact releases the body.
    public sealed class VictimWeb:MonoBehaviour
    {
        public Renderer intactCore;
        public LineRenderer[] supports,bodyBands;
        public Transform[] supportBones,bandBones;
        public Vector3[] boneOffsets,fixedAnchors,bandOffsets;
        public float[] bandHalfWidths;
        public bool Released {get;private set;}
        public int TearCount {get;private set;}
        public float TearSeconds {get;private set;}
        Vector3[] looseEnds;
        public void ResetWeb(bool released)
        {
            Released=released;TearCount=released?1:0;TearSeconds=released?1:0;
            intactCore.enabled=!released;looseEnds=new Vector3[supports.Length];
            foreach(var line in supports)line.enabled=!released;
            foreach(var line in bodyBands)line.enabled=!released;
            if(!released)UpdateBindings();
        }
        public void Release()
        {
            if(Released)return;Released=true;TearCount++;TearSeconds=0;intactCore.enabled=false;
            if(looseEnds==null||looseEnds.Length!=supports.Length)looseEnds=new Vector3[supports.Length];
            for(int i=0;i<supports.Length;i++)looseEnds[i]=supports[i].GetPosition(2);
            foreach(var line in bodyBands)line.enabled=false;
        }
        public void Simulate(float dt)
        {
            if(dt<=0)return;
            if(!Released){UpdateBindings();return;}
            TearSeconds+=dt;float t=Mathf.Clamp01(TearSeconds/.65f);
            for(int i=0;i<supports.Length;i++)
            {
                var line=supports[i];line.enabled=t<1;if(t>=1)continue;
                Vector3 fixedEnd=transform.TransformPoint(fixedAnchors[i]);
                Vector3 loose=Vector3.Lerp(looseEnds[i],fixedEnd,Mathf.SmoothStep(0,1,t))+Vector3.down*(1.8f*t*t);
                line.SetPosition(0,fixedEnd);line.SetPosition(1,Vector3.Lerp(fixedEnd,loose,.5f)+Vector3.down*.18f*t);line.SetPosition(2,loose);
            }
        }
        void UpdateBindings()
        {
            for(int i=0;i<supports.Length;i++)
            {
                Vector3 fixedEnd=transform.TransformPoint(fixedAnchors[i]),body=supportBones[i].TransformPoint(boneOffsets[i]);
                supports[i].SetPosition(0,fixedEnd);supports[i].SetPosition(1,Vector3.Lerp(fixedEnd,body,.5f)+Vector3.back*.025f);supports[i].SetPosition(2,body);
            }
            for(int i=0;i<bodyBands.Length;i++)
            {
                Vector3 center=bandBones[i].TransformPoint(bandOffsets[i]);float width=bandHalfWidths[i];
                bodyBands[i].SetPosition(0,center+Vector3.left*width);bodyBands[i].SetPosition(1,center+Vector3.back*.07f);bodyBands[i].SetPosition(2,center+Vector3.right*width);
            }
        }
    }
}
