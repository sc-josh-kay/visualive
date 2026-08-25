// Rotating spiral overlay for the Vortex Wave weapon (spec7 §8) — "swirling the wet paint." An
// additive, energy-colored set of spiral arms that rotate over time and pulse with the music, drawn
// on a world quad scaled to the vortex radius. It is a transient overlay (energy), not coverage.
Shader "PlayVisualizer/VortexSwirl"
{
    Properties
    {
        _Color ("Color", Color) = (0.6, 0.4, 1, 1)
        _Progress ("Progress 0..1", Float) = 0
        _Strength ("Strength", Float) = 1
        _Time0 ("Time", Float) = 0
        _Arms ("Arm count", Float) = 5
        _Twist ("Twist", Float) = 3
        _Speed ("Spin speed", Float) = 2
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
            float _Progress, _Strength, _Time0, _Arms, _Twist, _Speed;

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
                float r = length(c) * 2.0;          // 0 center, 1 edge
                if (r > 1.0) return fixed4(0, 0, 0, 0);
                float ang = atan2(c.y, c.x);

                // Spiral arms: angle twisted by log-radius, rotating over time.
                float spiral = sin(_Arms * ang + _Twist * log(max(r, 0.02)) + _Time0 * _Speed);
                float arms = pow(saturate(0.5 + 0.5 * spiral), 3.0);

                // Ring envelope: nothing at the very center or edge; strongest in the swirl band.
                float env = smoothstep(0.0, 0.18, r) * smoothstep(1.0, 0.65, r);

                float life = smoothstep(0.0, 0.1, _Progress) * (1.0 - smoothstep(0.8, 1.0, _Progress));
                float intensity = arms * env * life * _Strength;

                float3 col = _Color.rgb * intensity + float3(1, 1, 1) * (intensity * 0.15);
                return fixed4(col, intensity);
            }
            ENDCG
        }
    }
}
