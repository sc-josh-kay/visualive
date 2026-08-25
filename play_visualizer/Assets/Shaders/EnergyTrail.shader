// Additive trail for weapon projectiles — the "energy" filament (spec7). Uses the TrailRenderer's
// per-vertex color (set per projectile from the music) and softens the edges across the trail width,
// so shots read as sharp, bright, short-lived energy — visually distinct from the organic smoke paint.
Shader "PlayVisualizer/EnergyTrail"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
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

            float4 _Tint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float2 uv : TEXCOORD0; float4 color : COLOR; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // uv.y goes 0..1 across the trail width — soft gaussian falloff for a fuzzy glowing
                // line (softer than a smoothstep, so the wobble reads as smooth bending).
                float d = i.uv.y - 0.5;
                float edge = exp(-d * d * 7.0);
                float a = i.color.a * edge;
                float3 col = i.color.rgb * _Tint.rgb * a;
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
