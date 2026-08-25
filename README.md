# Play Visualizer — a music-painting twin-stick game

A Unity (URP, 2D) game where **you don't survive — you keep the world beautiful.** Movement paints a living, music-reactive visualizer; enemies eat that color; killing enemies bursts it back. Played for the length of a song, with no player death.

Unity project lives in [`play_visualizer/`](play_visualizer/). Specs `spec0`–`spec7` document the original intent per feature.

---

## Design pillars

1. **Color = score, enemies = threats to color.** Score accrues continuously from current visualizer **coverage** (nonlinear: ~coverage²). There is no death; poor play just means a darker, lower-scoring world.
2. **Three visual languages** (learnable without a tutorial):
   - 🟣 **PAINT** (persistent, adds coverage) — player movement trail + enemy-death explosions. Organic, flowing, slowly fading.
   - ⚡ **ENERGY** (transient, no coverage) — weapon fire. Sharp, bright, short-lived; it *distorts/ripples* the visualizer but never paints it.
   - 💥 **EXPLOSION** (persistent, the payoff) — enemy death: a music- and combo-scaled color burst. **Killing enemies is the main way combat paints.**
3. **Alive / non-uniform / per-run-unique.** Effects avoid perfect circles and identical repeats — consume auras wobble, explosions are hashed fireworks, projectiles weave. Everything reacts to music.
4. **Music drives *expression*, not unpredictability.** Music changes how things look/feel; it never randomizes weapon aim, fire, or enemy AI in ways the player can't read.
5. **Mobile-first controls.** Twin-stick **auto-fire**: left stick moves, right stick aims, the weapon fires automatically while aiming — no fire button. Desktop mouse and gamepad both stand in for the right stick.

---

## Architecture (layers)

```
AudioAnalyzer → MusicState ──┬──→ Visualizer (owns the visual field)
 (analysis only)             └──→ Gameplay (player, enemies, score, flow)
                                       └──→ Weapons

          Gameplay ⇄ Visualizer talk ONLY through VisualizerField
```

- **`MusicState`** is the shared read-only snapshot of the current musical moment. Only `AudioAnalyzer` writes it; everyone else reads it. No other system runs FFT.
- **`VisualizerField`** (`Assets/Scripts/Visuals/`) is the single seam between gameplay and the visualizer. Gameplay never touches RenderTextures/shaders — it calls:
  - `Consume(pos, radius, strength)` — eat coverage to black (enemies).
  - `Paint(pos, radius, intensity, MusicState)` — inject a color burst (enemy deaths).
  - `Distort(pos, strength)` — a transient ripple, **no coverage** (weapon energy).
  - `Coverage`, `HotWorldPos`/`HasHotPoint` (targeting), `PlayerPaintGain` (trail throttle).

---

## Graphics / Visualizer (`Assets/Scripts/Visuals`, `Assets/Shaders`)

- **`VisualizerCore`** coordinates a persistent RenderTexture field and swappable `IVisualizerPattern`s. The primary pattern is **`KaleidoscopePattern` + `SmokeField.shader`**: a player-painted smoke field that advects, fades, and emits at the player. This field *is* the coverage surface.
- **Splats:** each frame the core drains `VisualizerField`'s queued `Consume`/`Paint` requests and stamps them into the field via **`FieldSplat.shader`** (additive color for paint, multiplicative darken for consume). Consume is **non-uniform** (angle/time/position-wobbled boundary).
- **Coverage:** `CoverageSampler` GPU-downsamples the field and uses `AsyncGPUReadback` to compute coverage (0–1) + the "hot" painted-mass centroid that enemies target.
- **Distortion:** `RippleSystem` supplies transient radial ripples (beats, onsets, and weapon/vortex `Distort` calls).
- **Trail feel:** the smoke's persistence is coupled to `PlayerPaintGain` so a still/hit player's world actively dissipates.

---

## Music analysis & how it drives the game (`Assets/Scripts/Audio`)

