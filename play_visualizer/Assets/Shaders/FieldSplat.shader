// Gameplay splat pass for the persistent visualizer field. Reads the current field (_MainTex) and
// applies up to _Count splats requested by gameplay through VisualizerField:
//   * paint  (mode > 0): additive tinted blob — an enemy death "color explosion".
//   * consume (mode < 0): multiplicative darkening — an enemy eating coverage into black.
// Run once per frame via Graphics.Blit into the field, so splats persist, advect, and fade with the
// field like any other painted content. Gameplay never touches this shader directly.
Shader "PlayVisualizer/FieldSplat"
{
    Properties
    {
        _MainTex ("Field", 2D) = "black" {}
        _Aspect ("Aspect", Float) = 1.777
        _Count ("Splat count", Float) = 0
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

            #define MAX_SPLATS 48

            sampler2D _MainTex;
            float _Aspect;
            float _Count;
            float _Time0;
            float4 _Splats[MAX_SPLATS];       // (u, v, radiusV, strength)
            float4 _SplatColors[MAX_SPLATS];  // (r, g, b, mode)

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
                float3 col = tex2D(_MainTex, i.uv).rgb;

                int count = (int)_Count;
                for (int s = 0; s < MAX_SPLATS; s++)
                {
                    if (s >= count) break;

                    float4 sp = _Splats[s];
                    float2 p = i.uv - sp.xy;
                    p.x *= _Aspect;                 // match the field's height-normalized metric
                    float d = length(p);
                    float radius = max(sp.z, 1e-4);
                    float4 sc = _SplatColors[s];

                    if (sc.w > 0.0)
                    {
                        // Paint: additive color burst (uniform disc).
                        float falloff = smoothstep(radius, 0.0, d);
                        if (falloff > 0.0) col += sc.rgb * sp.w * falloff;
                    }
                    else
                    {
                        // Consume: eat toward black, but with a NON-UNIFORM, slowly-wobbling boundary
                        // so the hole is organic (lobed + alive) instead of a perfect circle. The
                        // wobble is keyed to the splat position (so nearby holes differ) and time.
                        float ang = atan2(p.y, p.x);
                        float seed = dot(sp.xy, float2(23.14, 71.7));
                        float wob = 1.0
                                  + 0.35 * sin(ang * 3.0 + _Time0 * 2.2 + seed)
                                  + 0.18 * sin(ang * 6.0 - _Time0 * 1.6 + seed * 1.7);
                        float rr = radius * max(0.2, wob);
                        float falloff = smoothstep(rr, rr * 0.15, d); // softer, fuller bite
                        if (falloff > 0.0) col *= saturate(1.0 - sp.w * falloff);
                    }
                }

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
