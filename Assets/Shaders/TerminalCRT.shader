Shader "Custom/TerminalCRT"
{
    Properties
    {
        _BgColor ("Background", Color) = (0.05, 0.06, 0.08, 1)
        _ScanlineColor ("Scanline Tint", Color) = (0.0, 0.8, 1.0, 0.02) // Barely visible cyan
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

            float4 _BgColor;
            float4 _ScanlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base color
                float3 finalColor = _BgColor.rgb;

                // Micro-scanlines (dense, static)
                float scanlines = sin(i.uv.y * 1000.0) * 0.5 + 0.5;
                finalColor -= scanlines * 0.015; // Darken slightly on scanlines

                // Slow rolling sync bar (like an old monitor refreshing)
                float roll = sin(i.uv.y * 5.0 + _Time.y * 1.5);
                roll = smoothstep(0.95, 1.0, roll);
                finalColor += roll * _ScanlineColor.rgb * _ScanlineColor.a;

                // Heavy CRT Vignette (darkens corners)
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * 1.8;
                finalColor *= smoothstep(0.0, 1.0, vignette);

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}