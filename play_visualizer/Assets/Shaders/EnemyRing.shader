// Color Eater visual: 3–5 thin circle perimeters of DIFFERENT colors, slightly offset and stacked,
// each reacting to the music a little differently (localized wobble in one part of each ring, its own
// frequency/phase/speed). Dark center. Driven per-enemy via a MaterialPropertyBlock. Additive.
Shader "PlayVisualizer/EnemyRing"
{
    Properties
    {
        _BaseHue ("Base hue", Float) = 0.7
        _HueStep ("Hue step per ring", Float) = 0.13
        _RingCount ("Ring count", Float) = 4
        _BaseRadius ("Base radius", Float) = 0.58
        _Spacing ("Ring radius spacing", Float) = 0.05
        _Offset ("Center offset amount", Float) = 0.07
        _Thickness ("Ring thickness", Float) = 0.03
        _Wobble ("Wobble amp (energy)", Float) = 0.03
        _WobbleTreble ("Wobble amp (treble)", Float) = 0
        _Pulse ("Beat pulse", Float) = 0
        _Time0 ("Time", Float) = 0
        _Seed ("Per-enemy seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_RINGS 5
            #define TWO_PI 6.2831853

            float _BaseHue, _HueStep, _RingCount, _BaseRadius, _Spacing, _Offset;
            float _Thickness, _Wobble, _WobbleTreble, _Pulse, _Time0, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            float3 hsv2rgb(float3 c)
            {
                float3 p = abs(frac(c.xxx + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
                return c.z * lerp(float3(1, 1, 1), saturate(p - 1.0), c.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                int n = (int)_RingCount;
                float t = max(_Thickness, 1e-3);

                float glow = 0.0;
                float3 col = 0.0;

                for (int k = 0; k < MAX_RINGS; k++)
                {
                    if (k >= n) break;
                    float fk = float(k);
                    float h1 = hash(fk * 1.3 + _Seed);
                    float h2 = hash(fk * 2.1 + _Seed * 1.7);
                    float h3 = hash(fk * 3.7 + _Seed * 0.5);
                    float h4 = hash(fk * 5.3 + _Seed * 2.3);

                    // Each ring slightly offset from center → stacked, not concentric.
                    float2 p = c - (float2(h1, h2) - 0.5) * _Offset;
                    float r = length(p) * 2.0;
                    float ang = atan2(p.y, p.x);

                    // Localized wobble: a window emphasizes one side of THIS ring (different per ring),
                    // with its own frequency/phase/speed — so each ring vibrates differently.
                    float win = 0.5 + 0.5 * cos(ang - h3 * TWO_PI);
                    float freq = 4.0 + floor(h4 * 5.0);           // 4..8 lobes
                    float speed = 0.6 + h1 * 1.4;
                    float wob = (_Wobble + _WobbleTreble) * win
                              * sin(ang * freq + _Time0 * speed + h2 * TWO_PI);

                    float rr = (_BaseRadius - fk * _Spacing + wob) * (1.0 + _Pulse * 0.1);
                    float band = t / (abs(r - rr) + t);
                    band *= smoothstep(1.0, 0.82, r);

                    float hue = frac(_BaseHue + fk * _HueStep);
                    col += hsv2rgb(float3(hue, 0.9, 1.0)) * band;
                    glow += band;
                }

                return fixed4(col, glow);
            }
            ENDCG
        }
    }
}
