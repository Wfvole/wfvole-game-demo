Shader "Custom/ToonLitWithLightmapAndOutline"
{
    Properties
    {
        [Header(Base Color)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)

        [Header(Alpha Clipping)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Shading)]
        _CelShadeMidPoint("Cel MidPoint", Range(-1,1)) = -0.5
        _CelShadeSoftness("Cel Softness", Range(0,1)) = 0.05
        _ShadowMapColor("Shadow Color", Color) = (1,0.825,0.78)
        _ReceiveShadowMappingAmount("Shadow Strength", Range(0,1)) = 0.65

        [Header(Indirect Light)]
        _IndirectLightMinColor("Indirect Min Color", Color) = (0.1,0.1,0.1,1)

        [Header(Emission)]
        [Toggle] _UseEmission("Use Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionMulByBaseColor("Multiply Base Color", Range(0,1)) = 0

        [Header(Outline)]
        _OutlineWidth("Outline Width", Range(0,4)) = 1
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineZOffset("Outline ZOffset (View Space)", Range(0,1)) = 0.0001
        [NoScaleOffset] _OutlineZOffsetMaskTex("Outline ZOffset Mask", 2D) = "black" {}
        _OutlineZOffsetMaskRemapStart("Remap Start", Range(0,1)) = 0
        _OutlineZOffsetMaskRemapEnd("Remap End", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        // -------------------------------------
        // ForwardLit Pass (主要渲染，支持光照贴图和阴影)
        // -------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // URP 核心关键词
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 材质属性 (不包含 LitInput.hlsl，手动定义)
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _CelShadeMidPoint;
                half _CelShadeSoftness;
                half3 _ShadowMapColor;
                half _ReceiveShadowMappingAmount;
                half3 _IndirectLightMinColor;
                half _UseEmission;
                half3 _EmissionColor;
                half _EmissionMulByBaseColor;
                float4 _EmissionMap_ST;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float3 normalWS     : TEXCOORD3;
                float4 shadowCoord  : TEXCOORD4;
                float4 positionCS   : SV_POSITION;
                float fogFactor     : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.lightmapUV = input.lightmapUV;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.positionCS = vertexInput.positionCS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            // 卡通单光源计算
            half3 ShadeToonSingleLight(half3 albedo, half3 normalWS, half3 lightDir, half3 lightColor, half shadowAttenuation, half distanceAttenuation, bool isAdditionalLight)
            {
                half NoL = dot(normalWS, lightDir);
                half litOrShadow = smoothstep(_CelShadeMidPoint - _CelShadeSoftness, _CelShadeMidPoint + _CelShadeSoftness, NoL);
                // 阴影衰减
                half shadowAtten = shadowAttenuation > 0.5 ? 1 : 0;
                litOrShadow *= lerp(1, shadowAtten, _ReceiveShadowMappingAmount);
                half3 litOrShadowColor = lerp(_ShadowMapColor, 1, litOrShadow);
                half atten = litOrShadowColor * distanceAttenuation;
                // 附加光源强度降低，避免太亮
                half intensity = isAdditionalLight ? 0.25 : 1;
                return saturate(lightColor) * atten * intensity;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseMap.rgb * _BaseColor.rgb;

                #if defined(_ALPHATEST_ON)
                    clip(baseMap.a * _BaseColor.a - _Cutoff);
                #endif

                // 法线（无需法线贴图，直接使用顶点法线）
                half3 normalWS = normalize(input.normalWS);

                // ---------- 间接光（光照贴图或 Light Probe） ----------
                half3 indirect = 0;
                #if defined(LIGHTMAP_ON)
                    // 采样光照贴图
                    half3 lightmapColor = SampleLightmap(input.lightmapUV, normalWS);
                    indirect = lightmapColor;
                #else
                    // 采样 Light Probe
                    indirect = SampleSH(normalWS);
                #endif
                indirect = max(_IndirectLightMinColor, indirect);

                // ---------- 直接光 ----------
                half3 direct = 0;

                // 主光源
                //Light mainLight = GetMainLight(input.shadowCoord);
                //half3 mainLightResult = ShadeToonSingleLight(albedo, normalWS, mainLight.direction, mainLight.color, mainLight.shadowAttenuation, 1, false);
                Light mainLight = GetMainLight();   // 不传入 shadowCoord
                float3 positionWS = input.positionWS;
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                half shadowAtten = MainLightRealtimeShadow(shadowCoord);
                half3 mainLightResult = ShadeToonSingleLight(albedo, normalWS, mainLight.direction, mainLight.color, shadowAtten, 1, false);
                direct += mainLightResult;

                // 附加光源
                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalLightsCount = GetAdditionalLightsCount();
                    for (uint i = 0; i < additionalLightsCount; i++)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);
                        half3 addLightResult = ShadeToonSingleLight(albedo, normalWS, light.direction, light.color, light.shadowAttenuation, light.distanceAttenuation, true);
                        direct += addLightResult;
                    }
                #endif

                // ---------- 自发光 ----------
                half3 emission = 0;
                if (_UseEmission)
                {
                    half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                    emission = emissionMap * _EmissionColor.rgb;
                    if (_EmissionMulByBaseColor > 0)
                        emission *= albedo;
                }

                // 合成
                half3 color = albedo * (indirect + direct) + emission;
                color = MixFog(color, input.fogFactor);
                return half4(color, baseMap.a * _BaseColor.a);
            }

            ENDHLSL
        }

        // -------------------------------------
        // Outline Pass (轮廓线)
        // -------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _OutlineWidth;
                half4 _OutlineColor;
                half _OutlineZOffset;
                sampler2D _OutlineZOffsetMaskTex;
                float4 _OutlineZOffsetMaskTex_ST;
                half _OutlineZOffsetMaskRemapStart;
                half _OutlineZOffsetMaskRemapEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // 辅助函数 (从原文件复制)
            float GetCameraFOV()
            {
                float t = unity_CameraProjection._m11;
                float Rad2Deg = 180 / 3.1415;
                float fov = atan(1.0f / t) * 2.0 * Rad2Deg;
                return fov;
            }

            float ApplyOutlineDistanceFadeOut(float inputMulFix)
            {
                return saturate(inputMulFix);
            }

            float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS_Z)
            {
                float cameraMulFix;
                if(unity_OrthoParams.w == 0)
                {
                    cameraMulFix = abs(positionVS_Z);
                    cameraMulFix = ApplyOutlineDistanceFadeOut(cameraMulFix);
                    cameraMulFix *= GetCameraFOV();
                }
                else
                {
                    float orthoSize = abs(unity_OrthoParams.y);
                    orthoSize = ApplyOutlineDistanceFadeOut(orthoSize);
                    cameraMulFix = orthoSize * 50;
                }
                return cameraMulFix * 0.00005;
            }

            float4 NiloGetNewClipPosWithZOffset(float4 originalPositionCS, float viewSpaceZOffsetAmount)
            {
                if(unity_OrthoParams.w == 0)
                {
                    float2 ProjM_ZRow_ZW = UNITY_MATRIX_P[2].zw;
                    float modifiedPositionVS_Z = -originalPositionCS.w + -viewSpaceZOffsetAmount;
                    float modifiedPositionCS_Z = modifiedPositionVS_Z * ProjM_ZRow_ZW[0] + ProjM_ZRow_ZW[1];
                    originalPositionCS.z = modifiedPositionCS_Z * originalPositionCS.w / (-modifiedPositionVS_Z);
                    return originalPositionCS;
                }
                else
                {
                    originalPositionCS.z += -viewSpaceZOffsetAmount / _ProjectionParams.z;
                    return originalPositionCS;
                }
            }

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                float outlineExpandAmount = _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(vertexInput.positionVS.z);
                float3 positionWS = vertexInput.positionWS + normalInput.normalWS * outlineExpandAmount;
                output.positionCS = TransformWorldToHClip(positionWS);

                // ZOffset mask
                float outlineZOffsetMask = tex2Dlod(_OutlineZOffsetMaskTex, float4(input.uv, 0, 0)).r;
                outlineZOffsetMask = 1 - outlineZOffsetMask;
                outlineZOffsetMask = saturate((outlineZOffsetMask - _OutlineZOffsetMaskRemapStart) / (_OutlineZOffsetMaskRemapEnd - _OutlineZOffsetMaskRemapStart));
                output.positionCS = NiloGetNewClipPosWithZOffset(output.positionCS, _OutlineZOffset * outlineZOffsetMask);

                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }

        // -------------------------------------
        // ShadowCaster Pass
        // -------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // -------------------------------------
        // DepthOnly Pass
        // -------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // -------------------------------------
        // Meta Pass (Lightmap baking)
        // -------------------------------------
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}