"""Seeded original weapon/fire layers and distinct CC0-derived cadaver voices."""
import array,math,random,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/_Game/Audio';SR=22050
def read(path):
    with wave.open(str(path),'rb') as f:
        width=f.getsampwidth();rate=f.getframerate();channels=f.getnchannels();raw=f.readframes(f.getnframes())
    values=[int.from_bytes(raw[i:i+width],'little',signed=True)/(2**(width*8-1)) for i in range(0,len(raw),width)]
    return [sum(values[i:i+channels])/channels for i in range(0,len(values),channels)],rate
def at(values,index):
    i=int(index)%max(1,len(values)-1);return values[i]*(1-index%1)+values[i+1]*(index%1)
ghost,rate=read(ROOT/'ArtSource/Vendor/enemy-voices-cc0/ghost.wav')
rng=random.Random(140711)
for name,duration in [('head_introduction',3.4),('head_growl',4),('head_bite',.65),('head_spit',1.25),('head_death',1.5),('shotgun_fire',1.15),('shotgun_reload',1.7),('shotgun_empty',.12),('wreck_fire',6),('fireball_impact',.7)]:
    samples=[];filtered=0
    for i in range(round(duration*SR)):
        t=i/SR;noise=rng.uniform(-1,1);filtered=filtered*.84+noise*.16
        if name.startswith('head_'):
            speed=.68 if name=='head_growl' else .38 if name=='head_introduction' else .93
            throat=at(ghost,i*rate/SR*speed);pulse=.5+.5*math.sin(t*math.tau*9)**2
            env=math.sin(math.pi*t/duration)**.55
            v=(throat*.54+math.sin(t*math.tau*(72+7*math.sin(t*5)))*.12+filtered*.15)*pulse*env
            if name=='head_spit':v+=filtered*.5*math.exp(-((t-.65)/.16)**2)
        elif name=='shotgun_fire':v=(noise*.7+filtered*.8+math.sin(t*math.tau*58)*.45)*math.exp(-t*10)+filtered*.1*math.exp(-t*3)
        elif name=='shotgun_reload':
            env=sum(math.exp(-max(0,t-p)*70) if t>=p else 0 for p in [0,.38,.68,1.34,1.54]);v=(noise*.2+math.sin(t*math.tau*1100)*.08)*env
        elif name=='shotgun_empty':v=(noise*.2+math.sin(t*math.tau*1450)*.15)*math.exp(-t*55)
        elif name=='wreck_fire':v=filtered*.5+noise*.1*(max(0,math.sin(t*23+math.sin(t*13)))**9)+math.sin(t*math.tau*43)*.04
        else:v=(filtered*.75+noise*.3+math.sin(t*math.tau*68)*.24)*math.exp(-t*5)
        fade=min(1,t/.003,(duration-t)/.03);samples.append(v*max(0,fade))
    peak=max(map(abs,samples)) or 1;pcm=array.array('h',(round(v/peak*.76*32767) for v in samples))
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:f.setparams((1,2,SR,len(pcm),'NONE',''));f.writeframes(pcm.tobytes())
# Original quiet twelve-second dread loop. Every oscillator completes an
# integer number of cycles, so the sustained score has no restart click.
duration=12
samples=[]
for i in range(duration*SR):
    t=i/SR;phase=math.tau*t
    drone=sum(math.sin(phase*f) * gain for f,gain in [(49,.42),(49+1/12,.16),(73+1/12,.18),(103+5/12,.09),(147,.055)])
    swell=.7+.18*math.sin(phase/12)+.12*math.sin(phase*2/12)
    whisper=math.sin(phase*411)*math.sin(phase*419)*math.sin(phase*7/12)*.045
    samples.append(drone*swell+whisper)
peak=max(map(abs,samples));pcm=array.array('h',(round(v/peak*.76*32767) for v in samples))
with wave.open(str(OUT/'head_score.wav'),'wb') as f:f.setparams((1,2,SR,len(pcm),'NONE',''));f.writeframes(pcm.tobytes())
print('Created ten Norton effect clips and the original quiet head approach score.')
