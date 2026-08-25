using UnityEngine;
using UnityEngine.Rendering;

namespace PlayVisualizer.Visuals
{
    /// <summary>Live-tunable coverage-measurement parameters (edited on VisualizerCore).</summary>
    [System.Serializable]
    public class CoverageParams
    {
        [Tooltip("Side length of the low-res grid the field is reduced to before readback.")]
        public int GridSize = 32;
        [Tooltip("Luminance above which a cell counts as 'covered' (has visualizer content).")]
        [Range(0f, 1f)] public float Threshold = 0.06f;
        [Tooltip("Frames between coverage samples. Higher = cheaper, coarser.")]
        public int SampleInterval = 3;
        [Tooltip("Smoothing on the reported coverage so score rate doesn't jitter (0 = none).")]
        [Range(0f, 0.99f)] public float Smoothing = 0.6f;
    }

    /// <summary>
    /// Measures current visualizer coverage (spec §2) off the GPU field without stalling the
    /// pipeline: the field is downsampled to a small grid, then read back asynchronously. Coverage
    /// is the fraction of grid cells whose luminance clears a threshold — a "how much of the play
    /// area currently has content" number, naturally falling as the trail fades or enemies eat it.
    ///
    /// Owned by VisualizerCore. Purely a measurement — it never modifies the field.
    /// </summary>
    public class CoverageSampler
    {
        private CoverageParams _p = new CoverageParams();
        private RenderTexture _small;
        private int _grid;
        private int _frame;
        private bool _requestInFlight;
        private float _coverage;
        private Vector2 _hotUV = new Vector2(0.5f, 0.5f);
        private bool _hasContent;

        /// <summary>Latest smoothed coverage, 0..1.</summary>
        public float Coverage => _coverage;

        /// <summary>Viewport-space centroid of the painted mass (where the color is densest).</summary>
        public Vector2 HotUV => _hotUV;

        /// <summary>True when there is any painted content to target.</summary>
        public bool HasContent => _hasContent;

        public void Configure(CoverageParams p)
        {
            _p = p ?? new CoverageParams();
        }

        /// <summary>Sample coverage from <paramref name="field"/>; call once per frame.</summary>
        public void Update(RenderTexture field)
        {
            if (field == null) return;
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                return; // Coverage stays at last value on platforms without async readback.
            }

            _frame++;
            int interval = Mathf.Max(1, _p.SampleInterval);
            if (_requestInFlight || _frame % interval != 0)
            {
                return;
            }

            EnsureSmall();
            // A single bilinear downsample from the already low-res field to the grid. Good enough
            // as an average for a coverage estimate; the grid is small so readback is cheap.
            Graphics.Blit(field, _small);

            _requestInFlight = true;
            AsyncGPUReadback.Request(_small, 0, TextureFormat.RGBAFloat, OnReadback);
        }

        private void OnReadback(AsyncGPUReadbackRequest req)
        {
            _requestInFlight = false;
            if (req.hasError)
            {
                return;
            }

            var data = req.GetData<Color>();
            int n = data.Length;
            if (n == 0) return;

            int covered = 0;
            float weight = 0f;
            float sumX = 0f, sumY = 0f;
            for (int i = 0; i < n; i++)
            {
                Color c = data[i];
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                if (lum >= _p.Threshold)
                {
                    covered++;
                    // Luminance-weighted centroid → the enemies aim at where color is densest.
                    int x = i % _grid;
                    int y = i / _grid;
                    float u = (x + 0.5f) / _grid;
                    float v = (y + 0.5f) / _grid;
                    sumX += u * lum;
                    sumY += v * lum;
                    weight += lum;
                }
            }

            float instant = (float)covered / n;
            float s = Mathf.Clamp01(_p.Smoothing);
            _coverage = Mathf.Lerp(instant, _coverage, s);

            _hasContent = covered > 0;
            if (_hasContent && weight > 0f)
            {
                _hotUV = new Vector2(sumX / weight, sumY / weight);
            }
        }

        private void EnsureSmall()
        {
            int grid = Mathf.Clamp(_p.GridSize, 4, 128);
            if (_small != null && grid == _grid) return;

            _grid = grid;
            if (_small != null) _small.Release();
            _small = new RenderTexture(grid, grid, 0, RenderTextureFormat.ARGBFloat)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _small.Create();
        }

        public void Release()
        {
            if (_small != null) { _small.Release(); _small = null; }
        }
    }
}
