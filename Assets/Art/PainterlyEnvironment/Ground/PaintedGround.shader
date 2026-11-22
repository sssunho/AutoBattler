Shader "RaggedBanner/Painted Ground"
{
    Properties
    {
        [MainColor] _BaseColor("Surface Color", Color) = (0.68, 0.65, 0.59, 1)
        _BandShadeColor("Shade Tint", Color) = (0.51, 0.60, 0.72, 1)
        _BandMidColor("Middle Tint", Color) = (0.77, 0.80, 0.84, 1)
        _BandLitColor("Light Tint", Color) = (1, 1, 0.97, 1)
        _MidThreshold("Shade to Middle", Range(0.01, 0.97)) = 0.27
        _LitThreshold("Middle to Light", Range(0.03, 0.99)) = 0.63
        [ToggleUI] _FlatGroundLighting("Single-Tone Ground Lighting", Float) = 0
        [Enum(Rock,0,Ground,1,Clearing,2)] _GroundSurface("Surface Layout", Float) = 0
        _ClearingFootprint("Clearing Centre XZ and Half Extents (m)", Vector) = (0, 0, 7, 6)
        _ClearingGrassEdge("Rim Grass (XZ Normal, Offset, Feather in m)", Vector) = (0.4, 0.92, 1.25, 1.85)
        [NoScaleOffset] _GroundMask("Ground Path Mask (R)", 2D) = "black" {}
        _PathColor("Path Color", Color) = (0.71, 0.56, 0.41, 1)
        _MossColor("Rock Moss Color", Color) = (0.45, 0.50, 0.31, 1)
        [NoScaleOffset] _BrushMask("Brush Mask (R Warm, G Cool, B Moss)", 2D) = "black" {}
        [Toggle(_PROCEDURAL_BRUSH)] _ProceduralBrush("Procedural Brush", Float) = 0
        _StrokeSize("Stroke Size (Ground Metres / Rock Local Units)", Float) = 1
        _StrokeCoverage("Stroke Coverage", Range(0, 1)) = 0.50
        _BrushStrength("Brush Strength", Range(0, 1)) = 1
        _BrushValueContrast("Brush Value Contrast", Range(0, 0.18)) = 0.12
        _WarmTint("Warm Brush Tint", Color) = (1, 0.89, 0.74, 1)
        _CoolTint("Cool Brush Tint", Color) = (0.81, 0.86, 1, 1)
        _BrushBoundsMin("Rock Bounds Minimum", Vector) = (-0.5, -0.5, -0.5, 0)
        _BrushBoundsSize("Rock Bounds Size", Vector) = (1, 1, 1, 0)
        _ClearingEdgeSharpness("Clearing Edge Sharpness", Range(0, 1)) = 0
        _PathBandBlend("Path Band Tint Blend", Range(0, 1)) = 0
        _PathBandShadeColor("Path Shade Tint", Color) = (0.36, 0.46, 0.58, 1)
        _PathBandMidColor("Path Middle Tint", Color) = (0.66, 0.72, 0.80, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "DisableBatching" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _BandShadeColor;
            half4 _BandMidColor;
            half4 _BandLitColor;
            half4 _PathColor;
            half4 _MossColor;
            half4 _WarmTint;
            half4 _CoolTint;
            float4 _BrushBoundsMin;
            float4 _BrushBoundsSize;
            float4 _ClearingFootprint;
            float4 _ClearingGrassEdge;
            float4 _BrushMask_TexelSize;
            float _MidThreshold;
            float _LitThreshold;
            float _FlatGroundLighting;
            float _GroundSurface;
            float _BrushStrength;
            float _BrushValueContrast;
            float _ProceduralBrush;
            float _StrokeSize;
            float _StrokeCoverage;
            half4 _PathBandShadeColor;
            half4 _PathBandMidColor;
            float _ClearingEdgeSharpness;
            float _PathBandBlend;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex StudyVertex
            #pragma fragment StudyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _PROCEDURAL_BRUSH
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GroundMask);
            SAMPLER(sampler_GroundMask);
            TEXTURE2D(_BrushMask);
            SAMPLER(sampler_BrushMask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 brushPosition : TEXCOORD2;
                half3 normalOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings StudyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_PROCEDURAL_BRUSH)
                    output.brushPosition = input.positionOS.xyz;
                #else
                    output.brushPosition = (input.positionOS.xyz - _BrushBoundsMin.xyz) / max(_BrushBoundsSize.xyz, 0.0001);
                #endif
                output.normalOS = input.normalOS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half3 BrushProjectionWeights(half3 normalOS)
            {
                half3 weights = abs(normalOS);
                weights *= weights;
                weights *= weights;
                return weights / max(weights.x + weights.y + weights.z, 0.0001h);
            }

            #if defined(_PROCEDURAL_BRUSH)
            float BrushHash(float2 cell)
            {
                float3 p = frac(cell.xyx * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float BrushNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 weight = frac(position);
                weight = weight * weight * (3.0 - 2.0 * weight);
                float2 lower = float2(BrushHash(cell), BrushHash(cell + float2(1, 0)));
                float2 upper = float2(BrushHash(cell + float2(0, 1)), BrushHash(cell + float2(1, 1)));
                float2 values = lerp(lower, upper, weight.y);
                return lerp(values.x, values.y, weight.x);
            }

            half2 StrokeStamp(float2 position, float widthScale)
            {
                float2 cell = floor(position);
                float2 local = frac(position) - 0.5;
                float shape = BrushHash(cell + float2(19, 29));
                float tilt = BrushHash(cell + float2(43, 11));
                float pigment = BrushHash(cell + float2(7, 67));
                float halfLength = lerp(0.31, 0.46, shape);
                float halfWidth = min(0.34, lerp(0.095, 0.32, saturate(_StrokeCoverage)) * lerp(0.84, 1.05, tilt) * widthScale);
                float u = (local.x - (tilt - 0.5) * 0.055) / halfLength;
                float spine = (pigment - 0.5) * 0.14 + (tilt - 0.5) * 0.13 * u;
                spine += (shape - 0.5) * 0.11 * (u * u - 0.30);
                float v = (local.y - spine) / halfWidth;
                float tail = u * (shape < 0.5 ? -1.0 : 1.0);
                float taper = 1.0 - 0.42 * saturate(tail + 0.10);
                float bristle = abs(frac(v * 1.85 + tilt * 2.3) * 2.0 - 1.0);
                float bevel = abs(v - (shape - 0.5) * 0.55) * lerp(0.19, 0.55, saturate(tail));
                float end = abs(u) + bevel + bristle * lerp(0.06, 0.24, saturate(tail));
                float side = abs(v) / taper + 0.10 * abs(frac(u * 1.6 + shape) * 2.0 - 1.0);
                float distance = max(end, side) - 1.0;
                float2 pixelSpan = fwidth(position) / float2(halfLength, halfWidth);
                float edge = max(0.018, 0.55 * max(pixelSpan.x, pixelSpan.y));
                float ink = (1.0 - smoothstep(-edge, edge, distance)) * lerp(0.62, 0.99, shape);
                float warm = step(0.57, pigment);
                return ink * float2(warm, 1.0 - warm);
            }

            half2 ProceduralStroke(float2 position, bool ground)
            {
                float2 p = position / max(_StrokeSize, 0.01);
                float2 alongAcross = float2(dot(p, float2(0.86, 0.51)), dot(p, float2(-0.51, 0.86)));
                float turn = BrushNoise(p * 0.41 + float2(7.8, 19.3)) - 0.5;
                float shift = BrushNoise(p * 0.70 + float2(11.4, 52.6)) - 0.5;
                float2 footprint = float2(alongAcross.x * 0.71 + shift * 0.55, alongAcross.y * 1.64 + turn * 1.44);
                float widthScale = 1.0;
                float ink = 1.0;
                float glaze = 0.0;
                if (ground)
                {
                    float group = smoothstep(0.31, 0.67, 0.5 + 0.8 * turn + 0.30 * shift);
                    footprint += float2(turn * 0.42, group * 0.72 + shift * 0.81);
                    widthScale = lerp(0.62, 1.44, group);
                    ink = group * lerp(0.55, 1.0, group);
                    glaze = 0.16 * smoothstep(0.60, 0.99, group);
                }
                float2 second = float2(footprint.x * 1.41 + footprint.y * 0.11, footprint.y * 1.27 - footprint.x * 0.23);
                half2 pigments = max(StrokeStamp(footprint + float2(14.3, 23.7), widthScale), 0.76 * StrokeStamp(second + float2(41.6, 8.9), widthScale));
                float2 pixelSpan = fwidth(second);
                return max(pigments * ink, half2(0, glaze)) * (1.0 - smoothstep(0.24, 0.80, max(pixelSpan.x, pixelSpan.y)));
            }

            half2 ClearingStroke(float2 grid)
            {
                float2 cell = floor(grid);
                float angleSeed = BrushHash(cell + float2(31, 7));
                float widthSeed = BrushHash(cell + float2(9, 43));
                float lengthSeed = BrushHash(cell + float2(57, 19));
                float2 centre = 0.50 + 0.11 * (float2(widthSeed, lengthSeed) - 0.5);
                float2 offsetFromCentre = frac(grid) - centre;
                float sine, cosine;
                sincos(angleSeed * 6.2831853, sine, cosine);
                float halfLength = lerp(0.22, 0.39, lengthSeed);
                float halfWidth = lerp(0.046, 0.14, widthSeed) * lerp(0.72, 1.28, saturate(_StrokeCoverage));
                float u = dot(offsetFromCentre, float2(cosine, sine)) / halfLength;
                float v = dot(offsetFromCentre, float2(-sine, cosine)) / halfWidth;
                v -= (angleSeed - 0.5) * (u * u - 0.30) * 1.35;
                float tail = saturate(u * 0.65 + 0.45);
                float bristles = abs(frac(v * 2.25 + lengthSeed) * 2.0 - 1.0);
                float tip = abs(u) + 0.26 * abs(v) + 0.27 * tail * bristles;
                float width = 0.95 - 0.35 * tail - 0.17 * u * u + 0.10 * sin(4.0 * u + angleSeed * 5.0);
                float side = abs(v) / max(0.16, width);
                float edge = max(0.035, 0.65 * length(fwidth(grid)) / halfWidth);
                float ink = 1.0 - smoothstep(1.0 - edge, 1.0 + edge, max(tip, side));
                ink *= 1.0 - 0.55 * tail * smoothstep(0.52, 0.97, bristles);
                ink *= 1.0 - smoothstep(0.25, 0.75, edge);
                float warm = step(0.49, BrushHash(cell + float2(13, 53)));
                return ink * lerp(0.60, 1.0, angleSeed) * float2(warm, 1.0 - warm);
            }

            void SampleClearing(float2 position, half trail, out half earth, out half2 brush)
            {
                float2 local = position - _ClearingFootprint.xy;
                float2 extent = max(float2(0.1, 0.1), _ClearingFootprint.zw);
                float broad = BrushNoise(position * 0.31 + float2(15.6, 8.1)) - 0.5;
                float grain = BrushNoise(position * 1.12 + float2(23.8, 37.2)) - 0.5;
                float fleck = BrushNoise(position * 2.85 + float2(34.1, 13.2)) - 0.5;
                local.x += local.y * 0.25 + 0.55 * sin(local.y * 0.68);
                float rim = length(local / extent) + broad * 0.46 + grain * 0.16 + fleck * 0.06;
                float soil = 1.0 - smoothstep(0.78, 1.05, rim);
                float lip = dot(position, _ClearingGrassEdge.xy) - _ClearingGrassEdge.z + broad * 0.96 + grain * 0.46 + fleck * 0.22;
                soil *= 1.0 - smoothstep(-max(0.01, _ClearingGrassEdge.w), 0.0, lip);
                float margin = 4.0 * soil * (1.0 - soil);
                soil = smoothstep(0.10, 0.90, soil + margin * (grain * 0.50 + fleck * 0.34));
                earth = max(soil, trail * 0.94);

                float2 grid = position / max(_StrokeSize, 0.01) + float2(broad + grain * 0.38, grain + fleck * 0.27) * 0.28;
                float2 underlay = float2(dot(grid, float2(0.6, 0.8)), dot(grid, float2(-0.8, 0.6)));
                half2 broadMarks = ClearingStroke(underlay * 0.74 + float2(8.6, 29.1));
                half2 brokenMarks = ClearingStroke(grid * 1.31 + float2(51.3, 4.9));
                float loading = lerp(0.44, 0.95, smoothstep(-0.34, 0.34, broad));
                brush = max(broadMarks, brokenMarks * 0.70) * loading;
                float wash = broad * 0.82 + grain * 0.39 + fleck * 0.15;
                brush += float2(max(wash, 0.0), max(-wash, 0.0));
                brush = saturate(brush);
                earth = saturate(earth + (brush.r - brush.g) * earth * (1.0 - earth) * 1.25 * saturate(_BrushStrength));
                if (_ClearingEdgeSharpness > 0.0)
                {
                    float brushShift = (brush.r - brush.g) * 1.25 * saturate(_BrushStrength);
                    float shapedSoil = saturate(soil + brushShift * soil * (1.0 - soil));
                    float edgeWidth = max(fwidth(shapedSoil), 0.01);
                    float hardSoil = smoothstep(0.5 - edgeWidth, 0.5 + edgeWidth, shapedSoil);
                    float trailEarth = trail * 0.94;
                    trailEarth = saturate(trailEarth + brushShift * trailEarth * (1.0 - trailEarth));
                    earth = lerp(earth, max(hardSoil, trailEarth), _ClearingEdgeSharpness);
                }
            }

            half3 SampleRockBrush(float3 position, half3 normalOS, float3 normalWS)
            {
                half3 weights = BrushProjectionWeights(normalOS);
                half2 brush = ProceduralStroke(position.xz, false) * weights.y
                            + ProceduralStroke(position.xy + float2(4.2, 7.1), false) * weights.z
                            + ProceduralStroke(position.zy + float2(9.7, 2.3), false) * weights.x;
                float2 mossPosition = position.xz * 0.68 + position.y * float2(0.19, -0.24);
                float mossField = BrushNoise(mossPosition + float2(17.2, 43.6));
                float2 mossDetail = float2(mossPosition.x * 2.2 + mossPosition.y * 1.4, mossPosition.y * 4.6 - mossPosition.x * 2.1);
                mossField += (BrushNoise(mossDetail + float2(7.5, 11.3)) - 0.5) * 0.27;
                float moss = 0.59 * saturate(normalize(normalWS).y) + 0.8 * mossField - 0.955;
                float edge = max(0.009, 0.5 * fwidth(moss));
                return half3(brush, smoothstep(-edge, edge, moss));
            }
            #else
            half3 SampleBrushPanel(float2 uv, float panel)
            {
                float2 halfTexel = _BrushMask_TexelSize.xy * float2(1.5, 0.5);
                uv = clamp(uv, halfTexel, 1.0 - halfTexel);
                uv.x = (panel + uv.x) / 3.0;
                return SAMPLE_TEXTURE2D(_BrushMask, sampler_BrushMask, uv).rgb;
            }

            half3 SampleRockBrush(float3 position, half3 normalOS, float3 normalWS)
            {
                half3 weights = BrushProjectionWeights(normalOS);
                return SampleBrushPanel(position.xz, 0.0) * weights.y
                     + SampleBrushPanel(position.xy, 1.0) * weights.z
                     + SampleBrushPanel(position.zy, 2.0) * weights.x;
            }
            #endif

            half4 StudyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 surface = _BaseColor.rgb;
                half2 brush;
                half path = 0.0h;
                if (_GroundSurface > 0.5)
                {
                    path = SAMPLE_TEXTURE2D(_GroundMask, sampler_GroundMask, input.uv).r;
                    #if defined(_PROCEDURAL_BRUSH)
                        if (_GroundSurface > 1.5)
                        {
                            half earth;
                            SampleClearing(input.positionWS.xz, path, earth, brush);
                            path = earth;
                        }
                        else
                        {
                            brush = ProceduralStroke(input.positionWS.xz, true) * lerp(1.0h, 0.45h, path);
                        }
                    #else
                        brush = SAMPLE_TEXTURE2D(_BrushMask, sampler_BrushMask, input.uv).rg;
                    #endif
                    surface = lerp(surface, _PathColor.rgb, path);
                }
                else
                {
                    half3 rockMask = SampleRockBrush(input.brushPosition, input.normalOS, input.normalWS);
                    surface = lerp(surface, _MossColor.rgb, rockMask.b);
                    brush = rockMask.rg;
                }

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half3 band;
                half3 pathBand;
                if (_FlatGroundLighting > 0.5)
                {
                    band = lerp(_BandShadeColor.rgb, _BandLitColor.rgb, sun.shadowAttenuation);
                    pathBand = lerp(_PathBandShadeColor.rgb, _BandLitColor.rgb, sun.shadowAttenuation);
                }
                else
                {
                    float illumination = saturate(dot(normalize(input.normalWS), sun.direction)) * sun.shadowAttenuation;
                    float litThreshold = clamp(_LitThreshold, 0.03, 0.99);
                    float midThreshold = clamp(_MidThreshold, 0.01, litThreshold - 0.01);
                    float halfWidth = clamp(fwidth(illumination) * 0.5, 0.0001, min(0.035, (litThreshold - midThreshold) * 0.45));
                    half middle = smoothstep(midThreshold - halfWidth, midThreshold + halfWidth, illumination);
                    half lit = smoothstep(litThreshold - halfWidth, litThreshold + halfWidth, illumination);
                    band = lerp(lerp(_BandShadeColor.rgb, _BandMidColor.rgb, middle), _BandLitColor.rgb, lit);
                    pathBand = lerp(lerp(_PathBandShadeColor.rgb, _PathBandMidColor.rgb, middle), _BandLitColor.rgb, lit);
                }
                band = lerp(band, pathBand, path * _PathBandBlend);
                half3 color = surface * band * sun.color * sun.distanceAttenuation;

                if (_BrushStrength > 0.0)
                {
                    half3 tint = lerp(half3(1, 1, 1), _WarmTint.rgb, brush.r);
                    tint = lerp(tint, _CoolTint.rgb, brush.g);
                    half3 painted = color * tint;
                    half luminanceRatio = Luminance(color) / max(Luminance(painted), 0.0001h);
                    half valueFactor = 1.0h + clamp(_BrushValueContrast, 0.0h, 0.18h) * (brush.r - brush.g);
                    color = lerp(color, painted * luminanceRatio * valueFactor, saturate(_BrushStrength));
                }
                half fog = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                return half4(MixFog(color, fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
