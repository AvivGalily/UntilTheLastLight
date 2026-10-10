"""Seeded, synthesized prototype audio. No sampled or downloaded source audio."""
import math
import random
import struct
import wave
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'Assets'/'_Game'/'Audio'
out.mkdir(parents=True,exist_ok=True)
rate=22050

def write(name,seconds,kind):
    rng=random.Random(1407)
    samples=[]
    slow=0
    for i in range(int(seconds*rate)):
        t=i/rate
        noise=rng.uniform(-1,1)
        slow=slow*.985+noise*.015
        if kind=='wind':
            v=(slow*2.3+.008*math.sin(t*math.tau*83))*(.65+.25*math.sin(t*math.tau/seconds))
        elif kind=='breath':
            envelope=max(0,math.sin(t*math.tau/3))**2
            v=(slow*2+noise*.04)*envelope+.02*math.sin(t*math.tau*71)*envelope
        elif kind=='warning':
            envelope=math.sin(math.pi*t/seconds)**.5
            v=(math.sin(math.tau*(110*t+70*t*t))*.1+noise*.11)*envelope
        elif kind=='click':
            v=0
            for onset in (0,.071,.16):
                age=t-onset
                if age>=0:
                    v+=(noise*.22+math.sin(math.tau*(1650*age-1200*age*age))*.18)*math.exp(-age*105)
        elif kind=='howl':
            # A voiced formant sweep, airy harmonics and slow vibrato; no sampled film/audio.
            envelope=math.sin(math.pi*t/seconds)**1.5
            phase=math.tau*(310*t+36*seconds/math.pi*math.sin(math.pi*t/seconds)+2.2*math.sin(t*math.tau*4.7))
            v=(math.sin(phase)*.18+math.sin(phase*2)*.09+math.sin(phase*3)*.03+slow*.4)*envelope
        elif kind=='tension':
            v=(math.sin(math.tau*43*t)*.10+math.sin(math.tau*47*t)*.075+math.sin(math.tau*86*t)*.025+slow*.25)*(.75+.15*math.cos(math.tau*t/seconds))
        elif kind=='pulse':
            v=0
            for onset in (.02,.24):
                age=t-onset
                if age>=0:v+=math.sin(math.tau*57*age)*.4*math.exp(-age*21)
        else:
            v=(noise*.2+slow*3)*math.exp(-t*20)*math.sin(min(1,t*80)*math.pi/2)
        fade=min(1,t*20,(seconds-t)*20)
        samples.append(struct.pack('<h',int(max(-.8,min(.8,v*fade))*32767)))
    with wave.open(str(out/name),'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes(b''.join(samples))

write('forest_wind.wav',12,'wind')
write('walker_breath.wav',6,'breath')
write('attack_warning.wav',1.2,'warning')
write('footstep.wav',.3,'step')
write('creature_click.wav',.32,'click')
write('wolf_howl.wav',5.5,'howl')
write('proximity_tension.wav',8,'tension')
write('threat_pulse.wav',.7,'pulse')
print('Generated eight original synthesized sounds, including clicks, distant howl and proximity layers.')
from generate_night_audio import generate
generate()
