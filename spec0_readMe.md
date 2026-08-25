# Music-Driven Arcade Game

## Project Overview

This project is a personal game-development project built in **Unity**.

The core concept is:

> **The music is the level.**

The game is a minimalist, top-down arcade shooter inspired by **Geometry Wars** and classic music visualizers such as the old Apple/iTunes visualizer and MilkDrop.

The long-term vision is for a player to select a music track and have the game procedurally generate a unique audiovisual gameplay experience from that music. Music should eventually influence the environment, enemies, difficulty, gameplay events, colors, geometry, particles, and other visual effects.

For the initial MVP, keep the game intentionally simple. The purpose of the MVP is to prove that **music-driven gameplay is fun and visually compelling**, while establishing an architecture that can support the much larger vision later.

---

# Instructions for Claude Code

You are helping implement this project incrementally.

## Core Development Philosophy

Prioritize:

1. **Working gameplay over architectural complexity**
2. **Simple systems that can be expanded later**
3. **Fast iteration**
4. **Strong audiovisual feedback**
5. **Clear separation between systems**
6. **Minimal dependencies**
7. **Avoiding premature implementation of future features**

Do not implement the entire long-term vision up front.

When there are multiple reasonable implementation options, prefer the simplest one that preserves the ability to expand later.

Do not introduce abstractions solely for theoretical future flexibility. However, avoid tightly coupling the MVP to assumptions that are explicitly expected to change later.

---

# Technology

## Engine

Use **Unity**.

Use a current stable Unity version appropriate for a new personal project.

## Language

Use **C#**.

## Input

Use Unity's modern **Input System**.

## Rendering

Use the simplest rendering pipeline that supports the intended visual style.

The game should support:

* 2D/2.5D presentation
* particles
* glowing objects
* procedural geometry
* post-processing effects where useful
* shaders when they provide meaningful visual effects

Do not build a complex rendering framework during the MVP.

---

# Long-Term Vision

The eventual game should look conceptually like:

```text
                         MUSIC
                           |
                    AUDIO ANALYSIS
                           |
             +-------------+-------------+
             |             |             |
           Rhythm       Frequency      Energy
             |             |             |
             +-------------+-------------+
                           |
                     GAME DIRECTOR
                           |
        +------------------+------------------+
        |                  |                  |
     Gameplay            World            Visuals
        |                  |                  |
     Enemies            Geometry          Particles
     Weapons            Hazards            Shaders
     Events             Arenas             Effects
     Difficulty          Structures         Color
```

A song should eventually be capable of determining much more than visual effects.

For example:

* enemy density
* enemy behavior
* enemy speed
* world geometry
* obstacles
* game intensity
* color palette
* particle density
* camera behavior
* special events
* boss encounters
* gameplay pacing

The eventual experience should make different songs feel meaningfully different.

---

# MVP Scope

The MVP should implement:

* A top-down player-controlled ship/object
* Player movement
* Player aiming
* Player shooting
* One basic enemy type
* Enemy spawning
* Enemy/player interaction
* Health
* Score
* Game over/restart
* One or more local audio tracks
* Real-time audio analysis
* Bass/mid/treble values
* Overall audio energy
* Beat/onset detection
* Music-driven enemy spawning
* At least one additional music-driven gameplay parameter
* Audio-reactive visual effects
* Basic psychedelic/neon visual identity

The MVP does **not** need:

* Spotify integration
* Online functionality
* Multiplayer
* Multiple weapons
* Multiple enemy types
* Complex AI
* Procedurally generated levels
* Song-section recognition
* Automatic detection of verses/choruses/drops
* User accounts
* Save games
* Mobile support
* Sophisticated menus

---

# MVP Gameplay

The player exists inside a bounded top-down arena.

The player should be able to:

* move freely
* aim independently of movement
* shoot projectiles
* destroy enemies
* avoid enemies

The game should feel like a minimalist arcade game rather than a physics simulation.

Prioritize responsive controls.

The initial game loop is:

```text
Start
  ↓
Music begins
  ↓
Player enters arena
  ↓
Enemies spawn
  ↓
Player shoots enemies
  ↓
Music affects gameplay and visuals
  ↓
Player accumulates score
  ↓
Player eventually dies
  ↓
Game Over
  ↓
Restart
```

The music track itself should conceptually function as the level.

