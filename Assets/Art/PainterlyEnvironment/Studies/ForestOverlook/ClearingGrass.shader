Shader "RaggedBanner/Studies/Forest Overlook/Clearing Grass"
{
Properties
{
[ToggleUI]_Use_Vertex_Color_Wind("Use Vertex Color Wind", Float) = 1
[ToggleUI]_Use_Color_Noise("Use Color Noise", Float) = 1
_Color_Noise_Small_Freq("Color Noise Small Freq", Float) = 0.78
_Color_Noise_Large_Freq("Color Noise Large Freq", Float) = 0.68
_Alpha_Clip_Threshold("Alpha Clip Threshold", Range(0, 1)) = 0.5
[ToggleUI]_Enable_Back_Face_Lighting("Enable Back Face Lighting", Float) = 0
[NoScaleOffset]_Leaf_Texture("Leaf Texture", 2D) = "white" {}
_Leaf_Tiling("Leaf Tiling", Vector, 2) = (1, 1, 0, 0)
_Leaf_Offset("Leaf Offset", Vector, 2) = (0, 0, 0, 0)
[ToggleUI]_Enable_Leaf_Normal("Enable Leaf Normal", Float) = 0
[Normal][NoScaleOffset]_Leaf_Normal("Leaf Normal", 2D) = "bump" {}
_Leaf_Normal_Tiling("Leaf Normal Tiling", Vector, 2) = (1, 1, 0, 0)
_Leaf_Normal_Offset("Leaf Normal Offset", Vector, 2) = (0, 0, 0, 0)
_Leaf_Normal_Strength("Leaf Normal Strength", Range(0, 1)) = 0.5
_Leaf_Noise_Color("Leaf Noise Color", Color) = (1, 1, 1, 0)
_Leaf_Base_Color("Leaf Base Color", Color) = (1, 1, 1, 0)
_Leaf_Noise_Large_Color("Leaf Noise Large Color", Color) = (1, 1, 1, 0)
[ToggleUI]_Leaf_Flat_Color("Leaf Flat Color", Float) = 0
_Leaf_Metallic("Leaf Metallic", Range(0, 1)) = 0
_Leaf_Smoothness("Leaf Smoothness", Range(0, 1)) = 0.2
[NoScaleOffset]_Leaf_Ambient_Occlusion("Leaf Ambient Occlusion", 2D) = "white" {}
_Leaf_Ambient_Occlusion_Intensity("Leaf Ambient Occlusion Intensity", Range(0, 1)) = 0.5
[NoScaleOffset]_Trunk_Texture("Trunk Texture", 2D) = "white" {}
_Trunk_Tiling("Trunk Tiling", Vector, 2) = (1, 1, 0, 0)
_Trunk_Offset("Trunk Offset", Vector, 2) = (0, 0, 0, 0)
[ToggleUI]_Enable_Trunk_Normal("Enable Trunk Normal", Float) = 0
[Normal][NoScaleOffset]_Trunk_Normal("Trunk Normal", 2D) = "bump" {}
_Trunk_Normal_Tiling("Trunk Normal Tiling", Vector, 2) = (1, 1, 0, 0)
_Trunk_Normal_Offset("Trunk Normal Offset", Vector, 2) = (0, 0, 0, 0)
_Trunk_Normal_Strength("Trunk Normal Strength", Range(0, 1)) = 0.5
_Trunk_Base_Color("Trunk Base Color", Color) = (1, 1, 1, 0)
_Trunk_Noise_Color("Trunk Noise Color", Color) = (1, 1, 1, 0)
[ToggleUI]_Trunk_Flat_Color_Switch("Trunk Flat Color Switch", Float) = 0
_Trunk_Emissive_Color("Trunk Emissive Color", Color) = (0, 0, 0, 0)
[NoScaleOffset]_Trunk_Emissive_Mask("Trunk Emissive Mask", 2D) = "white" {}
_Trunk_Metallic("Trunk Metallic", Range(0, 1)) = 0
_Trunk_Smoothness("Trunk Smoothness", Range(0, 1)) = 0.2
[NoScaleOffset]_Trunk_Ambient_Occlusion("Trunk Ambient Occlusion", 2D) = "white" {}
_Trunk_Ambient_Occlusion_Intensity("Trunk Ambient Occlusion Intensity", Range(0, 1)) = 0.5
[ToggleUI]_Enable_Frosting("Enable Frosting", Float) = 0
_Frosting_Color("Frosting Color", Color) = (0.454902, 0.6039216, 0.07843138, 0)
_Frosting_Falloff("Frosting Falloff", Float) = 1.02
_Frosting_Height("Frosting Height", Float) = 0
[ToggleUI]_Frosting_Use_World_Normals("Frosting Use World Normals", Float) = 0
[ToggleUI]_Enable_Emission("Enable Emission", Float) = 0
_Emissive_Color("Emissive Color", Color) = (0, 0, 0, 0)
_Emissive_2_Color("Emissive 2 Color", Color) = (0, 0, 0, 0)
[NoScaleOffset]_Emissive_Mask("Emissive Mask", 2D) = "white" {}
[NoScaleOffset]_Emissive_2_Mask("Emissive 2 Mask", 2D) = "white" {}
_Emissive_Amount("Emissive Amount", Range(0, 2)) = 0
[ToggleUI]_Enable_Pulse("Enable Pulse", Float) = 0
[NoScaleOffset]_Emissive_Pulse_Map("Emissive Pulse Map", 2D) = "white" {}
_Pulse_Tiling("Pulse Tiling", Float) = 0.03
_Pulse_Speed("Pulse Speed", Float) = 0
_Emissive_Mask_Tiling("Emissive Mask Tiling", Vector, 2) = (1, 1, 0, 0)
_Emissive_Mask_Offset("Emissive Mask Offset", Vector, 2) = (0, 0, 0, 0)
_Emissive_Mask_2_Tiling("Emissive Mask 2 Tiling", Vector, 2) = (1, 1, 0, 0)
_Emissive_Mask_2_Offset("Emissive Mask 2 Offset", Vector, 2) = (0, 0, 0, 0)
[ToggleUI]_Use_Global_Weather_Controller("Use Global Weather Controller", Float) = 0
[ToggleUI]_Enable_Breeze("Enable Breeze", Float) = 1
_Breeze_Strength("Breeze Strength", Range(0, 1)) = 0.2
[ToggleUI]_Enable_Light_Wind("Enable Light Wind", Float) = 1
_Light_Wind_Strength("Light Wind Strength", Range(0, 1)) = 0.5
_Light_Wind_Y_Strength("Light Wind Y Strength", Range(0, 1)) = 1
_Light_Wind_Y_Offset("Light Wind Y Offset", Range(0, 1)) = 0
[ToggleUI]_Light_Wind_Use_Leaf_Fade("Light Wind Use Leaf Fade", Float) = 1
[ToggleUI]_Enable_Strong_Wind("Enable Strong Wind", Float) = 1
_Strong_Wind_Strength("Strong Wind Strength", Range(0, 1)) = 0.5
_Strong_Wind_Frequency("Strong Wind Frequency", Range(0, 1)) = 0.5
[ToggleUI]_Enable_Wind_Twist("Enable Wind Twist", Float) = 1
_Wind_Twist_Strength("Wind Twist Strength", Range(0, 1)) = 0.15
_Gale_Bend("Gale Bend", Range(0, 1)) = 1
[HideInInspector]_WorkflowMode("_WorkflowMode", Float) = 1
[HideInInspector]_CastShadows("_CastShadows", Float) = 1
[HideInInspector]_ReceiveShadows("_ReceiveShadows", Float) = 1
[HideInInspector]_Surface("_Surface", Float) = 0
[HideInInspector]_Blend("_Blend", Float) = 0
[HideInInspector]_AlphaClip("_AlphaClip", Float) = 1
[HideInInspector]_BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1
[HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
[HideInInspector]_DstBlend("_DstBlend", Float) = 0
[HideInInspector]_SrcBlendAlpha("_SrcBlendAlpha", Float) = 1
[HideInInspector]_DstBlendAlpha("_DstBlendAlpha", Float) = 0
[HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 1
[HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
[HideInInspector]_ZTest("_ZTest", Float) = 4
[HideInInspector]_Cull("_Cull", Float) = 0
[HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 1
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
[HideInInspector]_BUILTIN_Surface("Float", Float) = 0
[HideInInspector]_BUILTIN_Blend("Float", Float) = 0
[HideInInspector]_BUILTIN_AlphaClip("Float", Float) = 1
[HideInInspector]_BUILTIN_SrcBlend("Float", Float) = 1
[HideInInspector]_BUILTIN_DstBlend("Float", Float) = 0
[HideInInspector]_BUILTIN_ZWrite("Float", Float) = 1
[HideInInspector]_BUILTIN_ZWriteControl("Float", Float) = 0
[HideInInspector]_BUILTIN_ZTest("Float", Float) = 4
[HideInInspector]_BUILTIN_CullMode("Float", Float) = 0
[HideInInspector]_BUILTIN_QueueOffset("Float", Float) = 0
[HideInInspector]_BUILTIN_QueueControl("Float", Float) = -1
_PaintMap("Paint Map", 2D) = "white" {}
[MainColor]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
_LowColor("Lower Foliage Color", Color) = (0.22, 0.34, 0.16, 1)
_MidColor("Middle Foliage Color", Color) = (0.47, 0.60, 0.21, 1)
_HighColor("Upper Foliage Color", Color) = (0.76, 0.84, 0.34, 1)
_ShadowColor("Shadow Color", Color) = (0.52, 0.65, 0.78, 1)
_LightColor("Light Color", Color) = (1.03, 1.03, 1, 1)
_PaintScale("World Paint Scale", Float) = 0.23
_PaintStrength("World Paint Strength", Range(0, 1)) = 0.35
_LeafDetail("Original Leaf Detail", Range(0, 1)) = 0.18
_LightWrap("Light Wrap", Range(0, 1)) = 0.6
_NormalInfluence("Leaf Normal Influence", Range(0, 1)) = 0.3
}
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "UniversalMaterialType" = "Lit"
            "Queue" = "AlphaTest"
        }

        Cull [_Cull]
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #define SHADERPASS SHADERPASS_FORWARD
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #define SHADERPASS SHADERPASS_SHADOWCASTER
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #define SHADERPASS SHADERPASS_MOTION_VECTORS
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #define SHADERPASS SHADERPASS_DEPTHONLY
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #define SHADERPASS SHADERPASS_DEPTHNORMALS
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature _ EDITOR_VISUALIZATION
            #define SHADERPASS SHADERPASS_META
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #define SHADERPASS SHADERPASS_DEPTHONLY
            #define SCENESELECTIONPASS 1
            #define ALPHA_CLIP_THRESHOLD 1
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #define SHADERPASS SHADERPASS_DEPTHONLY
            #define SCENEPICKINGPASS 1
            #define ALPHA_CLIP_THRESHOLD 1
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Universal 2D"
            Tags { "LightMode" = "Universal2D" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZTest [_ZTest]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #define SHADERPASS SHADERPASS_2D
            #include_with_pragmas "ClearingGrassPass.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
