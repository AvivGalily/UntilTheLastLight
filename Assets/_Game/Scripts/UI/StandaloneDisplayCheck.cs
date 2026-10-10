using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Game
{
    // Opt-in player verification. Normal launches do not create this component.
    public sealed class StandaloneDisplayCheck:MonoBehaviour
    {
        [Serializable] public sealed class Measurement {public string mode;public int width,height;public bool passed;}
        [Serializable] public sealed class Report {public string version;public bool passed;public Measurement[] measurements;}
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-verify-display");
            if(Application.isEditor||at<0||at+1>=args.Length)return;
            Application.runInBackground=true;
            new GameObject("Opt-in display check").AddComponent<StandaloneDisplayCheck>().output=args[at+1];
        }
        IEnumerator Start()
        {
            var game=FindFirstObjectByType<GameSession>();bool original=game.Display.Fullscreen;
            var records=new List<Measurement>();var desktop=Screen.currentResolution;
            foreach(bool mode in new[]{true,false,true})
            {
                game.Display.SetFullscreen(mode);game.hud.Invalidate();yield return new WaitForSecondsRealtime(1.5f);
                bool passed=Screen.fullScreenMode==(mode?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed)
                    &&(mode?Screen.width==desktop.width&&Screen.height==desktop.height:Screen.width<desktop.width&&Screen.height<desktop.height);
                records.Add(new Measurement{mode=Screen.fullScreenMode.ToString(),width=Screen.width,height=Screen.height,passed=passed});
                Debug.Log($"DISPLAY CHECK: {records.Count} {Screen.fullScreenMode}, {Screen.width}x{Screen.height}, pass={passed}");
            }
            game.Display.SetFullscreen(original);bool success=records.TrueForAll(m=>m.passed);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output,JsonUtility.ToJson(new Report{version=Application.version,passed=success,measurements=records.ToArray()},true));
            Debug.Log(success?"DISPLAY CHECK PASS":"DISPLAY CHECK FAIL");Application.Quit(success?0:1);
        }
    }
}
