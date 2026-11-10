# SPDX-License-Identifier: MIT; see assets/audio/LICENSE.txt
"""Original, deterministic short cues. Standard library only; no sampled recordings."""
from pathlib import Path
import math
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
RATE = 22050
CUES = {
    "new-day": [(0, 0.24, 392), (0.16, 0.30, 523.25)],
    "at-the-door": [(0, 0.09, 620), (0.17, 0.09, 620)],
    "support-down": [(0, 0.26, 311.13), (0.23, 0.35, 233.08)],
    "award-made": [(0, 0.16, 440), (0.12, 0.24, 659.25)],
}


def generate(directory=ROOT / "assets" / "audio"):
    directory.mkdir(parents=True, exist_ok=True)
    for name, notes in CUES.items():
        length = int((max(start + duration for start, duration, _ in notes) + 0.03) * RATE)
        samples = []
        for n in range(length):
            t = n / RATE
            value = 0
            for start, duration, frequency in notes:
                local = t - start
                if 0 <= local < duration:
                    envelope = min(1, local / 0.012) * (1 - local / duration) ** 2
                    value += 0.22 * envelope * (math.sin(2 * math.pi * frequency * local)
                                              + 0.15 * math.sin(4 * math.pi * frequency * local))
            samples.append(round(max(-1, min(1, value)) * 32767))
        with wave.open(str(directory / (name + ".wav")), "wb") as output:
            output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
            output.writeframes(struct.pack("<" + "h" * len(samples), *samples))


if __name__ == "__main__":
    generate()
