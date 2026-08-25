# Implementation Plan — Music Analysis Engine v2

Derived from `spec2_music_analysis_engine.md`. **Plan for approval — no code yet.** We execute
phase by phase, each with a verification you can run.

---

## Guiding principles (from the spec)

1. **The analyzer describes the music; it never decides what the game does with it.** No gameplay
   logic in `AudioAnalyzer`. Everything flows through `MusicState`.
2. **Separate three concepts that the current code conflates:** *energy* (how loud), *onset*
   (something started), *beat* (the rhythmic pulse). Different detectors, different signals.
3. **Continuous vs. event signals are processed differently.** Continuous features may be smoothed
   (attack/release); event signals (onsets/beats) must stay sharp and low-latency.
4. **Keep raw and processed side by side** — never overwrite raw with a smoothed value.
5. **Backward compatible throughout.** `Bass/Mid/Treble/Energy/Beat` stay on `MusicState` (now
   *derived* from the richer representation), so the existing feedback/kaleidoscope visualizer keeps
   running while we build. We only re-point it at the new features in Phase G.

---

## Architecture

`AudioAnalyzer` becomes a thin **orchestrator** that runs a set of focused DSP modules each update
and writes results into `MusicState`. New files under `Assets/Scripts/Audio/`:

```
AudioAnalyzer.cs        orchestrator (owns the pipeline, writes MusicState)
Analysis/
  SpectrumProvider.cs   FFT access (configurable size) + windowed magnitude spectrum
  FrequencyBands.cs     log-spaced band mapping (config boundaries) → band energies
  SpectralFeatures.cs   centroid, flux, (optional) contrast
  EnvelopeFollower.cs   reusable attack/release smoother
  OnsetDetector.cs      flux → onset strength, adaptive threshold + peak-pick + refractory
  BeatTracker.cs        onset envelope → tempo (BPM), beat events, beat phase
  Chroma.cs             12-bin pitch-class energy
MusicState.cs           v2 data (all fields below)
MusicAnalysisConfig.cs  ScriptableObject: FFT size, band edges, sensitivities, attack/release, etc.
```

**MusicState v2** (superset of today — old fields remain, now derived):

```csharp
// Continuous — spectral
float OverallEnergy;          // stable-ish overall intensity
float Bass, Mid, Treble;      // derived convenience (from bands), enveloped
float SpectralCentroid;       // 0..1 brightness
float SpectralFlux;           // positive spectral change
float SpectralContrast;       // optional
float[] FrequencyBands;       // ~10 log bands, adaptive-normalized
float[] FrequencyBandsRaw;    // stable/raw version

// Onsets
float OnsetStrength, BassOnsetStrength, MidOnsetStrength, TrebleOnsetStrength;
bool  Onset, BassOnset, MidOnset, TrebleOnset;   // one-frame events

// Rhythm
float BPM;                    // slow-updating
float BeatPhase;              // 0..1 between beats
bool  Beat;                   // event
double LastBeatDspTime;       // audio-clock timestamp

// Tonal
float[] Chroma;               // 12 pitch classes (best-effort)
```

**Timing:** event timestamps use `AudioSettings.dspTime` (not `Time.time`), per §17, so beats/onsets
carry an audio-accurate time and an offline `TrackAnalysis` stays possible later (§25).

### Key technical decisions (I'll validate these in Phase A)

- **FFT size:** default **1024** (bin ≈ 23 Hz, window ≈ 21 ms) as the balance of low-end resolution
  vs. transient responsiveness; **configurable**, and we'll A/B against 2048 during Phase A. (2048
  resolves the 20–80 Hz bands better but blurs transients more — the spec explicitly warns bigger
  isn't automatically better.)
- **Source:** keep Unity's `GetSpectrumData` (already wired through `IMusicProvider`) — no custom FFT
  needed, no new dependency.
- **Cadence:** per-frame for v1 (simple, per §18), but flux/onset use `dspTime` deltas so timing is
  frame-rate independent; we can move to a fixed audio-thread cadence later if profiling demands.
- **Normalization:** each band exposes **raw** (slow/stable reference) and **adaptive** (fast peak
  follower) — satisfying §7.1 and §2.4. Onsets use flux (change), not level thresholds.

### Debug-UI strategy (important)

The spec puts the debug dashboard at Phase 6, but also says it's how we *verify features against the
music*. So I'll **grow the debug overlay incrementally** — each feature phase adds its readout, and
Phase F consolidates everything into the full scrolling dashboard. That way every phase below has a
real, visual verification you can do while listening.

---

## Phases & verification

Each phase: what I build, then **✅ how you verify** (mostly: play the track, watch the debug overlay,
confirm against what you hear). The existing visualizer keeps working the whole time.

### Phase A — Refactor + richer bands + bigger FFT  *(spec Dev 1)*
- Introduce `MusicAnalysisConfig`, `SpectrumProvider` (configurable FFT), `FrequencyBands`
  (~10 log-spaced bands).
- Rewrite `AudioAnalyzer` as the orchestrator; `MusicState` v2 (new fields present, populated as we
  go). Derive `Bass/Mid/Treble/Energy` from the bands so the current visualizer is unaffected.
