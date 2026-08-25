using System.Collections.Generic;
using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Detects onsets from an onset-strength (flux) signal using an adaptive threshold and a
    /// refractory period — "something just started", not "the sound is currently loud".
    ///
    /// Threshold = local mean + sensitivity × local std over a sliding window, so it adapts to
    /// each song's dynamic range. An event fires on the rising edge across that threshold (low
    /// latency), rate-limited by the refractory. <see cref="Strength"/> is a continuous 0..1
    /// normalization of the flux for use as a smooth signal.
    ///
    /// Reused per channel (global / bass / mid / treble). No smoothing is applied to the event.
    /// </summary>
    public class OnsetDetector
    {
        private readonly Queue<float> _history = new Queue<float>();
        private float _sum, _sumSq;
        private int _window;
        private float _sensitivity;
        private double _refractory;

        private float _prevFlux;
        private double _lastOnsetTime = -10.0;
        private float _peak;

        public float Strength { get; private set; }
        public bool Onset { get; private set; }

        public OnsetDetector(int window, float sensitivity, float refractorySeconds)
        {
            SetParams(window, sensitivity, refractorySeconds);
        }

        public void SetParams(int window, float sensitivity, float refractorySeconds)
        {
            _window = Mathf.Max(4, window);
            _sensitivity = sensitivity;
            _refractory = refractorySeconds;
        }

        public void Process(float flux, double dspTime, float decay, float dt)
        {
            Onset = false;

            int n = _history.Count;
            float mean = n > 0 ? _sum / n : 0f;
            float variance = n > 0 ? Mathf.Max(0f, _sumSq / n - mean * mean) : 0f;
            float std = Mathf.Sqrt(variance);
            float threshold = mean + _sensitivity * std + 1e-4f;

            // Continuous strength: flux relative to its adaptive peak.
            _peak = flux > _peak ? flux : Mathf.Max(flux, _peak - decay * _peak * dt);
            Strength = _peak > 1e-6f ? Mathf.Clamp01(flux / _peak) : 0f;

            // Rising-edge crossing of the adaptive threshold, rate-limited.
            if (flux > threshold && _prevFlux <= threshold && (dspTime - _lastOnsetTime) > _refractory)
            {
                Onset = true;
                _lastOnsetTime = dspTime;
            }
            _prevFlux = flux;

            // Update the sliding window AFTER deciding, so the sample isn't in its own threshold.
            _history.Enqueue(flux);
            _sum += flux;
            _sumSq += flux * flux;
            if (_history.Count > _window)
            {
                float old = _history.Dequeue();
                _sum -= old;
                _sumSq -= old * old;
            }
        }
    }
}
