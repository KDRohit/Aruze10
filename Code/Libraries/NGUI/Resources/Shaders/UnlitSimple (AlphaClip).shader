// Kind of a misgnomer, renderers using this shouldn't have alpha
Shader "Unlit/UnlitSimple (AlphaClip)"
{
  Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"
			        
            // texture we will sample
            sampler2D _MainTex;
			float4 _MainTex_ST;
			
            // vertex shader inputs
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

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
             	half4 col = tex2D(_MainTex, i.texcoord) * i.color;
             	
             	// Determine if we're in the clipping rect
				float2 factor = abs(i.worldPos);
				float val = 1.0 - max(factor.x, factor.y);
				
				// Clip if we're outside of it (below 0 value on val)
				clip(val);

				return col;
            }
            ENDCG
        }
    }
}