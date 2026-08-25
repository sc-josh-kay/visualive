// Transient "firework" spark burst for an enemy hitting the player. Instead of uniform straight
// spokes, it renders N discrete sparks — each with a hash-randomized angle, length, expansion speed
// and a curved (arched) trajectory — so every burst is unique (via _Seed) and organic, matching the
// alive/non-uniform feel of the smoke. Additively blended, enemy-tinted, paints NOTHING into the
// smoke field (the black hole it leaves is done separately via VisualizerField.Consume). Driven by
// the CollisionBurst component on a world quad.
Shader "PlayVisualizer/CollisionBurst"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.4, 0.1, 1)
        _Progress ("Progress 0..1", Float) = 0
        _Sparks ("Spark count", Float) = 22
        _Curl ("Arch amount", Float) = 0.8
        _Width ("Spark width", Float) = 0.06
        _Tail ("Trail falloff", Float) = 1.5
        _Seed ("Per-burst seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend One One   // additive glow

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_SPARKS 32
            #define TWO_PI 6.2831853

            float4 _Color;
            float _Progress, _Sparks, _Curl, _Width, _Tail, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash11(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

            fixed4 frag (v2f i) : SV_Target
            {
                float p = saturate(_Progress);

                float2 c = i.uv - 0.5;
                float r = length(c) * 2.0;          // 0 center, 1 quad edge
                float ang = atan2(c.y, c.x);

                // Ease-out expansion: sparks shoot out fast, then coast.
                float pe = 1.0 - pow(1.0 - p, 2.0);

                float glow = 0.0;   // enemy-colored trails
                float head = 0.0;   // hot white leading sparks

                int n = (int)_Sparks;
                for (int s = 0; s < MAX_SPARKS; s++)
                {
                    if (s >= n) break;

                    float fs = float(s);
                    float h1 = hash11(fs * 1.7 + _Seed * 3.3);
                    float h2 = hash11(fs * 2.9 + _Seed * 1.7);
                    float h3 = hash11(fs * 4.1 + _Seed * 2.3);
                    float h4 = hash11(fs * 5.7 + _Seed * 0.9);

                    float baseAng = h1 * TWO_PI;        // random (clustered) directions
                    float len     = 0.5 + 0.5 * h2;     // varying reach
                    float curl    = (h3 - 0.5) * 2.0 * _Curl;
                    float speed   = 0.75 + 0.5 * h4;    // varying expansion speed

                    float tipR = min(len * pe * speed, 1.0);
                    if (r > tipR + 0.03) continue;

                    // Curved path: the spark's angle drifts with radius (arch grows ~r²).
                    float expAng = baseAng + curl * r * r;
                    float da = ang - expAng;
                    da = atan2(sin(da), cos(da));       // wrap to [-pi, pi]
                    float arc = da * max(r, 0.06);      // ~perpendicular screen distance
                    float w = _Width * (0.6 + 0.8 * h2);
                    float streak = exp(-(arc * arc) / (2.0 * w * w));

                    float trail = pow(saturate(r / max(tipR, 1e-3)), _Tail); // brighter toward tip
                    float tip = smoothstep(tipR, tipR - 0.06, r);            // bright leading spark

                    glow += streak * (0.35 * trail);
                    head += streak * tip;
                }

                // Envelope: snap in, ease out; plus a brief central flash.
                float fadeIn  = smoothstep(0.0, 0.06, p);
                float fadeOut = 1.0 - smoothstep(0.5, 1.0, p);
                float life = fadeIn * fadeOut;
                float core = smoothstep(0.12, 0.0, r) * (1.0 - smoothstep(0.0, 0.25, p));

                // Dominantly the enemy's color (trails + heads + core), with only a small white
                // hotspot — so each enemy's burst clearly reads as ITS color, not generic white.
                float colored = glow + head * 0.9 + core * 0.8;
                float3 col = _Color.rgb * (colored * life)
                           + float3(1, 1, 1) * ((head * 0.25 + core * 0.4) * life);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
