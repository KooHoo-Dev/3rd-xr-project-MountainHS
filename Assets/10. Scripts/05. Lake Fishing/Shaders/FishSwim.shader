Shader "StudyXR/Lake Fishing/Fish Swim"
{
    Properties
    {
        [MainTexture] _BaseMap ("텍스처", 2D) = "white" {}
        [MainColor] _BaseColor ("색", Color) = (1, 1, 1, 1)
        _HeadAxis ("머리 방향 (메시 공간)", Vector) = (0, 0, 1, 0)
        _SideAxis ("옆 방향 (메시 공간)", Vector) = (1, 0, 0, 0)
        _BodyCenter ("몸 가운데 (메시 공간)", Vector) = (0, 0, 0, 0)
        _HalfLength ("몸길이의 절반 (메시 단위)", Float) = 0.5
        _Amplitude ("꼬리 진폭 (몸길이 절반에 대한 비율)", Range(0, 0.6)) = 0.25
        _WaveNumber ("몸에 실린 물결 (라디안)", Range(0, 8)) = 3
        _SwimSpeed ("파닥이는 빠르기 (라디안/초)", Float) = 14
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _HeadAxis;
                float4 _SideAxis;
                float4 _BodyCenter;
                float _HalfLength;
                float _Amplitude;
                float _WaveNumber;
                float _SwimSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 p = input.positionOS.xyz;
                float along = dot(p - _BodyCenter.xyz, _HeadAxis.xyz) / max(_HalfLength, 0.0001);
                float tail = saturate((0.4 - along) / 1.4);
                tail *= tail;
                p += _SideAxis.xyz * (_Amplitude * _HalfLength * tail * sin(_Time.y * _SwimSpeed + along * _WaveNumber));

                output.positionCS = TransformObjectToHClip(p);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                float3 n = normalize(input.normalWS);
                Light light = GetMainLight();
                half ndotl = saturate(dot(n, light.direction));
                half3 color = albedo * (light.color * ndotl + SampleSH(n));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
