Shader "Hidden/AtlasGen/NormalMapShader"
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
				
				float4 hG = tex2D(_MainTex, i.uv);
				float4 hR = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0));
				float4 hA = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y));

				float height_Ground = (hG.r + hG.g + hG.b) / 3;
				float height_Right = (hR.r + hR.g + hR.b) / 3;
				float height_Above = (hA.r + hA.g + hA.b) / 3;

				float height_Difference_R = (height_Right - height_Ground) * _Strength;
				float height_Difference_A = (height_Above - height_Ground) * _Strength;

				float3 dif_Vector_R = float3(1, 0, height_Difference_R);
				float3 dif_Vector_A = float3(0, 1, height_Difference_A);

				float3 dir_Vector = cross(dif_Vector_R, dif_Vector_A);
				float3 nor_Vector = normalize(dir_Vector);

				return float4(nor_Vector.xyz, 1) / 2 + 0.5;
			}
			ENDCG
		}
	}
}
