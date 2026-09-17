Shader "Custom/Cp"
{
    Properties
    {
        [NoScaleOffset] _Chars ("Character Texture (Grid)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM // Built-in uses CGPROGRAM block
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // Replaced URP's Core.hlsl

            struct Attributes
            {
                float4 vertex       : POSITION; // renamed from positionOS
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex       : SV_POSITION; // renamed from positionCS
                float2 uv           : TEXCOORD0;
            };

            // Standard Built-in sampler declarations
            sampler2D _Chars;
            sampler2D _NoiseTex;

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Replaced URP TransformObjectToHClip with Built-in function
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 UV = input.uv;

                // Random character
                float2 uv = frac(UV * 32.0); 
                
                // Replaced URP Sample() syntax with traditional tex2D()
                float noise = tex2D(_NoiseTex, UV).g;
                noise = round(noise * 10.0) / 10.0; 
                
                uv.x = (uv.x / 10.0) - 0.005; 
                uv.x += noise; 
                
                uv.x += round(_Time.y * 0.5 * 10.0) / 10.0; 

                // Distortion
                float rain = UV.y; 
                float distortion = tex2D(_NoiseTex, UV / float2(1.0, 32.0)).g; 
                distortion = round(distortion * 10.0) / 10.0; 
                
                rain -= round(_Time.y * 0.2 * 32.0) / 32.0; 
                rain += distortion; 
                rain = frac(rain); 
                rain = round(rain * 16.0) / 16.0; 
                rain = pow(max(0.0, rain), 3.0); 
                rain *= 2.0; 

                // Color calculation
                float3 finalColor = tex2D(_Chars, uv).rgb * rain * float3(0.0, 1.0, 0.0);
                float alpha = rain; 

                return half4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
