Shader "Custom/CyberpunkGrid3D"
{
    Properties
    {
        [Header(Background)]
        _BgTopColor ("Top Color", Color) = (0.169, 0.039, 0.020, 1)
        _BgBottomColor ("Bottom Color", Color) = (0.012, 0.004, 0.0, 1)
        _GradientPower ("Gradient Falloff", Range(0.5, 3.0)) = 1.15
        _HorizonGlowColor ("Top Glow Color", Color) = (0.55, 0.06, 0.05, 1)
        _HorizonGlow ("Top Glow Strength", Range(0, 1)) = 0.35

        [Header(3D Grid)]
        _GridColor ("Grid Line Color", Color) = (1.0, 0.88, 0.0, 1.0)
        _GridScale ("Grid Density", Float) = 2.0
        _LineThickness ("Line Thickness", Range(0.1, 4.0)) = 1.0
        _PerspectiveStrength ("3D Depth Strength", Range(0.1, 5.0)) = 0.8
        _HorizonOffset ("Horizon Y Offset", Range(-0.5, 0.5)) = 0.0
        _HorizonFade ("Horizon Fade Distance", Range(0.01, 0.5)) = 0.15
        _MoireStart ("Fade Lines Denser Than (cells/pixel)", Range(0.02, 1.0)) = 0.1
        _GridRotation ("Grid Rotation", Float) = 0.0
        _GridOpacity ("Grid Opacity", Range(0, 1)) = 0.15
        _ScrollSpeed ("Scroll Speed (X=Strafe, Y=Forward)", Vector) = (0, -1, 0, 0)
        _PulseSpeed ("Line Pulse Speed", Float) = 0.5
        _PulseAmount ("Line Pulse Amount", Range(0, 1)) = 0.15

        [Header(Texture Layers)]
        _DotColor ("Dot Color", Color) = (0.9, 0.15, 0.1, 1)
        _DotIntensity ("Dot Intensity", Range(0, 1)) = 0.3
        _DotRows ("Dot Rows Per Screen Height", Float) = 170
        _BlockColor ("Ghost Block Color", Color) = (0.8, 0.1, 0.1, 1)
        _BlockIntensity ("Ghost Block Intensity", Range(0, 1)) = 0.08
        _BlockCols ("Block Columns", Float) = 10
        _BlockRows ("Block Rows", Float) = 6
        _AccentColor ("Cyan Accent", Color) = (0.0, 0.9, 1.0, 1)
        _CrossIntensity ("Plus-Mark Intensity", Range(0, 1)) = 0.18
        _CrossDensity ("Plus-Mark Density", Float) = 28
        _ScanBarIntensity ("Scan Bar Intensity", Range(0, 0.3)) = 0.04
        _ScanPeriod ("Scan Bar Period (sec)", Float) = 14

        [Header(Glitch)]
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.6
        _GlitchInterval ("Avg Seconds Between Bursts", Float) = 8
        _BurstLength ("Burst Length (sec)", Range(0.1, 1.0)) = 0.35
        _PulseDim ("UI Pulse Dim", Range(0, 0.8)) = 0.3

        [Header(Mouse and Vignette)]
        _MouseGlowColor ("Pointer Glow Color", Color) = (1.0, 0.95, 0.3, 1.0)
        _InteractionRadius ("Glow Radius", Float) = 0.2
        _MouseGlowIntensity ("Pointer Glow Intensity", Range(0, 1)) = 0.25
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

            struct appdata { float4 vertex : POSITION; };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 glitch : TEXCOORD1; // x = band Y, y = band height, z = band X shift, w = strength (0 = off)
                float4 sliver : TEXCOORD2; // x = Y, y = X start, z = length, w = on/off
            };

            float4 _BgTopColor, _BgBottomColor, _HorizonGlowColor;
            float _GradientPower, _HorizonGlow;

            float4 _GridColor, _ScrollSpeed;
            float _GridScale, _LineThickness, _PerspectiveStrength, _HorizonOffset, _HorizonFade;
            float _MoireStart, _GridRotation, _GridOpacity, _PulseSpeed, _PulseAmount;

            float4 _DotColor, _BlockColor, _AccentColor;
            float _DotIntensity, _DotRows, _BlockIntensity, _BlockCols, _BlockRows;
            float _CrossIntensity, _CrossDensity, _ScanBarIntensity, _ScanPeriod;

            float _GlitchIntensity, _GlitchInterval, _BurstLength, _PulseDim;
            float _GlitchPulse; // set globally from C# (not in Properties on purpose)

            float4 _MousePos;
            float4 _MouseGlowColor;
            float _InteractionRadius, _MouseGlowIntensity, _VignetteStrength;

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);

                // Glitch decisions are identical for every pixel, so they are rolled once per vertex.
                float time = _Time.y;
                float slotLen = max(_BurstLength, 0.05);
                float slot = floor(time / slotLen);
                float chance = saturate(slotLen / max(_GlitchInterval, 0.1));
                float randomBurst = step(1.0 - chance, Hash(float2(slot, 11.0)));

                float strength = max(randomBurst * _GlitchIntensity, saturate(_GlitchPulse));

                // Inside a burst: re-roll ~24x/sec, and drop some ticks so it stutters
                float tick = floor(time * 24.0);
                strength *= step(0.25, Hash(float2(tick, 5.0)));

                o.glitch = float4(
                    Hash(float2(tick, 1.0)),
                    0.02 + Hash(float2(tick, 2.0)) * 0.07,
                    (Hash(float2(tick, 3.0)) - 0.5) * 0.10,
                    strength);

                float sliverOn = step(0.45, Hash(float2(tick, 6.0))) * step(0.001, strength);
                o.sliver = float4(
                    Hash(float2(tick, 7.0)),
                    Hash(float2(tick, 8.0)) * 0.7,
                    0.1 + Hash(float2(tick, 9.0)) * 0.3,
                    sliverOn);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float time = _Time.y;
                float strength = i.glitch.w;
                float glitching = step(0.001, strength);

                // ---- Tear band (rolled in screen space, so it always lands on screen) ----
                float inBand = step(i.glitch.x, screenUV.y) * step(screenUV.y, i.glitch.x + i.glitch.y) * glitching;
                float2 sUV = screenUV;
                sUV.x += inBand * i.glitch.z * strength;

                // ---- Gradient + glow at the top, where your horizon sits ----
                float g = pow(saturate(screenUV.y), _GradientPower);
                float3 color = lerp(_BgBottomColor.rgb, _BgTopColor.rgb, g);
                float gy = (1.0 - screenUV.y) / 0.18;
                color += _HorizonGlowColor.rgb * exp(-gy * gy) * _HorizonGlow;

                // ---- 3D floor grid ----
                float2 uv = sUV - 0.5;
                uv.x *= aspect;
                uv.y += _HorizonOffset;

                float yDist = max(abs(uv.y), 0.001);
                float zDepth = _PerspectiveStrength / yDist;
                float2 uv3D = float2(uv.x * zDepth, zDepth);

                float rs, rc;
                sincos(radians(_GridRotation), rs, rc);
                float2 gridUV = mul(float2x2(rc, -rs, rs, rc), uv3D) * _GridScale;
                gridUV += time * float2(_ScrollSpeed.x, _ScrollSpeed.y * 5.0);

                float2 fw = fwidth(gridUV);
                float2 gridFrac = abs(frac(gridUV) - 0.5);
                //float2 gridLines = 1.0 - smoothstep(float2(0.0, 0.0), fw * _LineThickness + 0.0001, gridFrac);        //removed this beacuse horzontal line was tweaking on mobile
                float2 gridLines = 1.0 - smoothstep(float2(0.0, 0.0), fw * _LineThickness + (fw * 1.5), gridFrac);
                float gridMask = max(gridLines.x, gridLines.y);

                gridMask *= 1.0 - smoothstep(_MoireStart, _MoireStart * 3.0, max(fw.x, fw.y));
                gridMask *= smoothstep(0.0, _HorizonFade, yDist);
                gridMask *= 1.0 + sin(time * _PulseSpeed) * _PulseAmount;

                // ---- Mouse glow ----
                float2 pos = float2((screenUV.x - 0.5) * aspect, screenUV.y - 0.5);
                float2 mousePos = float2((_MousePos.x - 0.5) * aspect, _MousePos.y - 0.5);
                float mouse = 1.0 - smoothstep(0.0, _InteractionRadius, length(pos - mousePos));

                float3 gridColor = lerp(_GridColor.rgb, _AccentColor.rgb, inBand * 0.7);
                color += gridColor * gridMask * (_GridOpacity + mouse * _MouseGlowIntensity);
                color += _MouseGlowColor.rgb * mouse * _MouseGlowIntensity * 0.15;

                // ---- Vignette on the base only (lighter at the top so the glow survives) ----
                float2 vc = screenUV - 0.5;
                vc.y *= lerp(1.0, 0.35, step(0.0, vc.y));
                color *= smoothstep(0.0, 1.0, 1.0 - dot(vc, vc) * _VignetteStrength);

                // ---- Dot-matrix rows (strongest at the side edges and toward the top) ----
                float topW = smoothstep(0.1, 1.0, screenUV.y);
                if (_DotIntensity > 0.001)
                {
                    float2 dp = float2(sUV.x * aspect, sUV.y) * _DotRows;
                    float2 dc = floor(dp);
                    float cellX = (dc.x + 0.5) / (_DotRows * aspect);
                    float cellEdge = smoothstep(0.18, 0.5, abs(cellX - 0.5));
                    float rowSeed = Hash(float2(dc.y, 17.0));
                    float rowOn = step(0.45, Hash(float2(dc.y, floor(time * 0.4 + rowSeed * 10.0))));
                    float cellOn = step(1.0 - lerp(0.10, 0.55, cellEdge), Hash(dc));
                    float shape = 1.0 - smoothstep(0.28, 0.38, max(abs(frac(dp.x) - 0.5), abs(frac(dp.y) - 0.5)));
                    color += _DotColor.rgb * _DotIntensity * rowOn * cellOn * shape * (0.25 + 0.75 * cellEdge) * topW;
                }

                // ---- Ghost data blocks ----
                if (_BlockIntensity > 0.001)
                {
                    float2 bp = sUV * float2(_BlockCols, _BlockRows);
                    float2 bc = floor(bp);
                    float2 bf = frac(bp);
                    float bEdge = smoothstep(0.18, 0.5, abs((bc.x + 0.5) / _BlockCols - 0.5));
                    float exists = step(1.0 - lerp(0.04, 0.22, bEdge), Hash(bc + 3.1));
                    float cyc = frac(time * 0.07 + Hash(bc + 9.7));
                    float slowVis = smoothstep(0.0, 0.1, cyc) * (1.0 - smoothstep(0.55, 0.65, cyc));
                    float burstVis = step(0.5, Hash(bc + floor(time * 24.0))) * glitching;
                    float vis = saturate(slowVis + burstVis * 0.6);
                    float2 inset = float2(0.12, 0.2) + 0.15 * float2(Hash(bc + 1.7), Hash(bc + 5.3));
                    float inside = step(inset.x, bf.x) * step(bf.x, 1.0 - inset.x)
                                 * step(inset.y, bf.y) * step(bf.y, 1.0 - inset.y);
                    float topEdge = step(1.0 - inset.y - 0.05, bf.y);
                    color += _BlockColor.rgb * _BlockIntensity * exists * vis * inside * (0.5 + 0.5 * topEdge);
                }

                // ---- Cyan "+" marks, upper right ----
                if (_CrossIntensity > 0.001)
                {
                    float region = smoothstep(0.58, 0.72, sUV.x) * smoothstep(0.55, 0.65, sUV.y)
                                 * (1.0 - smoothstep(0.80, 0.88, sUV.y));
                    if (region > 0.001)
                    {
                        float2 cp = float2(sUV.x * aspect, sUV.y) * _CrossDensity;
                        float2 cf = abs(frac(cp) - 0.5);
                        float arm = step(min(cf.x, cf.y), 0.03) * step(max(cf.x, cf.y), 0.16);
                        float2 cid = floor(cp);
                        float on = step(0.35, Hash(cid));
                        float flick = 0.6 + 0.4 * sin(time * 1.3 + Hash(cid + 4.2) * 6.2831);
                        color += _AccentColor.rgb * _CrossIntensity * region * arm * on * flick;
                    }
                }

                // ---- Slow scan bar (sweeps down, then idles) ----
                if (_ScanBarIntensity > 0.001)
                {
                    float p = frac(time / max(_ScanPeriod, 0.5)) / 0.35;
                    float sd = (screenUV.y - (1.0 - p)) / 0.03;
                    color += _AccentColor.rgb * _ScanBarIntensity * exp(-sd * sd) * step(p, 1.0);
                }

                // ---- Cyan sliver during glitch ticks ----
                float sliver = i.sliver.w * glitching
                    * step(i.sliver.x, screenUV.y) * step(screenUV.y, i.sliver.x + 0.0025)
                    * step(i.sliver.y, screenUV.x) * step(screenUV.x, i.sliver.y + i.sliver.z);
                color += _AccentColor.rgb * sliver * (0.25 + 0.5 * strength);

                // ---- UI pulse: brief dim so page changes read as a blink ----
                color *= 1.0 - saturate(_GlitchPulse) * _PulseDim;

                return float4(color, 1.0);
            }
            ENDCG
        }
    }
}