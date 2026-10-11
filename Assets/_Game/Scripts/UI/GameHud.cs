using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game
{
    public sealed class GameHud:MonoBehaviour
    {
        public TMP_FontAsset font;
        public GameObject overlay,hudRoot,mapRoot;
        public TMP_Text heading,body,eyebrow,objective,resources,prompt,message,warning,footer;
        public Button[] buttons;
        public TMP_Text[] buttonLabels,mapLabels;
        public string[] mapKeys;
        public Image vignette;
        public Image blood;
        GameSession game;
        ScreenMode previous=(ScreenMode)(-1);
        int revision=-1,journalPage;
        string document;
        bool hint;
        readonly Color cream=new Color(.89f,.87f,.79f),gold=new Color(.76f,.59f,.32f);
        public void Bind(GameSession value){game=value;Invalidate();}
        public void Invalidate(){previous=(ScreenMode)(-1);}
        public void Construct()
        {
            GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            hudRoot=Node("HUD",transform);Stretch(hudRoot);
            objective=Text("Objective",hudRoot.transform,new Vector2(66,-62),new Vector2(650,100),22,cream);
            resources=Text("Resources",hudRoot.transform,new Vector2(66,-835),new Vector2(630,215),22,cream);
            prompt=Text("Interaction",hudRoot.transform,new Vector2(510,-850),new Vector2(900,80),24,cream);prompt.alignment=TextAlignmentOptions.Center;
            message=Text("Message",hudRoot.transform,new Vector2(480,-165),new Vector2(960,130),23,cream);message.alignment=TextAlignmentOptions.Center;
            warning=Text("Warning",hudRoot.transform,new Vector2(480,-760),new Vector2(960,70),22,new Color(.94f,.66f,.4f));warning.alignment=TextAlignmentOptions.Center;
            Text("Reticle",hudRoot.transform,new Vector2(945,-517),new Vector2(30,30),21,cream).text="·";
            var shade=Node("Wound border",hudRoot.transform);Stretch(shade);vignette=shade.AddComponent<Image>();vignette.color=Color.clear;vignette.raycastTarget=false;
            overlay=Node("Menu",transform);Stretch(overlay);overlay.AddComponent<Image>().color=new Color(.018f,.03f,.034f,.95f);
            eyebrow=Text("Eyebrow",overlay.transform,new Vector2(140,-82),new Vector2(1600,60),19,gold);
            heading=Text("Heading",overlay.transform,new Vector2(140,-152),new Vector2(1550,180),64,cream);
            body=Text("Body",overlay.transform,new Vector2(145,-352),new Vector2(1000,590),25,cream);
            footer=Text("Footer",overlay.transform,new Vector2(145,-997),new Vector2(1620,65),18,new Color(.54f,.59f,.57f));
            buttons=new Button[11];buttonLabels=new TMP_Text[11];
            for(int i=0;i<buttons.Length;i++)
            {
                var item=Node("Choice "+i,overlay.transform);Place(item,new Vector2(1230,-365-i*56),new Vector2(530,52));
                item.AddComponent<Image>().color=new Color(.08f,.12f,.13f,.95f);buttons[i]=item.AddComponent<Button>();
                var colors=buttons[i].colors;colors.highlightedColor=colors.selectedColor=new Color(.8f,.7f,.43f);buttons[i].colors=colors;
                buttonLabels[i]=Text("Label",item.transform,new Vector2(20,0),new Vector2(490,52),23,cream);buttonLabels[i].alignment=TextAlignmentOptions.MidlineLeft;
            }
            mapRoot=Node("Paper map",overlay.transform);Place(mapRoot,new Vector2(145,-345),new Vector2(1010,610));
            mapRoot.AddComponent<Image>().color=new Color(.16f,.19f,.16f);
            foreach(var path in ForestLayout.Paths)for(int i=1;i<path.Length;i++)MapLine(MapPoint(path[i-1]),MapPoint(path[i]));
            var labels=new List<TMP_Text>();var keys=new List<string>();
            Mark(ForestLayout.Cabin,"map_cabin",new Vector2(-80,0),gold,labels,keys);
            Mark(ForestLayout.Store,"map_store",new Vector2(-25,12),cream,labels,keys);
            Mark(ForestLayout.Research,"map_research",new Vector2(-210,8),new Color(.85f,.47f,.34f),labels,keys);
            Mark(ForestLayout.Lantern,"map_lantern",new Vector2(10,-5),gold,labels,keys);
            Mark(ForestLayout.Gate,"map_gate",new Vector2(15,-12),cream,labels,keys);
            Vector2[] offsets={new Vector2(15,-3),new Vector2(-205,15),new Vector2(-210,-56),new Vector2(25,-10)};
            for(int i=0;i<4;i++)Mark(ForestLayout.Bodies[i],"map_body"+(i+1),offsets[i],new Color(.70f,.69f,.60f),labels,keys);
            labels.Add(Text("Map legend",mapRoot.transform,new Vector2(28,-549),new Vector2(955,52),18,gold));keys.Add("map_legend");
            mapLabels=labels.ToArray();mapKeys=keys.ToArray();mapRoot.SetActive(false);
            var gore=Node("Blood on the lens",transform);Stretch(gore);blood=gore.AddComponent<Image>();blood.raycastTarget=false;
            blood.material=UnityEngine.Resources.Load<Material>("BloodScreen");blood.color=Color.clear;
        }
        static Vector2 MapPoint(Vector3 p)=>new Vector2(70+(p.x+115)*3.55f,52+(125-p.z)*1.78f);
        void Mark(Vector3 p,string key,Vector2 offset,Color color,List<TMP_Text> labels,List<string> keys)
        {
            var point=MapPoint(p);var mark=Node("Landmark "+key,mapRoot.transform);Place(mark,new Vector2(point.x-5,-point.y+5),new Vector2(10,10));mark.AddComponent<Image>().color=color;
            labels.Add(Text(key,mapRoot.transform,new Vector2(point.x+offset.x,-point.y+offset.y),new Vector2(205,65),18,color));keys.Add(key);
        }
        GameObject Node(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;}
        static void Stretch(GameObject go){var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(GameObject go,Vector2 position,Vector2 size){var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;}
        TMP_Text Text(string name,Transform parent,Vector2 pos,Vector2 size,int pointSize,Color color)
        {var go=Node(name,parent);Place(go,pos,size);var t=go.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=pointSize;t.color=color;t.raycastTarget=false;t.alignment=TextAlignmentOptions.TopLeft;t.overflowMode=TextOverflowModes.Overflow;return t;}
        void MapLine(Vector2 a,Vector2 b)
        {
            float length=Vector2.Distance(a,b);int dashes=Mathf.CeilToInt(length/12);
            for(int i=0;i<dashes;i++)
            {
                var go=Node("Sketched trail",mapRoot.transform);var r=(RectTransform)go.transform;var p=Vector2.Lerp(a,b,(float)i/dashes);
                r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(p.x,-p.y);r.sizeDelta=new Vector2(length/dashes*.7f,2);
                r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(a.y-b.y,b.x-a.x)*Mathf.Rad2Deg);go.AddComponent<Image>().color=new Color(.55f,.52f,.35f);
            }
        }
        void Put(TMP_Text target,string value)=>game.Locale.Apply(target,value);
        string T(string key,params object[] args)=>game.Text(key,args);
        void Choice(int n,string title,Action action,bool enabled=true)
        {buttons[n].gameObject.SetActive(true);buttons[n].interactable=enabled;Put(buttonLabels[n],title);buttons[n].onClick.RemoveAllListeners();buttons[n].onClick.AddListener(()=>{action();Invalidate();});}
        void Close()=>game.Resume();
        public void Refresh()
        {
            if(game==null)return;
            if(revision!=game.Locale.Revision||document!=game.OpenDocument){revision=game.Locale.Revision;document=game.OpenDocument;Invalidate();hint=false;}
            bool playing=game.State.Running,dying=game.State.Mode==ScreenMode.Dying||game.State.Mode==ScreenMode.Intro;hudRoot.SetActive(playing);overlay.SetActive(!playing&&!dying);
            if(blood)blood.color=new Color(1,1,1,game.damage?game.damage.Blood:0);
            if(dying)return;
            overlay.GetComponent<Image>().color=new Color(.018f,.03f,.034f,game.State.Mode==ScreenMode.Dead?.73f:.95f);
            if(playing)
            {
                Put(objective,T("reserve")+"\n"+T(game.Flashlight.Faulted?(game.forestEncounter&&game.forestEncounter.Escaping?"objective_escape":"objective_repair"):game.PowerOn?"objective_gate":game.Inventory.HasFuse?"objective_return":"objective_fuse"));
                Put(resources,(game.Flashlight.Active||game.Flashlight.SwapRemaining>0?T("resources",game.Flashlight.Charge,game.Inventory.Batteries):T("light_off",game.Inventory.Batteries))+(game.player.Stamina<.99f?"\n"+T("breath",game.player.Stamina*100):"")+(game.Condition.FireRecoveryRemaining>0?"\n"+T("fire_recovery",game.Condition.FireRecoveryRemaining):game.Condition.Wounded?"\n"+T("wounded"):"")+(game.Inventory.AxeEquipped?"\n"+T("axe_control"):"")+(game.Inventory.HasShotgun?"\n"+T(game.Inventory.ShotgunEquipped?"shotgun_hud":"shotgun_stowed",game.Inventory.LoadedShells,game.Inventory.SpareShells)+(game.shotgun.ReloadRemaining>0?"\n"+T("shotgun_reloading",game.shotgun.ReloadRemaining):""):""));
                Put(prompt,game.Flashlight.SwapRemaining>0?T("swapping",game.Flashlight.SwapRemaining):game.InteractionPrompt+(game.HoldProgress>0?$"\n{game.HoldProgress*100:0}%":""));Put(message,game.Message);Put(warning,"");
                if(game.visualWarnings&&game.monster.HasSpawned&&game.monster.Distance<8&&!game.IsSafe)
                {var d=game.player.transform.InverseTransformDirection(game.monster.transform.position-game.player.transform.position);Put(warning,game.monster.Attack.WindingUp?T("attack"):T("click_warning",T(d.z<-.5f?"behind":d.x<-.5f?"left":d.x>.5f?"right":"ahead")));}
                vignette.color=new Color(.4f,.025f,.015f,game.Condition.Wounded?.055f:0);
            }
            if(previous==game.State.Mode){if(!playing&&game.State.Mode==ScreenMode.Inventory)Put(body,InventoryText());return;}
            previous=game.State.Mode;foreach(var b in buttons)b.gameObject.SetActive(false);mapRoot.SetActive(false);body.gameObject.SetActive(true);body.fontSize=25;
            Put(eyebrow,T("reserve"));Put(footer,T("controls"));
            switch(game.State.Mode)
            {
                case ScreenMode.Title:
                    Put(heading,T("title"));Put(eyebrow,T("eyebrow"));Put(body,T("intro"));Put(footer,T("demo_footer"));
                    Choice(0,T("begin"),game.StartNew);Choice(1,T("continue"),game.Continue,game.Save.Exists);Choice(2,T("settings"),game.OpenSettings);Choice(3,T("language"),game.ToggleLanguage);Choice(4,T("exit"),()=>Application.Quit());break;
                case ScreenMode.Pause:
                    Put(heading,T("pause_title"));Put(body,T(game.intro&&game.intro.Active?"intro_paused":"pause_body",game.CheckpointLabel));Choice(0,T("resume"),Close);Choice(1,T("load"),game.Continue,game.Save.Exists);Choice(2,T("settings"),game.OpenSettings);Choice(3,T("main"),game.ReturnToTitle);break;
                case ScreenMode.Inventory:
                    Put(heading,T("pack_title"));Put(body,InventoryText());
                    Choice(0,WeaponChoice(HeldWeapon.Axe),()=>game.SelectWeapon(HeldWeapon.Axe),game.Inventory.HasAxe);
                    Choice(1,WeaponChoice(HeldWeapon.Shotgun),()=>game.SelectWeapon(HeldWeapon.Shotgun),game.Inventory.HasShotgun);
                    Choice(2,T("replace"),game.SwapFromInventory,game.Inventory.Batteries>0);
                    Choice(3,T("journal"),()=>game.State.Set(ScreenMode.Journal));Choice(4,T("map_title").Replace('\n',' '),game.OpenMap,game.HasMap);Choice(5,T("close_pack"),Close);break;
                case ScreenMode.Journal:
                    Put(heading,T("journal_title"));body.fontSize=24;Put(body,T(game.OpenDocument??"note_guard_rules"));
                    var found=new List<string>();foreach(var id in ForestLayout.Documents)if(game.Documents.Contains(id))found.Add(id);
                    if(journalPage*5>=found.Count)journalPage=0;
                    for(int i=0;i<5&&i+journalPage*5<found.Count;i++){string id=found[i+journalPage*5];Choice(i,T(id+"_title"),()=>game.ReadNote(id));}
                    if(found.Count>5)Choice(5,T("next_page"),()=>journalPage=journalPage==0?1:0);
                    Choice(6,T("hint"),()=>{hint=true;Invalidate();});Choice(7,T("resume"),Close);
                    if(hint)Put(body,T(game.PowerOn?"hint_gate":game.Inventory.HasFuse?"hint_return":"hint_fuse"));break;
                case ScreenMode.Map:
                    Put(heading,T("map_title"));body.gameObject.SetActive(false);mapRoot.SetActive(true);for(int i=0;i<mapLabels.Length;i++)Put(mapLabels[i],T(mapKeys[i]));Choice(0,T("close_map"),Close);Choice(1,T("journal"),()=>game.State.Set(ScreenMode.Journal));break;
                case ScreenMode.Settings:
                    Put(heading,T("settings_title"));Put(body,T("settings_body"));
                    Choice(0,T("fov",game.player.fieldOfView),()=>game.player.fieldOfView=game.player.fieldOfView>=95?60:game.player.fieldOfView+5);
                    Choice(1,T("sensitivity",game.player.sensitivity),()=>game.player.sensitivity=game.player.sensitivity>.17f?.04f:game.player.sensitivity+.025f);
                    Choice(2,T("invert",T(game.player.invertY?"on":"off")),()=>game.player.invertY=!game.player.invertY);
                    Choice(3,T("bob",T(game.player.headBob?"on":"off")),()=>game.player.headBob=!game.player.headBob);
                    Choice(4,T("visual",T(game.visualWarnings?"on":"off")),()=>game.visualWarnings=!game.visualWarnings);
                    Choice(5,T("volume",AudioListener.volume*100),()=>AudioListener.volume=AudioListener.volume<=.05f?1:Mathf.Max(0,AudioListener.volume-.25f));
                    Choice(6,T("language"),game.ToggleLanguage);Choice(7,T("display",T(game.Display.Fullscreen?"fullscreen":"windowed")),game.ToggleFullscreen);
                    Choice(8,T("intro_subtitles",T(game.introSubtitles?"on":"off")),()=>game.introSubtitles=!game.introSubtitles);
                    Choice(9,T("intro_flash",T(game.reducedIntroFlash?"off":"on")),()=>game.reducedIntroFlash=!game.reducedIntroFlash);
                    Choice(10,T("back"),game.LeaveSettings);break;
                case ScreenMode.Dead:Put(heading,T("dead_title"));Put(body,T("dead_body"));Choice(0,T("continue"),game.Continue,game.Save.Exists);Choice(1,T("again"),game.StartNew);Choice(2,T("main"),game.ReturnToTitle);break;
                case ScreenMode.Complete:Put(heading,T("complete_title"));Put(eyebrow,T("complete_eyebrow"));Put(body,T("complete_body",TimeSpan.FromSeconds(game.ActiveSeconds).ToString(@"mm\:ss"),game.Documents.Count));Choice(0,T("main"),game.ReturnToTitle);Choice(1,T("again"),game.StartNew);break;
            }
        }
        string WeaponName(HeldWeapon weapon)=>T(weapon==HeldWeapon.Axe?"weapon_axe":"weapon_shotgun");
        string WeaponChoice(HeldWeapon weapon)=>T(game.Inventory.OwnsWeapon(weapon)?game.RequestedWeapon==weapon?"weapon_selected":"weapon_equip":"weapon_missing",WeaponName(weapon));
        string InventoryText()=>T("inventory_body",game.Flashlight.Charge,game.Inventory.Batteries,T(game.Inventory.HasFuse?"carried":game.PowerOn?"installed":"not_found"),T(game.HasMap?"carried":"not_found"),game.CheckpointLabel)
            +"\n\n"+T("weapon_pack",game.Inventory.OwnsWeapon(game.RequestedWeapon)?WeaponName(game.RequestedWeapon):T("not_found"))
            +(game.Inventory.HasShotgun?"\n"+T("shotgun_hud",game.Inventory.LoadedShells,game.Inventory.SpareShells):"");
    }
}
