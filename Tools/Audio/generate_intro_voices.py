"""Create twelve English neural VO clips, captions, receipt and a dialogue audition.

Authoring only: download the two model files to Logs/VoiceModel first. Synthesis
and the game then run offline. Install Tools/Audio/intro_voice_requirements.txt in a
venv; use --refresh to regenerate cached source takes. No paid account needed.
"""
import argparse
import array
import hashlib
import json
import math
import subprocess
import sys
import wave
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from kokoro_onnx import Kokoro

ROOT = Path(__file__).resolve().parents[2]
RECIPE = ROOT / "ArtSource/Audio/ForestIntroVoice.json"
OUT = ROOT / "Assets/_Game/Audio/VO/English"
CACHE = ROOT / "Logs/IntroVoiceSource"
PREVIEW = ROOT / "Docs/Audio"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
MODEL = ROOT / "Logs/VoiceModel/kokoro-v1.0.onnx"
VOICES = ROOT / "Logs/VoiceModel/voices-v1.0.bin"
SR = 24000


def ffmpeg(*args):
    return subprocess.run(
        [FFMPEG, "-hide_banner", "-loglevel", "error", "-y", *map(str, args)],
        check=True, capture_output=True,
    ).stdout


def read_wave(path):
    with wave.open(str(path), "rb") as w:
        assert w.getnchannels() == 1 and w.getsampwidth() == 2 and w.getframerate() == SR
        pcm = array.array("h", w.readframes(w.getnframes()))
        if sys.byteorder != "little":
            pcm.byteswap()
        return pcm


def write_wave(path, pcm):
    if sys.byteorder != "little":
        pcm = array.array("h", pcm)
        pcm.byteswap()
    with wave.open(str(path), "wb") as w:
        w.setparams((1, 2, SR, len(pcm), "NONE", ""))
        w.writeframes(pcm.tobytes())


def timestamp(seconds):
    ms = round(seconds * 1000)
    return f"{ms // 3600000:02}:{ms // 60000 % 60:02}:{ms // 1000 % 60:02},{ms % 1000:03}"


def caption(start, end, actor, text):
    return f"{timestamp(start)} --> {timestamp(end)}\n{actor}: {text}\n"


def source_take(line, voice, engine, refresh):
    # Source identity includes every synthesis setting, so tuning never reuses
    # the wrong cached take. Original service output remains outside the build.
    identity = {"voice": voice, "text": line["text"], "segments": line.get("segments"), "rate": line["rate"], "engine": "kokoro-v1.0-fp32"}
    key = hashlib.sha256(json.dumps(identity, sort_keys=True).encode()).hexdigest()[:12]
    source = CACHE / f'{line["id"]}_{key}.wav'
    if refresh or not source.exists():
        parts=[]
        for segment in line.get("segments", [{"text":line["text"], "pauseAfter":0}]):
            samples, sample_rate = engine.create(segment["text"], voice=voice, speed=1 + line["rate"] / 100, lang="en-us")
            assert sample_rate == SR
            parts.extend((samples,np.zeros(round(segment["pauseAfter"] * SR),dtype=np.float32)))
        samples=np.concatenate(parts)
        write_wave(source, array.array("h", np.rint(np.clip(samples, -.999, .999) * 32767).astype(np.int16)))
    return source


