// Simple two-texture crossfade used for pattern transitions. out = lerp(A, B, _T).
Shader "PlayVisualizer/Blend"
{
    Properties
    {
        _MainTex ("A", 2D) = "black" {}
        _TexB ("B", 2D) = "black" {}
        _T ("Blend", Float) = 0
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

            sampler2D _MainTex, _TexB;
            float _T;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 a = tex2D(_MainTex, i.uv).rgb;
                float3 b = tex2D(_TexB, i.uv).rgb;
                return fixed4(lerp(a, b, saturate(_T)), 1.0);
            }
            ENDCG
        }
    }
}
