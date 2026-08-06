"""Create a brighter, shorter 'kiek!' variant from the approved cute death cry."""

from __future__ import annotations

import math
import struct
import wave
from pathlib import Path

import numpy as np
from scipy.signal import butter, resample_poly, sosfilt


SAMPLE_RATE = 48_000
ROOT = Path(__file__).resolve().parents[1]
SOURCE_PATH = ROOT / "Assets" / "Audio" / "SFX" / "goblin_death_common.wav"
OUTPUT_PATH = ROOT / "Assets" / "Audio" / "SFX" / "goblin_death_kiek.wav"


def read_mono_pcm16(path: Path) -> np.ndarray:
    with wave.open(str(path), "rb") as source:
        if source.getnchannels() != 1 or source.getsampwidth() != 2:
            raise RuntimeError(f"Expected mono PCM16 WAV: {path}")
        if source.getframerate() != SAMPLE_RATE:
            raise RuntimeError(f"Expected {SAMPLE_RATE} Hz WAV: {path}")
        data = source.readframes(source.getnframes())
    return np.frombuffer(data, dtype="<i2").astype(np.float64) / 32768.0


def main() -> None:
    source = read_mono_pcm16(SOURCE_PATH)

    # Raise the approved voice by about 2.6 semitones and tighten the syllable.
    voice = resample_poly(source, 86, 100)

    # A presence lift makes the opening vowel read closer to "ki" while keeping
    # the real recorded consonant and the friendly monster character.
    presence = sosfilt(
        butter(2, 1_450.0, btype="highpass", fs=SAMPLE_RATE, output="sos"),
        voice,
    )
    voice = voice * 0.90 + presence * 0.24

    # Layer a tiny voiced "i" flick only at the attack; it is deliberately
    # subtle so the result remains a creature voice rather than an instrument.
    attack_length = min(round(0.072 * SAMPLE_RATE), len(voice))
    attack_time = np.arange(attack_length, dtype=np.float64) / SAMPLE_RATE
    attack_progress = attack_time / max(attack_time[-1], 1e-9)
    frequency = 390.0 - 75.0 * attack_progress
    phase = 2.0 * math.pi * np.cumsum(frequency) / SAMPLE_RATE
    chirp = (
        np.sin(phase)
        + 0.22 * np.sin(2.0 * phase)
        + 0.08 * np.sin(3.0 * phase)
    )
    chirp_envelope = np.sin(np.linspace(0.0, math.pi, attack_length)) ** 2
    voice[:attack_length] += chirp * chirp_envelope * 0.045

    # Give the final "ek!" a tiny downward snap and soft pop.
    tail_length = min(round(0.080 * SAMPLE_RATE), len(voice))
    tail_time = np.arange(tail_length, dtype=np.float64) / SAMPLE_RATE
    pop_phase = 2.0 * math.pi * (205.0 * tail_time - 80.0 * tail_time**2)
    pop = np.sin(pop_phase) * np.exp(-tail_time / 0.021)
    voice[-tail_length:] += pop * 0.040

    fade_in = min(round(0.006 * SAMPLE_RATE), len(voice))
    fade_out = min(round(0.045 * SAMPLE_RATE), len(voice))
    voice[:fade_in] *= np.sin(np.linspace(0.0, math.pi / 2.0, fade_in)) ** 2
    voice[-fade_out:] *= np.cos(np.linspace(0.0, math.pi / 2.0, fade_out)) ** 2

    peak = max(float(np.max(np.abs(voice))), 1e-9)
    voice *= (10.0 ** (-1.0 / 20.0)) / peak
    pcm = b"".join(
        struct.pack("<h", round(max(-1.0, min(1.0, sample)) * 32767.0))
        for sample in voice
    )

    with wave.open(str(OUTPUT_PATH), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm)

    print(f"Generated {OUTPUT_PATH} ({len(voice) / SAMPLE_RATE:.2f}s)")


if __name__ == "__main__":
    main()
