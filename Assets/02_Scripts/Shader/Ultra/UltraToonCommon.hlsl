#ifndef ULTRA_TOON_COMMON_INCLUDED
#define ULTRA_TOON_COMMON_INCLUDED

#ifdef ULTRA_COMMON_FAKELIGHT
#define MAX_FAKE_LIGHTS 8
float4 _FakeLightPositions[MAX_FAKE_LIGHTS];
float4 _FakeLightParams[MAX_FAKE_LIGHTS];
half4  _FakeLightColors[MAX_FAKE_LIGHTS];
int    _FakeLightCount;
half4  _EmissivePPCompensation;

half3 AccumulateFakeLights(float3 positionWS)
{
    half3 accum = half3(0, 0, 0);
    for (int fl = 0; fl < _FakeLightCount; fl++)
    {
        float3 d = positionWS - _FakeLightPositions[fl].xyz;
        float distSq = dot(d, d);
        if (distSq >= _FakeLightParams[fl].x) continue;

        half t = saturate(1.0 - sqrt(distSq) * _FakeLightParams[fl].w);
        half atten = _FakeLightParams[fl].y * pow(t, _FakeLightParams[fl].z);
        accum += _FakeLightColors[fl].rgb * atten;
    }
    return accum;
}
#endif

#ifdef ULTRA_COMMON_RAMP
half ToonRampU(half3 gi, half normalY, half fakeAoStrength, half stepContrast, half stepOffset, half stepCount, half giGamma, out half giLum)
{
    giLum = dot(gi, half3(0.299, 0.587, 0.114));
    giLum = max(giLum, 0.35);
    if (giGamma != 1.0)
        giLum = pow(giLum, giGamma);

    half ao = saturate(normalY * 0.5 + 0.5);
    half combined = giLum * lerp(1.0, ao, fakeAoStrength);

    half u = saturate(combined * stepContrast + stepOffset);
    half invSteps = rcp(stepCount - 1.0);
    half scaled = u * (stepCount - 1.0);
    half stepped = floor(scaled + 0.5);
    half fw = fwidth(scaled);
    return lerp(stepped, scaled, smoothstep(0.0, 1.0, fw)) * invSteps;
}
#endif

half3 ApplyLuminanceKnee(half3 col)
{
    half lum = dot(col, half3(0.299, 0.587, 0.114));
    if (lum > 1.0)
    {
        half tonedLum = 2.0 - 1.0 / lum;
        col *= tonedLum / lum;
    }
    return col;
}

#ifdef ULTRA_COMMON_GRASS_WIND
float3 GrassWindSway(float3 posOS, half uvY, half phaseJitter, half tipFlutter, out half tipMask)
{
    half h01 = saturate(uvY);
    tipMask = pow(h01, _GradientPower);
    half bendCurve = h01 * h01;
    half hAbs = h01 * _BladeHeight;

    float3 worldPos = TransformObjectToWorld(posOS);
    float t = _Time.y * (float)_WindSpeed;

    float2 dir = normalize(float2(_WindDirection.x, _WindDirection.y) + float2(0.0001, 0));
    float2 perp = float2(-dir.y, dir.x);

    float along = dot(float2(worldPos.x, worldPos.z), dir);
    float across = dot(float2(worldPos.x, worldPos.z), perp);

    float2 cell = floor(worldPos.xz * 0.7);
    float cellPhase = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453) * 6.2831853 * phaseJitter;

    float mainPhase = t + along * (float)_WindFreq + cellPhase;
    float perpPhase = t * 0.55 + across * (float)_WindFreq * 0.5 + cellPhase;
    float gustPhase = _Time.y * (float)_WindGustSpeed + along * 0.18;
    float flutterPhase = t * 3.7 + along * (float)_WindFreq * 2.0 + cellPhase;

    mainPhase = mainPhase - 6.2831853 * floor(mainPhase * 0.1591549);
    perpPhase = perpPhase - 6.2831853 * floor(perpPhase * 0.1591549);
    gustPhase = gustPhase - 6.2831853 * floor(gustPhase * 0.1591549);
    flutterPhase = flutterPhase - 6.2831853 * floor(flutterPhase * 0.1591549);

    half primary = (half)sin(mainPhase);
    half lateralWave = (half)sin(perpPhase) * _WindLateralAmount;
    half gustWave = (half)sin(gustPhase) * 0.5 + 0.5;
    half gust = 1.0 - _WindGustAmount + gustWave * (2.0 * _WindGustAmount);
    half flutter = (half)sin(flutterPhase) * tipFlutter * (h01 * h01 * h01);

    half bendAmount = _WindStrength * bendCurve * gust;
    half lateralX = primary * bendAmount + flutter;
    half lateralZ = lateralWave * bendAmount;

    posOS.x += (half)dir.x * lateralX + (half)perp.x * lateralZ;
    posOS.z += (half)dir.y * lateralX + (half)perp.y * lateralZ;

    half lateralSqr = lateralX * lateralX + lateralZ * lateralZ;
    posOS.y -= lateralSqr / max(hAbs, 0.05);

    return posOS;
}
#endif

#ifdef ULTRA_COMMON_SHADOWCASTER
float3 _LightDirection;
float3 _LightPosition;

float4 UltraShadowCasterHClip(float3 posWS, half3 normalWS)
{
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        float3 lightDir = normalize(_LightPosition - posWS);
    #else
        float3 lightDir = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, lightDir));

    #if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

struct UltraShadowAttributes
{
    float4 positionOS : POSITION;
    half3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UltraShadowVaryings
{
    float4 positionCS : SV_POSITION;
};

UltraShadowVaryings UltraShadowVert(UltraShadowAttributes input)
{
    UltraShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);

    float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
    half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.positionCS = UltraShadowCasterHClip(posWS, normalWS);
    return output;
}

half4 UltraShadowFrag() : SV_TARGET { return 0; }
#endif

#ifdef ULTRA_COMMON_DEPTHONLY
struct UltraDepthAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UltraDepthVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

UltraDepthVaryings UltraDepthVert(UltraDepthAttributes input)
{
    UltraDepthVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 UltraDepthFrag(UltraDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return 0;
}
#endif

#endif
