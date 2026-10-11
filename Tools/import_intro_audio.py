"""Import supplied opening MP3s without changing speech speed or cutting words.
Retain originals/provenance, decode preloaded PCM, retime VO, and make auditions.
Requires ffmpeg (imageio-ffmpeg or --ffmpeg). No account/network access.
"""
import argparse, array, hashlib, json, math, re, shutil, subprocess, sys, wave
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SR=24000
VOICE=ROOT/'Assets/_Game/Audio/VO/English'
SFX=ROOT/'Assets/_Game/Audio/Opening'
SOURCE=ROOT/'ArtSource/Audio/Runway'
DOCS=ROOT/'Docs/Audio'
LEGACY=ROOT/'ArtSource/Audio/LegacyIntro'
STARTS=[8,14,20,34,40,44.6,50,57,64.5,80,86.25,89.6]
ACTORS=['Tyler','Jack','Tyler','Ethan','Jack','Ethan','Tyler','Jack','Tyler','Ethan','Jack','Tyler']
ROLES={'SFX_ForestVehicleAmbience':('vehicle',True,2),'SFX_ForestGate':('gate',False,2),
       'SFX_CreatureApproach':('threat',False,1),'SFX_VehicleExplosion':('blast',False,2)}

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def retain(source,target):
    if source.resolve()!=target.resolve():shutil.copy2(source,target)
def stamp(t):
    n=round(t*1000)
    return f'{n//3600000:02}:{n//60000%60:02}:{n//1000%60:02},{n%1000:03}'
