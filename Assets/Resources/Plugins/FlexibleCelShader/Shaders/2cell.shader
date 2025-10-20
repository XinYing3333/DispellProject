Shader "FlexibleCelShader/Cel Outline2"
{
	Properties
	{

		_OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
		_OutlineSize("Outline Size", Range(0, 20)) = 10

	}
	
	SubShader
	{

		// This Pass Renders the outlines
		Cull Front
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
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
				float4 vertex : SV_POSITION;
			};

			float _OutlineSize;
			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				worldPos.xyz = worldPos.xyz + worldNormal * _OutlineSize * 0.005;
				o.vertex = mul(UNITY_MATRIX_VP, worldPos);
				return o;
			}

			float4 _OutlineColor;
			fixed4 frag(v2f i) : SV_Target
			{
				return _OutlineColor;
			}
				ENDCG
		}// End Outline Pass

		// Shadow casting
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}