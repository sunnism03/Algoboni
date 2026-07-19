Shader "TelleR/UltraToon"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.5, 1)

        [Toggle] _ShadingEnabled("Shading Enabled", Float) = 1

        _StepCount("Step Count", Range(2,6)) = 3
        _StepContrast("Step Contrast", Range(0,2)) = 1.2
        _StepOffset("Step Offset", Range(-0.5,0.5)) = 0.0

        _GiGamma("GI Gamma", Range(0.3, 3.0)) = 1.0
        _GiColorBleed("GI Color Bleed", Range(0, 0.5)) = 0.0

        _FakeAoStrength("Fake AO Strength", Range(0, 1)) = 0.0
        _ShadowRichness("Shadow Richness", Range(0, 1)) = 0.0

        [Toggle] _RimEnabled("Rim Enabled", Float) = 1
        _RimColor("Rim Color", Color) = (0.35, 0.45, 0.65, 1)
        _RimPower("Rim Power", Range(1, 8)) = 4.0
        _RimIntensity("Rim Intensity", Range(0, 0.5)) = 0.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    CustomEditor "TelleRUltraToonShaderGUI"

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _StepCount;
                half _StepContrast;
                half _StepOffset;
                half _ShadingEnabled;
                half _GiGamma;
                half _GiColorBleed;
                half _FakeAoStrength;
                half _ShadowRichness;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _RimEnabled;
            CBUFFER_END

            #define ULTRA_COMMON_FAKELIGHT
            #define ULTRA_COMMON_RAMP
            #include "UltraToonCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS    : NORMAL;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half3 gi           : TEXCOORD1;
                half3 normalWS     : TEXCOORD2;
                half3 viewDirWS    : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
                half fogFactor     : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vp.positionCS;
                OUT.positionWS  = vp.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceNormalizeViewDir(vp.positionWS);
                OUT.uv          = IN.uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.gi          = SampleSH(OUT.normalWS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 N = normalize(IN.normalWS);
                half NdotV = saturate(dot(normalize(IN.viewDirWS), N));

                half3 gi = IN.gi;

                half giLum;
                half u = ToonRampU(gi, N.y, _FakeAoStrength, _StepContrast, _StepOffset, _StepCount, _GiGamma, giLum);
                u = lerp(1.0, u, _ShadingEnabled);

                half3 ramp = lerp(_ShadowColor.rgb, half3(1,1,1), u);

                half3 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                half3 col = baseTex * _BaseColor.rgb * ramp;

                if (_ShadowRichness > 0.0)
                {
                    half shadowMask = (1.0 - u) * _ShadowRichness;
                    col *= 1.0 + shadowMask * half3(-0.05, 0.0, 0.1);
                    col = lerp(col, col * 1.15, shadowMask * 0.3);
                }

                if (_GiColorBleed > 0.0)
                {
                    half3 giNorm = gi * rcp(max(giLum, 0.1));
                    col *= lerp(half3(1,1,1), saturate(giNorm), _GiColorBleed);
                }

                if (_RimEnabled * _RimIntensity > 0.0)
                {
                    half rim = pow(1.0 - NdotV, _RimPower) * (1.0 - u * 0.7);
                    col += rim * _RimColor.rgb * _RimIntensity;
                }

                half3 comp = _EmissivePPCompensation.rgb;

                half3 fakeLightAccum = AccumulateFakeLights(IN.positionWS);
                col += fakeLightAccum * baseTex * _BaseColor.rgb * comp;

                col = MixFog(col, IN.fogFactor);
                return half4(saturate(col), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UltraShadowVert
            #pragma fragment UltraShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            #define ULTRA_COMMON_SHADOWCASTER
            #include "UltraToonCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UltraDepthVert
            #pragma fragment UltraDepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define ULTRA_COMMON_DEPTHONLY
            #include "UltraToonCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaVert
            #pragma fragment MetaFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _StepCount;
                half _StepContrast;
                half _StepOffset;
                half _ShadingEnabled;
                half _GiGamma;
                half _GiColorBleed;
                half _FakeAoStrength;
                half _ShadowRichness;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _RimEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings MetaVert(Attributes input)
            {
                Varyings output;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return output;
            }

            half4 MetaFrag(Varyings input) : SV_Target
            {
                MetaInput metaInput;
                metaInput.Albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                metaInput.Emission = 0;
                return MetaFragment(metaInput);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