`AudioAnalyzer` is the **only** component that runs FFT. It pulls the spectrum from an `IMusicProvider` (`LocalClipMusicProvider`) and publishes the shared `MusicState`: `Energy`, `Bass`/`Mid`/`Treble`, `SpectralCentroid`/`SpectralFlux`, onset flags/strengths, `BPM`/`Beat` (+ `BeatPhase`), `Chroma`, and `SongTime`/`SongLength`. `AudioAnalyzer.Instance` is a cached accessor for lightweight visuals.

**Two paths from music to the game:**

- **Gameplay coupling is centralized.** Enemies/spawner/player are audio-agnostic — the *only* place music changes **gameplay** is `MusicMapper` (press **`M`** to A/B toggle it). This keeps gameplay testable and prevents music from silently altering behavior everywhere.
- **Visuals read `MusicState` directly.** Anything purely cosmetic (smoke, explosions, weapon/projectile look) samples `MusicState` itself.

**Signal → effect, by system:**

- **Spawn director** (`MusicMapper` + `MusicMappingConfig`): **SongProgress** → an intensity curve that scales spawn rate + on-screen enemy cap and unlocks tougher enemy types (the main difficulty ramp); **Energy** → spawn rate; **Bass** → enemy move speed (via `EnemyModulation`); **Beat** → spawn burst. A warmup keeps the opening calm.
- **Smoke / visualizer** (`VisualizerController`, `KaleidoscopePattern`, `RippleSystem`): **Energy** → bloom + overall visual intensity; **Bass** → background pulse, smoke blob radius, ring pop; **Treble** → particle rate + smoke sparkle; **Beat / bass onsets** → ripples; **SpectralCentroid** → hue.
- **Enemy-death explosion** (`EnemyBase.Die`): **Bass** → radius, **Energy** → brightness, **Beat** → pulse, **SpectralCentroid** → color — all multiplied by the **combo**.
- **Weapons — look, never behavior** (`WeaponBase.EnergyColor` + per-weapon): every shot's color = **SpectralCentroid** (hue) × **Energy** (brightness). Scatter **Bass**→pellet size, Beat→extra pellets; Growth **Bass**→growth/max size, Beat→growth pulse; Rapid **Energy**→max fire rate, Beat→rate burst; Homing **Treble**→count, **Mid**→curvature; Vortex **Bass**→radius, **Energy**→intensity, Beat→expansion (reacts continuously).
- **Projectile "alive" look** (`ProjectileVisual`): **Bass** → head pulse, **Beat** → blip, **Energy** → brightness, **Treble** → hue shimmer. (The per-shot tail wobble is intentionally *not* music-driven, so the two kinds of life don't compound into jitter.)

**Guiding rule (spec7 §9):** music changes how things **look and feel** — it must never randomize aim, fire timing you can't read, or enemy targeting. The player should always understand what their weapon and the enemies are doing; music just makes it feel alive.

---

## Enemies (`Assets/Scripts/Enemies`)

`EnemyBase` (abstract) holds shared plumbing: health, projectile damage (`IDamageable`), targeting, anti-clumping (separation + per-shot jitter), vortex pull, and death. Three types:

- **Color Eater** — basic; seeks dense smoke, eats a non-uniform aura, chases the player when close.
- **Corruptor** — slow/tanky; entrenches and grows an expanding corruption; big death explosion.
- **Swarm** — weak flocking units (cohesion + separation); dangerous in numbers.

Enemies **never damage the player**. Reaching the player triggers a "puff of blackness" (`CollisionBurst` firework + strong consume, no reward). **Death** = the persistent color explosion, scaled by **Bass→radius, Energy→brightness, Beat→pulse, and the combo multiplier**.

Spawning: `EnemySpawner` uses a weighted type table with group spawns (swarms as clouds). A **song-time intensity director** (in `MusicMapper`) ramps spawn rate + on-screen cap and unlocks tougher types as the song progresses — this is the main density/difficulty control.

---

## Weapons (`Assets/Scripts/Weapons`)

Twin-stick **auto-fire**; **Space cycles** weapons (debug). `WeaponController` builds a per-frame `WeaponContext` (aim, muzzle, `IsAiming`, `MusicState`) and ticks the active `WeaponBase`. Six weapons, each a different way to fling **ENERGY** (no coverage):

| Weapon | Behavior |
|---|---|
| Pulse Stream | precise single shots |
| Scatter | shotgun fan (bass→size) |
| Growth Stream | shots that grow + gain damage over distance |
| Rapid Stream | fire rate ramps while aiming |
| Homing Swarm | curving projectiles seek enemies |
| Vortex Wave | a slow wave anchors a vortex that pulls/slows/damages enemies and swirls the visualizer |

`Projectile` = physics **root** (straight, predictable hits) + a **`ProjectileVisual`** child (soft core+halo glow, per-shot wobbling trail, bass pulse, treble hue-shimmer, impact bloom). All cosmetics live on the child so they never affect trajectory. On impact: damage + a transient `Distort` ripple only.

**Combo:** rapid kills raise a multiplier (`ScoreManager`) that scales enemy-death coverage and score.

---

## Player, flow, scoring

- **`PlayerController` / `PlayerInputReader`** — twin-stick abstraction; `PlayerInputReader` is the only place that touches input devices (keyboard/mouse/gamepad + injected touch via `TouchControls`).
- **`PlayerTrail`** — writes `PlayerPaintGain` from movement speed (stand still → trail fades to a floor) and enemy-hit disruption.
- **`GameManager`** — menu → play → results. On song end: stop spawning, brief outro, then a summary (final score, peak/avg coverage, enemies destroyed, per-song high score via `PlayerPrefs`). No game-over. **Dev shortcut: press `K` in the Editor to end the song.**
- **`ScoreManager`** — coverage→score accumulation + run stats + combo.

---

## Building the scene (Editor menus)

The scene is assembled by re-runnable `PlayVisualizer/…` menu scripts (`Assets/Scripts/Editor/`), e.g. base player/camera, visualizer core, enemies + spawner, HUD, **`Build Corruptor + Swarm (Phase 6)`**, **`Gameplay MVP: Wire Player Trail (Phase 4)`**, and **`Build Weapon System (spec7)`** (builds the six weapons + projectile/vortex prefabs + energy materials). Re-run the relevant menu after changing a prefab-building setup.

---

## How to extend

- **New enemy:** subclass `EnemyBase`, implement `Behave(dt)` (movement + `VisualizerField.Consume`), give it an `EnemyConfig`, add it to the spawner's weighted table (with a `MinSongProgress` unlock).
- **New weapon:** subclass `WeaponBase`, implement `Tick(ctx)` (own fire timing → `Spawn(...)`), set a unique `Order`, add it in the weapon setup. Keep it **energy-only** (no `Paint`).
- **New visual pattern:** implement `IVisualizerPattern`; only field patterns need to apply splats.
- **Tuning** lives on ScriptableObject configs (`Assets/ScriptableObjects`) and serialized fields on the weapon/enemy/visual components.

---

## Critical gotchas

- **iOS/device shader stripping:** shaders obtained at runtime via `Shader.Find` (e.g. `FieldSplat`) get stripped from device builds unless referenced. All `PlayVisualizer/*` shaders are in **Project Settings → Graphics → Always Included Shaders**; keep new custom shaders there if they aren't referenced by a saved material/scene. (This once broke consume/paint on iPhone while working in the Editor.)
- **`MusicState` is a shared, mutating snapshot** — sample the values you need at call time; don't retain the reference.
- Target frame rate is capped (see `AppBootstrap`) because the feedback visuals are tuned for it.

---

*Runtime toggles: `V` old/new visualizer · `B` cycle pattern · `M` music-driven spawning · `Space` cycle weapon · `K` (Editor) end song.*