def generate(refresh):
    recipe = json.loads(RECIPE.read_text(encoding="utf-8"))
    for folder in (OUT, CACHE, PREVIEW):
        folder.mkdir(parents=True, exist_ok=True)
    if not MODEL.exists() or not VOICES.exists():
        raise RuntimeError("Download the modelUrl and voicesUrl in ArtSource/Audio/ForestIntroVoice.json to Logs/VoiceModel first.")
    engine = Kokoro(str(MODEL), str(VOICES))
    records, clips = [], []
    for line in recipe["lines"]:
        voice = recipe["actors"][line["actor"]]["voice"]
        source = source_take(line, voice, engine, refresh)
        # Keep expressive internal pauses. Remove generator lead/tail padding only;
        # never time-stretch or truncate words to make an authored slot fit.
        decoded = CACHE / (line["id"] + "_decoded.wav")
        ffmpeg("-i", source, "-ac", 1, "-ar", SR, "-c:a", "pcm_s16le", decoded)
        pcm = read_wave(decoded)
        active = [i for i, v in enumerate(pcm) if abs(v) >= 64]
        if not active:
            raise RuntimeError(f'{line["id"]}: silent synthesis')
        pcm = pcm[max(0, active[0] - round(.055 * SR)):min(len(pcm), active[-1] + round(.14 * SR))]
        trimmed = CACHE / (line["id"] + "_trimmed.wav")
        write_wave(trimmed, pcm)
        path = OUT / f'VO_{line["actor"]}_{line["id"]}.wav'
        # Gentle normalization, rumble removal and peak headroom. Avoid reverb,
        # robotic pitch effects, or flattening the neural speech dynamics.
        filters = f'highpass=f=65,loudnorm=I=-19:TP=-3:LRA=11,volume={line["gainDb"]}dB,alimiter=limit=0.79:level=false'
        ffmpeg("-i", trimmed, "-af", filters, "-ac", 1, "-ar", SR, "-c:a", "pcm_s16le", path)
        pcm = read_wave(path)
        duration = len(pcm) / SR
        if duration + .08 > line["budget"]:
            raise RuntimeError(f'{line["id"]} takes {duration:.3f}s; budget {line["budget"]}s. Retune rate or start, never clip speech.')
        peak = max(map(abs, pcm)) / 32768
        rms = math.sqrt(sum(float(v) ** 2 for v in pcm) / len(pcm)) / 32768
        if peak >= .999 or rms < .01:
            raise RuntimeError(f'{line["id"]}: clipping or inaudible source')
        record = dict(line, voice=voice, file=path.relative_to(ROOT).as_posix(), duration=round(duration, 5),
                      peak=round(peak, 5), rmsDb=round(20 * math.log10(rms), 2), clippedSamples=sum(abs(v) >= 32767 for v in pcm),
                      sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        records.append(record)
        clips.append(pcm)
        path.with_suffix(".srt").write_text("1\n" + caption(0, duration, line["actor"], line["text"]), encoding="utf-8")
        print(f'{line["id"]} {line["actor"]}: {duration:.2f}s, peak {peak:.3f}, {voice}', flush=True)

    # Dry audition: short turn-taking gaps, a longer break before discovery.
    audition, subtitles = array.array("h", [0] * round(.4 * SR)), []
    for n, (record, pcm) in enumerate(zip(records, clips)):
        start = len(audition) / SR
        audition.extend(pcm)
        subtitles.append(f'{n+1}\n' + caption(start, len(audition) / SR, record["actor"], record["text"]))
        audition.extend([0] * round((1.8 if n == 8 else .35) * SR))
    write_wave(PREVIEW / "ForestOpening_Dialogue_Preview.wav", audition)
    (PREVIEW / "ForestOpening_Dialogue_Preview.srt").write_text("\n".join(subtitles), encoding="utf-8")
    receipt = {"provider": recipe["provider"], "providerUrl": recipe["providerUrl"], "synthetic": True,
               "modelSha256": hashlib.sha256(MODEL.read_bytes()).hexdigest(),
               "voicesSha256": hashlib.sha256(VOICES.read_bytes()).hexdigest(),
               "sampleRate": SR, "channels": 1, "bits": 16, "lines": records,
               "preview": "Docs/Audio/ForestOpening_Dialogue_Preview.wav"}
    (PREVIEW / "ForestOpening_VoiceManifest.json").write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f'Created {len(records)} complete neural VO clips and {len(audition)/SR:.1f}s dry audition.', flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", action="store_true")
    generate(parser.parse_args().refresh)
