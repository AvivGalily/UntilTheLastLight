using UnityEngine;
using UnityEngine.InputSystem;
namespace Game
{
    public enum ScreenMode { Title, Playing, Pause, Inventory, Journal, Map, Settings, Dead, Complete, Dying, Intro }
    public sealed class GameStateService
    {
        public ScreenMode Mode { get; private set; } = ScreenMode.Title;
        public bool Running => Mode == ScreenMode.Playing;
        readonly InputActionMap gameplay;
        readonly InputActionMap intro;
        public GameStateService(InputActionAsset actions) { gameplay = actions.FindActionMap("Gameplay", true);intro=actions.FindActionMap("Intro"); }
        public void Set(ScreenMode mode)
        {
            Mode = mode;
            bool animated=Running||mode==ScreenMode.Dying||mode==ScreenMode.Intro;
            Time.timeScale = animated ? 1 : 0;
            AudioListener.pause = !animated;
            Cursor.lockState = animated ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !animated;
            if (Running) gameplay.Enable(); else gameplay.Disable();
            if(mode==ScreenMode.Intro)intro?.Enable();else intro?.Disable();
        }
        public void Dispose() { Time.timeScale = 1; AudioListener.pause = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
