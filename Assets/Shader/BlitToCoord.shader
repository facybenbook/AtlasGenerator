Shader "Hidden/AtlasGen/BlitToCoord"
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

			float4 _BlitSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				float2 nUV = float2(v.uv.x, 1 - v.uv.y);
				o.uv = (nUV - _BlitSize.xy) * (1 / _BlitSize.zw);//float2(-_BlitSize.x, _BlitSize.y) + (v.uv * 2);// + (v.uv * (1 / _BlitSize.zw));
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, float2(i.uv.x, 1 - i.uv.y));

				if ((i.uv.x < 0 || i.uv.x > 1) || (i.uv.y < 0 || i.uv.y > 1)) {
					clip(-1);
				}

				return col;
			}
			ENDCG
		}
	}
}
