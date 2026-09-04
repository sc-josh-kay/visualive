# README — Dasher, Splitter & Swarm Enemy MVP

## Goal

Add/refine three enemy types that fit the game's visual language:

> **Enemies are forces that fight against the player's visualizer. Their visual design should communicate both how they behave and how they interact with the music.**

Each enemy should have a distinct geometric language while sharing the game's overall aesthetic of **thin luminous structures, dark interiors, organic motion, and music-reactive behavior**.

### Enemy visual language

| Enemy       | Geometry                  | Musical influence | Visual/gameplay identity |
| ----------- | ------------------------- | ----------------- | ------------------------ |
| Color Eater | Concentric waveform rings | Bass / music      | Absorbs                  |
| Corruptor   | Spiral / vortex           | Bass              | Vacuums                  |
| Swarm       | Tiny waveform particles   | Treble            | Vibrates                 |
| Dasher      | Waveform ribbon/blade     | Beat              | Cuts                     |
| Splitter    | Broken waveform arcs      | Spectral flux     | Fractures                |

---

# 1. Dasher

## Identity

The Dasher is a **fast, directional enemy that cuts through the player's painting**.

Its visual design should immediately communicate:

> **This thing has a direction.**

Unlike the circular Color Eater or amorphous Corruptor, the Dasher should be elongated and directional.

---

## Visual Design — "Oscilloscope Blade"

Build the Dasher primarily from a **thin luminous waveform stretched into a long ribbon or blade**.

Conceptually:

```text
          ~~~~~~~
       ~~~       ~~~
  ~~~~~             ~~~~~
                         >
```

The enemy has a small, sharp geometric core at the leading edge, with 2–3 thin waveform traces extending behind it.

The waveform should remain thin. Avoid turning it into a solid filled projectile.

### Normal movement

The waveform gently oscillates:

```text
~~~~~ ~~~~ ~~~~~ ~~~
```

The Dasher should feel like a piece of the visualizer that has become directional and alive.

### Charge

During its charge/telegraph:

* waveform compresses
* oscillation becomes more organized
* amplitude increases
* core becomes brighter

Conceptually:

```text
~~~~~ ~~~~ ~~~ ~~ ~
        ↓
~~~~~~~~~~~~~~~~~~~
        ↓
          >
```

This visually communicates that energy is being compressed before release.

### Dash

During the actual dash, the waveform stretches dramatically behind the enemy:

```text
<~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                               >
```

The dash should leave a brief visual streak and create a **narrow destructive trench through the player's paint**.

The visual effect should make the gameplay immediately understandable:

> **The Dasher is literally slicing the visualizer.**

---

## Music Modulation

The Dasher is primarily **beat-driven**.

### Beat

A beat/onset should cause the Dasher to:

1. Compress its waveform.
2. Briefly brighten/flash.
3. Launch into its dash.

### Bass

Bass controls the strength of the waveform:

* bass amplitude
* dash destructive radius
* potentially dash speed/length

Strong bass should make the Dasher visibly more powerful without fundamentally changing its behavior.

The intended relationship is:

```text
Beat → WHEN it attacks
Bass → HOW powerful it is
```

---

# 2. Splitter

## Identity

The Splitter is a relatively weak enemy that becomes more dangerous when ignored.

Its defining gameplay characteristic:

> **Killing it creates more enemies.**

Its visual design should communicate fragmentation and instability.

---

## Visual Design — "Fractured Waveform"

The Splitter should look like a **waveform ring that has broken apart**.

It should share some visual DNA with the Color Eater without looking like another Color Eater.

### Color Eater

```text
     ~~~~~~~~~
   ~~         ~~
  ~             ~
   ~~         ~~
     ~~~~~~~~~
```

### Splitter

```text
      ~~~~~
   ~~~     ~~
             \
   ~         /
    ~~     ~
```

The Splitter consists of several **thin, disconnected luminous waveform arcs** surrounding a dark center.

The arcs should not be uniform. They should feel like pieces that were once part of a coherent object.

---

## Movement

The fragments should subtly shift relative to one another.

For example:

```text
       ~~~
    ~~     ~~~
          ●
   ~~~       ~
       ~~~
```

The pieces can:

* rotate slightly
* wobble
* change separation
* vary waveform amplitude

This should create a feeling that the enemy is struggling to remain together.

Do not make the actual gameplay movement random. The visual instability can be much stronger than the AI movement.

---

## Music Modulation

The Splitter is primarily influenced by **spectral flux / musical change**.

### Low flux

The fragments remain relatively stable.

### High flux

The fragments become increasingly agitated:

* increased jitter
* greater relative rotation
* greater separation
* stronger waveform amplitude

The relationship should be:

> **The more the music changes, the less stable the Splitter becomes.**

This should be visually subtle most of the time, with stronger reactions during major musical transitions.

---

## Death / Split Animation

The death animation should visually communicate that the waveform is **shattering**.

