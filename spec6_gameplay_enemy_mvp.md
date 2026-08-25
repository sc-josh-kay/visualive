# README — Gameplay & Enemy MVP

## Purpose

Extend the existing music-visualizer framework into the first playable version of the game.

The core concept is:

> **The player is not trying to survive. They are trying to keep the world beautiful.**

The player paints the world through movement and combat. Enemies continuously consume that visualizer and turn areas black. Destroying enemies creates large bursts of color.

The game is played for the **entire duration of a song**. There is no player death and no traditional game-over state.

---

# 1. Core Gameplay Loop

```text
             MOVE
               ↓
        Paint the world
               ↓
       Increase coverage
               ↓
          Score rises
               ↑
               │
       Kill enemies
               ↑
               │
        ┌──────┴──────┐
        │             │
      AIM           SHOOT
        │             │
        └──────→ Destroy
                   enemies
                       ↓
               Color explosion
                       ↓
                Increase coverage


Enemies continuously:
        ↓
   Consume color
        ↓
   Create black space
        ↓
   Reduce coverage
        ↓
   Reduce score rate
```

The player therefore has two primary ways to create visualizer coverage:

1. **Movement/trail**
2. **Enemy destruction**

---

# 2. Coverage Is the Core Game State

Create a concept of **Visualizer Coverage**.

Coverage represents how much of the active play area currently contains meaningful visualizer content.

```text
100% = almost entirely filled with visualizer
 50% = roughly half visualizer / half black
  0% = entirely black
```

Coverage must be **current**, not historical.

Visualizer content naturally fades over time.

Therefore:

```text
Player moves
    ↓
Creates color
    ↓
Color fades
    ↓
Eventually becomes black
```

Enemies accelerate this process.

---

# 3. Scoring

The primary score should continuously increase based on current coverage.

Conceptually:

```text
scoreRate = f(coverage)
```

Higher coverage should produce disproportionately higher score rates.

For the MVP, use a simple nonlinear curve such as:

```text
scoreRate ∝ coverage²
```

This is tunable and should not be hard-coded throughout gameplay code.

The important behavior is:

> Maintaining 90% coverage should be substantially more valuable than maintaining 50%.

This creates a reason to actively defend the visualizer.

---

# 4. Enemy Purpose

Enemies **cannot damage or kill the player**.

They exist to attack the visualizer.

Their primary behavior is:

> **Find, consume, corrupt, or otherwise reduce visualizer coverage.**

The player therefore shoots enemies because they threaten the player's score and the visual state of the world.

Enemies should interact with the visualizer field rather than simply being rendered on top of it.

---

# 5. MVP Enemy Roster

Start with **three enemy types**.

Do not implement a large enemy ecosystem yet.

## Enemy 1 — Color Eater

The basic enemy.

Behavior:

* Moves toward nearby/high-value visualizer regions.
* Consumes visualizer coverage as it moves.
* Leaves a small blackened region behind it.
* Does not target or damage the player.

Conceptually:

```text
Before:

████████████
████████████
████████████

Enemy enters:

██████░█████
█████░●░████
██████░█████
```

Purpose:

* Establish the fundamental threat.
* Force the player to keep moving.
* Provide a simple target to shoot.

---

# 6. Enemy 2 — Corruptor

A slower, more dangerous territorial enemy.

Behavior:

* Moves slowly or remains relatively stationary.
* Creates an expanding area of visualizer decay/blackness.
* Can dramatically reduce coverage if ignored.
* Preferably targets a high-value region rather than the player.

Example:

```text
████████████████
██████░░░███████
████░░●░░░░█████
██████░░░███████
████████████████
```

The corrupted region should visibly grow.

Purpose:

> Create a "deal with this now" threat.

Killing it should produce a substantially larger visual explosion than a basic Color Eater.

---

# 7. Enemy 3 — Swarm

A group of small enemies that behave as a collective.

Behavior:

