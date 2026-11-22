struct Attributes
{
 float3 positionOS : POSITION;
 float3 normalOS : NORMAL;
 float4 tangentOS : TANGENT;
 float4 uv0 : TEXCOORD0;
 float4 uv1 : TEXCOORD1;
 float4 uv2 : TEXCOORD2;
 float4 color : COLOR;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
};
struct Varyings
{
 float4 positionCS : SV_POSITION;
 float3 positionWS;
 float3 normalWS;
 float4 tangentWS;
 float4 texCoord0;
#if defined(EDITOR_VISUALIZATION) && defined(OVERLOOK_PAINTED_GRASS)
 float4 texCoord1;
 float4 texCoord2;
#endif
 float4 color;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion;
#endif
 float4 fogFactorAndVertexLight;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};
struct SurfaceDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 TangentSpaceNormal;
 float3 WorldSpacePosition;
 float4 uv0;
 float4 VertexColor;
 float3 TimeParameters;
 float FaceSign;
};
struct VertexDescriptionInputs
{
 float3 ObjectSpaceNormal;
 float3 WorldSpaceNormal;
 float3 ObjectSpaceTangent;
 float3 ObjectSpacePosition;
 float3 WorldSpacePosition;
 float4 VertexColor;
 float3 TimeParameters;
};
struct PackedVaryings
{
 float4 positionCS : SV_POSITION;
#if defined(LIGHTMAP_ON)
 float2 staticLightmapUV : INTERP0;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
 float2 dynamicLightmapUV : INTERP1;
#endif
#if !defined(LIGHTMAP_ON)
 float3 sh : INTERP2;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
 float4 probeOcclusion : INTERP3;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
 float4 shadowCoord : INTERP4;
#endif
 float4 tangentWS : INTERP5;
 float4 texCoord0 : INTERP6;
#if defined(EDITOR_VISUALIZATION) && defined(OVERLOOK_PAINTED_GRASS)
 float4 texCoord1 : INTERP0;
 float4 texCoord2 : INTERP1;
#endif
 float4 color : INTERP7;
 float4 fogFactorAndVertexLight : INTERP8;
 float3 positionWS : INTERP9;
 float3 normalWS : INTERP10;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
#if defined(EDITOR_VISUALIZATION) && defined(OVERLOOK_PAINTED_GRASS)
output.texCoord1 = input.texCoord1;
output.texCoord2 = input.texCoord2;
#endif
output.color.xyzw = input.color;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.positionWS.xyz = input.positionWS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
#if defined(EDITOR_VISUALIZATION) && defined(OVERLOOK_PAINTED_GRASS)
output.texCoord1 = input.texCoord1;
output.texCoord2 = input.texCoord2;
#endif
output.color = input.color.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.positionWS.xyz;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

CBUFFER_START(UnityPerMaterial)
float4 _Trunk_Noise_Color;
float4 _Trunk_Base_Color;
float4 _Leaf_Noise_Color;
float4 _Leaf_Base_Color;
float4 _Leaf_Noise_Large_Color;
float _Color_Noise_Large_Freq;
float _Color_Noise_Small_Freq;
float4 _Leaf_Texture_TexelSize;
float4 _Trunk_Texture_TexelSize;
float4 _Frosting_Color;
float _Frosting_Falloff;
float _Frosting_Height;
float _Trunk_Metallic;
float _Trunk_Smoothness;
float _Enable_Frosting;
float _Frosting_Use_World_Normals;
float _Trunk_Flat_Color_Switch;
float _Leaf_Flat_Color;
float4 _Emissive_Color;
float4 _Emissive_2_Color;
float4 _Trunk_Emissive_Color;
float4 _Emissive_2_Mask_TexelSize;
float4 _Emissive_Mask_TexelSize;
float4 _Trunk_Emissive_Mask_TexelSize;
float _Emissive_Amount;
float _Pulse_Tiling;
float _Pulse_Speed;
float4 _Emissive_Pulse_Map_TexelSize;
float _Strong_Wind_Strength;
float _Strong_Wind_Frequency;
float _Enable_Breeze;
float _Breeze_Strength;
float _Enable_Light_Wind;
float _Enable_Strong_Wind;
float _Enable_Wind_Twist;
float _Light_Wind_Strength;
float _Wind_Twist_Strength;
float2 _Trunk_Tiling;
float2 _Trunk_Offset;
float4 _Trunk_Normal_TexelSize;
float4 _Leaf_Normal_TexelSize;
float _Enable_Trunk_Normal;
float _Enable_Leaf_Normal;
float2 _Leaf_Tiling;
float2 _Leaf_Offset;
float2 _Leaf_Normal_Tiling;
float2 _Leaf_Normal_Offset;
float2 _Trunk_Normal_Tiling;
float2 _Trunk_Normal_Offset;
float _Leaf_Normal_Strength;
float _Trunk_Normal_Strength;
float _Leaf_Metallic;
float _Leaf_Smoothness;
float4 _Leaf_Ambient_Occlusion_TexelSize;
float4 _Trunk_Ambient_Occlusion_TexelSize;
float _Leaf_Ambient_Occlusion_Intensity;
float _Trunk_Ambient_Occlusion_Intensity;
float _Use_Vertex_Color_Wind;
float _Light_Wind_Y_Strength;
float _Light_Wind_Use_Leaf_Fade;
float _Gale_Bend;
float _Light_Wind_Y_Offset;
float _Use_Color_Noise;
float _Enable_Pulse;
float _Enable_Emission;
float _Use_Global_Weather_Controller;
float2 _Emissive_Mask_Tiling;
float2 _Emissive_Mask_Offset;
float2 _Emissive_Mask_2_Tiling;
float2 _Emissive_Mask_2_Offset;
float _Alpha_Clip_Threshold;
float _Enable_Back_Face_Lighting;
#if defined(OVERLOOK_PAINTED_GRASS)
float4 _PaintMap_ST;
half4 _BaseColor;
half4 _LowColor;
half4 _MidColor;
half4 _HighColor;
half4 _ShadowColor;
half4 _LightColor;
float _PaintScale;
half _PaintStrength;
half _LeafDetail;
half _LightWrap;
half _NormalInfluence;
#endif
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_Leaf_Texture);
SAMPLER(sampler_Leaf_Texture);
TEXTURE2D(_Trunk_Texture);
SAMPLER(sampler_Trunk_Texture);
TEXTURE2D(_Emissive_2_Mask);
SAMPLER(sampler_Emissive_2_Mask);
TEXTURE2D(_Emissive_Mask);
SAMPLER(sampler_Emissive_Mask);
TEXTURE2D(_Trunk_Emissive_Mask);
SAMPLER(sampler_Trunk_Emissive_Mask);
TEXTURE2D(_Emissive_Pulse_Map);
SAMPLER(sampler_Emissive_Pulse_Map);
float3 _WindDirection;
TEXTURE2D(_Trunk_Normal);
SAMPLER(sampler_Trunk_Normal);
TEXTURE2D(_Leaf_Normal);
SAMPLER(sampler_Leaf_Normal);
TEXTURE2D(_Leaf_Ambient_Occlusion);
SAMPLER(sampler_Leaf_Ambient_Occlusion);
TEXTURE2D(_Trunk_Ambient_Occlusion);
SAMPLER(sampler_Trunk_Ambient_Occlusion);
float _GaleStrength;
float _WindIntensity;

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Sine_float(float In, out float Out)
{
    Out = sin(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Rotate_About_Axis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    Rotation = radians(Rotation);
    float s, c;
    sincos(Rotation, s, c);
    Axis = normalize(Axis);
    Out = In * c + cross(Axis, In) * s + Axis * dot(Axis, In) * (1 - c);
}

void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Not_float(float In, out float Out)
{
    Out = !In;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
{
Out = A * B;
}

void Unity_Add_float2(float2 A, float2 B, out float2 Out)
{
    Out = A + B;
}

float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
{
float2 i = floor(uv);
float2 f = frac(uv);
f = f * f * (3.0 - 2.0 * f);
uv = abs(frac(uv) - 0.5);
float2 c0 = i + float2(0.0, 0.0);
float2 c1 = i + float2(1.0, 0.0);
float2 c2 = i + float2(0.0, 1.0);
float2 c3 = i + float2(1.0, 1.0);
float r0; Hash_Tchou_2_1_float(c0, r0);
float r1; Hash_Tchou_2_1_float(c1, r1);
float r2; Hash_Tchou_2_1_float(c2, r2);
float r3; Hash_Tchou_2_1_float(c3, r3);
float bottomOfGrid = lerp(r0, r1, f.x);
float topOfGrid = lerp(r2, r3, f.x);
float t = lerp(bottomOfGrid, topOfGrid, f.y);
return t;
}

void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
{
float freq, amp;
Out = 0.0f;
freq = pow(2.0, float(0));
amp = pow(0.5, float(3-0));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(1));
amp = pow(0.5, float(3-1));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
freq = pow(2.0, float(2));
amp = pow(0.5, float(3-2));
Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
}

void Unity_OneMinus_float(float In, out float Out)
{
    Out = 1 - In;
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Comparison_Greater_float(float A, float B, out float Out)
{
    Out = A > B ? 1 : 0;
}

void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}

void Unity_Saturate_float3(float3 In, out float3 Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

void Unity_Blend_Multiply_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Power_float(float A, float B, out float Out)
{
    Out = pow(A, B);
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = SafeNormalize(float3(A.rg + B.rg, A.b * B.b));
}

void Unity_Negate_float3(float3 In, out float3 Out)
{
    Out = -1 * In;
}

void Unity_Add_float4(float4 A, float4 B, out float4 Out)
{
    Out = A + B;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Sine_float2(float2 In, out float2 Out)
{
    Out = sin(In);
}

void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}

void Unity_Maximum_float3(float3 A, float3 B, out float3 Out)
{
    Out = max(A, B);
}

void Unity_Remap_float3(float3 In, float2 InMinMax, float2 OutMinMax, out float3 Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
float _Property_4d0852c2e98e43b89255fdae06f64e1c_Out_0_Boolean = _Use_Global_Weather_Controller;
float3 _Property_2750abdc933b4c1097d96340613caac4_Out_0_Vector3 = _WindDirection;
float3 _Transform_d362344fdfbf4791a9b1aaf40d4b815b_Out_1_Vector3;
_Transform_d362344fdfbf4791a9b1aaf40d4b815b_Out_1_Vector3 = TransformWorldToObjectDir(_Property_2750abdc933b4c1097d96340613caac4_Out_0_Vector3.xyz, true);
float3 _Vector3_567a57fb9a664e3e99d317c7a4a359d4_Out_0_Vector3 = float3(float(1), float(0), float(0));
float3 _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3;
Unity_Branch_float3(_Property_4d0852c2e98e43b89255fdae06f64e1c_Out_0_Boolean, _Transform_d362344fdfbf4791a9b1aaf40d4b815b_Out_1_Vector3, _Vector3_567a57fb9a664e3e99d317c7a4a359d4_Out_0_Vector3, _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3);
float _Sine_1609290db60d4418ad8b8afebea48352_Out_1_Float;
Unity_Sine_float(IN.TimeParameters.x, _Sine_1609290db60d4418ad8b8afebea48352_Out_1_Float);
float _Multiply_25bfc631a9c34a0ea34919844a7e3c2c_Out_2_Float;
Unity_Multiply_float_float(_Sine_1609290db60d4418ad8b8afebea48352_Out_1_Float, 0.5, _Multiply_25bfc631a9c34a0ea34919844a7e3c2c_Out_2_Float);
float _Add_2638625a29ad44d28b14351ff55b4651_Out_2_Float;
Unity_Add_float(_Multiply_25bfc631a9c34a0ea34919844a7e3c2c_Out_2_Float, float(0.5), _Add_2638625a29ad44d28b14351ff55b4651_Out_2_Float);
float _Property_11f31a226a9343b9932e34cc0fa1b231_Out_0_Boolean = _Use_Global_Weather_Controller;
float _Property_0eae2ee203f64a2494298e6f083c2775_Out_0_Float = _GaleStrength;
float _Float_cff776e8ec9547c0884cdd246646a4a2_Out_0_Float = float(0.2);
float _Branch_5300389596594ea48232616772569449_Out_3_Float;
Unity_Branch_float(_Property_11f31a226a9343b9932e34cc0fa1b231_Out_0_Boolean, _Property_0eae2ee203f64a2494298e6f083c2775_Out_0_Float, _Float_cff776e8ec9547c0884cdd246646a4a2_Out_0_Float, _Branch_5300389596594ea48232616772569449_Out_3_Float);
float _Multiply_f2898be87a55494da019cee36e16abda_Out_2_Float;
Unity_Multiply_float_float(_Branch_5300389596594ea48232616772569449_Out_3_Float, 20, _Multiply_f2898be87a55494da019cee36e16abda_Out_2_Float);
float _Multiply_f4f5083f56194056b4c9e9ae62a6befc_Out_2_Float;
Unity_Multiply_float_float(_Add_2638625a29ad44d28b14351ff55b4651_Out_2_Float, _Multiply_f2898be87a55494da019cee36e16abda_Out_2_Float, _Multiply_f4f5083f56194056b4c9e9ae62a6befc_Out_2_Float);
float _Multiply_db22755433114b3899d2d07bc4245e1a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5300389596594ea48232616772569449_Out_3_Float, 50, _Multiply_db22755433114b3899d2d07bc4245e1a_Out_2_Float);
float _Add_e57faebf640e4caaba853e90880b7510_Out_2_Float;
Unity_Add_float(_Multiply_f4f5083f56194056b4c9e9ae62a6befc_Out_2_Float, _Multiply_db22755433114b3899d2d07bc4245e1a_Out_2_Float, _Add_e57faebf640e4caaba853e90880b7510_Out_2_Float);
float3 _RotateAboutAxis_fd94b453937c4a1fb53c74ef8b22b997_Out_3_Vector3;
Unity_Rotate_About_Axis_Degrees_float(IN.ObjectSpacePosition, _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3, _Add_e57faebf640e4caaba853e90880b7510_Out_2_Float, _RotateAboutAxis_fd94b453937c4a1fb53c74ef8b22b997_Out_3_Vector3);
float _Split_7617ce0839e641c28b2e6757ae592ad3_R_1_Float = IN.VertexColor[0];
float _Split_7617ce0839e641c28b2e6757ae592ad3_G_2_Float = IN.VertexColor[1];
float _Split_7617ce0839e641c28b2e6757ae592ad3_B_3_Float = IN.VertexColor[2];
float _Split_7617ce0839e641c28b2e6757ae592ad3_A_4_Float = IN.VertexColor[3];
float _Property_bfe872968fa14e50964a78a3f41e7ff8_Out_0_Float = _Gale_Bend;
float _Multiply_da24289f2f1d4e87896678d934046199_Out_2_Float;
Unity_Multiply_float_float(_Split_7617ce0839e641c28b2e6757ae592ad3_R_1_Float, _Property_bfe872968fa14e50964a78a3f41e7ff8_Out_0_Float, _Multiply_da24289f2f1d4e87896678d934046199_Out_2_Float);
float3 _Lerp_f9e198d0f7f84a2aacccbfaa387f100c_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _RotateAboutAxis_fd94b453937c4a1fb53c74ef8b22b997_Out_3_Vector3, (_Multiply_da24289f2f1d4e87896678d934046199_Out_2_Float.xxx), _Lerp_f9e198d0f7f84a2aacccbfaa387f100c_Out_3_Vector3);
float _Property_719013ad16c94a5ab18c6a7c65faad34_Out_0_Boolean = _Enable_Wind_Twist;
float _Not_cd64b799c70d4994b5b8eec450812b06_Out_1_Boolean;
Unity_Not_float(_Property_719013ad16c94a5ab18c6a7c65faad34_Out_0_Boolean, _Not_cd64b799c70d4994b5b8eec450812b06_Out_1_Boolean);
float _Property_cff689f0f9f8434a84fa51e2e212e80f_Out_0_Boolean = _Enable_Strong_Wind;
float _Property_69ce91e29bf94fcfacf9ca1bf83b37ac_Out_0_Float = _Strong_Wind_Frequency;
float _Add_1f1bba3da67d431a909ee963eb2c9909_Out_2_Float;
Unity_Add_float(_Branch_5300389596594ea48232616772569449_Out_3_Float, float(2), _Add_1f1bba3da67d431a909ee963eb2c9909_Out_2_Float);
float _Float_5f2d8a4b153c45f9b417aaaffda27ca9_Out_0_Float = float(1.5);
float _Multiply_98a7ae3df0284405a75f706234b199d8_Out_2_Float;
Unity_Multiply_float_float(_Add_1f1bba3da67d431a909ee963eb2c9909_Out_2_Float, _Float_5f2d8a4b153c45f9b417aaaffda27ca9_Out_0_Float, _Multiply_98a7ae3df0284405a75f706234b199d8_Out_2_Float);
float _Multiply_840785dd9fbb40d1bd4d4ba6c5e60064_Out_2_Float;
Unity_Multiply_float_float(_Property_69ce91e29bf94fcfacf9ca1bf83b37ac_Out_0_Float, _Multiply_98a7ae3df0284405a75f706234b199d8_Out_2_Float, _Multiply_840785dd9fbb40d1bd4d4ba6c5e60064_Out_2_Float);
float _Split_874f505d36e74eb7af199bd40ae4c622_R_1_Float = SHADERGRAPH_OBJECT_POSITION[0];
float _Split_874f505d36e74eb7af199bd40ae4c622_G_2_Float = SHADERGRAPH_OBJECT_POSITION[1];
float _Split_874f505d36e74eb7af199bd40ae4c622_B_3_Float = SHADERGRAPH_OBJECT_POSITION[2];
float _Split_874f505d36e74eb7af199bd40ae4c622_A_4_Float = 0;
float _Add_9fa2b51a795c4bbaa628f1715640e535_Out_2_Float;
Unity_Add_float(_Split_874f505d36e74eb7af199bd40ae4c622_R_1_Float, _Split_874f505d36e74eb7af199bd40ae4c622_B_3_Float, _Add_9fa2b51a795c4bbaa628f1715640e535_Out_2_Float);
float _Add_25a3539d4fce4b98b996948116d3842f_Out_2_Float;
Unity_Add_float(IN.TimeParameters.x, _Add_9fa2b51a795c4bbaa628f1715640e535_Out_2_Float, _Add_25a3539d4fce4b98b996948116d3842f_Out_2_Float);
float _Multiply_ec4065689fe04ad8bca92818b45db39d_Out_2_Float;
Unity_Multiply_float_float(_Multiply_840785dd9fbb40d1bd4d4ba6c5e60064_Out_2_Float, _Add_25a3539d4fce4b98b996948116d3842f_Out_2_Float, _Multiply_ec4065689fe04ad8bca92818b45db39d_Out_2_Float);
float _Sine_52cfa2b96db24119a6e7273f618ee68e_Out_1_Float;
Unity_Sine_float(_Multiply_ec4065689fe04ad8bca92818b45db39d_Out_2_Float, _Sine_52cfa2b96db24119a6e7273f618ee68e_Out_1_Float);
float _Property_a113f2a62b9348c68a921c534f5927ac_Out_0_Float = _Strong_Wind_Strength;
float _Multiply_c4d5f8b7d978421d9d9f0bc023974a9b_Out_2_Float;
Unity_Multiply_float_float(_Property_a113f2a62b9348c68a921c534f5927ac_Out_0_Float, 5, _Multiply_c4d5f8b7d978421d9d9f0bc023974a9b_Out_2_Float);
float _Multiply_b202a027338b45648e4b61b8dc32ffaa_Out_2_Float;
Unity_Multiply_float_float(_Sine_52cfa2b96db24119a6e7273f618ee68e_Out_1_Float, _Multiply_c4d5f8b7d978421d9d9f0bc023974a9b_Out_2_Float, _Multiply_b202a027338b45648e4b61b8dc32ffaa_Out_2_Float);
float3 _Multiply_61798c60e2924b99a9f4269b8618ebb8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_b202a027338b45648e4b61b8dc32ffaa_Out_2_Float.xxx), _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3, _Multiply_61798c60e2924b99a9f4269b8618ebb8_Out_2_Vector3);
float _Float_228d93b55e6e40209d438717ff406210_Out_0_Float = float(0);
float3 _Branch_38f5a68a18aa4991ac46d7a2d4827535_Out_3_Vector3;
Unity_Branch_float3(_Property_cff689f0f9f8434a84fa51e2e212e80f_Out_0_Boolean, _Multiply_61798c60e2924b99a9f4269b8618ebb8_Out_2_Vector3, (_Float_228d93b55e6e40209d438717ff406210_Out_0_Float.xxx), _Branch_38f5a68a18aa4991ac46d7a2d4827535_Out_3_Vector3);
float3 _Add_8542f402de1845678f84c8544a908058_Out_2_Vector3;
Unity_Add_float3(_Branch_38f5a68a18aa4991ac46d7a2d4827535_Out_3_Vector3, _Lerp_f9e198d0f7f84a2aacccbfaa387f100c_Out_3_Vector3, _Add_8542f402de1845678f84c8544a908058_Out_2_Vector3);
float3 _Vector3_544ea0df4c174350b2f417a0fecb76a8_Out_0_Vector3 = float3(float(0), float(1), float(0));
float _Property_b6bcf8df8f4a4f0fa8f238003e3d87e8_Out_0_Float = _Strong_Wind_Frequency;
float _Multiply_2b25f005384a4ec193130bb0b3d5c88f_Out_2_Float;
Unity_Multiply_float_float(_Add_25a3539d4fce4b98b996948116d3842f_Out_2_Float, _Property_b6bcf8df8f4a4f0fa8f238003e3d87e8_Out_0_Float, _Multiply_2b25f005384a4ec193130bb0b3d5c88f_Out_2_Float);
float _Sine_4efe9968d26347789641728dbd8e753b_Out_1_Float;
Unity_Sine_float(_Multiply_2b25f005384a4ec193130bb0b3d5c88f_Out_2_Float, _Sine_4efe9968d26347789641728dbd8e753b_Out_1_Float);
float _Property_4253e844531b483ab97fb6fc5611c7f9_Out_0_Float = _Wind_Twist_Strength;
float _Multiply_b16103aa3c13498ea1030d44b7d68820_Out_2_Float;
Unity_Multiply_float_float(_Property_4253e844531b483ab97fb6fc5611c7f9_Out_0_Float, 40, _Multiply_b16103aa3c13498ea1030d44b7d68820_Out_2_Float);
float _Multiply_656055b111414cc8a2a5e731083e0689_Out_2_Float;
Unity_Multiply_float_float(_Sine_4efe9968d26347789641728dbd8e753b_Out_1_Float, _Multiply_b16103aa3c13498ea1030d44b7d68820_Out_2_Float, _Multiply_656055b111414cc8a2a5e731083e0689_Out_2_Float);
float3 _RotateAboutAxis_7492bd07d5dc4a3d974f5698d1c233dd_Out_3_Vector3;
Unity_Rotate_About_Axis_Degrees_float(_Add_8542f402de1845678f84c8544a908058_Out_2_Vector3, _Vector3_544ea0df4c174350b2f417a0fecb76a8_Out_0_Vector3, _Multiply_656055b111414cc8a2a5e731083e0689_Out_2_Float, _RotateAboutAxis_7492bd07d5dc4a3d974f5698d1c233dd_Out_3_Vector3);
float3 _Branch_1a801a10383f49589d32c1fa71887d15_Out_3_Vector3;
Unity_Branch_float3(_Not_cd64b799c70d4994b5b8eec450812b06_Out_1_Boolean, _Add_8542f402de1845678f84c8544a908058_Out_2_Vector3, _RotateAboutAxis_7492bd07d5dc4a3d974f5698d1c233dd_Out_3_Vector3, _Branch_1a801a10383f49589d32c1fa71887d15_Out_3_Vector3);
float _Property_daa1e20f7e06450f9006f6b09e422f20_Out_0_Boolean = _Enable_Light_Wind;
float _Property_beb2291579fe40cea9b0232c675f4e24_Out_0_Boolean = _Light_Wind_Use_Leaf_Fade;
float _Split_06ef18a0ac2b4ba996df5cc24c828131_R_1_Float = IN.VertexColor[0];
float _Split_06ef18a0ac2b4ba996df5cc24c828131_G_2_Float = IN.VertexColor[1];
float _Split_06ef18a0ac2b4ba996df5cc24c828131_B_3_Float = IN.VertexColor[2];
float _Split_06ef18a0ac2b4ba996df5cc24c828131_A_4_Float = IN.VertexColor[3];
float _Branch_d9649312322a48cc8a095d5f525ecb97_Out_3_Float;
Unity_Branch_float(_Property_beb2291579fe40cea9b0232c675f4e24_Out_0_Boolean, _Split_06ef18a0ac2b4ba996df5cc24c828131_G_2_Float, _Split_06ef18a0ac2b4ba996df5cc24c828131_B_3_Float, _Branch_d9649312322a48cc8a095d5f525ecb97_Out_3_Float);
float _Remap_b9f84097492446ed94ab6f7d1cefee4f_Out_3_Float;
Unity_Remap_float(_Branch_5300389596594ea48232616772569449_Out_3_Float, float2 (0, 1), float2 (0.25, 1), _Remap_b9f84097492446ed94ab6f7d1cefee4f_Out_3_Float);
float _Multiply_c20b258939f0476b9bf6f63505d7d100_Out_2_Float;
Unity_Multiply_float_float(_Remap_b9f84097492446ed94ab6f7d1cefee4f_Out_3_Float, 0.2, _Multiply_c20b258939f0476b9bf6f63505d7d100_Out_2_Float);
float _Multiply_5cf3a854578849b8ae133d7e6af736b8_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Multiply_c20b258939f0476b9bf6f63505d7d100_Out_2_Float, _Multiply_5cf3a854578849b8ae133d7e6af736b8_Out_2_Float);
float _Split_760b90adc3a94c548e42317800a998ec_R_1_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[0];
float _Split_760b90adc3a94c548e42317800a998ec_G_2_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[1];
float _Split_760b90adc3a94c548e42317800a998ec_B_3_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[2];
float _Split_760b90adc3a94c548e42317800a998ec_A_4_Float = 0;
float2 _Vector2_aa46362dfd4f4304b197404e106197b1_Out_0_Vector2 = float2(_Split_760b90adc3a94c548e42317800a998ec_R_1_Float, _Split_760b90adc3a94c548e42317800a998ec_B_3_Float);
float2 _Multiply_48c16537a36f47b29c46bc5f495eb395_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_aa46362dfd4f4304b197404e106197b1_Out_0_Vector2, float2(-1, -1), _Multiply_48c16537a36f47b29c46bc5f495eb395_Out_2_Vector2);
float2 _Multiply_56706b67332142ef83325ea03e23ad09_Out_2_Vector2;
Unity_Multiply_float2_float2((_Multiply_5cf3a854578849b8ae133d7e6af736b8_Out_2_Float.xx), _Multiply_48c16537a36f47b29c46bc5f495eb395_Out_2_Vector2, _Multiply_56706b67332142ef83325ea03e23ad09_Out_2_Vector2);
float _Split_8ec5a0620e474a64893bc1242ecd26d9_R_1_Float = IN.WorldSpacePosition[0];
float _Split_8ec5a0620e474a64893bc1242ecd26d9_G_2_Float = IN.WorldSpacePosition[1];
float _Split_8ec5a0620e474a64893bc1242ecd26d9_B_3_Float = IN.WorldSpacePosition[2];
float _Split_8ec5a0620e474a64893bc1242ecd26d9_A_4_Float = 0;
float2 _Vector2_4dea069a486745edbb44d3e3330a16ae_Out_0_Vector2 = float2(_Split_8ec5a0620e474a64893bc1242ecd26d9_R_1_Float, _Split_8ec5a0620e474a64893bc1242ecd26d9_B_3_Float);
float2 _Vector2_de98b065d3b14228a88cbda0d05bf470_Out_0_Vector2 = float2(float(0.01), float(0.01));
float2 _Multiply_27d1c8dd3b7a4b7c9d51ca3a0ecbfbc3_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_4dea069a486745edbb44d3e3330a16ae_Out_0_Vector2, _Vector2_de98b065d3b14228a88cbda0d05bf470_Out_0_Vector2, _Multiply_27d1c8dd3b7a4b7c9d51ca3a0ecbfbc3_Out_2_Vector2);
float2 _Add_66263a9fe81144588ec02abd6ece65fd_Out_2_Vector2;
Unity_Add_float2(_Multiply_56706b67332142ef83325ea03e23ad09_Out_2_Vector2, _Multiply_27d1c8dd3b7a4b7c9d51ca3a0ecbfbc3_Out_2_Vector2, _Add_66263a9fe81144588ec02abd6ece65fd_Out_2_Vector2);
float _SimpleNoise_1b75d6f96bca4f0994e2041684580712_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_Add_66263a9fe81144588ec02abd6ece65fd_Out_2_Vector2, float(50), _SimpleNoise_1b75d6f96bca4f0994e2041684580712_Out_2_Float);
float _Property_4397a2e2677046de82e47553d4a7c42e_Out_0_Float = _Light_Wind_Y_Offset;
float _OneMinus_a341eaa0d130407fb5c641bd5f0e0af4_Out_1_Float;
Unity_OneMinus_float(_Property_4397a2e2677046de82e47553d4a7c42e_Out_0_Float, _OneMinus_a341eaa0d130407fb5c641bd5f0e0af4_Out_1_Float);
float _Multiply_d71f952a2b84481ea25b7ff1cbd1e5af_Out_2_Float;
Unity_Multiply_float_float(_OneMinus_a341eaa0d130407fb5c641bd5f0e0af4_Out_1_Float, 0.5, _Multiply_d71f952a2b84481ea25b7ff1cbd1e5af_Out_2_Float);
float _Subtract_219053c397dd4886ad90b9281c523216_Out_2_Float;
Unity_Subtract_float(_SimpleNoise_1b75d6f96bca4f0994e2041684580712_Out_2_Float, _Multiply_d71f952a2b84481ea25b7ff1cbd1e5af_Out_2_Float, _Subtract_219053c397dd4886ad90b9281c523216_Out_2_Float);
float _Multiply_548b12ffef6c4a25a38d6348739d8f23_Out_2_Float;
Unity_Multiply_float_float(_Subtract_219053c397dd4886ad90b9281c523216_Out_2_Float, 2, _Multiply_548b12ffef6c4a25a38d6348739d8f23_Out_2_Float);
float _Property_226d030ea15244c78fcc2d201051ee39_Out_0_Float = _Light_Wind_Y_Strength;
float3 _Vector3_41a6c7c0ffdc4cea960a1eb114c4e611_Out_0_Vector3 = float3(float(1), _Property_226d030ea15244c78fcc2d201051ee39_Out_0_Float, float(1));
float3 _Multiply_c2b568aa1dd54bdbb485440ec3b1f499_Out_2_Vector3;
Unity_Multiply_float3_float3(IN.WorldSpaceNormal, _Vector3_41a6c7c0ffdc4cea960a1eb114c4e611_Out_0_Vector3, _Multiply_c2b568aa1dd54bdbb485440ec3b1f499_Out_2_Vector3);
float3 _Multiply_4260489fd7784662aa1ccb9f74e646bc_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_548b12ffef6c4a25a38d6348739d8f23_Out_2_Float.xxx), _Multiply_c2b568aa1dd54bdbb485440ec3b1f499_Out_2_Vector3, _Multiply_4260489fd7784662aa1ccb9f74e646bc_Out_2_Vector3);
float _Property_2ec622c2e73a48f19bb87b961716af23_Out_0_Float = _Light_Wind_Strength;
float _Multiply_f09079affd854985bf8213934c79d7cb_Out_2_Float;
Unity_Multiply_float_float(_Property_2ec622c2e73a48f19bb87b961716af23_Out_0_Float, 3, _Multiply_f09079affd854985bf8213934c79d7cb_Out_2_Float);
float3 _Multiply_be0d5baf1edf4a9c8093580ecd2cb54d_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_4260489fd7784662aa1ccb9f74e646bc_Out_2_Vector3, (_Multiply_f09079affd854985bf8213934c79d7cb_Out_2_Float.xxx), _Multiply_be0d5baf1edf4a9c8093580ecd2cb54d_Out_2_Vector3);
float3 _Multiply_212e70cee1764e3697cbb18c5792ef13_Out_2_Vector3;
Unity_Multiply_float3_float3((_Branch_d9649312322a48cc8a095d5f525ecb97_Out_3_Float.xxx), _Multiply_be0d5baf1edf4a9c8093580ecd2cb54d_Out_2_Vector3, _Multiply_212e70cee1764e3697cbb18c5792ef13_Out_2_Vector3);
float _Float_640a1fa514f243ddbc0ad58d12a6e414_Out_0_Float = float(0);
float3 _Branch_2d1a61023bd849d1a6339f180c674a96_Out_3_Vector3;
Unity_Branch_float3(_Property_daa1e20f7e06450f9006f6b09e422f20_Out_0_Boolean, _Multiply_212e70cee1764e3697cbb18c5792ef13_Out_2_Vector3, (_Float_640a1fa514f243ddbc0ad58d12a6e414_Out_0_Float.xxx), _Branch_2d1a61023bd849d1a6339f180c674a96_Out_3_Vector3);
float3 _Add_5155c0fc773b410b8afa0f11f416a216_Out_2_Vector3;
Unity_Add_float3(_Branch_1a801a10383f49589d32c1fa71887d15_Out_3_Vector3, _Branch_2d1a61023bd849d1a6339f180c674a96_Out_3_Vector3, _Add_5155c0fc773b410b8afa0f11f416a216_Out_2_Vector3);
float _Property_9acc275e93fc4184b08807ee975645e6_Out_0_Boolean = _Enable_Breeze;
float _Split_faa474300522404e97d9e2a1ed8ff587_R_1_Float = IN.VertexColor[0];
float _Split_faa474300522404e97d9e2a1ed8ff587_G_2_Float = IN.VertexColor[1];
float _Split_faa474300522404e97d9e2a1ed8ff587_B_3_Float = IN.VertexColor[2];
float _Split_faa474300522404e97d9e2a1ed8ff587_A_4_Float = IN.VertexColor[3];
float _Multiply_b2a776ea33254853aac05b682acfefd9_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, 0.01, _Multiply_b2a776ea33254853aac05b682acfefd9_Out_2_Float);
float _Split_b3338aed39b248e5961f63d03387502a_R_1_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[0];
float _Split_b3338aed39b248e5961f63d03387502a_G_2_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[1];
float _Split_b3338aed39b248e5961f63d03387502a_B_3_Float = _Branch_54d69d13e0384fd0bc06e9694fa2eb31_Out_3_Vector3[2];
float _Split_b3338aed39b248e5961f63d03387502a_A_4_Float = 0;
float2 _Vector2_9baee0069b3346c98461d687056c2a2d_Out_0_Vector2 = float2(_Split_b3338aed39b248e5961f63d03387502a_R_1_Float, _Split_b3338aed39b248e5961f63d03387502a_B_3_Float);
float2 _Multiply_b14da6676c6c4d0ab686c007d0adaf95_Out_2_Vector2;
Unity_Multiply_float2_float2((_Multiply_b2a776ea33254853aac05b682acfefd9_Out_2_Float.xx), _Vector2_9baee0069b3346c98461d687056c2a2d_Out_0_Vector2, _Multiply_b14da6676c6c4d0ab686c007d0adaf95_Out_2_Vector2);
float _Split_92bd11b5b0e445db83ec0bb78c9f5bab_R_1_Float = IN.WorldSpacePosition[0];
float _Split_92bd11b5b0e445db83ec0bb78c9f5bab_G_2_Float = IN.WorldSpacePosition[1];
float _Split_92bd11b5b0e445db83ec0bb78c9f5bab_B_3_Float = IN.WorldSpacePosition[2];
float _Split_92bd11b5b0e445db83ec0bb78c9f5bab_A_4_Float = 0;
float2 _Vector2_a80d1cd203bb4ed2b0368ae19ea8ec8a_Out_0_Vector2 = float2(_Split_92bd11b5b0e445db83ec0bb78c9f5bab_R_1_Float, _Split_92bd11b5b0e445db83ec0bb78c9f5bab_B_3_Float);
float2 _Vector2_0c7ef599d5a84959ade85607166a73cf_Out_0_Vector2 = float2(float(0.01), float(0.01));
float2 _Multiply_1cf83237912f46b5a443bc3c955984d7_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_a80d1cd203bb4ed2b0368ae19ea8ec8a_Out_0_Vector2, _Vector2_0c7ef599d5a84959ade85607166a73cf_Out_0_Vector2, _Multiply_1cf83237912f46b5a443bc3c955984d7_Out_2_Vector2);
float2 _Add_fc7ddde490274d018cc255027dbaed64_Out_2_Vector2;
Unity_Add_float2(_Multiply_b14da6676c6c4d0ab686c007d0adaf95_Out_2_Vector2, _Multiply_1cf83237912f46b5a443bc3c955984d7_Out_2_Vector2, _Add_fc7ddde490274d018cc255027dbaed64_Out_2_Vector2);
float _SimpleNoise_3c152a3422154039a9bc9ae6281e9685_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_Add_fc7ddde490274d018cc255027dbaed64_Out_2_Vector2, float(1000), _SimpleNoise_3c152a3422154039a9bc9ae6281e9685_Out_2_Float);
float _Subtract_b75d828ebffe4e81bf6c96bdf27abe01_Out_2_Float;
Unity_Subtract_float(_SimpleNoise_3c152a3422154039a9bc9ae6281e9685_Out_2_Float, float(0.5), _Subtract_b75d828ebffe4e81bf6c96bdf27abe01_Out_2_Float);
float _Multiply_19eb96c7865d4b1bb2f6fac3d1a6f491_Out_2_Float;
Unity_Multiply_float_float(_Subtract_b75d828ebffe4e81bf6c96bdf27abe01_Out_2_Float, 2, _Multiply_19eb96c7865d4b1bb2f6fac3d1a6f491_Out_2_Float);
float3 _Multiply_684173e85cfe4097a575d822b77e6e6b_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_19eb96c7865d4b1bb2f6fac3d1a6f491_Out_2_Float.xxx), IN.WorldSpaceNormal, _Multiply_684173e85cfe4097a575d822b77e6e6b_Out_2_Vector3);
float _Property_abca9771e4cc4d1da58e26078da0ed85_Out_0_Float = _Breeze_Strength;
float3 _Vector3_557ae4eabe834a86b14908521895da77_Out_0_Vector3 = float3(_Property_abca9771e4cc4d1da58e26078da0ed85_Out_0_Float, float(0), _Property_abca9771e4cc4d1da58e26078da0ed85_Out_0_Float);
float _Remap_0faca8aabde544b4ab522768ce3be74f_Out_3_Float;
Unity_Remap_float(_Branch_5300389596594ea48232616772569449_Out_3_Float, float2 (0, 1), float2 (2, 4), _Remap_0faca8aabde544b4ab522768ce3be74f_Out_3_Float);
float _Float_2b5e9ef388614ff690dd764b7e4a29f1_Out_0_Float = _Remap_0faca8aabde544b4ab522768ce3be74f_Out_3_Float;
float3 _Multiply_6c74f9fe1f294277bdbcaf90f36f7aeb_Out_2_Vector3;
Unity_Multiply_float3_float3(_Vector3_557ae4eabe834a86b14908521895da77_Out_0_Vector3, (_Float_2b5e9ef388614ff690dd764b7e4a29f1_Out_0_Float.xxx), _Multiply_6c74f9fe1f294277bdbcaf90f36f7aeb_Out_2_Vector3);
float3 _Multiply_d4f5da9d52df463fb4b5cd7549f37773_Out_2_Vector3;
Unity_Multiply_float3_float3(_Multiply_684173e85cfe4097a575d822b77e6e6b_Out_2_Vector3, _Multiply_6c74f9fe1f294277bdbcaf90f36f7aeb_Out_2_Vector3, _Multiply_d4f5da9d52df463fb4b5cd7549f37773_Out_2_Vector3);
float3 _Multiply_3cbf5ecdb5504c6e87f6a111e1d7d82a_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_faa474300522404e97d9e2a1ed8ff587_G_2_Float.xxx), _Multiply_d4f5da9d52df463fb4b5cd7549f37773_Out_2_Vector3, _Multiply_3cbf5ecdb5504c6e87f6a111e1d7d82a_Out_2_Vector3);
float _Float_51436c6ba0884585ab3a195c90893bb0_Out_0_Float = float(0);
float3 _Branch_a0fa339fe05d4daf8fdb89a833ce0727_Out_3_Vector3;
Unity_Branch_float3(_Property_9acc275e93fc4184b08807ee975645e6_Out_0_Boolean, _Multiply_3cbf5ecdb5504c6e87f6a111e1d7d82a_Out_2_Vector3, (_Float_51436c6ba0884585ab3a195c90893bb0_Out_0_Float.xxx), _Branch_a0fa339fe05d4daf8fdb89a833ce0727_Out_3_Vector3);
float3 _Add_21a74c649f6d47cdb4ebd79a67220937_Out_2_Vector3;
Unity_Add_float3(_Add_5155c0fc773b410b8afa0f11f416a216_Out_2_Vector3, _Branch_a0fa339fe05d4daf8fdb89a833ce0727_Out_3_Vector3, _Add_21a74c649f6d47cdb4ebd79a67220937_Out_2_Vector3);
float _Property_79e027cc362d4044b7640363cecc4f21_Out_0_Boolean = _Use_Vertex_Color_Wind;
float _Split_978f8a7610ed4f44b9e1ac1f87829a83_R_1_Float = IN.VertexColor[0];
float _Split_978f8a7610ed4f44b9e1ac1f87829a83_G_2_Float = IN.VertexColor[1];
float _Split_978f8a7610ed4f44b9e1ac1f87829a83_B_3_Float = IN.VertexColor[2];
float _Split_978f8a7610ed4f44b9e1ac1f87829a83_A_4_Float = IN.VertexColor[3];
float _Split_a6868c3d7d8a4cb38fd9a95c7b3f31ea_R_1_Float = IN.ObjectSpacePosition[0];
float _Split_a6868c3d7d8a4cb38fd9a95c7b3f31ea_G_2_Float = IN.ObjectSpacePosition[1];
float _Split_a6868c3d7d8a4cb38fd9a95c7b3f31ea_B_3_Float = IN.ObjectSpacePosition[2];
float _Split_a6868c3d7d8a4cb38fd9a95c7b3f31ea_A_4_Float = 0;
float _Multiply_a62c0d3820fd4304b1b272d911ace2be_Out_2_Float;
Unity_Multiply_float_float(_Split_a6868c3d7d8a4cb38fd9a95c7b3f31ea_G_2_Float, 0.5, _Multiply_a62c0d3820fd4304b1b272d911ace2be_Out_2_Float);
float _Branch_bd3b72e30719491292ac07e6e7150175_Out_3_Float;
Unity_Branch_float(_Property_79e027cc362d4044b7640363cecc4f21_Out_0_Boolean, _Split_978f8a7610ed4f44b9e1ac1f87829a83_R_1_Float, _Multiply_a62c0d3820fd4304b1b272d911ace2be_Out_2_Float, _Branch_bd3b72e30719491292ac07e6e7150175_Out_3_Float);
float3 _Lerp_d911a0172ce34f448002d9029a5c529f_Out_3_Vector3;
Unity_Lerp_float3(_Lerp_f9e198d0f7f84a2aacccbfaa387f100c_Out_3_Vector3, _Add_21a74c649f6d47cdb4ebd79a67220937_Out_2_Vector3, (_Branch_bd3b72e30719491292ac07e6e7150175_Out_3_Float.xxx), _Lerp_d911a0172ce34f448002d9029a5c529f_Out_3_Vector3);
float _Property_ec18cc6d656348b3b6ea911e392c0398_Out_0_Boolean = _Use_Global_Weather_Controller;
float _Property_6c30d0e090814936acf0ddc08febf9d0_Out_0_Float = _WindIntensity;
float _Saturate_4978f2ef05b24862b49b0a822d526de8_Out_1_Float;
Unity_Saturate_float(_Property_6c30d0e090814936acf0ddc08febf9d0_Out_0_Float, _Saturate_4978f2ef05b24862b49b0a822d526de8_Out_1_Float);
float _Float_58477ba82225429cab3d87d6f7c57ae9_Out_0_Float = float(0.2);
float _Branch_996a9eb2f34145ada29ab2e0ca3a40ba_Out_3_Float;
Unity_Branch_float(_Property_ec18cc6d656348b3b6ea911e392c0398_Out_0_Boolean, _Saturate_4978f2ef05b24862b49b0a822d526de8_Out_1_Float, _Float_58477ba82225429cab3d87d6f7c57ae9_Out_0_Float, _Branch_996a9eb2f34145ada29ab2e0ca3a40ba_Out_3_Float);
float3 _Lerp_8d55983b523947f6a1e28deb6130c4c2_Out_3_Vector3;
Unity_Lerp_float3(IN.ObjectSpacePosition, _Lerp_d911a0172ce34f448002d9029a5c529f_Out_3_Vector3, (_Branch_996a9eb2f34145ada29ab2e0ca3a40ba_Out_3_Float.xxx), _Lerp_8d55983b523947f6a1e28deb6130c4c2_Out_3_Vector3);
description.Position = _Lerp_8d55983b523947f6a1e28deb6130c4c2_Out_3_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

