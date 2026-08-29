// Corruptor visual: a black hole — a glowing event-horizon ring with a bright horizontal accretion
// disk crossing the middle (extending past the ring), a dark center, and a bass-driven pulse/flare.
// Driven via a MaterialPropertyBlock. Additive; warm by default. Distinct from the Color Eater ring
// via the through-disk, size, and bass response.
Shader "PlayVisualizer/EnemyBlackHole"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.5, 0.15, 1)
        _Time0 ("Time", Float) = 0
        _Bass ("Bass", Float) = 0
        _Flare ("Bass-onset flare", Float) = 0
        _Ring ("Ring softness", Float) = 0.04
        _Disk ("Disk thickness", Float) = 0.06
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
            float _Time0, _Bass, _Flare, _Ring, _Disk;

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

                // Event-horizon glow ring.
                float ringR = 0.5;
                float ring = _Ring / (abs(r - ringR) + _Ring);

                // Accretion disk: a bright horizontal bar across the middle, thickening with bass,
                // extending past the ring, with a dark gap at the very center (the hole).
                float diskThick = _Disk * (1.0 + _Bass * 0.7);
                float bar = exp(-(c.y * c.y) / (2.0 * diskThick * diskThick));
                bar *= smoothstep(1.1, 0.15, r);                 // fade out; dark hole center
                bar *= 0.85 + 0.15 * sin(c.x * 40.0 + _Time0 * 3.0); // subtle shimmer

                float glow = ring * 0.6 + bar * (1.0 + _Bass * 0.8);
                glow *= smoothstep(1.05, 0.82, r);

                float3 col = _Color.rgb * glow
                           + float3(1, 1, 1) * (bar * 0.35 + _Flare * ring * 0.4);
                return fixed4(col, glow);
            }
            ENDCG
        }
    }
}
