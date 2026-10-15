"""Original voiced approach howl and a publicly licensed CC0 creature growl."""
import math,random,struct,wave,json,urllib.request,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Assets/_Game/Audio';vendor=root/'ArtSource/Vendor/wolf-audio-cc0';vendor.mkdir(parents=True,exist_ok=True)
url='https://opengameart.org/sites/default/files/wolf_monster_6.mp3'
file=vendor/'wolf_monster.mp3'
if not file.exists():
    with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'UntilTheLastLight/0.7 asset importer'}),timeout=60) as response:file.write_bytes(response.read())
data=file.read_bytes();(out/'wolf_monster.mp3').write_bytes(data)
(vendor/'source.json').write_text(json.dumps({'title':'Wolf Monster Sound','author':'CaveboyTup','license':'CC0','page':'https://opengameart.org/content/wolf-monster-sound','download':url,'sha256':hashlib.sha256(data).hexdigest(),'description':'Creator modified CC0 horse snorting to make a fantasy wolf growl; not a field recording of a wolf.'},indent=2),encoding='utf-8')
sr=22050;seconds=3.6;rng=random.Random(717);phase=low=0;samples=[]
for i in range(round(seconds*sr)):
    t=i/sr;u=t/seconds;freq=270+105*math.sin(math.pi*min(1,u*1.1))+3*math.sin(t*math.tau*5.1)
    phase+=math.tau*freq/sr;noise=rng.uniform(-1,1);low+=(noise-low)*.06
    envelope=math.sin(math.pi*u)**.8*min(1,t/.25,(seconds-t)/.35)
    voiced=math.sin(phase)*.48+math.sin(phase*2)*.16+math.sin(phase*3)*.045
    samples.append((voiced+low*.16)*envelope*.75)
with wave.open(str(out/'wolf_approach_howl.wav'),'wb') as w:
    w.setparams((1,2,sr,len(samples),'NONE','not compressed'));w.writeframes(b''.join(struct.pack('<h',round(v*32767)) for v in samples))
print('Wolf growl: CC0 CaveboyTup; approach howl: original 3.6-second synthesis.')
