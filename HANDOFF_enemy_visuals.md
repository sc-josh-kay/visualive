# Handoff — Enemy Visuals & Music-Reactive Behavior

Context for picking this work back up. For the whole-game architecture see [`README.md`](README.md); this doc covers what this thread built: **spec8 music-reactive enemy behavior** and a full **procedural visual redesign of all three enemies**, ending with the **Corruptor "black-hole vacuum."**

Unity project: [`play_visualizer/`](play_visualizer/) · Unity 6000.5.9f1 · URP 2D. Enemy code: `Assets/Scripts/Enemies/`. Shaders: `Assets/Shaders/`. Editor setup: `Assets/Scripts/Editor/`.

---

## ⚠️ How to rebuild the enemies in the Editor (read this first)

Enemy prefabs are built by **re-runnable menu scripts**, and the order matters:

1. `PlayVisualizer → Build Enemies + Spawner (Phase 3)` — builds the **Color Eater** prefab + spawner.
2. `PlayVisualizer → Build Corruptor + Swarm (Phase 6)` — builds **Corruptor + Swarm** + writes the weighted spawn table.
3. **Save the scene** (Cmd+S).

**Phase 3 resets the spawner to Color-Eaters-only**, so you must run **Phase 6 AFTER Phase 3** or Corruptor/Swarm stop spawning. Most visual tweaks are runtime (recompile only), but anything touching serialized prefab/component defaults or the spawn table needs the relevant menu re-run. Each change note below says which.

Device builds: see the "Put it on iPhone" section in chat history — Unity → Xcode → device; custom shaders are kept via Always Included Shaders (the `FieldSplat` fix) or by being referenced from saved `.mat` assets.

---

## Enemy state (what each looks/behaves like now)

### Color Eater — `ColorEater.cs` + `ColorEaterVisualizer.cs`
"The music wrapped around a circle." A **geometry** visualizer (LineRenderers, not a shader quad):
- **3–5 thin rings**, each a dim baseline with **one bright waveform ARC** whose points are displaced by that ring's **frequency band** (ring 0 = bass … ring N = treble). Idle rings nearly vanish; active bands brighten/deform. Treble/flux add fine detail.
- Arcs are **distributed into separate sectors** and **sway in place** (bounded, no full-rotation drift so they never re-clump).
- **Fixed base color chosen at spawn** (from the music), and rings stay within a **restricted hue range** around it (`_hueRange`). Dim **music-colored center fill** (a soft glow SpriteRenderer) for readability.
- Behavior (spec8): Energy→speed, Beat→forward nudge + scale pulse.

### Corruptor — `Corruptor.cs` + `CorruptorVisualizer.cs` + `EnemyBlackHole.shader`
A **black hole** in a **teal / purple / pink** palette:
- Shader quad: dark center + **rotating spiral arms** (the spiral spins via `_Spin`; the rest holds its oblong tilt), brightening with bass.
- `CorruptorVisualizer` overlay: a **horizontal audio waveform** LineRenderer (spectrum × bass, purple) that does **not** rotate, plus **soft round speckle particles** that drift out and **burst on bass onsets**. (The two solid neon rings were removed for a cloud-like look.)
- **VACUUM (latest):** it now **sucks the smoke in like a drain** — see below.
- Per-enemy **oblong stretch + tilt** variety. **Capped at 6 alive** at once (hard enemy).
- Behavior (spec8): Bass→corruption size, bass onset→lunge + flare.

### Swarm — `SwarmUnit.cs` + `EnemyStar.shader`
Tiny **neon red-purple starfish/asterisk** whose arms **flutter** with treble/flux. Flocks (cohesion + separation), spawns in clouds. Larger/thicker after tuning (`Scale 0.55`, `_Sharp 2`).

---

## The Corruptor Vacuum (most recent feature)

The Corruptor pulls the player's painted smoke **in + swirling + eaten**, like water down a drain, growing with bass. Pipeline (mirrors the splat/distort seams):

