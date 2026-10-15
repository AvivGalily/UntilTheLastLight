using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    // Bounded drifting billboards complement distance fog. All time belongs to
    // GameSession, so fog, wildlife and the creature freeze in menus together.
    public sealed class ForestAtmosphere : MonoBehaviour
    {
        public const float BaseDensity=.026f,ThreatDensity=.044f;
        public Renderer[] wisps;
        Vector3[] offsets;
        float[] phases;
        float indoor;
        public float ActiveTime {get;private set;}
        public void ResetSequence(int seed,Vector3 eyes)
        {
            var rng=new System.Random(seed+19371);ActiveTime=0;indoor=0;
            offsets=new Vector3[wisps.Length];phases=new float[wisps.Length];
            for(int n=0;n<wisps.Length;n++)
            {
                offsets[n]=new Vector3((float)rng.NextDouble()*84-42,.45f+(float)rng.NextDouble()*4.8f,(float)rng.NextDouble()*84-42);
                phases[n]=(float)rng.NextDouble()*Mathf.PI*2;
                wisps[n].transform.position=new Vector3(eyes.x,0,eyes.z)+offsets[n];
            }
            Shader.SetGlobalFloat("_AtmosphereTime",0);
        }
        public void Simulate(float dt,GameSession game)
        {
            if(dt<=0)return;Vector3 eyes=game.player.view.transform.position;
            if(offsets==null)ResetSequence(game.tuning.seed,eyes);
            ActiveTime+=dt;Shader.SetGlobalFloat("_AtmosphereTime",ActiveTime);
            indoor=Mathf.MoveTowards(indoor,ForestLayout.InBuilding(eyes)?1:0,dt*.6f);
            RenderSettings.fogDensity=Mathf.Lerp(BaseDensity,ThreatDensity,game.Dread.Value)*Mathf.Lerp(1,.62f,indoor);
            for(int n=0;n<wisps.Length;n++)
            {
                var wisp=wisps[n];Vector3 p=wisp.transform.position;
                p.x+=dt*(.19f+.07f*Mathf.Sin(phases[n]+ActiveTime*.12f));p.z+=dt*.10f;
                p.y=offsets[n].y+Mathf.Sin(phases[n]+ActiveTime*.18f)*.28f;
                p.x=eyes.x+Mathf.Repeat(p.x-eyes.x+42,84)-42;p.z=eyes.z+Mathf.Repeat(p.z-eyes.z+42,84)-42;
                wisp.transform.position=p;wisp.transform.rotation=Quaternion.LookRotation(p-eyes,Vector3.up);
                wisp.enabled=!ForestLayout.InBuilding(p,1.5f);
            }
            var beam=game.flashlightVisual;
            Shader.SetGlobalVector("_MistBeamPosition",beam.transform.position);
            Shader.SetGlobalVector("_MistBeamDirection",beam.transform.forward);
            Shader.SetGlobalVector("_MistBeamParams",new Vector4(beam.range,Mathf.Cos(beam.spotAngle*.5f*Mathf.Deg2Rad),game.Flashlight.Active?1:0,0));
        }
    }
}
