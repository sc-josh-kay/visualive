// Persistent "fluid swirl" field. Like SmokeField but advects the previous frame along a swirling
// vector field (tangential rotation + wavy warp) instead of straight outward — organic, flowing.
// Driven by mid/flux (swirl/warp amount). Player continuously emits into it; it fades and hue-drifts.
Shader "PlayVisualizer/FluidField"
{
    Properties
    {
        _MainTex ("Previous", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Swirl ("Swirl", Float) = 0.004
        _Warp ("Warp", Float) = 0.004
        _Freq ("Warp freq", Float) = 2
        _Speed ("Warp speed", Float) = 1
        _Fade ("Fade", Float) = 0.95
        _Hue ("Hue shift/frame", Float) = 0
        _EmitColor ("Emit color", Color) = (1,1,1,1)
        _BlobRadius ("Blob radius", Float) = 0.05
        _BaseStrength ("Base strength", Float) = 0.1
        _Treble ("Treble", Float) = 0
        _Time0 ("Time", Float) = 0
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

            sampler2D _MainTex;
            float4 _Center, _EmitColor;
            float _Aspect, _Swirl, _Warp, _Freq, _Speed, _Fade, _Hue, _BlobRadius, _BaseStrength, _Treble, _Time0;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

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
                float r = length(p);

                // Swirl: tangential rotation around the player + a wavy domain warp.
                float2 tang = float2(-p.y, p.x) / max(r, 1e-3);
                float2 wave = float2(sin(i.uv.y * _Freq * 6.2831853 + _Time0 * _Speed),
                                     sin(i.uv.x * _Freq * 6.2831853 - _Time0 * _Speed));
                float2 flow = tang * _Swirl + wave * _Warp;

                float3 prev = hueShift(tex2D(_MainTex, i.uv - flow).rgb, _Hue) * _Fade;

                float d = length(p);
                float blob = smoothstep(_BlobRadius, 0.0, d);
                float e = blob * _BaseStrength + blob * _Treble * 0.5;
                float3 emit = _EmitColor.rgb * e;

                return fixed4(prev + emit, 1.0);
            }
            ENDCG
        }
    }
}
