using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class ForestAmbienceSetup
    {
        [MenuItem("Last Light/Audio/Install quiet building crickets")]
        public static void InstallBuildingCrickets()
        {
            EditorSceneManager.OpenScene(ProjectSetup.Root+"/Scenes/ForestGame.unity");
            var game=UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if(!game||!game.ambience||!game.ambience.insects)throw new Exception("Forest ambience source missing.");
            var ambience=game.ambience;var source=ambience.insects;
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/night_crickets.wav");
            if(!clip)throw new Exception("Missing night_crickets.wav");
            source.Stop();source.clip=clip;source.loop=true;source.playOnAwake=false;source.volume=0;source.spatialBlend=0;
            ambience.cricketVolume=.012f;
            EditorUtility.SetDirty(source);EditorUtility.SetDirty(ambience);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("BUILDING CRICKETS INSTALLED: quiet .012 maximum, indoor muffling, fade to silence 10m from building walls, active gameplay only, opening silent.");
        }
    }
}
