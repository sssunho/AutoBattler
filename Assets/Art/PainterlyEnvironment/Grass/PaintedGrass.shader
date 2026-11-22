Shader "RaggedBanner/Painted Foliage Base"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Toggle(_ALPHATEST_ON)] _GrassCard("Grass Card", Float) = 0
        _Cutoff("Card Alpha Cutoff", Range(0, 1)) = 0.25
        [Normal] [NoScaleOffset] _BumpMap("Card Normal Map", 2D) = "bump" {}
        _BumpScale("Card Normal Strength", Range(0, 1)) = 1
        _PaintMap("Paint Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _LowColor("Lower Foliage Color", Color) = (0.22, 0.34, 0.16, 1)
        _MidColor("Middle Foliage Color", Color) = (0.47, 0.60, 0.21, 1)
        _HighColor("Upper Foliage Color", Color) = (0.76, 0.84, 0.34, 1)
        _HeightRange("World Height Range Min Max", Vector) = (0, 1.1, 0, 0)
        _PaintScale("World Paint Scale", Float) = 0.23
        _PaintStrength("World Paint Strength", Range(0, 1)) = 0.35
        _LeafDetail("Original Leaf Detail", Range(0, 1)) = 0.18
        _LightWrap("Light Wrap", Range(0, 1)) = 0.6
        _NormalInfluence("Leaf Normal Influence", Range(0, 1)) = 0.3
        _ShadowColor("Shadow Color", Color) = (0.52, 0.65, 0.78, 1)
        _LightColor("Light Color", Color) = (1.03, 1.03, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        [ToggleUI] _GrassBandLighting("Leaf Band Lighting", Float) = 0
        _GrassBandMidThreshold("Middle Band Threshold", Range(0.001, 0.98)) = 0.08
        _GrassBandLitThreshold("Bright Band Threshold", Range(0.01, 0.99)) = 0.45
        _GrassBandSoftness("Band Transition Width", Range(0, 0.3)) = 0.06
        _GrassBandLitColor("Bright Band Tint", Color) = (1, 1, 1, 1)
        _GrassBandMidColor("Middle Band Tint", Color) = (0.61, 0.68, 0.57, 1)
        _GrassBandShadeColor("Shadow Band Tint", Color) = (0.34, 0.46, 0.44, 1)
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

        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        half4 _PainterlyAmbientColor;

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_PaintMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _PaintMap_ST;
            half4 _BaseColor;
            half4 _LowColor;
            half4 _MidColor;
            half4 _HighColor;
            float4 _HeightRange;
            half4 _ShadowColor;
            half4 _LightColor;
            float _PaintScale;
            half _PaintStrength;
            half _LeafDetail;
            half _LightWrap;
            half _NormalInfluence;
            half _Cull;
            half _GrassCard;
            half _Cutoff;
            half _BumpScale;
            half _GrassBandLighting;
            half _GrassBandMidThreshold;
            half _GrassBandLitThreshold;
            half _GrassBandSoftness;
            half4 _GrassBandLitColor;
            half4 _GrassBandMidColor;
            half4 _GrassBandShadeColor;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            #if defined(_ALPHATEST_ON)
                float4 tangentOS : TANGENT;
                half4 color : COLOR;
            #endif
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fogFactor : TEXCOORD3;
            #if defined(_ALPHATEST_ON)
                half height : TEXCOORD4;
                float4 tangentWS : TEXCOORD5;
            #endif
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
            output.positionCS = positions.positionCS;
            output.positionWS = positions.positionWS;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.fogFactor = ComputeFogFactor(positions.positionCS.z);
            #if defined(_ALPHATEST_ON)
                output.height = input.color.g;
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
            #endif
            return output;
        }

        float3 PaintedNormal(Varyings input, bool isFrontFace)
        {
            float3 normalWS = SafeNormalize(input.normalWS);
            if (!isFrontFace)
            {
                normalWS = -normalWS;
            }
            return normalWS;
        }

        void ClipGrassCard(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a - _Cutoff);
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }
            AlphaToMask [_GrassCard]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma shader_feature_local _ _ALPHATEST_ON
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define PAINTED_GRASS_BAND_LIGHTING
            #include "PaintedGrassLighting.hlsl"

            void PaintedFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC, out half4 outColor : SV_Target0
                #if defined(_WRITE_RENDERING_LAYERS) && defined(_ALPHATEST_ON)
                    , out uint outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                #if defined(_ALPHATEST_ON)
                    half height = input.height;
                    half alpha = OutputAlpha(AlphaDiscard(source.a, _Cutoff), false);
                    half fog = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                #else
                    half height = saturate((input.positionWS.y - _HeightRange.x) / max(_HeightRange.y - _HeightRange.x, 0.01));
                    half alpha = 1;
                    half fog = input.fogFactor;
                #endif
                float3 normalWS = PaintedNormal(input, IS_FRONT_VFACE(face, true, false));
                half3 color = EvaluatePaintedGrass(input.positionWS, normalWS, input.positionCS, height, source.rgb);
                outColor = half4(MixFog(color, fog), alpha);
                #if defined(_WRITE_RENDERING_LAYERS) && defined(_ALPHATEST_ON)
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
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
            #pragma shader_feature_local _ _ALPHATEST_ON
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
                ClipGrassCard(input.uv);
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
            #pragma shader_feature_local _ _ALPHATEST_ON

            half PaintedDepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipGrassCard(input.uv);
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
            #pragma instancing_options renderinglayer
            #pragma shader_feature_local _ _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            void PaintedDepthNormalsFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC, out half4 outNormalWS : SV_Target0
                #if defined(_WRITE_RENDERING_LAYERS) && defined(_ALPHATEST_ON)
                    , out uint outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipGrassCard(input.uv);
                #if defined(_ALPHATEST_ON) && !defined(_GBUFFER_NORMALS_OCT)
                    float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                    normalTS = SafeNormalize(float3(normalTS.xy * _BumpScale, lerp(1.0, normalTS.z, saturate(_BumpScale))));
                    float sign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
                    float3 bitangentWS = sign * cross(input.normalWS, input.tangentWS.xyz);
                    float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));
                #elif defined(_ALPHATEST_ON)
                    float3 normalWS = normalize(input.normalWS);
                #else
                    float3 normalWS = PaintedNormal(input, IS_FRONT_VFACE(face, true, false));
                #endif
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    outNormalWS = half4(PackFloat2To888(remappedNormalWS), 0);
                #else
                    outNormalWS = half4(normalWS, 0);
                #endif
                #if defined(_WRITE_RENDERING_LAYERS) && defined(_ALPHATEST_ON)
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedSelectionFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _ALPHATEST_ON

            int _ObjectId;
            int _PassValue;

            half4 PaintedSelectionFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipGrassCard(input.uv);
                return half4(_ObjectId, _PassValue, 1.0, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedPickingFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _ALPHATEST_ON

            float4 _SelectionID;

            half4 PaintedPickingFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipGrassCard(input.uv);
                return unity_SelectionID;
            }
            ENDHLSL
        }
    }
}
