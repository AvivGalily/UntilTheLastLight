using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Read-only inventory. Reachability is evidence, never an automatic delete list:
    // authoring scripts and string-based loads must be checked separately.
    public static class ProjectMaintenance
    {
        [Serializable] public sealed class Entry { public string path,guid;public long bytes;public bool reachable; }
        [Serializable] public sealed class Report { public string[] roots;public Entry[] assets; }
        [MenuItem("Last Light/Maintenance/Audit asset usage")]
        public static void AuditAssets()
        {
            var assets=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(p)).ToArray();
            var rootExtensions=new HashSet<string>{".unity",".prefab",".asset",".mat",".shader",".cs",".asmdef",".inputactions"};
            var roots=assets.Where(p=>rootExtensions.Contains(Path.GetExtension(p))||p.Contains("/Resources/")||p.Contains("/StreamingAssets/")).ToArray();
            var reachable=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
            var report=new Report{roots=roots,assets=assets.Select(p=>new Entry{path=p,guid=AssetDatabase.AssetPathToGUID(p),bytes=new FileInfo(p).Length,reachable=reachable.Contains(p)}).ToArray()};
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/AssetUsage.json",JsonUtility.ToJson(report,true));
            Debug.Log($"ASSET USAGE AUDIT: {assets.Length} assets, {roots.Length} conservative roots; {report.assets.Count(a=>!a.reachable)} require authoring/dynamic-load review. No deletion performed.");
        }
    }
}
