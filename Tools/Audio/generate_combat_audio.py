"""Licensed field recordings plus original, deterministic breathing/combat layers."""
import math,random,wave,struct,shutil,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/_Game/Audio';SR=22050
def write(name,samples,peak=.75):
    maximum=max(abs(v) for v in samples) or 1
    data=b''.join(struct.pack('<h',round(max(-1,min(1,v/maximum*peak))*32767)) for v in samples)
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:w.setparams((1,2,SR,len(samples),'NONE','not compressed'));w.writeframes(data)
def noise_clip(name,seconds,kind):
    rng=random.Random(450+len(name));lo=mid=0;data=[]
    for i in range(round(seconds*SR)):
        t=i/SR;x=rng.uniform(-1,1);lo+=.025*(x-lo);mid+=.20*(x-mid);breath=mid-lo
        if kind=='calm' or kind=='hard':
            period=4 if kind=='calm' else 1.55;phase=(t%period)/period
            envelope=math.sin(math.pi*phase/.39)**2*.75 if phase<.39 else math.sin(math.pi*(phase-.47)/.43)**2 if .47<phase<.90 else 0
            v=(breath+.25*lo)*envelope
        elif kind=='whoosh':v=breath*math.sin(math.pi*t/seconds)**2*(.5+t/seconds)
        elif kind=='impact':v=(lo*3+breath*.5+math.sin(t*math.tau*82)*.15)*math.exp(-t*18)*min(1,t*700)
        else:
            envelope=math.sin(math.pi*t/seconds)**.6
            freq=(97+13*math.sin(t*4)) if kind=='growl' else (160-60*t/seconds if kind=='bite' else 320-160*t/seconds)
            carrier=sum(math.sin(t*freq*math.tau*h)/h for h in range(1,9))
            v=(carrier*.15+breath*.5)*(0.55+.45*math.sin(t*math.tau*25)**2)*envelope
        data.append(v)
    write(name,data,.7 if kind in ('impact','bite') else .55)
for name,seconds,kind in [('player_breath_calm',8,'calm'),('player_breath_hard',6.2,'hard'),('axe_swing',.34,'whoosh'),('axe_impact',.30,'impact'),('zombie_growl',2.3,'growl'),('zombie_bite',.65,'bite'),('zombie_death',.8,'yelp')]:noise_clip(name,seconds,kind)
for source,name in [('3yell13.wav','player_scream'),('3grunt1.wav','player_hurt')]:
    with wave.open(str(ROOT/'ArtSource/Vendor/yells-cc0/yelling sounds'/source),'rb') as w:
        rate=w.getframerate();channels=w.getnchannels();raw=struct.unpack('<'+'h'*(w.getnframes()*channels),w.readframes(w.getnframes()))
        # Trim recording silence, downmix and resample the actual human performance.
        mono=[sum(raw[i:i+channels])/channels/32768 for i in range(0,len(raw),channels)]
        active=[i for i,v in enumerate(mono) if abs(v)>.015];a=max(0,active[0]-int(.02*rate));b=min(len(mono),active[-1]+int(.15*rate))
        mono=mono[a:b];resampled=[mono[min(len(mono)-1,int(i*rate/SR))] for i in range(int(len(mono)*SR/rate))]
        fade=min(220,len(resampled)//4)
        for i in range(fade):resampled[i]*=i/fade;resampled[-i-1]*=i/fade
        write(name,resampled,.85)
for source,name in [('leaves01','step_forest_1'),('leaves02','step_forest_2'),('gravel','step_forest_3'),('mud02','step_forest_4'),('wood01','step_wood_1'),('wood02','step_wood_2'),('wood03','step_wood_3')]:
    shutil.copyfile(ROOT/'ArtSource/Vendor/steps-cc0'/(source+'.ogg'),OUT/(name+'.ogg'))
print('Combat audio: 7 original clips, 2 CC0 human performances and 7 CC0 ground steps.')
import runpy
runpy.run_path(str(ROOT/'Tools/Audio/generate_wolf_audio.py'),run_name='__main__')
runpy.run_path(str(ROOT/'Tools/Audio/generate_enemy_audio.py'),run_name='__main__')
