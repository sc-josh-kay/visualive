# Music Analysis Engine v2

## Purpose

This document specifies the next iteration of the music-analysis engine for the Unity music-driven arcade game.

The current system successfully extracts basic:

* Bass
* Mid
* Treble
* Energy
* Beat

signals from a real-time FFT and exposes them through a shared `MusicState`.

However, the current approach is too simplistic for the direction of the game.

The immediate goal is **not** to build more gameplay.

The goal is to build a substantially better **general-purpose music analysis engine** that can provide rich, musically meaningful signals to the game and visualizer.

Once the engine is working, we will demonstrate it by integrating the new signals into the existing player/circle visualizer and gameplay features.

---

# 1. Current System

The existing `AudioAnalyzer.cs` currently:

1. Calls Unity `GetSpectrumData()` every frame.
2. Uses a 512-bin FFT.
3. Splits the spectrum into:

   * Bass: 0–150 Hz
   * Mid: 150–2000 Hz
   * Treble: 2000–8000 Hz
4. Sums each frequency range.
5. Normalizes each band against a decaying peak follower.
6. Smooths the results with `Lerp`.
7. Detects beats using a bass threshold relative to a running average.
8. Publishes:

```text
MusicState
├── Bass
├── Mid
├── Treble
├── Energy
└── Beat
```

The existing system should be treated as the starting point, not discarded blindly.

---

# 2. Problems With the Current System

## 2.1 Beat detection is too simplistic

The current detector effectively asks:

> "Is bass currently loud compared with its recent average?"

This causes several problems:

* Some obvious musical hits are missed.
* Sustained bass can repeatedly look like a beat.
* Adaptive normalization changes what a fixed threshold means.
* Different songs behave very differently.
* Beat timing can feel soft or late.
* A musical onset is incorrectly assumed to be a beat.

We need to distinguish:

**energy**, **onset**, and **beat**.

They are different concepts.

---

## 2.2 Smoothing adds unwanted latency

The current smoothing:

```csharp
State.X = Mathf.Lerp(State.X, normalized, 12f * dt);
```

is useful for continuous visual motion but inappropriate for event detection.

A sharp transient should be detected immediately.

Do not apply the same smoothing pipeline to:

* continuous energy values
* onset detection
* beat detection

---

## 2.3 Three frequency bands are too coarse

Bass/mid/treble are useful high-level features, but they throw away a lot of information.

The game should eventually be able to distinguish different types of musical activity occurring at different frequency ranges.

The engine should therefore retain a richer frequency representation internally.

---

## 2.4 Relative normalization hides song-level dynamics

The current peak follower makes the values visually lively, but means that:

> "high"

really means:

> "high relative to recent history."

That is useful for visual effects but insufficient for understanding the structure and intensity of an entire song.

The engine should retain both relative and more stable representations where useful.

---

# 3. Design Goal

The new engine should answer questions such as:

### What is happening right now?

```text
How much bass?
How much treble?
How bright is the spectrum?
How intense is the audio?
```

### Did something just happen?

```text
Was there a new bass onset?
Was there a mid-frequency onset?
Was there a high-frequency onset?
Was there a strong transient?
```

### Is this part of the rhythm?

```text
What is the estimated tempo?
Is this a beat?
Where are we within the beat?
```

### What musical pitch content is present?

```text
Which pitch classes are strongest?
Is the music becoming harmonically different?
```

The engine should expose these signals independently so gameplay systems can choose how to use them.

---

# 4. Target Architecture

The analysis pipeline should conceptually become:

```text
                         AUDIO
                           │
                           ↓
                     FFT / STFT DATA
                           │
          ┌────────────────┼────────────────┐
          ↓                ↓                ↓
      SPECTRAL          RHYTHMIC           TONAL
      FEATURES          FEATURES          FEATURES
          │                │                │
          │                │                │
          ↓                ↓                ↓
     Band Energy         Onsets           Chroma
     Spectral Flux       Tempo            Pitch
     Centroid            Beat
     Contrast
          │                │                │
          └────────────────┼────────────────┘
                           ↓
                   FEATURE PROCESSING
                           │
              ┌────────────┴────────────┐
              ↓                         ↓
        CONTINUOUS FEATURES          EVENTS
              │                         │
       Attack / Release            Onset / Beat
              │                         │
              └────────────┬────────────┘
                           ↓
                      MUSIC STATE
                           │
              ┌────────────┴────────────┐
              ↓                         ↓
          GAMEPLAY                 VISUALIZER
```

