Shader "Custom/AmbientStarfield"
{
    Properties
    {
        _StarColor ("Star Color", Color) = (0.5, 0.8, 1.0, 0.15) // Very dim blue/white
        _BgColor ("Background Color", Color) = (0.043, 0.047, 0.063, 1)
        _Speed ("Flight Speed", Float) = 0.02 // Extremely slow
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            float4 _StarColor;
            float4 _BgColor;
            float _Speed;

            // Math-based pseudo-random number generator
            float Hash(float2 p)
            {
                float3 p3  = frac(float3(p.xyx) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5; // Center UVs
                uv.x *= 1.77; // Approximate 16:9 aspect ratio correction
                
                float3 color = _BgColor.rgb;
                float time = _Time.y * _Speed;

                // Create 3 layers of depth
                for(float j = 1.0; j <= 3.0; j++)
                {
                    // Scale and shift UVs for this layer
                    float2 layerUV = uv * (10.0 * j);
                    layerUV.y += time * (5.0 / j); // Move downwards
                    layerUV.x += time * (2.0 / j); // Slight diagonal drift
                    
                    // Create grid cells for stars
                    float2 cellId = floor(layerUV);
                    float2 cellUv = frac(layerUV) - 0.5;
                    
                    // Randomize star position inside cell and its brightness
                    float randomValue = Hash(cellId);
                    
                    // Only draw a star if random value is very high (sparse stars)
                    if(randomValue > 0.95) 
                    {
                        // Soft circle math
                        float star = smoothstep(0.15, 0.0, length(cellUv));
                        // Vary brightness based on layer and random value
                        color += _StarColor.rgb * star * _StarColor.a * (randomValue - 0.95) * 20.0;
                    }
                }

                // Subtle Vignette
                float vignette = 1.0 - dot(i.uv - 0.5, i.uv - 0.5) * 2.5;
                color *= smoothstep(0.0, 1.0, vignette);

                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}