Shader "RaggedBanner/Painted Character"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_NORMALMAP)] _EnableNormalMap("Enable Normal Map", Float) = 1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 1)) = 0.35
        _BandLitColor("Band Lit Color", Color) = (1.04, 1.02, 0.97, 1)
        _BandMidColor("Band Mid Color", Color) = (0.76, 0.82, 0.92, 1)
        _BandShadeColor("Band Shade Color", Color) = (0.48, 0.59, 0.77, 1)
        _BandThreshold("Band Lit Threshold", Range(0.01, 0.99)) = 0.48
        _BandMidThreshold("Band Mid Threshold", Range(0.001, 0.98)) = 0.14
        _BandSoftness("Band Softness", Range(0, 0.5)) = 0.05
        _BandFormDetail("Subtle Form Within Bands", Range(0, 0.4)) = 0.14
        _SpecularColor("Specular Color", Color) = (1, 0.94, 0.82, 1)
        _SpecularStrength("Specular Strength", Range(0, 0.3)) = 0.035
        _SpecularPower("Specular Power", Range(4, 64)) = 16
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        half4 _PainterlyAmbientColor;

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            half4 _BaseColor;
            half4 _BandLitColor;
            half4 _BandMidColor;
            half4 _BandShadeColor;
            half4 _SpecularColor;
            half _BumpScale;
            half _BandThreshold;
            half _BandMidThreshold;
            half _BandSoftness;
            half _BandFormDetail;
            half _SpecularStrength;
            half _SpecularPower;
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
            float4 uv : TEXCOORD2;
            half fogFactor : TEXCOORD3;
            float4 tangentWS : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct DepthVaryings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings CharacterVertex(Attributes input)
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
            output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
            output.uv.zw = TRANSFORM_TEX(input.uv, _BumpMap);
            output.fogFactor = ComputeFogFactor(positions.positionCS.z);
            return output;
        }

        DepthVaryings CharacterDepthVertex(Attributes input)
        {
            DepthVaryings output = (DepthVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }

        float3 CharacterNormal(Varyings input)
        {
            float3 normalWS = SafeNormalize(input.normalWS);
            #if defined(_NORMALMAP)
                float3 tangentWS = SafeNormalize(input.tangentWS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv.zw);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);
                normalWS = SafeNormalize(TransformTangentToWorld(normalTS, float3x3(tangentWS, bitangentWS, normalWS)));
            #endif
            return normalWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CharacterVertex
            #pragma fragment CharacterFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half3 CharacterAdditionalLighting(Light light, float3 normalWS)
            {
                float illumination = saturate(dot(normalWS, light.direction));
                float threshold = clamp(_BandThreshold, 0.01, 0.99);
                float midThreshold = clamp(_BandMidThreshold, 0.001, threshold - 0.001);
                float halfWidth = min(max(_BandSoftness * 0.5, 0.0001), (threshold - midThreshold) * 0.45);
                float midLowerEdge = max(0.0001, midThreshold - halfWidth);
                float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
                float lowerEdge = max(0.0001, threshold - halfWidth);
                float upperEdge = max(lowerEdge + 0.0001, min(1.0, threshold + halfWidth));
                half midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
                half litBand = smoothstep(lowerEdge, upperEdge, illumination);
                half3 bandTint = lerp(_BandMidColor.rgb * midBand, _BandLitColor.rgb, litBand);
                return bandTint * light.color * light.distanceAttenuation * light.shadowAttenuation;
            }

            half4 CharacterFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy).rgb * _BaseColor.rgb;
                float3 normalWS = CharacterNormal(input);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    inputData.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                Light light = GetMainLight(inputData, half4(1, 1, 1, 1), aoFactor);
                float ndotl = dot(normalWS, light.direction);
                float illumination = saturate(ndotl) * saturate(light.shadowAttenuation);
                float threshold = clamp(_BandThreshold, 0.01, 0.99);
                float midThreshold = clamp(_BandMidThreshold, 0.001, threshold - 0.001);
                float halfWidth = min(max(_BandSoftness * 0.5, fwidth(illumination) * 0.5), (threshold - midThreshold) * 0.45);
                float midLowerEdge = max(0.0001, midThreshold - halfWidth);
                float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
                float lowerEdge = max(0.0001, threshold - halfWidth);
                float upperEdge = max(lowerEdge + 0.0001, min(1.0, threshold + halfWidth));
                half midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
                half litBand = smoothstep(lowerEdge, upperEdge, illumination);
                half form = saturate(ndotl * 0.5h + 0.5h);
                half3 shadeTint = _BandShadeColor.rgb * lerp(1.0h - _BandFormDetail, 1.0h + _BandFormDetail, form);
                half3 midTint = _BandMidColor.rgb * lerp(1.0h - _BandFormDetail * 0.5h, 1.0h + _BandFormDetail * 0.5h, form);
                half3 litTint = _BandLitColor.rgb * lerp(1.0h - _BandFormDetail * 0.5h, 1.0h, form);
                half3 bandTint = lerp(lerp(shadeTint, midTint, midBand), litTint, litBand);
                half3 color = albedo * bandTint;
                if (_SpecularStrength > 0)
                {
                    float3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 halfDirectionWS = SafeNormalize(light.direction + viewDirectionWS);
                    half specular = pow(saturate(dot(normalWS, halfDirectionWS)), max(_SpecularPower, 1.0h));
                    color += _SpecularColor.rgb * (_SpecularStrength * specular * illumination);
                }
                color *= light.color * light.distanceAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light additionalLight = GetAdditionalLight(lightIndex, inputData, half4(1, 1, 1, 1), aoFactor);
                        color += albedo * CharacterAdditionalLighting(additionalLight, normalWS);
                    LIGHT_LOOP_END
                #endif
                color += albedo * _PainterlyAmbientColor.rgb;
                return half4(MixFog(color, input.fogFactor), 1);
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
            #pragma vertex CharacterShadowVertex
            #pragma fragment CharacterShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            DepthVaryings CharacterShadowVertex(Attributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(positionWS));
                return output;
            }

            half4 CharacterShadowFragment(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
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
            #pragma vertex CharacterDepthVertex
            #pragma fragment CharacterDepthFragment
            #pragma multi_compile_instancing

            half CharacterDepthFragment(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
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
            #pragma vertex CharacterVertex
            #pragma fragment CharacterDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            half4 CharacterDepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = CharacterNormal(input);
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
