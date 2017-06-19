Shader "Hidden/NormalMapShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float _Strength;

			float4 frag (v2f i) : SV_Target
			{
			
				float xLeft = dot(tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float xRight = dot(tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x, 0)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float yUp = dot(tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float yDown = dot(tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float xDelta = ((xLeft-xRight)+1)*0.5;
				float yDelta = ((yUp-yDown)+1)*0.5;
				float4 col = float4(xDelta,yDelta,1,1);

				return col;
			}
			ENDCG
		}
	}
}
