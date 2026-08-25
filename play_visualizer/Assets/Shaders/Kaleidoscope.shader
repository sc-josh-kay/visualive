// Display pass for the feedback buffer. Applies N-fold radial mirror symmetry about a
// center (usually the player), plus a final hue shift and brightness. _Segments <= 1 is a
// straight passthrough (used while verifying the feedback loop before enabling symmetry).
Shader "PlayVisualizer/Kaleidoscope"
{
    Properties
    {
        _MainTex ("Tex", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Segments ("Segments", Float) = 1
        _Rotation ("Rotation", Float) = 0
        _Hue ("Hue", Float) = 0
        _Brightness ("Brightness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Center;
            float _Aspect, _Segments, _Rotation, _Hue, _Brightness;

            // Ripple distortion (Phase V3). Unused slots are zero (no effect). Default users of
            // this shader don't set these, so ripples only apply where the pattern feeds them.
            float4 _Ripples[8];
            float _RippleWidth;
            float _BlackPoint; // faint values below this crush to black (default 0 = off)

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
                float2 c = _Center.xy;
                float2 uv;

                if (_Segments < 1.5)
                {
                    uv = i.uv; // passthrough
                }
                else
                {
                    float2 p = i.uv - c;
                    p.x *= _Aspect;
                    float r = length(p);
                    float ang = atan2(p.y, p.x) + _Rotation;
                    float wedge = 6.2831853 / _Segments;
                    ang = fmod(ang, wedge);
                    if (ang < 0) ang += wedge;
                    ang = abs(ang - wedge * 0.5); // mirror within the wedge
                    float2 q = float2(cos(ang), sin(ang)) * r;
                    q.x /= _Aspect;
                    uv = c + q;
                }

                // Ripple displacement: push sampling outward in a travelling ring per ripple.
                float w = max(_RippleWidth, 1e-4);
                float2 duv = float2(0, 0);
                for (int k = 0; k < 8; k++)
                {
                    float2 o = _Ripples[k].xy;
                    float radius = _Ripples[k].z;
                    float strength = _Ripples[k].w;
                    float2 dir = uv - o;
                    dir.x *= _Aspect;
                    float dist = length(dir);
                    float x = (dist - radius) / w;
                    float band = exp(-x * x);
                    float2 n = dir / max(dist, 1e-4);
                    n.x /= _Aspect;
                    duv += n * band * strength;
                }
                uv += duv;

                float3 col = tex2D(_MainTex, uv).rgb;
                col = hueShift(col, _Hue);
                col *= _Brightness;
                col = max(col - _BlackPoint, 0.0); // crush faint haze to black
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
