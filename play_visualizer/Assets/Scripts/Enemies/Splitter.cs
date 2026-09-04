using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Enemy — the Splitter (spec10): a relatively weak seeker that becomes dangerous when ignored,
    /// because KILLING IT SHATTERS IT INTO 2–3 smaller Splitters (one generation only — fragments
    /// never split again). The shatter also fractures/consumes some paint and throws a teal + deep
    /// purple burst, and each fragment bursts outward from the death point before settling into its
    /// seek. Spectral flux drives the fractured-ring visual's instability (on SplitterVisualizer),
    /// never the AI. No direct player damage (contact = inherited "puff of blackness").
    /// </summary>
    public class Splitter : EnemyBase
    {
        [Tooltip("The Splitter prefab spawned as fragments on death (wired to itself by Phase 10).")]
        [SerializeField] private Splitter _fragmentPrefab;

        private static readonly Color DeepPurple = new Color(0.5f, 0.12f, 0.9f, 1f);
        private static readonly Color Teal = new Color(0.25f, 1f, 0.8f, 1f);

        private int _generation;   // 0 = parent, 1 = fragment (won't split)
        private float _coastTimer; // fragments coast outward from the shatter before seeking

        /// <summary>Turn a freshly-spawned copy into a fragment: smaller, one-generation, bursting out.</summary>
        public void MarkAsFragment(Vector2 outwardDir, float popSpeed, float scale, float coastTime)
        {
            _generation = 1;
            transform.localScale = Vector3.one * scale; // OnEnable already set full scale; shrink it
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) _rb.linearVelocity = outwardDir * popSpeed;
            _coastTimer = coastTime;
        }

        protected override void Behave(float dt)
        {
            Vector2 pos = transform.position;

            // Just after the shatter, coast outward (AI suspended) so the burst reads, then settle.
            if (_coastTimer > 0f)
            {
                _coastTimer -= dt;
                _rb.linearVelocity *= 0.9f;
                return;
            }

            // Weak seeker: head for dense paint, with anti-clumping separation.
            Vector2 seek = SmokeTargetWorld() - pos;
            if (seek.sqrMagnitude > 1e-6f) seek.Normalize();
            Vector2 sep = SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            Vector2 dir = seek + sep;
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier);

            // Erodes a little paint while alive (the real threat is being ignored → splitting).
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(
                    pos, _config.ConsumeRadius, _config.ConsumeStrengthPerSecond * dt);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            // Teal identity — the death firework inherits this; the fractured-ring animation and the
            // flux-driven instability live on SplitterVisualizer (which reads the music itself).
            CurrentColor = Teal;
        }

        protected override void OnDeath(Vector2 at, MusicState s)
        {
            if (_generation >= 1) return; // one generation only — fragments don't split

            // Fracture: the shatter consumes some paint.
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(at, _config.SplitConsumeRadius, _config.SplitConsumeStrength);
            }

            // Deep-purple accent burst layered over the teal death firework → the teal+purple shatter.
            SpawnCollisionBurst(at, DeepPurple, _config.ShatterBurstRadius, 0.45f, 0f);

            // Shatter into 2–3 fragments that burst outward from the death point — but never past the
            // total Splitter cap (this parent is about to be destroyed, so it frees one slot). Bounds
            // the self-replication so the population (and its per-frame visual cost) can't balloon.
            if (_fragmentPrefab == null) return;
            int desired = Random.Range(_config.SplitCountMin, _config.SplitCountMax + 1);
            int aliveExcludingSelf = CountAlive(typeof(Splitter)) - 1;
            int slots = Mathf.Max(0, _config.SplitterMaxAlive - aliveExcludingSelf);
            int count = Mathf.Min(desired, slots);
            if (count <= 0) return;
            float fragScale = _config.Scale * _config.FragmentScale;
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                Vector2 outward = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 spawnPos = at + outward * _config.SplitSpread;
                Splitter child = Instantiate(_fragmentPrefab, spawnPos, Quaternion.identity);
                child.SetTarget(_target); // fragments bypass the spawner, so hand them the player target
                child.MarkAsFragment(outward, _config.FragmentPopSpeed, fragScale, _config.FragmentCoastTime);
            }
        }
    }
}
