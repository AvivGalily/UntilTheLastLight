using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        [MenuItem("Last Light/Player/Install equipment grips only")]
        public static void InstallEquipmentGrips()
        {
            ForestArt.Prepare();
            foreach(string scene in new[]{"MechanicsTest","ForestGame"})
            {
                EditorSceneManager.OpenScene(Root+"/Scenes/"+scene+".unity");
                var g=UnityEngine.Object.FindFirstObjectByType<GameSession>();
                UnityEngine.Object.DestroyImmediate(g.presence.axeGrip.gameObject);
                UnityEngine.Object.DestroyImmediate(g.presence.flashlightGrip.gameObject);
                g.presence.axeGrip=ForestArt.Model("player_axe_hand_01",new Vector3(-.014f,.22f,0),Quaternion.Euler(0,-90,0),g.axe.heldModel,1/.62f).transform;
                AttachBodyArm(g.presence.axeGrip,g.player.view,false);
                g.presence.flashlightGrip=ForestArt.Model("player_flashlight_hand_01",new Vector3(-.22f,-.18f,.395f),Quaternion.identity,g.player.hand).transform;
                AttachBodyArm(g.presence.flashlightGrip,g.player.view,true);
                g.presence.flashlightGrip.GetComponent<FirstPersonArm>().forearmAxis=Vector3.left;
                if(g.shotgun)
                {
                    foreach(var arm in g.shotgun.heldModel.GetComponentsInChildren<FirstPersonArm>(true))UnityEngine.Object.DestroyImmediate(arm.gameObject);
                    var grip=ForestArt.Model("player_shotgun_hand_01",new Vector3(0,-.014f,-.09f),Quaternion.identity,g.shotgun.heldModel,1/.68f);
                    AttachBodyArm(grip.transform,g.player.view,false);
                    grip.GetComponent<FirstPersonArm>().forearmAxis=Vector3.right;
                }
                for(int i=0;i<g.deathSequence.defensiveHands.Length;i++)
                {
                    UnityEngine.Object.DestroyImmediate(g.deathSequence.defensiveHands[i].gameObject);
                    var hand=ForestArt.Model(i==0?"player_flashlight_hand_01":"player_axe_hand_01",new Vector3(i==0?-.3f:.3f,-.25f,.48f),Quaternion.Euler(35,i==0?-15:15,i==0?-22:22),g.player.view.transform);
                    g.deathSequence.defensiveHands[i]=hand.transform;hand.SetActive(false);AttachBodyArm(hand.transform,g.player.view,i==0);
                }
                ConfigurePlayerShadowLights(g);
                foreach(var arm in g.player.hand.GetComponentsInChildren<FirstPersonArm>(true))
                {
                    var bones=arm.GetComponentsInChildren<Transform>();string side=arm.bodyAnchor.x<0?"L":"R";
                    var roots=Enumerable.Range(2,4).Select(i=>bones.First(b=>b.name=="finger"+i+"-1."+side).position).ToArray();
                    Vector3 knuckles=roots.Aggregate(Vector3.zero,(a,b)=>a+b)/4;
                    float bend=Vector3.Angle(arm.wrist.position-arm.lower.position,arm.transform.TransformDirection(arm.handForward));
                    Debug.Log("EQUIPMENT GRIP "+arm.name+" wrist "+arm.transform.InverseTransformPoint(arm.wrist.position).ToString("F4")+" knuckles "+arm.transform.InverseTransformPoint(knuckles).ToString("F4")+" wrist bend "+bend.ToString("F1")+" lengths "+arm.upperLength+" / "+arm.lowerLength+" elbow "+g.player.view.transform.InverseTransformPoint(arm.lower.position).ToString("F3"));
                }
                PrefabUtility.SaveAsPrefabAsset(g.player.gameObject,Root+"/Prefabs/Player.prefab");
                EditorSceneManager.SaveScene(g.gameObject.scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("EQUIPMENT GRIPS INSTALLED: anatomical overhand assets and rebound shoulder/elbow/wrist targets.");
        }
    }
}
