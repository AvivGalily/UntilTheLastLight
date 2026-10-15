using UnityEngine;
namespace Game
{
    // The paper sketch and the generated level share these landmarks. No player GPS.
    public static class ForestLayout
    {
        public const float Size = 280;
        public static bool InBuilding(Vector3 p,float padding=0)
        {
            return (Mathf.Abs(p.x-Cabin.x)<5+padding&&Mathf.Abs(p.z-Cabin.z)<5+padding)
                ||(Mathf.Abs(p.x-Store.x)<5+padding&&Mathf.Abs(p.z-Store.z)<5+padding)
                ||(Mathf.Abs(p.x-Research.x)<9+padding&&Mathf.Abs(p.z-Research.z)<7+padding);
        }
        public static readonly Vector3 Cabin = new Vector3(-75,0,-100);
        public static readonly Vector3 Store = new Vector3(70,0,-10);
        public static readonly Vector3 Research = new Vector3(-35,0,35);
        public static readonly Vector3 Lantern = new Vector3(45,0,65);
        public static readonly Vector3 Gate = new Vector3(-5,0,120);
        public static readonly Vector3 EncounterTree = new Vector3(-56,0,-70);
        public static readonly Vector3 Wreck=Research+new Vector3(13,0,7);
        public static readonly Vector3 NortonBody=Research+new Vector3(11.7f,0,12.8f);
        public static bool InRefugeHouse(Vector3 p)
        {
            return (Mathf.Abs(p.x-Cabin.x)<4.1f&&Mathf.Abs(p.z-Cabin.z)<3.7f)
                ||(Mathf.Abs(p.x-Store.x)<4.1f&&Mathf.Abs(p.z-Store.z)<3.7f);
        }
        public static readonly Vector3[] Bodies = {new Vector3(-68,0,-78),new Vector3(52,0,-21),new Vector3(-41,0,44),new Vector3(5,0,92)};
        public static readonly string[] BodyNotes = {"note_cartographer","note_clicks","note_research","note_last_watch"};
        public static readonly string[] Documents = {"note_guard_rules","note_route","note_storehouse","note_cartographer","note_clicks","note_research","note_last_watch","note_norton"};
        public static readonly Vector3[] Patrol = {new Vector3(-75,0,-78),new Vector3(-38,0,-55),new Vector3(15,0,-38),new Vector3(55,0,-26),Store+Vector3.forward*8,new Vector3(83,0,25),Lantern+Vector3.forward*9,new Vector3(0,0,75),Research+Vector3.forward*10,new Vector3(-58,0,9)};
        public static readonly Vector3[][] Paths = {
            new[]{Cabin+new Vector3(0,0,6),new Vector3(-75,0,-78),new Vector3(-38,0,-55),new Vector3(15,0,-38),new Vector3(55,0,-26),Store+new Vector3(0,0,6)},
            new[]{Store+new Vector3(0,0,6),new Vector3(83,0,25),Lantern,new Vector3(0,0,75),Research+new Vector3(0,0,9),new Vector3(-58,0,9),new Vector3(-75,0,-78)},
            new[]{new Vector3(0,0,75),new Vector3(-5,0,96),Gate-Vector3.forward},
            new[]{new Vector3(15,0,-38),new Vector3(-5,0,-5),Research+new Vector3(0,0,9)}};
        public static bool Clearing(Vector3 p)
        {
            if(Vector3.Distance(p,Wreck)<7)return true;
            if(Vector3.Distance(p,EncounterTree)<9)return true;
            if(Vector3.Distance(p,Cabin)<11||Vector3.Distance(p,Store)<11||Vector3.Distance(p,Lantern)<7||Vector3.Distance(p,Gate)<12)return true;
            if(Mathf.Abs(p.x-Research.x)<12&&Mathf.Abs(p.z-Research.z)<12)return true;
            foreach(var b in Bodies)if(Vector3.Distance(p,b)<4)return true;
            return false;
        }
    }
}
