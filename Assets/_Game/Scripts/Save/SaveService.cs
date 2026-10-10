using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace Game
{
    [Serializable] public sealed class ItemRecord { public string id; public int remaining; }
    [Serializable] public sealed class SaveData
    {
        public int schemaVersion = 5;
        public string build = "M2-0.11.0", scene = "ForestGame", checkpoint, spawnId = "ranger_cabin";
        public float charge, activeSeconds;
        public bool flashlightOn, fuse, powerOn, wounded, completed;
        public int batteries, seed = 1407;
        public string[] documents;
        public ItemRecord[] pickups;
        public bool tutorialRepelled;
        public bool mapAcquired;
        public bool axe;
        public bool forestEncounterSeen,flashlightFaulted;
        public bool shotgun,headUnlocked,headIntroduced;
        public int loadedShells,spareShells,weapon;
        public float fireRecovery;
        public string[] deadWolves=new string[0];
    }
    public sealed class SaveService
    {
        public readonly string Path;
        public bool Exists => File.Exists(Path) || File.Exists(Path + ".bak");
        public SaveService(string path) { Path = path; }
        public static void Validate(SaveData s)
        {
            if (s == null || s.schemaVersion<1||s.schemaVersion>5) throw new InvalidDataException("Unsupported or missing save version.");
            if(s.schemaVersion>=5&&(s.loadedShells<0||s.loadedShells>2||s.spareShells<0||s.spareShells>30||s.weapon<0||s.weapon>1||!float.IsFinite(s.fireRecovery)||s.fireRecovery<0||s.fireRecovery>10||(!s.shotgun&&(s.loadedShells!=0||s.weapon==1))||(s.headIntroduced&&!s.headUnlocked)))throw new InvalidDataException("Invalid Norton encounter state.");
            if(s.schemaVersion>=4&&s.flashlightFaulted&&(!s.forestEncounterSeen||s.flashlightOn||s.charge!=0))throw new InvalidDataException("Invalid flashlight circuit state.");
            if (s.scene != "ForestGame" || s.spawnId != "ranger_cabin" || string.IsNullOrWhiteSpace(s.checkpoint)) throw new InvalidDataException("Unknown checkpoint.");
            if (!float.IsFinite(s.charge) || s.charge < 0 || s.charge > 100 || !float.IsFinite(s.activeSeconds) || s.activeSeconds < 0 || s.batteries < 0 || s.batteries > 6) throw new InvalidDataException("Invalid resources.");
            if (s.documents == null || s.pickups == null || (s.completed && !s.powerOn) || (s.powerOn && s.fuse)) throw new InvalidDataException("Invalid quest state.");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var item in s.pickups)
                if (item == null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id) || item.remaining < 0 || item.remaining > 6) throw new InvalidDataException("Invalid pickup state.");
            if(s.schemaVersion>=3)
            {
                if(s.deadWolves==null||s.deadWolves.Length>12)throw new InvalidDataException("Invalid wolf state.");
                ids.Clear();foreach(var id in s.deadWolves)if(string.IsNullOrEmpty(id)||!ids.Add(id))throw new InvalidDataException("Invalid wolf identity.");
            }
        }
        SaveData Read(string path)
        {
            string json = File.ReadAllText(path);
            if (!json.Contains("\"schemaVersion\"") || !json.Contains("\"charge\"") || !json.Contains("\"pickups\"")) throw new InvalidDataException("Save is missing required fields.");
            var s = JsonUtility.FromJson<SaveData>(json); Validate(s);
            // Version 1 did not have a discoverable map; keep existing progress and add discovery.
            if(s.schemaVersion==1){s.mapAcquired=false;s.schemaVersion=2;}
            if(s.schemaVersion==2){s.axe=false;s.deadWolves=new string[0];s.schemaVersion=3;}
            if(s.schemaVersion==3){s.forestEncounterSeen=s.flashlightFaulted=false;s.schemaVersion=4;}
            if(s.schemaVersion==4){s.shotgun=s.headUnlocked=s.headIntroduced=false;s.loadedShells=s.spareShells=s.weapon=0;s.fireRecovery=0;s.schemaVersion=5;}
            return s;
        }
        public SaveData Load(out string message)
        {
            try { var s = Read(Path); message = "Checkpoint loaded."; return s; }
            catch (Exception primary) when (primary is IOException || primary is ArgumentException || primary is InvalidDataException)
            {
                try { var s = Read(Path + ".bak"); message = "Primary save was unreadable. Recovered the last valid backup."; return s; }
                catch (Exception backup) when (backup is IOException || backup is ArgumentException || backup is InvalidDataException)
                { throw new InvalidDataException("No valid checkpoint could be loaded. Your files were kept intact.", primary); }
            }
        }
        public void Write(SaveData data)
        {
            Validate(data);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
            string temp = Path + ".tmp";
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(Path))
            {
                bool valid = false;
                try { Read(Path); valid = true; } catch (Exception e) when (e is IOException || e is ArgumentException || e is InvalidDataException) { }
                // Never replace a healthy backup with a corrupt primary.
                File.Replace(temp, Path, valid ? Path + ".bak" : null);
            }
            else File.Move(temp, Path);
        }
    }
}