The key architectural principle is:

> **Audio analysis determines what is happening in the music. Game/visual systems determine what those musical features mean.**

The analyzer should not contain gameplay logic.

---

# 5. FFT Configuration

Increase the FFT size from 512.

Start by evaluating:

**1024 or 2048 samples**

Use whichever provides the best balance of frequency resolution, temporal responsiveness, and CPU cost.

The implementation should make FFT size configurable.

Do not assume that the largest possible FFT is automatically better.

We care about detecting transient musical events, so temporal responsiveness remains important.

---

# 6. Frequency Representation

Internally, maintain a richer frequency representation than the existing three bands.

Start with approximately **8–12 logarithmically spaced frequency bands**.

For example:

```text
20–40 Hz
40–80 Hz
80–160 Hz
160–320 Hz
320–640 Hz
640–1.25 kHz
1.25–2.5 kHz
2.5–5 kHz
5–10 kHz
10–20 kHz
```

Exact boundaries should be configurable and adjusted based on actual FFT resolution.

The engine should expose the resulting band values as an array rather than hard-coding every band into separate fields.

For example:

```csharp
float[] FrequencyBands;
```

The existing convenience features should remain available:

```text
Bass
Mid
Treble
```

but these should be derived from the richer representation.

---

# 7. Continuous Spectral Features

Implement the following continuous features.

## 7.1 Band Energy

For each frequency band:

```text
BandEnergy[i]
```

Normalize these in a stable and useful manner.

Avoid making the raw representation dependent exclusively on a short-term peak follower.

Where useful, expose both:

```text
Raw / stable energy
Relative / adaptive energy
```

---

## 7.2 Overall Energy

Maintain:

```text
OverallEnergy
```

This represents the overall intensity of the audio.

It should be useful for:

* overall visual intensity
* game difficulty
* enemy density
* background effects

Do not confuse it with instantaneous peak amplitude.

---

## 7.3 Spectral Centroid

Implement spectral centroid / spectral brightness.

Conceptually:

```text
Low centroid
    ↓
bass-heavy / dark spectrum

High centroid
    ↓
treble-heavy / bright spectrum
```

Normalize this to a convenient range such as:

```text
0 → 1
```

Potential future uses:

* hue
* brightness
* visual sharpness
* particle behavior
* world style

---

## 7.4 Spectral Flux

Implement spectral flux.

This should measure the amount of **new spectral energy appearing compared with the previous analysis frame**.

The important concept is:

```text
Current spectrum
        -
Previous spectrum
        ↓
Positive spectral changes
        ↓
Spectral flux
```

This is the primary basis for onset detection.

Do not use the existing bass-threshold detector as the primary onset mechanism.

---

## 7.5 Spectral Contrast

Implement spectral contrast if computationally practical.

This can provide another dimension describing the character of the spectrum.

It may eventually be useful for distinguishing:

* tonal/harmonic content
* dense/noisy content
* percussion-heavy sections
* bright/high-contrast sections

Do not over-optimize this feature during the first implementation.

---

# 8. Onset Detection

This is a major priority.

The engine should distinguish:

> **"Something just started"**

from:

> **"The sound is currently loud."**

Use spectral flux / onset strength rather than a simple amplitude threshold.

---

## 8.1 Global Onset

Expose:

```text
AnyOnset
OnsetStrength
```

`OnsetStrength` should be continuous.

`AnyOnset` should be a one-frame or otherwise explicitly time-bounded event.

---

## 8.2 Frequency-Specific Onsets

At minimum implement onset strength/events for:

```text
Bass
Mid
Treble
```

Preferably derive these from the richer frequency-band representation.

Expose:

```text
BassOnsetStrength
MidOnsetStrength
TrebleOnsetStrength
```

and corresponding event signals:

```text
BassOnset
MidOnset
TrebleOnset
```

This distinction is important.

For example:

```text
BassOnset
    → large pulse

TrebleOnset
    → sparks

MidOnset
    → smaller geometric event
```

The engine should not assume these mappings itself.

---

# 9. Onset Processing

Do not heavily smooth onset signals.

Onset detection should prioritize:

* low latency
* sharp response
* reliable detection
* low false-positive rate

Use an appropriate adaptive threshold or local peak-picking approach rather than a fixed global threshold.

