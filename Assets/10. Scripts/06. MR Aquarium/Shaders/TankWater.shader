Shader "StudyXR/MR Aquarium/Tank Water"
{
    Properties
    {
        _DeepColor ("바닥 쪽 물빛", Color) = (0.02, 0.10, 0.20, 1)
        _ShallowColor ("수면 쪽 물빛", Color) = (0.10, 0.42, 0.55, 1)
        _BackShade ("뒤쪽 어둡게 (껍데기)", Range(0, 1)) = 0.55
        _Alpha ("불투명도", Range(0, 1)) = 1
        _ShaftStrength ("빛줄기 세기", Range(0, 1)) = 0
        _ShaftCount ("빛줄기 수 (1 m 당)", Range(1, 12)) = 5
        _ShaftSpeed ("빛줄기 흐름", Range(0, 1)) = 0.15
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("버릴 면", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("섞기 (그릴 색)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("섞기 (있던 색)", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("깊이 쓰기", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float _BackShade;
                float _Alpha;
                float _ShaftStrength;
                float _ShaftCount;
                float _ShaftSpeed;
                float _Cull;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t = saturate(input.positionOS.y + 0.5);
                float back = saturate(input.positionOS.z + 0.5);
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, t);
                color *= lerp(1.0, _BackShade, back);

                float wobble = sin(input.positionOS.y * 2.0 + _Time.y * _ShaftSpeed * 3.0) * 0.8;
                float shaft = sin(input.positionOS.x * _ShaftCount * 6.2832 + wobble + _Time.y * _ShaftSpeed);
                shaft = pow(saturate(shaft * 0.5 + 0.5), 6.0) * t * _ShaftStrength;
                color += _ShallowColor.rgb * shaft;

                return half4(color, saturate(_Alpha + shaft * 0.5));
            }
            ENDHLSL
        }
    }
}
