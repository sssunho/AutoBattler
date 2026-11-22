#ifndef RAGGED_BANNER_LEAF_THREE_TONE_LIGHTING_INCLUDED
#define RAGGED_BANNER_LEAF_THREE_TONE_LIGHTING_INCLUDED

half4 _PainterlyAmbientColor;

#if defined(_OLIVE_VISTA_FOG)
half3 MixOliveLeafFog(half3 color, half fogCoord, half leafMask)
{
    const half3 oliveFog = half3(0.325h, 0.387h, 0.197h);
    half transmission = saturate(ComputeFogIntensity(fogCoord));
    #if defined(_FAR_BANK_FOG)
        half oliveWeight = 0.612h * smoothstep(0.245h, 0.495h, transmission);
    #else
        half oliveWeight = 0.85h * smoothstep(0.015h, 0.16h, transmission);
    #endif
    half tintAmount = leafMask > 0.5h ? oliveWeight : 0.0h;
    half3 fogColor = lerp((half3)unity_FogColor.rgb, oliveFog, tintAmount);
    return MixFogColor(color, fogColor, fogCoord);
}
#endif

half2 StudyLeafBandWeights(float illumination, float transitionHalfWidth, float shadowVisibility)
{
    const float midThreshold = 0.08;
    const float litThreshold = 0.45;
    float width = min(transitionHalfWidth, (litThreshold - midThreshold) * 0.45);
    float2 thresholds = float2(lerp(0.02, midThreshold, shadowVisibility), litThreshold);
    float2 lower = max(0.0001, thresholds - width);
    float2 upper = max(lower + 0.0001, min(1.0, thresholds + width));
    return smoothstep(lower, upper, illumination);
}

half4 EvaluateStudyFoliage(InputData inputData, SurfaceData surface)
{
    half4 shadowMask = CalculateShadowMask(inputData);
    Light sun = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    float sunVisibility = saturate(sun.shadowAttenuation);
    #if defined(_FLAT_FOLIAGE_LIGHTING)
        const half3 shadeTint = half3(0.477h, 0.604h, 0.638h);
        half3 bandTint = lerp(shadeTint, 1.0h, sunVisibility);
    #else
        #if defined(_SCENE_PALETTE)
            const half3 midTint = half3(0.530h, 0.615h, 0.496h);
            const half3 shadeTint = half3(0.238h, 0.398h, 0.372h);
        #else
            const half3 midTint = half3(0.70h, 0.76h, 0.81h);
            const half3 shadeTint = half3(0.40h, 0.52h, 0.62h);
        #endif
        float3 normalWS = normalize(inputData.normalWS);
        float illumination = saturate(dot(normalWS, sun.direction)) * sunVisibility;
        half2 bands = StudyLeafBandWeights(illumination, max(0.03, fwidth(illumination) * 0.5), sunVisibility);
        half3 bandTint = lerp(lerp(shadeTint, midTint, bands.x), 1.0h, bands.y);
    #endif
    half3 lightColor = _PainterlyAmbientColor.rgb;
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(sun.layerMask, GetMeshRenderingLayer()))
    #endif
    {
        lightColor += bandTint * sun.color * sun.distanceAttenuation;
    }

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
            #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))
            #endif
            {
                #if defined(_FLAT_FOLIAGE_LIGHTING)
                    half3 lightTint = 1.0h;
                #else
                    float lightIllumination = saturate(dot(normalWS, light.direction));
                    half2 lightBands = StudyLeafBandWeights(lightIllumination, 0.03, 1.0);
                    half3 lightTint = lerp(midTint * lightBands.x, 1.0h, lightBands.y);
                #endif
                lightColor += lightTint * light.color * light.distanceAttenuation * saturate(light.shadowAttenuation);
            }
        LIGHT_LOOP_END
    #endif
    return half4(surface.albedo * lightColor + surface.emission, surface.alpha);
}

half4 EvaluateStudyLeafLighting(InputData inputData, SurfaceData surface, half leafMask)
{
    #if defined(_FLAT_FOLIAGE_LIGHTING)
        return EvaluateStudyFoliage(inputData, surface);
    #else
        half4 color = 0;
        if (leafMask > 0.5h)
        {
            color = EvaluateStudyFoliage(inputData, surface);
        }
        else
        {
            color = UniversalFragmentPBR(inputData, surface);
        }
        return color;
    #endif
}

#endif
