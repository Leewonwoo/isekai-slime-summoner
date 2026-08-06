"""Turn a dry vocal reference into the shared in-game goblin death cry."""

from __future__ import annotations

import math
import random
import struct
import sys
import wave
from pathlib import Path

import av
import numpy as np
from scipy.signal import butter, resample_poly, sosfilt


SAMPLE_RATE = 48_000
OUTPUT_PATH = (
    Path(__file__).resolve().parents[1]
    / "Assets"
    / "Audio"
    / "SFX"
    / "goblin_death_common.wav"
)


def decode_mono(path: Path) -> np.ndarray:
    container = av.open(str(path))
    stream = container.streams.audio[0]
    chunks: list[np.ndarray] = []
    input_rate = SAMPLE_RATE

    for frame in container.decode(stream):
        samples = frame.to_ndarray()
        if samples.ndim == 2:
            samples = samples.mean(axis=0)
        if np.issubdtype(samples.dtype, np.integer):
            samples = samples.astype(np.float32) / np.iinfo(samples.dtype).max
        else:
            samples = samples.astype(np.float32)
        chunks.append(samples)
        input_rate = frame.sample_rate

    if not chunks:
        raise RuntimeError(f"No audio frames decoded from {path}")

    audio = np.concatenate(chunks)
    if input_rate != SAMPLE_RATE:
        audio = resample_poly(audio, SAMPLE_RATE, input_rate)
    return audio.astype(np.float64)


def find_strongest_take(audio: np.ndarray) -> tuple[int, int]:
    window = round(0.025 * SAMPLE_RATE)
    hop = round(0.010 * SAMPLE_RATE)
    rms = np.array(
        [
            math.sqrt(float(np.mean(audio[index : index + window] ** 2)) + 1e-12)
            for index in range(0, len(audio) - window, hop)
        ]
    )
    noise_floor = float(np.percentile(rms, 20))
    threshold = max(noise_floor * 4.0, float(np.max(rms)) * 0.07)
    active = rms > threshold

    # Fill gaps shorter than 120 ms so a single spoken take remains contiguous.
    max_gap = round(0.12 / 0.01)
    true_indices = np.flatnonzero(active)
    for left, right in zip(true_indices[:-1], true_indices[1:]):
        if right - left <= max_gap:
            active[left : right + 1] = True

    candidates: list[tuple[float, int, int]] = []
    start = None
    for index, enabled in enumerate(active):
        if enabled and start is None:
            start = index
        if start is not None and (not enabled or index == len(active) - 1):
            end = index if not enabled else index + 1
            duration = (end - start) * hop / SAMPLE_RATE
            if duration >= 0.10:
                score = float(np.sum(rms[start:end]))
                candidates.append((score, start * hop, end * hop + window))
            start = None

    if not candidates:
        raise RuntimeError("No usable vocal take was detected.")

    _, start_sample, end_sample = max(candidates)
    pre_roll = round(0.035 * SAMPLE_RATE)
    post_roll = round(0.070 * SAMPLE_RATE)
    return max(0, start_sample - pre_roll), min(len(audio), end_sample + post_roll)


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("Usage: process_goblin_death_recording.py <reference.m4a>")

    input_path = Path(sys.argv[1]).expanduser().resolve()
    audio = decode_mono(input_path)
    audio -= float(np.mean(audio))
    start, end = find_strongest_take(audio)
    take = audio[start:end]

    # Remove room rumble and phone-mic hiss before character processing.
    filters = butter(
        3,
        [105.0, 7_200.0],
        btype="bandpass",
        fs=SAMPLE_RATE,
        output="sos",
    )
    take = sosfilt(filters, take)
    take /= max(float(np.max(np.abs(take))), 1e-9)

    # A modest speed-up raises pitch and formants by about 1.6 semitones. This
    # keeps the recorded consonants recognizable while giving the death cry the
    # small, friendly monster character requested for the casual game.
    processed = resample_poly(take, 91, 100)

    random.seed(20260728)
    time = np.arange(len(processed), dtype=np.float64) / SAMPLE_RATE
    peak_envelope = np.maximum.accumulate(np.abs(processed))
    reversed_peak = np.maximum.accumulate(np.abs(processed)[::-1])[::-1]
    activity = np.minimum(1.0, np.minimum(peak_envelope, reversed_peak) * 5.0)

    # Gentle saturation and a tiny wobble make the voice cartoony without
    # turning it into the harsh rasp or brass-like timbre of the earlier drafts.
    saturated = np.tanh(processed * 2.0)
    cute_wobble = 0.975 + 0.025 * np.sin(2.0 * math.pi * 10.5 * time)
    noise = np.array([random.uniform(-1.0, 1.0) for _ in processed])
    noise = sosfilt(
        butter(2, [1_400.0, 6_500.0], btype="bandpass", fs=SAMPLE_RATE, output="sos"),
        noise,
    )
    processed = (
        processed * 0.86
        + saturated * cute_wobble * 0.11
        + noise * activity * 0.006
    )

    # Add a tiny soft cartoon "poof" under the ending. It reads as defeat
    # feedback while remaining quieter than the recorded vocal.
    poof_start = max(0, len(processed) - round(0.115 * SAMPLE_RATE))
    poof_time = np.arange(len(processed) - poof_start, dtype=np.float64) / SAMPLE_RATE
    poof_phase = 2.0 * math.pi * (
        175.0 * poof_time - 55.0 * poof_time * poof_time
    )
    poof = np.sin(poof_phase) * np.exp(-poof_time / 0.032)
    processed[poof_start:] += poof * 0.055

    # One compact reflection gives the cry body without an ambient tail.
    dry = processed.copy()
    for delay_seconds, amount in ((0.021, 0.045),):
        delay = round(delay_seconds * SAMPLE_RATE)
        processed[delay:] += dry[:-delay] * amount

    fade_in = min(round(0.009 * SAMPLE_RATE), len(processed))
    fade_out = min(round(0.060 * SAMPLE_RATE), len(processed))
    processed[:fade_in] *= np.sin(np.linspace(0.0, math.pi / 2.0, fade_in)) ** 2
    processed[-fade_out:] *= np.cos(np.linspace(0.0, math.pi / 2.0, fade_out)) ** 2

    peak = max(float(np.max(np.abs(processed))), 1e-9)
    processed *= (10.0 ** (-1.0 / 20.0)) / peak
    pcm = b"".join(
        struct.pack("<h", round(max(-1.0, min(1.0, sample)) * 32767.0))
        for sample in processed
    )

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUTPUT_PATH), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm)

    print(
        f"Processed {input_path.name}: source {start / SAMPLE_RATE:.3f}-"
        f"{end / SAMPLE_RATE:.3f}s -> {OUTPUT_PATH} "
        f"({len(processed) / SAMPLE_RATE:.2f}s)"
    )


if __name__ == "__main__":
    main()
