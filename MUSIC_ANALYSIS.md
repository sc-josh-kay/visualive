# Music Analysis & the Player Smoke — Deep Dive

How the game listens to the music, what it extracts, and how those signals become the living smoke the player paints with. For the whole-game picture see [`README.md`](README.md); this doc drills into `Assets/Scripts/Audio/` and the `SmokeField` visualizer engine.

---

## 1. Architecture in one breath

```
AudioClip ─▶ LocalClipMusicProvider ─▶ AudioAnalyzer (the ONLY FFT owner)
                                             │  fills every frame
                                             ▼
                                        MusicState  (shared read-only snapshot)
                                       /            \
                             MusicMapper             Visuals (smoke, enemies,
                          (gameplay coupling)         weapons) read it directly
```

- **One analyzer.** `AudioAnalyzer` is the only component that runs an FFT. Everyone else reads the `MusicState` snapshot; nobody else touches audio. `AudioAnalyzer.Instance` is a static accessor so cheap visuals can read it without a scene lookup.
- **`MusicState` is a mutating snapshot** — sample the fields you need at call time, don't retain the reference.
- **Continuous vs. event signals are kept distinct.** Continuous features (Bass, Energy, Centroid…) are smoothed and safe to read every frame; event flags (`Beat`, `*Onset`) are **true for exactly one frame** and never smoothed.

Everything below runs once per frame in `AudioAnalyzer.Update()`, in this order.

---

## 2. The spectrum (FFT) — `SpectrumProvider`

The raw material is a magnitude spectrum from Unity's built-in FFT:

```
provider.GetSpectrumData(spectrum, channel:0, window:BlackmanHarris)
```

- **Size** = power of two (default **1024** bins), so each frame we get 1024 magnitudes covering `0 … SampleRate/2`.
- **Window** = Blackman–Harris (default) — tapers each analysis block so frequency leakage between bins is small (sharper, cleaner bins than a rectangular window).
- **Bin → frequency:** `BinHz = (SampleRate/2) / Size`. At 44.1 kHz / 1024 that's ~21.5 Hz per bin. Bin `i` is centered at `i · BinHz`.

Every feature below is just a different way of collapsing these 1024 numbers into something musically meaningful.

---

## 3. Frequency representation

### 3a. Log-spaced bands — `FrequencyBands`
Human hearing is logarithmic, so the spectrum is folded into ~10 **log-spaced bands** (default edges `20, 40, 80, 160, 320, 640, 1250, 2500, 5000, 10000, 20000 Hz`). Each band sums the FFT magnitudes whose bins fall in its `[loHz, hiHz)` range:

```
raw[b] = Σ spectrum[i]   for i in band b
```

### 3b. Adaptive normalization (used everywhere)
Absolute FFT magnitudes vary wildly by track/master level, so almost every signal is normalized against a **decaying peak follower** — a running "loudest I've seen recently":

```
peak = max(value, peak − decay · peak · dt)      // peak rises instantly, decays slowly
normalized = clamp01(value / peak)               // 0..1, self-calibrating per song
```

This is why a quiet song and a loud song both produce full-range 0..1 signals: the analyzer continuously re-scales to the music's own dynamic range (`NormalizeDecay`, default 0.5/s).

### 3c. Convenience bands — Bass / Mid / Treble / Energy
Three coarse bands are summed directly over Hz ranges (defaults: Bass `<150 Hz`, Mid `150–2000 Hz`, Treble `2000–8000 Hz`), `Energy = Bass+Mid+Treble`. Each is normalized (3b) and then passed through an **attack/release envelope** so it reacts like a musical signal rather than a raw meter.

---

## 4. Attack/release envelope — `EnvelopeFollower`

A symmetric smoother both *delays* a hit and *rounds it off*. Instead we use an **asymmetric one-pole**: fast up, slow down.

```
tc    = (target > value) ? attack : release        // attack≈0.03s, release≈0.25s
alpha = 1 − exp(−dt / tc)                          // frame-rate independent
value += (target − value) · alpha
```

So `Bass` snaps up on a kick and then falls away naturally — immediate response, musical tail. `Bass`/`Mid`/`Treble`/`Energy` are all enveloped; `BassInstant` keeps the pre-envelope value for consumers that want the raw hit.

---

## 5. Continuous spectral features — `SpectralFeatures`

**Spectral Centroid (brightness), 0..1.** The magnitude-weighted mean frequency — the spectrum's "balance point." High when a hi-hat/cymbal dominates, low when a bass drone does:

```
centroidHz = Σ(i·BinHz · mag[i]) / Σ mag[i]
Centroid01 = (log centroidHz − log 50) / (log(SR/2) − log 50)     // log-mapped to 0..1
```

This is the game's main **hue** driver.

**Spectral Flux (change).** Sum of *positive* bin-to-bin increases since the last frame — "how much new energy just appeared," the basis for onsets. Computed globally and split into bass/mid/treble in the same pass:

```
flux = Σ max(0, spectrum[i] − prev[i])
```