The implementation should account for the fact that different songs have very different dynamic ranges.

A short refractory period may be useful to prevent multiple detections of the same transient.

Make onset sensitivity configurable.

---

# 10. Attack / Release Envelopes

Continuous signals should support asymmetric smoothing.

Instead of one global smoothing constant, use:

```text
Attack
Release
```

For example:

```text
Attack = fast
Release = slower
```

This allows a signal to react quickly to a musical event and then decay naturally.

Conceptually:

```text
Audio energy

       /\
      /  \
_____/    \________
```

rather than:

```text
       /\
      /  \
_____/    \________
      ↑
 delayed/rounded response
```

The envelope processor should be reusable for:

* bass
* mid
* treble
* overall energy
* visual parameters

Do not use these envelopes as a substitute for onset detection.

---

# 11. Beat Detection

Beat detection should be separated from onset detection.

An onset means:

> A new sound/transient appeared.

A beat means:

> An event corresponding to the underlying rhythmic pulse.

These are not equivalent.

The beat system should use the onset-strength signal to estimate:

* tempo
* beat positions
* beat timing

The initial implementation can be relatively simple, but should be more robust than:

```text
bass > runningAverage * threshold
```

---

# 12. Tempo

Expose an estimated BPM:

```text
BPM
```

The BPM should update slowly and should not jump wildly every frame.

Use temporal information from the onset signal rather than attempting to infer tempo from instantaneous bass energy.

---

# 13. Beat Phase

If practical, expose:

```text
BeatPhase
```

normalized:

```text
0 → 1
```

where:

```text
0 = beat
0.5 = halfway to next beat
1 = next beat
```

This could eventually enable continuous rhythmic animation.

For example:

```text
Circle size = sin(BeatPhase)
```

rather than only triggering discrete pulses.

This is likely to become very useful later.

---

# 14. Tonal / Pitch Features

Implement a basic **chroma representation** if practical within the Unity real-time budget.

Expose:

```text
Chroma[12]
```

corresponding to:

```text
C
C#
D
D#
E
F
F#
G
G#
A
A#
B
```

Chroma should represent relative pitch-class energy across octaves.

This is intentionally not full instrument/source separation.

The goal is to provide musical pitch information that can eventually drive:

* color
* geometry
* effects
* game events

For example:

```text
Strong A
    ↓
hue shifts toward a corresponding color

Strong C
    ↓
different hue
```

The exact musical-to-color mapping will be developed later.

---

# 15. MusicState v2

Expand the existing `MusicState`.

A reasonable target structure is:

```csharp
MusicState

// Continuous spectral features
float OverallEnergy;
float Bass;
float Mid;
float Treble;

float SpectralCentroid;
float SpectralFlux;
float SpectralContrast;

// Rich frequency representation
float[] FrequencyBands;

// Onset features
float OnsetStrength;
float BassOnsetStrength;
float MidOnsetStrength;
float TrebleOnsetStrength;

// Events
bool Onset;
bool BassOnset;
bool MidOnset;
bool TrebleOnset;
bool Beat;

// Rhythm
float BPM;
float BeatPhase;

// Tonal
float[] Chroma;
```

The exact naming can be adjusted to match the existing project conventions.

Do not blindly duplicate values.

If a feature can be derived efficiently, consider whether it belongs in `MusicState` or should remain internal.

---

# 16. Raw vs Processed Signals

Where useful, maintain a distinction between:

```text
Raw feature
Processed feature
```

For example:

```text
RawBass
BassEnvelope
BassOnsetStrength
```

These mean different things.

Do not overwrite raw information with a smoothed value.

This allows future systems to choose the appropriate representation.

---

# 17. Audio Timing

Use Unity's audio clock where timing matters.

Prefer:

```csharp
AudioSettings.dspTime
```

over:

```csharp
Time.time
```

for audio-event timestamps.

The audio clock should eventually allow the engine to answer:

> "At what exact audio time did this onset/beat occur?"

This will become important for tight visual synchronization and future offline analysis.

---

# 18. Update Rate

Do not assume that the analysis must be calculated once per rendered frame.

Audio analysis should have an appropriate update cadence independent of visual rendering where practical.

However, keep the first implementation simple.

The priority is:

1. reliable analysis
2. low latency
3. reasonable CPU usage

Do not introduce a complex multithreaded audio-analysis system unless profiling demonstrates that it is necessary.

