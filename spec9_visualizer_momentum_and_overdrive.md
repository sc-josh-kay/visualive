# README — Visualizer Momentum & Overdrive MVP

## Goal

Add a **Visualizer Momentum → Overdrive** system that creates tension without turning the game into a traditional survival game.

The player still **cannot die** and enemies still cannot damage the player.

Instead, maintaining a high level of visualizer coverage builds momentum. Reaching maximum momentum transforms how the player paints the world and temporarily puts them into **Overdrive**.

The important design principle is:

> **Normal play: the player paints the music through movement.**
> **Overdrive: the player becomes a music-powered paint emitter.**

Overdrive should feel like a fundamentally more powerful and spectacular way to interact with the visualizer, not simply a collection of stat buffs.

---

# 1. Visualizer Momentum

Add a gameplay resource:

```text
VisualizerMomentum: 0–100
```

Momentum is driven primarily by **current visualizer coverage**.

### Coverage relationship

| Coverage | Momentum             |
| -------- | -------------------- |
| `< 50%`  | Drains               |
| `50–80%` | Approximately stable |
| `> 80%`  | Fills                |
| `100%`   | Enter Overdrive      |

Suggested starting values:

```text
Momentum drain below 50%:   -8 / sec
Momentum neutral 50–80%:     0 / sec
Momentum gain above 80%:    +10 / sec
```

All values should be configurable.

Coverage should be lightly smoothed for the purpose of momentum calculation so tiny GPU readback fluctuations don't cause visible instability.

---

# 2. Momentum Is Not Health

This is explicitly **not a life bar**.

If momentum reaches zero:

* The player does not take damage.
* The player does not die.
* The song does not end.
* No game-over state occurs.

The consequence of low coverage is simply:

> **The player loses access to the Overdrive reward.**

This should create pressure without anxiety.

---

# 3. Entering Overdrive

When:

```text
VisualizerMomentum >= 100
```

immediately enter:

```text
OVERDRIVE
```

Suggested duration:

```text
8 seconds
```

Make the duration configurable.

When Overdrive begins:

```text
VisualizerMomentum = 0
```

The player immediately begins building momentum toward the next Overdrive while the current Overdrive is active.

However, do not allow another Overdrive to trigger while already in Overdrive.

If momentum reaches 100 during Overdrive, clamp it at 100 until Overdrive ends.

---

# 4. Overdrive Is a Different Painting Mode

The most important part of this feature:

**Do not implement Overdrive simply as `PlayerPaintGain × 2`.**

Normal painting and Overdrive painting should behave differently.

### Normal

The player acts like a brush:

```text
                  movement →
                         🚀
                 ░░░░░░░
              ░░░░░░░░░░░
           ░░░░░░░░░░░░░░░
```

The player primarily creates a persistent trail behind itself.

### Overdrive

The player becomes a **music-powered paint emitter**:

```text
                 ~~~~~
             ~~~       ~~~
          ~~~     🚀      ~~~
             ~~~       ~~~
                 ~~~~~
```

Paint should continuously emit around the player, while discrete music events create stronger outward waves.

This makes Overdrive immediately recognizable as a different gameplay state.

---

# 5. Radial Paint Emission

During Overdrive, add a radial painting system centered on the player.

Instead of painting primarily behind the player:

> **Paint is emitted in all directions around the player.**

The emission should be persistent enough to meaningfully increase coverage, but should not instantly fill the entire screen.

Suggested starting behavior:

* Radial emission continuously paints around the player.
* Movement trail remains active at reduced/enhanced strength.
* Radial emission is affected by music.
* Emission is constrained by the existing visualizer field system.

### Movement should still matter

Do not completely replace the player trail.

Suggested conceptual balance:

```text
Normal:
    Movement = primary paint source

Overdrive:
    Radial music emission = primary paint source
    Movement trail = secondary paint source
```

The player should still have a reason to move and position themselves intelligently.

---

# 6. Music-Powered Paint Waves

The radial emitter should generate **expanding waves of paint** in response to music.

On a strong beat/onset:

