// Additive glow sprite for weapon projectile heads (core + halo) and impact blooms (spec7 organic
// pass). Multiplies a soft radial texture by the SpriteRenderer's per-instance color and blends it
// additively, so heads read as soft glowing orbs tinted by the music — energy, never coverage.
Shader "PlayVisualizer/EnergySprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float2 uv : TEXCOORD0; float4 color : COLOR; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).a * i.color.a;
                return fixed4(i.color.rgb * a, a);
            }
            ENDCG
        }
    }
}