---

# 19. Debug Visualization

Before relying on the new engine for gameplay, implement a debug visualization.

This is a required part of the development process.

The debug view should show at minimum:

```text
Bass          ███████░░░
Low-Mid       █████░░░░░
Mid           ██████░░░░
High-Mid      ████████░░
Treble        ████░░░░░░

Energy        ███████░░░
Brightness    █████░░░░░
Flux          ████████░░

BPM: 128

BEAT          ●
ONSET         ●
BASS ONSET    ●
MID ONSET
TREBLE ONSET  ●
```

Preferably also show scrolling graphs over the previous several seconds.

The debug visualization should make it possible to listen to a song and visually determine:

* whether kicks are detected
* whether snares are detected
* whether sustained sounds cause false onsets
* whether beats are consistently detected
* whether frequency-specific events correspond reasonably to what we hear

This debug view is more important than polishing the gameplay integration at this stage.

---

# 20. Existing Demo Integration

Once the new analysis engine works, integrate it into the existing visualizer.

The current demo contains:

* a simple triangle player
* a pulsing circle around the player
* feedback visuals
* kaleidoscope effects
* hue changes
* rotation/zoom effects

Do not redesign these systems yet.

Use them as a test harness for the new analysis engine.

---

# 21. First Demonstration Mapping

Use the player circle as the primary demonstration.

The circle should combine multiple musical features.

Suggested mapping:

```text
Bass Energy
    ↓
Baseline circle radius
```

```text
Bass Onset
    ↓
Pulse amplitude
```

```text
Beat
    ↓
Strong synchronized pulse
```

```text
Spectral Brightness
    ↓
Color / hue
```

```text
Overall Energy
    ↓
Visual intensity
```

```text
Treble Onset
    ↓
Small particle/spark event
```

Do not hard-code these relationships into the audio analyzer.

The analyzer only exposes features.

The visualizer decides what they mean.

---

# 22. Circle Behavior

The desired behavior is approximately:

```text
Normal music
    ↓
circle continuously breathes according to bass energy

Bass transient
    ↓
circle rapidly expands

Beat
    ↓
stronger pulse

Higher spectral brightness
    ↓
circle shifts toward brighter/different colors

Higher overall energy
    ↓
more intense visual response
```

The goal is to demonstrate that the same music can control:

* continuous values
* transient events
* rhythm
* tonal characteristics

rather than simply making the circle larger when the music gets louder.

---

# 23. Do Not Optimize for Perfect Musical Semantics

The system does **not** need to perfectly identify:

> "That was a kick drum."

or:

> "That was a guitar."

Frequency-domain analysis cannot reliably make those claims by itself.

The goal is:

> **Extract stable, interesting, musically correlated signals that are useful for procedural game design.**

If a feature produces a visually compelling response that isn't perfectly semantically pure, that is acceptable.

---

# 24. Do Not Implement ML Source Separation Yet

Do not introduce neural-network-based source separation in this version.

Do not attempt to extract:

```text
Vocals
Drums
Bass
Guitar
Piano
```

as separate sources.

That may be investigated later if conventional DSP proves insufficient.

For now, focus on:

* frequency analysis
* spectral flux
* onset detection
* beat tracking
* tempo
* spectral features
* chroma

These provide a strong foundation without introducing significant model/dependency complexity.

---

# 25. Offline Analysis — Future Architecture

The long-term system should eventually support pre-analyzing known tracks.

Conceptually:

```text
Audio File
    ↓
Offline Analyzer
    ↓
TrackAnalysis
    ↓
Saved analysis data
    ↓
Game
```

The analysis could contain:

```text
Beats
Onsets
Tempo
Band energies
Spectral features
Chroma
Song sections
```

This would allow the game to know the entire musical timeline in advance.

For example:

```text
TrackAnalysis

0.00 sec  Energy .10
0.02 sec  Energy .12
...
15.32 sec Beat
15.78 sec BassOnset
16.24 sec Beat
...
83.50 sec MajorEnergyIncrease
...
```

This is **not required for v2**.

However, do not design the real-time architecture in a way that makes an offline `TrackAnalysis` system impossible later.

---

# 26. Acceptance Criteria

The new engine is successful when:

### Analysis

