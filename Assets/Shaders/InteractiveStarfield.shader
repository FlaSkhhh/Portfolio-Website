Shader "Custom/InteractiveStarfield"
{
    Properties
    {
        _StarColor ("Star Color", Color) = (0.5, 0.8, 1.0, 0.15)
        _BgColor ("Background Color", Color) = (0.043, 0.047, 0.063, 1)
        _MouseGlowColor ("Pointer Glow Color", Color) = (0.0, 0.898, 1.0, 1.0)
        _Speed ("Flight Speed", Float) = 0.02 
        _InteractionRadius ("Glow Radius", Float) = 0.25
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0; // Using ScreenPos instead of UVs
            };

            float4 _StarColor;
            float4 _BgColor;
            float4 _MouseGlowColor;
            float4 _MousePos;
            float _Speed;
            float _InteractionRadius;

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
                
                // Calculate physical screen coordinates regardless of Quad size
                o.screenPos = ComputeScreenPos(o.vertex); 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize screen coordinates from 0.0 to 1.0
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                
                // Center the screen UVs at 0,0 and correct for aspect ratio
                float2 uv = screenUV - 0.5;
                uv.x *= aspect; 
                
                // Apply the exact same centering and aspect ratio to the C# Mouse input
                float2 aspectMouse = _MousePos.xy - 0.5;
                aspectMouse.x *= aspect;
                
                // Now the distance math is 1:1 perfect!
                float mouseDist = length(uv - aspectMouse);
                float mouseInteraction = smoothstep(_InteractionRadius, 0.0, mouseDist);

                float3 color = _BgColor.rgb;
                float time = _Time.y * _Speed;

                for(float j = 1.0; j <= 3.0; j++)
                {
                    float2 layerUV = uv * (10.0 * j);
                    layerUV.y += time * (5.0 / j); 
                    layerUV.x += time * (2.0 / j); 
                    
                    float2 cellId = floor(layerUV);
                    float2 cellUv = frac(layerUV) - 0.5;
                    float randomValue = Hash(cellId);
                    
                    if(randomValue > 0.95) 
                    {
                        float star = smoothstep(0.15, 0.0, length(cellUv));
                        float brightness = (randomValue - 0.95) * 20.0;
                        brightness += mouseInteraction * 10.0; 
                        color += _StarColor.rgb * star * _StarColor.a * brightness;
                    }
                }

                color += _MouseGlowColor.rgb * mouseInteraction * 0.035;

                // Vignette now strictly hugs the edges of the visible screen!
                float2 vignetteCenter = screenUV - 0.5;
                float vignette = 1.0 - dot(vignetteCenter, vignetteCenter) * 2.5;
                color *= smoothstep(0.0, 1.0, vignette);

                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}