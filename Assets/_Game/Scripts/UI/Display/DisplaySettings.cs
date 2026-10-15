using UnityEngine;
namespace Game
{
    // Display changes use borderless desktop resolution; no monitor mode switch.
    public sealed class DisplaySettings
    {
        const string Preference="lastlight.fullscreen";
        int windowWidth,windowHeight,resizeFrame=-1;
        public bool Fullscreen {get;private set;}
        public DisplaySettings()
        {
            Fullscreen=PlayerPrefs.GetInt(Preference,1)!=0;
            Apply();
        }
        public void Toggle()
        {
            SetFullscreen(!Fullscreen,true);
        }
        public void SetFullscreen(bool value,bool persist=false)
        {
            Fullscreen=value;if(persist){PlayerPrefs.SetInt(Preference,Fullscreen?1:0);PlayerPrefs.Save();}Apply();
        }
        void Apply()
        {
            // The editor Game view has its own maximize control.
            if(Application.isEditor)return;
            var desktop=Screen.currentResolution;
            int width=Fullscreen?desktop.width:Mathf.Min(1600,Mathf.RoundToInt(desktop.width*.88f));
            int height=Fullscreen?desktop.height:Mathf.Min(900,Mathf.RoundToInt(desktop.height*.88f));
            Screen.SetResolution(width,height,Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
            // Windows may restore its previous window size during the mode change.
            // Confirm dimensions after that transition rather than in the same frame.
            windowWidth=width;windowHeight=height;resizeFrame=Fullscreen?-1:Time.frameCount+2;
            Debug.Log($"DISPLAY REQUEST: {width}x{height}, {(Fullscreen?"FullScreenWindow":"Windowed")}");
        }
        public void Tick()
        {
            if(resizeFrame<0||Time.frameCount<resizeFrame)return;
            if(Screen.fullScreenMode!=FullScreenMode.Windowed)return;
            resizeFrame=-1;
            if(Screen.width!=windowWidth||Screen.height!=windowHeight)
                Screen.SetResolution(windowWidth,windowHeight,FullScreenMode.Windowed);
        }
    }
}