```text
          ✦
      ~~~   ~~~
    ~~   ●     ~~
       / | \
   ~~~   |   ~~~
       \ | /
          ✦
```

The parent Splitter breaks into 2–3 pieces.

Those pieces then become the smaller Splitter enemies.

The player should be able to understand the entire mechanic visually:

> **Waveform → shatter → fragments → new enemies**

For the MVP, allow only **one generation of splitting**. Fragments should not recursively split.

---

# 3. Swarm

## Identity

The Swarm represents **high-frequency musical energy**.

Rather than looking like a collection of conventional tiny spaceships, the individual enemies should feel like **small pieces of audio/static that have become alive**.

The visual identity should be:

> **Treble = nervous energy.**

---

## Visual Design — "Waveform Particles"

Each Swarm unit should be extremely small and primarily composed of:

* a tiny dark/bright core
* a thin irregular waveform or arc
* a small amount of luminous energy

Conceptually:

```text
·  ˙  · '  ·  ˙  ·
```

Avoid giving every unit an identical sprite.

Small variations in waveform shape, orientation, and scale should make the group feel organic.

The overall swarm should read as a **cloud of tiny high-frequency signals**.

---

## Treble Response

Treble should primarily influence the Swarm's **visual agitation**.

### Low treble

The units move with relatively gentle jitter:

```text
·   ·    ·
   ·  ·
 ·    ·   ·
```

### High treble

The units become much more nervous:

```text
·˙·'·˙·'·˙·
  ·˙·'·˙·'
·'·˙·'·˙·'·
```

Increase:

* jitter frequency
* waveform vibration
* small rotational movement
* local separation/recombination

The entire group should begin to look almost like **musical static** during high-frequency sections.

Again, keep the actual AI movement readable and deterministic. The music should primarily modulate the motion and appearance.

---

# 4. Shared Visual Rules

All three enemies should share the same rendering philosophy.

### Thin luminous structures

Avoid large solid sprites.

The player's visualizer can become extremely colorful, so thin structures provide better readability.

### Dark interiors

Where appropriate, use **dark/black negative space inside the enemy**.

This is particularly important for:

* Color Eater
* Splitter
* Corruptor

The enemy should remain visible even when surrounded by dense player paint.

### Music creates activity

Enemies should not constantly look maximally animated.

Most of the time they should be relatively restrained.

Musical events should cause the structure to come alive.

For example:

```text
Normal music
     ↓
subtle motion

Strong musical event
     ↓
visible response
```

This keeps the visualizer from becoming visual noise.

---

# 5. Gameplay ↔ Visual Design Principle

Whenever possible, the visual effect should **be the gameplay mechanic**, rather than being a cosmetic effect layered on top.

Examples:

### Color Eater

**Looks:** waveform rings
**Does:** absorbs the visualizer inside its rings

### Corruptor

**Looks:** black hole/vortex
**Does:** pulls the visualizer into a vortex

### Dasher

**Looks:** waveform blade
**Does:** cuts a destructive path through the visualizer

### Splitter

**Looks:** fractured waveform
**Does:** fractures into multiple enemies

### Swarm

**Looks:** high-frequency particles
**Does:** rapidly consumes/erodes the visualizer as a group

This should remain the guiding principle for future enemy designs.

---

# MVP Acceptance Criteria

### Dasher

* [ ] Thin, elongated waveform/ribbon visual.
* [ ] Clearly directional silhouette.
* [ ] Charge animation compresses/organizes waveform.
* [ ] Beat/onset drives the charge → dash event.
* [ ] Bass modulates waveform amplitude and/or dash strength.
* [ ] Dash creates a narrow destructive path through player paint.
* [ ] Clear enough to identify against dense visualizer coverage.
* [ ] No direct player damage.

### Splitter

* [ ] Thin fractured waveform arcs around a dark center.
* [ ] Visually related to Color Eater without being circularly identical.
* [ ] Fragments subtly move relative to one another.
* [ ] Spectral flux increases visual instability.
* [ ] Death visibly shatters the enemy.
* [ ] Shatter produces 2–3 smaller Splitters.
* [ ] Only one generation of splitting in the MVP.
* [ ] Split event damages/consumes some player paint.
* [ ] No direct player damage.

### Swarm

* [ ] Individual enemies read as tiny waveform/audio particles.
* [ ] Group remains clearly visible against the visualizer.
* [ ] Treble increases vibration/jitter.
* [ ] High treble can make the swarm visually resemble musical static.
* [ ] Music modulation does not make AI movement unpredictable.
* [ ] Swarm continues to threaten and consume player paint.
* [ ] No direct player damage.

## Overall Design Goal

The enemies should feel like they belong to the same strange musical ecosystem:

> **The player paints the music.
> The enemies are different forces within that music that fight back.**

Each new enemy should ideally introduce **a new geometric form, a new musical influence, and a new way of destroying the player's visualizer.**
