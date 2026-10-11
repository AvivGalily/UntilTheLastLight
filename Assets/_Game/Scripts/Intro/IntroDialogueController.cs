using TMPro;
using UnityEngine;
namespace Game
{
    public sealed class IntroDialogueController : MonoBehaviour
    {
        public TMP_Text subtitle;
        public GameObject subtitlePanel;
        public AudioSource[] voices;
        public IntroPassenger[] passengers;
        int current=-1;
        public bool Speaking {get{foreach(var voice in voices)if(voice.isPlaying)return true;return false;}}
        public void ResetDialogue(){current=-1;subtitle.text="";subtitlePanel.SetActive(false);foreach(var voice in voices)voice.Stop();}
        public void Simulate(float time,ForestIntroConfig config,Localization locale,bool show)
        {
            int line=-1;
            for(int n=0;n<config.lines.Length;n++)if(time>=config.lines[n].start&&time<config.lines[n].start+config.lines[n].duration){line=n;break;}
            if(line!=current)
            {
                current=line;
                // Let the complete take finish if a slow frame outlasts the
                // subtitle window. Reset/cancel and the next line still stop it.
                if(line>=0){foreach(var voice in voices)voice.Stop();var entry=config.lines[line];var clip=entry.VoiceFor(locale.Hebrew);if(clip)voices[entry.passenger].PlayOneShot(clip);}
            }
            subtitlePanel.SetActive(show&&line>=0);
            if(line>=0){var entry=config.lines[line];locale.Apply(subtitle,locale.Get(entry.speakerKey)+"\n"+locale.Get("intro_"+entry.id));}
            for(int n=0;n<passengers.Length;n++)passengers[n].Pose(time,line>=0&&config.lines[line].passenger==n,n,time>=config.discovery,time>=config.blast);
        }
    }
}
