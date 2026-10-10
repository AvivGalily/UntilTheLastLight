"""Original night wildlife synthesis: no recordings or film/game samples.

Periodic stereo insect bed; mono directional owl/nightjar/frog/rustle voices.
Uses only Python's standard library, so asset regeneration needs no service.
"""
from array import array
import math
from pathlib import Path
import random
import struct
import wave

RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_Game/Audio'
OUT.mkdir(parents=True, exist_ok=True)
TAU = math.tau


def sound(seconds, channels=1):
    return [array('d', [0.0]) * round(seconds * RATE) for _ in range(channels)]


def add(clip, onset, duration, voice, gain=1, pan=0, periodic=False):
    count = len(clip[0]); start = round(onset * RATE)
    left = math.sqrt((1 - pan) * .5); right = math.sqrt((1 + pan) * .5)
    for k in range(round(duration * RATE)):
        index = start + k
        if not periodic and not 0 <= index < count: continue
        value = voice(k / RATE, k / max(1, round(duration * RATE) - 1)) * gain
        index %= count
        if len(clip) == 1: clip[0][index] += value
        else:
            clip[0][index] += value * left; clip[1][index] += value * right


def save(name, clip, peak=.55, loop=False):
    maximum = max(abs(value) for channel in clip for value in channel)
    scale = peak / max(maximum, .001)
    data = array('h')
    if loop:
        # Crossfade a short wrap region without inserting a periodic silence.
        for channel in clip:
            n = round(.035 * RATE)
            for k in range(n):
                t = k / (n - 1)
                blend = channel[k] * t + channel[-n + k] * (1 - t)
                channel[k] = blend
            channel[-1] = channel[0]
    for values in zip(*clip):
        data.extend(round(max(-.98, min(.98, value * scale)) * 32767) for value in values)
    if __import__('sys').byteorder != 'little': data.byteswap()
    with wave.open(str(OUT / name), 'wb') as wav:
        wav.setnchannels(len(clip)); wav.setsampwidth(2); wav.setframerate(RATE); wav.writeframes(data.tobytes())
    rms = math.sqrt(sum(value * value for channel in clip for value in channel) / (len(clip) * len(clip[0]))) * scale
    print(name, 'seconds=', len(clip[0]) / RATE, 'channels=', len(clip), 'peak=', round(peak, 3), 'RMS=', round(rms, 4), flush=True)


def generate():
    rng = random.Random(82147)
    bed = sound(18, 2)
    for colony in range(9):
        frequency = 3300 + colony * 291 + rng.uniform(-80, 80)
        pan = -.85 + colony * .21
        period = 18 / rng.randint(15, 24)
        offset = rng.random() * period
        for call in range(math.ceil(18 / period)):
            for pulse in range(rng.randint(3, 5)):
                phase = rng.random() * TAU
                f = frequency + rng.uniform(-65, 65)
                def insect(t, q, f=f, phase=phase):
                    envelope = math.sin(math.pi * q) ** 1.7
                    return envelope * (math.sin(TAU * f * t + phase) + .23 * math.sin(TAU * f * 1.97 * t + phase))
                add(bed, offset + call * period + pulse * .052, .032, insect,
                    gain=.13 + rng.random() * .12, pan=pan, periodic=True)
    save('night_crickets.wav', bed, peak=.38, loop=True)

    owl = sound(4.6)
    for onset, duration, frequency, gain in [( .12, .62, 390, .85), (1.15, 1.12, 355, 1), (2.75, .75, 374, .68)]:
        def hoot(t, q, f=frequency):
            # Soft breath attack, hollow voiced fundamental and low formants.
            envelope = math.sin(math.pi * q) ** 1.3
            phase = TAU * (f * t + 7 * math.sin(math.pi * q) * t + .45 * math.sin(TAU * 4.3 * t))
            return envelope * (math.sin(phase) * .72 + math.sin(phase * 2) * .16 + math.sin(phase * 3) * .045)
        add(owl, onset, duration, hoot, gain)
    save('night_owl.wav', owl, .58)

    nightjar = sound(3.8)
    for i in range(15):
        f = 1330 + rng.uniform(-100, 130)
        def churr(t, q, f=f):
            phase = TAU * (f * t + 180 * t * t + .34 * math.sin(TAU * 38 * t))
            return math.sin(math.pi * q) ** 1.8 * (math.sin(phase) * .7 + math.sin(phase * 1.997) * .18)
        add(nightjar, .18 + i * .17, .105, churr, .6 + .3 * math.sin(i / 14 * math.pi))
    def whistle(t, q):
        return math.sin(math.pi * q) ** 1.5 * math.sin(TAU * (1800 * t + 650 * t * t))
    add(nightjar, 3.1, .32, whistle, .35)
    save('nightjar_calls.wav', nightjar, .45)

    frogs = sound(5.5)
    for i in range(6):
        f = 165 + rng.uniform(-30, 30)
        def croak(t, q, f=f):
            phase = TAU * (f * t + 25 * t * t)
            return math.sin(math.pi * q) ** 1.5 * (.5 * math.sin(phase) + .3 * math.sin(phase * 3) + .15 * math.sin(phase * 5)) * (.7 + .3 * math.sin(TAU * 25 * t))
        add(frogs, .15 + i * .72, .32, croak, .7 + rng.random() * .3)
    save('forest_frogs.wav', frogs, .36)

    rustle = sound(1.7); filtered = 0; previous = 0
    for k in range(len(rustle[0])):
        t = k / RATE; n = rng.uniform(-1, 1); filtered = filtered * .95 + n * .05
        high = n - previous; previous = n
        env = math.sin(math.pi * t / 1.7) ** 1.6 * (.55 + .45 * math.sin(TAU * 4.1 * t) ** 2)
        rustle[0][k] = (filtered * 2.5 + high * .05) * env
    save('leaf_rustle.wav', rustle, .35)


if __name__ == '__main__': generate()
