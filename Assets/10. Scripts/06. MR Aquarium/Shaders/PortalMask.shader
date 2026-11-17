Shader "StudyXR/MR Aquarium/Portal Mask"
{
    Properties
    {
        _BaseColor ("색 (마스크 역할에서는 쓰지 않음)", Color) = (1, 1, 1, 1)
        [IntRange] _StencilRef ("스텐실 값", Range(0, 15)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("스텐실 비교", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("통과하면", Float) = 2
        [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("색 쓰기", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("깊이 쓰기", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }
            ColorMask [_ColorMask]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _StencilRef;
                float _StencilComp;
                float _StencilPass;
                float _ColorMask;
                float _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                Light light = GetMainLight();
                half ndotl = saturate(dot(n, light.direction));
                half3 color = _BaseColor.rgb * (light.color * ndotl + SampleSH(n));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
