using UnityEngine;
namespace Game
{
    public enum HeldWeapon { Axe, Shotgun }
    public sealed class InventoryService
    {
        public int Batteries { get; private set; } = 2;
        public bool HasFuse { get; set; }
        public bool HasAxe {get;set;}
        public bool HasShotgun {get;set;}
        public int LoadedShells {get;private set;}
        public int SpareShells {get;private set;}
        public HeldWeapon Weapon {get;set;}
        public bool AxeEquipped=>HasAxe&&Weapon==HeldWeapon.Axe;
        public bool ShotgunEquipped=>HasShotgun&&Weapon==HeldWeapon.Shotgun;
        public void AcquireShotgun(){if(HasShotgun)return;HasShotgun=true;LoadedShells=2;AddShells(12);Weapon=HeldWeapon.Shotgun;}
        public int AddShells(int amount){int n=Mathf.Clamp(amount,0,30-SpareShells);SpareShells+=n;return n;}
        public bool FireShell(){if(!ShotgunEquipped||LoadedShells==0)return false;LoadedShells--;return true;}
        public int ReloadShotgun(){int n=Mathf.Min(2-LoadedShells,SpareShells);LoadedShells+=n;SpareShells-=n;return n;}
        public void RestoreWeapon(bool shotgun,int loaded,int spare,HeldWeapon selected){HasShotgun=shotgun;LoadedShells=shotgun?Mathf.Clamp(loaded,0,2):0;SpareShells=Mathf.Clamp(spare,0,30);Weapon=shotgun?selected:HeldWeapon.Axe;}
        public int AddBatteries(int offered)
        { int accepted = Mathf.Clamp(offered, 0, 6 - Batteries); Batteries += accepted; return accepted; }
        public bool ConsumeBattery() { if (Batteries <= 0) return false; Batteries--; return true; }
        public void Restore(int batteries, bool fuse,bool axe=false) { Batteries = Mathf.Clamp(batteries, 0, 6); HasFuse = fuse;HasAxe=axe;RestoreWeapon(false,0,0,HeldWeapon.Axe); }
    }
}
