using System;
namespace Game
{
    [Serializable] public sealed class SaveData
    {
        public int schemaVersion = 6;
        public string build = "M2-0.14.0", scene = "ForestGame", checkpoint, spawnId = "ranger_cabin";
        public float charge, activeSeconds;
        public bool flashlightOn, fuse, powerOn, wounded, completed;
        public int batteries, seed = 1407;
        public string[] documents;
        public ItemRecord[] pickups;
        public bool tutorialRepelled;
        public bool introCompleted;
        public bool mapAcquired;
        public bool axe;
        public bool forestEncounterSeen,flashlightFaulted;
        public bool shotgun,headUnlocked,headIntroduced;
        public int loadedShells,spareShells,weapon;
        public float fireRecovery;
        public float aquaticRecovery;
        public bool gateKey,gateUnlocked;
        public ResearchSave research=new ResearchSave();
        public string[] deadWolves=new string[0];
    }
    [Serializable] public sealed class ResearchSave
    {
        public int phase,health=4;
        public float elapsed,x,y,z,yaw;
    }
}
