using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// The psychedelic video-feedback visualizer. Pipeline per frame:
    ///
    ///   seed (player/enemy glow) → [FEEDBACK accum] → [KALEIDOSCOPE] → [PERSISTENCE accum] → quad
    ///
    /// 1. FEEDBACK: a short flowing melt of the raw seed (fresh, so the source stays crisp).
    /// 2. KALEIDOSCOPE: mirror the melt into an N-fold mandala centered on the player.
    /// 3. PERSISTENCE: THIS is what makes the mandala linger — each frame stacks the new mandala
    ///    on a slightly-faded, slightly-smeared copy of the previous displayed frame, so the
    ///    kaleidoscope pattern trails in the player's wake, blends, and slowly fades.
    ///
    /// Reads MusicState for reactivity but never analyzes audio itself.
    /// </summary>
    public class FeedbackController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Camera _feedbackCamera;
        [SerializeField] private Transform _player;
        [SerializeField] private Renderer _displayRenderer;
        [SerializeField] private Shader _feedbackShader;
        [SerializeField] private Shader _kaleidoscopeShader;
        [SerializeField] private AudioAnalyzer _analyzer;

        [Header("Feedback (source melt — flows outward to keep generating pattern)")]
        [Range(0.25f, 1f)] [SerializeField] private float _resScale = 0.5f;
        [Tooltip("<1 flows content outward from the player, continuously generating radial arms.")]
        [SerializeField] private float _baseZoom = 0.984f;
        [SerializeField] private float _bassZoom = 0.02f;
        [SerializeField] private float _rotation = 0.006f;
        [Tooltip("Source melt length. Kept lowish so the living mandala stays localized at the " +
                 "ship and clears quickly — the persistence stage owns the trailing wake.")]
        [SerializeField] private float _fade = 0.9f;
        [SerializeField] private float _hueSpeed = 0.4f;
        [SerializeField] private float _seedBoost = 0.35f;

        [Header("Reactivity")]
        [Tooltip("Treble adds swirl to the feedback rotation.")]
        [SerializeField] private float _trebleSwirl = 0.02f;
        [SerializeField] private float _beatRotationKick = 0.05f;
        [SerializeField] private float _beatZoomKick = 0.03f;
        [SerializeField] private float _beatHueJump = 0.2f;

        [Header("Kaleidoscope")]
        [Tooltip("Mirror count. 1 = passthrough (raw feedback).")]
        [SerializeField] private float _segments = 6f;
        [SerializeField] private float _kaleidoRotationSpeed = 0.08f;

        [Header("Persistence (the lingering trail)")]
        [Tooltip("How long the kaleidoscope pattern lingers. Closer to 1 = longer, dreamier trail.")]
        [Range(0.5f, 0.99f)] [SerializeField] private float _displayFade = 0.96f;
        [Tooltip("<1 gently smears the lingering copies together; 1 = crisp stacked echoes.")]
        [SerializeField] private float _displaySmearZoom = 0.997f;
        [Tooltip("Input gain into the persistence loop (kept so brightness is stable as fade changes).")]
        [SerializeField] private float _persistGain = 1f;

        [Header("Output")]
        [SerializeField] private float _brightness = 0.34f;
        [Tooltip("World Z for the background quad (must be behind gameplay at z=0).")]
        [SerializeField] private float _displayZ = 5f;

        private Material _feedbackMat;   // stage 1
        private Material _kaleidoMat;    // stage 2 (blit)
        private Material _persistMat;    // stage 3 (reuses feedback shader)
        private Material _displayMat;    // stage 4 (quad, passthrough)

        private RenderTexture _seedRT, _accumA, _accumB, _kaleidoRT, _dispA, _dispB;
        private bool _pingpong, _dispPing;
        private int _w, _h;
        private float _beatKick;
        private float _kaleidoRotation;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();

            _feedbackMat = new Material(_feedbackShader);
            _persistMat = new Material(_feedbackShader);
            _kaleidoMat = new Material(_kaleidoscopeShader);
            if (_displayRenderer != null)
            {
                // The quad's own material instance (Kaleidoscope shader in passthrough mode).
                _displayMat = _displayRenderer.material;
            }

            EnsureRenderTextures();
        }

        private void LateUpdate()
        {
            EnsureRenderTextures();
            SyncFeedbackCamera();
            FitDisplayQuad();

            float aspect = _mainCamera != null ? _mainCamera.aspect : 1.777f;
            Vector3 vp = _mainCamera != null && _player != null
                ? _mainCamera.WorldToViewportPoint(_player.position)
                : new Vector3(0.5f, 0.5f, 0f);
            var center = new Vector4(vp.x, vp.y, 0f, 0f);

            MusicState s = _analyzer != null ? _analyzer.State : null;
            float bass = s != null ? s.Bass : 0f;
            float energy = s != null ? s.Energy : 0f;
            float treble = s != null ? s.Treble : 0f;
            bool beat = s != null && s.Beat;

            _beatKick = Mathf.Max(0f, _beatKick - Time.deltaTime * 4f);
            if (beat) _beatKick = 1f;

            // --- Stage 1: feedback (short melt of the raw seed) ---
            _feedbackMat.SetTexture("_SeedTex", _seedRT);
            _feedbackMat.SetVector("_Center", center);
            _feedbackMat.SetFloat("_Aspect", aspect);
            _feedbackMat.SetFloat("_Zoom", _baseZoom - bass * _bassZoom - _beatKick * _beatZoomKick);
            _feedbackMat.SetFloat("_Rot", _rotation + treble * _trebleSwirl + _beatKick * _beatRotationKick);
            _feedbackMat.SetFloat("_Fade", _fade);
            _feedbackMat.SetFloat("_Hue",
                _hueSpeed * (0.5f + energy) * Time.deltaTime + (beat ? _beatHueJump : 0f));
            _feedbackMat.SetFloat("_SeedBoost", _seedBoost);

            RenderTexture accumSrc = _pingpong ? _accumB : _accumA;
            RenderTexture accumDst = _pingpong ? _accumA : _accumB;
            Graphics.Blit(accumSrc, accumDst, _feedbackMat);
            _pingpong = !_pingpong;

            // --- Stage 2: kaleidoscope (mirror the melt into a mandala) ---
            _kaleidoRotation += _kaleidoRotationSpeed * (0.5f + energy) * Time.deltaTime;
            _kaleidoMat.SetVector("_Center", center);
            _kaleidoMat.SetFloat("_Aspect", aspect);
            _kaleidoMat.SetFloat("_Segments", _segments);
            _kaleidoMat.SetFloat("_Rotation", _kaleidoRotation);
            _kaleidoMat.SetFloat("_Hue", 0f);
            _kaleidoMat.SetFloat("_Brightness", 1f);
            Graphics.Blit(accumDst, _kaleidoRT, _kaleidoMat);

            // --- Stage 3: persistence (the lingering, smearing trail of the mandala) ---
            // Reuses the feedback shader: displayNew = displayPrev*fade (smeared) + mandala*gain.
            // SeedBoost is normalized by (1-fade) so brightness stays stable as fade changes.
            _persistMat.SetTexture("_SeedTex", _kaleidoRT);
            _persistMat.SetVector("_Center", center);
            _persistMat.SetFloat("_Aspect", aspect);
            _persistMat.SetFloat("_Zoom", _displaySmearZoom);
            _persistMat.SetFloat("_Rot", 0f);
            _persistMat.SetFloat("_Fade", _displayFade);
            _persistMat.SetFloat("_Hue", 0f);
            _persistMat.SetFloat("_SeedBoost", (1f - _displayFade) * _persistGain);

            RenderTexture dispSrc = _dispPing ? _dispB : _dispA;
            RenderTexture dispDst = _dispPing ? _dispA : _dispB;
            Graphics.Blit(dispSrc, dispDst, _persistMat);
            _dispPing = !_dispPing;

            // --- Stage 4: display the persistence buffer on the quad (passthrough) ---
            if (_displayMat != null)
            {
                _displayMat.SetTexture("_MainTex", dispDst);
                _displayMat.SetVector("_Center", center);
                _displayMat.SetFloat("_Aspect", aspect);
                _displayMat.SetFloat("_Segments", 1f); // already mirrored upstream
                _displayMat.SetFloat("_Rotation", 0f);
                _displayMat.SetFloat("_Hue", 0f);
                _displayMat.SetFloat("_Brightness", _brightness);
            }
        }

        // Keep the background quad exactly filling the orthographic view, centered on the
        // camera, so the RT's UVs line up with the screen (player-centered effects stay aligned).
        private void FitDisplayQuad()
        {
            if (_displayRenderer == null || _mainCamera == null || !_mainCamera.orthographic)
            {
                return;
            }
            float h = _mainCamera.orthographicSize * 2f;
            float w = h * _mainCamera.aspect;
            Transform t = _displayRenderer.transform;
            Vector3 camPos = _mainCamera.transform.position;
            t.position = new Vector3(camPos.x, camPos.y, _displayZ);
            t.rotation = _mainCamera.transform.rotation;
            t.localScale = new Vector3(w, h, 1f);
        }

        private void SyncFeedbackCamera()
        {
            if (_feedbackCamera == null || _mainCamera == null)
            {
                return;
            }
            _feedbackCamera.orthographic = _mainCamera.orthographic;
            _feedbackCamera.orthographicSize = _mainCamera.orthographicSize;
            _feedbackCamera.transform.SetPositionAndRotation(
                _mainCamera.transform.position, _mainCamera.transform.rotation);
        }

        private void EnsureRenderTextures()
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * _resScale));
            int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * _resScale));
            if (_seedRT != null && w == _w && h == _h)
            {
                return;
            }

            _w = w;
            _h = h;
            ReleaseRenderTextures();

            _seedRT = NewRT(w, h);
            _accumA = NewRT(w, h);
            _accumB = NewRT(w, h);
            _kaleidoRT = NewRT(w, h);
            _dispA = NewRT(w, h);
            _dispB = NewRT(w, h);
            ClearRT(_accumA);
            ClearRT(_accumB);
            ClearRT(_dispA);
            ClearRT(_dispB);

            if (_feedbackCamera != null)
            {
                _feedbackCamera.targetTexture = _seedRT;
            }
        }

        private static RenderTexture NewRT(int w, int h)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            return rt;
        }

        private static void ClearRT(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        private void ReleaseRenderTextures()
        {
            if (_feedbackCamera != null)
            {
                _feedbackCamera.targetTexture = null;
            }
            if (_seedRT != null) { _seedRT.Release(); _seedRT = null; }
            if (_accumA != null) { _accumA.Release(); _accumA = null; }
            if (_accumB != null) { _accumB.Release(); _accumB = null; }
            if (_kaleidoRT != null) { _kaleidoRT.Release(); _kaleidoRT = null; }
            if (_dispA != null) { _dispA.Release(); _dispA = null; }
            if (_dispB != null) { _dispB.Release(); _dispB = null; }
        }

        private void OnDisable()
        {
            ReleaseRenderTextures();
        }
    }
}
