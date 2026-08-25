The player controls:

* **Left stick:** movement
* **Right stick:** aim
* **Weapon:** automatically fires according to its behavior

That actually gives us a nice constraint: weapons should be differentiated by **how they paint the scene while automatically firing**, rather than by different firing inputs.

I would replace the Charge Cannon with something that works naturally in this control scheme.

# README — Weapon System MVP

## Purpose

Implement six distinct auto-fire weapons for the mobile twin-stick shooter.

The core design principle is:

> **The player isn't just shooting enemies. They are using weapons to paint, disturb, and reshape the music visualizer.**

The weapons should therefore differ in both **combat behavior** and **how they contribute to the visual scene**.

The game runs as an **auto-fire twin-stick shooter**:

```text
Left Stick  → Movement
Right Stick → Aim
Weapon      → Automatically fires
```

There should be no weapon behavior requiring the player to press, hold, or release a fire button.

For desktop testing:

**Space bar cycles through weapons.**

No progression/unlock system yet.

---

# 1. Weapon Set

Implement these six weapons:

1. **Pulse Stream** — precision
2. **Scatter** — wide-area paint
3. **Growth Stream** — projectiles that grow over distance
4. **Rapid Stream** — escalating fire rate
5. **Homing Swarm** — autonomous targeting
6. **Vortex Wave** — crowd control + visualizer manipulation

These provide six genuinely different behaviors:

```text
Pulse Stream    → precision
Scatter         → area
Growth Stream   → distance
Rapid Stream    → volume
Homing Swarm    → targeting
Vortex Wave     → control
```

---

# 2. Common Weapon Architecture

Create a modular weapon system.

```text
WeaponController
├── PulseStream
├── Scatter
├── GrowthStream
├── RapidStream
├── HomingSwarm
└── VortexWave
```

All weapons:

* automatically fire
* use the player's current aim direction
* consume `MusicState`
* have their own projectile/effect behavior
* contribute to the visualizer
* can damage enemies through the existing damage system

Do not put weapon-specific audio analysis into individual weapons.

---

# 3. Pulse Stream

### Role

The baseline weapon.

**Precision / reliable damage.**

This is the existing single-stream shooter and should remain as the control/reference weapon.

### Behavior

* Fires continuously in the aim direction.
* One projectile per shot.
* Relatively fast fire rate.
* Moderate damage.
* Projectiles disappear on impact or lifetime expiration.

### Visualizer behavior

Each projectile leaves a short-lived glowing filament.

Music affects the visual expression:

* `Energy` → brightness
* `Treble` → filament detail
* `Bass` → impact size
* `Beat` → impact flash

### Painting metaphor

**Drawing with a fine glowing pen.**

The player can literally "draw" through the visualizer while moving and shooting.

---

# 4. Scatter

### Role

**Area damage / crowd clearing.**

### Behavior

Automatically fires a burst of several projectiles in a spread.

Example:

```text
          ●
       ●  ●  ●
    ●    ●    ●
          ↑
        PLAYER
```

Each automatic firing cycle produces a shotgun-like fan.

Suggested starting point:

* 5–7 projectiles
* moderate spread
* moderate projectile speed
* lower damage per projectile

### Music integration

* `Bass` → projectile size
* `Energy` → brightness
* `Treble` → trail complexity
* `Beat` → stronger burst effect

### Painting metaphor

**Splattering paint across the canvas.**

The overlapping projectile trails should create interesting patterns in the visualizer.

---

# 5. Growth Stream

### Role

**Long-range / sustained-fire weapon.**

This replaces the charge weapon.

### Behavior

Automatically fires a continuous stream of projectiles, but each projectile **grows as it travels**.

```text
PLAYER → · → ● → ◉ → ◎
```

The projectile should start small and become progressively larger.

Potential behavior:

* damage increases with projectile age/distance
* maximum size is capped
* projectile eventually disappears

This creates an interesting tactical tradeoff:

> Keep enemies at range and let the projectile become powerful.

### Music integration

* `Bass` → growth rate / maximum size
* `Energy` → glow
* `Spectral brightness` → color
* `Beat` → temporary growth pulse

### Painting metaphor

**Throwing expanding drops of paint into the distance.**

The trail should become increasingly substantial as the projectile travels.

---

# 6. Rapid Stream

### Role

**Volume / aggressive sustained fire.**

This is the weapon that explores the "increase shot rate" idea.

### Behavior

Automatically fires continuously, with the fire rate gradually increasing while the player maintains an active firing direction.

Example:

```text
slow → faster → faster → FASTER
```

If the player stops aiming or the weapon is inactive, the fire-rate ramp resets or decays.

Do not require a fire button.

The right-stick aiming state is sufficient to determine whether the player is actively firing.

### Music integration

* `Energy` → maximum fire-rate multiplier
* `Beat` → temporary rate burst
* `Treble` → projectile visual detail
* `Bass` → impact size

### Painting metaphor

**Increasing the brush stroke frequency until the entire scene becomes saturated.**

This weapon should feel particularly good during energetic sections of a song.

---

# 7. Homing Swarm

### Role

**Multi-target / low-aim-pressure weapon.**

### Behavior

Each automatic firing cycle launches multiple small projectiles.

Instead of traveling straight, each projectile searches for a nearby enemy and curves toward it.

```text
              ●
             ↗
PLAYER → ● → ●
             ↘
              ●
```

Suggested starting behavior:

* 5–8 projectiles per burst
* small projectile size
* low/moderate damage
* limited lifetime
* moderate homing strength