* Individual units are weak.
* Seek dense/valuable visualizer regions.
* Consume small amounts individually.
* Become dangerous through numbers.
* Killing individual swarm members produces small color bursts.
* Clearing an entire swarm produces a satisfying visual event.

Purpose:

* Increase visual density.
* Create rapid targeting decisions.
* Produce large music-synchronized visual moments when many are destroyed.

The swarm should feel more like a **cloud of visualizer-eating organisms** than conventional enemies.

---

# 8. Enemy Destruction

Enemy death is a major part of the visualizer.

When an enemy dies:

```text
Enemy
  ↓
Explosion
  ↓
Large visualizer injection
  ↓
Coverage increases
  ↓
Score rate increases
```

The explosion should not simply be a conventional particle effect.

It should use the existing visualizer system.

The explosion should:

* generate color
* create pattern fragments
* potentially create a ripple
* respond to the current `MusicState`
* leave temporary visualizer residue

Larger/more difficult enemies should produce larger visualizer events.

---

# 9. Enemy → Visualizer Interaction

Create a clean interface between enemies and the visualizer.

Conceptually:

```text
Enemy
  ↓
Consume(position, radius, strength)
  ↓
Visualizer / Trail Field
```

and:

```text
Enemy Death
  ↓
Paint(position, radius, intensity, MusicState)
  ↓
Visualizer / Trail Field
```

Gameplay code should **not directly manipulate shader/rendering implementation details**.

The visualizer owns the visual field.

Enemies request visual effects.

---

# 10. Player Controls

Use the existing twin-stick abstraction.

```text
Left stick  → movement
Right stick → aim
Fire        → shoot
```

For desktop/controller:

* Left stick → move
* Right stick → aim
* Fire button/trigger → shoot

For future iPhone:

* Left touch zone → move
* Right touch zone → aim
* Automatic fire while aiming

The gameplay layer should consume an abstract `PlayerInput` rather than directly depending on a particular input device.

---

# 11. Player Shooting

Keep shooting extremely simple for the MVP.

The player should have:

* continuous projectile firing
* a fixed/moderate fire rate
* projectiles that destroy enemies
* no ammunition
* no reload
* no weapon switching

The purpose of shooting is primarily:

> **Remove threats to the visualizer.**

Do not spend time building a complex weapon system yet.

---

# 12. Player Movement → Visualizer

The existing trail system remains the primary movement-based source of color.

```text
Player movement
      ↓
Trail
      ↓
Persistent visualizer field
      ↓
Coverage
      ↓
Score
```

The player should be incentivized to move around the entire play area rather than remain stationary.

The trail should continue to use the previously established:

* decay
* reinforcement
* music-responsive visual properties
* psychedelic pattern generation

---

# 13. Enemy Spawning

For the MVP, use a simple continuous spawning system.

Do **not** implement traditional Geometry Wars waves yet.

Instead:

```text
Song starts
    ↓
Small number of enemies
    ↓
Continuous spawning
    ↓
Intensity gradually increases
    ↓
Song progresses
    ↓
More/different enemies
```

The exact spawn system should be tunable.

Eventually, spawning can respond to:

* song energy
* beats
* song sections
* current coverage
* player performance

But the MVP only needs a simple intensity curve.

---

# 14. Music Integration

Use the existing `MusicState`.

The gameplay system should eventually be able to respond to:

* `Energy`
* `Bass`
* `Mid`
* `Treble`
* `Beat`
* future onset/section features

For the MVP, use music primarily to influence:

### Spawn intensity

Higher musical energy → more enemy activity.

### Enemy events

Beats can synchronize:

* spawning
* movement bursts
* death explosions
* visual effects

### Visual effects

Enemy destruction should inherit the current visualizer state.

Do not add sophisticated song-section analysis in this phase.

---

# 15. No Player Death

There is deliberately **no losing**.

The player:

* cannot take damage
* cannot die
* cannot lose the song
* plays until the song ends

The consequence of poor play is simply:

```text
More black
    ↓
Lower coverage
    ↓
Lower score rate
```

