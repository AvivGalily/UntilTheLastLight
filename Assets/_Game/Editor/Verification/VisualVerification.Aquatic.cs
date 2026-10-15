using UnityEngine;

namespace Game.Editor
{
    public static partial class VisualVerification
    {
        static void AquaticShot(GameSession game,string name,Vector3 position,Vector3 target)
        {
            // Capture several skeleton poses in one editor frame. Recalculate
            // bone matrices for each render instead of reusing the first pose.
            var skins=game.researchSpecimen.monster.GetComponentsInChildren<SkinnedMeshRenderer>();
            var previous=new bool[skins.Length];
            try
            {
                for(int i=0;i<skins.Length;i++)
                {
                    previous[i]=skins[i].forceMatrixRecalculationPerRender;skins[i].forceMatrixRecalculationPerRender=true;
                }
                Shot(game.player.view,name,position,target,true);
            }
            finally
            {
                for(int i=0;i<skins.Length;i++)skins[i].forceMatrixRecalculationPerRender=previous[i];
            }
        }
    }
}
