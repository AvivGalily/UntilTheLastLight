"""Original cinematic layers from the project's retained CC0 voices and synthesis."""
import array,math,random,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/_Game/Audio';SR=22050
def read(path):
    with wave.open(str(path),'rb') as s:
        rate=s.getframerate();channels=s.getnchannels();width=s.getsampwidth();raw=s.readframes(s.getnframes())
    values=[int.from_bytes(raw[i:i+width],'little',signed=True)/(2**(width*8-1)) for i in range(0,len(raw),width)]
    return [sum(values[i:i+channels])/channels for i in range(0,len(values),channels)],rate
def sample(values,index):
    n=int(index)
    if n<0 or n>=len(values)-1:return 0
    f=index-n;return values[n]*(1-f)+values[n+1]*f
def write(name,data):
    mean=sum(data)/len(data);data=[v-mean for v in data];peak=max(map(abs,data)) or 1
    pcm=array.array('h',(round(max(-1,min(1,v/peak*.74))*32767) for v in data))
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:w.setparams((1,2,SR,len(data),'NONE',''));w.writeframes(pcm.tobytes())
ghost,rate=read(ROOT/'ArtSource/Vendor/enemy-voices-cc0/ghost.wav')
yell,yr=read(ROOT/'ArtSource/Vendor/yells-cc0/yelling sounds/3yell13.wav')
randomizer=random.Random(140710)
for name,seconds in [('walker_devour',5.6),('flesh_tear',.62),('fatal_sting',1.4)]:
    data=[];filtered=0
    for i in range(round(seconds*SR)):
        t=i/SR;fade=min(1,t/.012,(seconds-t)/.10);noise=randomizer.uniform(-1,1);filtered=filtered*.79+noise*.21
        if name=='walker_devour':
            pulse=.6+.4*math.sin(t*math.tau*8)**2
            value=.45*sample(ghost,(i*rate/SR*.54)%max(2,len(ghost)-1))*pulse+.28*sample(yell,i*yr/SR*.40)+.08*math.sin(t*math.tau*47)*pulse
        elif name=='flesh_tear':
            envelope=math.exp(-t*5)*(.45+.55*math.sin(t*math.tau*23)**2)
            value=(noise*.12+filtered*.9+math.sin(t*math.tau*71)*.1)*envelope
        else:
            envelope=math.exp(-t*3.4)
            value=(filtered*.34+math.sin(math.tau*(58*t-13*t*t))*.3+math.sin(t*math.tau*127)*.2)*envelope
        data.append(value*max(0,fade))
    write(name,data)
print('Generated three limited mono fatal attack layers from retained CC0 voices and original tearing/sting synthesis.')
