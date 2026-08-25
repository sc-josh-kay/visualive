# README — Music Visualizer Playground

## Purpose

Build the next visual layer of the game around one core idea:

> **The player flies through a psychedelic music visualizer, and their movement acts like a brush that paints the current musical state into a persistent, slowly decaying visual field.**

The existing music-analysis engine and player/circle demo are the foundation. This phase should focus exclusively on making the **visualizer itself compelling**.

Do not add enemies or significant new gameplay yet.

---

# 1. Core Experience

The scene should feel like:

**iTunes Visualizer × Geometry Wars × psychedelic flight**

The player remains the visual anchor while the environment continuously responds to the music.

Conceptually:

```text
                MUSIC
                  ↓
           Music Analysis
                  ↓
          Visualizer Field
                  ↑
           Player Movement
                  ↓
          Persistent Trail
                  ↓
          Ripple / Distortion
```

The background should never feel static. The player is effectively **creating the visualizer by moving through it**.

---

# 2. Visual Layers

Build the visualizer as several independent layers.

### A. Global Visualizer

The large-scale psychedelic environment.

Examples:

* kaleidoscopic geometry
* fluid swirls
* organic blobs
* particle fields
* ribbons/filaments
* tunnels/vortices

Driven primarily by `MusicState`.

### B. Player Wake / Trail

The player leaves a persistent visual trail behind them.

The trail should:

* glow
* slowly fade
* vary with the current music
* accumulate when the player crosses an existing trail
* preserve visual history
* eventually disappear

Think of it as **painting onto a canvas that slowly erases itself**.

### C. Ripple

The player periodically generates an outward-moving ripple through the visualizer.

The ripple should:

* originate at the player
* travel outward
* distort/deform the visualizer
* potentially intensify with bass/beat strength
* not affect the player
* not affect enemies/gameplay objects

This is a visual disturbance, not a gameplay collision.

### D. Player

Keep the existing player and music-reactive circle.

The player should remain highly readable even when the visualizer becomes intense.

---

# 3. Trail System

The trail is a core feature, not a cosmetic particle trail.

Conceptually:

```text
Player movement
      ↓
Stamp visual influence
      ↓
Persistent field
      ↓
Decay over time
```

The implementation should favor a **texture/render-texture/GPU field approach** rather than creating large numbers of persistent GameObjects.

The trail should support:

### Decay

New trail:

```text
████████
```

Later:

```text
████
```

Eventually:

```text
█
```

### Reinforcement

If the player crosses an existing trail:

```text
old trail
   ↓
player crosses it
   ↓
trail becomes bright/strong again
```

The oldest visual information should naturally disappear first.

The trail should feel organic rather than like a fixed line following the player.

---

# 4. Music → Visual Mapping

Do not hard-code music analysis into the analyzer.

The visualizer consumes `MusicState`.

Initial conceptual mappings:

| Music Feature        | Visual Behavior                   |
| -------------------- | --------------------------------- |
| Bass energy          | visual mass / trail width / scale |
| Bass onset           | strong local pulse                |
| Beat                 | stronger global pulse             |
| Overall energy       | visual intensity                  |
| Spectral brightness  | color / sharpness                 |
| Mid energy/onsets    | filaments / organic movement      |
| Treble energy/onsets | particles / fine detail           |
| Spectral flux        | turbulence / deformation          |
| Chroma               | color family                      |

These are starting points, not strict rules.

Each visualizer pattern should choose a **small subset** of these features so that every pattern has its own visual personality.

---

# 5. Ripple System

Implement a reusable visual ripple/distortion system.

Conceptually:

```text
                 ripple
              ╱──────────╲
            ╱              ╲
          ╱                  ╲
         │     VISUALIZER     │
          ╲                  ╱
            ╲              ╱
              ╲────●─────╱
                   PLAYER
```

The ripple should **distort the existing visualizer**, rather than simply rendering a circular outline.

Possible effects:

* displacement
* radial distortion
* brightness pulse
* particle deflection
* geometry deformation

The ripple should be parameterized by:

* origin
* radius
* speed
* strength
* width
* lifetime

Bass/onset/beat should be able to trigger it.

---

# 6. Visualizer Pattern System

Create a modular pattern architecture.

Patterns should share a common interface so they can be swapped without changing the rest of the visualizer.

Conceptually:

```text
VisualizerPattern
├── Update(MusicState)
├── Render()
├── ApplyRipple()
├── Reset()
└── Transition(...)
```

Start with **2–3 patterns**, not eight.

Recommended initial patterns:

### 1. Kaleidoscope

* radial symmetry
* geometric
* energetic
* strongly beat/bass driven

### 2. Fluid Swirl

* organic
* warped
* flowing
* driven by mid/treble/flux

### 3. Filament / Particle Field

* glowing lines
* particles
* trails
* driven by treble/onsets