def write(path,data,channels):
    pcm=array.array('h',(round(max(-.999,min(.999,v))*32767) for v in data))
    if sys.byteorder!='little':pcm.byteswap()
    with wave.open(str(path),'wb') as w:w.setparams((channels,2,SR,len(data)//channels,'NONE',''));w.writeframes(pcm.tobytes())
def decode(ffmpeg,path,channels):
    result=subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-i',str(path),'-ar',str(SR),'-ac',str(channels),'-f','f32le','pipe:1'],check=True,capture_output=True)
    data=array.array('f',result.stdout)
    if sys.byteorder!='little':data.byteswap()
    if not data or any(not math.isfinite(v) for v in data):raise ValueError('Invalid audio '+str(path))
    return data
def prepare(ffmpeg,source,target,channels,loop=False):
    data=decode(ffmpeg,source,channels);original_duration=len(data)/channels/SR
    decoded_peak=max(map(abs,data));crossfade=0
    if loop:
        # Blend the tail into the head, preserving a continuous loop join.
        frames=min(round(SR*.15),len(data)//channels//4);crossfade=frames/SR
        head=data[:frames*channels];data=data[frames*channels:]
        for frame in range(frames):
            a=frame/(frames-1)
            for channel in range(channels):
                i=(len(data)//channels-frames+frame)*channels+channel
                data[i]=data[i]*(1-a)+head[frame*channels+channel]*a
    ceiling=.85 if channels==1 and target.parent==VOICE else .80
    gain=min(1,ceiling/(max(map(abs,data)) or 1))
    data=array.array('f',(v*gain for v in data));write(target,data,channels)
    peak=max(map(abs,data));rms=math.sqrt(sum(v*v for v in data)/len(data))
    return data,{'file':target.relative_to(ROOT).as_posix(),'source':source.relative_to(ROOT).as_posix(),
                 'sourceSha256':digest(source),'sha256':digest(target),'sourceDuration':round(original_duration,5),
                 'duration':round(len(data)/channels/SR,5),'channels':channels,'sampleRate':SR,'bits':16,
                 'gainDb':round(20*math.log10(gain),3),'decodedSourcePeak':round(decoded_peak,5),
                 'peak':round(peak,5),'rmsDb':round(20*math.log10(max(rms,1e-9)),2),
                 'clippedSamples':sum(abs(v)>=1 for v in data),'loopCrossfade':crossfade,'speed':1.0}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--source',type=Path,default=Path('C:/Users/avivg/Downloads'))
    parser.add_argument('--ffmpeg');args=parser.parse_args()
    if args.ffmpeg:ffmpeg=args.ffmpeg
    else:
        try:
            import imageio_ffmpeg
            ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
        except ImportError:
            ffmpeg=shutil.which('ffmpeg')
            if not ffmpeg:raise RuntimeError('Install imageio-ffmpeg or pass --ffmpeg.')
    manifest_path=args.source/'Forest_Opening_Audio_Manifest.json'
    manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    if len(manifest['dialogue'])!=12 or {e['assetName'] for e in manifest['effects']}!=set(ROLES):raise ValueError('Unexpected supplied manifest.')
    for i,line in enumerate(manifest['dialogue']):
        if line['id']!=f'L{i+1:02}' or line['speaker']!=ACTORS[i] or line['assetName']!=f'VO_{ACTORS[i]}_L{i+1:02}' or line['expectedExtension']!='.mp3':raise ValueError('Invalid voice identity.')
    for entry in manifest['dialogue']+manifest['effects']:
        if entry['expectedExtension']!='.mp3' or not (args.source/(entry['assetName']+'.mp3')).is_file():raise FileNotFoundError(args.source/(entry['assetName']+'.mp3'))
    for folder in (VOICE,SFX,SOURCE,DOCS,LEGACY):folder.mkdir(parents=True,exist_ok=True)
    # Keep the former authored takes outside Unity; stable WAV/meta paths retain GUIDs.
    previous=DOCS/'ForestOpening_VoiceManifest.json'
    if not (LEGACY/'ForestOpening_VoiceManifest.json').exists():shutil.copy2(previous,LEGACY/previous.name)
    for path in VOICE.glob('*.wav'):
        if not (LEGACY/path.name).exists():shutil.copy2(path,LEGACY/path.name)
    retain(manifest_path,SOURCE/manifest_path.name)
    records=[];pcm=[];captions=[];elapsed=.35
    for i,line in enumerate(manifest['dialogue']):
        source=SOURCE/(line['assetName']+'.mp3');retain(args.source/source.name,source)
        data,stats=prepare(ffmpeg,source,VOICE/(line['assetName']+'.wav'),1)
        budget=round(max(2.15,stats['duration']+.15),5)
        end=STARTS[i]+budget;next_start=STARTS[i+1] if i<11 else 92
        if end>next_start-.05:raise ValueError(f'{line["id"]} needs a longer playback slot ({end} vs {next_start}).')
        record=dict(line,actor=line['speaker'],voice=manifest['voices'][line['speaker']],provider='User-supplied Runway MP3',start=STARTS[i],budget=budget,**stats)
        records.append(record);pcm.extend([0.0]*round(.35*SR));pcm.extend(data)
        caption=f'{line["speaker"]}: {line["text"]}'
        (VOICE/(line['assetName']+'.srt')).write_text(f'1\n{stamp(0)} --> {stamp(stats["duration"])}\n{caption}\n',encoding='utf-8')
        captions.append(f'{i+1}\n{stamp(elapsed)} --> {stamp(elapsed+stats["duration"])}\n{caption}\n');elapsed+=stats['duration']+.35
        print(line['assetName'],stats['duration'],'s; starts',STARTS[i],'; window',budget,flush=True)
    voice_manifest={'provider':'User-supplied Runway MP3s','language':manifest['language'],'voices':manifest['voices'],
                    'sampleRate':SR,'channels':1,'bits':16,'sourceManifest':(SOURCE/manifest_path.name).relative_to(ROOT).as_posix(),
                    'conversion':'Complete decoded takes; no word trimming, pitch change or time stretching. Linear peak attenuation only if required.',
                    'lines':records,'missing':[]}
    (DOCS/'ForestOpening_VoiceManifest.json').write_text(json.dumps(voice_manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    write(DOCS/'ForestOpening_Dialogue_Preview.wav',pcm,1)
    (DOCS/'ForestOpening_Dialogue_Preview.srt').write_text('\n'.join(captions),encoding='utf-8')
    effects=[];effect_data={}
    for entry in manifest['effects']:
        role,loop,channels=ROLES[entry['assetName']]
        if entry['loop']!=loop:raise ValueError('Loop mode mismatch '+entry['assetName'])
        source=SOURCE/(entry['assetName']+'.mp3');retain(args.source/source.name,source)
        data,stats=prepare(ffmpeg,source,SFX/(entry['assetName']+'.wav'),channels,loop)
        effects.append(dict(entry,role=role,**stats));effect_data[role]=(data,channels)
        print(entry['assetName'],stats['duration'],'s;',channels,'channels; loop',loop,flush=True)
    effect_manifest={'provider':'User-supplied Runway MP3s','sourceManifest':(SOURCE/manifest_path.name).relative_to(ROOT).as_posix(),
                     'effects':effects,'timing':{'vehicle':'drive to blackout; fades at forest silence','gate':'gateCrossed+1, aligned to closing leaves',
                     'threat':'discovery-0.25, just before Ethan hears it; moves from rear-left to vehicle at threat beat',
                     'blast':'blast; complete 6-second tail through blackout'},'missing':[]}
    (DOCS/'ForestOpening_SuppliedAudio.json').write_text(json.dumps(effect_manifest,indent=2)+'\n',encoding='utf-8')
    preview=[0.0]*round(14*SR)
    for role,at,gain in [('threat',.5,.70),('blast',8,.72)]:
        data,channels=effect_data[role]
        for frame in range(len(data)//channels):
            dest=round(at*SR)+frame
            if dest>=len(preview):break
            preview[dest]+=sum(data[frame*channels:frame*channels+channels])/channels*gain
    write(DOCS/'ForestOpening_ThreatImpact_Preview.wav',preview,1)
    print('Imported all twelve voices and four SFX. Original MP3s retained; previous voices archived outside Assets.')

if __name__=='__main__':main()
