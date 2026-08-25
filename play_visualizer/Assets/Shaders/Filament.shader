// Procedural "filament / particle field" pattern: glowing warped lines and radial rays emanating
// from the player, intensified by treble/onsets. Kept sparse via a vignette and the visual-intensity
// gate so it isn't a solid bright fill. Rendered fullscreen via Graphics.Blit (source ignored).
Shader "PlayVisualizer/Filament"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Time0 ("Time", Float) = 0
        _Treble ("Treble", Float) = 0
        _Energy ("Energy", Float) = 0
        _Centroid ("Centroid", Float) = 0
        _Intensity ("Intensity", Float) = 1
        _Rays ("Rays", Float) = 14
        _LineFreq ("Line freq", Float) = 26
        _Sharp ("Sharpness", Float) = 6
        _Speed ("Speed", Float) = 1.5
        _Sat ("Saturation", Float) = 0.8
        _Vignette ("Vignette radius", Float) = 1.3
        _Brightness ("Brightness", Float) = 1.0
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

            float4 _Center;
            float _Aspect, _Time0, _Treble, _Energy, _Centroid, _Intensity;
            float _Rays, _LineFreq, _Sharp, _Speed, _Sat, _Vignette, _Brightness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv - _Center.xy;
                p.x *= _Aspect;
                float r = length(p);
                float ang = atan2(p.y, p.x);

                // Domain-warped flowing lines + radial rays.
                float2 w = i.uv + 0.08 * float2(sin(i.uv.y * 8.0 + _Time0), cos(i.uv.x * 8.0 - _Time0));
                float lines = pow(0.5 + 0.5 * sin(w.x * _LineFreq + w.y * 3.0 + _Time0 * _Speed + r * 6.0), _Sharp);
                float rays = pow(0.5 + 0.5 * sin(ang * _Rays - _Time0 * 0.5), _Sharp);

                float v = max(lines * 0.6, rays * 0.8);
                v *= (0.12 + _Treble * 2.0 + _Energy * 0.4);
                v *= _Intensity;

                float hue = frac(_Centroid * 0.4 + _Time0 * 0.05);
                float3 col = hsv2rgb(float3(hue, _Sat, saturate(v)));
                col *= smoothstep(_Vignette, _Vignette * 0.2, r);

                return fixed4(col * _Brightness, 1.0);
            }
            ENDCG
        }
    }
}