This is central to the game's identity.

The game should feel challenging without feeling punishing.

---

# 16. Song Completion

When the song ends:

1. Stop spawning enemies.
2. Allow a short visual/audio outro.
3. Calculate/display final score.
4. Display useful summary statistics.

MVP summary could include:

```text
FINAL SCORE

Peak Coverage       94%
Average Coverage    72%
Enemies Destroyed   143
Song                <track name>
```

A high score should be the primary persistent achievement.

---

# 17. Difficulty Philosophy

The goal is **not survival difficulty**.

Instead:

> Difficulty determines how difficult it is to maintain high visualizer coverage.

A player who ignores enemies can continue playing, but their score should deteriorate.

A skilled player can maintain high coverage and achieve a much higher score.

This should create a satisfying gradient:

```text
Casual play
    ↓
Pretty visualizer
    ↓
Moderate score


Active play
    ↓
High coverage
    ↓
Frequent kills
    ↓
High score


Expert play
    ↓
Near-continuous coverage
    ↓
Efficient enemy targeting
    ↓
Spectacular visual chaos
    ↓
Very high score
```

---

# 18. MVP Architecture

Build on the existing systems:

```text
                    MusicAnalyzer
                         ↓
                     MusicState
                         ↓
              ┌──────────┴──────────┐
              ↓                     ↓
         Visualizer            GameplayDirector
              ↑                     ↓
              │              ┌──────┴──────┐
              │              ↓             ↓
         TrailSystem      Player        Enemies
              ↑              │             │
              │              ↓             ↓
              └──────── PlayerInput    Enemy Events
```

### Visualizer

Owns:

* visual patterns
* trail field
* coverage calculation
* ripple system
* visual effects

### Gameplay

Owns:

* player
* projectiles
* enemies
* spawning
* score
* game/song state

### Music Analyzer

Only analyzes audio and exposes `MusicState`.

Do not put gameplay logic into `AudioAnalyzer`.

---

# 19. MVP Development Order

Implement in this order.

### Phase 1 — Coverage

* Establish measurable visualizer coverage.
* Confirm trail decay changes coverage.
* Confirm player movement increases coverage.
* Display coverage/debug information.

### Phase 2 — Score

* Implement continuous score accumulation.
* Tie score rate to coverage.
* Tune nonlinear coverage curve.

### Phase 3 — Basic Enemy

Implement Color Eater:

* spawning
* movement toward visualizer
* visualizer consumption
* shooting
* destruction
* color explosion

### Phase 4 — Corruptor

Add:

* area-based corruption
* larger threat
* larger death explosion

### Phase 5 — Swarm

Add:

* multiple small enemies
* collective movement
* individual death effects

### Phase 6 — Music Integration

Connect enemy activity and destruction to `MusicState`.

### Phase 7 — Song Loop

Implement:

```text
Start song
    ↓
Play
    ↓
Continuous enemy activity
    ↓
Song ends
    ↓
Final score
```

---

# 20. Definition of Done

The MVP is successful when we can:

* Start a predefined music track.
* Move the player around using twin-stick controls.
* Paint the world using the existing visualizer/trail system.
* Watch painted regions naturally fade.
* See a measurable coverage percentage.
* Accumulate score based on coverage.
* Spawn Color Eaters that consume visualizer content.
* Shoot and destroy them.
* See their deaths create large psychedelic visualizer explosions.
* Spawn Corruptors that threaten larger areas.
* Spawn Swarms that create many small threats.
* Play continuously without the possibility of player death.
* Reach the end of the song and receive a final score.
* Clearly understand that **color = score and enemies = threats to color**.

### The fundamental test

Put on a song and ask:

> **Is it satisfying to fly around painting the world, while constantly deciding which enemies need to be killed to keep the world colorful?**

If yes, we have the core game.

Everything else—more enemy types, sophisticated music-driven spawning, combos, bosses, weapons, procedural song analysis, Spotify integration, etc.—can be built on top of this foundation later.
