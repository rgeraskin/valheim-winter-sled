"""Builds the sled's sound loops from the recordings in sounds/src (see sounds/SOURCES.md for who made them).

A loop of a recording gives itself away: every few seconds the same crunch comes back, and the ear soon hears
the pattern. Random longer stretches don't either: each one sounds a bit different, so the sled's sound changes
by itself while nothing else does. So each loop is a texture rebuilt from the recording: short grains (an eighth
of a second, overlapping by half, power-complementary windows, each brought to the same loudness) taken from
random places in its steady stretches. That keeps the recording's tone and its fine crackle, but evens out
everything slower, so the sound is steady and has nothing in it to recognize. The grains wrap around the loop's end, so it repeats
seamlessly.

The steady stretches (picked by ear and by their loudness over time; the ranges below) are each high-passed
(wind rumble and handling noise) and brought to the same loudness first; the loop is leveled again, its
loudest peaks softly limited, and written as 16-bit mono WAV into WinterSled/Resources/sounds/, which the
plugin embeds.

Usage: python3 sounds/build_sounds.py   (stdlib plus ffmpeg, which mise provides)
"""

import array
import math
import random
import subprocess
import sys
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "src"
OUT = ROOT.parent / "WinterSled" / "Resources" / "sounds"

RATE = 24000          # keep in sync with SledAudio.Rate
LOOP = 8.0            # seconds; keep in sync with SledAudio.LoopSeconds
GRAIN = 0.125         # seconds, unless a loop sets its own: long enough to keep a crackle whole, short enough to even out anything slower
QUIET = 0.3           # grains quieter than this (times the recording's RMS) are passed over
HIGHPASS = 70         # Hz
TARGET_RMS_DB = -18.0
KNEE = 0.6            # peaks above this are softly limited, never past CEILING
CEILING = 0.95

# loop: the stretches [(freesound id, from s, to s), ...] and optionally: "filter" (more ffmpeg filters), "grain" (s;
# LOOP must hold a whole number of half-grains), "speed" (faster and higher: 1.1 = 10 % up; 0.9 slower and deeper),
# "crest" (grains whose peak stands this many times over their RMS are passed over: clicks and rattles), "steady"
# (0..1: how much of the recording's fast loudness flutter, a rattle, is evened out before the grains; see steady()), "noise"
# (True: not grains of the recording but noise with its tone; see colored()), "seed" (the random choice of grains;
# otherwise the loop's place in this list)
POOLS = {
    "snow": {"from": [("612664", 0.0, 10.0), ("612665", 0.5, 11.5)]},
    # deep powder: the snow, heavier: more bass, softer highs (the plugin also plays it lower)
    "powder": {"from": [("612664", 0.0, 10.0), ("612665", 0.5, 11.5)], "filter": "bass=g=8:f=180,treble=g=-6:f=3000"},
    # plastic sled on icy snow; 2.55-3.4 s has a child's shout in it. Spare: sliding on an icy road,
    # {"from": [("420392", 0.0, 53.0)], "crest": 5.5, "seed": 100}
    "ice": {"from": [("863338", 1.0, 2.55), ("863338", 3.4, 5.6)], "crest": 5.5, "seed": 100},
    # gritty, with sharp knocks in it: an 8 s loop of them repeats audibly, so the knocks are passed over
    "frozen": {"from": [("420634", 6.5, 9.5), ("420634", 11.5, 17.5)], "crest": 5.0},
    "ground": {"from": [("488335", 1.0, 6.3), ("488335", 7.5, 12.2), ("488335", 13.5, 24.5)]},
    # longer grains: an eighth of a second chops up the knock that makes it wood
    "wood": {"from": [("710621", 10.5, 17.5), ("710621", 19.5, 28.0), ("710621", 44.5, 55.0), ("710621", 59.5, 68.5)], "grain": 0.25, "seed": 100},
    # air at a car window at speed; below 500 Hz is the car (engine, road), above it the air
    "wind": {"from": [("439242", 10.0, 40.0)], "filter": "highpass=f=500,highpass=f=500", "seed": 100},
}


