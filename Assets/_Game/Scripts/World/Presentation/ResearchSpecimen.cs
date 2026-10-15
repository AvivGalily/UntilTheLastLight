using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    public enum ResearchPhase { Sealed, Draining, Opening, Emerging, Hunting, Dead }
    // One active-time clock owns the alarm, water, doors, escape and combat handoff.
    public sealed class ResearchSpecimen : MonoBehaviour
    {
        public Transform suspended,water,waterSurface,leftDoor,rightDoor,beaconRotor,keyVisual,keyChain,lifeSupport;
        public Transform[] bubbles,attachedHoses;
        public Light alarmLight;
        public Renderer beaconGlass;
        public AudioSource alarm,mechanism;
        public AudioClip alarmClip,openClip,drainClip;
        public Collider tankSolid,lootCollider;
        public NavMeshObstacle tankObstacle;
        public AquaticMonster monster;
        public ResearchPhase Phase {get;private set;}
        public float PhaseTime {get;private set;}
        public float WaterFraction=>Phase==ResearchPhase.Sealed?1:Phase==ResearchPhase.Draining?1-Mathf.Clamp01(PhaseTime/5):0;
        public bool KeyTaken {get;private set;}
        public float KeyGlow {get;private set;}
        public Vector3 ExitPoint=>transform.TransformPoint(new Vector3(0,0,2.4f));
        Vector3 rest,waterRest,surfaceRest;
        Vector3[] hoseRest;
        Quaternion[] hoseRotations;
        float clock;
        bool paused;
        bool cached;
        MaterialPropertyBlock beaconBlock;
        MaterialPropertyBlock keyBlock;
        Renderer[] keyRenderers;
        GameSession session;
        void Awake()=>Cache();
        void Cache()
        {
            if(cached)return;cached=true;beaconBlock=new MaterialPropertyBlock();
            rest=suspended.localPosition;waterRest=water?water.localPosition:Vector3.zero;surfaceRest=waterSurface?waterSurface.localPosition:Vector3.zero;
            hoseRest=new Vector3[attachedHoses?.Length??0];hoseRotations=new Quaternion[hoseRest.Length];
            for(int i=0;i<hoseRest.Length;i++){hoseRest[i]=attachedHoses[i].localPosition;hoseRotations[i]=attachedHoses[i].localRotation;}
        }
        public bool Release(GameSession game)
        {
            if(Phase!=ResearchPhase.Sealed||!game.State.Running)return false;
            session=game;Phase=ResearchPhase.Draining;PhaseTime=0;paused=false;
            alarm.clip=alarmClip;alarm.time=0;alarm.Play();mechanism.PlayOneShot(drainClip,.55f);
            game.Notify(game.Text("containment_warning"),5);game.SaveCheckpoint("checkpoint_release");Apply();return true;
        }
        public void SetPaused(bool value)
        {
            if(paused==value)return;paused=value;
            if(value){alarm?.Pause();mechanism?.Pause();monster?.SetPaused(true);}
            else{alarm?.UnPause();mechanism?.UnPause();monster?.SetPaused(false);}
        }
        public void Simulate(float dt,GameSession game=null)
        {
            Cache();
            if(game)session=game;
            if(game&&!game.State.Running){SetPaused(true);return;}
            if(dt<=0)return;
            clock+=dt;
            float remaining=dt,combatTime=0,deadTime=0;
            while(remaining>0)
            {
                float duration=Phase==ResearchPhase.Draining?5:Phase==ResearchPhase.Opening?1.2f:Phase==ResearchPhase.Emerging?4:float.PositiveInfinity;
                float step=Mathf.Min(remaining,Mathf.Max(0,duration-PhaseTime));
                if(Phase==ResearchPhase.Hunting)combatTime+=step;if(Phase==ResearchPhase.Dead)deadTime+=step;
                PhaseTime+=step;remaining-=step;
                if(PhaseTime<duration)break;
                PhaseTime=0;Phase++;
                if(Phase==ResearchPhase.Opening){alarm.Stop();mechanism.PlayOneShot(openClip,.8f);}
                if(Phase==ResearchPhase.Hunting){monster.transform.SetParent(transform.parent,true);monster.transform.position=ExitPoint;monster.BeginHunt();}
            }
            Apply();
            if(Phase==ResearchPhase.Hunting&&game)monster.Simulate(combatTime,game);
            else if(Phase==ResearchPhase.Dead){monster.AdvanceDeath(deadTime);if(lootCollider)lootCollider.enabled=monster.DeathSettled&&!KeyTaken;}
        }
        void Apply()
        {
            float fraction=WaterFraction;
            if(water){water.localScale=new Vector3(1,Mathf.Max(.002f,fraction),1);water.gameObject.SetActive(fraction>.002f);water.localPosition=waterRest;}
            if(waterSurface){waterSurface.localPosition=new Vector3(surfaceRest.x,waterRest.y+2.51f*fraction,surfaceRest.z);waterSurface.gameObject.SetActive(fraction>.002f);}
            bool warning=Phase==ResearchPhase.Draining;
            if(alarmLight){alarmLight.enabled=warning;alarmLight.intensity=3+Mathf.Sin(clock*12)*1.6f;}
            if(beaconGlass)
            {
                beaconBlock.SetColor("_EmissionColor",warning?new Color(5,.015f,.003f):new Color(.04f,0,0));beaconGlass.SetPropertyBlock(beaconBlock);
            }
            if(beaconRotor)beaconRotor.localRotation=Quaternion.Euler(0,clock*210,0);
            float opened=Phase==ResearchPhase.Opening?Mathf.SmoothStep(0,1,Mathf.Clamp01(PhaseTime/1.2f)):Phase>=ResearchPhase.Emerging?1:0;
            if(leftDoor)leftDoor.localRotation=Quaternion.Euler(0,-opened*100,0);
            if(rightDoor)rightDoor.localRotation=Quaternion.Euler(0,opened*100,0);
            if(tankSolid)tankSolid.enabled=Phase<ResearchPhase.Opening;
            if(tankObstacle)tankObstacle.enabled=Phase<ResearchPhase.Opening;
            bool inside=Phase<=ResearchPhase.Emerging;
            if(inside)
            {
                suspended.localPosition=rest+(Phase==ResearchPhase.Sealed||Phase==ResearchPhase.Draining?Vector3.up*(Mathf.Sin(clock*.65f)*.026f):Vector3.zero);
                suspended.localRotation=Phase==ResearchPhase.Sealed?Quaternion.Euler(Mathf.Sin(clock*.31f)*1.4f,Mathf.Sin(clock*.22f)*2,Mathf.Sin(clock*.4f)*1.5f):Quaternion.identity;
            }
            if(Phase<ResearchPhase.Emerging)monster?.Pose(0,clock,0);
            if(Phase==ResearchPhase.Emerging&&monster)
            {
                float t=Mathf.Clamp01(PhaseTime/4);
                monster.transform.position=Vector3.Lerp(suspended.position,ExitPoint,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.92f,t)));
                monster.transform.rotation=transform.rotation;monster.Pose(1,PhaseTime,0);
            }
            for(int i=0;i<(attachedHoses?.Length??0);i++)
            {
                float drop=Phase==ResearchPhase.Emerging?Mathf.Clamp01(PhaseTime/.8f):Phase>ResearchPhase.Emerging?1:0;
                attachedHoses[i].gameObject.SetActive(drop<1);
                attachedHoses[i].localPosition=hoseRest[i]-Vector3.up*drop*.4f;
                attachedHoses[i].localRotation=hoseRotations[i]*Quaternion.Euler(0,0,drop*(i%2==0?28:-28));
            }
            if(lifeSupport)lifeSupport.localPosition=inside?suspended.localPosition:rest;
            PresentKey();
            for(int i=0;i<(bubbles?.Length??0);i++)
            {
                float height=.34f+Mathf.Repeat(clock*.12f+i*.137f,1)*2.35f;
                bubbles[i].gameObject.SetActive(fraction>.01f&&height<waterRest.y+2.51f*fraction);
                bubbles[i].localPosition=new Vector3(Mathf.Sin(i*9.2f+clock*.6f)*.53f,height,Mathf.Cos(i*5.7f)*.53f);
            }
        }
        public void Killed()
        {
            Phase=ResearchPhase.Dead;PhaseTime=0;alarm.Stop();
            PresentKey();
            session?.Notify(session.Text("specimen_down"),7);session?.SaveCheckpoint("checkpoint_specimen_dead");
        }
        public bool TakeKey(GameSession game)
        {
            if(Phase!=ResearchPhase.Dead||!monster.DeathSettled||KeyTaken)return false;
            KeyTaken=true;game.Inventory.HasGateKey=true;PresentKey();lootCollider.enabled=false;
            game.Notify(game.Text("gate_key_acquired"),9);game.SaveCheckpoint("checkpoint_specimen_key");return true;
        }
        public ResearchSave Capture()=>new ResearchSave{phase=(int)Phase,elapsed=Mathf.Min(PhaseTime,600),health=monster?monster.Health:4,x=monster?monster.transform.position.x:0,y=monster?monster.transform.position.y:0,z=monster?monster.transform.position.z:0,yaw=monster?monster.transform.eulerAngles.y:0};
        public void Restore(ResearchSave data=null,bool keyTaken=false)
        {
            Cache();
            data=data??new ResearchSave();Phase=(ResearchPhase)data.phase;PhaseTime=data.elapsed;KeyTaken=keyTaken;clock=0;paused=false;alarm.Stop();mechanism.Stop();
            suspended.SetLocalPositionAndRotation(rest,Quaternion.identity);
            monster.transform.SetParent(suspended,false);monster.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);monster.ResetMonster(data.health,Phase==ResearchPhase.Dead);
            if(Phase>=ResearchPhase.Hunting)
            {
                monster.transform.SetParent(transform.parent,true);
                Vector3 position=new Vector3(data.x,data.y,data.z);
                if(position.sqrMagnitude<.01f||!NavMesh.SamplePosition(position,out var hit,2,NavMesh.AllAreas))position=ExitPoint;
                else position=hit.position;
                monster.transform.SetPositionAndRotation(position,Quaternion.Euler(0,data.yaw,0));
                if(Phase==ResearchPhase.Hunting)monster.BeginHunt();else monster.AdvanceDeath(10);
            }
            PresentKey();
            if(lootCollider)lootCollider.enabled=Phase==ResearchPhase.Dead&&!KeyTaken;
            if(Phase==ResearchPhase.Draining){alarm.clip=alarmClip;alarm.time=Mathf.Min(PhaseTime,alarmClip.length-.01f);if(Application.isPlaying)alarm.Play();}
            Apply();
        }
        void PresentKey()
        {
            if(!keyVisual)return;
            keyVisual.gameObject.SetActive(!KeyTaken);
            KeyGlow=Phase==ResearchPhase.Dead&&!KeyTaken?Mathf.Pow(.5f+.5f*Mathf.Cos(PhaseTime*Mathf.PI*2*1.25f),3):0;
            if(keyRenderers==null){keyRenderers=keyVisual.GetComponentsInChildren<Renderer>(true);keyBlock=new MaterialPropertyBlock();}
            foreach(var renderer in keyRenderers)
            {
                renderer.GetPropertyBlock(keyBlock);keyBlock.SetColor("_EmissionColor",new Color(5.5f,2.7f,.38f)*KeyGlow);renderer.SetPropertyBlock(keyBlock);
            }
        }
    }
}
