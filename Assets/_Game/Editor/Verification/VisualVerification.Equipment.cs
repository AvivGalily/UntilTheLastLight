using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void VerifyEquipmentInput(GameSession g)
        {
            var keyboard=Keyboard.current;bool added=keyboard==null;if(added)keyboard=InputSystem.AddDevice<Keyboard>();
            var mouse=Mouse.current;bool addedMouse=mouse==null;if(addedMouse)mouse=InputSystem.AddDevice<Mouse>();
            var routing=InputSystem.settings.editorInputBehaviorInPlayMode;var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var advance=typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null);
            var update=typeof(GameSession).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            void Step(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));advance.Invoke(null,new object[]{InputUpdateType.Dynamic});update.Invoke(g,null);}
            void Press(Key key){Step();Step(key);Step();}
            void Scroll(float delta){Step();InputSystem.QueueStateEvent(mouse,new MouseState{scroll=new Vector2(0,delta)});Step();Step();}
            void CheckWeapon(HeldWeapon weapon)
            {
                Require(g.Inventory.Weapon==weapon&&g.axe.heldModel.gameObject.activeSelf==(weapon==HeldWeapon.Axe)&&g.shotgun.heldModel.gameObject.activeSelf==(weapon==HeldWeapon.Shotgun),"keyboard cannot switch weapon/model: "+weapon);
                Require(g.player.handLight.gameObject.activeInHierarchy&&g.presence.flashlightGrip.gameObject.activeInHierarchy,"weapon switching hides the left flashlight");
            }
            var i=g.Inventory;int batteries=i.Batteries,loaded=i.LoadedShells,spare=i.SpareShells;
            bool fuse=i.HasFuse,axe=i.HasAxe,gun=i.HasShotgun,on=g.Flashlight.SwitchedOn,language=g.Locale.Hebrew;
            HeldWeapon selected=i.Weapon;float charge=g.Flashlight.Charge;string message=g.Message;var position=g.player.transform.position;float yaw=g.player.transform.eulerAngles.y;
            try
            {
                g.Resume();i.Restore(6,fuse,true);i.AcquireShotgun();g.axe.ResetCombat();g.shotgun.ResetCombat();PlaceNortonPlayer(g,ForestLayout.Cabin);
                Press(Key.Digit1);CheckWeapon(HeldWeapon.Axe);Press(Key.Digit2);CheckWeapon(HeldWeapon.Shotgun);
                Press(Key.Numpad1);CheckWeapon(HeldWeapon.Axe);Press(Key.Numpad2);CheckWeapon(HeldWeapon.Shotgun);
                Scroll(120);CheckWeapon(HeldWeapon.Axe);Scroll(-120);CheckWeapon(HeldWeapon.Shotgun);Scroll(360);CheckWeapon(HeldWeapon.Axe);
                Press(Key.Tab);Require(g.State.Mode==ScreenMode.Inventory&&AudioListener.pause&&Cursor.visible,"TAB fails to open weapon pack");
                Scroll(120);Require(i.AxeEquipped,"wheel changes weapons behind the pack");
                g.Locale.SetHebrew(true);g.hud.Invalidate();UIShot(g,"72-tab-weapons-hebrew",false);
                g.Locale.SetHebrew(false);g.hud.Invalidate();UIShot(g,"73-tab-weapons-english",false);
                Require(g.hud.buttons[0].interactable&&g.hud.buttons[1].interactable,"owned weapons disabled in pack");
                g.hud.buttons[1].onClick.Invoke();g.hud.Refresh();CheckWeapon(HeldWeapon.Shotgun);
                Require(g.State.Mode==ScreenMode.Inventory,"weapon choice unexpectedly closes pack");
                g.hud.buttons[0].onClick.Invoke();g.hud.Refresh();CheckWeapon(HeldWeapon.Axe);Press(Key.Tab);Require(g.State.Running&&!AudioListener.pause,"TAB fails to close weapon pack");
                NortonArtShot(g,"74-detailed-axe-hands",true);Press(Key.Digit2);NortonArtShot(g,"75-detailed-shotgun-hands",true);
                foreach(var weapon in new[]{HeldWeapon.Axe,HeldWeapon.Shotgun})
                {
                    Press(weapon==HeldWeapon.Axe?Key.Digit1:Key.Digit2);
                    foreach(var key in new[]{Key.R,Key.B})
                    {
                        g.Flashlight.Restore(8,true);int cells=i.Batteries,shells=i.LoadedShells,reserve=i.SpareShells;
                        Press(key);Require(g.Flashlight.SwapRemaining>0&&i.Batteries==cells-1&&i.LoadedShells==shells&&i.SpareShells==reserve&&g.shotgun.ReloadRemaining<=0,"battery key reloads gun or fails with "+weapon+" / "+key);
                        if(weapon==HeldWeapon.Shotgun&&key==Key.R)
                        {
                            g.Locale.SetHebrew(true);g.Notify(g.Text("shotgun_acquired"));g.hud.Invalidate();UIShot(g,"62-shotgun-battery-swap-hebrew",false);
                            g.Locale.SetHebrew(false);g.Notify(g.Text("shotgun_acquired"));g.hud.Invalidate();UIShot(g,"63-shotgun-battery-swap-english",false);
                        }
                        g.Flashlight.Tick(g.Flashlight.SwapRemaining+.01f,false);Require(g.Flashlight.Active&&g.Flashlight.Charge>99,"new cell fails to refill the flashlight");
                    }
                }
                i.FireShell();int total=i.LoadedShells+i.SpareShells,cellsBeforeReload=i.Batteries;
                Press(Key.T);Require(g.shotgun.ReloadRemaining>0&&i.Batteries==cellsBeforeReload&&g.Flashlight.SwapRemaining==0,"T consumes a battery instead of reloading the shotgun");
                g.Flashlight.Restore(8,true);Press(Key.R);Require(g.Flashlight.SwapRemaining>0&&i.Batteries==cellsBeforeReload-1&&g.shotgun.ReloadRemaining>0,"gun reload blocks a flashlight battery swap");
                g.Pause();float remaining=g.shotgun.ReloadRemaining,swapRemaining=g.Flashlight.SwapRemaining;int pausedCells=i.Batteries;
                Press(Key.Digit1);Press(Key.B);Require(i.ShotgunEquipped&&i.Batteries==pausedCells&&g.shotgun.ReloadRemaining==remaining&&g.Flashlight.SwapRemaining==swapRemaining,"equipment input runs while paused");g.Resume();
                Press(Key.Digit1);Require(i.ShotgunEquipped,"weapon switch prematurely interrupts the reload");
                g.shotgun.Simulate(2,g);Step();CheckWeapon(HeldWeapon.Axe);Require(i.LoadedShells==2&&i.LoadedShells+i.SpareShells==total,"queued switch duplicates or loses shotgun ammo");
                Require(g.axe.BeginSwing(g),"cannot test a busy axe");Press(Key.Digit2);Require(i.AxeEquipped,"weapon switch prematurely interrupts an axe swing");
                g.axe.Simulate(.8f,g);Step();CheckWeapon(HeldWeapon.Shotgun);
                Press(Key.Digit1);Require(g.axe.BeginSwing(g),"wheel queue fixture");Scroll(120);Require(i.AxeEquipped&&g.RequestedWeapon==HeldWeapon.Shotgun,"wheel interrupts busy axe or forgets request");
                Scroll(-120);Require(g.RequestedWeapon==HeldWeapon.Axe,"rapid wheel input cannot return to original requested weapon");g.axe.Simulate(.8f,g);Step();CheckWeapon(HeldWeapon.Axe);
                Press(Key.Digit2);i.FireShell();Press(Key.T);Press(Key.Tab);g.hud.buttons[0].onClick.Invoke();g.hud.Refresh();
                Require(i.ShotgunEquipped&&g.RequestedWeapon==HeldWeapon.Axe&&g.State.Mode==ScreenMode.Inventory,"pack selection interrupts reload or loses queued choice");
                float frozen=g.shotgun.ReloadRemaining;Step();Require(g.shotgun.ReloadRemaining==frozen,"pack advances weapon reload");Press(Key.Tab);g.shotgun.Simulate(2,g);Step();CheckWeapon(HeldWeapon.Axe);
                g.Flashlight.FailCircuit();int cellsBeforeFault=i.Batteries;Press(Key.R);Require(g.Flashlight.Faulted&&i.Batteries==cellsBeforeFault,"battery key bypasses the ambush's failed circuit");
                while(i.ConsumeBattery()){}g.Flashlight.Restore(8,true);Press(Key.B);Require(i.Batteries==0&&g.Flashlight.SwapRemaining==0,"battery key invents a spare cell");
                Press(Key.Digit2);i.HasAxe=false;Press(Key.Digit1);CheckWeapon(HeldWeapon.Shotgun);i.HasAxe=true;Press(Key.Digit1);i.HasShotgun=false;Press(Key.Digit2);CheckWeapon(HeldWeapon.Axe);
                Scroll(120);CheckWeapon(HeldWeapon.Axe);Press(Key.Tab);g.hud.Refresh();Require(!g.hud.buttons[1].interactable,"uncollected shotgun selectable in TAB");
                Require(!g.SelectWeapon(HeldWeapon.Shotgun)&&i.AxeEquipped,"pack invents uncollected weapon");Press(Key.Tab);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());advance.Invoke(null,new object[]{InputUpdateType.Dynamic});
                InputSystem.QueueStateEvent(mouse,new MouseState());advance.Invoke(null,new object[]{InputUpdateType.Dynamic});
                InputSystem.settings.editorInputBehaviorInPlayMode=routing;InputSystem.settings.backgroundBehavior=background;if(added)InputSystem.RemoveDevice(keyboard);
                if(addedMouse)InputSystem.RemoveDevice(mouse);
                i.Restore(batteries,fuse,axe);i.RestoreWeapon(gun,loaded,spare,selected);g.Flashlight.Restore(charge,on);g.axe.ResetCombat();g.shotgun.ResetCombat();
                g.Resume();g.player.Teleport(position,yaw);g.player.Simulate(g.tuning.player,Vector2.zero,Vector2.zero,false,false,.01f);g.axe.Simulate(0,g);g.shotgun.Simulate(0,g);
                g.Locale.SetHebrew(language);g.Notify(message);g.hud.Invalidate();Physics.SyncTransforms();
            }
            Debug.Log("EQUIPMENT INPUT SMOKE PASS: real keyboard and both wheel directions; TAB open/close, bilingual owned-weapon buttons and queued menu/wheel selection through reload/swing; R/B battery with both weapons; T ammo-only reload; pause/missing-item guards and independent left flashlight.");
        }
    }
}
