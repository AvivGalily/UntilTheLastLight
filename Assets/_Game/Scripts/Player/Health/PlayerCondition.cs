using UnityEngine;
namespace Game
{
    public sealed class PlayerCondition
    {
        public bool Wounded => physicalWound || FireRecoveryRemaining > 0 || AquaticRecoveryRemaining>0;
        public float AquaticRecoveryRemaining {get;private set;}
        public bool PhysicalWound => physicalWound;
        public float FireRecoveryRemaining { get; private set; }
        bool physicalWound;
        public bool Dead { get; private set; }
        public float Invulnerability { get; private set; }
        float shelterTime;
        public bool Hit(PlayerTuning tuning,bool lethal=false)
        {
            if (Dead || Invulnerability > 0 && !lethal) return false;
            if (Wounded||lethal) Dead = true;
            else physicalWound = true;
            Invulnerability = tuning.hitInvulnerability; shelterTime = 0; return true;
        }
        public void Tick(float dt, bool safe, PlayerTuning tuning)
        {
            if(dt<=0)return;
            FireRecoveryRemaining=Mathf.Max(0,FireRecoveryRemaining-dt);
            AquaticRecoveryRemaining=Mathf.Max(0,AquaticRecoveryRemaining-dt);
            Invulnerability = Mathf.Max(0, Invulnerability - dt);
            shelterTime = safe ? shelterTime + dt : 0;
            if (shelterTime >= tuning.woundRecovery) physicalWound = false;
        }
        public bool HitFire()
        {
            if(Dead)return false;
            if(FireRecoveryRemaining>0)Dead=true;else FireRecoveryRemaining=10;
            return true;
        }
        public bool HitAquatic()
        {
            if(Dead)return false;
            if(AquaticRecoveryRemaining>0)Dead=true;else AquaticRecoveryRemaining=5;
            return true;
        }
        public void Restore(bool wounded,float fireRemaining=0,float aquaticRemaining=0) { physicalWound = wounded; FireRecoveryRemaining=Mathf.Clamp(fireRemaining,0,10);AquaticRecoveryRemaining=Mathf.Clamp(aquaticRemaining,0,5);Dead = false; Invulnerability = 10; shelterTime = 0; }
    }
}
