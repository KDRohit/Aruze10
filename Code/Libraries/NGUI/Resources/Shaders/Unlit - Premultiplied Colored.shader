// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Premultiplied Colored"
{
	Properties
	{
		_MainTex ("Base RGBA", 2D) = "white" {}

		[Toggle] Alpha_Split("Use AlphaSplit?", Float) = 0
		_AlphaTex ("Base (AlphaSplit)", 2D) = "white" {}
	}

	SubShader
	{
		LOD 200

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			AlphaTest Off
			Fog { Mode Off }
			Offset -1, -1
			ColorMask RGB
			Blend One OneMinusSrcAlpha
		
			CGPROGRAM
			#pragma multi_compile ALPHA_SPLIT_OFF ALPHA_SPLIT_ON
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			#ifdef ALPHA_SPLIT_ON
			sampler2D _AlphaTex;
			#endif

			struct appdata_t
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				half4 col = tex2D(_MainTex, IN.texcoord);
				#ifdef ALPHA_SPLIT_ON
				col.a = tex2D(_AlphaTex, IN.texcoord).g;
				#endif
				col = col * IN.color;

				//col.rgb = lerp(half3(0.0, 0.0, 0.0), col.rgb, col.a);
				return col;
			}
			ENDCG
		}
	}
}
