Shader "Unlit/Leaf2"
{
   Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 cameraRight = UNITY_MATRIX_IT_MV._m00_m10_m20;
                float3 cameraUp = UNITY_MATRIX_IT_MV._m01_m11_m21;

                float2 offset = v.vertex.xy;

                float3 worldPos = unity_ObjectToWorld._m03_m13_m23 +
                                  offset.x * cameraRight +
                                  offset.y * cameraUp;

                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }
}