---

# Player

The player should initially be a simple visually distinctive object/ship.

Required:

* movement
* aiming
* shooting
* health

Possible future feature:

* dash

Do not implement dash unless it can be added very cheaply without distracting from the MVP.

### Player Design Principle

The player should be visually simple.

The environment, music, enemies, particles, and procedural effects should provide most of the visual complexity.

---

# Arena

The MVP should use a simple fixed arena.

The arena should:

* support unrestricted player movement within bounds
* provide enough space for enemies to approach from different directions
* have a dark/minimal background
* leave room for visualizer effects

Do not implement procedural level generation yet.

However, avoid making the player/gameplay architecture fundamentally dependent on the arena being static.

Future versions may transform or procedurally generate the arena based on music.

---

# Enemy

The MVP should contain **one enemy type**.

The enemy should:

1. Spawn at a reasonable distance from the player.
2. Move toward the player.
3. Damage the player when contacting them.
4. Be destroyed by player projectiles.
5. Award score when destroyed.

Keep enemy AI extremely simple.

The enemy spawning system should be separate from the enemy behavior.

Expose parameters such as:

```text
Spawn Rate
Maximum Enemies
Enemy Speed
Enemy Health
Spawn Distance
Damage
```

These parameters should eventually be controllable by the music system.

---

# Audio Architecture

Audio is a central part of the project.

For the MVP, use **local audio files**.

Do not implement Spotify.

The architecture should nevertheless avoid assuming that local files are the permanent source of music.

Future sources could include:

* Spotify
* user-selected files
* another streaming service
* generated music
* other audio sources

The game should therefore conceptually separate:

```text
Audio Source
     ↓
Audio Playback
     ↓
Audio Analysis
     ↓
Music State
     ↓
Game Systems
```

Do not allow gameplay systems to directly depend on the `AudioClip`.

---

# Audio Analysis

The MVP should provide a real-time representation of the current music state.

At minimum, expose:

```text
Bass
Mid
Treble
Energy
Beat
```

Prefer normalized values:

```text
0.0 → 1.0
```

where possible.

For example:

```csharp
public class MusicState
{
    public float Bass;
    public float Mid;
    public float Treble;
    public float Energy;
    public bool Beat;
}
```

The exact implementation is intentionally flexible.

Do not over-engineer audio analysis.

Perfect beat detection is not required for the MVP.

The system should prioritize:

* stability
* responsiveness
* low latency
* visually interesting results

---

# Music State

Create a central representation of the current musical state.

Conceptually:

```text
Audio Analyzer
       ↓
   MusicState
       ↓
+------+------+------+------+
|      |      |      |      |
Bass  Mid  Treble Energy  Beat
```

Gameplay and visual systems should consume `MusicState`.

They should not perform their own FFT/audio analysis.

This is an important architectural boundary.

---

# Music → Gameplay

The MVP should demonstrate that music directly changes gameplay.

Start with a very small number of mappings.

## Mapping 1: Energy → Spawn Rate

Higher musical energy should produce more enemies.

Conceptually:

```text
Low energy    → few enemies
High energy   → many enemies
```

## Mapping 2: Energy/Bass → Enemy Behavior

Use bass or energy to influence one enemy parameter, such as:

* movement speed
* scale
* spawn frequency

Choose whichever produces the most compelling result during implementation.

## Mapping 3: Beat → Event

A detected beat should trigger some gameplay event.

For example:

* enemy spawn
* temporary enemy burst
* projectile/event
* other simple arcade event

The implementation should be easy to replace later.

---

# Music → Visuals

The game should visibly respond to the music even when the player does nothing.

This is essential.

The MVP should implement several simple effects.

Potential mappings:

### Bass

* background pulse
* radial pulse
* camera scale
* large geometric effect

### Mid

* environmental movement
* geometry scale
* enemy glow

### Treble

* particle intensity
* sparks
* trails
* small visual details

### Beat

* flash
* shockwave
* particle burst

Do not implement all possible effects.

Choose a small set that produces a strong visual result.

---

# Visual Style

Target aesthetic:

> **Retro-futuristic psychedelic arcade visualizer**

Reference points:

* Geometry Wars
* Apple/iTunes visualizers
* MilkDrop
* neon vector graphics
* psychedelic computer graphics
* old-school screensavers

