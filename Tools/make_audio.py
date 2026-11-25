# Makes every sound and music loop used in Comet Run.
# I wrote my own tiny synth (sine / square / triangle / noise with simple envelopes)
# so the game doesn't need any downloaded audio at all.
#
# Run it from the project folder:   python Tools/make_audio.py
# It writes .wav files into Assets/CometRun/Audio (mono, 44.1 kHz, 16 bit).
#
# Needs numpy (pip install numpy).

import os
import wave
import numpy as np

RATE = 44100
OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "CometRun", "Audio"))
rng = np.random.default_rng(7)  # fixed seed so the noise sounds the same every time


# ---------- basic building blocks ----------

def t_axis(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def note_freq(n):
    # n = semitones away from A4 (440 Hz)
    return 440.0 * 2 ** (n / 12.0)


def osc(kind, freq, seconds):
    """freq can be a number or an array (for slides)."""
    t = t_axis(seconds)
    if np.isscalar(freq):
        phase = 2 * np.pi * freq * t
    else:
        phase = 2 * np.pi * np.cumsum(freq) / RATE
    if kind == "sine":
        return np.sin(phase)
    if kind == "square":
        return np.sign(np.sin(phase)) * 0.6
    if kind == "tri":
        return 2 / np.pi * np.arcsin(np.sin(phase))
    if kind == "noise":
        return rng.uniform(-1, 1, len(t))
    raise ValueError(kind)


def env(seconds, attack=0.005, release=0.05, curve=1.0):
    """attack ramp up, then fall off towards zero at the end"""
    n = int(RATE * seconds)
    e = np.ones(n)
    a = max(1, int(RATE * attack))
    r = max(1, int(RATE * release))
    e[:a] = np.linspace(0, 1, a)
    if r < n:
        e[-r:] *= np.linspace(1, 0, r)
    decay = np.linspace(1, 0, n) ** curve
    return e * (0.35 + 0.65 * decay)


def slide(f0, f1, seconds):
    return np.linspace(f0, f1, int(RATE * seconds))


def lowpass(x, amount=0.2):
    """very simple one pole filter, smaller amount = darker"""
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += amount * (v - acc)
        y[i] = acc
    return y


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def pad(x, seconds_before=0.0):
    return np.concatenate([np.zeros(int(RATE * seconds_before)), x])


def save(name, x, peak=0.8):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x))
    if m > 0:
        x = x / m * peak
    data = (x * 32767).astype(np.int16)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print("wrote", path, "%.2fs" % (len(x) / RATE))


def blip(kind, f, seconds, attack=0.003, release=0.03, curve=1.5):
    return osc(kind, f, seconds) * env(seconds, attack, release, curve)


# ---------- UI sounds ----------

def ui_sounds():
    save("ui_hover", blip("sine", note_freq(19), 0.04, release=0.02), 0.35)
    save("ui_click", mix(blip("square", note_freq(15), 0.06), 0.5 * blip("sine", note_freq(27), 0.05)), 0.6)
    save("ui_back", blip("square", slide(note_freq(12), note_freq(5), 0.09), 0.09), 0.55)
    save("ui_panel_open", lowpass(osc("noise", 0, 0.22) * env(0.22, 0.12, 0.08, 0.3), 0.15)
         + 0.4 * blip("sine", slide(note_freq(3), note_freq(15), 0.22), 0.22, 0.05, 0.08), 0.55)
    save("ui_panel_close", lowpass(osc("noise", 0, 0.2) * env(0.2, 0.01, 0.1, 1.5), 0.12)
         + 0.4 * blip("sine", slide(note_freq(15), note_freq(3), 0.2), 0.2, 0.01, 0.08), 0.5)
    # pause = two notes going down, resume = two notes going up
    save("ui_pause", mix(blip("tri", note_freq(7), 0.12), pad(blip("tri", note_freq(0), 0.18), 0.09)), 0.6)
    save("ui_resume", mix(blip("tri", note_freq(0), 0.1), pad(blip("tri", note_freq(7), 0.18), 0.08)), 0.6)
    # success = happy little major arpeggio
    notes = [3, 7, 10, 15]
    save("ui_success", mix(*[pad(blip("square", note_freq(n), 0.16, curve=2), i * 0.07) for i, n in enumerate(notes)]), 0.6)
    # error = low buzzy double beep
    save("ui_error", mix(blip("square", note_freq(-10), 0.1, curve=0.5), pad(blip("square", note_freq(-11), 0.14, curve=0.5), 0.12)), 0.55)
    save("ui_gem", mix(blip("sine", note_freq(22), 0.18, curve=2), 0.4 * blip("sine", note_freq(34), 0.12, curve=2)), 0.6)


# ---------- gameplay sounds ----------

