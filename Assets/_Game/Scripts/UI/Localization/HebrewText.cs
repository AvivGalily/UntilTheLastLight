using System;
using System.Text;
namespace Game
{
    public static class HebrewText
    {
        public static bool ContainsHebrew(string s){foreach(char c in s)if(c>='א'&&c<='ת')return true;return false;}
        public static string Visual(string value)
        {
            string[] lines=value.Split('\n');
            for(int n=0;n<lines.Length;n++)lines[n]=Line(lines[n].TrimEnd('\r'));
            return string.Join("\n",lines);
        }
        static int Strong(char c)=>c>='א'&&c<='ת'?1:char.IsLetterOrDigit(c)?2:0;
        static char Mirror(char c)=>c=='('?')':c==')'?'(':c=='['?']':c==']'?'[':c=='<'?'>':c=='>'?'<':c;
        static string Line(string line)
        {
            if(!ContainsHebrew(line))return line;
            char[] chars=line.ToCharArray();int[] level=new int[chars.Length];
            for(int i=0;i<level.Length;i++)level[i]=Strong(chars[i]);
            for(int i=0;i<level.Length;i++)if(level[i]==0)
            {
                int before=1,after=1;
                for(int j=i-1;j>=0;j--)if(Strong(chars[j])!=0){before=Strong(chars[j]);break;}
                for(int j=i+1;j<level.Length;j++)if(Strong(chars[j])!=0){after=Strong(chars[j]);break;}
                level[i]=before==after?before:1;
                if(chars[i]=='%'&&i>0&&char.IsDigit(chars[i-1]))level[i]=2;
            }
            for(int i=0;i<chars.Length;i++)if(level[i]==1)chars[i]=Mirror(chars[i]);
            // UAX#9 L2 ordering for the resolved 1/2 levels used by these strings.
            for(int i=0;i<level.Length;i++)if(level[i]==2)
            {int start=i;while(i+1<level.Length&&level[i+1]==2)i++;Array.Reverse(chars,start,i-start+1);}
            Array.Reverse(chars);return new string(chars);
        }
    }
}
