"""Original deterministic five-second containment siren, fluid/motor and creature layers."""
from pathlib import Path
import numpy as np
import wave
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/_Game/Audio';SR=24000
rng=np.random.default_rng(10514)
def write(name,sound):
    fade=min(int(SR*.035),len(sound)//2);sound[:fade]*=np.linspace(0,1,fade);sound[-fade:]*=np.linspace(1,0,fade)
    sound=np.tanh(sound);sound*=.82/max(.82,np.max(np.abs(sound)))
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.int16(sound*32767).tobytes())
def timeline(seconds):return np.arange(round(seconds*SR))/SR
def filtered_noise(n,k):return np.convolve(rng.normal(size=n+k-1),np.ones(k)/k,'valid')
t=timeline(5);phase=np.cumsum(630+260*np.sin(t*np.pi*1.6))/SR*2*np.pi
write('research_alarm',.35*np.sin(phase)+.12*np.sin(2*phase)+.035*np.sin(3*phase))
t=timeline(5);water=filtered_noise(len(t),21)*.65+filtered_noise(len(t),113)*1.3
for i in range(46):
    start=int(rng.uniform(0,4.8)*SR);n=min(int(SR*.18),len(t)-start);q=np.arange(n)/SR
    water[start:start+n]+=.10*np.sin(2*np.pi*(140*q+800*q*q))*np.exp(-q*24)
write('research_drain',water*(.7+.3*np.sin(t*8)**2))
t=timeline(1.2);write('research_hatch',(.22*np.sin(2*np.pi*77*t)+filtered_noise(len(t),8)*.52)*np.sin(np.pi*t/1.2)**.4+.12*np.sin(2*np.pi*1400*t)*np.exp(-t*24))
for key,duration,base in [('growl',2.5,48),('attack',.9,66),('death',2.2,40)]:
    t=timeline(duration);phase=np.cumsum(base+9*np.sin(t*6)+3*np.sin(t*25))/SR*2*np.pi
    throat=(np.sin(phase)+.42*np.sin(phase*2)+.20*np.sin(phase*5))*.20
    hiss=filtered_noise(len(t),7)*(.5+.5*np.sin(phase*.48)**2)*.38
    envelope=np.sin(np.pi*t/duration)**.5
    write('research_'+key,(throat+hiss)*envelope)
print('Research alarm (5.000s), draining, hatch and three creature voices generated.')