- Extend the debug overlay to draw the ~10 band bars.
- **✅ Verify:** F1 shows ~10 band bars that track the spectrum (low bands move on bass, high bands on
  cymbals). The existing kaleidoscope visualizer looks unchanged. No Console errors. We compare 1024
  vs 2048 together and lock the default.

### Phase B — Continuous spectral features  *(spec Dev 2)*
- Band energy (raw + adaptive), `OverallEnergy` (stable), `SpectralCentroid` (0–1),
  `SpectralFlux`, optional `SpectralContrast`.
- `EnvelopeFollower` (attack/release) applied to Bass/Mid/Treble/Energy — **separate from raw**.
- Debug adds Energy / Brightness / Flux bars (and raw-vs-enveloped for one band).
- **✅ Verify:** brightness bar is low on bass-heavy parts, high on hats/cymbals; flux spikes on
  changes and sits low on sustained sounds; the enveloped bass snaps up fast and eases down slowly
  vs. the raw bass.

### Phase C — Onset detection  *(spec Dev 3)*  ← core priority
- Global `OnsetStrength` + `Onset` from spectral flux with **adaptive threshold + local peak-picking
  + refractory**; per-band Bass/Mid/Treble onset strengths + events. Configurable sensitivity.
- Debug adds onset indicators (global/bass/mid/treble flash) + onset-strength bars.
- **✅ Verify:** while listening, **bass-onset flashes on kicks, treble-onset on hi-hats, mid-onset on
  snares/vocal hits**; sustained pads do **not** machine-gun false onsets; feels immediate, not late.

### Phase D — Rhythm: tempo, beat, phase  *(spec Dev 4)*
- `BPM` from autocorrelation of the onset-strength envelope over a few seconds (slow-updating, clamped
  to a sane BPM range); beat events phase-locked to tempo+onsets; `BeatPhase` 0→1; `dspTime` stamps.
- Debug shows the BPM number, a beat dot, and a sweeping beat-phase bar (metronome).
- **✅ Verify:** BPM settles near the track's real tempo and doesn't jump around; the beat dot lands on
  the pulse when you tap along; beat-phase sweeps smoothly 0→1 between beats. Clearly better than the
  old `bass > avg×1.4` detector.

### Phase E — Tonal: chroma  *(spec Dev 5, best-effort)*
- `Chroma[12]` by folding FFT bins into pitch classes across octaves.
- Debug adds 12 chroma bars (labeled C…B).
- **✅ Verify:** sustained notes/chords light plausible pitch classes and shift on chord changes.
  (Accepted as best-effort per §23; we won't over-invest if it's disproportionately hard.)

### Phase F — Full debug dashboard  *(spec Dev 6)*
- Consolidate into the spec's dashboard (§19): band bars, energy/brightness/flux, BPM, event dots
  (beat/onset/bass/mid/treble), **scrolling graphs** over the last few seconds. Toggle key.
- **✅ Verify:** you can load a song and *just from the dashboard* judge whether kicks/snares are
  caught, whether sustains cause false onsets, and whether beats are consistent — across 2+ songs.

### Phase G — Visualizer integration: the player circle demo  *(spec Dev 7)*
- Add a **persistent circle around the ship** driven by the new features (mappings live in the
  visualizer, **not** the analyzer):
  - Bass **energy** → baseline radius (continuous breathing)
  - Bass **onset** → sharp pulse
  - **Beat** → stronger synchronized pulse
  - Spectral **brightness** → hue/color
  - **Overall energy** → intensity (glow)
  - **Treble onset** → small spark event
- Retire the current bass-threshold shockwave trigger in favor of these; feedback/kaleidoscope can
  also read the cleaner energy/onset signals.
- **✅ Verify (acceptance):** the circle breathes with bass, pops crisply on bass onsets, gives a
  stronger hit on beats, shifts color with brightness, intensifies with energy, and sparks on treble —
  and the sync finally feels **tight**, not loose.

### Phase H — Tune across multiple songs  *(spec Dev 8)*
- Test with substantially different tracks (electronic, rock/instrumental, strong vocals, quiet
  intro/build, strong drums, weak percussion); tune sensitivities so it isn't overfit to one song.
- **✅ Verify:** onsets/beats/BPM behave reasonably across all of them; no single-song overfitting.

---

## Risks / watch-items
- **Transient vs. bass-resolution tension** in FFT size — addressed by making it configurable and
  A/B-ing in Phase A.
- **Beat tracking is the hardest module** — v1 aims for "clearly better than threshold," not perfect;
  autocorrelation-based tempo + phase tracking is the plan, refined in Phase H.
- **Chroma** is explicitly best-effort; may be simplified if costly.
- **Per-frame cadence** at low FPS could coarsen onsets — mitigated by dspTime-aware deltas; audio-
  thread cadence deferred unless profiling shows it's needed.

## Resolved decisions
1. **Tonal scope:** include **both chroma[12] and spectral contrast**, best-effort (simplify if they
   get disproportionately hard, per §23).
2. **Test tracks:** user will add ~4–6 varied tracks to `Assets/Audio/Tracks` **before Phase H**;
   Phases A–G are verified against the current track (Tame Impala — Loser).
