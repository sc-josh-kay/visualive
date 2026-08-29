// Swarm-unit visual: a tiny neon starfish/asterisk whose arms FLUTTER with Treble/Spectral Flux
// (fast per-arm length oscillation), driven via a MaterialPropertyBlock. Additive; kept small and
// readable.
Shader "PlayVisualizer/EnemyStar"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 1, 0.7, 1)
        _Time0 ("Time", Float) = 0
        _Arms ("Arm count", Float) = 5
        _Flutter ("Flutter", Float) = 0
        _Sharp ("Arm sharpness", Float) = 2
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
            float _Time0, _Arms, _Flutter, _Sharp;

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
                float ang = atan2(c.y, c.x);

                float arms = pow(abs(cos(_Arms * ang * 0.5)), _Sharp);
                // Per-arm length flutter (fast) rising with treble/flux.
                float reach = 0.7 + _Flutter * 0.3 * sin(_Time0 * 22.0 + ang * _Arms);
                float along = saturate(1.0 - r / max(reach, 0.05));
                float core = smoothstep(0.18, 0.0, r);

                float glow = arms * along * along + core * 0.9;
                glow *= smoothstep(1.0, 0.8, r);
                return fixed4(_Color.rgb * glow, glow);
            }
            ENDCG
        }
    }
}
