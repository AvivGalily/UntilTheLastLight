using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
namespace Game
{
    public sealed class Localization
    {
        [Serializable] sealed class Table { public Entry[] entries; }
        [Serializable] sealed class Entry { public string id,value; }
        readonly Dictionary<string,string> english=new Dictionary<string,string>(), hebrew=new Dictionary<string,string>();
        readonly Dictionary<TMP_Text,(string text,int revision,float width,float size)> displayed=new Dictionary<TMP_Text,(string,int,float,float)>();
        public bool Hebrew {get;private set;}
        public int Revision {get;private set;}
        public Localization(TextAsset en,TextAsset he)
        { Read(en,english);Read(he,hebrew);Hebrew=PlayerPrefs.GetString("LastLight.Language","he")=="he"; }
        static void Read(TextAsset asset,Dictionary<string,string> target)
        { if(asset)foreach(var e in JsonUtility.FromJson<Table>(asset.text).entries)target[e.id]=e.value; }
        public void SetHebrew(bool value)
        { Hebrew=value;Revision++;PlayerPrefs.SetString("LastLight.Language",value?"he":"en");PlayerPrefs.Save(); }
        public string Get(string id,params object[] args)
        {
            string value;
            if(!(Hebrew&&hebrew.TryGetValue(id,out value))&&!english.TryGetValue(id,out value))value=id;
            return args.Length==0?value:string.Format(System.Globalization.CultureInfo.InvariantCulture,value,args);
        }
        public void Apply(TMP_Text target,string logical)
        {
            float width=target.rectTransform.rect.width;
            if(displayed.TryGetValue(target,out var old)&&old.text==logical&&old.revision==Revision&&old.width==width&&old.size==target.fontSize)return;
            displayed[target]=(logical,Revision,width,target.fontSize);
            bool rtl=Hebrew&&HebrewText.ContainsHebrew(logical);
            target.isRightToLeftText=false; // The line reordering below preserves Latin controls and numeric runs.
            if(target.alignment==TextAlignmentOptions.TopLeft||target.alignment==TextAlignmentOptions.TopRight)
                target.alignment=rtl?TextAlignmentOptions.TopRight:TextAlignmentOptions.TopLeft;
            if(target.alignment==TextAlignmentOptions.MidlineLeft||target.alignment==TextAlignmentOptions.MidlineRight)
                target.alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            target.textWrappingMode=rtl?TextWrappingModes.NoWrap:TextWrappingModes.Normal;
            target.richText=false;
            target.text=rtl?HebrewText.Visual(Wrap(target,logical)):logical;
        }
        static string Wrap(TMP_Text target,string value)
        {
            var result=new StringBuilder();float width=target.rectTransform.rect.width-8;
            foreach(string paragraph in value.Split('\n'))
            {
                string line="";
                foreach(var word in paragraph.Split(' '))
                {
                    string next=line.Length==0?word:line+" "+word;
                    if(line.Length>0&&target.GetPreferredValues(next,float.PositiveInfinity,float.PositiveInfinity).x>width)
                    {result.AppendLine(line);line=word;}else line=next;
                }
                result.AppendLine(line);
            }
            return result.ToString().TrimEnd('\n','\r');
        }
    }
    // Hebrew, Latin, numbers and neutral punctuation for the game's unpointed Hebrew text.
    // Resolve each already-wrapped logical line independently so paragraph order is retained.

}
