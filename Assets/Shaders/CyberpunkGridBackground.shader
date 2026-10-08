Shader "Custom/CyberpunkGrid3D"
{
    Properties
    {
        _BgColor ("Background Color", Color) = (0.02, 0.02, 0.025, 1)
        _GridColor ("Grid Line Color", Color) = (1.0, 0.88, 0.0, 1.0)
        
        [Header(3D Settings)]
        _GridScale ("Grid Density", Float) = 2.0
        _LineThickness ("Line Thickness", Range(0.1, 4.0)) = 1.0
        _PerspectiveStrength ("3D Depth Strength", Range(0.1, 5.0)) = 0.8
        _HorizonOffset ("Horizon Y Offset", Range(-0.5, 0.5)) = 0.0
        _HorizonFade ("Horizon Fade Distance", Range(0.01, 0.5)) = 0.15
        
        [Header(Motion)]
        _GridRotation ("Grid Rotation (Set to 0 for classic 3D highway)", Float) = 0.0
        _GridOpacity ("Grid Opacity", Range(0,1)) = 0.15
        _ScrollSpeed ("Scroll Speed (X=Strafe, Y=Forward)", Vector) = (0.0, -1.0, 0, 0)
        _PulseSpeed ("Line Pulse Speed", Float) = 0.5
        _PulseAmount ("Line Pulse Amount", Range(0,1)) = 0.15

        [Header(Glitch FX)]
        _GlitchIntensity ("Glitch Intensity", Range(0,1)) = 0.3
        _GlitchFrequency ("Glitch Check Rate (per sec)", Float) = 1.5
        _GlitchChance ("Glitch Chance Per Check", Range(0,1)) = 0.08

        [Header(Mouse Interaction)]
        _MouseGlowColor ("Pointer Glow Color", Color) = (1.0, 0.95, 0.3, 1.0)
        _InteractionRadius ("Glow Radius", Float) = 0.2
        _MouseGlowIntensity ("Pointer Glow Intensity", Range(0,1)) = 0.25

        _VignetteStrength ("Vignette Strength", Float) = 2.5
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
            #pragma target 3.0 
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            float4 _BgColor;
            float4 _GridColor;
            float _GridScale;
            float _LineThickness;
            float _GridRotation;
            float _GridOpacity;
            float4 _ScrollSpeed;
            float _PulseSpeed;
            float _PulseAmount;

            float _PerspectiveStrength;
            float _HorizonOffset;
            float _HorizonFade;

            float _GlitchIntensity;
            float _GlitchFrequency;
            float _GlitchChance;

            float4 _MousePos;
            float4 _MouseGlowColor;
            float _InteractionRadius;
            float _MouseGlowIntensity;

            float _VignetteStrength;

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
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 uv = screenUV - 0.5;
                uv.x *= aspect;
                
                // Allow moving the horizon line up or down
                uv.y += _HorizonOffset;

                float2 aspectMouse = _MousePos.xy - 0.5;
                aspectMouse.x *= aspect;

                float time = _Time.y;

                // ---------------- GLITCH (Applied in 2D space) ----------------
                float glitchStep = floor(time * _GlitchFrequency);
                float glitchRoll = Hash(float2(glitchStep, 0.0));
                float glitchActive = step(1.0 - _GlitchChance, glitchRoll);

                float2 glitchUV = uv;
                if (glitchActive > 0.5)
                {
                    float bandY = Hash(float2(glitchStep, 1.0)) * 2.0 - 1.0;
                    float bandHeight = 0.05 + Hash(float2(glitchStep, 2.0)) * 0.08;
                    float inBand = step(bandY, uv.y) * step(uv.y, bandY + bandHeight);

                    float offsetAmount = (Hash(float2(glitchStep, 3.0)) - 0.5) * 0.08 * _GlitchIntensity;
                    glitchUV.x += inBand * offsetAmount;
                }

                // ---------------- 3D PERSPECTIVE MATH ----------------
                // We take the absolute value of Y so we get both a Floor AND a Ceiling.
                float yDist = max(abs(glitchUV.y), 0.001); 
                
                // Z depth increases toward infinity as Y approaches the horizon (0)
                float zDepth = _PerspectiveStrength / yDist;

                // Warp the UVs: X flares outward, Y stretches forward
                float2 uv3D = float2(glitchUV.x * zDepth, zDepth);

                // ---------------- GRID DRAWING ----------------
                float rad = radians(_GridRotation);
                float2x2 rotMat = float2x2(cos(rad), -sin(rad), sin(rad), cos(rad));
                float2 gridUV = mul(rotMat, uv3D);

                gridUV *= _GridScale;
                
                // Apply scroll speed. Z depth requires much faster scrolling to feel right.
                gridUV.x += time * _ScrollSpeed.x;
                gridUV.y += time * _ScrollSpeed.y * 5.0; 

                float2 gridFrac = abs(frac(gridUV) - 0.5);
                float2 gridDeriv = fwidth(gridUV) * _LineThickness;
                float2 gridLines = smoothstep(gridDeriv, float2(0, 0), gridFrac);
                float gridMask = max(gridLines.x, gridLines.y);

                // ---------------- HORIZON FADE ----------------
                // Smoothly fade out the grid as it reaches the center vanishing point
                float horizonFade = smoothstep(0.0, _HorizonFade, yDist);
                gridMask *= horizonFade;

                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseAmount;
                gridMask *= pulse;

                // ---------------- MOUSE GLOW (2D Screen Space Overlay) ----------------
                float mouseDist = length(uv - aspectMouse - float2(0, _HorizonOffset));
                float mouseInteraction = smoothstep(_InteractionRadius, 0.0, mouseDist);

                // ---------------- COMPOSE ----------------
                float3 color = _BgColor.rgb;

                float gridStrength = gridMask * _GridOpacity;
                gridStrength += gridMask * mouseInteraction * _MouseGlowIntensity;

                float3 gridColor = _GridColor.rgb;
                if (glitchActive > 0.5)
                {
                    gridColor.r *= 1.0 + 0.3 * _GlitchIntensity;
                    gridColor.b *= 1.0 - 0.2 * _GlitchIntensity;
                }

                color += gridColor * gridStrength;
                color += _MouseGlowColor.rgb * mouseInteraction * _MouseGlowIntensity * 0.15;

                float2 vignetteCenter = screenUV - 0.5;
                float vignette = 1.0 - dot(vignetteCenter, vignetteCenter) * _VignetteStrength;
                color *= smoothstep(0.0, 1.0, vignette);

                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}