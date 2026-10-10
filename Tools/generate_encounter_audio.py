"""Original deterministic scare score, crow calls, wing beats and feeding foley.
The English help line is generated separately with the installed David SAPI voice.
"""
import math,random,wave,struct
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]/'Assets/_Game/Audio';SR=22050
def make(name,seconds,kind):
    data=[];rng=random.Random(613);lo=mid=0
    for i in range(int(seconds*SR)):
        t=i/SR;x=rng.uniform(-1,1);lo+=.03*(x-lo);mid+=.3*(x-mid);n=mid-lo
        if kind=='score':
            # Exact periodic oscillators and repeating envelopes avoid loop clicks.
            v=sum(math.sin(t*math.tau*f)*(.11 if j<3 else .04) for j,f in enumerate([43,46,61,123,131,257,269]))
            v*=.55+.45*math.sin(t*math.pi*2)**8
            v+=math.sin(t*math.tau*519+3*math.sin(t*math.tau*.5))*.035
        elif kind=='sting':
            env=min(1,t*200)*math.exp(-t*2.4);v=(n*.8+lo*3+sum(math.sin(t*math.tau*(f+150*t*t))*.12 for f in [52,71,680,739,1133]))*env
        elif kind=='heart':
            env=math.exp(-t*32)+.65*math.exp(-max(0,t-.17)*38)*(t>.17);v=(math.sin(t*math.tau*48)+lo*.7)*env
        elif kind=='crow':
            env=sum(math.sin(math.pi*(t-start)/.30)**2 if start<t<start+.30 else 0 for start in [0,.42,.86])
            v=(sum(math.sin(t*math.tau*(650+85*math.sin(t*13))*h)/h for h in range(1,6))*.2+n*.4)*env
        elif kind=='flap':v=n*sum(math.exp(-((t-k*.14)/.037)**2) for k in range(6))
        elif kind=='bite':v=(lo*4+n*.7+math.sin(t*math.tau*(80-30*t))*.25)*math.exp(-t*8)*(min(1,t*200))
        else:v=(n+math.sin(t*math.tau*(900-600*t))*.2)*max(0,1-t/seconds)*(1 if int(t*60)%3 else 0)
        data.append(v)
    maximum=max(abs(x) for x in data) or 1
    if kind!='score':
        for i in range(min(100,len(data))):data[i]*=i/100;data[-i-1]*=i/100
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setparams((1,2,SR,len(data),'NONE','not compressed'));w.writeframes(b''.join(struct.pack('<h',int(max(-1,min(1,x/maximum*.82))*32767)) for x in data))
for spec in [('gallows_sting',2.7,'sting'),('escape_score',8,'score'),('panic_heartbeat',.55,'heart'),('crow_caw',1.25,'crow'),('crow_wings',1,'flap'),('feeding_impact',.65,'bite'),('flashlight_short',.75,'power')]:make(*spec)
