// Persistent "smoke ring" field. Each frame: redraw the previous field advected OUTWARD from the
// player (sampling slightly toward the player), faded and hue-rotated, then add a fresh emission
// at the player — a soft blob (continuous trail) plus a ring pop (bass onset / beat) and treble
// sparkle. Run every frame via Graphics.Blit; the field starts empty so the player paints it in.
Shader "PlayVisualizer/SmokeField"
{
    Properties
    {
        _MainTex ("Previous", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Flow ("Outward flow", Float) = 0.01
        _Swirl ("Swirl", Float) = 0.004
        _Warp ("Warp", Float) = 0.004
        _WarpFreq ("Warp freq", Float) = 2
        _WarpSpeed ("Warp speed", Float) = 0.8
        _Fade ("Fade", Float) = 0.95
        _Hue ("Hue shift/frame", Float) = 0
        _EmitColor ("Emit color", Color) = (1,1,1,1)
        _BlobRadius ("Blob radius", Float) = 0.04
        _RingRadius ("Ring radius", Float) = 0.04
        _RingWidth ("Ring width", Float) = 0.02
        _BaseStrength ("Base strength", Float) = 0.1
        _PopStrength ("Pop strength", Float) = 0
        _Treble ("Treble sparkle", Float) = 0
        _Time0 ("Time", Float) = 0
        _VacCount ("Vacuum count", Float) = 0
        _VacPull ("Vacuum inward pull", Float) = 0.028
        _VacSwirlAmt ("Vacuum swirl", Float) = 0.05
        _VacEat ("Vacuum eat", Float) = 0.10
        _TurbCount ("Turbulence zone count", Float) = 0
        _TurbPush ("Turbulence directional push", Float) = 0.03
        _TurbChurn ("Turbulence noise displacement", Float) = 0.022
        _TurbStretch ("Turbulence stretch along flow", Float) = 0.05
        _TurbEat ("Turbulence eat", Float) = 0.14
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define VAC_MAX 6
            #define TURB_MAX 6

            sampler2D _MainTex;
            float4 _Center, _EmitColor;
            float _Aspect, _Flow, _Swirl, _Warp, _WarpFreq, _WarpSpeed, _Fade, _Hue;
            float _BlobRadius, _RingRadius, _RingWidth, _BaseStrength, _PopStrength, _Treble, _Time0;
            float _VacCount, _VacPull, _VacSwirlAmt, _VacEat;
            float4 _Vacuums[VAC_MAX]; // (u, v, radiusV, strength)
            float _VacSwirl[VAC_MAX]; // signed swirl per vacuum
            float _TurbCount, _TurbPush, _TurbChurn, _TurbStretch, _TurbEat;
            float4 _Turbs[TURB_MAX];    // (u, v, radiusV, agitation)
            float4 _TurbFlow[TURB_MAX]; // (flowX, flowY, stretch01, seed)

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Cheap hash-based value noise (no textures) for the turbulent tear. 2 octaves of fbm.
            float hash21(float2 v)
            {
                return frac(sin(dot(v, float2(127.1, 311.7))) * 43758.5453);
            }
            float vnoise(float2 v)
            {
                float2 ip = floor(v);
                float2 fp = frac(v);
                fp = fp * fp * (3.0 - 2.0 * fp);
                float a = hash21(ip);
                float b = hash21(ip + float2(1.0, 0.0));
                float c = hash21(ip + float2(0.0, 1.0));
                float dd = hash21(ip + float2(1.0, 1.0));
                return lerp(lerp(a, b, fp.x), lerp(c, dd, fp.x), fp.y);
            }
            float fbm2(float2 v)
            {
                return vnoise(v) * 0.65 + vnoise(v * 2.03 + 7.3) * 0.35;
            }

            float3 hueShift(float3 c, float a)
            {
                const float3 k = float3(0.57735, 0.57735, 0.57735);
                float cosA = cos(a);
                return c * cosA + cross(k, c) * sin(a) + k * dot(k, c) * (1.0 - cosA);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv - _Center.xy;
                p.x *= _Aspect;
                float d = length(p);

                // Advect: OUTWARD from the player + a swirl (tangential) and wavy turbulence, so
                // the smoke drifts and twists into shapes instead of just expanding radially.
                float2 tang = float2(-p.y, p.x) / max(d, 1e-4);
                float2 wave = float2(sin(i.uv.y * _WarpFreq * 6.2831853 + _Time0 * _WarpSpeed),
                                     sin(i.uv.x * _WarpFreq * 6.2831853 - _Time0 * _WarpSpeed));
                // Base advection offset (aspect space): outward from the player + swirl + warp.
                float2 offset = -_Flow * p + tang * _Swirl + wave * _Warp;

                // Vacuums (Corruptors): sample from further OUT + tangential → content is pulled IN
                // and swirls toward the center, which then EATS (fades) what it draws in — a drain.
                float eat = 1.0;
                int vcount = (int)_VacCount;
                for (int k = 0; k < VAC_MAX; k++)
                {
                    if (k >= vcount) break;
                    float4 v = _Vacuums[k];
                    float2 pv = i.uv - v.xy;
                    pv.x *= _Aspect;
                    float dv = length(pv);
                    if (dv < v.z)
                    {
                        float fo = 1.0 - dv / v.z;
                        fo *= fo;
                        float2 dir = pv / max(dv, 1e-4);
                        float2 perp = float2(-dir.y, dir.x) * _VacSwirl[k];
                        offset += (dir * _VacPull + perp * _VacSwirlAmt) * v.w * fo;
                        eat *= saturate(1.0 - _VacEat * v.w * fo);
                    }
                }

                // Turbulence zones (Swarm): each aggregated zone TEARS the smoke as it moves through it
                // — a noisy directional displacement (drag along the flow + churn), a STRETCH that
                // samples further along the flow axis (smoke elongates into ragged streaks), and a
                // noise-modulated EAT that leaves a ragged hole. Agitation (treble/flux) scales all
                // three, so a high-treble section shreds faster. Bounded to TURB_MAX zones.
                int tcount = (int)_TurbCount;
                for (int t = 0; t < TURB_MAX; t++)
                {
                    if (t >= tcount) break;
                    float4 z = _Turbs[t];
                    float2 pz = i.uv - z.xy;
                    pz.x *= _Aspect;
                    float dz = length(pz);
                    if (dz < z.z)
                    {
                        float fo = 1.0 - dz / z.z;
                        fo *= fo;
                        float agit = z.w;
                        float4 fl = _TurbFlow[t];
                        float2 flow = fl.xy; // normalized viewport-space flow (aspect-corrected)
                        float seed = fl.w;

                        // Churn: fbm-driven displacement vector, animated + per-zone seed.
                        float2 np = pz * 9.0 + float2(seed, seed * 1.7) + _Time0 * 1.3;
                        float2 churn = float2(fbm2(np) - 0.5, fbm2(np + 19.7) - 0.5) * 2.0;

                        // Stretch: pull the sample further BACK along the flow so existing smoke smears
                        // forward into a streak (anisotropic advection along the flow axis).
                        float2 stretch = -flow * (_TurbStretch * fl.z);

                        offset += (flow * _TurbPush + churn * _TurbChurn + stretch) * (0.4 + agit) * fo;

                        float ragged = fbm2(pz * 7.0 + seed - _Time0 * 0.6);
                        eat *= saturate(1.0 - _TurbEat * (0.4 + agit) * fo * ragged);
                    }
                }

                float2 ps = offset;
                ps.x /= _Aspect;
                float3 prev = tex2D(_MainTex, i.uv + ps).rgb;
                prev = hueShift(prev, _Hue) * _Fade * eat;

                // Emission at the player: soft blob (trail) + ring pop + treble sparkle.
                float blob = smoothstep(_BlobRadius, 0.0, d);
                float ring = smoothstep(_RingWidth, 0.0, abs(d - _RingRadius));
                float sparkle = _Treble * (0.5 + 0.5 * sin(d * 120.0 - _Time0 * 6.0));
                float e = blob * _BaseStrength + ring * _PopStrength + blob * sparkle;

                float3 emit = _EmitColor.rgb * e;
                return fixed4(prev + emit, 1.0);
            }
            ENDCG
        }
    }
}
