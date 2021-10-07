// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Additive Colored"
{
	Properties
	{
		_MainTex ("Base RGBA", 2D) = "white" {}

		[Toggle] Alpha_Split("Use AlphaSplit?", Float) = 0
		_AlphaTex ("Base (AlphaSplit)", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		LOD 100
		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		ColorMask RGB
		AlphaTest Greater .01
		Blend One One
		
		Pass
		{
			CGPROGRAM
				#pragma multi_compile ALPHA_SPLIT_OFF ALPHA_SPLIT_ON
				#pragma vertex vert
				#pragma fragment frag
				
				#include "UnityCG.cginc"
	
				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					fixed4 color : COLOR;
				};
	
				struct v2f
				{
					float4 vertex : SV_POSITION;
					half2 texcoord : TEXCOORD0;
					fixed4 color : COLOR;
				};
	
				sampler2D _MainTex;
				float4 _MainTex_ST;
				#ifdef ALPHA_SPLIT_ON
				sampler2D _AlphaTex;
				#endif

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.color = v.color;
					return o;
				}
				
				fixed4 frag (v2f i) : COLOR
				{
					fixed4 col = tex2D(_MainTex, i.texcoord);
					#ifdef ALPHA_SPLIT_ON
					col.a = tex2D(_AlphaTex, i.texcoord).g;
					#endif
					return col * i.color;
				}
			ENDCG
		}
	}
}