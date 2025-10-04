Shader "Custom/OutlineOnly"
{
    Properties
    {
        _OutColor ("Outline Color", Color) = (0,0,0,1)
        _OutThickness ("Outline Thickness", Range(0.0, 0.2)) = 0.05
        _OutFade ("Outline Fade", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Front  // 只畫背面，避免和原模型重疊

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _OutColor;
            float _OutThickness;
            float _OutFade;

            v2f vert(appdata v)
            {
                v2f o;
                float3 inflated = v.vertex.xyz + normalize(v.normal) * _OutThickness;
                o.pos = UnityObjectToClipPos(float4(inflated, 1));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(_OutColor.rgb, _OutColor.a * _OutFade);
            }
            ENDCG
        }
    }
}
