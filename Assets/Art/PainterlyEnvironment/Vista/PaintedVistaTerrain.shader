Shader "RaggedBanner/Painted Vista Terrain"
{
    Properties
    {
        [MainColor] _Color_Tint("Surface Tint", Color) = (1, 1, 1, 1)
        _FlatShadeTint("Shade Tint", Color) = (0.76, 0.82, 0.88, 1)
        [NoScaleOffset] _Triplanar_Texture_Top("Top", 2D) = "white" {}
        [NoScaleOffset] _Triplanar_Texture_Side("Side", 2D) = "white" {}
        [NoScaleOffset] _Triplanar_Texture_Bottom("Bottom", 2D) = "white" {}
        _Triplanar_Tiling_Top("Top Tiling", Vector) = (20, 20, 0, 0)
        _Triplanar_Tiling_Side("Side Tiling", Vector) = (20, 20, 0, 0)
        _Triplanar_Tiling_Bottom("Bottom Tiling", Vector) = (20, 20, 0, 0)
        _Triplanar_Offset_Top("Top Offset", Vector) = (0, 0, 0, 0)
        _Triplanar_Offset_Side("Side Offset", Vector) = (0, 0, 0, 0)
        _Triplanar_Offset_Bottom("Bottom Offset", Vector) = (0, 0, 0, 0)
        _Triplanar_Top_To_Side_Difference("Top to Side Difference", Range(0, 1)) = 0.2
        _Triplanar_Fade("Triplanar Fade", Range(0, 1)) = 1
        _Triplanar_Intensity("Triplanar Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color_Tint;
            half4 _FlatShadeTint;
            float4 _Triplanar_Tiling_Top;
            float4 _Triplanar_Tiling_Side;
            float4 _Triplanar_Tiling_Bottom;
            float4 _Triplanar_Offset_Top;
            float4 _Triplanar_Offset_Side;
            float4 _Triplanar_Offset_Bottom;
            float _Triplanar_Top_To_Side_Difference;
            float _Triplanar_Fade;
            float _Triplanar_Intensity;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VistaVertex
            #pragma fragment VistaFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Triplanar_Texture_Top);
            SAMPLER(sampler_Triplanar_Texture_Top);
            TEXTURE2D(_Triplanar_Texture_Side);
            SAMPLER(sampler_Triplanar_Texture_Side);
            TEXTURE2D(_Triplanar_Texture_Bottom);
            SAMPLER(sampler_Triplanar_Texture_Bottom);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VistaVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half3 SampleVistaTerrain(float3 position, float3 normal)
            {
                float3 weights = abs(normal);
                weights.y *= lerp(0.25, 4.0, saturate(_Triplanar_Top_To_Side_Difference));
                weights /= max(max(weights.x, weights.y), max(weights.z, 0.0001));
                weights = pow(weights, lerp(0.1, 50.0, saturate(_Triplanar_Fade)));
                weights /= max(weights.x + weights.y + weights.z, 0.0001);

                float3 signNormal = sign(normal);
                float2 xProjection = float2(-position.z * signNormal.x, position.y);
                float2 yProjection = float2(position.x * signNormal.y, -position.z);
                float2 zProjection = float2(position.x * signNormal.z, position.y);
                float2 uvX = xProjection * (0.01 * _Triplanar_Tiling_Side.xy) + _Triplanar_Offset_Side.xy;
                float2 uvZ = zProjection * (0.01 * _Triplanar_Tiling_Side.xy) + _Triplanar_Offset_Side.xy;
                float2 uvTop = yProjection * (0.01 * _Triplanar_Tiling_Top.xy) + _Triplanar_Offset_Top.xy;
                float2 uvBottom = yProjection * (0.01 * _Triplanar_Tiling_Bottom.xy) + _Triplanar_Offset_Bottom.xy;

                half3 sideX = SAMPLE_TEXTURE2D(_Triplanar_Texture_Side, sampler_Triplanar_Texture_Side, uvX).rgb;
                half3 sideZ = SAMPLE_TEXTURE2D(_Triplanar_Texture_Side, sampler_Triplanar_Texture_Side, uvZ).rgb;
                half3 top = SAMPLE_TEXTURE2D(_Triplanar_Texture_Top, sampler_Triplanar_Texture_Top, uvTop).rgb;
                half3 bottom = SAMPLE_TEXTURE2D(_Triplanar_Texture_Bottom, sampler_Triplanar_Texture_Bottom, uvBottom).rgb;
                half3 surface = sideX * weights.x + (normal.y > 0 ? top : bottom) * weights.y + sideZ * weights.z;
                return saturate(surface + (1.0h - _Triplanar_Intensity)) * _Color_Tint.rgb;
            }

            half4 VistaFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 surface = SampleVistaTerrain(GetAbsolutePositionWS(input.positionWS), input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half3 lightTint = lerp(_FlatShadeTint.rgb, 1.0h, sun.shadowAttenuation);
                half3 color = surface * lightTint * sun.color * sun.distanceAttenuation;
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
