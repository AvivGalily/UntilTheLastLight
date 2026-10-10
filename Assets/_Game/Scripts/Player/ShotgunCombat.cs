using UnityEngine;
namespace Game
{
    public sealed class ShotgunCombat:MonoBehaviour
    {
        public Transform heldModel;
        public Light muzzleLight;
        public ParticleSystem muzzleSmoke;
        public AudioSource voice;
        public AudioClip fireClip,reloadClip,emptyClip;
        public float ReloadRemaining {get;private set;}
        public int Shots {get;private set;}
        public int Hits {get;private set;}
        Vector3 restPosition;
        Quaternion restRotation;
        float cooldown,recoil,flash;
        void Awake(){restPosition=heldModel.localPosition;restRotation=heldModel.localRotation;}
        public void ResetCombat(){ReloadRemaining=cooldown=recoil=flash=0;Shots=Hits=0;voice.Stop();muzzleLight.enabled=false;heldModel.localPosition=restPosition;heldModel.localRotation=restRotation;muzzleSmoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        public bool BeginReload(GameSession game)
        {
            if(!game.State.Running||!game.Inventory.ShotgunEquipped||ReloadRemaining>0||game.Inventory.LoadedShells>=2||game.Inventory.SpareShells<=0)return false;
            ReloadRemaining=1.7f;voice.PlayOneShot(reloadClip,.7f);return true;
        }
        public bool Fire(GameSession game)
        {
            if(!game.State.Running||!game.Inventory.ShotgunEquipped||cooldown>0||ReloadRemaining>0)return false;
            if(!game.Inventory.FireShell()){cooldown=.3f;voice.PlayOneShot(emptyClip,.6f);game.Notify(game.Text("shotgun_empty"));return false;}
            Shots++;cooldown=.85f;recoil=1;flash=.065f;muzzleLight.enabled=true;muzzleSmoke.Emit(12);voice.PlayOneShot(fireClip,.9f);
            var eyes=game.player.view.transform;CinderHead head=null;ZombieWolf wolf=null;float nearest=18;
            if(game.norton)foreach(var e in game.norton.enemies)
            {if(e.Alive&&e.gameObject.activeSelf&&Aim(eyes,e.AimPoint,game,ref nearest)){head=e;wolf=null;}}
            foreach(var e in game.wolves)
            {if(e.Alive&&Aim(eyes,e.transform.position+Vector3.up*.65f,game,ref nearest)){wolf=e;head=null;}}
            if(head){head.Hit(2);Hits++;}else if(wolf){wolf.Hit();wolf.Hit();Hits++;}
            return true;
        }
        static bool Aim(Transform eyes,Vector3 target,GameSession game,ref float nearest)
        {
            Vector3 delta=target-eyes.position;
            if(delta.magnitude>nearest||Vector3.Angle(eyes.forward,delta)>6.5f||!game.Protection.Clear(eyes.position,target))return false;
            nearest=delta.magnitude;return true;
        }
        public void Simulate(float dt,GameSession game)
        {
            bool selected=game.Inventory.ShotgunEquipped;heldModel.gameObject.SetActive(selected);
            if(dt<=0||!game.State.Running)return;cooldown-=dt;recoil=Mathf.MoveTowards(recoil,0,dt*5);flash-=dt;muzzleLight.enabled=selected&&flash>0;
            muzzleSmoke.Simulate(dt,true,false,false);muzzleSmoke.Pause();
            if(ReloadRemaining>0)
            {
                ReloadRemaining=Mathf.Max(0,ReloadRemaining-dt);
                if(ReloadRemaining==0)game.Inventory.ReloadShotgun();
            }
            float reload=ReloadRemaining>0?Mathf.Sin((1-ReloadRemaining/1.7f)*Mathf.PI):0;
            heldModel.localPosition=restPosition+new Vector3(0,-reload*.16f,-recoil*.08f);
            heldModel.localRotation=restRotation*Quaternion.Euler(-recoil*9+reload*25,0,-reload*12);
        }
    }
}
