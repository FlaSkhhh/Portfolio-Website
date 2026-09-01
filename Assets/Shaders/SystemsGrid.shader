Shader "Custom/SystemsGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0, 0.898, 1, 0.25) // Cyan with low alpha
        _BgColor ("Background Color", Color) = (0.043, 0.047, 0.063, 1) // #0B0C10
        _GridSize ("Grid Size", Float) = 15.0
        _LineThickness ("Line Thickness", Float) = 0.03
        _Speed ("Scroll Speed (X, Y)", Vector) = (0.02, 0.04, 0, 0)    }
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

            float4 _GridColor;
            float4 _BgColor;
            float _GridSize;
            float _LineThickness;
            float4 _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Scroll UVs vertically over time
                float2 scrolledUV = i.uv + float2(_Time.y * _Speed.x, _Time.y * _Speed.y);                
                // Generate the grid mathematically (no textures required)
                float2 grid = frac(scrolledUV * _GridSize);
                
                // Use smoothstep for cheap anti-aliasing on WebGL
                float linesX = smoothstep(_LineThickness, _LineThickness + 0.02, grid.x);
                float linesY = smoothstep(_LineThickness, _LineThickness + 0.02, grid.y);
                float gridLine = 1.0 - (linesX * linesY);
                
                // Create a Vignette to fade the grid out at the edges
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * 2.5;
                vignette = smoothstep(0.0, 1.0, vignette);

                // Mix background color and grid color
                float4 finalColor = lerp(_BgColor, _GridColor, gridLine * _GridColor.a * vignette);
                
                return finalColor;
            }
            ENDCG
        }
    }
}