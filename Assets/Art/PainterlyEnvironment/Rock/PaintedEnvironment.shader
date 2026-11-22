Shader "RaggedBanner/Painted Environment"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _PaintMap("Paint Map", 2D) = "white" {}
        [Normal] [NoScaleOffset] _NormalMap("Rock Normal Map", 2D) = "bump" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.52, 0.62, 0.72, 1)
        _LightColor("Light Color", Color) = (1.05, 1, 0.88, 1)
        [Enum(Ground, 0, Rock, 1, Foliage, 2, FlatFoliage, 3)] _SurfaceKind("Surface Kind", Float) = 0
        _PaintScale("Paint Scale", Float) = 0.25
        _PaintStrength("Paint Strength", Range(0, 1)) = 1
        _NormalStrength("Rock Normal Strength", Range(0, 1)) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _LowColor("Foliage Low Color", Color) = (0.25, 0.38, 0.10, 1)
        _HighColor("Foliage High Color", Color) = (0.70, 0.80, 0.27, 1)
        _KeepColor("Keep Original Color", Range(0, 1)) = 0
        _FacetStrength("Rock Facet Strength", Range(0, 1)) = 0.3
        _ValueSteps("Value Steps", Range(2, 12)) = 5
        _AdditionalLightStepBlend("Additional Light Step Blend", Range(0, 1)) = 0.45
        _RockFacetJitter("Rock Facet Jitter", Range(0, 0.3)) = 0
        _RockBandMidThreshold("Rock Middle Band Threshold", Range(0.001, 0.98)) = 0.08
        _RockBandLitThreshold("Rock Bright Band Threshold", Range(0.01, 0.99)) = 0.45
        _RockBandSoftness("Rock Band Transition Width", Range(0, 0.3)) = 0.06
        _RockBandFormDetail("Rock Lit Band Form Detail", Range(0, 0.4)) = 0.1
        _RockBandMidColor("Rock Middle Band Tint", Color) = (0.66, 0.72, 0.80, 1)
        _RockBandShadeColor("Rock Shadow Band Tint", Color) = (0.36, 0.46, 0.58, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Lit"
        }

        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        half4 _PainterlyAmbientColor;

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_PaintMap);
        SAMPLER(sampler_PaintMap);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _PaintMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half4 _LightColor;
            half4 _LowColor;
            half4 _HighColor;
            float _SurfaceKind;
            float _PaintScale;
            half _PaintStrength;
            half _NormalStrength;
            half _Cutoff;
            half _Cull;
            half _KeepColor;
            half _FacetStrength;
            half _ValueSteps;
            half _RockFacetJitter;
            half _RockBandMidThreshold;
            half _RockBandLitThreshold;
            half _RockBandSoftness;
            half _RockBandFormDetail;
            half _AdditionalLightStepBlend;
            half4 _RockBandMidColor;
            half4 _RockBandShadeColor;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fogFactor : TEXCOORD3;
            float4 tangentWS : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings PaintedVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
            output.positionCS = positions.positionCS;
            output.positionWS = positions.positionWS;
            output.normalWS = normals.normalWS;
            output.tangentWS = float4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.fogFactor = ComputeFogFactor(positions.positionCS.z);
            return output;
        }

        void ClipFoliage(half alpha)
        {
            if (_SurfaceKind > 1.5)
            {
                clip(alpha * _BaseColor.a - _Cutoff);
            }
        }

        half PaintedLuminance(half3 color)
        {
            return dot(color, half3(0.2126, 0.7152, 0.0722));
        }

        float2 MirroredProjectedUV(float2 position, float scale, float4 textureST)
        {
            float2 uv = position * max(scale, 0.001) * textureST.xy + textureST.zw;
            return 1.0 - abs(frac(uv * 0.5) * 2.0 - 1.0);
        }

        float3 TriplanarWeights(float3 normalWS)
        {
            float3 weights = abs(normalWS);
            weights *= weights;
            weights *= weights;
            return weights / max(weights.x + weights.y + weights.z, 0.0001);
        }

        float3 RockFaceNormal(float3 positionWS, float3 referenceNormal)
        {
            float3 faceNormal = SafeNormalize(cross(ddy(positionWS), ddx(positionWS)));
            if (dot(faceNormal, referenceNormal) < 0)
            {
                faceNormal = -faceNormal;
            }
            return faceNormal;
        }

        half RockFacetJitter(float3 positionWS, float3 referenceNormal)
        {
            float3 faceNormal = RockFaceNormal(positionWS, referenceNormal);
            float planeOffset = dot(faceNormal, positionWS);
            float jitter = 0.5 * sin(dot(faceNormal, float3(0.267, 0.802, 0.535)) * 9.0 + planeOffset * 1.5)
                + 0.5 * sin(dot(faceNormal, float3(-0.707, 0.408, 0.577)) * 9.0 + planeOffset * 1.1);
            return (half)jitter;
        }

        float3 PaintedNormal(Varyings input, bool isFrontFace)
        {
            float3 normalWS = SafeNormalize(input.normalWS);
            if (_SurfaceKind > 0.5 && _SurfaceKind < 1.5)
            {
                if (_NormalStrength > 0)
                {
                    float3 tangentWS = SafeNormalize(input.tangentWS.xyz);
                    float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
                    half4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                    half3 normalTS = UnpackNormalScale(packedNormal, _NormalStrength);
                    normalWS = SafeNormalize(TransformTangentToWorld(normalTS, float3x3(tangentWS, bitangentWS, normalWS)));
                }
                float3 faceNormal = RockFaceNormal(input.positionWS, normalWS);
                normalWS = SafeNormalize(lerp(normalWS, faceNormal, _FacetStrength));
            }
            if (!isFrontFace)
            {
                normalWS = -normalWS;
            }
            return normalWS;
        }

        half3 SamplePaint(float3 positionWS, float3 normalWS)
        {
            half3 paint;
            if (_SurfaceKind < 0.5)
            {
                paint = SAMPLE_TEXTURE2D(_PaintMap, sampler_TrilinearClamp, MirroredProjectedUV(positionWS.xz, _PaintScale, _PaintMap_ST)).rgb;
            }
            else
            {
                float3 position = positionWS * max(_PaintScale, 0.001);
                float2 uv = position.xz * _PaintMap_ST.xy + _PaintMap_ST.zw;
                paint = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, uv).rgb;
                if (_SurfaceKind > 0.5 && _SurfaceKind < 1.5)
                {
                    float3 weights = TriplanarWeights(normalWS);
                    float2 uvX = position.zy * _PaintMap_ST.xy + _PaintMap_ST.zw;
                    float2 uvZ = position.xy * _PaintMap_ST.xy + _PaintMap_ST.zw;
                    half3 paintX = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, uvX).rgb;
                    half3 paintZ = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, uvZ).rgb;
                    paint = paintX * weights.x + paint * weights.y + paintZ * weights.z;
                }
            }
            return paint;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half3 FlatFoliageLight(Light light, half3 tint)
            {
                #if defined(_LIGHT_LAYERS)
                    if (!IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
                    {
                        return 0;
                    }
                #endif
                return tint * light.color * light.distanceAttenuation;
            }

            half3 PaintedAdditionalLighting(float3 normalWS, Light additionalLight)
            {
                if (_SurfaceKind > 2.5)
                {
                    return FlatFoliageLight(additionalLight, additionalLight.shadowAttenuation);
                }
                half diffuse = saturate(dot(normalWS, additionalLight.direction));
                half steps = max(_ValueSteps - 1.0h, 1.0h);
                half steppedDiffuse = floor(diffuse * steps + 0.5h) / steps;
                diffuse = lerp(diffuse, steppedDiffuse, _AdditionalLightStepBlend);
                float attenuation = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                return _LightColor.rgb * additionalLight.color * (diffuse * attenuation);
            }

            half4 PaintedFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 source;
                if (_SurfaceKind < 0.5)
                {
                    source = SAMPLE_TEXTURE2D(_BaseMap, sampler_TrilinearClamp, MirroredProjectedUV(input.positionWS.xz, _PaintScale, _PaintMap_ST));
                }
                else
                {
                    source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                }
                ClipFoliage(source.a);
                float3 normalWS = PaintedNormal(input, IS_FRONT_VFACE(face, true, false));
                half3 paint = SamplePaint(input.positionWS, SafeNormalize(input.normalWS));
                half3 albedo = lerp(source.rgb, paint, _PaintStrength);

                if (_SurfaceKind > 0.5 && _SurfaceKind < 1.5)
                {
                    half form = lerp(0.74h, 1.16h, saturate(PaintedLuminance(source.rgb) * 2.0h));
                    albedo = lerp(half3(1, 1, 1), paint, _PaintStrength) * form;
                    albedo *= 1.0h + _RockFacetJitter * RockFacetJitter(input.positionWS, SafeNormalize(input.normalWS));
                }
                else if (_SurfaceKind > 1.5)
                {
                    half leafValue = sqrt(saturate(PaintedLuminance(source.rgb)));
                    half paintValue = saturate((sqrt(saturate(PaintedLuminance(paint))) - 0.25h) * 2.0h);
                    leafValue = lerp(leafValue, paintValue, _PaintStrength);
                    half3 leafColor = lerp(_LowColor.rgb, _HighColor.rgb, leafValue);
                    albedo = lerp(leafColor, source.rgb, _KeepColor);
                }
                albedo *= _BaseColor.rgb;

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light light = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half3 lighting;
                if (_SurfaceKind > 2.5)
                {
                    half3 tint = lerp(_ShadowColor.rgb, _LightColor.rgb, saturate(light.shadowAttenuation));
                    lighting = FlatFoliageLight(light, tint);
                }
                else if (_SurfaceKind > 0.5 && _SurfaceKind < 1.5)
                {
                    float ndotl = dot(normalWS, light.direction);
                    float illumination = saturate(ndotl) * saturate(light.shadowAttenuation);
                    float litThreshold = clamp(_RockBandLitThreshold, 0.01, 0.99);
                    float midThreshold = clamp(_RockBandMidThreshold, 0.001, litThreshold - 0.001);
                    float halfWidth = min(max(_RockBandSoftness * 0.5, fwidth(illumination) * 0.5), (litThreshold - midThreshold) * 0.45);
                    float midLowerEdge = max(0.0001, midThreshold - halfWidth);
                    float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
                    float litLowerEdge = max(0.0001, litThreshold - halfWidth);
                    float litUpperEdge = max(litLowerEdge + 0.0001, min(1.0, litThreshold + halfWidth));
                    half midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
                    half litBand = smoothstep(litLowerEdge, litUpperEdge, illumination);
                    half litForm = saturate((ndotl - litThreshold) / max(1.0 - litThreshold, 0.001));
                    half3 litTint = lerp(1.0h - _RockBandFormDetail, 1.0h, litForm).xxx;
                    half3 bandTint = lerp(lerp(_RockBandShadeColor.rgb, _RockBandMidColor.rgb, midBand), litTint, litBand);
                    lighting = bandTint * light.color * light.distanceAttenuation;
                }
                else
                {
                    half diffuse = saturate(dot(normalWS, light.direction));
                    if (_SurfaceKind > 1.5)
                    {
                        diffuse = saturate((dot(normalWS, light.direction) + 0.35h) / 1.35h);
                        diffuse = lerp(diffuse, saturate(light.direction.y), _PaintStrength * 0.5h);
                    }
                    half steps = max(_ValueSteps - 1.0h, 1.0h);
                    half steppedDiffuse = floor(diffuse * steps + 0.5h) / steps;
                    diffuse = lerp(diffuse, steppedDiffuse, 0.45h);
                    half illumination = diffuse * light.shadowAttenuation * light.distanceAttenuation;
                    const half referenceLightLuminance = 1.25h;
                    half shadowBrightness = max(PaintedLuminance(light.color), 0.0h) / referenceLightLuminance * saturate(light.distanceAttenuation);
                    lighting = lerp(_ShadowColor.rgb * shadowBrightness, _LightColor.rgb * light.color, illumination);
                }
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        lighting += PaintedAdditionalLighting(normalWS, additionalLight);
                    LIGHT_LOOP_END
                #endif
                lighting += _PainterlyAmbientColor.rgb;
                half fog = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                half3 color = MixFog(albedo * lighting, fog);
                return half4(color, 1);
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
            #pragma vertex PaintedShadowVertex
            #pragma fragment PaintedShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings PaintedShadowVertex(Attributes input)
            {
                Varyings output = PaintedVertex(input);
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - output.positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float3 positionWS = ApplyShadowBias(output.positionWS, output.normalWS, lightDirectionWS);
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(positionWS));
                return output;
            }

            half4 PaintedShadowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                ClipFoliage(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedDepthFragment
            #pragma multi_compile_instancing

            half PaintedDepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipFoliage(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            half4 PaintedDepthNormalsFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipFoliage(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a);
                float3 normalWS = PaintedNormal(input, IS_FRONT_VFACE(face, true, false));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    return half4(PackFloat2To888(remappedNormalWS), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
