# Implementation Plan — Music Visualizer Playground

Derived from `spec4_music_visualizer_playground.md`. **Plan for approval — no code yet.** Execute
phase by phase, each with a Play-mode verification. Aesthetic first; no enemies/gameplay this phase.

---

## Where we're starting from

We already have most of the raw ingredients from Phase 7 / the analysis engine:

- A **render-texture feedback pipeline** (`FeedbackController`): seed → feedback accumulation →
  kaleidoscope → persistence → fullscreen background quad. Two shaders (`Feedback`, `Kaleidoscope`).
- The rich **`MusicState` v2** (bands, energy, centroid, flux, onsets, beat/BPM/phase, chroma).
- **`PlayerCircle`** + the player triangle (readable anchor), URP **Bloom**.

The spec asks us to build clean, swappable layers plus a dedicated trail field and a ripple system.
**Decision: build the new layered system fresh, alongside the existing effect** — the current
`FeedbackController` keeps running until the new `VisualizerCore` reaches parity, then we swap the
display over and retire the old one. We carry over the shader/RT *techniques* (and generic shader
files where useful), but the new pipeline stands on its own.

---

## Target architecture (spec §11)

```
AudioAnalyzer → MusicState → VisualizerCore ──┬── VisualizerPattern (active, + crossfade target)
                                              ├── TrailSystem   (persistent painted field)
                                              ├── RippleSystem  (radial distortions)
                                              └── VisualIntensity (calm↔intense contrast)
                                                        ↓
                                              composite → background quad  (player renders on top)
```

- **`VisualizerCore`** — the coordinator (evolves from `FeedbackController`; folds in the old
  `VisualizerController` bloom duty). Owns the RTs, the active pattern (+ a second during
  crossfade), the trail and ripple systems, the visual-intensity value, and the display quad.
  Reads `MusicState`; contains **no gameplay logic**.
- **`IVisualizerPattern`** — common interface so patterns are swappable:
  `Configure() · Update(MusicState, intensity) · Render(ctx) · ApplyRipple(ripples) · Reset()`.
- **`TrailSystem`** — a persistent **field RenderTexture**. Each frame: decay (fade + slight
  diffusion for organic spread), then **stamp** a music-modulated brush at the player's position.
  Reinforcement is automatic (additive accumulation where the player re-crosses), oldest fades
  first. GPU field, **no per-effect GameObjects** (spec §3/§13). Replaces the old seed-camera hack.
- **`RippleSystem`** — a small pool of active ripples (origin, radius, speed, strength, width,
  lifetime) packed into shader uniforms; patterns distort their sampling by them. Triggered by
  bass onset / beat, strength scaled by onset/beat strength. Visual-only (never touches gameplay).
- **`VisualIntensity`** — a 0..1 "how intense should the visuals be" derived from energy with
  **contrast shaping** (quiet ⇒ sparse/dark, energetic ⇒ large/intense), so we don't just max
  everything (spec §9).

**Patterns each pick a small feature subset** for their own personality (spec §4/§6):
1. **Kaleidoscope** — radial symmetry, geometric, **beat/bass** driven (our current look, refactored).
2. **Fluid Swirl** — organic domain-warp/curl flow, **mid/treble/flux** driven.
3. **Filament / Particle Field** — glowing lines/particles, **treble/onset** driven.

**Reuse note:** the trail field feeds the patterns (the player "paints" and each pattern stylizes
the painted history), matching the core idea that *the player creates the visualizer by moving*.

---

## Phases & verification

### Phase V1 — Visualizer foundation & pattern architecture (built fresh)
- Introduce `IVisualizerPattern` + a new `VisualizerCore` and a fresh `KaleidoscopePattern`
  (reusing the generic shaders where useful), rendering to **its own** display quad. It runs
  **alongside** the existing `FeedbackController`, with a **toggle key** to switch which one is
  shown. Add the manual pattern-cycle key (one pattern for now). Player/circle untouched.
- **✅ Verify:** toggle between the old effect and the new `VisualizerCore` kaleidoscope; the new one
  renders a working psychedelic background; player + circle intact; no errors. (The new look need
  not match the old exactly — it's a fresh implementation.) Once the new system reaches parity in a
  later phase, we retire `FeedbackController`.

### Phase V2 — Trail system
- Dedicated persistent **field RT**: the player stamps a brush (width ← bass, color ← chroma/
  brightness) that **decays over seconds**, **reinforces** when re-crossed, and spreads organically.
  The active pattern consumes the field.
- **✅ Verify:** flying around leaves a **glowing trail that slowly fades**; crossing your own trail
  **brightens it**; trail width/color shift with the music; the oldest parts disappear first. It
  should feel like *painting on a canvas that slowly erases itself*.

### Phase V3 — Ripple system
- `RippleSystem` + a distortion term in the pattern shaders (radial displacement of sampling).
  Ripples spawn on **bass onset / beat**, strength from onset/beat strength; travel outward; fade.
- **✅ Verify:** strong bass/beats send a **ring of distortion outward from the player**, warping the
  visualizer (not just drawing a circle); bigger hits = bigger ripples; the **player is unaffected**.

### Phase V4 — Patterns (Fluid Swirl + Filament) & transitions
- Implement `FluidSwirlPattern` (mid/treble/flux warp) and `FilamentPattern` (treble/onset
  particles/filaments). Manual cycling with a **crossfade** between the outgoing and incoming
  pattern.
- **✅ Verify:** the cycle key smoothly **crossfades** between Kaleidoscope / Fluid Swirl / Filament;
  each is **visually distinct** and reacts to **different features** (kaleidoscope pops on beat/bass,
  fluid churns on mid/flux, filaments sparkle on treble/onset).

### Phase V5 — Visual energy, contrast & tuning
- Add `VisualIntensity` contrast (quiet ⇒ sparse/dark/subtle, build ⇒ complexity, drop ⇒ large/
  intense, breakdown ⇒ simple/strange). Tune color, bloom, trail persistence, ripple strength,
  density, responsiveness, and **player readability / negative space**.
- **✅ Verify (definition of done):** quiet sections look calm and dark, energetic sections intense;
  ripples/trail/patterns all read; the player stays clearly visible throughout; and the ultimate
  test — *put on a good song, fly around and "paint" for several minutes: is it fun?*

---

## Key technical choices (baked in unless you object)
- **Trail = GPU field via RT stamping** (decay + additive reinforcement), not `TrailRenderer` and not
  many GameObjects — the spec mandates this (§3, §13).
- **Ripple = true shader distortion** of the visualizer sampling, not a drawn outline (§5).
- **Build fresh alongside**: the new `VisualizerCore` is implemented from scratch and runs next to
  the old `FeedbackController` (toggle to compare) until it reaches parity, then the old one is
  retired. Generic shaders are reused where they fit.
- **RT resolution** stays ~half-res (as now); profile before optimizing (§13).
- **Player readability**: player + circle keep rendering on top of the composited visualizer at full
  clarity; intensity modulation keeps backgrounds from washing the player out (§10).

## Risks / watch-items
- Most of this is **shader + render-texture work** — the largest visual effort so far, though it
  builds directly on Phase 7's pipeline.
- **Fluid Swirl / Filament** are new shader patterns; each is a bounded, self-contained addition.
- Keeping **contrast** (not maxing everything) is a tuning problem for Phase V5, not a code problem.

## Resolved decisions
1. **Build fresh alongside** the existing effect (toggle to compare), retire the old one once the
   new system reaches parity.
2. **Three patterns**: Kaleidoscope · Fluid Swirl · Filament/Particle Field.
