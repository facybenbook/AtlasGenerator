Shader "Hidden/AlphaGridShader"
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

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				fixed4 border = fixed4(0.5,0.5,0.5,0.5);
				if (((round((i.uv.y % 0.1) / 0.1) * 0.05 + i.uv.x) % 0.1) >= 0.05) {
					border = fixed4(0.75,0.75,0.75,0.75);
				}

				col.rgb = lerp(col.rgb, border.rgb, 1 - col.a);

				return col;
			}
			ENDCG
		}
	}
}
