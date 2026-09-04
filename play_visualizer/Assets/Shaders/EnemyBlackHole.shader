// Corruptor visual: a black hole with a SWIRLING multi-color glow — an event-horizon ring plus
// rotating spiral arms, tinted between two palette colors (teal↔pink through purple), over a dark
// center. Brightens/pulses with bass; flares on a bass onset. The crisp neon rings, the horizontal
// audio waveform, and the drifting speckles are separate geometry/particles (CorruptorVisualizer).
// Driven per-enemy via a MaterialPropertyBlock. Additive.
Shader "PlayVisualizer/EnemyBlackHole"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.1, 0.9, 1, 1)
        _ColorB ("Color B", Color) = (1, 0.2, 0.8, 1)
        _Time0 ("Time", Float) = 0
        _Bass ("Bass", Float) = 0
        _Flare ("Bass-onset flare", Float) = 0
        _Ring ("Ring softness", Float) = 0.045
        _Arms ("Spiral arms", Float) = 4
        _Twist ("Spiral twist", Float) = 3
        _Spin ("Spin speed", Float) = 1.1
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

            float4 _ColorA, _ColorB;
            float _Time0, _Bass, _Flare, _Ring, _Arms, _Twist, _Spin;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float r = length(c) * 2.0;
                if (r > 1.05) return fixed4(0, 0, 0, 0);
                float ang = atan2(c.y, c.x);

                // Event-horizon glow ring.
                float ring = _Ring / (abs(r - 0.5) + _Ring);

                // Rotating spiral arms (the swirl), strongest in a mid-radius band.
                float spiral = sin(_Arms * ang + _Twist * log(max(r, 0.04)) + _Time0 * _Spin);
                float arms = pow(saturate(0.5 + 0.5 * spiral), 2.0);
                float swirlEnv = smoothstep(0.08, 0.4, r) * smoothstep(1.0, 0.55, r);
                float swirl = arms * swirlEnv;

                float glow = ring * 0.7 + swirl * (0.5 + _Bass * 0.7);
                glow *= smoothstep(1.05, 0.8, r); // soft edge, dark center preserved

                // Palette mix (teal ↔ pink) across angle + spiral, drifting with the spin.
                float mix = 0.5 + 0.5 * sin(ang * 2.0 + _Time0 * _Spin * 0.5 + spiral * 0.5);
                float3 palette = lerp(_ColorA.rgb, _ColorB.rgb, mix);

                float3 col = palette * glow + float3(1, 1, 1) * (_Flare * ring * 0.3);
                return fixed4(col, glow);
            }
            ENDCG
        }
    }
}
