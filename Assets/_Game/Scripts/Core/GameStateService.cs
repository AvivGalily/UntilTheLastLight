using UnityEngine;
using UnityEngine.InputSystem;
namespace Game
{
    public enum ScreenMode { Title, Playing, Pause, Inventory, Journal, Map, Settings, Dead, Complete, Dying }
    public sealed class GameStateService
    {
        public ScreenMode Mode { get; private set; } = ScreenMode.Title;
        public bool Running => Mode == ScreenMode.Playing;
        readonly InputActionMap gameplay;
        public GameStateService(InputActionAsset actions) { gameplay = actions.FindActionMap("Gameplay", true); }
        public void Set(ScreenMode mode)
        {
            Mode = mode;
            bool animated=Running||mode==ScreenMode.Dying;
            Time.timeScale = animated ? 1 : 0;
            AudioListener.pause = !animated;
            Cursor.lockState = animated ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !animated;
            if (Running) gameplay.Enable(); else gameplay.Disable();
        }
        public void Dispose() { Time.timeScale = 1; AudioListener.pause = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