* [ ] FFT resolution is improved from the current 512-bin implementation.
* [ ] Richer frequency bands are available.
* [ ] Bass/mid/treble remain available as convenient derived features.
* [ ] Overall energy remains available.
* [ ] Spectral centroid/brightness is available.
* [ ] Spectral flux is available.
* [ ] Onset strength is available.
* [ ] Frequency-specific onset signals are available.
* [ ] Basic beat detection is substantially more reliable than the current bass threshold.
* [ ] BPM can be estimated.
* [ ] Chroma is available if computationally practical.
* [ ] Continuous features and event features are clearly separated.
* [ ] Continuous features support attack/release behavior.
* [ ] Audio event timing uses the audio clock where appropriate.

### Debugging

* [ ] A debug view shows the primary analysis signals.
* [ ] Onsets and beats can be visually inspected against the music.
* [ ] The system can be evaluated across multiple different songs.

### Integration

* [ ] The existing player circle uses the new features.
* [ ] Bass controls baseline circle size.
* [ ] Bass onset creates a responsive pulse.
* [ ] Beat creates a stronger rhythmic event.
* [ ] Spectral brightness affects color.
* [ ] Overall energy affects visual intensity.
* [ ] At least one treble/mid onset drives a secondary visual event.

### Architecture

* [ ] Gameplay/visual systems do not perform their own FFT.
* [ ] `AudioAnalyzer` contains no gameplay-specific behavior.
* [ ] `MusicState` is the interface between analysis and the rest of the game.
* [ ] Event signals are not heavily smoothed.
* [ ] Continuous signals can be smoothed independently.
* [ ] The system remains usable with local audio files.
* [ ] No Spotify dependency is introduced.

---

# 27. Development Order

Implement in this order.

## Phase 1 — Refactor Current Analyzer

* Preserve current behavior where practical.
* Establish cleaner separation between raw features, processed features, and events.
* Increase FFT resolution.
* Create richer frequency-band representation.

## Phase 2 — Spectral Features

Implement:

1. Band energy
2. Overall energy
3. Spectral centroid
4. Spectral flux
5. Optional spectral contrast

## Phase 3 — Onsets

Implement:

1. Global onset strength
2. Bass onset
3. Mid onset
4. Treble onset
5. Adaptive peak/threshold logic

## Phase 4 — Rhythm

Implement:

1. Tempo estimation
2. Beat detection
3. Beat timing
4. Beat phase if practical

## Phase 5 — Tonal Features

Implement:

1. Chroma
2. Any additional inexpensive tonal feature that proves useful

Do not spend excessive time here if chroma becomes disproportionately difficult.

## Phase 6 — Debug UI

Build the analysis dashboard.

Do this before extensive gameplay integration.

## Phase 7 — Visualizer Integration

Replace the current demo mappings with:

```text
Bass Energy → circle size
Bass Onset → circle pulse
Beat → strong pulse
Spectral Brightness → color
Energy → intensity
Treble/Mid Onset → secondary effects
```

## Phase 8 — Tune With Multiple Songs

Test with substantially different tracks.

At minimum include:

* a simple electronic track
* a rock/instrumental track
* a song with strong vocals
* a track with a quiet intro/build
* a track with strong drums
* a track with relatively weak percussion

The system should not be tuned exclusively around one song.

---

# 28. Important Implementation Principle

Do not attempt to make the analysis engine "understand music" all at once.

Build it as a collection of increasingly useful signals.

The progression should be:

```text
FFT
 ↓
Frequency bands
 ↓
Spectral features
 ↓
Onsets
 ↓
Tempo / beats
 ↓
Pitch / chroma
 ↓
Song structure
 ↓
Eventually: musical understanding
```

Each layer should build on the previous one.

---

# 29. Final Design Principle

The most important architectural principle for this system is:

> **The analysis engine should describe the music; it should not decide how the game responds to the music.**

For example, the engine should say:

```text
Bass = 0.82
BassOnset = true
SpectralBrightness = 0.73
Beat = true
BPM = 128
Chroma[A] = 0.61
```

It should **not** say:

```text
MakeCircleBigger()
SpawnEnemy()
ChangeColor()
```

Those decisions belong to the game/visualizer layer.

This separation will allow the same music-analysis engine to eventually drive:

* the visualizer
* player effects
* enemies
* procedural geometry
* game difficulty
* procedural events
* the future game director
* potentially an entire music-generated level

The immediate objective is simply to build a **rich, reliable, low-latency musical feature layer** and prove its usefulness through the existing player-circle demo.