Then normalized (3b) to `SpectralFlux` (0..1). This is the **treble/change** signal the Swarm and Splitter react to.

**Spectral Contrast.** Rough peak-vs-mean across log chunks — a "peaky vs. flat" descriptor (best-effort, lightly used).

---

## 6. Onset detection — `OnsetDetector`

An onset is *"a sound just started,"* not *"it's currently loud."* We threshold the flux with a **sliding-window adaptive threshold** and fire on the rising edge:

```
threshold = mean(flux) + sensitivity · std(flux)      // over the last ~43 frames
onset = (flux > threshold) && (prevFlux ≤ threshold)  // rising-edge crossing
        && (time − lastOnset > refractory)            // rate-limit (~0.06s)
```

Because the threshold is `mean + k·std` over a moving window, it **auto-adapts to each song's dynamics** — a busy section raises the bar so we don't fire on every hi-hat. `Strength` is a continuous 0..1 flux (for smooth reactions); `Onset` is the one-frame event. Run four times: global / bass / mid / treble.

---

## 7. Tempo & beat — `BeatTracker`

Deliberately separate from onsets (onset = "a sound started"; beat = "the rhythmic pulse"):

1. **Onset envelope (ODF):** resample the onset strength into a fixed-rate, peak-held buffer using the **audio DSP clock** (`AudioSettings.dspTime`), so lags map to real seconds regardless of frame rate (default 90 Hz).
2. **Tempo via autocorrelation:** periodically correlate the ODF with delayed copies of itself over the plausible BPM range (70–180). The lag with the strongest correlation is the beat period → `BPM = 60·odfRate / bestLag`. A mild inertia weight favors lags near the current BPM (so it doesn't hop between half/double time), and the result is slewed slowly.
3. **Phase-locked loop:** a phase accumulator advances at `BPM/60` per second; each onset nudges the phase toward the nearest beat. `Beat` fires when the phase wraps past 1. `BeatPhase` (0..1) is the position between beats.

v1 is intentionally simple and can occasionally lock to a tempo octave.

---

## 8. Tonal — `Chroma`

Folds the spectrum into **12 pitch classes** (C…B) by mapping each bin to its nearest MIDI note and summing across octaves, then max-normalizing + smoothing:

```
midi = 69 + 12·log2(f / 440)      // A4=440Hz → MIDI 69
pitchClass = round(midi) mod 12
```

A coarse tonal descriptor for color/geometry — not source separation.

---

## 9. What `MusicState` exposes

| Category | Fields | Kind |
|---|---|---|
| Intensity | `Energy` | continuous |
| Bands | `Bass` `Mid` `Treble` (+`BassInstant`) | continuous, enveloped |
| Rich bands | `FrequencyBands[]` (norm), `FrequencyBandsRaw[]` | continuous |
| Spectral | `SpectralCentroid` (brightness), `SpectralFlux` (change), `SpectralContrast` | continuous 0..1 |
| Onsets | `Onset` `BassOnset` `MidOnset` `TrebleOnset` + `*Strength` | **event** + continuous |
| Rhythm | `Beat` (event), `BeatPhase`, `BPM`, `LastBeatDspTime` | event + continuous |
| Tonal | `Chroma[12]` | continuous |

---

## 10. How the game uses it

Two paths, kept strictly separate (spec7 §9 — *music changes how things look/feel, never what the player can't read*):

- **Gameplay coupling is centralized in `MusicMapper`.** The only place music alters *gameplay*: song-progress intensity curve → spawn rate + enemy cap + type unlocks; Energy → spawn rate; smoothed per-channel levels (Bass/Treble/Flux) → spawn *composition* (bass-heavy → Corruptors, treble-heavy → Swarms). Toggle with `M`.
- **Visuals read `MusicState` directly.** Enemies (Color Eater←Energy, Corruptor←Bass, Swarm/Splitter←Treble/Flux, Dasher←Beat), weapons (Centroid→hue, Energy→brightness), and the **player smoke** — the centerpiece below.

---

## 11. Deep dive — the player smoke engine

The smoke the player trails is a **persistent GPU feedback field**, and that same field *is the coverage surface the game scores* (`README.md` pillar 1). Owned by `KaleidoscopePattern` + `SmokeField.shader`.

### 11a. The feedback loop (per frame)
Two ping-ponged RenderTextures (`RGB111110Float`, ~half-screen res). Each frame:

```
1. advect + fade + emit    (SmokeField.shader):  src ─▶ dst
2. apply gameplay splats   (FieldSplat.shader):  enemy consume / death paint / overdrive
3. display                 (Kaleidoscope pass):  dst ─▶ screen (+ ripples)
```

The field starts black; the player paints it in, and it **persists and slowly decays** — the whole look is one image feeding back into itself, nudged by the music every frame.

### 11b. `SmokeField.shader` — the three operations
For each pixel `p` (relative to the player, aspect-corrected):

**Advect** — resample the previous frame from a slightly offset position, so content appears to *flow outward from the player* and curl:

```
offset = (−Flow·p  +  tangential·Swirl  +  wave·Warp) · DtScale
prev   = tex2D(field, uv + offset)
```

`−Flow·p` pushes content radially outward (sampling inward toward the player); `Swirl` adds tangential rotation; `Warp` is a wavy turbulence. `DtScale` (= `dt × 60`) makes the per-frame step integrate to the same motion per *second* at any frame rate — so the 30 fps thermal cap looks identical to 60 (fade uses an exponent, advection/emit a linear scale).

**Fade** — persistence, applied as a per-second-correct exponent so stillness/decay are frame-rate independent:

```
prev *= Fade^DtScale      // Fade≈0.99 ≈ multi-second tail; drops toward ~0.90 when the player isn't painting
```

**Emit** — add fresh light at the player: a soft **blob** (the continuous trail), a **ring pop** (bass onset / beat), and a **treble sparkle**:

```
blob    = smoothstep(BlobRadius, 0, d)
ring    = smoothstep(RingWidth, 0, |d − RingRadius|)
sparkle = Treble · (0.5 + 0.5·sin(d·120 − t·6))
emit    = (blob·BaseStrength + ring·PopStrength + blob·sparkle) · DtScale
result  = prev + EmitColor·emit
```

(The same advection pass also folds in Corruptor **vacuums** and Swarm **turbulence** zones — capped per-point field ops that pull/eat/tear the smoke — but the player trail is the blob+ring+sparkle emission above.)

### 11c. Music → smoke mapping (`KaleidoscopePattern.UpdatePattern`)
Every uniform above is driven by `MusicState`:

| Music signal | Smoke parameter | Effect |
|---|---|---|
| **Energy** | `Flow` (Lerp Min→Max), `Fade` (longer at high energy) | busier music → faster, more persistent flow |
| **Bass** | `BlobRadius` / `RingRadius` (Lerp Min→Max) | bass swells the emitted blob & ring |
| **Bass onset / Beat** | `PopStrength` (a decaying pop envelope) | kicks/beats fire an outward ring pop |
| **Treble** | `_Treble` sparkle amount | highs add fine shimmering detail |
| **Spectral Centroid** | emit **hue** (`HSV(centroid·0.5 + drift)`) | brighter timbre → hue shifts; the smoke is colored by the music |
| **(gameplay) PlayerPaintGain** | `BaseStrength`, `PopStrength`, and the `Fade` floor | movement-gated: a still/just-hit player paints little and the field actively dissipates |

`PlayerPaintGain` (0..1) is the one gameplay lever into the smoke — written by `PlayerTrail` from movement speed and enemy-hit disruption. It scales emission *and* couples the fade toward `DissipateFade`, which is what makes stopping or getting hit **visibly** clear the world (emission-scaling alone can't, because the field lingers for seconds).

### 11d. Why it doubles as the score surface
`CoverageSampler` GPU-downsamples this field to a small grid and reads it back asynchronously to compute **coverage** (fraction of cells above a luminance threshold) + the painted-mass centroid enemies target. Score accrues from coverage², so "keeping the music beautiful" and "keeping the screen painted" are literally the same quantity.

---

## 12. Key files & tuning

**Analysis** (`Assets/Scripts/Audio/`)
- `AudioAnalyzer.cs` — the per-frame pipeline orchestrator.
- `Analysis/` — `SpectrumProvider`, `FrequencyBands`, `SpectralFeatures`, `EnvelopeFollower`, `OnsetDetector`, `BeatTracker`, `Chroma`.
- `MusicState.cs` — the shared snapshot. `MusicAnalysisConfig.cs` (+ `.asset`) — all tunables: FFT size/window, band edges, Bass/Mid/Treble crossovers, envelope attack/release, normalize decay, onset sensitivity/window/refractory, BPM range, ODF rate, chroma range.
- `LocalClipMusicProvider.cs` — the `IMusicProvider` (clip playback + `GetSpectrumData`). `SongLibrary.cs` — selectable tracks (refresh via `PlayVisualizer → Refresh Song List`).

**Smoke** (`Assets/Scripts/Visuals/`, `Assets/Shaders/`)
- `KaleidoscopePattern.cs` — maps `MusicState` → shader uniforms, owns the ping-pong field.
- `SmokeField.shader` — advect/fade/emit (+ vacuum/turbulence). `SmokePatternParams` (on `VisualizerCore`) — flow, fade, blob/ring, pop, sparkle, brightness.
- `CoverageSampler.cs` — coverage + hot-point readback. `VisualizerField.cs` — the single gameplay↔visual seam (`Paint`/`Consume`/`Vacuum`/`Turbulence`/`PlayerPaintGain`/`Coverage`).

---

*Guiding rule: music changes how things **look and feel** — it never randomizes aim, fire timing, or enemy targeting. The analyzer turns sound into a vocabulary of readable signals; the smoke turns that vocabulary back into something the player paints the song with.*
