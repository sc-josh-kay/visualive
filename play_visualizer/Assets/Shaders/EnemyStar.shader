// Swarm-unit visual: a tiny "waveform particle" — a hot glowing core with several THIN WAVY tendrils
// radiating out, squiggling toward their tips (and beading faintly), like a spark of high-frequency
// audio that has become alive. Treble/Spectral-Flux (_Flutter) grows the tendrils' length, squiggle
// amplitude and speed so the swarm reads as "musical static" at high treble. Per-unit _Seed + _Arms
// vary shape/orientation so no two units are identical. Driven via a MaterialPropertyBlock; additive,
// one quad per unit (cheap for ~40 units).
Shader "PlayVisualizer/EnemyStar"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.6, 0.12, 1)
        _Time0 ("Time", Float) = 0
        _Arms ("Arm count", Float) = 5
        _Flutter ("Flutter (agitation)", Float) = 0
        _Sharp ("Arm sharpness (thinness)", Float) = 3
        _Seed ("Per-unit seed", Float) = 0
        _CoreSize ("Core radius", Float) = 0.14
        _Reach ("Arm length (base)", Float) = 0.42
        _ReachGain ("Arm length at full agitation", Float) = 0.5
        _Squiggle ("Squiggle amplitude", Float) = 0.7
        _WaveFreq ("Squiggle frequency", Float) = 9
        _WaveSpeed ("Squiggle speed", Float) = 16
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

            float4 _Color;
            float _Time0, _Arms, _Flutter, _Sharp, _Seed;
            float _CoreSize, _Reach, _ReachGain, _Squiggle, _WaveFreq, _WaveSpeed;

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
                float r = length(c) * 2.0;          // 0 at center → 1 at quad edge
                float ang = atan2(c.y, c.x);
                float agit = saturate(_Flutter);
                float sp = _Seed * 6.2831853;        // per-unit phase

                // Tendrils get longer with agitation; low treble = short calm stubs.
                float reach = _Reach + agit * _ReachGain;

                // Wavy centerline: perturb the radial arm pattern by a sine that grows outward (r) and
                // with agitation, so each tendril squiggles more toward its tip during high treble.
                float amp = _Squiggle * (0.3 + agit) * r;
                float squig = amp * sin(r * _WaveFreq + _Time0 * _WaveSpeed + sp);
                float k = _Arms * ang * 0.5 + squig + sp;
                float arm = pow(abs(cos(k)), _Sharp);

                // Taper to the tip + hard-ish cutoff at reach.
                float along = saturate(1.0 - r / max(reach, 0.05));
                float tendril = arm * along * along;

                // Faint beading along the tendrils (the dotted look), scrolling outward.
                float bead = 0.5 + 0.5 * sin(r * 28.0 - _Time0 * 10.0 + sp * 3.0);
                tendril *= lerp(0.72, 1.0, bead);

                // Hot core: tight bright dot with a soft halo.
                float core = smoothstep(_CoreSize, 0.0, r);
                core = core * core;

                float edge = smoothstep(1.0, 0.82, r); // fade before the quad boundary

                float3 armCol = _Color.rgb;
                float3 coreCol = lerp(_Color.rgb, float3(1.0, 0.92, 0.65), 0.85); // hot yellow-white
                float3 col = (armCol * tendril + coreCol * (core * 1.3)) * edge;
                float a = saturate((tendril + core) * edge);
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
