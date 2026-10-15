"""CC0 canine bark and layered creature voices, with retained sources/hashes."""
import array,hashlib,json,math,urllib.request,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/_Game/Audio';VENDOR=ROOT/'ArtSource/Vendor/enemy-voices-cc0';SR=22050
VENDOR.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
sources=[]
for filename,title,author,page in [('dog_barking_mono.wav','Dog barking mono','Brandon Morris / HaelDB','dog-barking-mono'),('ghost.wav','Ghost','Ogrebane','ghost')]:
    url='https://opengameart.org/sites/default/files/'+filename;target=VENDOR/filename
    if not target.exists():
        with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'UntilTheLastLight/0.8 audio authoring'}),timeout=40) as response:target.write_bytes(response.read())
    sources.append(dict(title=title,author=author,license='CC0',page='https://opengameart.org/content/'+page,download=url,sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
(VENDOR/'sources.json').write_text(json.dumps(sources,indent=2)+'\n',encoding='utf-8')
def sample(data,index):
    left=int(index)
    if left<0 or left>=len(data)-1:return 0
    fraction=index-left;return data[left]*(1-fraction)+data[left+1]*fraction
def read(path):
    with wave.open(str(path),'rb') as stream:
        channels=stream.getnchannels();rate=stream.getframerate()
        width=stream.getsampwidth();raw=stream.readframes(stream.getnframes())
        if width==2:data=array.array('h',raw)
        elif width==1:data=[v-128 for v in raw]
        else:data=[int.from_bytes(raw[i:i+width],'little',signed=True) for i in range(0,len(raw),width)]
    mono=[sum(data[i:i+channels])/channels/(2**(width*8-1)) for i in range(0,len(data),channels)]
    return [sample(mono,i*rate/SR) for i in range(round(len(mono)*SR/rate))]
def write(name,data,peak=.72):
    mean=sum(data)/len(data);data=[v-mean for v in data];fade=min(int(SR*.025),len(data)//8)
    for i in range(fade):data[i]*=i/fade;data[-1-i]*=i/fade
    maximum=max(abs(v) for v in data) or 1
    pcm=array.array('h',(round(v/maximum*peak*32767) for v in data))
    with wave.open(str(OUT/(name+'.wav')),'wb') as stream:stream.setparams((1,2,SR,len(data),'NONE','not compressed'));stream.writeframes(pcm.tobytes())
dog=read(VENDOR/'dog_barking_mono.wav');block=int(SR*.025)
energies=[sum(v*v for v in dog[i:i+block]) for i in range(0,len(dog),block)]
best=max(range(len(energies)),key=energies.__getitem__);start=max(0,best-2);end=min(len(energies),best+2);threshold=energies[best]*.045
while start>0 and energies[start-1]>threshold and best-start<10:start-=1
while end<len(energies) and energies[end]>threshold and end-best<16:end+=1
start=max(0,min(start,best-4));end=min(len(energies),max(end,best+9))
single=dog[start*block:min(len(dog),(end+1)*block)]
write('wolf_bark',[sample(single,i*.88) for i in range(round(len(single)/.88))])
def trim(data):
    active=[i for i,v in enumerate(data) if abs(v)>.012]
    return data[max(0,active[0]-500):min(len(data),active[-1]+2000)]
ghost=trim(read(VENDOR/'ghost.wav'));human=ROOT/'ArtSource/Vendor/yells-cc0/yelling sounds'
for name,seconds,filename in [('walker_growl',2.8,'3grunt1.wav'),('walker_roar',3.1,'3yell13.wav')]:
    voice=trim(read(human/filename));data=[]
    for i in range(round(seconds*SR)):
        t=i/SR;envelope=min(1,t/.055,(seconds-t)/.3)
        organic=.64*sample(voice,i*.49)+.33*sample(ghost,i*.65)+.15*sample(voice,i*.72)
        data.append((organic*(.7+.3*math.sin(t*math.tau*21)**2)+.025*math.sin(t*math.tau*43)*math.sin(math.pi*t/seconds))*max(0,envelope))
    write(name,data,.74)
print('Generated mono wolf bark and two layered Walker voices from retained CC0 recordings; sources.json contains credits/hashes.')
