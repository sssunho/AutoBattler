// Made with Amplify Shader Editor v1.9.9.7
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "RaggedBanner/Painted Tree Leaves Three Tone"
{
	Properties
	{
		[Header(Three Tone Lighting)] _BandThreshold("Bright Band Threshold", Range(0.01, 0.99)) = 0.45
		_BandMidThreshold("Middle Band Threshold", Range(0.001, 0.98)) = 0.08
		_BandSoftness("Light Band Transition Width", Range(0, 0.3)) = 0.06
		_BandFormDetail("Subtle Form Within Bands", Range(0, 0.4)) = 0
		[HDR] _BandLitColor("Lit Face Tint", Color) = (1, 1, 1, 1)
		[HDR] _BandMidColor("Middle Face Tint", Color) = (0.7, 0.76, 0.81, 1)
		[HDR] _BandShadeColor("Shadow Face Tint", Color) = (0.4, 0.52, 0.62, 1)
		[Header(Base)][Toggle( _ENABLEFLIPNORMALS_ON )] _EnableFlipNormals( "Enable Flip Normals", Float ) = 0
		[Toggle( _ENABLEGLANCINGANGLECUT_ON )] _EnableGlancingAngleCut( "Enable Glancing Angle Cut", Float ) = 0
		_BaseGlancingAngleCut( "Base Glancing Angle Cut", Range( 0, 1 ) ) = 0.8
		_CutOff( "Base Opacity Cutoff", Range( 0, 1 ) ) = 0.3
		[HDR] _BaseAlbedoColor( "Base Albedo Color", Color ) = ( 0.5019608, 0.5019608, 0.5019608 )
		_BaseAlbedoBrightness( "Base Albedo Brightness", Range( 0, 5 ) ) = 1
		_BaseAlbedoDesaturation( "Base Albedo Desaturation", Range( 0, 1 ) ) = 0
		[Toggle(_ENABLENORMALMAP_ON)] _EnableNormalMap("Enable Normal Map", Float) = 0
		_BaseNormalIntensity( "Base Normal Intensity", Range( 0, 5 ) ) = 1
		_BaseTreeAOIntensity( "Base Tree AO Intensity", Range( 0, 1 ) ) = 0.5
		_BaseLerpBetweenColorandTexture( "Base Lerp Between Color and Texture", Range( 0, 1 ) ) = 1
		[NoScaleOffset] _BaseAlbedo( "Base Albedo", 2D ) = "gray" {}
		[NoScaleOffset] _BaseNormal( "Base Normal", 2D ) = "bump" {}
		[Header(Base Gradient Color)][Toggle( _ENABLEGRADIENTCOLOR_ON )] _EnableGradientColor( "Enable Gradient Color", Float ) = 0
		[HDR] _GradientColor( "Gradient Color", Color ) = ( 0.5019608, 0.5019608, 0.5019608 )
		_GradientColorIntensity( "Gradient Color Intensity", Range( 0, 1 ) ) = 1
		_GradientColorOffset( "Gradient Color Offset", Range( 0, 5 ) ) = 1
		_GradientColorContrast( "Gradient Color Contrast", Range( 0, 30 ) ) = 1
		[IntRange] _GradientColorInvertMask( "Gradient Color Invert Mask", Range( 0, 1 ) ) = 0
		[Header(Base Second Color)][Toggle( _ENABLESECONDCOLOR_ON )] _EnableSecondColor( "Enable Second Color", Float ) = 0
		[HDR] _SecondColor( "Second Color", Color ) = ( 0.5019608, 0.5019608, 0.5019608 )
		_SecondColorIntensity( "Second Color Intensity", Range( 0, 1 ) ) = 1
		_SecondColorOffset( "Second Color Offset", Range( 0, 3 ) ) = 1
		_SecondColorContrast( "Second Color Contrast", Range( 0, 30 ) ) = 1
		[IntRange] _SecondColorInvertMask( "Second Color Invert Mask", Range( 0, 1 ) ) = 0
		[Header(Base Tint Color)][Toggle( _ENABLETINTCOLOR_ON )] _EnableTintColor( "Enable Tint Color", Float ) = 0
		[HDR] _TintColor1( "Tint Color 1", Color ) = ( 0.5019608, 0.5019608, 0.5019608, 0 )
		[HDR] _TintColor2( "Tint Color 2", Color ) = ( 0.5019608, 0.5019608, 0.5019608, 0 )
		_TintNoiseIntensity( "Tint Noise Intensity", Range( 0, 1 ) ) = 1
		_TintNoiseOffset( "Tint Noise Offset", Range( 0, 10 ) ) = 1
		_TintNoiseContrast( "Tint Noise Contrast", Range( 0, 10 ) ) = 1
		[Header(Top Layer)][Toggle( _ENABLETOPLAYERBLEND_ON )] _EnableTopLayerBlend( "Enable Top Layer Blend", Float ) = 0
		[HDR] _TopLayerColor( "Top Layer Color", Color ) = ( 0.7764706, 0.8392157, 0.9490196 )
		_TopLayerIntensity( "Top Layer Intensity", Range( 0, 1 ) ) = 1
		_TopLayerOffset( "Top Layer Offset", Range( 0, 1 ) ) = 0.5
		_TopLayerContrast( "Top Layer Contrast", Range( 0, 30 ) ) = 10
		_TopLayerAOMask( "Top Layer AO Mask", Range( 0, 10 ) ) = 1
		[Toggle( _ENABLEWORLDPROJECTION_ON )] _EnableWorldProjection( "Enable World Projection", Float ) = 1
		[Toggle( _ENABLEBACKFACEPROJECTION_ON )] _EnableBackfaceProjection( "Enable Backface Projection", Float ) = 1
		[Header(Wind)][Toggle( _ENABLEWIND_ON )] _EnableWind( "Enable Wind", Float ) = 1
		_WindLeafAmplitude( "Wind Leaf Amplitude", Range( 0, 2 ) ) = 1
		_WindLeafSpeed( "Wind Leaf Speed", Range( 0, 2 ) ) = 1
		_WindLeafScale( "Wind Leaf Scale", Range( 0, 2 ) ) = 1
		_WindLeafTurbulence( "Wind Leaf Turbulence", Range( 0, 2 ) ) = 1
		_WindLeafOffset( "Wind Leaf Offset", Range( 0, 2 ) ) = 1
		[Header(Wind (Use the same values for each tree material))] _WindTreeFlexibility( "Wind Tree Flexibility", Range( 0, 2 ) ) = 1
		_WindTreeBaseRigidity( "Wind Tree Base Rigidity", Range( 0, 5 ) ) = 2.5
		[Toggle( _ENABLESTATICMESHSUPPORT_ON )] _EnableStaticMeshSupport( "Enable Static Mesh Support", Float ) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}


		//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		//_TransStrength( "Strength", Range( 0, 50 ) ) = 1
		//_TransNormal( "Normal Distortion", Range( 0, 1 ) ) = 0.5
		//_TransScattering( "Scattering", Range( 1, 50 ) ) = 2
		//_TransDirect( "Direct", Range( 0, 1 ) ) = 0.9
		//_TransAmbient( "Ambient", Range( 0, 1 ) ) = 0.1
		//_TransShadow( "Shadow", Range( 0, 1 ) ) = 0.5

		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		//_InstancedTerrainNormals("Instanced Terrain Normals", Float) = 1.0

		//[HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1

		[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1

		[HideInInspector] _AlphaClip("__clip", Float) = 0.0
	}

	SubShader
	{
		LOD 0

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }

		Cull Off
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#if ( SHADER_TARGET > 35 ) && defined( SHADER_API_GLES3 )
			#error For WebGL2/GLES3, please set your shader target to 3.5 via SubShader options. URP shaders in ASE use target 4.5 by default.
		#endif

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		half4 _PainterlyAmbientColor;

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ENABLENORMALMAP_ON


			#define SHADERPASS SHADERPASS_FORWARD

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined( UNITY_INSTANCING_ENABLED ) && defined( ASE_INSTANCED_TERRAIN ) && ( defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL) || defined(_INSTANCEDTERRAINNORMALS_PIXEL) )
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_TANGENT
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_BITANGENT
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLETOPLAYERBLEND_ON
			#pragma shader_feature_local _ENABLETINTCOLOR_ON
			#pragma shader_feature_local _ENABLESECONDCOLOR_ON
			#pragma shader_feature_local _ENABLEGRADIENTCOLOR_ON
			#pragma shader_feature_local _ENABLEBACKFACEPROJECTION_ON
			#pragma shader_feature_local _ENABLEWORLDPROJECTION_ON
			#pragma shader_feature_local _ENABLEFLIPNORMALS_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					float4 texcoord1 : TEXCOORD1;
				#endif
				#if defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					float4 texcoord2 : TEXCOORD2;
				#endif
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
				float4 lightmapUVOrVertexSH : TEXCOORD3;
				#if defined(ASE_FOG)
					half4 fogFactorAndVertexLight : TEXCOORD4;
				#endif
				#if defined(USE_APV_PROBE_OCCLUSION)
					float4 probeOcclusion : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			half3 EvaluatePaintedTreeAdditionalLight(Light light, half3 normalWS, uint meshRenderingLayers)
			{
				#ifdef _LIGHT_LAYERS
					if (!IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
					{
						return 0;
					}
				#endif

				float illumination = saturate(dot(normalWS, light.direction));
				float threshold = clamp(_BandThreshold, 0.01, 0.99);
				float midThreshold = clamp(_BandMidThreshold, 0.001, threshold - 0.001);
				float halfWidth = min(max(_BandSoftness * 0.5, 0.0), (threshold - midThreshold) * 0.45);
				float midLowerEdge = max(0.0001, midThreshold - halfWidth);
				float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
				float lowerEdge = max(0.0001, threshold - halfWidth);
				float upperEdge = max(lowerEdge + 0.0001, min(1.0, threshold + halfWidth));
				float midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
				float litBand = smoothstep(lowerEdge, upperEdge, illumination);
				half3 bandTint = lerp(_BandMidColor.rgb * midBand, _BandLitColor.rgb, litBand);
				return bandTint * light.color * (light.distanceAttenuation * saturate(light.shadowAttenuation));
			}

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;
			half ASP_GlobalTreeAO;
			half ASP_GlobalTintNoiseUVScale;
			half ASP_GlobalTintNoiseContrast;
			half ASP_GlobalTintNoiseIntensity;
			sampler2D _BaseNormal;
			half ASPT_TopLayerOffset;
			half ASPT_TopLayerContrast;
			half ASPT_TopLayerIntensity;
			half ASPT_TopLayerHeightStart;
			float ASPT_TopLayerHeightFade;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord7.xy = input.texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord7.zw = input.texcoord2.xy;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif
				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
				OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.lightmapUVOrVertexSH.xyz, output.probeOcclusion);

				#if defined(ASE_FOG)
					output.fogFactorAndVertexLight = 0;
					#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
						output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
					#endif
				#endif

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					output.tangentWS.zw = input.texcoord.xy;
					output.tangentWS.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					float4 texcoord1 : TEXCOORD1;
				#endif
				#if defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					float4 texcoord2 : TEXCOORD2;
				#endif
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					output.texcoord1 = input.texcoord1;
				#endif
				#if defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					output.texcoord2 = input.texcoord2;
				#endif
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				#endif
				#if defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				#endif
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out uint outRenderingLayers : SV_Target1
						#endif
						, uint ase_vface : SV_IsFrontFace ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWS );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float2 sampleCoords = (input.tangentWS.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					NormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					TangentWS = -cross(GetObjectToWorldMatrix()._13_23_33, NormalWS);
					BitangentWS = cross(NormalWS, -TangentWS);
				#endif

				float2 uv_BaseAlbedo1753 = input.ase_texcoord7.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float3 desaturateInitialColor1712 = tex2DNode1753.rgb;
				float desaturateDot1712 = dot( desaturateInitialColor1712, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar1712 = lerp( desaturateInitialColor1712, desaturateDot1712.xxx, _BaseAlbedoDesaturation );
				float3 blendOpSrc3324 = ( desaturateVar1712 * _BaseAlbedoBrightness );
				float3 blendOpDest3324 = _BaseAlbedoColor;
				float3 lerpResult1716 = lerp( _BaseAlbedoColor , ( saturate( (( blendOpDest3324 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest3324 ) * ( 1.0 - blendOpSrc3324 ) ) : ( 2.0 * blendOpDest3324 * blendOpSrc3324 ) ) )) , _BaseLerpBetweenColorandTexture);
				float lerpResult1727 = lerp( 1.0 , input.ase_color.a , saturate( ( _BaseTreeAOIntensity * ASP_GlobalTreeAO ) ));
				float Base_AO1735 = lerpResult1727;
				float3 Base_Albedo1723 = ( lerpResult1716 * Base_AO1735 );
				float3 blendOpSrc1836 = Base_Albedo1723;
				float3 blendOpDest1836 = _GradientColor;
				float3 temp_output_1836_0 = ( saturate( (( blendOpDest1836 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1836 ) * ( 1.0 - blendOpSrc1836 ) ) : ( 2.0 * blendOpDest1836 * blendOpSrc1836 ) ) ));
				float2 texCoord1840 = input.ase_texcoord7.zw * float2( 1,1 ) + float2( 0,0 );
				float saferPower1879 = abs( ( ( 1.0 - texCoord1840.y ) * _GradientColorOffset ) );
				float temp_output_1848_0 = saturate( pow( saferPower1879 , _GradientColorContrast ) );
				float lerpResult1877 = lerp( temp_output_1848_0 , ( 1.0 - temp_output_1848_0 ) , _GradientColorInvertMask);
				float Gradient_Color_Mask1854 = ( lerpResult1877 * _GradientColorIntensity );
				float3 lerpResult1838 = lerp( Base_Albedo1723 , temp_output_1836_0 , Gradient_Color_Mask1854);
				float3 Gradient_Color_Albedo1856 = lerpResult1838;
				#ifdef _ENABLEGRADIENTCOLOR_ON
				float3 staticSwitch1867 = Gradient_Color_Albedo1856;
				#else
				float3 staticSwitch1867 = Base_Albedo1723;
				#endif
				float3 Step_2_Albedo1914 = staticSwitch1867;
				float3 blendOpSrc1897 = Base_Albedo1723;
				float3 blendOpDest1897 = _SecondColor;
				float3 temp_output_1897_0 = ( saturate( (( blendOpDest1897 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1897 ) * ( 1.0 - blendOpSrc1897 ) ) : ( 2.0 * blendOpDest1897 * blendOpSrc1897 ) ) ));
				float saferPower1886 = abs( ( ( 1.0 - input.ase_color.a ) * _SecondColorOffset ) );
				float temp_output_1889_0 = saturate( pow( saferPower1886 , ( _SecondColorContrast + 2.0 ) ) );
				float lerpResult1890 = lerp( temp_output_1889_0 , ( 1.0 - temp_output_1889_0 ) , _SecondColorInvertMask);
				float Second_Color_Mask1895 = ( lerpResult1890 * _SecondColorIntensity );
				float3 lerpResult1899 = lerp( Step_2_Albedo1914 , temp_output_1897_0 , Second_Color_Mask1895);
				float3 Second_Color_Albedo1902 = lerpResult1899;
				#ifdef _ENABLESECONDCOLOR_ON
				float3 staticSwitch1921 = Second_Color_Albedo1902;
				#else
				float3 staticSwitch1921 = Step_2_Albedo1914;
				#endif
				float3 Step_3_Albedo1954 = staticSwitch1921;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Tint_Color_Mask1949 = saturate( round( ( frac( ( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) * ( _TintNoiseOffset * ASP_GlobalTintNoiseUVScale ) ) ) * ( _TintNoiseContrast * ASP_GlobalTintNoiseContrast ) ) ) );
				float3 lerpResult1951 = lerp( _TintColor1.rgb , _TintColor2.rgb , Tint_Color_Mask1949);
				float3 blendOpSrc1952 = Step_3_Albedo1954;
				float3 blendOpDest1952 = lerpResult1951;
				float3 temp_output_1952_0 = ( saturate( (( blendOpDest1952 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1952 ) * ( 1.0 - blendOpSrc1952 ) ) : ( 2.0 * blendOpDest1952 * blendOpSrc1952 ) ) ));
				float temp_output_1961_0 = ( _TintNoiseIntensity * ASP_GlobalTintNoiseIntensity );
				float3 lerpResult1956 = lerp( Step_3_Albedo1954 , temp_output_1952_0 , temp_output_1961_0);
				float3 Tint_Color_Albedo1963 = lerpResult1956;
				#ifdef _ENABLETINTCOLOR_ON
				float3 staticSwitch2080 = Tint_Color_Albedo1963;
				#else
				float3 staticSwitch2080 = Step_3_Albedo1954;
				#endif
				float3 Step_4_Albedo2083 = staticSwitch2080;
				float3 Top_Layer_Color1995 = _TopLayerColor;
				#if defined(_ENABLENORMALMAP_ON)
				float2 uv_BaseNormal1741 = input.ase_texcoord7.xy;
				float3 unpack1741 = UnpackNormalScale( tex2D( _BaseNormal, uv_BaseNormal1741 ), _BaseNormalIntensity );
				unpack1741.z = lerp( 1, unpack1741.z, saturate(_BaseNormalIntensity) );
				#else
				float3 unpack1741 = float3(0, 0, 1);
				#endif
				float3 tex2DNode1741 = unpack1741;
				float3 break105_g45 = tex2DNode1741;
				float switchResult107_g45 = (((ase_vface>0)?(break105_g45.z):(-break105_g45.z)));
				float3 appendResult108_g45 = (float3(break105_g45.x , break105_g45.y , switchResult107_g45));
				#ifdef _ENABLEFLIPNORMALS_ON
				float3 staticSwitch1744 = appendResult108_g45;
				#else
				float3 staticSwitch1744 = tex2DNode1741;
				#endif
				float3 Base_Normal1747 = staticSwitch1744;
				float3 desaturateInitialColor2013 = Base_Normal1747;
				float desaturateDot2013 = dot( desaturateInitialColor2013, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar2013 = lerp( desaturateInitialColor2013, desaturateDot2013.xxx, 1.0 );
				float3 tanToWorld0 = float3( TangentWS.x, BitangentWS.x, NormalWS.x );
				float3 tanToWorld1 = float3( TangentWS.y, BitangentWS.y, NormalWS.y );
				float3 tanToWorld2 = float3( TangentWS.z, BitangentWS.z, NormalWS.z );
				float3 tanNormal2012 = Base_Normal1747;
				float3 worldNormal2012 = float3( dot( tanToWorld0, tanNormal2012 ), dot( tanToWorld1, tanNormal2012 ), dot( tanToWorld2, tanNormal2012 ) );
				float3 temp_cast_0 = (worldNormal2012.y).xxx;
				#ifdef _ENABLEWORLDPROJECTION_ON
				float3 staticSwitch2017 = temp_cast_0;
				#else
				float3 staticSwitch2017 = desaturateVar2013;
				#endif
				float3 saferPower17_g32 = abs( abs( ( saturate( staticSwitch2017 ) + ( _TopLayerOffset * ASPT_TopLayerOffset ) ) ) );
				float temp_output_2005_0 = ( _TopLayerContrast * ASPT_TopLayerContrast );
				float3 temp_cast_1 = (temp_output_2005_0).xxx;
				float saferPower2024 = abs( ( 1.0 - input.ase_color.a ) );
				float Top_Layer_Contrast2025 = temp_output_2005_0;
				float3 lerpResult2029 = lerp( ( ( saturate( pow( saferPower17_g32 , temp_cast_1 ) ) * ( _TopLayerIntensity * ASPT_TopLayerIntensity ) ) * saturate(  (0.0 + ( PositionWS.y - ASPT_TopLayerHeightStart ) * ( 1.0 - 0.0 ) / ( ( ASPT_TopLayerHeightStart + ASPT_TopLayerHeightFade ) - ASPT_TopLayerHeightStart ) ) ) ) , float3( 0,0,0 ) , saturate( ( pow( saferPower2024 , Top_Layer_Contrast2025 ) * _TopLayerAOMask ) ));
				float3 Top_Layer_Mask2031 = saturate( lerpResult2029 );
				float3 lerpResult2034 = lerp( Step_4_Albedo2083 , Top_Layer_Color1995 , Top_Layer_Mask2031);
				float3 lerpResult2037 = lerp( Step_4_Albedo2083 , lerpResult2034 , saturate( ( ase_vface > 0 ? +1 : -1 ) ));
				#ifdef _ENABLEBACKFACEPROJECTION_ON
				float3 staticSwitch2041 = lerpResult2034;
				#else
				float3 staticSwitch2041 = lerpResult2037;
				#endif
				float3 Top_Layer_Albedo2042 = staticSwitch2041;
				#ifdef _ENABLETOPLAYERBLEND_ON
				float3 staticSwitch2043 = Top_Layer_Albedo2042;
				#else
				float3 staticSwitch2043 = Step_4_Albedo2083;
				#endif
				float3 Output_Albedo1873 = staticSwitch2043;
				
				
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				
				

				float3 BaseColor = Output_Albedo1873;
				float3 Normal = Base_Normal1747;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = 0;
				float Occlusion = Base_AO1735;
				float3 Emission = 0;
				float Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _CutOff;
					float AlphaClipThresholdShadow = 0.5;
				#endif
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _CLEARCOAT
					float CoatMask = 0;
					float CoatSmoothness = 0;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.viewDirectionWS = ViewDirWS;
				inputData.shadowCoord = ShadowCoord;

				#ifdef _NORMALMAP
						#if _NORMAL_DROPOFF_TS
							inputData.normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							inputData.normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							inputData.normalWS = Normal;
						#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = NormalWS;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				#endif

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = input.lightmapUVOrVertexSH.xyz;
				#endif

				#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
					inputData.bakedGI = SAMPLE_GI( SH, GetAbsolutePositionWS(inputData.positionWS),
						inputData.normalWS,
						inputData.viewDirectionWS,
						input.positionCS.xy,
						input.probeOcclusion,
						inputData.shadowMask );
				#else
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#endif


				#if defined(DEBUG_DISPLAY)
					#if defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVOrVertexSH.xy;
					#else
						inputData.vertexSH = SH;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				SurfaceData surfaceData;
				surfaceData.albedo              = BaseColor;
				surfaceData.metallic            = saturate(Metallic);
				surfaceData.specular            = Specular;
				surfaceData.smoothness          = saturate(Smoothness),
				surfaceData.occlusion           = Occlusion,
				surfaceData.emission            = Emission,
				surfaceData.alpha               = saturate(Alpha);
				surfaceData.normalTS            = Normal;
				surfaceData.clearCoatMask       = 0;
				surfaceData.clearCoatSmoothness = 1;

				#ifdef _CLEARCOAT
					surfaceData.clearCoatMask       = saturate(CoatMask);
					surfaceData.clearCoatSmoothness = saturate(CoatSmoothness);
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
				#endif

				half4 shadowMask = CalculateShadowMask(inputData);
				Light bandLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
				float illumination = saturate(dot(normalize(inputData.normalWS), bandLight.direction)) * saturate(bandLight.shadowAttenuation);
				float threshold = clamp(_BandThreshold, 0.01, 0.99);
				float midThreshold = clamp(_BandMidThreshold, 0.001, threshold - 0.001);
				float halfWidth = min(max(_BandSoftness * 0.5, fwidth(illumination) * 0.5), (threshold - midThreshold) * 0.45);
				float midLowerEdge = max(0.0001, midThreshold - halfWidth);
				float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
				float lowerEdge = max(0.0001, threshold - halfWidth);
				float upperEdge = max(lowerEdge + 0.0001, min(1.0, threshold + halfWidth));
				float midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
				float litBand = smoothstep(lowerEdge, upperEdge, illumination);
				half attenuation = bandLight.distanceAttenuation;
				#ifdef _LIGHT_LAYERS
					if (!IsMatchingLightLayer(bandLight.layerMask, GetMeshRenderingLayer()))
					{
						attenuation = 0;
					}
				#endif
				half form = saturate(dot(normalize(inputData.normalWS), bandLight.direction) * 0.5h + 0.5h);
				half3 shadeTint = _BandShadeColor.rgb * lerp(1.0h - _BandFormDetail, 1.0h + _BandFormDetail, form);
				half3 midTint = _BandMidColor.rgb * lerp(1.0h - _BandFormDetail * 0.5h, 1.0h + _BandFormDetail * 0.5h, form);
				half3 litTint = _BandLitColor.rgb * lerp(1.0h - _BandFormDetail * 0.5h, 1.0h, form);
				half3 bandTint = lerp(lerp(shadeTint, midTint, midBand), litTint, litBand);
				half4 color = half4(surfaceData.albedo * bandTint * bandLight.color * attenuation + surfaceData.emission, surfaceData.alpha);

				#if defined(_ADDITIONAL_LIGHTS)
					uint pixelLightCount = GetAdditionalLightsCount();
					half3 additionalNormalWS = normalize(inputData.normalWS);
					uint meshRenderingLayers = GetMeshRenderingLayer();
					LIGHT_LOOP_BEGIN(pixelLightCount)
						Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
						color.rgb += surfaceData.albedo * EvaluatePaintedTreeAdditionalLight(light, additionalNormalWS, meshRenderingLayers);
					LIGHT_LOOP_END
				#endif

				color.rgb += surfaceData.albedo * _PainterlyAmbientColor.rgb;

				#ifdef ASE_REFRACTION
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, float4( NormalWS,0 ) ).xyz * ( 1.0 - dot( NormalWS, ViewDirWS ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos.xy ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						color.rgb = MixFog(color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					outRenderingLayers = EncodeMeshRenderingLayer();
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( color.rgb, color.a );
				#else
					return half4( color.rgb, OutputAlpha( color.a, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off
			ColorMask 0

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex vert
			#pragma fragment frag


			#define SHADERPASS SHADERPASS_SHADOWCASTER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;


			float3 _LightDirection;
			float3 _LightPosition;

			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				float3 positionWS = TransformObjectToWorld( input.positionOS.xyz );
				float3 normalWS = TransformObjectToWorldDir(input.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				//code for UNITY_REVERSED_Z is moved into Shadows.hlsl from 6000.0.22 and or higher
				positionCS = ApplyShadowClamping(positionCS);

				output.positionCS = positionCS;
				output.positionWS = positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(	PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_BaseAlbedo1753 = input.ase_texcoord1.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				float Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _CutOff;
					float AlphaClipThresholdShadow = 0.5;
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					#if defined( _ALPHATEST_SHADOW_ON )
						AlphaDiscard( Alpha, AlphaClipThresholdShadow );
					#else
						AlphaDiscard( Alpha, AlphaClipThreshold );
					#endif
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask R
			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag


			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(	PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 uv_BaseAlbedo1753 = input.ase_texcoord1.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				float Alpha = Base_Opacity1739;
				float AlphaClipThreshold = _CutOff;

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300

			#pragma shader_feature EDITOR_VISUALIZATION

			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ENABLENORMALMAP_ON


			#define SHADERPASS SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLETOPLAYERBLEND_ON
			#pragma shader_feature_local _ENABLETINTCOLOR_ON
			#pragma shader_feature_local _ENABLESECONDCOLOR_ON
			#pragma shader_feature_local _ENABLEGRADIENTCOLOR_ON
			#pragma shader_feature_local _ENABLEBACKFACEPROJECTION_ON
			#pragma shader_feature_local _ENABLEWORLDPROJECTION_ON
			#pragma shader_feature_local _ENABLEFLIPNORMALS_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				#ifdef EDITOR_VISUALIZATION
					float4 VizUV : TEXCOORD1;
					float4 LightCoord : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;
			half ASP_GlobalTreeAO;
			half ASP_GlobalTintNoiseUVScale;
			half ASP_GlobalTintNoiseContrast;
			half ASP_GlobalTintNoiseIntensity;
			sampler2D _BaseNormal;
			half ASPT_TopLayerOffset;
			half ASPT_TopLayerContrast;
			half ASPT_TopLayerIntensity;
			half ASPT_TopLayerHeightStart;
			float ASPT_TopLayerHeightFade;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				float3 ase_tangentWS = TransformObjectToWorldDir( input.tangentOS.xyz );
				output.ase_texcoord4.xyz = ase_tangentWS;
				float3 ase_normalWS = TransformObjectToWorldNormal( input.normalOS );
				output.ase_texcoord5.xyz = ase_normalWS;
				float ase_tangentSign = input.tangentOS.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_bitangentWS = cross( ase_normalWS, ase_tangentWS ) * ase_tangentSign;
				output.ase_texcoord6.xyz = ase_bitangentWS;
				
				output.ase_texcoord3.xy = input.texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord3.zw = input.texcoord2.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord4.w = 0;
				output.ase_texcoord5.w = 0;
				output.ase_texcoord6.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef EDITOR_VISUALIZATION
					float2 VizUV = 0;
					float4 LightCoord = 0;
					UnityEditorVizData(input.positionOS.xyz, input.texcoord.xy, input.texcoord1.xy, input.texcoord2.xy, VizUV, LightCoord);
					output.VizUV = float4(VizUV, 0, 0);
					output.LightCoord = LightCoord;
				#endif

				output.positionCS = MetaVertexPosition( input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				output.positionWS = TransformObjectToWorld( input.positionOS.xyz );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.texcoord1 = input.texcoord1;
				output.texcoord2 = input.texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , uint ase_vface : SV_IsFrontFace ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;

				float2 uv_BaseAlbedo1753 = input.ase_texcoord3.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float3 desaturateInitialColor1712 = tex2DNode1753.rgb;
				float desaturateDot1712 = dot( desaturateInitialColor1712, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar1712 = lerp( desaturateInitialColor1712, desaturateDot1712.xxx, _BaseAlbedoDesaturation );
				float3 blendOpSrc3324 = ( desaturateVar1712 * _BaseAlbedoBrightness );
				float3 blendOpDest3324 = _BaseAlbedoColor;
				float3 lerpResult1716 = lerp( _BaseAlbedoColor , ( saturate( (( blendOpDest3324 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest3324 ) * ( 1.0 - blendOpSrc3324 ) ) : ( 2.0 * blendOpDest3324 * blendOpSrc3324 ) ) )) , _BaseLerpBetweenColorandTexture);
				float lerpResult1727 = lerp( 1.0 , input.ase_color.a , saturate( ( _BaseTreeAOIntensity * ASP_GlobalTreeAO ) ));
				float Base_AO1735 = lerpResult1727;
				float3 Base_Albedo1723 = ( lerpResult1716 * Base_AO1735 );
				float3 blendOpSrc1836 = Base_Albedo1723;
				float3 blendOpDest1836 = _GradientColor;
				float3 temp_output_1836_0 = ( saturate( (( blendOpDest1836 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1836 ) * ( 1.0 - blendOpSrc1836 ) ) : ( 2.0 * blendOpDest1836 * blendOpSrc1836 ) ) ));
				float2 texCoord1840 = input.ase_texcoord3.zw * float2( 1,1 ) + float2( 0,0 );
				float saferPower1879 = abs( ( ( 1.0 - texCoord1840.y ) * _GradientColorOffset ) );
				float temp_output_1848_0 = saturate( pow( saferPower1879 , _GradientColorContrast ) );
				float lerpResult1877 = lerp( temp_output_1848_0 , ( 1.0 - temp_output_1848_0 ) , _GradientColorInvertMask);
				float Gradient_Color_Mask1854 = ( lerpResult1877 * _GradientColorIntensity );
				float3 lerpResult1838 = lerp( Base_Albedo1723 , temp_output_1836_0 , Gradient_Color_Mask1854);
				float3 Gradient_Color_Albedo1856 = lerpResult1838;
				#ifdef _ENABLEGRADIENTCOLOR_ON
				float3 staticSwitch1867 = Gradient_Color_Albedo1856;
				#else
				float3 staticSwitch1867 = Base_Albedo1723;
				#endif
				float3 Step_2_Albedo1914 = staticSwitch1867;
				float3 blendOpSrc1897 = Base_Albedo1723;
				float3 blendOpDest1897 = _SecondColor;
				float3 temp_output_1897_0 = ( saturate( (( blendOpDest1897 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1897 ) * ( 1.0 - blendOpSrc1897 ) ) : ( 2.0 * blendOpDest1897 * blendOpSrc1897 ) ) ));
				float saferPower1886 = abs( ( ( 1.0 - input.ase_color.a ) * _SecondColorOffset ) );
				float temp_output_1889_0 = saturate( pow( saferPower1886 , ( _SecondColorContrast + 2.0 ) ) );
				float lerpResult1890 = lerp( temp_output_1889_0 , ( 1.0 - temp_output_1889_0 ) , _SecondColorInvertMask);
				float Second_Color_Mask1895 = ( lerpResult1890 * _SecondColorIntensity );
				float3 lerpResult1899 = lerp( Step_2_Albedo1914 , temp_output_1897_0 , Second_Color_Mask1895);
				float3 Second_Color_Albedo1902 = lerpResult1899;
				#ifdef _ENABLESECONDCOLOR_ON
				float3 staticSwitch1921 = Second_Color_Albedo1902;
				#else
				float3 staticSwitch1921 = Step_2_Albedo1914;
				#endif
				float3 Step_3_Albedo1954 = staticSwitch1921;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Tint_Color_Mask1949 = saturate( round( ( frac( ( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) * ( _TintNoiseOffset * ASP_GlobalTintNoiseUVScale ) ) ) * ( _TintNoiseContrast * ASP_GlobalTintNoiseContrast ) ) ) );
				float3 lerpResult1951 = lerp( _TintColor1.rgb , _TintColor2.rgb , Tint_Color_Mask1949);
				float3 blendOpSrc1952 = Step_3_Albedo1954;
				float3 blendOpDest1952 = lerpResult1951;
				float3 temp_output_1952_0 = ( saturate( (( blendOpDest1952 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1952 ) * ( 1.0 - blendOpSrc1952 ) ) : ( 2.0 * blendOpDest1952 * blendOpSrc1952 ) ) ));
				float temp_output_1961_0 = ( _TintNoiseIntensity * ASP_GlobalTintNoiseIntensity );
				float3 lerpResult1956 = lerp( Step_3_Albedo1954 , temp_output_1952_0 , temp_output_1961_0);
				float3 Tint_Color_Albedo1963 = lerpResult1956;
				#ifdef _ENABLETINTCOLOR_ON
				float3 staticSwitch2080 = Tint_Color_Albedo1963;
				#else
				float3 staticSwitch2080 = Step_3_Albedo1954;
				#endif
				float3 Step_4_Albedo2083 = staticSwitch2080;
				float3 Top_Layer_Color1995 = _TopLayerColor;
				#if defined(_ENABLENORMALMAP_ON)
				float2 uv_BaseNormal1741 = input.ase_texcoord3.xy;
				float3 unpack1741 = UnpackNormalScale( tex2D( _BaseNormal, uv_BaseNormal1741 ), _BaseNormalIntensity );
				unpack1741.z = lerp( 1, unpack1741.z, saturate(_BaseNormalIntensity) );
				#else
				float3 unpack1741 = float3(0, 0, 1);
				#endif
				float3 tex2DNode1741 = unpack1741;
				float3 break105_g45 = tex2DNode1741;
				float switchResult107_g45 = (((ase_vface>0)?(break105_g45.z):(-break105_g45.z)));
				float3 appendResult108_g45 = (float3(break105_g45.x , break105_g45.y , switchResult107_g45));
				#ifdef _ENABLEFLIPNORMALS_ON
				float3 staticSwitch1744 = appendResult108_g45;
				#else
				float3 staticSwitch1744 = tex2DNode1741;
				#endif
				float3 Base_Normal1747 = staticSwitch1744;
				float3 desaturateInitialColor2013 = Base_Normal1747;
				float desaturateDot2013 = dot( desaturateInitialColor2013, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar2013 = lerp( desaturateInitialColor2013, desaturateDot2013.xxx, 1.0 );
				float3 ase_tangentWS = input.ase_texcoord4.xyz;
				float3 ase_normalWS = input.ase_texcoord5.xyz;
				float3 ase_bitangentWS = input.ase_texcoord6.xyz;
				float3 tanToWorld0 = float3( ase_tangentWS.x, ase_bitangentWS.x, ase_normalWS.x );
				float3 tanToWorld1 = float3( ase_tangentWS.y, ase_bitangentWS.y, ase_normalWS.y );
				float3 tanToWorld2 = float3( ase_tangentWS.z, ase_bitangentWS.z, ase_normalWS.z );
				float3 tanNormal2012 = Base_Normal1747;
				float3 worldNormal2012 = float3( dot( tanToWorld0, tanNormal2012 ), dot( tanToWorld1, tanNormal2012 ), dot( tanToWorld2, tanNormal2012 ) );
				float3 temp_cast_0 = (worldNormal2012.y).xxx;
				#ifdef _ENABLEWORLDPROJECTION_ON
				float3 staticSwitch2017 = temp_cast_0;
				#else
				float3 staticSwitch2017 = desaturateVar2013;
				#endif
				float3 saferPower17_g32 = abs( abs( ( saturate( staticSwitch2017 ) + ( _TopLayerOffset * ASPT_TopLayerOffset ) ) ) );
				float temp_output_2005_0 = ( _TopLayerContrast * ASPT_TopLayerContrast );
				float3 temp_cast_1 = (temp_output_2005_0).xxx;
				float saferPower2024 = abs( ( 1.0 - input.ase_color.a ) );
				float Top_Layer_Contrast2025 = temp_output_2005_0;
				float3 lerpResult2029 = lerp( ( ( saturate( pow( saferPower17_g32 , temp_cast_1 ) ) * ( _TopLayerIntensity * ASPT_TopLayerIntensity ) ) * saturate(  (0.0 + ( PositionWS.y - ASPT_TopLayerHeightStart ) * ( 1.0 - 0.0 ) / ( ( ASPT_TopLayerHeightStart + ASPT_TopLayerHeightFade ) - ASPT_TopLayerHeightStart ) ) ) ) , float3( 0,0,0 ) , saturate( ( pow( saferPower2024 , Top_Layer_Contrast2025 ) * _TopLayerAOMask ) ));
				float3 Top_Layer_Mask2031 = saturate( lerpResult2029 );
				float3 lerpResult2034 = lerp( Step_4_Albedo2083 , Top_Layer_Color1995 , Top_Layer_Mask2031);
				float3 lerpResult2037 = lerp( Step_4_Albedo2083 , lerpResult2034 , saturate( ( ase_vface > 0 ? +1 : -1 ) ));
				#ifdef _ENABLEBACKFACEPROJECTION_ON
				float3 staticSwitch2041 = lerpResult2034;
				#else
				float3 staticSwitch2041 = lerpResult2037;
				#endif
				float3 Top_Layer_Albedo2042 = staticSwitch2041;
				#ifdef _ENABLETOPLAYERBLEND_ON
				float3 staticSwitch2043 = Top_Layer_Albedo2042;
				#else
				float3 staticSwitch2043 = Step_4_Albedo2083;
				#endif
				float3 Output_Albedo1873 = staticSwitch2043;
				
				
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				float3 BaseColor = Output_Albedo1873;
				float3 Emission = 0;
				float Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _CutOff;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = BaseColor;
				metaInput.Emission = Emission;
				#ifdef EDITOR_VISUALIZATION
					metaInput.VizUV = input.VizUV.xy;
					metaInput.LightCoord = input.LightCoord;
				#endif

				return UnityMetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ENABLENORMALMAP_ON


			#define SHADERPASS SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLETOPLAYERBLEND_ON
			#pragma shader_feature_local _ENABLETINTCOLOR_ON
			#pragma shader_feature_local _ENABLESECONDCOLOR_ON
			#pragma shader_feature_local _ENABLEGRADIENTCOLOR_ON
			#pragma shader_feature_local _ENABLEBACKFACEPROJECTION_ON
			#pragma shader_feature_local _ENABLEWORLDPROJECTION_ON
			#pragma shader_feature_local _ENABLEFLIPNORMALS_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;
			half ASP_GlobalTreeAO;
			half ASP_GlobalTintNoiseUVScale;
			half ASP_GlobalTintNoiseContrast;
			half ASP_GlobalTintNoiseIntensity;
			sampler2D _BaseNormal;
			half ASPT_TopLayerOffset;
			half ASPT_TopLayerContrast;
			half ASPT_TopLayerIntensity;
			half ASPT_TopLayerHeightStart;
			float ASPT_TopLayerHeightFade;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_TRANSFER_INSTANCE_ID( input, output );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				float3 ase_tangentWS = TransformObjectToWorldDir( input.tangentOS.xyz );
				output.ase_texcoord2.xyz = ase_tangentWS;
				float3 ase_normalWS = TransformObjectToWorldNormal( input.normalOS );
				output.ase_texcoord3.xyz = ase_normalWS;
				float ase_tangentSign = input.tangentOS.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_bitangentWS = cross( ase_normalWS, ase_tangentWS ) * ase_tangentSign;
				output.ase_texcoord4.xyz = ase_bitangentWS;
				
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				output.ase_color = input.ase_color;
				output.ase_texcoord1.zw = input.ase_texcoord2.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord2.w = 0;
				output.ase_texcoord3.w = 0;
				output.ase_texcoord4.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input , uint ase_vface : SV_IsFrontFace ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;

				float2 uv_BaseAlbedo1753 = input.ase_texcoord1.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float3 desaturateInitialColor1712 = tex2DNode1753.rgb;
				float desaturateDot1712 = dot( desaturateInitialColor1712, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar1712 = lerp( desaturateInitialColor1712, desaturateDot1712.xxx, _BaseAlbedoDesaturation );
				float3 blendOpSrc3324 = ( desaturateVar1712 * _BaseAlbedoBrightness );
				float3 blendOpDest3324 = _BaseAlbedoColor;
				float3 lerpResult1716 = lerp( _BaseAlbedoColor , ( saturate( (( blendOpDest3324 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest3324 ) * ( 1.0 - blendOpSrc3324 ) ) : ( 2.0 * blendOpDest3324 * blendOpSrc3324 ) ) )) , _BaseLerpBetweenColorandTexture);
				float lerpResult1727 = lerp( 1.0 , input.ase_color.a , saturate( ( _BaseTreeAOIntensity * ASP_GlobalTreeAO ) ));
				float Base_AO1735 = lerpResult1727;
				float3 Base_Albedo1723 = ( lerpResult1716 * Base_AO1735 );
				float3 blendOpSrc1836 = Base_Albedo1723;
				float3 blendOpDest1836 = _GradientColor;
				float3 temp_output_1836_0 = ( saturate( (( blendOpDest1836 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1836 ) * ( 1.0 - blendOpSrc1836 ) ) : ( 2.0 * blendOpDest1836 * blendOpSrc1836 ) ) ));
				float2 texCoord1840 = input.ase_texcoord1.zw * float2( 1,1 ) + float2( 0,0 );
				float saferPower1879 = abs( ( ( 1.0 - texCoord1840.y ) * _GradientColorOffset ) );
				float temp_output_1848_0 = saturate( pow( saferPower1879 , _GradientColorContrast ) );
				float lerpResult1877 = lerp( temp_output_1848_0 , ( 1.0 - temp_output_1848_0 ) , _GradientColorInvertMask);
				float Gradient_Color_Mask1854 = ( lerpResult1877 * _GradientColorIntensity );
				float3 lerpResult1838 = lerp( Base_Albedo1723 , temp_output_1836_0 , Gradient_Color_Mask1854);
				float3 Gradient_Color_Albedo1856 = lerpResult1838;
				#ifdef _ENABLEGRADIENTCOLOR_ON
				float3 staticSwitch1867 = Gradient_Color_Albedo1856;
				#else
				float3 staticSwitch1867 = Base_Albedo1723;
				#endif
				float3 Step_2_Albedo1914 = staticSwitch1867;
				float3 blendOpSrc1897 = Base_Albedo1723;
				float3 blendOpDest1897 = _SecondColor;
				float3 temp_output_1897_0 = ( saturate( (( blendOpDest1897 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1897 ) * ( 1.0 - blendOpSrc1897 ) ) : ( 2.0 * blendOpDest1897 * blendOpSrc1897 ) ) ));
				float saferPower1886 = abs( ( ( 1.0 - input.ase_color.a ) * _SecondColorOffset ) );
				float temp_output_1889_0 = saturate( pow( saferPower1886 , ( _SecondColorContrast + 2.0 ) ) );
				float lerpResult1890 = lerp( temp_output_1889_0 , ( 1.0 - temp_output_1889_0 ) , _SecondColorInvertMask);
				float Second_Color_Mask1895 = ( lerpResult1890 * _SecondColorIntensity );
				float3 lerpResult1899 = lerp( Step_2_Albedo1914 , temp_output_1897_0 , Second_Color_Mask1895);
				float3 Second_Color_Albedo1902 = lerpResult1899;
				#ifdef _ENABLESECONDCOLOR_ON
				float3 staticSwitch1921 = Second_Color_Albedo1902;
				#else
				float3 staticSwitch1921 = Step_2_Albedo1914;
				#endif
				float3 Step_3_Albedo1954 = staticSwitch1921;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Tint_Color_Mask1949 = saturate( round( ( frac( ( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) * ( _TintNoiseOffset * ASP_GlobalTintNoiseUVScale ) ) ) * ( _TintNoiseContrast * ASP_GlobalTintNoiseContrast ) ) ) );
				float3 lerpResult1951 = lerp( _TintColor1.rgb , _TintColor2.rgb , Tint_Color_Mask1949);
				float3 blendOpSrc1952 = Step_3_Albedo1954;
				float3 blendOpDest1952 = lerpResult1951;
				float3 temp_output_1952_0 = ( saturate( (( blendOpDest1952 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest1952 ) * ( 1.0 - blendOpSrc1952 ) ) : ( 2.0 * blendOpDest1952 * blendOpSrc1952 ) ) ));
				float temp_output_1961_0 = ( _TintNoiseIntensity * ASP_GlobalTintNoiseIntensity );
				float3 lerpResult1956 = lerp( Step_3_Albedo1954 , temp_output_1952_0 , temp_output_1961_0);
				float3 Tint_Color_Albedo1963 = lerpResult1956;
				#ifdef _ENABLETINTCOLOR_ON
				float3 staticSwitch2080 = Tint_Color_Albedo1963;
				#else
				float3 staticSwitch2080 = Step_3_Albedo1954;
				#endif
				float3 Step_4_Albedo2083 = staticSwitch2080;
				float3 Top_Layer_Color1995 = _TopLayerColor;
				#if defined(_ENABLENORMALMAP_ON)
				float2 uv_BaseNormal1741 = input.ase_texcoord1.xy;
				float3 unpack1741 = UnpackNormalScale( tex2D( _BaseNormal, uv_BaseNormal1741 ), _BaseNormalIntensity );
				unpack1741.z = lerp( 1, unpack1741.z, saturate(_BaseNormalIntensity) );
				#else
				float3 unpack1741 = float3(0, 0, 1);
				#endif
				float3 tex2DNode1741 = unpack1741;
				float3 break105_g45 = tex2DNode1741;
				float switchResult107_g45 = (((ase_vface>0)?(break105_g45.z):(-break105_g45.z)));
				float3 appendResult108_g45 = (float3(break105_g45.x , break105_g45.y , switchResult107_g45));
				#ifdef _ENABLEFLIPNORMALS_ON
				float3 staticSwitch1744 = appendResult108_g45;
				#else
				float3 staticSwitch1744 = tex2DNode1741;
				#endif
				float3 Base_Normal1747 = staticSwitch1744;
				float3 desaturateInitialColor2013 = Base_Normal1747;
				float desaturateDot2013 = dot( desaturateInitialColor2013, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar2013 = lerp( desaturateInitialColor2013, desaturateDot2013.xxx, 1.0 );
				float3 ase_tangentWS = input.ase_texcoord2.xyz;
				float3 ase_normalWS = input.ase_texcoord3.xyz;
				float3 ase_bitangentWS = input.ase_texcoord4.xyz;
				float3 tanToWorld0 = float3( ase_tangentWS.x, ase_bitangentWS.x, ase_normalWS.x );
				float3 tanToWorld1 = float3( ase_tangentWS.y, ase_bitangentWS.y, ase_normalWS.y );
				float3 tanToWorld2 = float3( ase_tangentWS.z, ase_bitangentWS.z, ase_normalWS.z );
				float3 tanNormal2012 = Base_Normal1747;
				float3 worldNormal2012 = float3( dot( tanToWorld0, tanNormal2012 ), dot( tanToWorld1, tanNormal2012 ), dot( tanToWorld2, tanNormal2012 ) );
				float3 temp_cast_0 = (worldNormal2012.y).xxx;
				#ifdef _ENABLEWORLDPROJECTION_ON
				float3 staticSwitch2017 = temp_cast_0;
				#else
				float3 staticSwitch2017 = desaturateVar2013;
				#endif
				float3 saferPower17_g32 = abs( abs( ( saturate( staticSwitch2017 ) + ( _TopLayerOffset * ASPT_TopLayerOffset ) ) ) );
				float temp_output_2005_0 = ( _TopLayerContrast * ASPT_TopLayerContrast );
				float3 temp_cast_1 = (temp_output_2005_0).xxx;
				float saferPower2024 = abs( ( 1.0 - input.ase_color.a ) );
				float Top_Layer_Contrast2025 = temp_output_2005_0;
				float3 lerpResult2029 = lerp( ( ( saturate( pow( saferPower17_g32 , temp_cast_1 ) ) * ( _TopLayerIntensity * ASPT_TopLayerIntensity ) ) * saturate(  (0.0 + ( PositionWS.y - ASPT_TopLayerHeightStart ) * ( 1.0 - 0.0 ) / ( ( ASPT_TopLayerHeightStart + ASPT_TopLayerHeightFade ) - ASPT_TopLayerHeightStart ) ) ) ) , float3( 0,0,0 ) , saturate( ( pow( saferPower2024 , Top_Layer_Contrast2025 ) * _TopLayerAOMask ) ));
				float3 Top_Layer_Mask2031 = saturate( lerpResult2029 );
				float3 lerpResult2034 = lerp( Step_4_Albedo2083 , Top_Layer_Color1995 , Top_Layer_Mask2031);
				float3 lerpResult2037 = lerp( Step_4_Albedo2083 , lerpResult2034 , saturate( ( ase_vface > 0 ? +1 : -1 ) ));
				#ifdef _ENABLEBACKFACEPROJECTION_ON
				float3 staticSwitch2041 = lerpResult2034;
				#else
				float3 staticSwitch2041 = lerpResult2037;
				#endif
				float3 Top_Layer_Albedo2042 = staticSwitch2041;
				#ifdef _ENABLETOPLAYERBLEND_ON
				float3 staticSwitch2043 = Top_Layer_Albedo2042;
				#else
				float3 staticSwitch2043 = Step_4_Albedo2083;
				#endif
				float3 Output_Albedo1873 = staticSwitch2043;
				
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				float3 BaseColor = Output_Albedo1873;
				float Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _CutOff;
				#endif

				half4 color = half4(BaseColor, Alpha );

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				return color;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZWrite On
			Blend One Zero
			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local_fragment _ENABLENORMALMAP_ON


			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
			//#define SHADERPASS SHADERPASS_DEPTHNORMALS

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined( UNITY_INSTANCING_ENABLED ) && defined( ASE_INSTANCED_TERRAIN ) && ( defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL) || defined(_INSTANCEDTERRAINNORMALS_PIXEL) )
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLEFLIPNORMALS_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				half4 texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseNormal;
			sampler2D _BaseAlbedo;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord3.xy = input.texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					output.tangentWS.zw = input.texcoord.xy;
					output.tangentWS.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.texcoord = input.texcoord;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(	PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out uint outRenderingLayers : SV_Target1
						#endif
						, uint ase_vface : SV_IsFrontFace )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float2 sampleCoords = (input.tangentWS.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					NormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					TangentWS = -cross(GetObjectToWorldMatrix()._13_23_33, NormalWS);
					BitangentWS = cross(NormalWS, -TangentWS);
				#endif

				#if defined(_ENABLENORMALMAP_ON)
				float2 uv_BaseNormal1741 = input.ase_texcoord3.xy;
				float3 unpack1741 = UnpackNormalScale( tex2D( _BaseNormal, uv_BaseNormal1741 ), _BaseNormalIntensity );
				unpack1741.z = lerp( 1, unpack1741.z, saturate(_BaseNormalIntensity) );
				#else
				float3 unpack1741 = float3(0, 0, 1);
				#endif
				float3 tex2DNode1741 = unpack1741;
				float3 break105_g45 = tex2DNode1741;
				float switchResult107_g45 = (((ase_vface>0)?(break105_g45.z):(-break105_g45.z)));
				float3 appendResult108_g45 = (float3(break105_g45.x , break105_g45.y , switchResult107_g45));
				#ifdef _ENABLEFLIPNORMALS_ON
				float3 staticSwitch1744 = appendResult108_g45;
				#else
				float3 staticSwitch1744 = tex2DNode1741;
				#endif
				float3 Base_Normal1747 = staticSwitch1744;
				
				float2 uv_BaseAlbedo1753 = input.ase_texcoord3.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				float3 Normal = Base_Normal1747;
				float Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _CutOff;
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					#if defined(_NORMALMAP)
						#if _NORMAL_DROPOFF_TS
							float3 normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							float3 normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							float3 normalWS = Normal;
						#endif
					#else
						float3 normalWS = NormalWS;
					#endif
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					outRenderingLayers = EncodeMeshRenderingLayer();
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag


			#define SCENESELECTIONPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag( PackedVaryings input
				#if defined( ASE_DEPTH_WRITE_ON )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float2 uv_BaseAlbedo1753 = input.ase_texcoord1.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				surfaceDescription.Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _CutOff;
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				return half4( _ObjectId, _PassValue, 1.0, 1.0 );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#pragma multi_compile_fragment _ DEBUG_DISPLAY
			#define _NORMALMAP 1
			#define ASE_VERSION 19907
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag


		    #define SCENEPICKINGPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma shader_feature_local _ENABLEWIND_ON
			#pragma shader_feature_local _ENABLESTATICMESHSUPPORT_ON
			#pragma shader_feature_local _ENABLEGLANCINGANGLECUT_ON


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BandLitColor;
				half4 _BandMidColor;
				half4 _BandShadeColor;
				half _BandThreshold;
				half _BandMidThreshold;
				half _BandSoftness;
				half _BandFormDetail;
			float4 _TintColor1;
			float4 _TintColor2;
			float3 _SecondColor;
			float3 _TopLayerColor;
			float3 _GradientColor;
			half3 _BaseAlbedoColor;
			float _TintNoiseOffset;
			float _TintNoiseContrast;
			float _TintNoiseIntensity;
			float _BaseNormalIntensity;
			half _TopLayerOffset;
			half _TopLayerIntensity;
			half _TopLayerAOMask;
			float _CutOff;
			float _BaseGlancingAngleCut;
			half _TopLayerContrast;
			float _SecondColorInvertMask;
			float _WindTreeFlexibility;
			float _WindTreeBaseRigidity;
			float _WindLeafScale;
			float _WindLeafSpeed;
			float _WindLeafAmplitude;
			float _WindLeafTurbulence;
			float _WindLeafOffset;
			float _BaseAlbedoDesaturation;
			float _BaseAlbedoBrightness;
			float _BaseLerpBetweenColorandTexture;
			float _BaseTreeAOIntensity;
			float _GradientColorOffset;
			float _GradientColorContrast;
			float _GradientColorInvertMask;
			float _GradientColorIntensity;
			float _SecondColorOffset;
			float _SecondColorContrast;
			float _SecondColorIntensity;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D ASP_GlobalTintNoiseTexture;
			half3 ASPW_WindDirection;
			half ASPW_WindTreeSpeed;
			half ASPW_WindTreeAmplitude;
			half ASPW_WindTreeFlexibility;
			half ASPW_WindTreeLeafSpeed;
			half ASPW_WindTreeLeafAmplitude;
			half ASPW_WindTreeLeafTurbulence;
			half ASPW_WindTreeLeafOffset;
			float ASPW_WindToggle;
			sampler2D _BaseAlbedo;


			float3 ASESafeNormalize(float3 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 temp_cast_0 = (0.0).xxx;
				float3 Global_Wind_Direction493_g37 = ASPW_WindDirection;
				float3 worldToObjDir269_g37 = mul( GetWorldToObjectMatrix(), float4( Global_Wind_Direction493_g37, 0.0 ) ).xyz;
				float3 normalizeResult268_g37 = ASESafeNormalize( worldToObjDir269_g37 );
				float3 Wind_Direction_Leaf85_g37 = normalizeResult268_g37;
				float3 break86_g37 = Wind_Direction_Leaf85_g37;
				float3 appendResult89_g37 = (float3(break86_g37.z , 0.0 , ( break86_g37.x * -1.0 )));
				float3 Wind_Direction91_g37 = appendResult89_g37;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float Wind_Tree_Randomization149_g37 = frac( ( ( ase_objectPosition.x + ase_objectPosition.y + ase_objectPosition.z ) * 1.23 ) );
				float temp_output_56_0_g37 = ( Wind_Tree_Randomization149_g37 + _TimeParameters.x );
				float Global_Wind_Tree_Speed491_g37 = ASPW_WindTreeSpeed;
				float temp_output_213_0_g37 = ( Global_Wind_Tree_Speed491_g37 * 7.0 );
				float Global_Wind_Tree_Amplitude464_g37 = ASPW_WindTreeAmplitude;
				float Global_Wind_Tree_Flexibility490_g37 = ASPW_WindTreeFlexibility;
				float temp_output_383_0_g37 = ( ( _WindTreeFlexibility * Global_Wind_Tree_Flexibility490_g37 ) * 0.3 );
				float Wind_Tree_257_g37 = ( ( ( ( ( sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.1 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.3 ) ) ) + sin( ( temp_output_56_0_g37 * ( temp_output_213_0_g37 * 0.5 ) ) ) ) + Global_Wind_Tree_Amplitude464_g37 ) * Global_Wind_Tree_Amplitude464_g37 ) * 0.01 ) * temp_output_383_0_g37 );
				float3 rotatedValue99_g37 = RotateAroundAxis( float3( 0,0,0 ), input.positionOS.xyz, Wind_Direction91_g37, Wind_Tree_257_g37 );
				float3 Rotate_About_Axis354_g37 = ( rotatedValue99_g37 - input.positionOS.xyz );
				float3 break452_g37 = normalizeResult268_g37;
				float3 appendResult454_g37 = (float3(break452_g37.x , 0.0 , break452_g37.z));
				float3 Wind_Direction_SM_Support453_g37 = appendResult454_g37;
				float Wind_Tree_Flexibility483_g37 = temp_output_383_0_g37;
				#ifdef _ENABLESTATICMESHSUPPORT_ON
				float3 staticSwitch482_g37 = ( Global_Wind_Tree_Amplitude464_g37 * Wind_Tree_257_g37 * Wind_Direction_SM_Support453_g37 * Wind_Tree_Flexibility483_g37 );
				#else
				float3 staticSwitch482_g37 = Rotate_About_Axis354_g37;
				#endif
				float3 Wind_Global450_g37 = staticSwitch482_g37;
				float2 texCoord433_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float Wind_Mask_UV_CH2_VChannel434_g37 = texCoord433_g37.y;
				float saferPower113_g37 = abs( Wind_Mask_UV_CH2_VChannel434_g37 );
				float Wind_Tree_Mask114_g37 = pow( saferPower113_g37 , _WindTreeBaseRigidity );
				float3 temp_output_385_0_g37 = ( Wind_Global450_g37 * Wind_Tree_Mask114_g37 );
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				float temp_output_227_0_g37 = ( ( ase_positionWS.y * ( _WindLeafScale * 30.0 ) ) + _TimeParameters.x );
				float Global_Wind_Tree_Leaf_Speed495_g37 = ASPW_WindTreeLeafSpeed;
				float temp_output_223_0_g37 = ( _WindLeafSpeed * Global_Wind_Tree_Leaf_Speed495_g37 * 4.0 );
				float Global_Wind_Tree_Leaf_Amplitude497_g37 = ASPW_WindTreeLeafAmplitude;
				float temp_output_242_0_g37 = ( ( sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 0.5 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.25 ) ) ) + sin( ( temp_output_227_0_g37 * ( temp_output_223_0_g37 * 1.5 ) ) ) ) * ( ( _WindLeafAmplitude * Global_Wind_Tree_Leaf_Amplitude497_g37 ) * 0.1 ) );
				float temp_output_243_0_g37 = ( temp_output_242_0_g37 * 0.2 );
				float3 appendResult244_g37 = (float3(temp_output_243_0_g37 , temp_output_242_0_g37 , temp_output_243_0_g37));
				float3 Wind_Leaf245_g37 = appendResult244_g37;
				float3 break416_g37 = normalizeResult268_g37;
				float3 appendResult417_g37 = (float3(( break416_g37.x * -1.0 ) , ( break416_g37.z * -1.0 ) , 0.0));
				float3 Wind_Direction_Turbulence419_g37 = appendResult417_g37;
				float2 appendResult322_g37 = (float2(ase_positionWS.x , ase_positionWS.z));
				float Global_Wind_Tree_Leaf_Turbulence502_g37 = ASPW_WindTreeLeafTurbulence;
				float Wind_Leaf_Turbulence334_g37 = saturate( ( tex2Dlod( ASP_GlobalTintNoiseTexture, float4( ( ( ( Wind_Direction_Turbulence419_g37 * _TimeParameters.x ) * 0.1 ) + float3( ( appendResult322_g37 * 0.1 ) ,  0.0 ) ).xy, 0, 0.0) ).g + ( ( Global_Wind_Tree_Leaf_Turbulence502_g37 * _WindLeafTurbulence ) * 0.1 ) ) );
				float Wind_Mask_VColor_Blue429_g37 = input.ase_color.b;
				float3 Wind_Leaf_Mask285_g37 = ( ( ( Wind_Leaf245_g37 * Wind_Direction_Leaf85_g37 ) * Wind_Leaf_Turbulence334_g37 ) * Wind_Mask_VColor_Blue429_g37 );
				float Global_Wind_Tree_Leaf_Offset500_g37 = ASPW_WindTreeLeafOffset;
				float Wind_Mask_VColor_Green430_g37 = input.ase_color.g;
				float2 texCoord312_g37 = input.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float3 Wind_Leaf_Offset314_g37 = ( ( ( ( Wind_Direction_Leaf85_g37 * ( _WindLeafOffset * Global_Wind_Tree_Leaf_Offset500_g37 * 0.5 ) ) + Wind_Leaf_Mask285_g37 ) * Wind_Mask_VColor_Green430_g37 ) * texCoord312_g37.y );
				float Global_Wind_Toggle504_g37 = ASPW_WindToggle;
				float3 lerpResult125_g37 = lerp( float3( 0,0,0 ) , ( temp_output_385_0_g37 + ( Wind_Leaf_Mask285_g37 + Wind_Leaf_Offset314_g37 ) ) , Global_Wind_Toggle504_g37);
				#ifdef _ENABLEWIND_ON
				float3 staticSwitch469_g37 = lerpResult125_g37;
				#else
				float3 staticSwitch469_g37 = temp_cast_0;
				#endif
				
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = staticSwitch469_g37;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag( PackedVaryings input
				#if defined( ASE_DEPTH_WRITE_ON )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float2 uv_BaseAlbedo1753 = input.ase_texcoord1.xy;
				float4 tex2DNode1753 = tex2D( _BaseAlbedo, uv_BaseAlbedo1753 );
				float Opacity_Texture1737 = tex2DNode1753.a;
				float3 normalizeResult1780 = normalize( cross( ddx( PositionWS ) , ddy( PositionWS ) ) );
				float3 normalizeResult1787 = normalize( ( _WorldSpaceCameraPos - PositionWS ) );
				float dotResult1782 = dot( normalizeResult1780 , normalizeResult1787 );
				float lerpResult1794 = lerp( 1.0 , abs( dotResult1782 ) , _BaseGlancingAngleCut);
				#ifdef _ENABLEGLANCINGANGLECUT_ON
				float staticSwitch1788 = lerpResult1794;
				#else
				float staticSwitch1788 = 1.0;
				#endif
				float Base_Glancing_Angle1790 = staticSwitch1788;
				float Base_Opacity1739 = ( Opacity_Texture1737 * Base_Glancing_Angle1790 );
				

				surfaceDescription.Alpha = Base_Opacity1739;
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = _CutOff;
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = input.positionCS.z;
				#endif

				return unity_SelectionID;
			}
			ENDHLSL
		}

	
	}
	
	
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
