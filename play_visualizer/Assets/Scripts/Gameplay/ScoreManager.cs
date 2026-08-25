using System;
using UnityEngine;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Tracks the run's score. In the gameplay MVP the PRIMARY score source is continuous:
    /// score accrues every frame from current visualizer coverage on a nonlinear curve
    /// (scoreRate ∝ coverage^exponent, spec §3), so maintaining a full, beautiful world is worth
    /// disproportionately more than a half-black one. Enemy kills can still add a discrete bonus.
    ///
    /// It also records the run stats the end-of-song summary needs (peak/average coverage,
    /// enemies destroyed). A lightweight singleton so gameplay/UI can reach it without wiring.
    /// Reads coverage from VisualizerField; it never analyzes audio or renders anything.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Coverage → score (tunable, spec §3)")]
        [Tooltip("Score per second at 100% coverage.")]
        [SerializeField] private float _scorePerSecondMax = 1200f;
        [Tooltip("Nonlinearity. 2 = coverage²; higher rewards near-full coverage even more.")]
        [SerializeField] private float _coverageExponent = 2f;

        [Header("Combo (rapid kills → multiplier)")]
        [Tooltip("Seconds allowed between kills to keep the combo alive.")]
        [SerializeField] private float _comboWindow = 2.5f;
        [Tooltip("Multiplier gained per chained kill.")]
        [SerializeField] private float _comboStep = 0.25f;
        [SerializeField] private float _maxMultiplier = 4f;

        private int _combo;
        private float _lastKillTime = -999f;

        /// <summary>Current chained-kill count (0 = no active combo).</summary>
        public int Combo => _combo;

        /// <summary>Multiplier applied to enemy-death coverage and score (1 when no combo).</summary>
        public float Multiplier => _combo <= 0 ? 1f : Mathf.Min(_maxMultiplier, 1f + (_combo - 1) * _comboStep);

        public int Score { get; private set; }

        /// <summary>Current per-second scoring rate (for HUD/tuning).</summary>
        public float ScoreRate { get; private set; }

        // --- Run stats (for the end-of-song summary, spec §16) ---
        public float PeakCoverage { get; private set; }
        public float AverageCoverage => _coverageTime > 0f ? (float)(_coverageIntegral / _coverageTime) : 0f;
        public int EnemiesDestroyed { get; private set; }

        /// <summary>Fired with the new score whenever the integer score changes.</summary>
        public event Action<int> ScoreChanged;

        private double _scoreAccum;
        private double _coverageIntegral; // ∫ coverage dt, for the time-weighted average
        private double _coverageTime;      // total scored time

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Time.deltaTime is 0 while paused / in the menu (timeScale 0), so scoring naturally
            // only runs during active play.
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                ScoreRate = 0f;
                return;
            }

            float coverage = VisualizerField.Instance != null
                ? Mathf.Clamp01(VisualizerField.Instance.Coverage)
                : 0f;

            ScoreRate = _scorePerSecondMax * Mathf.Pow(coverage, _coverageExponent);
            _scoreAccum += ScoreRate * dt;

            // Run stats.
            if (coverage > PeakCoverage) PeakCoverage = coverage;
            _coverageIntegral += coverage * dt;
            _coverageTime += dt;

            int newScore = (int)_scoreAccum;
            if (newScore != Score)
            {
                Score = newScore;
                ScoreChanged?.Invoke(Score);
            }

            // Combo decays if you stop killing.
            if (_combo > 0 && Time.time - _lastKillTime > _comboWindow)
            {
                _combo = 0;
            }
        }

        /// <summary>Discrete bonus, e.g. an enemy kill on top of the coverage it restores.</summary>
        public void AddScore(int points)
        {
            if (points == 0)
            {
                return;
            }
            _scoreAccum += points;
            int newScore = (int)_scoreAccum;
            if (newScore != Score)
            {
                Score = newScore;
                ScoreChanged?.Invoke(Score);
            }
        }

        /// <summary>
        /// Count a destroyed enemy and advance the combo. Call this BEFORE reading <see cref="Multiplier"/>
        /// so the multiplier includes the current kill.
        /// </summary>
        public void RegisterEnemyKill()
        {
            EnemiesDestroyed++;
            _combo = (Time.time - _lastKillTime <= _comboWindow) ? _combo + 1 : 1;
            _lastKillTime = Time.time;
        }

        public void ResetScore()
        {
            Score = 0;
            _scoreAccum = 0;
            _coverageIntegral = 0;
            _coverageTime = 0;
            PeakCoverage = 0f;
            EnemiesDestroyed = 0;
            _combo = 0;
            ScoreChanged?.Invoke(Score);
        }

        // Phase 1-3 debug: on-screen coverage/score readout (matches the project's OnGUI-debug
        // convention: MusicMapper, TestMode). Removed/replaced by the real HUD later.
        private void OnGUI()
        {
            if (Application.isMobilePlatform) return; // debug overlay: editor/desktop only
            VisualizerField field = VisualizerField.Instance;
            float coverage = field != null ? field.Coverage : 0f;
            float gain = field != null ? field.PlayerPaintGain : 1f;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = Color.white;
            string combo = _combo > 1 ? $"   ·   COMBO x{Multiplier:0.0} ({_combo})" : string.Empty;
            GUI.Label(new Rect(20, 400, 640, 24),
                $"COVERAGE {coverage * 100f:0}%   ·   PAINT {gain:0.00}   ·   +{ScoreRate:0}/s   ·   SCORE {Score}{combo}",
                style);
        }
    }
}