def decode(path, start, end, extra=None, speed=1.0):
    # cut first: a speed change moves every later timestamp
    filters = f"atrim={start}:{end},asetpts=PTS-STARTPTS,highpass=f={HIGHPASS}" + (f",{extra}" if extra else "")
    if speed != 1.0:
        # played faster, as a tape would be: higher too
        filters += f",aresample={RATE},asetrate={RATE * speed:.0f},aresample={RATE}"
    cmd = ["ffmpeg", "-v", "error", "-i", str(path), "-af", filters, "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"]
    samples = array.array("f")
    samples.frombytes(subprocess.run(cmd, capture_output=True, check=True).stdout)
    return list(samples)


def level(data, db):
    rms = math.sqrt(sum(s * s for s in data) / len(data))
    gain = 10 ** (db / 20) / max(rms, 1e-9)
    return [s * gain for s in data]


def texture(pool, seed, grain=GRAIN, crest=None):
    """LOOP seconds of grains from random places in the pool, overlapping by half and wrapping around the end."""
    size = int(grain * RATE) // 2 * 2
    hop = size // 2
    n = int(LOOP * RATE)
    if n % hop:
        raise ValueError(f"a {grain} s grain doesn't fit the {LOOP} s loop a whole number of times")
    # sine windows: at half overlap their squares add up to one, so uncorrelated grains keep a steady level
    window = [math.sin(math.pi * (i + 0.5) / size) for i in range(size)]
    target = math.sqrt(sum(s * s for s in pool) / len(pool))
    rng = random.Random(seed)
    out = [0.0] * n
    for k in range(n // hop):
        # every grain at the same loudness, so none stands out; a grain from a near-silent spot, or one with a click
        # in it, is passed over
        for _ in range(50):
            start = rng.randrange(len(pool) - size)
            part = pool[start:start + size]
            rms = math.sqrt(sum(s * s for s in part) / size)
            if rms > target * QUIET and (crest is None or max(abs(s) for s in part) < crest * rms):
                break
        gain = target / max(rms, 1e-9)
        at = k * hop
        for i in range(size):
            out[(at + i) % n] += pool[start + i] * window[i] * gain
    return out


def steady(data, amount, window=0.005):
    """
    Evens out the fast flutter in loudness (a rattle: 8-40 times a second) by `amount` (0..1, in dB: 0.5 halves it),
    leaving the tone alone: each sample is scaled against the loudness of the few milliseconds around it.
    """
    half = max(1, int(window * RATE / 2))
    squares = [s * s for s in data]
    running, total = [0.0], 0.0
    for v in squares:
        total += v
        running.append(total)
    mean = math.sqrt(total / len(data))
    out = []
    for i, s in enumerate(data):
        a, b = max(0, i - half), min(len(data), i + half + 1)
        local = math.sqrt((running[b] - running[a]) / (b - a))
        out.append(s * min(4.0, max(0.25, (mean / max(local, 1e-9)) ** amount)))
    return out


def fft(x, inverse=False):
    """Radix-2 FFT of a list of complex numbers (its length a power of two)."""
    n = len(x)
    x = list(x)
    j = 0
    for i in range(1, n):
        bit = n >> 1
        while j & bit:
            j ^= bit
            bit >>= 1
        j |= bit
        if i < j:
            x[i], x[j] = x[j], x[i]
    size = 2
    sign = 1 if inverse else -1
    while size <= n:
        step = complex(math.cos(2 * math.pi / size), sign * math.sin(2 * math.pi / size))
        for start in range(0, n, size):
            w = 1
            for k in range(size // 2):
                a, b = x[start + k], x[start + k + size // 2] * w
                x[start + k], x[start + k + size // 2] = a + b, a - b
                w *= step
        size *= 2
    return [v / n for v in x] if inverse else x


def colored(pool, seed):
    """
    LOOP seconds of noise with the pool's tone and nothing else: its average spectrum (narrow peaks, like a squeak,
    smoothed away) on random phases, in grain-sized frames overlapping by half and wrapping around the end. What the
    recording does over time (rattle, clicks, a voice) doesn't come along: for a recording whose tone is right but
    whose texture isn't.
    """
    m = 4096
    hann = [0.5 - 0.5 * math.cos(2 * math.pi * i / m) for i in range(m)]
    power = [0.0] * (m // 2 + 1)
    frames = 0
    for start in range(0, len(pool) - m, m // 4):
        spectrum = fft([pool[start + i] * hann[i] for i in range(m)])
        for k in range(len(power)):
            power[k] += abs(spectrum[k]) ** 2
        frames += 1
    # a median over ~70 Hz takes out narrow tones, a short mean evens the rest
    med = [sorted(power[max(0, k - 6):k + 7])[len(power[max(0, k - 6):k + 7]) // 2] for k in range(len(power))]
    mag = [math.sqrt(sum(med[max(0, k - 3):k + 4]) / len(med[max(0, k - 3):k + 4]) / max(frames, 1)) for k in range(len(med))]
    size = int(GRAIN * RATE) // 2 * 2
    hop = size // 2
    n = int(LOOP * RATE)
    window = [math.sin(math.pi * (i + 0.5) / size) for i in range(size)]
    rng = random.Random(seed)
    out = [0.0] * n
    for k in range(n // hop):
        half = [mag[i] * complex(math.cos(phi), math.sin(phi)) for i, phi in enumerate(rng.uniform(0, 2 * math.pi) for _ in mag)]
        half[0] = half[-1] = 0
        frame = fft(half + [v.conjugate() for v in reversed(half[1:-1])], inverse=True)
        at = k * hop
        for i in range(size):
            out[(at + i) % n] += frame[i].real * window[i]
    return out


def limit(data):
    room = CEILING - KNEE
    return [math.copysign(KNEE + room * math.tanh((abs(s) - KNEE) / room), s) if abs(s) > KNEE else s for s in data]


def write_wav(path, data):
    pcm = array.array("h", (int(round(s * 32767)) for s in data))
    if sys.byteorder == "big":
        pcm.byteswap()
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())


def build(pool_spec, seed, src=SRC):
    pool = []
    for sound_id, start, end in pool_spec["from"]:
        # a Freesound id is its MP3 preview; anything else is a file name in src
        name = sound_id if "." in sound_id else f"{sound_id}.mp3"
        part = level(decode(src / name, start, end, pool_spec.get("filter"), pool_spec.get("speed", 1.0)), TARGET_RMS_DB)
        pool += steady(part, pool_spec["steady"]) if pool_spec.get("steady") else part
    if pool_spec.get("noise"):
        return limit(level(colored(pool, seed), TARGET_RMS_DB))
    return limit(level(texture(pool, seed, pool_spec.get("grain", GRAIN), pool_spec.get("crest")), TARGET_RMS_DB))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for seed, (name, spec) in enumerate(POOLS.items()):
        data = build(spec, spec.get("seed", seed))
        path = OUT / f"sled_{name}.wav"
        write_wav(path, data)
        rms = math.sqrt(sum(s * s for s in data) / len(data))
        print(f"{path.relative_to(ROOT.parent)}: {len(data) / RATE:.1f} s of {spec.get('grain', GRAIN) * 1000:.0f} ms grains from "
              f"{', '.join(f'#{i} {a}-{b} s' for i, a, b in spec['from'])}, "
              f"rms {20 * math.log10(rms):.1f} dBFS, peak {20 * math.log10(max(abs(s) for s in data)):.1f} dBFS")


if __name__ == "__main__":
    main()
