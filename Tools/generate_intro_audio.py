"""Deterministic opening SFX using retained CC0 voices. No network needed.
Dialogue is authored separately by generate_intro_voices.py.
"""
import math, wave, array, random, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/_Game/Audio';SR=22050

def sample(data,index):
    left=int(index)
    if index<0 or left>=len(data)-1:return 0
    fraction=index-left
    return data[left]*(1-fraction)+data[left+1]*fraction

def read(path):
    with wave.open(str(path),'rb') as stream:
        channels,rate,width=stream.getnchannels(),stream.getframerate(),stream.getsampwidth()
        raw=stream.readframes(stream.getnframes())
    if width==1:data=[v-128 for v in raw]
    else:data=[int.from_bytes(raw[i:i+width],'little',signed=True) for i in range(0,len(raw),width)]
    mono=[sum(data[i:i+channels])/channels/(2**(width*8-1)) for i in range(0,len(data),channels)]
    converted=[sample(mono,i*rate/SR) for i in range(round(len(mono)*SR/rate))]
    active=[i for i,v in enumerate(converted) if abs(v)>.012]
    return converted[max(0,active[0]-500):min(len(converted),active[-1]+2000)]

source_paths=[ROOT/'ArtSource/Vendor/enemy-voices-cc0/ghost.wav',
              ROOT/'ArtSource/Vendor/yells-cc0/yelling sounds/3grunt1.wav',
              ROOT/'ArtSource/Vendor/yells-cc0/yelling sounds/3yell13.wav']
ghost,grunt,yell=[read(path) for path in source_paths]

def grain(data,t,speed,offset=0):
    # Overlap slow grains for a vocal texture without intelligible words.
    length,hop=.92,.46;index=int(t/hop);result=0
    for n in (index-1,index):
        local=t-n*hop
        if 0<=local<length:
            start=((n*.173+offset)%max(.01,len(data)/SR-length*speed))
            result+=sample(data,(start+local*speed)*SR)*math.sin(math.pi*local/length)**2
    return result

stats=[]
clips=[('intro_diesel',6),('intro_gravel',6),('intro_cabin_wind',6),
       ('intro_gate_creak',2.5),('intro_gate_latch',.7),('intro_branch_snap',.8),
       ('intro_threat',8),('intro_blast',3.8),('intro_impact_rattle',1.6),('intro_tinnitus',6)]
for name,duration in clips:
    rng=random.Random(82147+sum(map(ord,name)));samples=[];low=deep=0
    for i in range(round(duration*SR)):
        t=i/SR;noise=rng.uniform(-1,1);low=low*.92+noise*.08;deep=deep*.984+noise*.016
        fade=min(1,t/.015,(duration-t)/.15)
        if name=='intro_diesel':
            value=(math.sin(math.tau*36*t)*.26+math.sin(math.tau*72*t)*.16+low*.3)*(.75+.25*math.sin(math.tau*12*t)**2);fade=1
        elif name=='intro_gravel':
            value=low*.44+noise*.06*(.3+.7*math.sin(t*math.tau*19)**16);fade=1
        elif name=='intro_cabin_wind':
            value=(deep*.8+low*.16)*(.8+.2*math.sin(math.tau*t/6));fade=min(1,t/.08,(duration-t)/.08)
        elif name=='intro_gate_creak':
            value=(math.sin(math.tau*(170*t+40*t*t))*math.sin(t*math.tau*3.2)**2+low)*math.sin(t/duration*math.pi)*.3
        elif name=='intro_gate_latch':
            value=(low+math.sin(t*math.tau*85)*.4+noise*.15)*math.exp(-t*10)
        elif name=='intro_branch_snap':
            value=(noise*.5+low*.6)*math.exp(-t*18)
            value+=(noise*.12+math.sin(t*math.tau*720)*.07)*math.exp(-max(0,t-.13)*15)*(t>=.13)
            fade=min(1,t/.0015,(duration-t)/.08)
        elif name=='intro_threat':
            envelope=min(1,t/.65)*(.30+.70*min(1,t/5.2))*(.65+.35*math.sin(t*math.tau*.43)**2)
            organic=.75*grain(ghost,t,.52)+.65*grain(grunt,t,.44,.12)+.20*grain(yell,t,.37,.30)
            delayed=.26*grain(ghost,max(0,t-.23),.52)+.14*grain(grunt,max(0,t-.41),.44,.12)
            value=(organic+delayed+deep*.20+low*.12+math.sin(math.tau*(29*t+.8*t*t))*.035)*envelope
        elif name=='intro_blast':
            pressure=(deep*3.4+math.sin(math.tau*(62*t-5*t*t))*.72)*math.exp(-t*2.5)
            crack=(noise*.72+low*.60)*math.exp(-t*27)
            tail=(low*.8+deep*.8)*math.exp(-max(0,t-.12)*1.5)*min(1,t/.04)
            value=pressure+crack+tail;fade=min(1,t/.0015,(duration-t)/.4)
        elif name=='intro_impact_rattle':
            value=0
            for at,freq in ((0,410),(.08,920),(.19,230),(.38,1570),(.63,610),(.95,340)):
                age=t-at
                if age>=0:value+=(math.sin(math.tau*freq*age)*.22+noise*.12)*math.exp(-age*18)*(1-at*.65)
            value+=noise*.035*math.exp(-t*3);fade=min(1,t/.002,(duration-t)/.08)
        else:value=math.sin(t*math.tau*1800)*.075+low*.06;fade=1
        samples.append(value*max(0,fade))
    mean=sum(samples)/len(samples);samples=[v-mean for v in samples];peak=max(map(abs,samples)) or 1
    ceiling=.80 if name in ('intro_blast','intro_threat') else .65
    samples=[v/peak*ceiling for v in samples];pcm=array.array('h',(round(v*32767) for v in samples))
    path=OUT/(name+'.wav')
    with wave.open(str(path),'wb') as stream:stream.setparams((1,2,SR,len(samples),'NONE',''));stream.writeframes(pcm.tobytes())
    stats.append({'clip':name,'seconds':duration,'peak':ceiling,'rms':round(math.sqrt(sum(v*v for v in samples)/len(samples)),4),
                  'clippedSamples':sum(abs(v)>=1 for v in samples),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
provenance={'sampleRate':SR,'clips':stats,'sources':[{'file':p.relative_to(ROOT).as_posix(),'license':'CC0',
            'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in source_paths],
            'credits':'Ogrebane: Ghost; Brandon Morris / HaelDB: human grunts/yells. See Docs/ThirdPartyNotices.md.',
            'mix':'Threat approaches from 84s; dialogue ducking retained. Boom at 92s; interior rattle at +0.09s; tinnitus through wake.'}
(ROOT/'ArtSource/Audio/ForestIntroSFX.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')
(ROOT/'Logs/IntroAudio.json').write_text(json.dumps(stats,indent=2)+'\n')
print('Generated ten opening SFX; layered CC0 presence, pressure boom and debris; peaks <= 0.80, no clipping.')
