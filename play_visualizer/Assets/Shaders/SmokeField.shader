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

            sampler2D _MainTex;
            float4 _Center, _EmitColor;
            float _Aspect, _Flow, _Swirl, _Warp, _WarpFreq, _WarpSpeed, _Fade, _Hue;
            float _BlobRadius, _RingRadius, _RingWidth, _BaseStrength, _PopStrength, _Treble, _Time0;
            float _VacCount, _VacPull, _VacSwirlAmt, _VacEat;
            float4 _Vacuums[VAC_MAX]; // (u, v, radiusV, strength)
            float _VacSwirl[VAC_MAX]; // signed swirl per vacuum

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
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
