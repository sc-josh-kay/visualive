// Video-feedback pass: samples the PREVIOUS accumulation frame at a transformed UV
// (rotate + zoom about a center, usually the player), fades and hue-rotates it, then adds
// the current seed on top. Run every frame via Graphics.Blit to build flowing trails.
Shader "PlayVisualizer/Feedback"
{
    Properties
    {
        _MainTex ("Previous", 2D) = "black" {}
        _SeedTex ("Seed", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Zoom ("Zoom", Float) = 0.99
        _Rot ("Rotation (rad/frame)", Float) = 0.01
        _Fade ("Fade", Float) = 0.96
        _Hue ("Hue shift (rad/frame)", Float) = 0.0
        _SeedBoost ("Seed boost", Float) = 1.0
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
            sampler2D _SeedTex;
            float4 _Center;
            float _Aspect, _Zoom, _Rot, _Fade, _Hue, _SeedBoost;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Approximate hue rotation: rotate the color vector about the (1,1,1) axis.
            float3 hueShift(float3 c, float a)
            {
                const float3 k = float3(0.57735, 0.57735, 0.57735);
                float cosA = cos(a);
                return c * cosA + cross(k, c) * sin(a) + k * dot(k, c) * (1.0 - cosA);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = _Center.xy;
                float2 p = i.uv - c;
                p.x *= _Aspect;
                float s = sin(_Rot), co = cos(_Rot);
                p = float2(p.x * co - p.y * s, p.x * s + p.y * co);
                p *= _Zoom;
                p.x /= _Aspect;
                float2 uv = c + p;

                float3 prev = tex2D(_MainTex, uv).rgb;
                prev = hueShift(prev, _Hue);
                prev *= _Fade;

                float3 seed = tex2D(_SeedTex, i.uv).rgb * _SeedBoost;

                return fixed4(prev + seed, 1.0);
            }
            ENDCG
        }
    }
}
