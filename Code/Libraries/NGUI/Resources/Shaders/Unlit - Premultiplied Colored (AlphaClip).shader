// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Premultiplied Colored (AlphaClip)"
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
			float4 _MainTex_ST;
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
				float2 worldPos : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				o.worldPos = TRANSFORM_TEX(v.vertex.xy, _MainTex);
				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				// Sample the texture
				half4 col = tex2D(_MainTex, IN.texcoord);
				#ifdef ALPHA_SPLIT_ON
				col.a = tex2D(_AlphaTex, IN.texcoord).g;
				#endif
				col = col * IN.color;

				float2 factor = abs(IN.worldPos);
				float val = 1.0 - max(factor.x, factor.y);

				// Option 1: 'if' statement
				if (val < 0.0) col = half4(0.0, 0.0, 0.0, 0.0);

				// Option 2: no 'if' statement -- may be faster on some devices
				//col *= ceil(clamp(val, 0.0, 1.0));
				return col;
			}
			ENDCG
		}
	}

}
