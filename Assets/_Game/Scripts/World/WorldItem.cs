using UnityEngine;
namespace Game
{
    public enum ItemKind { Note, Batteries, Fuse, PowerPanel, EmergencyCharger, ExitGate, Corpse, MapCorpse, Axe, Shotgun, Shells }
    public sealed class WorldItem : MonoBehaviour
    {
        public string persistentId;
        public ItemKind kind;
        public int remaining = 1;
        public string label;
        public string noteId;
        public bool houseCharger;
        public string Prompt(GameSession game)
        {
            switch (kind)
            {
                case ItemKind.Batteries: return game.Text("take_batteries",remaining);
                case ItemKind.Fuse: return game.Text("take_fuse");
                case ItemKind.Axe:return game.Text("take_axe");
                case ItemKind.Shotgun:return game.Text("take_shotgun");
                case ItemKind.Shells:return game.Text("take_shells",remaining*4);
                case ItemKind.PowerPanel: return game.Text(game.PowerOn?"power_restored":game.Inventory.HasFuse?"install_fuse":"missing_fuse");
                case ItemKind.EmergencyCharger: return game.Text(game.Flashlight.Faulted?(houseCharger?"repair_hold":"repair_house_only"):game.Flashlight.Charge<game.tuning.flashlight.emergencyCharge?"charger_hold":"charger_full");
                case ItemKind.ExitGate: return game.Text(game.PowerOn?"leave":"gate_no_power");
                case ItemKind.MapCorpse: return game.Text(game.HasMap?"reread_body":"search_map");
                case ItemKind.Corpse: return game.Text(game.Documents.Contains(noteId)?"reread_body":"search_body");
                default: return game.Text("read");
            }
        }
        public void Use(GameSession game)
        {
            switch (kind)
            {
                case ItemKind.Note: game.ReadNote(noteId); break;
                case ItemKind.Shotgun:
                    game.Inventory.AcquireShotgun();remaining=0;gameObject.SetActive(false);game.Notify(game.Text("shotgun_acquired"),9);game.SaveCheckpoint("checkpoint_wreck");break;
                case ItemKind.Shells:
                    int shells=game.Inventory.AddShells(4*Mathf.Min(remaining,(30-game.Inventory.SpareShells)/4));remaining-=shells/4;
                    game.Notify(game.Text(shells>=4?"shells_acquired":"shells_full"));if(remaining<=0)gameObject.SetActive(false);break;
                case ItemKind.Axe:
                    game.Inventory.HasAxe=true;remaining=0;gameObject.SetActive(false);game.Notify(game.Text("axe_acquired"),8);break;
                case ItemKind.MapCorpse: game.AcquireMap();game.ReadNote(noteId);break;
                case ItemKind.Corpse: game.ReadNote(noteId);break;
                case ItemKind.Batteries:
                    int n = game.Inventory.AddBatteries(remaining); remaining -= n;
                    game.Notify(n>0?game.Text("added_batteries",n):game.Text("stack_full"));
                    if (remaining == 0) gameObject.SetActive(false); break;
                case ItemKind.Fuse:
                    game.Inventory.HasFuse = true; remaining = 0; gameObject.SetActive(false);
                    game.Notify(game.Text("fuse_acquired"),6);break;
                case ItemKind.PowerPanel:
                    if (!game.PowerOn && game.Inventory.HasFuse)
                    {
                        game.Inventory.HasFuse = false; game.SetPower(true); game.SaveCheckpoint("Cabin power restored");
                        game.Notify(game.Text("circuit_alive"),7);
                    }
                    else game.Notify(Prompt(game)); break;
                case ItemKind.ExitGate:
                    if (game.PowerOn) { game.Completed = true; game.SaveCheckpoint("Prototype completed"); game.State.Set(ScreenMode.Complete); }
                    else game.Notify(game.Text("restore_first"));break;
            }
        }
    }
}
