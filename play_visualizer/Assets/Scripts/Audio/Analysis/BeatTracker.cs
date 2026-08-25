using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Estimates tempo and tracks beats from the onset-strength signal — deliberately separate
    /// from onset detection (an onset is "a sound started"; a beat is "the rhythmic pulse").
    ///
    /// Approach:
    ///  • Resample the onset strength into a fixed-rate envelope (peak-held) using the audio clock,
    ///    so lags map to real time regardless of frame rate.
    ///  • Periodically autocorrelate that envelope over the plausible BPM lag range; the best lag
    ///    gives the tempo, which is slewed slowly (no per-frame jumping).
    ///  • Run a phase accumulator at the current tempo; detected onsets nudge its phase into
    ///    alignment (a light phase-locked loop). A beat fires when the phase wraps.
    ///
    /// v1 is intentionally simple; it can lock to a tempo octave (half/double) on some material.
    /// </summary>
    public class BeatTracker
    {
        private readonly float _odfRate;
        private readonly float _hop;
        private readonly int _bufLen;
        private readonly float[] _odf;
        private readonly int _minLag, _maxLag;
        private readonly float _minBPM, _maxBPM;
        private readonly float _tempoInterval;
        private readonly float _phaseCorrection;
        private readonly float _tempoInertia;

        private int _head, _count;
        private double _lastSampleDsp;
        private float _accum;
        private bool _started;

        private float _bpm = 120f;
        private float _phase;
        private float _tempoTimer;
        private double _lastBeatDsp;

        public float BPM => _bpm;
        public float Phase => _phase;
        public bool Beat { get; private set; }
        public double LastBeatDsp => _lastBeatDsp;

        public BeatTracker(float minBPM, float maxBPM, float odfRate, float tempoInterval,
                           float phaseCorrection, float tempoInertia)
        {
            _minBPM = minBPM;
            _maxBPM = maxBPM;
            _odfRate = Mathf.Max(30f, odfRate);
            _hop = 1f / _odfRate;
            _tempoInterval = tempoInterval;
            _phaseCorrection = phaseCorrection;
            _tempoInertia = tempoInertia;

            _minLag = Mathf.Max(1, Mathf.RoundToInt(60f * _odfRate / maxBPM));
            _maxLag = Mathf.RoundToInt(60f * _odfRate / minBPM);
            _bufLen = Mathf.Max(256, _maxLag * 4);
            _odf = new float[_bufLen];
        }

        public void Process(float onsetStrength, bool onsetEvent, double dspTime, float dt)
        {
            Beat = false;

            if (!_started)
            {
                _started = true;
                _lastSampleDsp = dspTime;
            }

            // Fixed-rate, peak-held resampling of the onset envelope.
            _accum = Mathf.Max(_accum, onsetStrength);
            int guard = 0;
            while (dspTime - _lastSampleDsp >= _hop && guard++ < 256)
            {
                _lastSampleDsp += _hop;
                _odf[_head] = _accum;
                _head = (_head + 1) % _bufLen;
                _count = Mathf.Min(_count + 1, _bufLen);
                _accum = 0f;
            }

            // Periodic tempo estimate.
            _tempoTimer += dt;
            if (_tempoTimer >= _tempoInterval && _count >= _maxLag * 2)
            {
                _tempoTimer = 0f;
                EstimateTempo();
            }

            // Advance beat phase at the current tempo.
            _phase += dt * (_bpm / 60f);

            // Nudge the phase toward the nearest beat on an onset (light PLL).
            if (onsetEvent)
            {
                float frac = _phase - Mathf.Floor(_phase);
                float err = frac < 0.5f ? frac : frac - 1f;
                _phase -= err * _phaseCorrection;
            }

            if (_phase >= 1f)
            {
                _phase -= Mathf.Floor(_phase);
                Beat = true;
                _lastBeatDsp = dspTime;
            }
        }

        private void EstimateTempo()
        {
            float best = 0f;
            int bestLag = _minLag;

            for (int lag = _minLag; lag <= _maxLag; lag++)
            {
                float sum = 0f;
                int pairs = _count - lag;
                if (pairs <= 0) continue;

                for (int a = 0; a < pairs; a++)
                {
                    sum += Sample(a) * Sample(a + lag);
                }
                sum /= pairs; // normalize for lag length

                // Inertia: gently favor lags whose tempo is near the current BPM, so the
                // estimate doesn't hop between adjacent autocorrelation peaks.
                float bpmAtLag = 60f * _odfRate / lag;
                float delta = (bpmAtLag - _bpm) / 25f;
                float weight = 1f / (1f + _tempoInertia * delta * delta);
                sum *= weight;

                if (sum > best)
                {
                    best = sum;
                    bestLag = lag;
                }
            }

            float newBpm = Mathf.Clamp(60f * _odfRate / bestLag, _minBPM, _maxBPM);
            _bpm = Mathf.Lerp(_bpm, newBpm, 0.07f); // slow slew
        }

        // Value at chronological age (0 = most recent).
        private float Sample(int age)
        {
            int idx = (_head - 1 - age) % _bufLen;
            if (idx < 0) idx += _bufLen;
            return _odf[idx];
        }
    }
}
