using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayVisualizer.Audio
{
    /// <summary>
    /// Temporary developer overlay that visualizes the MusicState (band bars + beat flash) so
    /// the analyzer can be verified and tuned before anything consumes it. Toggle with F1.
    /// Intended to be disabled/removed once real audio-reactive visuals exist (Phase 7).
    /// </summary>
    public class MusicDebugHUD : MonoBehaviour
    {
        [SerializeField] private AudioAnalyzer _analyzer;
        [SerializeField] private bool _visible = true;

        private float _beatFlash;
        private float _onsetFlash, _bassOnsetFlash, _midOnsetFlash, _trebleOnsetFlash;

        // Scrolling history (~5 s at 60 fps).
        private const int HistLen = 300;
        private readonly float[] _hOnset = new float[HistLen];
        private readonly float[] _hEnergy = new float[HistLen];
        private readonly bool[] _hBeat = new bool[HistLen];
        private readonly bool[] _hBass = new bool[HistLen];
        private readonly bool[] _hTreble = new bool[HistLen];
        private int _hHead;
        private float _fps;

        private void Awake()
        {
            if (_analyzer == null)
            {
                _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(1e-5f, Time.unscaledDeltaTime), 0.1f);

            float decay = Time.deltaTime * 6f;
            _beatFlash = Mathf.Max(0f, _beatFlash - Time.deltaTime * 4f);
            _onsetFlash = Mathf.Max(0f, _onsetFlash - decay);
            _bassOnsetFlash = Mathf.Max(0f, _bassOnsetFlash - decay);
            _midOnsetFlash = Mathf.Max(0f, _midOnsetFlash - decay);
            _trebleOnsetFlash = Mathf.Max(0f, _trebleOnsetFlash - decay);

            if (_analyzer != null)
            {
                MusicState st = _analyzer.State;
                if (st.Beat) _beatFlash = 1f;
                if (st.Onset) _onsetFlash = 1f;
                if (st.BassOnset) _bassOnsetFlash = 1f;
                if (st.MidOnset) _midOnsetFlash = 1f;
                if (st.TrebleOnset) _trebleOnsetFlash = 1f;

                // Record one history sample per frame.
                _hOnset[_hHead] = st.OnsetStrength;
                _hEnergy[_hHead] = st.Energy;
                _hBeat[_hHead] = st.Beat;
                _hBass[_hHead] = st.BassOnset;
                _hTreble[_hHead] = st.TrebleOnset;
                _hHead = (_hHead + 1) % HistLen;
            }
        }

        private void OnGUI()
        {
            if (Application.isMobilePlatform) return; // debug overlay: editor/desktop only
            if (!_visible || _analyzer == null)
            {
                return;
            }

            // Dashboard panel background for readability.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(8, 8, 820, 620), Texture2D.whiteTexture);
            GUI.color = Color.white;

            MusicState s = _analyzer.State;
            DrawBar(20, "BASS", s.Bass, new Color(1f, 0.2f, 0.4f));
            DrawMarker(20, s.BassInstant); // raw (pre-envelope) — shows attack/release at work
            DrawBar(80, "MID", s.Mid, new Color(0.3f, 1f, 0.5f));
            DrawBar(140, "TREBLE", s.Treble, new Color(0.4f, 0.7f, 1f));
            DrawBar(200, "ENERGY", s.Energy, new Color(1f, 0.9f, 0.3f));
            DrawBar(260, "BRIGHT", s.SpectralCentroid, new Color(1f, 0.6f, 1f));
            DrawBar(320, "FLUX", s.SpectralFlux, new Color(0.6f, 1f, 1f));
            DrawBar(380, "CONTRAST", s.SpectralContrast, new Color(1f, 0.8f, 0.5f));

            // Beat indicator
            var beatRect = new Rect(450, 20, 110, 110);
            GUI.color = new Color(1f, 1f, 1f, 0.15f + 0.85f * _beatFlash);
            GUI.DrawTexture(beatRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            GUI.Label(new Rect(470, 55, 100, 30), "BEAT");
            GUI.color = Color.white;

            // BPM + beat-phase sweep (metronome).
            GUI.Label(new Rect(450, 135, 120, 20), $"BPM {s.BPM:0}");
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(450, 158, 110, 12), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.9f, 0.3f);
            GUI.DrawTexture(new Rect(450, 158, 110 * Mathf.Clamp01(s.BeatPhase), 12), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBands(600f, s.FrequencyBands);

            // Onset indicators (flash on event; bar = continuous strength).
            DrawOnset(20, 300, "ONSET", s.OnsetStrength, _onsetFlash, new Color(1f, 1f, 1f));
            DrawOnset(130, 300, "BASS", s.BassOnsetStrength, _bassOnsetFlash, new Color(1f, 0.3f, 0.4f));
            DrawOnset(240, 300, "MID", s.MidOnsetStrength, _midOnsetFlash, new Color(0.3f, 1f, 0.5f));
            DrawOnset(350, 300, "TREBLE", s.TrebleOnsetStrength, _trebleOnsetFlash, new Color(0.4f, 0.7f, 1f));

            DrawChroma(600f, 300f, s.Chroma);

            // Scrolling history graph.
            GUI.color = Color.white;
            GUI.Label(new Rect(20, 448, 780, 20),
                "SCROLLING (~5 s): onset strength (cyan)  ·  beat (yellow)  ·  bass onset (red, bottom)  ·  treble onset (blue, top)");
            DrawGraph(new Rect(20, 470, 790, 130));

            GUI.Label(new Rect(20, 606, 500, 20), $"F1: toggle audio debug    ·    FPS {_fps:0}");
        }

        // Time-history strip: onset strength as an area, with beat/onset event markers.
        private void DrawGraph(Rect area)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);

            float col = area.width / HistLen;
            float w = Mathf.Max(1f, col);
            for (int i = 0; i < HistLen; i++)
            {
                int idx = (_hHead + i) % HistLen; // oldest → newest
                float x = area.x + i * col;

                float h = Mathf.Clamp01(_hOnset[idx]) * area.height;
                GUI.color = new Color(0.4f, 1f, 1f, 0.7f);
                GUI.DrawTexture(new Rect(x, area.yMax - h, w, h), Texture2D.whiteTexture);

                if (_hBeat[idx])
                {
                    GUI.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                    GUI.DrawTexture(new Rect(x, area.y, Mathf.Max(2f, col), area.height), Texture2D.whiteTexture);
                }
                if (_hBass[idx])
                {
                    GUI.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                    GUI.DrawTexture(new Rect(x, area.yMax - 8f, Mathf.Max(2f, col), 8f), Texture2D.whiteTexture);
                }
                if (_hTreble[idx])
                {
                    GUI.color = new Color(0.4f, 0.7f, 1f, 0.95f);
                    GUI.DrawTexture(new Rect(x, area.y, Mathf.Max(2f, col), 8f), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
        }

        private static readonly string[] NoteNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        // 12 pitch-class bars.
        private void DrawChroma(float x, float y, float[] chroma)
        {
            if (chroma == null || chroma.Length < 12)
            {
                return;
            }

            const float maxHeight = 110f;
            const float width = 20f;
            const float gap = 4f;
            float bottom = y + maxHeight;

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - 20f, 200f, 20f), "CHROMA");

            for (int i = 0; i < 12; i++)
            {
                float bx = x + i * (width + gap);
                GUI.color = new Color(0f, 0f, 0f, 0.4f);
                GUI.DrawTexture(new Rect(bx, y, width, maxHeight), Texture2D.whiteTexture);

                float h = Mathf.Clamp01(chroma[i]) * maxHeight;
                float hue = i / 12f; // pitch class → hue
                GUI.color = Color.HSVToRGB(hue, 0.7f, 1f);
                GUI.DrawTexture(new Rect(bx, bottom - h, width, h), Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(bx - 2f, bottom + 2f, width + 8f, 18f), NoteNames[i]);
            }
        }

        // Square that flashes on an onset event, with a thin strength bar underneath.
        private void DrawOnset(float x, float y, string label, float strength, float flash, Color color)
        {
            var box = new Rect(x, y, 60f, 60f);
            GUI.color = new Color(color.r, color.g, color.b, 0.12f + 0.88f * Mathf.Clamp01(flash));
            GUI.DrawTexture(box, Texture2D.whiteTexture);

            // strength bar
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(x, y + 66f, 60f, 10f), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y + 66f, 60f * Mathf.Clamp01(strength), 10f), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 78f, 90f, 20f), label);
        }

        // Row of log-spaced frequency band bars (the rich representation).
        private void DrawBands(float x, float[] bands)
        {
            if (bands == null || bands.Length == 0)
            {
                return;
            }

            const float maxHeight = 200f;
            const float bottom = 260f;
            const float gap = 3f;
            float width = Mathf.Min(26f, (Screen.width - x - 20f) / bands.Length - gap);
            if (width < 3f)
            {
                width = 3f;
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(x, bottom - maxHeight - 20f, 200f, 20f), "FREQUENCY BANDS");

            for (int i = 0; i < bands.Length; i++)
            {
                float bx = x + i * (width + gap);
                GUI.color = new Color(0f, 0f, 0f, 0.4f);
                GUI.DrawTexture(new Rect(bx, bottom - maxHeight, width, maxHeight), Texture2D.whiteTexture);

                float h = Mathf.Clamp01(bands[i]) * maxHeight;
                // Low bands red → high bands blue.
                float t = bands.Length > 1 ? (float)i / (bands.Length - 1) : 0f;
                GUI.color = new Color(1f - t, 0.4f + 0.3f * Mathf.Sin(t * 3.14f), 0.3f + 0.7f * t);
                GUI.DrawTexture(new Rect(bx, bottom - h, width, h), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private void DrawBar(float x, string label, float value, Color color)
        {
            const float maxHeight = 200f;
            const float width = 50f;
            float bottom = 260f;

            // Background
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(x, bottom - maxHeight, width, maxHeight), Texture2D.whiteTexture);

            // Fill
            float h = Mathf.Clamp01(value) * maxHeight;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, bottom - h, width, h), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(x - 4, bottom + 4, width + 20, 20), label);
        }

        // Thin horizontal line marking a second value on the same bar (e.g. the raw signal).
        private void DrawMarker(float x, float value)
        {
            const float maxHeight = 200f;
            const float width = 50f;
            const float bottom = 260f;
            float y = bottom - Mathf.Clamp01(value) * maxHeight;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y - 1f, width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
