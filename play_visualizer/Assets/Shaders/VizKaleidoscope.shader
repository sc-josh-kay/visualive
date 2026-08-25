// Procedural kaleidoscope pattern for the new layered visualizer. Generates a radially-symmetric
// psychedelic field from polar coordinates + time, modulated by MusicState (bass rings, mid arms,
// treble detail, energy intensity, centroid hue). A vignette preserves negative space so it isn't
// maximally bright everywhere. Rendered fullscreen via Graphics.Blit (source ignored).
Shader "PlayVisualizer/VizKaleidoscope"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "black" {}
        _Center ("Center (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777
        _Segments ("Segments", Float) = 6
        _Time0 ("Time", Float) = 0

        _Bass ("Bass", Float) = 0
        _Mid ("Mid", Float) = 0
        _Treble ("Treble", Float) = 0
        _Energy ("Energy", Float) = 0
        _Centroid ("Centroid", Float) = 0

        _RotSpeed ("Rot speed", Float) = 0.3
        _RingFreq ("Ring freq", Float) = 14
        _ArmFreq ("Arm freq", Float) = 9
        _DetailFreq ("Detail freq", Float) = 60
        _HueSpeed ("Hue speed", Float) = 0.05
        _HueBase ("Hue base", Float) = 0
        _Sat ("Saturation", Float) = 0.85
        _Vignette ("Vignette radius", Float) = 1.3
        _Brightness ("Brightness", Float) = 1.2
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
            float _Aspect, _Segments, _Time0;
            float _Bass, _Mid, _Treble, _Energy, _Centroid;
            float _RotSpeed, _RingFreq, _ArmFreq, _DetailFreq, _HueSpeed, _HueBase, _Sat, _Vignette, _Brightness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

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

                // Radial mirror symmetry (kaleidoscope), slowly rotating (faster with energy).
                float t = _Time0;
                ang -= t * _RotSpeed * (0.3 + _Energy * 0.7);
                float wedge = 6.2831853 / max(1.0, _Segments);
                ang = fmod(ang, wedge);
                if (ang < 0.0) ang += wedge;
                ang = abs(ang - wedge * 0.5);

                // Bass rings + mid arms.
                float rings = 0.5 + 0.5 * sin(r * _RingFreq - t * (1.0 + _Energy * 2.0) + _Bass * 8.0);
                float arms  = 0.5 + 0.5 * sin(ang * _ArmFreq + _Mid * 6.0);
                float v = pow(rings * arms, 1.5);

                // Treble fine detail + energy intensity.
                v += 0.25 * _Treble * (0.5 + 0.5 * sin(r * _DetailFreq - t * 4.0));
                v *= (0.3 + 0.7 * _Energy);

                // Hue from centroid, radius and time; vignette for negative space.
                float hue = frac(_HueBase + _Centroid * 0.4 + r * 0.12 + t * _HueSpeed);
                float3 col = hsv2rgb(float3(hue, _Sat, saturate(v)));
                col *= smoothstep(_Vignette, _Vignette * 0.2, r);

                return fixed4(col * _Brightness, 1.0);
            }
            ENDCG
        }
    }
}