The projectiles should **visibly curve**, rather than instantly snapping to enemies.

### Music integration

* `Treble` → projectile count/detail
* `Mid` → curvature/trail complexity
* `Energy` → brightness
* `Beat` → launch burst

### Painting metaphor

**Painting a web of lines through the scene.**

The trajectories themselves should be beautiful and visually important.

When many projectiles converge on enemies, this should create a temporary visual focal point.

---

# 8. Vortex Wave

### Role

**Crowd control + visualizer manipulation.**

This should be the most unusual weapon.

### Behavior

Automatically launches a slow-moving visual projectile/wave.

When the wave reaches a location, it creates a temporary vortex.

The vortex:

* pulls nearby enemies inward
* optionally slows them
* damages enemies inside it
* has a limited lifetime
* disappears after its lifetime

The exact physics can remain simple.

### Visualizer interaction

This weapon should strongly affect the visualizer.

The vortex creates:

* radial distortion
* swirling visualizer geometry
* particle attraction
* trail distortion
* expanding pulses

Music controls the visual character:

* `Bass` → vortex radius/strength
* `Energy` → intensity
* `Treble` → filament detail
* `Spectral flux` → turbulence
* `Beat` → periodic expansion

### Painting metaphor

**Swirling the wet paint.**

Rather than adding a new mark, the weapon grabs the existing visualizer and **distorts it**.

This makes it fundamentally different from the projectile-based weapons.

---

# 9. Music Integration Principle

Music should generally influence **how weapons look and paint**, rather than making their combat behavior unpredictable.

For example:

```text
Music
  ↓
Weapon behavior
       +
Visual expression
```

But avoid:

```text
Bass suddenly changes weapon targeting
Treble randomly changes fire direction
Beat prevents firing
```

The player should always understand what their weapon is doing.

Music should make the weapon **feel alive**.

---

# 10. Weapon → Visualizer Relationship

Every weapon should contribute to the scene.

| Weapon        | Painting behavior                  |
| ------------- | ---------------------------------- |
| Pulse Stream  | Fine glowing lines                 |
| Scatter       | Paint splatter / multiple strokes  |
| Growth Stream | Expanding strokes                  |
| Rapid Stream  | Dense continuous painting          |
| Homing Swarm  | Network of curved strokes          |
| Vortex Wave   | Distorts and swirls existing paint |

This is more important than simply giving every weapon a different projectile sprite.

---

# 11. Enemy Interaction

Enemy destruction is already part of the visualizer loop.

Enemies currently:

```text
Enemy
  ↓
Smoke absorption
  ↓
Visualizer becomes less visible
```

Killing them:

```text
Enemy destroyed
       ↓
Smoke absorber removed
       ↓
Visualizer revealed
       ↓
Explosion / smoke generated
       ↓
New visual event
```

Therefore, **enemy clearing itself is a form of painting the scene**.

Weapon effects should reinforce this.

### Enemy explosions

Enemy explosions should:

* create a satisfying burst
* generate smoke/particles using the existing system
* briefly disturb the visualizer
* respond to music intensity

For example:

```text
Bass ↑
  ↓
larger explosion

Energy ↑
  ↓
brighter explosion

Beat
  ↓
stronger visual pulse
```

Do not change the fundamental enemy mechanics in this weapon implementation.

---

# 12. Auto-Fire Requirements

All six weapons must work naturally with continuous auto-fire.

The weapon system should not assume:

```text
Fire button pressed
Fire button released
```

Instead:

```text
Update
  ↓
Is player actively aiming?
  ↓
Weapon's fire timer
  ↓
Fire automatically
```

The weapon's `fireRate` and firing behavior determine what happens.

Weapons may have:

* continuous streams
* bursts
* alternating shots
* projectile waves
* periodic effects

But none should require a discrete fire-button action.

---

# 13. Desktop Testing

For development:

**Space bar cycles weapons.**

Display a temporary debug label:

```text
WEAPON: PULSE STREAM
```

When Space is pressed:

```text
PULSE STREAM
↓
SCATTER
↓
GROWTH STREAM
↓
RAPID STREAM
↓
HOMING SWARM
↓
VORTEX WAVE
↓
PULSE STREAM
```

The mobile control scheme should not be modified to accommodate weapon switching yet.

---

# 14. Implementation Priority

Implement in this order:

### Phase 1

Refactor the existing shooter into a reusable weapon architecture.

### Phase 2

Implement:

1. Pulse Stream
2. Scatter
3. Growth Stream

Verify the auto-fire architecture.

### Phase 3

Implement:

4. Rapid Stream
5. Homing Swarm

### Phase 4

Implement:

6. Vortex Wave

### Phase 5

Tune visualizer integration across all six.

Do **not** spend significant time balancing damage, cooldowns, or progression yet.

---

# 15. Definition of Done

The MVP is successful when I can:

1. Move with the left stick.
2. Aim with the right stick.
3. Automatically fire each weapon.
4. Press Space to cycle weapons during desktop testing.
5. Immediately understand how each weapon differs.
6. See every weapon contribute visually to the scene.
7. See music affect the visual character of every weapon.
8. See enemy destruction reveal more of the underlying visualizer.
9. See enemy explosions create additional visual activity.
10. Feel that weapons are **painting/manipulating the visualizer**, rather than simply shooting enemies over a background.

### Guiding principle

> **Every weapon should answer the question: "What different way does this weapon let me paint the music into the world?"**

That should be the design test for any future weapon we add.