def game_sounds():
    save("sfx_collect", mix(blip("square", slide(note_freq(10), note_freq(22), 0.1), 0.1),
                            0.5 * blip("sine", note_freq(29), 0.12, curve=2)), 0.65)
    save("sfx_boost", mix(*[pad(blip("tri", note_freq(n), 0.12), i * 0.05) for i, n in enumerate([12, 16, 19, 24, 28])]), 0.65)
    hit = lowpass(osc("noise", 0, 0.25) * env(0.25, 0.002, 0.1, 2.5), 0.35)
    thump = blip("sine", slide(note_freq(-15), note_freq(-30), 0.25), 0.25, curve=2)
    save("sfx_damage", mix(hit, thump), 0.8)
    boom = lowpass(osc("noise", 0, 1.0) * env(1.0, 0.005, 0.3, 1.8), 0.08)
    fall = blip("square", slide(note_freq(0), note_freq(-30), 0.9), 0.9, curve=1.2)
    save("sfx_death", mix(boom, 0.35 * fall), 0.85)
    save("sfx_tick", blip("sine", note_freq(24), 0.06, release=0.03, curve=3), 0.5)
    save("sfx_go", mix(blip("square", note_freq(15), 0.12), pad(blip("square", note_freq(27), 0.25, curve=1.5), 0.1)), 0.6)
    win_notes = [3, 7, 10, 15, 19, 22]
    save("sfx_win", mix(*[pad(blip("square", note_freq(n), 0.25, curve=1.5), i * 0.09) for i, n in enumerate(win_notes)]), 0.65)
    lose_notes = [7, 3, -2, -5]
    save("sfx_lose", mix(*[pad(blip("tri", note_freq(n), 0.3, curve=1.2), i * 0.16) for i, n in enumerate(lose_notes)]), 0.65)


# ---------- music loops ----------
# Both loops are made of whole bars and every note finishes inside its bar,
# so the end lines up with the start and the loop doesn't click.

CHORDS = {  # semitones from A4
    "Am": [-12, -9, -5],
    "F": [-16, -12, -9],
    "C": [-21, -17, -14],
    "G": [-14, -10, -7],
}


def place(track, sound, at):
    i = int(RATE * at)
    end = min(len(track), i + len(sound))
    track[i:end] += sound[:end - i]


def menu_music():
    bpm = 84
    beat = 60 / bpm
    bar = beat * 4
    order = ["Am", "F", "C", "G", "Am", "F", "G", "G"]
    total = bar * len(order)
    track = np.zeros(int(RATE * total))
    for b, name in enumerate(order):
        start = b * bar
        chord = CHORDS[name]
        # soft pad, slow attack and release, stays inside the bar
        for n in chord:
            place(track, 0.18 * osc("tri", note_freq(n), bar * 0.98) * env(bar * 0.98, 0.5, 0.6, 0.6), start)
        # bass note on beat 1 and 3
        for k in (0, 2):
            place(track, 0.3 * blip("sine", note_freq(chord[0] - 12), beat * 1.8, 0.01, 0.2, 1.0), start + k * beat)
        # gentle arpeggio in 8th notes
        arp = [chord[0] + 12, chord[1] + 12, chord[2] + 12, chord[1] + 12]
        for k in range(8):
            place(track, 0.12 * blip("sine", note_freq(arp[k % 4]), beat * 0.45, 0.005, 0.1, 2.0), start + k * beat / 2)
    save("music_menu", lowpass(track, 0.3), 0.6)


def game_music():
    bpm = 128
    beat = 60 / bpm
    bar = beat * 4
    order = ["Am", "Am", "F", "F", "C", "C", "G", "G"]
    total = bar * len(order)
    track = np.zeros(int(RATE * total))
    kick = blip("sine", slide(150, 45, 0.15), 0.15, 0.001, 0.05, 2.0)
    hat = lowpass(osc("noise", 0, 0.04), 0.9) * env(0.04, 0.001, 0.02, 3)
    snare = lowpass(osc("noise", 0, 0.14), 0.5) * env(0.14, 0.001, 0.06, 2)
    for b, name in enumerate(order):
        start = b * bar
        chord = CHORDS[name]
        for k in range(4):
            place(track, 0.55 * kick, start + k * beat)
        for k in (1, 3):
            place(track, 0.25 * snare, start + k * beat)
        for k in range(8):
            place(track, 0.08 * hat, start + k * beat / 2 + beat / 4)
        # driving bass in 8th notes
        for k in range(8):
            n = chord[0] - 12 if k % 2 == 0 else chord[0]
            place(track, 0.16 * blip("square", note_freq(n), beat * 0.45, 0.003, 0.05, 1.2), start + k * beat / 2)
        # fast arpeggio lead in 16th notes
        arp = [chord[0] + 12, chord[1] + 12, chord[2] + 12, chord[0] + 24]
        for k in range(16):
            place(track, 0.07 * blip("square", note_freq(arp[k % 4]), beat * 0.22, 0.002, 0.04, 1.8), start + k * beat / 4)
    save("music_game", lowpass(track, 0.45), 0.6)


if __name__ == "__main__":
    ui_sounds()
    game_sounds()
    menu_music()
    game_music()
