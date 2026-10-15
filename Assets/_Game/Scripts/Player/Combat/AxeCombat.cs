using UnityEngine;
namespace Game
{
    public sealed class AxeCombat:MonoBehaviour
    {
        public Transform heldModel;
        public AudioSource audioSource;
        public AudioClip swingClip,hitClip;
        Quaternion restRotation;
        Vector3 restPosition;
        float elapsed=-1;
        bool struck;
        public int Hits{get;private set;}
        public bool Swinging=>elapsed>=0;
        void Awake(){restRotation=heldModel.localRotation;restPosition=heldModel.localPosition;}
        public void ResetCombat(){elapsed=-1;struck=false;Hits=0;if(audioSource)audioSource.Stop();RestorePose();}
        void RestorePose(){if(heldModel){heldModel.localRotation=restRotation;heldModel.localPosition=restPosition;}}
        public bool BeginSwing(GameSession game)
        {
            if(!game.State.Running||!game.Inventory.AxeEquipped||Swinging)return false;
            elapsed=0;struck=false;audioSource.PlayOneShot(swingClip,.55f);return true;
        }
        public void Simulate(float dt,GameSession game)
        {
            heldModel.gameObject.SetActive(game.Inventory.AxeEquipped);
            game.presence?.RefreshEquipment(game.Inventory.AxeEquipped);
            if(dt<=0||!Swinging)return;elapsed+=dt;
            float arc=elapsed<.2f?Mathf.Lerp(0,-48,elapsed/.2f):elapsed<.36f?Mathf.Lerp(-48,85,(elapsed-.2f)/.16f):Mathf.Lerp(85,0,(elapsed-.36f)/.4f);
            heldModel.localRotation=Quaternion.Euler(arc,0,-arc*.12f)*restRotation;
            heldModel.localPosition=restPosition+new Vector3(-.13f,.06f,.12f)*Mathf.Sin(Mathf.Clamp01(elapsed/.76f)*Mathf.PI);
            if(!struck&&elapsed>=.26f){struck=true;Strike(game);}
            if(elapsed>=.76f){elapsed=-1;RestorePose();}
        }
        void Strike(GameSession game)
        {
            ZombieWolf closest=null;float nearest=2.65f;var eyes=game.player.view.transform;
            foreach(var wolf in game.wolves)
            {
                if(!wolf||!wolf.Alive)continue;Vector3 chest=wolf.transform.position+Vector3.up*.65f;
                Vector3 delta=chest-eyes.position;float d=delta.magnitude;
                Vector3 horizontal=Vector3.ProjectOnPlane(delta,Vector3.up),facing=Vector3.ProjectOnPlane(eyes.forward,Vector3.up);
                if(d>nearest||Vector3.Angle(horizontal,facing)>55||!game.Protection.Clear(eyes.position,chest))continue;
                nearest=d;closest=wolf;
            }
            CinderHead head=null;
            if(game.norton)foreach(var enemy in game.norton.enemies)
            {
                if(!enemy||!enemy.Alive||!enemy.gameObject.activeSelf)continue;
                Vector3 delta=enemy.AimPoint-eyes.position;float d=delta.magnitude;
                if(d>nearest||Vector3.Angle(Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.ProjectOnPlane(eyes.forward,Vector3.up))>55||!game.Protection.Clear(eyes.position,enemy.AimPoint))continue;
                nearest=d;head=enemy;closest=null;
            }
            var aquatic=game.researchSpecimen?game.researchSpecimen.monster:null;
            bool aquaticHit=false;
            if(aquatic&&aquatic.CanBeHit)
            {
                Vector3 delta=aquatic.AimPoint-eyes.position;
                aquaticHit=delta.magnitude<=nearest&&Vector3.Angle(Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.ProjectOnPlane(eyes.forward,Vector3.up))<=55&&game.Protection.Clear(eyes.position,aquatic.AimPoint);
            }
            if(aquaticHit||closest||head){if(aquaticHit)aquatic.Hit();else if(head)head.Hit(1);else closest.Hit();Hits++;audioSource.PlayOneShot(hitClip,.85f);}
        }
    }
}