Visual characteristics:

* dark background
* bright colors
* glowing objects
* procedural geometry
* particles
* trails
* bloom/glow
* radial effects
* rhythmic motion
* strong beat synchronization

Avoid spending time creating traditional art assets.

The MVP's visual identity should come primarily from:

* procedural effects
* particles
* simple geometry
* shaders
* lighting/glow
* animation driven by music

---

# Scoring

Implement a basic score.

At minimum:

```text
Enemy destroyed → score increases
```

Keep scoring independent from audio analysis.

A future system may add:

* beat kills
* combos
* rhythm accuracy
* phrase bonuses
* survival bonuses
* multipliers

Do not implement these unless they are trivial.

---

# Health and Game Over

Use a simple health model.

For example:

```text
Player Health = 100

Enemy Contact = -25

Health <= 0
    ↓
Game Over
```

Game over should display:

```text
GAME OVER

Score: XXXX

Restart
```

Restarting should reset the game state and begin the selected track again.

---

# Configuration

Prefer data-driven configuration for tunable gameplay values.

Where appropriate, use Unity `ScriptableObject` assets.

Potential configuration objects:

```text
PlayerConfig
EnemyConfig
SpawnerConfig
MusicMappingConfig
VisualizerConfig
```

Do not create a configuration class for every possible object simply for abstraction.

Use ScriptableObjects when they provide clear benefits:

* easy tuning
* separation of data and behavior
* reusable configurations
* inspector-based experimentation

---

# Music Mapping Architecture

One of the most important long-term foundations is a reusable music-to-parameter mapping system.

Conceptually:

```text
MusicState
    |
    +-- Bass ------→ Camera Zoom
    |
    +-- Bass ------→ Enemy Size
    |
    +-- Energy ----→ Spawn Rate
    |
    +-- Energy ----→ Enemy Speed
    |
    +-- Treble ----→ Particle Intensity
    |
    +-- Beat ------→ Shockwave
```

The implementation does not need to support arbitrary mappings in the first version.

However, avoid hard-coding all music relationships directly into unrelated systems.

For example, avoid:

```csharp
if (audioAnalyzer.bass > 0.8f)
{
    // change enemy
}
```

inside the enemy class.

Prefer:

```text
Audio Analysis
      ↓
Music State
      ↓
Music Mapping / Controller
      ↓
Gameplay / Visual Systems
```

This allows the mapping system to become significantly more sophisticated later.

---

# Future Procedural Game Director

This is **not an MVP feature**, but the architecture should leave room for it.

The eventual game may have a higher-level `GameDirector` that interprets the music and decides what should happen.

For example:

```text
Music
  ↓
Analysis
  ↓
Song Structure
  ↓
Game Director
  ↓
"Intensity = 0.85"
"SpawnRate = 4.0"
"WorldComplexity = 0.7"
"EventProbability = 0.9"
```

The director could eventually understand:

* intros
* builds
* drops
* verses
* choruses
* breakdowns
* transitions

and use those structures to create intentionally dramatic gameplay.

Do not implement this now.

---

# Future Procedural World

Eventually, the fixed MVP arena could evolve into a music-generated world.

Potential future elements:

* geometric arenas
* tunnels
* obstacles
* waves
* moving walls
* radial structures
* procedural formations
* environmental hazards
* boss encounters

The eventual goal is:

> **A song should be capable of generating its own playable world.**

Do not implement this in the MVP.

---

# Future Spotify / Music Integration

Spotify integration is intentionally deferred.

Do not make Spotify a dependency of the core game architecture.

The MVP should prove the game concept using local audio files first.

Later, investigate what music services and APIs permit with respect to:

* playback
* audio access
* audio analysis
* synchronization
* game/visual synchronization
* licensing

Do not assume that a streaming service will permit the game to analyze or synchronize its audio.

The core game should remain functional without any streaming service.

---

# Code Organization

Use a clean, intuitive project structure.

A reasonable starting point:

```text
Assets/
├── Audio/
│   ├── Tracks/
│   └── Analysis/
│
├── Materials/
│
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Projectiles/
│   └── Visuals/
│
├── Scenes/
│
├── Scripts/
│   ├── Audio/
│   ├── Player/
│   ├── Enemies/
│   ├── Gameplay/
│   ├── Visuals/
│   └── UI/
│
├── ScriptableObjects/
│
└── Tests/
```

