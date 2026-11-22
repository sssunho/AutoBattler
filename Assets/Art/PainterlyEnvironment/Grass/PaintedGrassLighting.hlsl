#ifndef RAGGED_BANNER_PAINTED_GRASS_LIGHTING_INCLUDED
#define RAGGED_BANNER_PAINTED_GRASS_LIGHTING_INCLUDED

half PaintedLuminance(half3 color)
{
    return dot(color, half3(0.2126, 0.7152, 0.0722));
}

half PaintedGrassRawDiffuse(float3 normalWS, half3 lightDirection)
{
    half wrappedDiffuse = saturate((dot(normalWS, lightDirection) + _LightWrap) / (1.0h + _LightWrap));
    half massDiffuse = saturate((lightDirection.y + _LightWrap) / (1.0h + _LightWrap));
    return lerp(massDiffuse, wrappedDiffuse, _NormalInfluence);
}

half PaintedGrassDiffuse(float3 normalWS, half3 lightDirection)
{
    half diffuse = PaintedGrassRawDiffuse(normalWS, lightDirection);
    half steppedDiffuse = floor(diffuse * 4.0h + 0.5h) * 0.25h;
    return lerp(diffuse, steppedDiffuse, 0.45h);
}

#if defined(PAINTED_GRASS_BAND_LIGHTING)
half3 PaintedGrassBandLighting(float3 normalWS, Light light)
{
    float illumination = PaintedGrassRawDiffuse(normalWS, light.direction) * saturate(light.shadowAttenuation);
    float litThreshold = clamp(_GrassBandLitThreshold, 0.01, 0.99);
    float midThreshold = clamp(_GrassBandMidThreshold, 0.001, litThreshold - 0.001);
    float halfWidth = min(max(_GrassBandSoftness * 0.5, fwidth(illumination) * 0.5), (litThreshold - midThreshold) * 0.45);
    float midLowerEdge = max(0.0001, midThreshold - halfWidth);
    float midUpperEdge = max(midLowerEdge + 0.0001, min(1.0, midThreshold + halfWidth));
    float litLowerEdge = max(0.0001, litThreshold - halfWidth);
    float litUpperEdge = max(litLowerEdge + 0.0001, min(1.0, litThreshold + halfWidth));
    half midBand = smoothstep(midLowerEdge, midUpperEdge, illumination);
    half litBand = smoothstep(litLowerEdge, litUpperEdge, illumination);
    half3 bandTint = lerp(lerp(_GrassBandShadeColor.rgb, _GrassBandMidColor.rgb, midBand), _GrassBandLitColor.rgb, litBand);
    return bandTint * light.color * light.distanceAttenuation;
}
#endif

half3 EvaluatePaintedGrass(float3 positionWS, float3 normalWS, float4 positionCS, half height, half3 source)
{
    float2 paintUV = positionWS.xz * max(_PaintScale, 0.001) * _PaintMap_ST.xy + _PaintMap_ST.zw;
    paintUV = 1.0 - abs(frac(paintUV * 0.5) * 2.0 - 1.0);
    half3 paint = SAMPLE_TEXTURE2D(_PaintMap, sampler_TrilinearClamp, paintUV).rgb;
    half paintValue = saturate((sqrt(saturate(PaintedLuminance(paint))) - 0.25h) * 2.0h);
    half leafValue = sqrt(saturate(PaintedLuminance(source)));
    half massValue = saturate(height + (paintValue - 0.5h) * _PaintStrength + (leafValue - 0.5h) * _LeafDetail);
    half3 albedo = lerp(_LowColor.rgb, _MidColor.rgb, smoothstep(0.0h, 0.55h, massValue));
    albedo = lerp(albedo, _HighColor.rgb, smoothstep(0.5h, 1.0h, massValue));
    albedo *= _BaseColor.rgb;

    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(positionWS));
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    #endif
    Light light = GetMainLight(shadowCoord, positionWS, half4(1, 1, 1, 1));
    half3 lighting;
    #if defined(PAINTED_GRASS_BAND_LIGHTING)
    if (_GrassBandLighting > 0.5h)
    {
        lighting = PaintedGrassBandLighting(normalWS, light);
    }
    else
    #endif
    {
        half diffuse = PaintedGrassDiffuse(normalWS, light.direction);
        half illumination = diffuse * light.shadowAttenuation * light.distanceAttenuation;
        const half referenceLightLuminance = 1.25h;
        half shadowBrightness = max(PaintedLuminance(light.color), 0.0h) / referenceLightLuminance * saturate(light.distanceAttenuation);
        lighting = lerp(_ShadowColor.rgb * shadowBrightness, _LightColor.rgb * light.color, illumination);
    }
    #if defined(_ADDITIONAL_LIGHTS)
        InputData inputData = (InputData)0;
        inputData.positionWS = positionWS;
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
            half additionalDiffuse = PaintedGrassDiffuse(normalWS, additionalLight.direction);
            float attenuation = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
            half3 additionalLighting = _LightColor.rgb * additionalLight.color * (additionalDiffuse * attenuation);
            lighting += additionalLighting;
        LIGHT_LOOP_END
    #endif
    lighting += _PainterlyAmbientColor.rgb;
    return albedo * lighting;
}

#endif
