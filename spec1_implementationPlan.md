# Implementation Plan — Music-Driven Arcade Game (MVP)

Derived from `spec0_readMe.md`. This is a **plan for approval**, not code. Nothing gets
implemented until you say go, and we'll approve work **phase by phase**.

---

## 0. Current state (already done for us)

The `play_visualizer/` Unity project is the standard **Unity 6 URP template**:

- Unity `6000.5.9f1`
- Universal Render Pipeline `17.5.0` (installed + configured) — gives us bloom/glow for the neon look
- Input System `1.20.0` (installed) — modern input as the spec requires
- Test Framework `1.7.0` (installed) — lets us write EditMode/PlayMode tests
- `Assets/Scenes/SampleScene.unity` exists (template scene)
- `Assets/InputSystem_Actions.inputactions` exists (template input asset)

So **Phase 1 is mostly configuration, not creation.** We inherit a working render pipeline.

### How we'll verify things (two layers)

1. **CLI compile check (my job, every time I add/change scripts).** I can run the editor
   in batch mode to confirm scripts compile with no errors before you ever open the editor:
   ```bash
   "/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity" \
     -batchmode -quit -projectPath "/Users/josh_kay/Documents/dev/unity/play_visualizer/play_visualizer" \
     -logFile - | tail -40
   ```
   (Close the editor first — Unity locks the project. We'll refine this command in Phase 1.)
2. **Your smoke test (your job, end of each phase).** A short, concrete "press Play, expect
   X" check so you can confirm each stage works and understand what it does. Marked ✅ below.

### Working style for this project (agreed)

- **Hybrid editor setup.** I write all C# and ScriptableObject assets. For **learning-valuable,
  gameplay-critical wiring** (player, enemy, camera, first scene assembly) I'll give you
  step-by-step editor instructions so you re-learn the workflow. For **tedious/repetitive
  setup**, I may offer a small editor menu script to do it — I'll always flag which is which.
- **You supply your own audio** — you'll drop a track into `Assets/Audio/Tracks/` at Phase 5.
  I'll note the import settings that make real-time analysis work.
- **Approval gate:** I stop and get your OK before implementing each phase.

---

## 1. Target architecture (the boundaries the spec insists on)

The spec is emphatic about one thing: gameplay/visual systems must **not** reach into the
`AudioClip` or do their own FFT. Everything flows one direction through a `MusicState`:

```
Audio Source  →  Audio Playback  →  Audio Analysis  →  MusicState  →  Music Mapping  →  Game/Visual systems
 (AudioClip)     (AudioSource)      (AudioAnalyzer)     (data)         (MusicMapper)     (spawner, enemies, visuals)
```

Concretely:

- **`IMusicProvider`** — thin interface over "where playback + samples come from." MVP has one
  implementation, `LocalClipMusicProvider` (wraps an `AudioSource`). This is the seam that lets
  Spotify/files/generated audio slot in later *without touching gameplay*. One small interface,
  not a framework.
- **`AudioAnalyzer`** — the *only* thing that calls `GetSpectrumData`/`GetOutputData`. Produces:
  `Bass, Mid, Treble, Energy` (normalized 0–1) and `Beat` (bool this frame).
- **`MusicState`** — plain data object (the struct/class from the spec). Read-only to consumers.
- **`MusicMapper`** (a.k.a. mapping controller) — reads `MusicState` each frame and pushes values
  into gameplay/visual systems using a **`MusicMappingConfig`** ScriptableObject. This is where
  "Energy → spawn rate", "Bass → enemy size", "Beat → shockwave" live — *not* inside the enemy.
- **Gameplay/visual systems** expose plain setters/parameters. They never see the analyzer.

**Why this shape:** it's the minimum that honors the spec's hard boundary and makes the future
`GameDirector` a drop-in above `MusicMapper` later. Alternative considered: let each system read
`MusicState` directly (simpler, fewer files) — rejected because the spec explicitly warns against
scattering music logic into unrelated systems, and centralizing mappings is the stated long-term
foundation.

### ScriptableObject configs (data-driven tuning)

`PlayerConfig`, `EnemyConfig`, `SpawnerConfig`, `MusicMappingConfig`, `VisualizerConfig`.
Created only as each phase needs them — not all up front.

### Folder structure (created in Phase 1)

```
Assets/
├── Audio/{Tracks,Analysis}
├── Materials/
├── Prefabs/{Player,Enemies,Projectiles,Visuals}
├── Scenes/
├── Scripts/{Audio,Player,Enemies,Gameplay,Visuals,UI}
├── ScriptableObjects/
└── Tests/
```

---

## 2. Rendering approach (decided, minimal)

**Top-down, orthographic camera, 3D URP renderer, neon via emissive + Bloom.**

- Keep the existing URP 3D renderer (no switch to 2D renderer needed).
- Orthographic camera looking straight down (top-down arena).
- Objects are simple bright/unlit meshes or sprites; glow comes from **URP Global Volume → Bloom**
  reading emissive/HDR colors. This is the Geometry Wars / MilkDrop neon look with near-zero art.
- We do **not** build a rendering framework (spec rule). Post-processing is one Volume asset.

Alternative considered: URP 2D renderer + 2D lights. Rejected for MVP — Bloom on the 3D renderer
is the shortest path to glow and keeps the door open for 2.5D depth effects later.

---

## Phase 1 — Unity Foundation

**Goal:** clean project skeleton, input configured, an empty dark arena that runs, and a working
CLI compile check.

**I build:**
- The folder structure above (with `.gitkeep`-style placeholders so empty folders persist).
- A `GameManager` stub script (holds game state enum: Playing/GameOver — expanded in Phase 4).
- Confirm/adjust Player Input handling is set to the new Input System (Project Settings).

**We wire (manual — learning):**
- Duplicate `SampleScene` → `Arena` scene (or rename), set camera to **Orthographic**, dark
  background, position for top-down.
- Add a **Global Volume** with a Bloom override (we'll tune later).

**Architecture note:** nothing music/gameplay yet — just the stage.

**✅ Your smoke test:**
1. I run the CLI compile check → report "0 errors."
2. You open `Arena` scene, press **Play**. Expect: a dark empty view, no errors in Console,
   game exits Play cleanly. (You've confirmed the pipeline + input package load.)

---

## Phase 2 — Player

**Goal:** a controllable neon ship that moves, aims independently, and shoots. This is the
"twin-stick" core and the most important feel to get right.

**I build:**
- `PlayerController` — movement (WASD / left stick) + aiming (mouse position / right stick),
  decoupled so aim ≠ move direction. Responsive, not physics-y (direct transform/velocity).
- `PlayerShooter` — fires `Projectile` prefab toward aim at a fire rate.
- `Projectile` — moves forward, despawns on lifetime/bounds (object-pool-friendly but simple).
- `PlayerConfig` ScriptableObject — move speed, fire rate, projectile speed, etc.
- Input actions (Move, Aim, Fire) — either extend the template `.inputactions` or a focused new
  asset; I'll recommend one and explain.

**We wire (manual — learning):**
- Build the Player GameObject (bright emissive material), attach scripts, assign `PlayerConfig`.
- Build the Projectile prefab.
- Keep camera fixed for now (spec: "basic camera").

**✅ Your smoke test:**
Press Play. Move with WASD; the ship aims at the mouse independently of movement; click/trigger
fires glowing projectiles in the aim direction. Controls feel responsive. Bloom makes ship +
bullets glow.

---

## Phase 3 — Enemy + Spawning

**Goal:** enemies spawn, chase, die to bullets, damage on contact, award score — *without music
yet* (plain timer spawning).

**I build:**
- `Enemy` — moves toward player (dead-simple steering), has health, dies to projectiles (with a
  small death particle burst), deals contact damage. Exposes speed/scale/health/damage as plain
  fields (so the mapper can drive them later — **no music logic inside**).
- `EnemySpawner` — timer-based spawning at a distance from the player, with `Maximum Enemies`
  cap. Spawn rate is a **plain parameter** (music drives it in Phase 6).
- `EnemyConfig` + `SpawnerConfig` ScriptableObjects with the spec's parameter list
  (Spawn Rate, Max Enemies, Enemy Speed, Enemy Health, Spawn Distance, Damage).
- Collision handling (projectile↔enemy, enemy↔player) via triggers.

**We wire (hybrid):**
- Manual: Enemy prefab + material, spawner GameObject, assign configs (learning-valuable).
- I may provide a tiny helper if collision-layer setup is fiddly (I'll flag it).

**✅ Your smoke test:**
Press Play. Enemies appear at range and move toward you. Your bullets destroy them (with a pop of
particles). Touching one costs you (we'll log/health-bar it in Phase 4). Enemy count respects the
max cap. All still on a plain timer — no music.

---

## Phase 4 — Game Loop (health, score, game over, restart)

**Goal:** a complete playable arcade loop, still music-free. Spec checkpoint: *"the game should
already be playable without music-driven behavior."*

**I build:**
- `Health` (player) — starts 100, `-25` on enemy contact, triggers Game Over at ≤0.
- `ScoreManager` — enemy destroyed → score up. Independent of audio (spec).
- `GameManager` (expanded) — Playing → GameOver → Restart; restarts scene/state cleanly.
- Minimal UI (uGUI): score readout, health, and a **GAME OVER / Score / Restart** panel.

**We wire (manual — learning):**
- Canvas + text elements; hook Restart button; wire references.

**✅ Your smoke test:**
Full loop: play, shoot, score climbs, take hits until health hits 0 → GAME OVER panel with your
score → Restart → clean fresh game. **This is a milestone: a real (if plain) arcade game.**

---

## Phase 5 — Audio (playback + analysis + MusicState)

**Goal:** stand up the audio pipeline and *prove the analyzer works visually* before touching
gameplay.

**You provide:** a music track (WAV or MP3) into `Assets/Audio/Tracks/`. I'll specify import
settings (Decompress on load / streaming choice, force-mono considerations) so `GetSpectrumData`
returns usable data with low latency. Tracks with a clear beat and strong bass test best.

**I build:**
- `IMusicProvider` + `LocalClipMusicProvider` (wraps `AudioSource`).
- `AudioAnalyzer` — FFT via `GetSpectrumData`; bins into Bass/Mid/Treble; Energy via RMS;
  energy-based **beat/onset** detection (instantaneous vs. running average, with cooldown).
  Outputs normalized 0–1 where possible. Tunable thresholds exposed.
- `MusicState` — the data object consumers read.
- **Debug HUD** (temporary): on-screen bars for Bass/Mid/Treble/Energy + a flash on Beat. This is
  how you *see* the analyzer working with no gameplay coupling.

**Architecture note:** this is the whole left half of the spec's data flow, isolated. Gameplay
does not consume it yet.

**✅ Your smoke test:**
Press Play, your track plays. The debug bars move with the music — bass bar jumps on kicks, treble
on hats — and the beat indicator flashes roughly on the beat. Tune thresholds together until it
feels right. (No gameplay reaction yet — that's deliberate.)

---

## Phase 6 — Music-Driven Gameplay (the core proof)

**Goal:** music visibly changes gameplay through the `MusicMapper` — the spec's central claim.

**I build:**
- `MusicMapper` — reads `MusicState`, applies `MusicMappingConfig`, pushes values into systems.
- `MusicMappingConfig` ScriptableObject — tunable curves/ranges for each mapping.
- Wire the three spec mappings:
  1. **Energy → Spawn Rate** (more energy = more enemies).
  2. **Bass/Energy → Enemy parameter** (speed or scale — we'll pick whichever looks best live).
  3. **Beat → Event** (e.g., a small enemy burst or spawn pulse on the beat).

**Architecture note:** all three mappings live in `MusicMapper`/config — the enemy and spawner
stay music-agnostic. Swapping/adding a mapping later touches one place.

**✅ Your smoke test:**
Play a track. Quiet sections → sparse, calm enemies. Loud/high-energy sections → noticeably more
enemies, and the chosen enemy parameter (speed/size) shifts with the bass. On beats, the event
fires. Toggling the mapper off should make it feel like an ordinary shooter — the contrast is the
proof.

---

## Phase 7 — Visualizer (music → visuals)

**Goal:** the arena responds to music even when you do nothing — the "playable music visualizer"
feeling. Spec: *"This is essential."*

**I build (a small, high-impact set — not everything the spec lists):**
- **Bass → background/radial pulse** (arena or a large geometric ring scales/pulses).
- **Beat → shockwave/flash** (screen or radial burst on beat).
- **Treble → particle intensity** (ambient particle field reacts).
- **Bloom intensity** nudged by Energy for overall "lift."
- `VisualizerConfig` ScriptableObject for tuning; all driven via `MusicMapper` from `MusicState`.

**✅ Your smoke test:**
Press Play and **don't touch the controls.** The arena breathes with the bass, particles shimmer
on treble, beats produce a visible pulse/shockwave. It reads as a visualizer you happen to be able
to play inside.

---

## Phase 8 — Polish (only after the above works)

Tune gameplay feel, audio mapping ranges, visual effect strength; test multiple tracks; tighten
UI; remove the debug HUD (or hide behind a key). No new systems — just making it feel good and
confirming the **Definition of Done** (spec §"Definition of Done"): launch → play → hear music →
control → shoot → score → die → restart → *and it feels meaningfully different because the music is
driving it.*

---

## 3. Proposed approval flow

1. You approve **this plan** (or request changes).
2. For **each phase**: I show you exactly what I'll add (files + editor steps), you approve, I
   implement + run the CLI compile check, then you run the ✅ smoke test. We only advance when
   you're satisfied.
3. I'll briefly explain any meaningful architecture choice as it comes up (spec rule #8), without
   over-documenting trivia.

## 4. Open items / things I'll confirm as we go

- **Input actions:** extend the template `.inputactions` vs. a focused new asset (I'll recommend
  in Phase 2).
- **Beat detection tuning** is empirical — expect a short tuning pass with you in Phase 5.
- **Enemy parameter for Mapping 2** (speed vs. scale) — decided live in Phase 6 by what looks best.
- **Audio format** — you'll confirm your track format in Phase 5 so I give correct import settings.
```
