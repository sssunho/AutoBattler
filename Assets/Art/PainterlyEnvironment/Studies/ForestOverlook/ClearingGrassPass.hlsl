#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

#define OVERLOOK_PAINTED_GRASS
#define _NORMALMAP 1
#define _NORMAL_DROPOFF_TS 1
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_COLOR
#define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#define FEATURES_GRAPH_VERTEX
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_NORMAL_WS
#define VARYINGS_NEED_TANGENT_WS
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_COLOR
#define VARYINGS_NEED_CULLFACE

#if SHADERPASS == SHADERPASS_FORWARD
    #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    #define VARYINGS_NEED_SHADOW_COORD
#elif SHADERPASS == SHADERPASS_META
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
    #if defined(EDITOR_VISUALIZATION)
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
    #endif
#endif

#include "OverlookFoliageGraph.hlsl"

#if SHADERPASS == SHADERPASS_FORWARD
    TEXTURE2D(_PaintMap);
    half4 _PainterlyAmbientColor;
    #include "Assets/Art/PainterlyEnvironment/Grass/PaintedGrassLighting.hlsl"

    half4 EvaluateClearingGrass(Varyings input, half alpha)
    {
        float3 normalWS = SafeNormalize(input.normalWS);
        #if defined(SHADER_STAGE_FRAGMENT)
            normalWS *= IS_FRONT_VFACE(input.cullFace, 1.0, -1.0);
        #endif
        float2 atlasUV = input.texCoord0.xy * _Leaf_Tiling + _Leaf_Offset;
        half3 source = SAMPLE_TEXTURE2D(_Leaf_Texture, sampler_Leaf_Texture, atlasUV).rgb;
        half3 color = EvaluatePaintedGrass(input.positionWS, normalWS, input.positionCS, input.color.g, source);
        return half4(color, alpha);
    }

    #define UniversalFragmentPBR(data, surface) EvaluateClearingGrass(unpacked, surface.alpha)
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
    #undef UniversalFragmentPBR
#elif defined(SCENESELECTIONPASS) || defined(SCENEPICKINGPASS)
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
#elif SHADERPASS == SHADERPASS_SHADOWCASTER
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
#elif SHADERPASS == SHADERPASS_DEPTHONLY
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
#elif SHADERPASS == SHADERPASS_DEPTHNORMALS
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
#elif SHADERPASS == SHADERPASS_MOTION_VECTORS
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
#elif SHADERPASS == SHADERPASS_META
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
#elif SHADERPASS == SHADERPASS_2D
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
#endif
