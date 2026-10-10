using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void VerifyDisplayUI(GameSession game)
        {
            const string key="lastlight.fullscreen";bool existed=PlayerPrefs.HasKey(key);int pref=PlayerPrefs.GetInt(key,1);
            bool original=game.Display.Fullscreen,language=game.Locale.Hebrew;
            var keyboard=Keyboard.current;bool added=keyboard==null;if(added)keyboard=InputSystem.AddDevice<Keyboard>();
            var routing=InputSystem.settings.editorInputBehaviorInPlayMode;var background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var advance=typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null);
            void InputTick()=>advance.Invoke(null,new object[]{InputUpdateType.Dynamic});
            game.OpenSettings();
            try
            {
                InputTick();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F11));InputTick();
                typeof(GameSession).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
                Require(game.Display.Fullscreen!=original&&game.State.Mode==ScreenMode.Settings,"F11 cannot change display while a menu is open");
                Require(PlayerPrefs.GetInt(key)==(game.Display.Fullscreen?1:0),"display preference not saved");
                game.Locale.SetHebrew(true);game.hud.Invalidate();UIShot(game,"31-fullscreen-settings-hebrew");
                Require(game.hud.buttons[7].gameObject.activeSelf&&game.hud.buttons[8].gameObject.activeSelf,"display/back controls missing");
                game.hud.buttons[7].onClick.Invoke();Require(game.Display.Fullscreen==original,"settings display button does not switch mode");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputTick();
                InputSystem.settings.editorInputBehaviorInPlayMode=routing;InputSystem.settings.backgroundBehavior=background;if(added)InputSystem.RemoveDevice(keyboard);
                game.Display.SetFullscreen(original);if(existed)PlayerPrefs.SetInt(key,pref);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                game.Locale.SetHebrew(language);game.LeaveSettings();game.hud.Invalidate();
            }
            Debug.Log("DISPLAY UI SMOKE PASS: actual F11 input in settings, persisted preference, localized visible display/back controls and working display button; original preference restored.");
        }
    }
}
