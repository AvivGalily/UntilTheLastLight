using TMPro;
using UnityEngine;
namespace Game
{
    public sealed class LocalizedSign:MonoBehaviour
    {
        public string key;
        public bool singleLine;
        GameSession game;
        int revision=-1;
        void Start(){game=FindFirstObjectByType<GameSession>();Update();}
        void Update(){if(game&&revision!=game.Locale.Revision)Refresh(game.Locale);}
        public void Refresh(Localization locale)
        {
            revision=locale.Revision;var label=GetComponent<TMP_Text>();string logical=locale.Get(key);
            if(!singleLine){locale.Apply(label,logical);return;}
            // World labels use metre-sized rects, rather than the HUD's pixel wrap margin.
            label.textWrappingMode=TextWrappingModes.NoWrap;label.richText=false;label.isRightToLeftText=false;
            label.text=locale.Hebrew?HebrewText.Visual(logical):logical;label.ForceMeshUpdate();
        }
    }
}