`Corruptor.Behave` → `VisualizerField.Vacuum(pos, radius, strength, swirl)` → queued → `VisualizerCore.BuildVacuums` (world→viewport, cap 6) → `VacuumData` → `IVisualizerPattern.InjectVacuums` → `KaleidoscopePattern` sets `_Vacuums[6]/_VacSwirl[6]/_VacCount` on the smoke material → **`SmokeField.shader` advection**: within each radius it samples from further **out** (`_VacPull` → content pulled in) + tangential (`_VacSwirlAmt × signed swirl`) + **eats** (`_VacEat`, fades the field → coverage drops).

- **Bass grows/pulses the suck radius** (`radius × (1 + BassOnsetEnv·0.5)`, and `radius` already scales with bass); per-enemy random swirl direction.
- Replaced the old `Consume` for Corruptors (the vacuum's eat is the coverage loss).
- Tunables = `SmokeField.shader` defaults `_VacPull 0.028 / _VacSwirlAmt 0.05 / _VacEat 0.10`.

---

## spec8 — music-reactive behavior (also this thread)

Each enemy has a **musical personality**, bounded/readable (no random redirects, no collider scaling):
- Movement/visual reactions read `MusicState` directly (Color Eater=Energy/Beat, Corruptor=Bass, Swarm=Treble/Flux). Tunables on each `EnemyConfig` under "Music reaction".
- **Spawn composition** biased by music: `EnemySpawner.SpawnEntry` has a `MusicChannel` + `MusicInfluence`; `MusicMapper` pushes smoothed channel levels so bass-heavy songs → more Corruptors, treble-heavy → more Swarms, etc. Group **formations** also bias (bass tightens, treble disperses, energy → wave line).

See `spec8_music_reactive_enemy_behavior.md`.

---

## Key seams / where to look
- **`VisualizerField`** (Assets/Scripts/Visuals) — the only gameplay↔visualizer seam: `Consume`, `Paint`, `Distort`, **`Vacuum`**, `Coverage`, `HotWorldPos`, `PlayerPaintGain`.
- **`EnemyBase`** — shared plumbing: health, targeting, separation/jitter, `BeatEnv`/`BassOnsetEnv` envelopes, `UpdateVisual` hook, `SetVisualScale/Offset/BaseVisualScale/Rotation`, `CurrentColor` (music color for death), MaterialPropertyBlock helpers, `FindNearest`, `ApplyRadialDamage`.
- Enemy shaders: `EnemyBlackHole` (Corruptor), `EnemyStar` (Swarm), `EnergyTrail`/`EnergySprite` (Color-Eater rings + speckles). `EnemyRing.shader` is **retired** (Color Eater is geometry now) but left on disk.

## Gotchas learned this thread
- **Phase 3 wipes the spawn table** → always re-run Phase 6 after. (Phase 3 log now warns about this.)
- **`line` is a reserved word in Metal** — never name a shader variable `line` (broke `EnemyRing`). Watch `point`, `sample`, etc.
- **iOS shader stripping** — runtime `Shader.Find` shaders strip unless referenced by a saved material/scene or in Always Included Shaders (see `ios-shader-stripping` memory).
- LineRenderer width is in **world units** (not scaled by transform); useful for keeping lines thin as the enemy pulses.

## Tunable locations
- Color Eater look: `ColorEaterVisualizer` component (brightness, amp, `_hueRange`, center fill, ring count range in `ColorEater`).
- Corruptor look: `CorruptorVisualizer` (waveform, speckles, palette, `_outerRadius`), `EnemyBlackHole.mat` (arms/twist), `Corruptor._spiralSpin`, vacuum in `SmokeField.shader`.
- Swarm look: `EnemyStar.mat` (`_Sharp`), `SwarmConfig` (Scale), hue in `SwarmUnit`.
- Behavior/spawn: the three `*Config.asset`, `MusicMappingConfig`, and `EnemySpawner._entries` (channel/influence/MaxAlive/groups).

## Likely next threads
- **Swarm** hasn't had the full geometry/"alive" redesign treatment the other two got — probably next.
- Bring the organic/alive principles further into **weapon** look (partially done).
- **Weapon upgrade levels** (fire rate / projectile behavior / explosion complexity ladder) — long-standing open item.
- A **MusicDirector** (song-section events) — teed up by spec8's channel system.
- General **rebalance** now that visuals/behaviors are rich; verify device performance (many LineRenderers/particles).
