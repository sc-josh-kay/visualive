# README — Music-Reactive Enemy Behavior MVP

## Goal

Make the **music meaningfully change how enemies behave**, not just how they look.

The three existing enemy types should each have a distinct **musical personality**:

* **Color Eater → Rhythm / Energy**
* **Corruptor → Bass**
* **Swarm → Treble / Spectral Flux**

The player should be able to feel that different songs create different enemy ecosystems and therefore require different movement/combat strategies.

Do **not** add new enemy types or weapons in this spec.

---

## Design Principle

Music should influence four things:

1. **Visual response** — enemies visibly react to their musical characteristic.
2. **Movement** — music subtly changes how they move.
3. **Spawning** — music changes which enemies appear and how they are grouped.
4. **Death** — existing persistent paint explosions reinforce the musical identity.

Music must **modify predictable behavior**, not introduce arbitrary/random behavior that makes enemies difficult to read.

The player's core objective remains:

> **Protect and maximize the painted visualizer.**

Enemies remain threats to color, and killing them remains a major way to restore color.

---

# 1. Color Eater — Rhythm / Energy

Existing role:

> Basic, mobile enemy that seeks dense painted areas and chases the player when close.

### Music inputs

Primary:

* `Energy`
* `Beat`
* `OnsetStrength`

### Visual

Subtle rhythmic pulse:

* Scale/glow/aura briefly increases on `Beat`.
* Pulse should be noticeable but not large enough to obscure its normal shape.
* No gameplay collider scaling.

### Movement

Music should make Color Eaters feel more energetic without making them unpredictable.

* Higher `Energy` → modestly higher movement speed.
* Strong `Beat`/onset → very small forward movement impulse.
* Preserve existing targeting and steering behavior.

### Spawning

Color Eaters remain the baseline/common enemy.

* Low-energy sections → lower population.
* High-energy sections → increased population.
* Avoid spawning a new enemy on every beat.

### Death

Keep the existing death explosion system.

Allow the existing music-reactive explosion to reinforce the rhythmic identity, but **do not create a separate explosion system**.

---

# 2. Corruptor — Bass

Existing role:

> Slow, tanky enemy that entrenches, creates expanding corruption, and produces a large death explosion.

The Corruptor should feel like the **low-frequency / heavy instrument** of the enemy system.

### Music inputs

Primary:

* `Bass`
* Bass onset strength
* `Beat`

### Visual

Strong but smooth bass pulse:

* Body scale/glow responds to `Bass`.
* Strong bass events produce a more pronounced pulse.
* Internal/corruption animation can breathe with bass.
* Collider remains unchanged.

Desired feeling:

> **Bass hits → the Corruptor visibly "thumps."**

### Movement

Bass should influence movement without turning it into a rhythm enemy.

* Normal movement remains slow and predictable.
* Strong bass onset can produce a small forward lunge/impulse.
* Bass should not directly cause random direction changes.
* Preserve existing targeting/entrenchment behavior.

### Corruption

Make the existing corruption feel synchronized with bass.

* Corruption continues expanding according to its existing rules.
* Bass can temporarily increase the corruption radius/strength.
* Strong bass events can create a small outward corruption pulse.
* Keep the effect bounded so bass cannot cause runaway coverage loss.

Desired gameplay relationship:

> **Bass → Corruptor pulse → corruption expands → player needs to respond.**

### Spawning

Increase Corruptor prevalence during bass-heavy sections.

* Low bass → rare.
* Sustained/high bass → more frequent.
* Avoid spawning directly on every bass hit.
* Existing song progression/difficulty limits remain authoritative.

### Death

Corruptor death should remain the largest enemy paint payoff.

* Existing `Bass → explosion radius` relationship should be preserved.
* Strong bass should make the death feel especially impactful.

---

# 3. Swarm — Treble / Spectral Flux

Existing role:

> Weak flocking enemies that are dangerous through numbers.

The Swarm should feel like the **high-frequency / chaotic instrument**.

### Music inputs

Primary:

* `Treble`
* `SpectralFlux`
* `OnsetStrength`

### Visual

High-frequency agitation:

* Small rapid scale/position vibration.
* Increased jitter at high `Treble`.
* Increased visual agitation with `SpectralFlux`.
* Keep the swarm's overall silhouette readable.

Desired feeling:

> **High frequencies make the swarm "buzz."**

### Movement

Music should affect **agitation**, not basic targeting.

Normal:

```text
strong flock cohesion
        ↓
     ● ● ●
    ● ● ● ●
```

High treble:

```text
weaker cohesion
        ↓
  ●    ●
     ●      ●
 ●       ●
```

Implementation:

* Preserve existing flock target/steering behavior.
* Increase individual movement perturbation as `Treble` rises.
* Reduce flock cohesion slightly at high `Treble`.
* Increase agitation further with `SpectralFlux`.
* Clamp perturbation so individual enemies never become unreadable or uncontrollable.

### Spawning

Increase Swarm prevalence during treble-heavy sections.

* Low treble → smaller/less frequent swarms.
* High treble → larger or more frequent swarm groups.
* High flux can favor more fragmented/scattered groups.
* Continue using existing swarm group-spawn infrastructure.

### Death

Preserve existing individual enemy death behavior.

Multiple simultaneous swarm deaths should naturally create a rapid sequence of paint explosions.

---

# 4. Music-Driven Spawn Composition

The biggest gameplay change should be **enemy composition**, not simply enemy speed.

The existing `EnemySpawner` should derive a small set of music-responsive spawn weights.

Conceptually:

```text
MusicState
    ↓
MusicMapper / MusicDirector
    ↓
Enemy spawn weights
    ↓
EnemySpawner
```

Example:

| Musical Character | Color Eater | Corruptor | Swarm |
| ----------------- | ----------: | --------: | ----: |
| Low energy        |           ↓ |        ↓↓ |     ↓ |
| High energy       |           ↑ |         ↑ |     ↑ |
| Bass-heavy        |           → |        ↑↑ |     → |
| Treble-heavy      |           → |         ↓ |    ↑↑ |
| High flux         |           → |         → |     ↑ |

These should modify the **existing weighted spawn table**, not replace it.

Existing constraints remain:

* Song progression
* Enemy unlocks
* On-screen cap
* Spawn rate
* Group spawning

Music should bias the composition within those constraints.

---

# 5. Musical Spawn Formations

For the MVP, add only simple formation bias.

### Bass-heavy

Favor concentrated/heavy groups:

```text
   C C
  C C C
   C C
```

### Treble-heavy

Favor dispersed swarm groups:

```text
S       S

   S

       S    S
```

### High rhythmic energy

Favor more wave-like groups:

```text
S S S S S
    ↓
S S S S S
```

Do not build a sophisticated formation-generation system yet. Simple spawn-position/group biases are sufficient.

---

# 6. Musical State

Do **not** add FFT processing to enemies.

Continue using:

```text
AudioAnalyzer
      ↓
MusicState
      ↓
MusicMapper / MusicDirector
      ↓
Enemies
```

`MusicState` remains the only source of music analysis.

Use existing fields wherever possible:

* `Energy`
* `Bass`
* `Treble`
* `SpectralFlux`
* `OnsetStrength`
* `Beat`

If smoothing is needed for enemy behavior, add it in the music-mapping layer rather than modifying `AudioAnalyzer`.

---

# 7. Readability Rules

Music-reactive behavior must follow these rules:

### Do

* Make changes gradual and bounded.
* Make musical responses visually obvious.
* Preserve enemy targeting.
* Preserve recognizable enemy roles.
* Use music to create different tactical situations.

### Do not

* Randomly redirect enemies based on music.
* Make enemies teleport or suddenly accelerate.
* Change collision sizes with visual pulses.
* Make every beat spawn enemies.
* Allow music modifiers to overwhelm the existing difficulty system.

The player should think:

> "The swarm is getting frantic because the music got intense."

Not:

> "Why did that enemy suddenly do something random?"

---

# 8. MVP Acceptance Criteria

The implementation is successful when:

### Different songs produce different enemy populations

A bass-heavy song should naturally produce more Corruptor pressure.

A treble-heavy song should naturally produce more Swarm pressure.

### Enemies visibly communicate their musical response

* Color Eater → rhythmic/energy pulse.
* Corruptor → bass pulse.
* Swarm → treble/flux jitter.

### Music changes actual gameplay

* Corruptors become more threatening during bass-heavy sections.
* Swarms become more difficult to contain during treble-heavy sections.
* Color Eaters become more active during energetic sections.
* Spawn composition changes throughout a song.

### Existing gameplay remains intact

* Enemies still target painted areas.
* Enemies still consume coverage.
* Enemy deaths still restore coverage.
* No player damage/death is introduced.
* Existing weapons continue to work.
* Existing visualizer architecture remains unchanged.

---

## Implementation philosophy

**Keep this MVP deliberately small.**

We are testing one hypothesis:

> **Can music become a meaningful source of enemy behavior and tactical variety?**

Do not build full song-section detection, machine-learning classification, new enemy types, or complex procedural formations yet.

If this prototype makes two songs **feel noticeably different to play**, that becomes the foundation for a later `MusicDirector` system that can coordinate larger musical gameplay events.
