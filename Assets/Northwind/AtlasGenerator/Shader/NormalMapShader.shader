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

			float3 FetchNormalVector( float2 tc, bool readFromTexture, bool useSobelFilter )
			{
				if( readFromTexture )
				{
					// Use the simple pre-computed look-up
					float3 n = tex2D( _MainTex, tc ).rgb;
					return normalize( n * 2.0f - 1.0f );
				}
				else
				{
					if( useSobelFilter )
					{   
						/*
						Coordinates are laid out as follows:
			
							0,0 | 1,0 | 2,0
							----+-----+----
							0,1 | 1,1 | 2,1
							----+-----+----
							0,2 | 1,2 | 2,2
						*/
			
						// Compute the necessary offsets:
						float2 o00 = tc + float2( -_MainTex_TexelSize.x, -_MainTex_TexelSize.y );
						float2 o10 = tc + float2(          0.0, -_MainTex_TexelSize.y );
						float2 o20 = tc + float2(  _MainTex_TexelSize.x, -_MainTex_TexelSize.y );

						float2 o01 = tc + float2( -_MainTex_TexelSize.x, 0.0          );
						float2 o21 = tc + float2(  _MainTex_TexelSize.x, 0.0          );

						float2 o02 = tc + float2( -_MainTex_TexelSize.x,  _MainTex_TexelSize.y );
						float2 o12 = tc + float2(          0.0,  _MainTex_TexelSize.y );
						float2 o22 = tc + float2(  _MainTex_TexelSize.x,  _MainTex_TexelSize.y );

						// Use of the sobel filter requires the eight samples
						// surrounding the current pixel:
						float h00 = tex2D( _MainTex, o00 ).r;
						float h10 = tex2D( _MainTex, o10 ).r;
						float h20 = tex2D( _MainTex, o20 ).r;
						
						float h01 = tex2D( _MainTex, o01 ).r;
						float h21 = tex2D( _MainTex, o21 ).r;
						
						float h02 = tex2D( _MainTex, o02 ).r;
						float h12 = tex2D( _MainTex, o12 ).r;
						float h22 = tex2D( _MainTex, o22 ).r;
			
						// The Sobel X kernel is:
						//
						// [ 1.0  0.0  -1.0 ]
						// [ 2.0  0.0  -2.0 ]
						// [ 1.0  0.0  -1.0 ]
			
						float Gx = h00 - h20 + 2.0 * h01 - 2.0 * h21 + h02 - h22;
						
						// The Sobel Y kernel is:
						//
						// [  1.0    2.0    1.0 ]
						// [  0.0    0.0    0.0 ]
						// [ -1.0   -2.0   -1.0 ]
			
						float Gy = h00 + 2.0 * h10 + h20 - h02 - 2.0 * h12 - h22;
			
						// Generate the missing Z component - tangent
						// space normals are +Z which makes things easier
						// The 0.5f leading coefficient can be used to control
						// how pronounced the bumps are - less than 1.0 enhances
						// and greater than 1.0 smoothes.
						float Gz = 0.5 * sqrt( 1.0 - Gx * Gx - Gy * Gy );

						// Make sure the returned normal is of unit length
						return normalize( float3( 2.0 * Gx, 2.0 * Gy, Gz ) );
					}
					else
					{
						// Determine the offsets
						float2 o1 = float2( _MainTex_TexelSize.x, 0.0         );
						float2 o2 = float2( 0.0,         _MainTex_TexelSize.y );

						// Take three samples to determine two vectors that can be
						// use to generate the normal at this pixel
						float h0 = tex2D( _MainTex, tc ).r;
						float h1 = tex2D( _MainTex, tc + o1 ).r;
						float h2 = tex2D( _MainTex, tc + o2 ).r;
						
						float3 v01 = float3( o1, h1 - h0 );
						float3 v02 = float3( o2, h2 - h0 );

						float3 n = cross( v01, v02 );

						// Can be useful to scale the Z component to tweak the
						// amount bumps show up, less than 1.0 will make them
						// more apparent, greater than 1.0 will smooth them out
						n.z *= 0.5f;

						return normalize( n );
					}
				}
			}

			float4 frag (v2f i) : SV_Target
			{
			
				float xLeft = dot(tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float xRight = dot(tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x, 0)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float yUp = dot(tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float yDown = dot(tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)), float3(0.3, 0.59, 0.11)).r * _Strength;
				float yZUp = dot(tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)), float3(0, 0, 1)).r * _Strength;
				float yZDown = dot(tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y)), float3(0, 0, 1)).r * _Strength;
				float xDelta = ((xLeft-xRight)+1)*0.5;
				float yDelta = ((yUp-yDown)+1)*0.5;
				float zDelta = ((yZUp-yZDown)+2)*0.5;
				float4 col = float4((xDelta * 2 - 1) * 0.1 + 0.5, (yDelta * 2 - 1) * 0.1 + 0.5, 0, 1);//zDelta + 0.5,1);
				//col = float4(float((xDelta + yDelta)).xxx, 1);
				col.b = lerp(0.5, FetchNormalVector(i.uv, false, true).b, _Strength / (1/3));//float4(FetchNormalVector(i.uv, false, true).bb, 0, 1);
				return col;
			}
			ENDCG
		}
	}
}