The goal is to prove that the architecture supports distinctly different visual personalities.

---

# 7. Pattern Transitions

Patterns should eventually transition rather than simply disappear and restart.

For the initial implementation, a simple crossfade/morph is sufficient.

Do **not** implement sophisticated automatic music-based pattern selection yet.

For now, provide a simple way to manually cycle patterns for development/testing.

Future goal:

```text
Quiet section
    ↓
Fluid Swirl

Build
    ↓
Filaments

Drop
    ↓
Kaleidoscope
```

But automatic selection belongs to a later phase.

---

# 8. Visual Style

The aesthetic target is:

* psychedelic
* hypnotic
* colorful
* fluid
* highly stylized
* slightly surreal
* occasionally chaotic
* inspired by classic iTunes visualizers

Avoid making it look like a generic:

> neon particle effect

The environment should feel like a **living visual organism generated by the music**.

Important visual characteristics:

* strong glow/bloom
* saturated color variation
* fluid deformation
* trails
* particles
* warped geometry
* symmetry
* organic motion
* occasional extreme visual events

Use darkness/negative space as well. The screen should not always be maximally intense.

---

# 9. Visual Energy

Introduce a high-level concept of visual intensity.

The music should naturally create:

```text
Quiet
  ↓
sparse / dark / subtle

Build
  ↓
increasing complexity

Chorus / drop
  ↓
large forms / intense color / distortion

Breakdown
  ↓
simple / strange / slow
```

Do not simply maximize every visual parameter whenever the music gets louder.

The visualizer needs **contrast** between calm and intense moments.

---

# 10. Player Readability

The player must remain visually distinct.

Maintain:

* strong player silhouette
* local glow
* existing reactive circle
* clear separation from background

The visualizer may become extremely complex, but the player should never become indistinguishable from it.

---

# 11. Architecture

Keep these systems separated:

```text
AudioAnalyzer
      ↓
MusicState
      ↓
VisualizerController
      ↓
┌──────────────┬───────────────┐
│              │               │
Pattern     TrailSystem     RippleSystem
│              │               │
└──────────────┴───────────────┘
                ↓
             Rendering
```

### AudioAnalyzer

Describes the music.

### VisualizerController

Coordinates visual behavior.

### VisualizerPattern

Generates the current visual style.

### TrailSystem

Maintains persistent player-generated visual history.

### RippleSystem

Creates temporary radial disturbances.

### Gameplay

Should remain independent of all of these systems.

Do not put gameplay-specific logic into the audio analyzer.

---

# 12. Implementation Priorities

Implement in this order:

### Phase 1 — Visualizer foundation

* Create `VisualizerPattern` architecture.
* Preserve the existing player/circle demo.
* Establish clean visualizer layers.

### Phase 2 — Trail

* Player movement stamps into a persistent field.
* Add decay.
* Add reinforcement when crossing existing trail.
* Make trail visually responsive to `MusicState`.

### Phase 3 — Ripple

* Create radial distortion field.
* Trigger from bass/onset/beat.
* Ensure it affects only the visualizer.

### Phase 4 — Patterns

Implement:

1. Kaleidoscope
2. Fluid Swirl
3. Filament/Particle Field

Each should use different music features.

### Phase 5 — Integration/tuning

Tune:

* color
* bloom
* trail persistence
* ripple strength
* visual density
* movement response
* music responsiveness

The goal is **feel**, not technical complexity.

---

# 13. Performance Guidelines

Prefer GPU/shader-based rendering for:

* persistent visual fields
* distortion
* large particle populations
* trail decay
* kaleidoscope effects

Avoid thousands of individual GameObjects for visual effects.

However, do not prematurely build a complex rendering framework. The first goal is to produce a compelling prototype with a reasonable architecture.

Profile before optimizing.

---

# 14. Explicit Non-Goals

Do **not** implement yet:

* enemies
* weapons
* scoring
* procedural levels
* Spotify integration
* offline track analysis
* ML/source separation
* automatic pattern selection
* sophisticated song-section detection
* complex gameplay mechanics

Those will build on this system later.

---

# 15. Definition of Done

This phase is successful when we can:

1. Fly the player around the scene while music is playing.
2. See a visually compelling psychedelic environment responding to the music.
3. Leave a persistent trail behind the player.
4. Watch the trail slowly disappear over time.
5. Reinforce old trail by moving back over it.
6. Trigger visible ripples from the player into the visualizer.
7. See bass/beat/onset information meaningfully affect those ripples.
8. Cycle between at least three distinct psychedelic visual patterns.
9. See the patterns respond differently to different musical features.
10. Keep the player clearly visible throughout.
11. Watch the visualizer become calmer during quiet sections and more intense during energetic sections.

**The ultimate test:**

> Put on a good song, move the player around for several minutes, and determine whether it feels fun to simply fly around and "paint" the music.

If that is compelling, we have the foundation for the actual game.
