using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Game.Editor
{
    public static partial class ProjectSetup
    {
        static void CabinSign(string key,Transform building,Vector3 position,float width)
        {
            var root=new GameObject("Cabin name sign - "+key).transform;root.SetParent(building,false);root.localPosition=position;
            float scale=width/3;
            ForestArt.Model("cabin_sign_01",Vector3.zero,Quaternion.identity,root,scale);
            var go=new GameObject("Painted yellow name",typeof(TextMeshPro),typeof(LocalizedSign));go.transform.SetParent(root,false);
            go.transform.localPosition=new Vector3(0,0,.054f*scale);go.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=go.GetComponent<TextMeshPro>();text.font=font;text.alignment=TextAlignmentOptions.Center;
            text.fontSize=4;text.enableAutoSizing=true;text.fontSizeMin=1.5f;text.fontSizeMax=4;text.fontStyle=FontStyles.Bold;
            text.rectTransform.sizeDelta=new Vector2(width*.70f,.42f*scale);text.margin=Vector4.zero;
            text.color=new Color(.95f,.72f,.12f);text.textWrappingMode=TextWrappingModes.NoWrap;
            var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            go.layer=LayerMask.NameToLayer("Decor");
            var sign=go.GetComponent<LocalizedSign>();sign.key=key;sign.singleLine=true;
            sign.Refresh(new Localization(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/English.json"),AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/Hebrew.json")));
        }
    }
}
