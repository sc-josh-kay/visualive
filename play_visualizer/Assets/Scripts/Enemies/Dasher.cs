using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Enemy — the Dasher (spec10): a fast, DIRECTIONAL enemy that cuts a destructive trench through
    /// the player's paint. It roams toward dense paint, and on a BEAT/onset it charges (a readable
    /// telegraph), locks a direction, then DASHES across the paint — stamping a narrow consume trench
    /// along its path. BASS = how powerful (dash speed/length + cut radius). No direct player damage
    /// (contact uses the inherited "puff of blackness"). The oscilloscope-blade visual is on
    /// <see cref="DasherVisualizer"/>; this drives movement, the cut, and orientation.
    ///
    ///   Beat → WHEN it attacks · Bass → HOW powerful.
    /// </summary>
    public class Dasher : EnemyBase
    {
        private enum State { Roam, Charge, Dash, Recover }

        private State _state = State.Roam;
        private float _stateTimer;
        private float _cooldownTimer;
        private Vector2 _dashDir = Vector2.right;
        private float _dashBass;
        private float _headingDeg;
        private Vector2 _lastDashPos;

        private DasherVisualizer _viz;

        protected override void Awake()
        {
            base.Awake();
            _viz = GetComponentInChildren<DasherVisualizer>();
        }

        protected override void Behave(float dt)
        {
            Vector2 pos = transform.position;
            MusicState s = Music;

            switch (_state)
            {
                case State.Roam:
                {
                    // Approach dense paint (what it wants to slice); orient toward it.
                    Vector2 dir = SmokeTargetWorld() - pos;
                    if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
                    _headingDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier);

                    _cooldownTimer -= dt;
                    // Beat gates the attack; bass (sampled now) sets its power.
                    bool trigger = s != null && (s.Beat || s.BassOnset);
                    if (trigger && _cooldownTimer <= 0f)
                    {
                        _dashDir = dir;
                        _dashBass = s != null ? Mathf.Clamp01(s.Bass) : 0f;
                        EnterState(State.Charge);
                    }
                    break;
                }
                case State.Charge:
                {
                    // Wind up in place (energy compressing), facing the locked dash direction.
                    _rb.linearVelocity = Vector2.zero;
                    _headingDeg = Mathf.Atan2(_dashDir.y, _dashDir.x) * Mathf.Rad2Deg;
                    if (_stateTimer >= _config.ChargeDuration)
                    {
                        _lastDashPos = pos;
                        EnterState(State.Dash);
                    }
                    break;
                }
                case State.Dash:
                {
                    float power = 1f + _dashBass * _config.BassDashBoost;
                    _rb.linearVelocity = _dashDir * (_config.DashSpeed * power);
                    _headingDeg = Mathf.Atan2(_dashDir.y, _dashDir.x) * Mathf.Rad2Deg;

                    CutTrench(_lastDashPos, pos, dt);   // the destructive slice
                    _lastDashPos = pos;

                    if (_stateTimer >= _config.DashDuration) EnterState(State.Recover);
                    break;
                }
                case State.Recover:
                {
                    _rb.linearVelocity *= 0.88f; // ease out of the dash
                    if (_stateTimer >= _config.RecoverDuration)
                    {
                        _cooldownTimer = _config.DashCooldown;
                        EnterState(State.Roam);
                    }
                    break;
                }
            }

            _stateTimer += dt;
        }

        private void EnterState(State next)
        {
            _state = next;
            _stateTimer = 0f;
        }

        /// <summary>
        /// Stamp a narrow consume trench from <paramref name="from"/> to <paramref name="to"/> — a few
        /// interpolated discs so the trench is continuous regardless of dash speed / frame rate.
        /// </summary>
        private void CutTrench(Vector2 from, Vector2 to, float dt)
        {
            VisualizerField field = VisualizerField.Instance;
            if (field == null) return;

            float radius = _config.DashConsumeRadius * (1f + _dashBass * _config.BassConsumeBoost);
            float strength = _config.DashConsumeStrengthPerSecond * dt; // per-second, dt-scaled
            float dist = (to - from).magnitude;
            int steps = Mathf.Clamp(Mathf.CeilToInt(dist / Mathf.Max(0.05f, radius * 0.5f)), 1, 6);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                field.Consume(p, radius, strength);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            SetVisualRotation(_headingDeg);

            float charge = _state == State.Charge && _config.ChargeDuration > 0.001f
                ? Mathf.Clamp01(_stateTimer / _config.ChargeDuration) : 0f;
            float dashT = _state == State.Dash && _config.DashDuration > 0.001f
                ? Mathf.Clamp01(_stateTimer / _config.DashDuration) : 0f;

            // Purple at rest → pink during the dash (death explosion inherits this).
            float pinkMix = Mathf.Clamp01(0.25f * charge + dashT);
            Color c = Color.HSVToRGB(Mathf.Lerp(0.76f, 0.92f, pinkMix), 0.85f, 1f);
            CurrentColor = c;

            if (_viz != null)
            {
                DasherVisualizer.Phase phase =
                    _state == State.Charge ? DasherVisualizer.Phase.Charge :
                    _state == State.Dash ? DasherVisualizer.Phase.Dash :
                    _state == State.Recover ? DasherVisualizer.Phase.Recover :
                    DasherVisualizer.Phase.Roam;
                _viz.SetDynamics(phase, charge, dashT, c);
            }
        }
    }
}
