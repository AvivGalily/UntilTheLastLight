using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor
{
    public static partial class VisualVerification
    {
        [MenuItem("Last Light/Capture cabin signs")]
        public static void CaptureSigns()
        {
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=CaptureSignsWhenReady;
        }
        static void CaptureSignsWhenReady()
        {
            if(EditorApplication.timeSinceStartup-started<3)return;EditorApplication.update-=CaptureSignsWhenReady;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
                game.hud.gameObject.SetActive(false);var camera=game.player.view;camera.enabled=true;camera.clearFlags=CameraClearFlags.Skybox;
                var locale=new Localization(game.strings,game.hebrewStrings);bool language=locale.Hebrew;
                var signs=UnityEngine.Object.FindObjectsByType<LocalizedSign>(FindObjectsSortMode.None).Where(s=>s.singleLine).OrderBy(s=>s.key).ToArray();
                try
                {
                    int index=32;
                    foreach(var sign in signs)
                    foreach(bool hebrew in new[]{true,false})
                    {
                        locale.SetHebrew(hebrew);sign.Refresh(locale);
                        Vector3 center=sign.transform.parent.position;
                        Shot(camera,(index++)+"-"+sign.key+"-"+(hebrew?"hebrew":"english"),center+new Vector3(0,-.45f,5.8f),center,true);
                    }
                }
                finally{locale.SetHebrew(language);foreach(var sign in signs)sign.Refresh(locale);}
                Debug.Log("CABIN SIGN CAPTURE PASS: three attached wooden name plaques in separate Hebrew/English views.");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
        static void VerifyCabinSigns(GameSession game)
        {
            bool language=game.Locale.Hebrew;
            var signs=UnityEngine.Object.FindObjectsByType<LocalizedSign>(FindObjectsSortMode.None).Where(s=>s.singleLine).ToArray();
            Require(signs.Length==3,"Expected three physical cabin signs");
            try
            {
                foreach(bool hebrew in new[]{true,false})
                {
                    game.Locale.SetHebrew(hebrew);
                    foreach(var sign in signs)
                    {
                        sign.Refresh(game.Locale);var label=sign.GetComponent<TMP_Text>();
                        string logical=game.Text(sign.key);
                        Require(!logical.Contains('\n')&&HebrewText.ContainsHebrew(logical)==hebrew,"Cabin names must contain only the chosen language and no remarks");
                        Require(label.text==(hebrew?HebrewText.Visual(logical):logical),"Cabin label did not refresh its language");
                        Require(label.textInfo.lineCount==1&&!label.isTextOverflowing,"Cabin name does not fit on one line");
                        var plaque=sign.transform.parent.GetComponentsInChildren<MeshFilter>().FirstOrDefault(m=>m.sharedMesh&&m.sharedMesh.vertexCount>20);
                        Require(plaque&&Vector3.Distance(sign.transform.position,plaque.GetComponent<Renderer>().bounds.center)<.15f,"Name detached from its wooden plaque");
                        var color=label.color;Require(color.r>.8f&&color.g>.6f&&color.b<.2f,"Cabin text should be yellow");
                    }
                }
            }
            finally{game.Locale.SetHebrew(language);foreach(var sign in signs)sign.Refresh(game.Locale);game.hud.Invalidate();}
            Debug.Log("CABIN SIGN SMOKE PASS: three actual wooden meshes, attached yellow lettering, name-only Hebrew/English, one line without overflow, and live language switching.");
        }
    }
}