struct SurfaceDescription
{
float3 BaseColor;
float3 NormalTS;
float3 Emission;
float Metallic;
float3 Specular;
float Smoothness;
float Occlusion;
float Alpha;
float AlphaClipThreshold;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
float _Split_22e18d277ccc498f99daa6ecf0cca72a_R_1_Float = IN.VertexColor[0];
float _Split_22e18d277ccc498f99daa6ecf0cca72a_G_2_Float = IN.VertexColor[1];
float _Split_22e18d277ccc498f99daa6ecf0cca72a_B_3_Float = IN.VertexColor[2];
float _Split_22e18d277ccc498f99daa6ecf0cca72a_A_4_Float = IN.VertexColor[3];
float _Float_2e7a3e6b410b401aaef6c947492148db_Out_0_Float = float(0.5);
float _Comparison_6ac47c1f48a14a2f978d1bf37e8bdb01_Out_2_Boolean;
Unity_Comparison_Greater_float(_Split_22e18d277ccc498f99daa6ecf0cca72a_B_3_Float, _Float_2e7a3e6b410b401aaef6c947492148db_Out_0_Float, _Comparison_6ac47c1f48a14a2f978d1bf37e8bdb01_Out_2_Boolean);
float _Property_08c668170b28492f94cc488fdbbc4177_Out_0_Boolean = _Leaf_Flat_Color;
float _Property_94cc0a68d8e64a0c94b8da2363a001fa_Out_0_Boolean = _Use_Color_Noise;
float4 _Property_5d6237bf0e5145cdaee005ec1972388c_Out_0_Vector4 = _Leaf_Noise_Color;
float4 _Property_90e1572135294114aa8640a3a78cb1d1_Out_0_Vector4 = _Leaf_Base_Color;
float _Float_ec1029a7787647b1bf4866df25b6e3d1_Out_0_Float = float(0.5);
float3 _Multiply_c5c0cf090e3a456cb5f7be73b70cedda_Out_2_Vector3;
Unity_Multiply_float3_float3(IN.WorldSpacePosition, (_Float_ec1029a7787647b1bf4866df25b6e3d1_Out_0_Float.xxx), _Multiply_c5c0cf090e3a456cb5f7be73b70cedda_Out_2_Vector3);
float _Split_93c5c339d60849619a713324976ee936_R_1_Float = _Multiply_c5c0cf090e3a456cb5f7be73b70cedda_Out_2_Vector3[0];
float _Split_93c5c339d60849619a713324976ee936_G_2_Float = _Multiply_c5c0cf090e3a456cb5f7be73b70cedda_Out_2_Vector3[1];
float _Split_93c5c339d60849619a713324976ee936_B_3_Float = _Multiply_c5c0cf090e3a456cb5f7be73b70cedda_Out_2_Vector3[2];
float _Split_93c5c339d60849619a713324976ee936_A_4_Float = 0;
float2 _Vector2_69431d056d354bb49f49c94e9dc8803f_Out_0_Vector2 = float2(_Split_93c5c339d60849619a713324976ee936_R_1_Float, _Split_93c5c339d60849619a713324976ee936_B_3_Float);
float _Property_bb5764cd02a443ddb632644371f6ef77_Out_0_Float = _Color_Noise_Small_Freq;
float _SimpleNoise_027023bded644f3e9ebcdb3ce9224494_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_Vector2_69431d056d354bb49f49c94e9dc8803f_Out_0_Vector2, _Property_bb5764cd02a443ddb632644371f6ef77_Out_0_Float, _SimpleNoise_027023bded644f3e9ebcdb3ce9224494_Out_2_Float);
float3 _Contrast_7a64ff18fe5a41b28457e15b2b0675de_Out_2_Vector3;
Unity_Contrast_float((_SimpleNoise_027023bded644f3e9ebcdb3ce9224494_Out_2_Float.xxx), float(2), _Contrast_7a64ff18fe5a41b28457e15b2b0675de_Out_2_Vector3);
float3 _Saturate_7a59961901d14871972e0be7bc25f1fe_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_7a64ff18fe5a41b28457e15b2b0675de_Out_2_Vector3, _Saturate_7a59961901d14871972e0be7bc25f1fe_Out_1_Vector3);
float _Split_21a7b352b7e84916b21335e22b072643_R_1_Float = _Saturate_7a59961901d14871972e0be7bc25f1fe_Out_1_Vector3[0];
float _Split_21a7b352b7e84916b21335e22b072643_G_2_Float = _Saturate_7a59961901d14871972e0be7bc25f1fe_Out_1_Vector3[1];
float _Split_21a7b352b7e84916b21335e22b072643_B_3_Float = _Saturate_7a59961901d14871972e0be7bc25f1fe_Out_1_Vector3[2];
float _Split_21a7b352b7e84916b21335e22b072643_A_4_Float = 0;
float4 _Lerp_a2debaae9e4145209ce431cc718371ba_Out_3_Vector4;
Unity_Lerp_float4(_Property_5d6237bf0e5145cdaee005ec1972388c_Out_0_Vector4, _Property_90e1572135294114aa8640a3a78cb1d1_Out_0_Vector4, (_Split_21a7b352b7e84916b21335e22b072643_R_1_Float.xxxx), _Lerp_a2debaae9e4145209ce431cc718371ba_Out_3_Vector4);
float4 _Property_121799f07d3c4b9e842302c43b821180_Out_0_Vector4 = _Leaf_Noise_Large_Color;
float _Float_fb279a8b97264d8aba813a942d0c41bc_Out_0_Float = float(0.5);
float3 _Multiply_460eba5b3c8c46b0b316127da0742a66_Out_2_Vector3;
Unity_Multiply_float3_float3(IN.WorldSpacePosition, (_Float_fb279a8b97264d8aba813a942d0c41bc_Out_0_Float.xxx), _Multiply_460eba5b3c8c46b0b316127da0742a66_Out_2_Vector3);
float _Split_3a5da5502be344c491dbfc1a97b4bde7_R_1_Float = _Multiply_460eba5b3c8c46b0b316127da0742a66_Out_2_Vector3[0];
float _Split_3a5da5502be344c491dbfc1a97b4bde7_G_2_Float = _Multiply_460eba5b3c8c46b0b316127da0742a66_Out_2_Vector3[1];
float _Split_3a5da5502be344c491dbfc1a97b4bde7_B_3_Float = _Multiply_460eba5b3c8c46b0b316127da0742a66_Out_2_Vector3[2];
float _Split_3a5da5502be344c491dbfc1a97b4bde7_A_4_Float = 0;
float2 _Vector2_f9f0a8b68def492b8df3bf37f0424bd4_Out_0_Vector2 = float2(_Split_3a5da5502be344c491dbfc1a97b4bde7_R_1_Float, _Split_3a5da5502be344c491dbfc1a97b4bde7_B_3_Float);
float _Property_d17a295f21134bac99eef146122355bd_Out_0_Float = _Color_Noise_Large_Freq;
float _SimpleNoise_d1fc4fa06f6e4b68bb5bab42ebb697b2_Out_2_Float;
Unity_SimpleNoise_Deterministic_float(_Vector2_f9f0a8b68def492b8df3bf37f0424bd4_Out_0_Vector2, _Property_d17a295f21134bac99eef146122355bd_Out_0_Float, _SimpleNoise_d1fc4fa06f6e4b68bb5bab42ebb697b2_Out_2_Float);
float3 _Contrast_891b7c34cb2d42238659d1b4be50745a_Out_2_Vector3;
Unity_Contrast_float((_SimpleNoise_d1fc4fa06f6e4b68bb5bab42ebb697b2_Out_2_Float.xxx), float(4), _Contrast_891b7c34cb2d42238659d1b4be50745a_Out_2_Vector3);
float3 _Saturate_4f608a79be72463c9b0f43d56137e215_Out_1_Vector3;
Unity_Saturate_float3(_Contrast_891b7c34cb2d42238659d1b4be50745a_Out_2_Vector3, _Saturate_4f608a79be72463c9b0f43d56137e215_Out_1_Vector3);
float _Split_904fec1eb53f4260a5c0bc75149c9808_R_1_Float = _Saturate_4f608a79be72463c9b0f43d56137e215_Out_1_Vector3[0];
float _Split_904fec1eb53f4260a5c0bc75149c9808_G_2_Float = _Saturate_4f608a79be72463c9b0f43d56137e215_Out_1_Vector3[1];
float _Split_904fec1eb53f4260a5c0bc75149c9808_B_3_Float = _Saturate_4f608a79be72463c9b0f43d56137e215_Out_1_Vector3[2];
float _Split_904fec1eb53f4260a5c0bc75149c9808_A_4_Float = 0;
float4 _Lerp_7ead1f75ec964cb5919aa19b7bd45caa_Out_3_Vector4;
Unity_Lerp_float4(_Lerp_a2debaae9e4145209ce431cc718371ba_Out_3_Vector4, _Property_121799f07d3c4b9e842302c43b821180_Out_0_Vector4, (_Split_904fec1eb53f4260a5c0bc75149c9808_R_1_Float.xxxx), _Lerp_7ead1f75ec964cb5919aa19b7bd45caa_Out_3_Vector4);
float4 _Branch_5e4fc915decc48cbbb32e7b8d694eb51_Out_3_Vector4;
Unity_Branch_float4(_Property_94cc0a68d8e64a0c94b8da2363a001fa_Out_0_Boolean, _Lerp_7ead1f75ec964cb5919aa19b7bd45caa_Out_3_Vector4, _Property_90e1572135294114aa8640a3a78cb1d1_Out_0_Vector4, _Branch_5e4fc915decc48cbbb32e7b8d694eb51_Out_3_Vector4);
float _Split_31923935c972461bbf40259d1ecc2f0a_R_1_Float = IN.VertexColor[0];
float _Split_31923935c972461bbf40259d1ecc2f0a_G_2_Float = IN.VertexColor[1];
float _Split_31923935c972461bbf40259d1ecc2f0a_B_3_Float = IN.VertexColor[2];
float _Split_31923935c972461bbf40259d1ecc2f0a_A_4_Float = IN.VertexColor[3];
float _Float_f0e37f97607d4bb2993cf03d63e3b163_Out_0_Float = float(0.5);
float _Comparison_adb8778b630a4ba19c565a39628351e1_Out_2_Boolean;
Unity_Comparison_Greater_float(_Split_31923935c972461bbf40259d1ecc2f0a_B_3_Float, _Float_f0e37f97607d4bb2993cf03d63e3b163_Out_0_Float, _Comparison_adb8778b630a4ba19c565a39628351e1_Out_2_Boolean);
UnityTexture2D _Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Leaf_Texture, sampler_Leaf_Texture, _Leaf_Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_86188cf8946a45c996e76799c9328f7f_Out_0_Vector2 = _Leaf_Tiling;
float2 _Property_b90a791a9e14422593a379dd2c182b1a_Out_0_Vector2 = _Leaf_Offset;
float2 _TilingAndOffset_847b3360ba05499a89eee41bf40e4a4d_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_86188cf8946a45c996e76799c9328f7f_Out_0_Vector2, _Property_b90a791a9e14422593a379dd2c182b1a_Out_0_Vector2, _TilingAndOffset_847b3360ba05499a89eee41bf40e4a4d_Out_3_Vector2);
float4 _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D.tex, _Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D.samplerstate, _Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_847b3360ba05499a89eee41bf40e4a4d_Out_3_Vector2) );
if (_Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4, _Property_5a9e78069ae04b1ab43bb10463691a8b_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_R_4_Float = _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4.r;
float _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_G_5_Float = _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4.g;
float _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_B_6_Float = _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4.b;
float _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_A_7_Float = _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4.a;
UnityTexture2D _Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Trunk_Texture, sampler_Trunk_Texture, _Trunk_Texture_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_deccc75eacd34b17a0e16bab322c8d32_Out_0_Vector2 = _Trunk_Tiling;
float2 _Property_10b6f772fc2a49199efff28a7effface_Out_0_Vector2 = _Trunk_Offset;
float2 _TilingAndOffset_4729983180834ceb8a5e86f7a8faa43e_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_deccc75eacd34b17a0e16bab322c8d32_Out_0_Vector2, _Property_10b6f772fc2a49199efff28a7effface_Out_0_Vector2, _TilingAndOffset_4729983180834ceb8a5e86f7a8faa43e_Out_3_Vector2);
float4 _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D.tex, _Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D.samplerstate, _Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_4729983180834ceb8a5e86f7a8faa43e_Out_3_Vector2) );
if (_Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4, _Property_4a6b891e649d43bca2878bdb81dc2c56_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_R_4_Float = _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4.r;
float _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_G_5_Float = _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4.g;
float _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_B_6_Float = _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4.b;
float _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_A_7_Float = _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4.a;
float4 _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4;
Unity_Branch_float4(_Comparison_adb8778b630a4ba19c565a39628351e1_Out_2_Boolean, _SampleTexture2D_05b4653a182f4241b4ca23b3815363dc_RGBA_0_Vector4, _SampleTexture2D_b639a8b9f3cd4f9a838051d010e3c24c_RGBA_0_Vector4, _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4);
float4 _Blend_3e4e491eda3b422b9a971e2834e87f89_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4, _Lerp_7ead1f75ec964cb5919aa19b7bd45caa_Out_3_Vector4, _Blend_3e4e491eda3b422b9a971e2834e87f89_Out_2_Vector4, float(1));
float4 _Branch_5455f61b6a9543cc8287da7085020606_Out_3_Vector4;
Unity_Branch_float4(_Property_94cc0a68d8e64a0c94b8da2363a001fa_Out_0_Boolean, _Blend_3e4e491eda3b422b9a971e2834e87f89_Out_2_Vector4, _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4, _Branch_5455f61b6a9543cc8287da7085020606_Out_3_Vector4);
float4 _Branch_f4d5735b2acb4d70ae533f8147dc3376_Out_3_Vector4;
Unity_Branch_float4(_Property_08c668170b28492f94cc488fdbbc4177_Out_0_Boolean, _Branch_5e4fc915decc48cbbb32e7b8d694eb51_Out_3_Vector4, _Branch_5455f61b6a9543cc8287da7085020606_Out_3_Vector4, _Branch_f4d5735b2acb4d70ae533f8147dc3376_Out_3_Vector4);
float4 _Property_f19286f71ecc46c98de53fbae4455d70_Out_0_Vector4 = _Frosting_Color;
float _Property_3768f178f0da48ac9d126f838bb2cfd4_Out_0_Boolean = _Enable_Frosting;
float _Property_589e80ea2a7b4a6493a193fddadcb7c8_Out_0_Boolean = _Frosting_Use_World_Normals;
float3 _Branch_d03fe811ec7e444988fd6e2852349660_Out_3_Vector3;
Unity_Branch_float3(_Property_589e80ea2a7b4a6493a193fddadcb7c8_Out_0_Boolean, IN.WorldSpaceNormal, IN.ObjectSpaceNormal, _Branch_d03fe811ec7e444988fd6e2852349660_Out_3_Vector3);
float _Split_f84e2ab576c948bd99abb98c9a83c360_R_1_Float = _Branch_d03fe811ec7e444988fd6e2852349660_Out_3_Vector3[0];
float _Split_f84e2ab576c948bd99abb98c9a83c360_G_2_Float = _Branch_d03fe811ec7e444988fd6e2852349660_Out_3_Vector3[1];
float _Split_f84e2ab576c948bd99abb98c9a83c360_B_3_Float = _Branch_d03fe811ec7e444988fd6e2852349660_Out_3_Vector3[2];
float _Split_f84e2ab576c948bd99abb98c9a83c360_A_4_Float = 0;
float _Property_7110dee18bc44c0aa7b3f46526a957bb_Out_0_Float = _Frosting_Height;
float _Power_1de4465c40d74bc08daba10d5ebebf95_Out_2_Float;
Unity_Power_float(_Split_f84e2ab576c948bd99abb98c9a83c360_G_2_Float, _Property_7110dee18bc44c0aa7b3f46526a957bb_Out_0_Float, _Power_1de4465c40d74bc08daba10d5ebebf95_Out_2_Float);
float _Property_2133fdd20db14513a870036f4920f2d4_Out_0_Float = _Frosting_Falloff;
float _Multiply_a17f2166dea14de38e332417df944692_Out_2_Float;
Unity_Multiply_float_float(_Power_1de4465c40d74bc08daba10d5ebebf95_Out_2_Float, _Property_2133fdd20db14513a870036f4920f2d4_Out_0_Float, _Multiply_a17f2166dea14de38e332417df944692_Out_2_Float);
float _Saturate_82b8da22d5fd47d18d35c6fe8ce82c7b_Out_1_Float;
Unity_Saturate_float(_Multiply_a17f2166dea14de38e332417df944692_Out_2_Float, _Saturate_82b8da22d5fd47d18d35c6fe8ce82c7b_Out_1_Float);
float _Branch_db4d24cf182044a9b274ffeb0d4ecde6_Out_3_Float;
Unity_Branch_float(_Property_3768f178f0da48ac9d126f838bb2cfd4_Out_0_Boolean, _Saturate_82b8da22d5fd47d18d35c6fe8ce82c7b_Out_1_Float, float(0), _Branch_db4d24cf182044a9b274ffeb0d4ecde6_Out_3_Float);
float4 _Lerp_a7664715039c4777a61a48729821721c_Out_3_Vector4;
Unity_Lerp_float4(_Branch_f4d5735b2acb4d70ae533f8147dc3376_Out_3_Vector4, _Property_f19286f71ecc46c98de53fbae4455d70_Out_0_Vector4, (_Branch_db4d24cf182044a9b274ffeb0d4ecde6_Out_3_Float.xxxx), _Lerp_a7664715039c4777a61a48729821721c_Out_3_Vector4);
float _Property_0df8fec1230040f6b739cc2527eb1327_Out_0_Boolean = _Trunk_Flat_Color_Switch;
float4 _Property_6b4b7c7ce72b42789ba28227a9080ff2_Out_0_Vector4 = _Trunk_Noise_Color;
float4 _Property_da8943697c7443ab9e504a69964f22ec_Out_0_Vector4 = _Trunk_Base_Color;
float4 _Lerp_17a4d506fe56435898ae30865d135501_Out_3_Vector4;
Unity_Lerp_float4(_Property_6b4b7c7ce72b42789ba28227a9080ff2_Out_0_Vector4, _Property_da8943697c7443ab9e504a69964f22ec_Out_0_Vector4, (_Split_21a7b352b7e84916b21335e22b072643_R_1_Float.xxxx), _Lerp_17a4d506fe56435898ae30865d135501_Out_3_Vector4);
float _Property_233ad08698e74063b8fe28c6535244e7_Out_0_Boolean = _Use_Color_Noise;
float4 _Blend_a79c9def11a645679bc8d7d6888318e7_Out_2_Vector4;
Unity_Blend_Multiply_float4(_Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4, _Lerp_17a4d506fe56435898ae30865d135501_Out_3_Vector4, _Blend_a79c9def11a645679bc8d7d6888318e7_Out_2_Vector4, float(1));
float4 _Branch_e436cc2bbdcb4a4b835d6db5318241de_Out_3_Vector4;
Unity_Branch_float4(_Property_233ad08698e74063b8fe28c6535244e7_Out_0_Boolean, _Blend_a79c9def11a645679bc8d7d6888318e7_Out_2_Vector4, _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4, _Branch_e436cc2bbdcb4a4b835d6db5318241de_Out_3_Vector4);
float4 _Branch_e5a34617559a46ee90d3e45bf0c247c9_Out_3_Vector4;
Unity_Branch_float4(_Property_0df8fec1230040f6b739cc2527eb1327_Out_0_Boolean, _Lerp_17a4d506fe56435898ae30865d135501_Out_3_Vector4, _Branch_e436cc2bbdcb4a4b835d6db5318241de_Out_3_Vector4, _Branch_e5a34617559a46ee90d3e45bf0c247c9_Out_3_Vector4);
float4 _Branch_0fa1c22127194a53b47266042f6e9bf1_Out_3_Vector4;
Unity_Branch_float4(_Comparison_6ac47c1f48a14a2f978d1bf37e8bdb01_Out_2_Boolean, _Lerp_a7664715039c4777a61a48729821721c_Out_3_Vector4, _Branch_e5a34617559a46ee90d3e45bf0c247c9_Out_3_Vector4, _Branch_0fa1c22127194a53b47266042f6e9bf1_Out_3_Vector4);
float _Property_b2af51507fb542e19adebf49de5d259a_Out_0_Boolean = _Enable_Back_Face_Lighting;
float _IsFrontFace_6b33e95c85f04ba081bfd83cea052eba_Out_0_Boolean = max(0, IN.FaceSign.x);
float _Split_5aa300ba46e34dcaa0bf1bb7c71d997c_R_1_Float = IN.VertexColor[0];
float _Split_5aa300ba46e34dcaa0bf1bb7c71d997c_G_2_Float = IN.VertexColor[1];
float _Split_5aa300ba46e34dcaa0bf1bb7c71d997c_B_3_Float = IN.VertexColor[2];
float _Split_5aa300ba46e34dcaa0bf1bb7c71d997c_A_4_Float = IN.VertexColor[3];
float _Float_31abf9273c3c44d0aaa90a6cc10b2a0c_Out_0_Float = float(0.5);
float _Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean;
Unity_Comparison_Greater_float(_Split_5aa300ba46e34dcaa0bf1bb7c71d997c_B_3_Float, _Float_31abf9273c3c44d0aaa90a6cc10b2a0c_Out_0_Float, _Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean);
float _Property_3751000a2b8b45d58c134d5a8a3b3db2_Out_0_Boolean = _Enable_Leaf_Normal;
float3 _Vector3_285700f607174cda8dc7b3704fdce397_Out_0_Vector3 = float3(float(0), float(0), float(1));
UnityTexture2D _Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Leaf_Normal, sampler_Leaf_Normal, _Leaf_Normal_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_eb27c7bafcea4544a9dea6e1a5581791_Out_0_Vector2 = _Leaf_Normal_Tiling;
float2 _Property_11a345a9c42343acb775da229a73e84a_Out_0_Vector2 = _Leaf_Normal_Offset;
float2 _TilingAndOffset_db6342c543a54b7aa8cfc7deffb356dd_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_eb27c7bafcea4544a9dea6e1a5581791_Out_0_Vector2, _Property_11a345a9c42343acb775da229a73e84a_Out_0_Vector2, _TilingAndOffset_db6342c543a54b7aa8cfc7deffb356dd_Out_3_Vector2);
float4 _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D.tex, _Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D.samplerstate, _Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_db6342c543a54b7aa8cfc7deffb356dd_Out_3_Vector2) );
if (_Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4, _Property_a086a0ab69da46a5aaf3457dde538550_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4);
float _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_R_4_Float = _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.r;
float _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_G_5_Float = _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.g;
float _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_B_6_Float = _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.b;
float _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_A_7_Float = _SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.a;
float _Property_2b1770c4a3e74022942166377095147a_Out_0_Float = _Leaf_Normal_Strength;
float3 _NormalStrength_e58ea023871c48e895d02f504858691c_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_c08993351fcc464c8c4d87dd177debab_RGBA_0_Vector4.xyz), _Property_2b1770c4a3e74022942166377095147a_Out_0_Float, _NormalStrength_e58ea023871c48e895d02f504858691c_Out_2_Vector3);
float3 _NormalBlend_31a854a45ed34a9a88a4a03067ddea50_Out_2_Vector3;
Unity_NormalBlend_float(_Vector3_285700f607174cda8dc7b3704fdce397_Out_0_Vector3, _NormalStrength_e58ea023871c48e895d02f504858691c_Out_2_Vector3, _NormalBlend_31a854a45ed34a9a88a4a03067ddea50_Out_2_Vector3);
float3 _Branch_5691496ca4ed416e84423dd1604004e8_Out_3_Vector3;
Unity_Branch_float3(_Property_3751000a2b8b45d58c134d5a8a3b3db2_Out_0_Boolean, _NormalBlend_31a854a45ed34a9a88a4a03067ddea50_Out_2_Vector3, _Vector3_285700f607174cda8dc7b3704fdce397_Out_0_Vector3, _Branch_5691496ca4ed416e84423dd1604004e8_Out_3_Vector3);
float _Property_24d70b52337f411c8ba4234b5b77ebdc_Out_0_Boolean = _Enable_Trunk_Normal;
UnityTexture2D _Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Trunk_Normal, sampler_Trunk_Normal, _Trunk_Normal_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_0edb1c13ff854e3b9baf0f01f1cc8b8e_Out_0_Vector2 = _Trunk_Normal_Tiling;
float2 _Property_8537ea1474c04611ab6f0ad0b53c4634_Out_0_Vector2 = _Trunk_Normal_Offset;
float2 _TilingAndOffset_099ee15ecaba4d5fb6dc0a05ce623bbb_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_0edb1c13ff854e3b9baf0f01f1cc8b8e_Out_0_Vector2, _Property_8537ea1474c04611ab6f0ad0b53c4634_Out_0_Vector2, _TilingAndOffset_099ee15ecaba4d5fb6dc0a05ce623bbb_Out_3_Vector2);
float4 _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D.tex, _Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D.samplerstate, _Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_099ee15ecaba4d5fb6dc0a05ce623bbb_Out_3_Vector2) );
if (_Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4, _Property_eb54c02f11c74fc38c0aeaf5e54e0034_Out_0_Texture2D.hdrDecode);
_SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4);
float _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_R_4_Float = _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.r;
float _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_G_5_Float = _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.g;
float _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_B_6_Float = _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.b;
float _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_A_7_Float = _SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.a;
float _Property_2ca07f1351674b31a00af326fe3587e3_Out_0_Float = _Trunk_Normal_Strength;
float3 _NormalStrength_5d118577632f48f98dbc4e1a4feb6f4c_Out_2_Vector3;
Unity_NormalStrength_float((_SampleTexture2D_0a4ce735d02d4c2e9ba27e92741106a8_RGBA_0_Vector4.xyz), _Property_2ca07f1351674b31a00af326fe3587e3_Out_0_Float, _NormalStrength_5d118577632f48f98dbc4e1a4feb6f4c_Out_2_Vector3);
float3 _NormalBlend_ee022931397342cda361062e2eab6c1c_Out_2_Vector3;
Unity_NormalBlend_float(_Vector3_285700f607174cda8dc7b3704fdce397_Out_0_Vector3, _NormalStrength_5d118577632f48f98dbc4e1a4feb6f4c_Out_2_Vector3, _NormalBlend_ee022931397342cda361062e2eab6c1c_Out_2_Vector3);
float3 _Branch_994700d6d3f24ed9a8deab1c63bddf63_Out_3_Vector3;
Unity_Branch_float3(_Property_24d70b52337f411c8ba4234b5b77ebdc_Out_0_Boolean, _NormalBlend_ee022931397342cda361062e2eab6c1c_Out_2_Vector3, _Vector3_285700f607174cda8dc7b3704fdce397_Out_0_Vector3, _Branch_994700d6d3f24ed9a8deab1c63bddf63_Out_3_Vector3);
float3 _Branch_f75e0829375a42eda2f4fc2e32a434da_Out_3_Vector3;
Unity_Branch_float3(_Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean, _Branch_5691496ca4ed416e84423dd1604004e8_Out_3_Vector3, _Branch_994700d6d3f24ed9a8deab1c63bddf63_Out_3_Vector3, _Branch_f75e0829375a42eda2f4fc2e32a434da_Out_3_Vector3);
float3 _Negate_bc765ffbc8594dce8f7f4516c4e97014_Out_1_Vector3;
Unity_Negate_float3(_Branch_f75e0829375a42eda2f4fc2e32a434da_Out_3_Vector3, _Negate_bc765ffbc8594dce8f7f4516c4e97014_Out_1_Vector3);
float3 _Branch_77af1f3696f140e492c28409419e25e1_Out_3_Vector3;
Unity_Branch_float3(_IsFrontFace_6b33e95c85f04ba081bfd83cea052eba_Out_0_Boolean, _Branch_f75e0829375a42eda2f4fc2e32a434da_Out_3_Vector3, _Negate_bc765ffbc8594dce8f7f4516c4e97014_Out_1_Vector3, _Branch_77af1f3696f140e492c28409419e25e1_Out_3_Vector3);
float3 _Branch_834ece3a826f4ffe87b1dcacdee430ab_Out_3_Vector3;
Unity_Branch_float3(_Property_b2af51507fb542e19adebf49de5d259a_Out_0_Boolean, _Branch_77af1f3696f140e492c28409419e25e1_Out_3_Vector3, _Branch_f75e0829375a42eda2f4fc2e32a434da_Out_3_Vector3, _Branch_834ece3a826f4ffe87b1dcacdee430ab_Out_3_Vector3);
float _Property_1573896e50584d73ab3f57ccb56f98e7_Out_0_Boolean = _Enable_Emission;
float _Property_395f9bf885b14d27bb69032b584e62fe_Out_0_Boolean = _Enable_Pulse;
float _Split_687e1292db604f0a890652c669635bdf_R_1_Float = IN.VertexColor[0];
float _Split_687e1292db604f0a890652c669635bdf_G_2_Float = IN.VertexColor[1];
float _Split_687e1292db604f0a890652c669635bdf_B_3_Float = IN.VertexColor[2];
float _Split_687e1292db604f0a890652c669635bdf_A_4_Float = IN.VertexColor[3];
float _Float_fc6206fbd41f4e9cb9e9159708f98c20_Out_0_Float = float(0.5);
float _Comparison_337d1f112f0847619a11780395aeb515_Out_2_Boolean;
Unity_Comparison_Greater_float(_Split_687e1292db604f0a890652c669635bdf_B_3_Float, _Float_fc6206fbd41f4e9cb9e9159708f98c20_Out_0_Float, _Comparison_337d1f112f0847619a11780395aeb515_Out_2_Boolean);
float4 _Property_b6af9bd499f04bb3afdcaa2d2b525291_Out_0_Vector4 = _Emissive_Color;
float _Float_57a29b209d2c4b8081bfe8dcb33219a4_Out_0_Float = float(0);
UnityTexture2D _Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Mask, sampler_Emissive_Mask, _Emissive_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_430a6dee22ab4aec98c92b1ec95a7d43_Out_0_Vector2 = _Emissive_Mask_Tiling;
float2 _Property_e0dc6a6cea1348578e87361af73ad7b1_Out_0_Vector2 = _Emissive_Mask_Offset;
float2 _TilingAndOffset_978f82ba344644568994a6c555b7924e_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_430a6dee22ab4aec98c92b1ec95a7d43_Out_0_Vector2, _Property_e0dc6a6cea1348578e87361af73ad7b1_Out_0_Vector2, _TilingAndOffset_978f82ba344644568994a6c555b7924e_Out_3_Vector2);
float4 _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D.tex, _Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D.samplerstate, _Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_978f82ba344644568994a6c555b7924e_Out_3_Vector2) );
if (_Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4, _Property_90e72c69b7224bb590559cdb78cfac80_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_R_4_Float = _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4.r;
float _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_G_5_Float = _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4.g;
float _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_B_6_Float = _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4.b;
float _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_A_7_Float = _SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_RGBA_0_Vector4.a;
float _OneMinus_9949856766f5458fade8b7bb31418eec_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_0bf8bcad73714b4d8ae792eb4a08a7c5_B_6_Float, _OneMinus_9949856766f5458fade8b7bb31418eec_Out_1_Float);
float4 _Lerp_5e1391ecbab64589aa1a0e210ba9c38b_Out_3_Vector4;
Unity_Lerp_float4(_Property_b6af9bd499f04bb3afdcaa2d2b525291_Out_0_Vector4, (_Float_57a29b209d2c4b8081bfe8dcb33219a4_Out_0_Float.xxxx), (_OneMinus_9949856766f5458fade8b7bb31418eec_Out_1_Float.xxxx), _Lerp_5e1391ecbab64589aa1a0e210ba9c38b_Out_3_Vector4);
float4 _Property_72d7c2cf9e2f4b1d9ea2ee77b09de5e7_Out_0_Vector4 = _Emissive_2_Color;
float _Float_284353e1bc2244729748ededef81b9a1_Out_0_Float = float(0);
UnityTexture2D _Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_2_Mask, sampler_Emissive_2_Mask, _Emissive_2_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_4466194a93d54451819d4516347b94c2_Out_0_Vector2 = _Emissive_Mask_2_Tiling;
float2 _Property_2c1ca95d265145688dd547c613522a09_Out_0_Vector2 = _Emissive_Mask_2_Offset;
float2 _TilingAndOffset_ef4796ad7e05485c9730a0793547c7eb_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_4466194a93d54451819d4516347b94c2_Out_0_Vector2, _Property_2c1ca95d265145688dd547c613522a09_Out_0_Vector2, _TilingAndOffset_ef4796ad7e05485c9730a0793547c7eb_Out_3_Vector2);
float4 _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D.tex, _Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D.samplerstate, _Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_ef4796ad7e05485c9730a0793547c7eb_Out_3_Vector2) );
if (_Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4, _Property_060178e1e9014f6285f385c18b85610e_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_R_4_Float = _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4.r;
float _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_G_5_Float = _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4.g;
float _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_B_6_Float = _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4.b;
float _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_A_7_Float = _SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_RGBA_0_Vector4.a;
float _OneMinus_8d811b588e034358b2d4af52c940b5fe_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_e35b1cfc501842659f08c6a0e6f1d015_B_6_Float, _OneMinus_8d811b588e034358b2d4af52c940b5fe_Out_1_Float);
float4 _Lerp_4e8aa477edf34134ab28bb8cc5e8c049_Out_3_Vector4;
Unity_Lerp_float4(_Property_72d7c2cf9e2f4b1d9ea2ee77b09de5e7_Out_0_Vector4, (_Float_284353e1bc2244729748ededef81b9a1_Out_0_Float.xxxx), (_OneMinus_8d811b588e034358b2d4af52c940b5fe_Out_1_Float.xxxx), _Lerp_4e8aa477edf34134ab28bb8cc5e8c049_Out_3_Vector4);
float4 _Add_839560fc3fb149698b343c4f5854bb34_Out_2_Vector4;
Unity_Add_float4(_Lerp_5e1391ecbab64589aa1a0e210ba9c38b_Out_3_Vector4, _Lerp_4e8aa477edf34134ab28bb8cc5e8c049_Out_3_Vector4, _Add_839560fc3fb149698b343c4f5854bb34_Out_2_Vector4);
float4 _Property_d7087a4c1e5a46feb5d1410e51f0fb47_Out_0_Vector4 = _Trunk_Emissive_Color;
float _Float_3eae8191d2bd448ca859b0667c30b96e_Out_0_Float = float(0);
UnityTexture2D _Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Trunk_Emissive_Mask, sampler_Trunk_Emissive_Mask, _Trunk_Emissive_Mask_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D.tex, _Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D.samplerstate, _Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
if (_Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4, _Property_1bca72f2bb9842058059c67a24567ebe_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_R_4_Float = _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4.r;
float _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_G_5_Float = _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4.g;
float _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_B_6_Float = _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4.b;
float _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_A_7_Float = _SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_RGBA_0_Vector4.a;
float _OneMinus_5f1351fb907e477c852882fe6dabe2e3_Out_1_Float;
Unity_OneMinus_float(_SampleTexture2D_ca1f0ed62c65402ea5fc565acf4ffb25_R_4_Float, _OneMinus_5f1351fb907e477c852882fe6dabe2e3_Out_1_Float);
float4 _Lerp_d3fb88e839ef46e3a8811b3968892270_Out_3_Vector4;
Unity_Lerp_float4(_Property_d7087a4c1e5a46feb5d1410e51f0fb47_Out_0_Vector4, (_Float_3eae8191d2bd448ca859b0667c30b96e_Out_0_Float.xxxx), (_OneMinus_5f1351fb907e477c852882fe6dabe2e3_Out_1_Float.xxxx), _Lerp_d3fb88e839ef46e3a8811b3968892270_Out_3_Vector4);
float4 _Branch_377f1fc689a3424a93e25af3b25b0b99_Out_3_Vector4;
Unity_Branch_float4(_Comparison_337d1f112f0847619a11780395aeb515_Out_2_Boolean, _Add_839560fc3fb149698b343c4f5854bb34_Out_2_Vector4, _Lerp_d3fb88e839ef46e3a8811b3968892270_Out_3_Vector4, _Branch_377f1fc689a3424a93e25af3b25b0b99_Out_3_Vector4);
float _Property_778a3244ae854a48820dee0b1bbb707a_Out_0_Float = _Emissive_Amount;
float _Float_c2e38121d15041cbbb65ab797888e9d3_Out_0_Float = float(1.5);
float _Multiply_a29b1688fd284553b62f8414f8a5fda6_Out_2_Float;
Unity_Multiply_float_float(_Property_778a3244ae854a48820dee0b1bbb707a_Out_0_Float, _Float_c2e38121d15041cbbb65ab797888e9d3_Out_0_Float, _Multiply_a29b1688fd284553b62f8414f8a5fda6_Out_2_Float);
float4 _Multiply_8c240b5e860b43509a487b3ccb18fab1_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_377f1fc689a3424a93e25af3b25b0b99_Out_3_Vector4, (_Multiply_a29b1688fd284553b62f8414f8a5fda6_Out_2_Float.xxxx), _Multiply_8c240b5e860b43509a487b3ccb18fab1_Out_2_Vector4);
UnityTexture2D _Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Emissive_Pulse_Map, sampler_Emissive_Pulse_Map, _Emissive_Pulse_Map_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float4 _UV_138f2d669bd54049b0889d9f90f1cbd5_Out_0_Vector4 = IN.uv0;
float _Property_9e47830cb643472bab157db25bb1b21f_Out_0_Float = _Pulse_Tiling;
float4 _Multiply_1c31c2c9d8f440c6b042f08e95c45cc1_Out_2_Vector4;
Unity_Multiply_float4_float4(_UV_138f2d669bd54049b0889d9f90f1cbd5_Out_0_Vector4, (_Property_9e47830cb643472bab157db25bb1b21f_Out_0_Float.xxxx), _Multiply_1c31c2c9d8f440c6b042f08e95c45cc1_Out_2_Vector4);
float _Property_5a11a88503e647e897533a30f5de3f7e_Out_0_Float = _Pulse_Speed;
float _Multiply_63341e180bf245be8fc7d7268bcc4b7b_Out_2_Float;
Unity_Multiply_float_float(_Property_5a11a88503e647e897533a30f5de3f7e_Out_0_Float, IN.TimeParameters.x, _Multiply_63341e180bf245be8fc7d7268bcc4b7b_Out_2_Float);
float4 _Add_a2950059bbb642359a3f6fc42ada5b12_Out_2_Vector4;
Unity_Add_float4(_Multiply_1c31c2c9d8f440c6b042f08e95c45cc1_Out_2_Vector4, (_Multiply_63341e180bf245be8fc7d7268bcc4b7b_Out_2_Float.xxxx), _Add_a2950059bbb642359a3f6fc42ada5b12_Out_2_Vector4);
float4 _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D.tex, _Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D.samplerstate, _Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D.GetTransformedUV((_Add_a2950059bbb642359a3f6fc42ada5b12_Out_2_Vector4.xy)) );
if (_Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4, _Property_7cbfce28cf7249e9aa103ba5b6895d6c_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_R_4_Float = _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4.r;
float _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_G_5_Float = _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4.g;
float _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_B_6_Float = _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4.b;
float _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_A_7_Float = _SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_RGBA_0_Vector4.a;
float _Split_b6aa1f8c705541558419cb0c6e18c6a2_R_1_Float = IN.WorldSpacePosition[0];
float _Split_b6aa1f8c705541558419cb0c6e18c6a2_G_2_Float = IN.WorldSpacePosition[1];
float _Split_b6aa1f8c705541558419cb0c6e18c6a2_B_3_Float = IN.WorldSpacePosition[2];
float _Split_b6aa1f8c705541558419cb0c6e18c6a2_A_4_Float = 0;
float2 _Vector2_d6b695b523e9400f8cd63ad4bc85ec31_Out_0_Vector2 = float2(_Split_b6aa1f8c705541558419cb0c6e18c6a2_R_1_Float, _Split_b6aa1f8c705541558419cb0c6e18c6a2_B_3_Float);
float _Float_75f42d0273b54fbaabb6d9a347ef7390_Out_0_Float = float(0.5);
float2 _Multiply_538f3474341440a6a6ef011e9a5e4a4d_Out_2_Vector2;
Unity_Multiply_float2_float2(_Vector2_d6b695b523e9400f8cd63ad4bc85ec31_Out_0_Vector2, (_Float_75f42d0273b54fbaabb6d9a347ef7390_Out_0_Float.xx), _Multiply_538f3474341440a6a6ef011e9a5e4a4d_Out_2_Vector2);
float _Float_44b0c0568e664287a15f28d42ad1cfa4_Out_0_Float = float(4);
float _Multiply_1305e758ff744394adf8f532920d965c_Out_2_Float;
Unity_Multiply_float_float(IN.TimeParameters.x, _Float_44b0c0568e664287a15f28d42ad1cfa4_Out_0_Float, _Multiply_1305e758ff744394adf8f532920d965c_Out_2_Float);
float2 _Add_bd59045f546847999bf498043e00bd2f_Out_2_Vector2;
Unity_Add_float2(_Multiply_538f3474341440a6a6ef011e9a5e4a4d_Out_2_Vector2, (_Multiply_1305e758ff744394adf8f532920d965c_Out_2_Float.xx), _Add_bd59045f546847999bf498043e00bd2f_Out_2_Vector2);
float2 _Sine_ae74fe1363974b79894215f10819b8ff_Out_1_Vector2;
Unity_Sine_float2(_Add_bd59045f546847999bf498043e00bd2f_Out_2_Vector2, _Sine_ae74fe1363974b79894215f10819b8ff_Out_1_Vector2);
float3 _Saturation_8cd0a3b60478426f99fb4ee27e7babfe_Out_2_Vector3;
Unity_Saturation_float((float3(_Sine_ae74fe1363974b79894215f10819b8ff_Out_1_Vector2, 0.0)), float(0), _Saturation_8cd0a3b60478426f99fb4ee27e7babfe_Out_2_Vector3);
float3 _Maximum_b022edf4e7214e909240daba813e738f_Out_2_Vector3;
Unity_Maximum_float3((_SampleTexture2D_3a13c996cd4f4c5e8205d53035129237_R_4_Float.xxx), _Saturation_8cd0a3b60478426f99fb4ee27e7babfe_Out_2_Vector3, _Maximum_b022edf4e7214e909240daba813e738f_Out_2_Vector3);
float3 _Saturate_a01d0a8996144961bf78702ebfdd59cf_Out_1_Vector3;
Unity_Saturate_float3(_Maximum_b022edf4e7214e909240daba813e738f_Out_2_Vector3, _Saturate_a01d0a8996144961bf78702ebfdd59cf_Out_1_Vector3);
float3 _Remap_9c650101fc614b9799ad3e1b3b8991fa_Out_3_Vector3;
Unity_Remap_float3(_Saturate_a01d0a8996144961bf78702ebfdd59cf_Out_1_Vector3, float2 (0, 1), float2 (0.5, 1), _Remap_9c650101fc614b9799ad3e1b3b8991fa_Out_3_Vector3);
float3 _Multiply_69ff2d63807a4d7cbc38c94e8f906c1a_Out_2_Vector3;
Unity_Multiply_float3_float3((_Multiply_8c240b5e860b43509a487b3ccb18fab1_Out_2_Vector4.xyz), _Remap_9c650101fc614b9799ad3e1b3b8991fa_Out_3_Vector3, _Multiply_69ff2d63807a4d7cbc38c94e8f906c1a_Out_2_Vector3);
float3 _Branch_054fa415d6f34a08aa583d4203b72331_Out_3_Vector3;
Unity_Branch_float3(_Property_395f9bf885b14d27bb69032b584e62fe_Out_0_Boolean, _Multiply_69ff2d63807a4d7cbc38c94e8f906c1a_Out_2_Vector3, (_Multiply_8c240b5e860b43509a487b3ccb18fab1_Out_2_Vector4.xyz), _Branch_054fa415d6f34a08aa583d4203b72331_Out_3_Vector3);
float4 Color_7b2e153d78b64b88956e4545227f95bc = IsGammaSpace() ? float4(0, 0, 0, 0) : float4(SRGBToLinear(float3(0, 0, 0)), 0);
float3 _Branch_8a2ee7d6b916497caaaa7371550da9a4_Out_3_Vector3;
Unity_Branch_float3(_Property_1573896e50584d73ab3f57ccb56f98e7_Out_0_Boolean, _Branch_054fa415d6f34a08aa583d4203b72331_Out_3_Vector3, (Color_7b2e153d78b64b88956e4545227f95bc.xyz), _Branch_8a2ee7d6b916497caaaa7371550da9a4_Out_3_Vector3);
float _Property_7569841dff2f42988a41eaf60fffbf29_Out_0_Float = _Leaf_Metallic;
float _Property_0310c94e16c048f1bb0b6139cb83655f_Out_0_Float = _Trunk_Metallic;
float _Branch_aaa083272da046df9be698db561ddf1c_Out_3_Float;
Unity_Branch_float(_Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean, _Property_7569841dff2f42988a41eaf60fffbf29_Out_0_Float, _Property_0310c94e16c048f1bb0b6139cb83655f_Out_0_Float, _Branch_aaa083272da046df9be698db561ddf1c_Out_3_Float);
float _Property_9a8067f24b284e4b8b90230473f95aa1_Out_0_Float = _Leaf_Smoothness;
float _Property_f5d1d8b21f484f1ca8dfd4d96cae1e7a_Out_0_Float = _Trunk_Smoothness;
float _Branch_0ddfe4504e8b41ed894456f2a1d99e11_Out_3_Float;
Unity_Branch_float(_Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean, _Property_9a8067f24b284e4b8b90230473f95aa1_Out_0_Float, _Property_f5d1d8b21f484f1ca8dfd4d96cae1e7a_Out_0_Float, _Branch_0ddfe4504e8b41ed894456f2a1d99e11_Out_3_Float);
UnityTexture2D _Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Leaf_Ambient_Occlusion, sampler_Leaf_Ambient_Occlusion, _Leaf_Ambient_Occlusion_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_3dd3e57658724c3fb1d633acc02e88f0_Out_0_Vector2 = _Leaf_Tiling;
float2 _Property_2cab4949c76d41a5b48793143d1e6520_Out_0_Vector2 = _Leaf_Offset;
float2 _TilingAndOffset_d2650bc7e78443d1a5a0bbaa2bf106be_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_3dd3e57658724c3fb1d633acc02e88f0_Out_0_Vector2, _Property_2cab4949c76d41a5b48793143d1e6520_Out_0_Vector2, _TilingAndOffset_d2650bc7e78443d1a5a0bbaa2bf106be_Out_3_Vector2);
float4 _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D.tex, _Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D.samplerstate, _Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_d2650bc7e78443d1a5a0bbaa2bf106be_Out_3_Vector2) );
if (_Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4, _Property_1e063ad326ba48528cd077ce461fe3fa_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_R_4_Float = _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4.r;
float _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_G_5_Float = _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4.g;
float _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_B_6_Float = _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4.b;
float _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_A_7_Float = _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_RGBA_0_Vector4.a;
float _Property_989732c379934fcf80ed1e9e365a6cbb_Out_0_Float = _Leaf_Ambient_Occlusion_Intensity;
float _Lerp_00aa4521d4d840a8a71805c7e6ecb496_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_65e3daa438b642eb8d490d0264da6bb8_R_4_Float, _Property_989732c379934fcf80ed1e9e365a6cbb_Out_0_Float, _Lerp_00aa4521d4d840a8a71805c7e6ecb496_Out_3_Float);
UnityTexture2D _Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_Trunk_Ambient_Occlusion, sampler_Trunk_Ambient_Occlusion, _Trunk_Ambient_Occlusion_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
float2 _Property_9e975b6acef246f0bfe0912474908510_Out_0_Vector2 = _Trunk_Tiling;
float2 _Property_386123881f404bb5b208a627e910878a_Out_0_Vector2 = _Trunk_Offset;
float2 _TilingAndOffset_f09b8845a47e403cb14303f5e8a622bd_Out_3_Vector2;
Unity_TilingAndOffset_float(IN.uv0.xy, _Property_9e975b6acef246f0bfe0912474908510_Out_0_Vector2, _Property_386123881f404bb5b208a627e910878a_Out_0_Vector2, _TilingAndOffset_f09b8845a47e403cb14303f5e8a622bd_Out_3_Vector2);
float4 _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D.tex, _Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D.samplerstate, _Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_f09b8845a47e403cb14303f5e8a622bd_Out_3_Vector2) );
if (_Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D.hdrDecode.x > 0)
_SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4 = DecodeHDRSample(_SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4, _Property_560996ac8f2b4aaab6bd6d86038088a4_Out_0_Texture2D.hdrDecode);
float _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_R_4_Float = _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4.r;
float _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_G_5_Float = _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4.g;
float _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_B_6_Float = _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4.b;
float _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_A_7_Float = _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_RGBA_0_Vector4.a;
float _Property_42f5aa69efb245b5a02552e1e5e6a14e_Out_0_Float = _Trunk_Ambient_Occlusion_Intensity;
float _Lerp_6612d9dad11346008e9703f9539d688b_Out_3_Float;
Unity_Lerp_float(float(1), _SampleTexture2D_2f868c6e3727411fb630be28a9d944ac_R_4_Float, _Property_42f5aa69efb245b5a02552e1e5e6a14e_Out_0_Float, _Lerp_6612d9dad11346008e9703f9539d688b_Out_3_Float);
float _Branch_f17bba7b236f4c1bbdd38c5d875bde27_Out_3_Float;
Unity_Branch_float(_Comparison_36f989967a1b4addbcaf8c03de7c2e72_Out_2_Boolean, _Lerp_00aa4521d4d840a8a71805c7e6ecb496_Out_3_Float, _Lerp_6612d9dad11346008e9703f9539d688b_Out_3_Float, _Branch_f17bba7b236f4c1bbdd38c5d875bde27_Out_3_Float);
float _Split_26da3e9f19444cd7ae7bdfc68f4feb5f_R_1_Float = _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4[0];
float _Split_26da3e9f19444cd7ae7bdfc68f4feb5f_G_2_Float = _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4[1];
float _Split_26da3e9f19444cd7ae7bdfc68f4feb5f_B_3_Float = _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4[2];
float _Split_26da3e9f19444cd7ae7bdfc68f4feb5f_A_4_Float = _Branch_ab3b77ac4e154a928c3c1358e69d8417_Out_3_Vector4[3];
float _Property_ee7df170430d483f8713c1c6c226862d_Out_0_Float = _Alpha_Clip_Threshold;
surface.BaseColor = (_Branch_0fa1c22127194a53b47266042f6e9bf1_Out_3_Vector4.xyz);
surface.NormalTS = _Branch_834ece3a826f4ffe87b1dcacdee430ab_Out_3_Vector3;
surface.Emission = _Branch_8a2ee7d6b916497caaaa7371550da9a4_Out_3_Vector3;
surface.Metallic = _Branch_aaa083272da046df9be698db561ddf1c_Out_3_Float;
surface.Specular = IsGammaSpace() ? float3(0.5, 0.5, 0.5) : SRGBToLinear(float3(0.5, 0.5, 0.5));
surface.Smoothness = _Branch_0ddfe4504e8b41ed894456f2a1d99e11_Out_3_Float;
surface.Occlusion = _Branch_f17bba7b236f4c1bbdd38c5d875bde27_Out_3_Float;
surface.Alpha = _Split_26da3e9f19444cd7ae7bdfc68f4feb5f_A_4_Float;
surface.AlphaClipThreshold = _Property_ee7df170430d483f8713c1c6c226862d_Out_0_Float;
return surface;
}

#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    output.ObjectSpaceNormal =                          input.normalOS;
    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
    output.ObjectSpacePosition =                        input.positionOS;
    output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
    output.VertexColor =                                input.color;
    output.TimeParameters =                             _TimeParameters.xyz;
#if UNITY_ANY_INSTANCING_ENABLED
#else
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
#endif

#endif

    float3 unnormalizedNormalWS = input.normalWS;
    const float renormFactor = 1.0 / length(unnormalizedNormalWS);

    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;
    output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);

    output.WorldSpacePosition = input.positionWS;

    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif

    output.uv0 = input.texCoord0;
    output.VertexColor = input.color;
#if UNITY_ANY_INSTANCING_ENABLED
#else
#endif
    output.TimeParameters = _TimeParameters.xyz;
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
