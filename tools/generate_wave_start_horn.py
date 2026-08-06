"""Generate the casual fantasy goblin wave-start horn used by the game."""

from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 48_000
DURATION_SECONDS = 1.12
OUTPUT_PATH = (
    Path(__file__).resolve().parents[1]
    / "Assets"
    / "Audio"
    / "SFX"
    / "wave_start_goblin_horn.wav"
)


def smoothstep(value: float) -> float:
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def envelope(time: float, start: float, duration: float, attack: float, release: float) -> float:
    local = time - start
    if local < 0.0 or local >= duration:
        return 0.0
    attack_gain = smoothstep(local / attack)
    release_gain = smoothstep((duration - local) / release)
    return attack_gain * release_gain


def exponential_glide(start_hz: float, end_hz: float, progress: float) -> float:
    return start_hz * ((end_hz / start_hz) ** smoothstep(progress))


def main() -> None:
    random.seed(20260728)
    sample_count = round(SAMPLE_RATE * DURATION_SECONDS)
    samples = [0.0] * sample_count
    phase = 0.0
    filtered_noise = 0.0

    for index in range(sample_count):
        time = index / SAMPLE_RATE
        frequency = 0.0
        gain = 0.0
        brightness = 0.0

        # Match the reference recording:
        # low C3 call, a slight B2 dip, a connected rise, then a held G3 arrival.
        if 0.025 <= time < 1.085:
            local_time = time - 0.025
            if local_time < 0.22:
                frequency = exponential_glide(130.81, 123.47, local_time / 0.22)
            elif local_time < 0.31:
                frequency = exponential_glide(123.47, 130.81, (local_time - 0.22) / 0.09)
            elif local_time < 0.46:
                frequency = exponential_glide(130.81, 196.0, (local_time - 0.31) / 0.15)
            else:
                held_progress = (local_time - 0.46) / 0.60
                frequency = 196.0 * (1.0 + 0.004 * held_progress)

            frequency *= 1.0 + 0.0025 * math.sin(2.0 * math.pi * 5.0 * time)
            gain = envelope(time, 0.025, 1.06, 0.032, 0.085)

            # A subtle re-articulation marks the higher "arrival" note without
            # inserting the silence that made the first draft feel like two calls.
            arrival = smoothstep((local_time - 0.40) / 0.075)
            articulation_dip = 1.0 - 0.20 * math.exp(
                -((local_time - 0.405) / 0.032) ** 2
            )
            gain *= articulation_dip * (0.82 + 0.22 * arrival)
            brightness = 0.78 + 0.25 * arrival

        if gain <= 0.0:
            continue

        phase += 2.0 * math.pi * frequency / SAMPLE_RATE
        harmonic_weights = (1.0, 0.72, 0.43, 0.27, 0.16, 0.095, 0.055)
        brass = 0.0
        for harmonic, weight in enumerate(harmonic_weights, start=1):
            harmonic_rolloff = brightness ** (harmonic - 1)
            brass += weight * harmonic_rolloff * math.sin(harmonic * phase)

        # A quiet filtered breath layer keeps the result instrumental and horn-like.
        white_noise = random.uniform(-1.0, 1.0)
        filtered_noise += 0.035 * (white_noise - filtered_noise)
        samples[index] = gain * (0.46 * math.tanh(1.6 * brass) + 0.018 * filtered_noise)

    # Small early reflections add a handmade hollow-horn body without a long tail.
    dry = samples[:]
    for delay_seconds, amount in ((0.031, 0.12), (0.057, 0.07)):
        delay = round(delay_seconds * SAMPLE_RATE)
        for index in range(delay, sample_count):
            samples[index] += dry[index - delay] * amount

    peak = max(abs(sample) for sample in samples) or 1.0
    scale = (10.0 ** (-1.0 / 20.0)) / peak
    pcm = b"".join(
        struct.pack("<h", round(max(-1.0, min(1.0, sample * scale)) * 32767.0))
        for sample in samples
    )

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUTPUT_PATH), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm)

    print(f"Generated {OUTPUT_PATH} ({DURATION_SECONDS:.2f}s, {SAMPLE_RATE} Hz, mono PCM16)")


if __name__ == "__main__":
    main()