This structure is a starting point, not a rigid requirement.

Keep related code together and avoid creating deeply nested folders unnecessarily.

---

# Development Process

Implement the MVP incrementally.

Do not attempt to implement everything in one pass.

Recommended order:

## Phase 1 — Unity Foundation

* Create Unity project
* Establish folder structure
* Create initial scene
* Configure input
* Create basic game loop

## Phase 2 — Player

* Create player
* Movement
* Aiming
* Shooting
* Basic camera

## Phase 3 — Enemy

* Create enemy
* Enemy movement
* Projectile collision
* Damage
* Enemy destruction
* Basic spawning

## Phase 4 — Game Loop

* Health
* Score
* Game over
* Restart

At this point, the game should already be playable **without music-driven behavior**.

## Phase 5 — Audio

* Add local track
* Audio playback
* Audio analysis
* MusicState

## Phase 6 — Music-Driven Gameplay

* Energy → enemy spawn rate
* Music parameter → enemy parameter
* Beat → gameplay event

## Phase 7 — Visualizer

* Background response
* Beat effects
* Particles
* Geometry
* Glow/bloom
* Additional audio-reactive effects

## Phase 8 — Polish

Only after the above works:

* tune gameplay
* tune audio mappings
* improve visual effects
* improve UI
* improve feel
* test multiple tracks

---

# Development Rules for Claude Code

## 1. Keep the MVP small

If a proposed feature is not necessary to prove the core concept, defer it.

## 2. Do not over-engineer

This is a personal project.

Prefer:

```text
Simple + understandable + extensible
```

over:

```text
Highly abstract + theoretically scalable
```

## 3. Preserve architectural boundaries

Especially:

```text
Audio Source
    ↓
Audio Analysis
    ↓
Music State
    ↓
Music Mapping
    ↓
Game/Visual Systems
```

Do not bypass these boundaries without a good reason.

## 4. Make things tunable

Music-driven effects will require significant experimentation.

Expose important parameters so they can be tuned quickly.

## 5. Favor visual feedback

When implementing a music-driven feature, make the response obvious enough that it can be evaluated immediately.

## 6. Don't polish prematurely

First establish:

```text
Does it work?
    ↓
Is it fun?
    ↓
Does the music make it better?
    ↓
Does it look cool?
    ↓
Polish
```

## 7. Keep dependencies minimal

Avoid third-party packages unless there is a compelling reason to use them.

## 8. Explain architectural decisions

When making a meaningful architectural choice, briefly explain:

* what was chosen
* why
* what alternatives were considered
* whether it affects future extensibility

Do not produce excessive documentation for trivial decisions.

---

# Definition of Done for MVP

The MVP is complete when a user can:

1. Launch the game.
2. Start a game.
3. Hear a local music track.
4. Control the player.
5. Shoot enemies.
6. Destroy enemies.
7. Accumulate score.
8. Take damage and eventually die.
9. Restart the game.
10. See the game respond visibly to the music.
11. Experience enemy spawning/intensity changing with the music.
12. See strong visual effects synchronized to musical events.

Most importantly:

> **Playing the game should feel meaningfully different from playing the same shooter with ordinary background music.**

The music should feel like an active part of the game.

---

# Guiding Question

Whenever deciding whether something belongs in the MVP, ask:

> **Does this help prove that music can become gameplay?**

If yes, prioritize it.

If no, defer it unless it is required infrastructure.

---

# End State of MVP

The desired MVP experience is something approximately like:

```text
                  MUSIC STARTS
                       ↓
              Dark empty arena
                       ↓
             Subtle visual motion
                       ↓
                First beat hits
                       ↓
             Visual pulse / event
                       ↓
              Enemy appears
                       ↓
                 Player shoots
                       ↓
             Music intensifies
                       ↓
           More enemies + effects
                       ↓
               Music drops
                       ↓
             Game becomes quiet
                       ↓
                 Beat returns
                       ↓
             Visual/gameplay burst
                       ↓
                 Player dies
                       ↓
                  GAME OVER
```

It should feel like a **playable music visualizer**, not simply a shooter with a reactive soundtrack.

The MVP should establish that feeling while keeping the implementation small enough that the next iteration can radically expand the procedural music/gameplay relationship.
