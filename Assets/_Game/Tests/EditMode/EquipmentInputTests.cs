using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tests
{
    public sealed class EquipmentInputTests
    {
        [TestCase("EquipAxe",Key.Digit1)]
        [TestCase("EquipAxe",Key.Numpad1)]
        [TestCase("EquipShotgun",Key.Digit2)]
        [TestCase("EquipShotgun",Key.Numpad2)]
        [TestCase("BatterySwap",Key.R)]
        [TestCase("BatterySwap",Key.B)]
        [TestCase("Reload",Key.T)]
        public void EquipmentBindingResolvesToARealKeyboardControl(string name,Key key)
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var actions=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Game/Generated/Data/Player.inputactions"));
            try
            {
                actions.Enable();
                Assert.That(actions.FindAction("Gameplay/"+name,true).controls,Does.Contain(keyboard[key]),name+" must resolve on a real keyboard");
            }
            finally{actions.Disable();Object.DestroyImmediate(actions);InputSystem.RemoveDevice(keyboard);}
        }
        [Test] public void WheelAndPackResolveOnRealDevicesInTheirProperMaps()
        {
            var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
            var actions=Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Game/Generated/Data/Player.inputactions"));
            try
            {
                var state=new GameStateService(actions);actions.FindActionMap("UI",true).Enable();state.Set(ScreenMode.Playing);
                var wheel=actions.FindAction("Gameplay/CycleWeapon",true);var pack=actions.FindAction("UI/Inventory",true);
                Assert.That(wheel.controls,Does.Contain(mouse.scroll.y));Assert.That(pack.controls,Does.Contain(keyboard.tabKey));
                state.Set(ScreenMode.Inventory);Assert.True(pack.enabled,"TAB must be available to close the pack");Assert.False(wheel.enabled,"scroll must not change weapons behind a menu");state.Dispose();
            }
            finally{actions.Disable();Object.DestroyImmediate(actions);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);}
        }
        [TestCase(false,false,HeldWeapon.Axe,HeldWeapon.Axe)]
        [TestCase(true,false,HeldWeapon.Axe,HeldWeapon.Axe)]
        [TestCase(false,true,HeldWeapon.Shotgun,HeldWeapon.Shotgun)]
        [TestCase(true,true,HeldWeapon.Axe,HeldWeapon.Shotgun)]
        [TestCase(true,true,HeldWeapon.Shotgun,HeldWeapon.Axe)]
        public void CyclingOnlySelectsWeaponsThePlayerOwns(bool axe,bool shotgun,HeldWeapon current,HeldWeapon expected)
        {
            var pack=new InventoryService{HasAxe=axe,HasShotgun=shotgun,Weapon=current};
            Assert.That(pack.NextWeapon(current),Is.EqualTo(expected));
            Assert.False(pack.OwnsWeapon((HeldWeapon)99));
        }
    }
}