```text
             ~~~
          ~~~   ~~~
        ~~         ~~
       ~     🚀      ~
        ~~         ~~
          ~~~   ~~~
             ~~~
```

The wave expands outward from the player like a **ripple in water**.

The expanding edge deposits paint into the visualizer field.

After passing, the wave dissipates naturally into the existing smoke/visualizer system.

Multiple waves should be able to exist simultaneously.

This should create moments like:

```text
Beat 1 →   (   🚀   )

Beat 2 →  ((   🚀   ))

Beat 3 → (((   🚀   )))

Beat 4 → (((( 🚀 ))))
```

The persistent paint from each wave can overlap and build increasingly complex visual patterns.

---

# 7. Bass Controls Wave Strength

Use the existing `MusicState.Bass`.

Bass should primarily control **how powerful the radial paint wave is**.

For example:

```text
Weak bass:
       ( 🚀 )

Strong bass:
    ((( 🚀 )))
```

Bass can control:

* maximum wave radius
* paint intensity
* radial emission strength
* waveform/ripple amplitude

Do not simply scale the player itself with bass.

The goal is:

> **Bass determines how much paint the player can push into the world.**

---

# 8. Beat / Onset Triggers the Wave

Use the existing onset/beat detection for discrete pulse events.

Conceptually:

```text
Bass     = wave strength
Beat     = wave timing
```

On each qualifying beat/onset:

1. Sample the current bass strength.
2. Create a new radial paint wave.
3. Set its radius/intensity based on music.
4. Expand the wave outward.
5. Deposit paint along the wavefront.
6. Fade the wave after it reaches its maximum radius.

Avoid generating a new expensive object every frame.

Use a lightweight pooled or reusable wave implementation.

---

# 9. Overdrive Should Help Recover Coverage

The radial painting is not purely cosmetic.

It should be a meaningful **coverage recovery mechanism**.

A player who has entered Overdrive should be able to use the music-powered waves to reclaim areas that enemies have consumed.

This creates an important gameplay loop:

```text
Enemies destroy paint
       ↓
Coverage falls
       ↓
Player fights back
       ↓
Kills create paint
       ↓
Coverage rises
       ↓
Momentum fills
       ↓
OVERDRIVE
       ↓
Music-powered radial painting
       ↓
Rapidly reclaim the visualizer
```

The player should feel a strong transition from **struggling to maintain the visualizer** to **actively rebuilding it.**

---

# 10. Overdrive Should Not Directly Kill Enemies

Keep the roles distinct.

**Weapons:**

> Kill enemies.

**Enemy deaths:**

> Create persistent paint.

**Overdrive:**

> Create massive amounts of paint.

Do not make the Overdrive radial pulse directly damage enemies in this MVP.

This keeps the system focused on the game's central objective: **painting the scene.**

---

# 11. Overdrive Player Appearance

The player should have an unmistakable visual state while in Overdrive.

Add a glowing energy effect around the ship.

The effect should feel like the player is **charged with the same energy that is creating the paint waves.**

Possible components:

* soft outer glow
* animated halo
* small orbiting particles
* subtle energy tendrils
* bass-reactive brightness
* pulsing ring around the player

Conceptually:

```text
                 ✦
             ✦       ✦
                ▲
             ✦ / \ ✦
               /___\
             ✦       ✦
                 ✦
```

Do not obscure the player's ship silhouette.

The player should remain immediately readable during combat.

---

# 12. Beat-Synchronized Player Pulse

When a paint wave is emitted:

1. Briefly intensify the player's glow.
2. Emit the radial wave.
3. Return toward the normal Overdrive glow.

This creates a clear visual connection:

```text
MUSIC
  ↓
BEAT
  ↓
PLAYER CHARGES
  ↓
💥 RADIAL PAINT WAVE
  ↓
VISUALIZER EXPANDS
```

The player should feel like the **source of the musical energy**.

---

# 13. Overdrive Visualizer Intensity

Overdrive should also amplify existing visualizer effects, but avoid creating a completely separate visual system.

Potential effects:

* stronger music-reactive pattern response
* stronger ripple effects
* more intense player paint
* stronger enemy death explosions
* slightly more pronounced kaleidoscope/swirl behavior

The radial paint waves should be the **primary new visual effect**.

---

# 14. Existing Rewards During Overdrive

Retain the previously defined rewards:

### Player paint

Increase the effectiveness of the player's overall painting.

### Enemy death paint

Increase persistent paint generated by enemy deaths.

Suggested starting multiplier:

```text
EnemyDeathPaint × 1.5
```

### Score

Increase score generation.

Suggested starting multiplier:

```text
Score × 2.0
```

These should stack with the existing combo multiplier.

All values should be configurable.

---

# 15. HUD

Add a **Visualizer Momentum** bar to the existing HUD.

Example:

```text
VISUALIZER MOMENTUM

██████████████████░░
```

The player should be able to intuitively understand:

* low momentum → need to improve coverage
* high momentum → close to reward
* full momentum → Overdrive

During Overdrive, replace or transform the bar into an obvious:

```text
OVERDRIVE
████████░░
```

indicating remaining Overdrive time.

Avoid cluttering the screen with additional numbers.

---

# 16. Architecture

Create a dedicated controller rather than embedding the system into `PlayerTrail` or `ScoreManager`.

Suggested structure:

```text
VisualizerField
      │
      │ Coverage
      ↓
VisualizerMomentum
      │
      ├── Normal
      │
      └── Overdrive
              │
              ├── RadialPaintEmitter
              ├── PaintWaveSystem
              ├── Player Overdrive VFX
              ├── Paint multipliers
              └── Score multiplier
```

`VisualizerMomentum` should own the state transition.

`RadialPaintEmitter` should own radial painting.

`PaintWaveSystem` should own discrete expanding waves.

The existing `VisualizerField` remains the seam through which painting reaches the visualizer.

Do not allow these systems to manipulate the RenderTexture directly.

---

# 17. Performance Requirements

The game is targeting iPhone.

Avoid:

* per-frame allocation
* creating/destroying wave GameObjects
* new RenderTextures for each pulse
* CPU simulation of thousands of particles
* expensive per-pixel CPU operations

Prefer:

* pooled wave objects
* reusable data structures
* existing `VisualizerField.Paint(...)`
* GPU-based visual effects where appropriate
* a small number of lightweight wave instances

The visualizer should remain responsive at the existing target frame rate.

---

# 18. MVP Acceptance Criteria

The feature is complete when:

1. Visualizer coverage drives a `0–100` Momentum value.
2. Momentum drains below 50% coverage.
3. Momentum is stable between 50–80%.
4. Momentum builds above 80%.
5. Reaching 100% enters Overdrive.
6. Reaching zero never causes damage or game-over.
7. Overdrive lasts approximately 8 seconds.
8. Momentum resets when Overdrive begins.
9. Overdrive cannot chain directly into another Overdrive.
10. Normal movement-based painting continues to function.
11. Overdrive adds persistent **radial painting around the player**.
12. Beat/onset events generate expanding paint waves.
13. Bass controls the strength/size of those waves.
14. Multiple waves can overlap.
15. The waves meaningfully help recover coverage.
16. Overdrive increases enemy-death paint.
17. Overdrive increases score.
18. The player has a clear glowing Overdrive visual state.
19. The player glow responds to the music/wave events.
20. The HUD clearly communicates Momentum and Overdrive.
21. No existing enemy behavior or weapon behavior is broken.
22. The implementation remains performant on iPhone.

---

# Design Principle

The entire feature should reinforce this transition:

```text
NORMAL PLAY

Player = Brush

Movement
   ↓
Trail
   ↓
Paint
   ↓
Maintain the visualizer


OVERDRIVE

Player = Instrument

Music
   ↓
Player
   ↓
Radial Paint Waves
   ↓
Visualizer expands
   ↓
Coverage surges
   ↓
Massive score
```

**Overdrive should feel like the moment the player stops merely keeping the visualizer alive and starts actively playing the music through it.**
