Shader "StudyXR/MR Aquarium/Fish Swim"
{
    Properties
    {
        _BaseMap ("텍스처", 2D) = "white" {}
        _BaseColor ("색", Color) = (1, 1, 1, 1)
        _SwimAmount ("꼬리 흔들기 (반길이 비율)", Range(0, 0.4)) = 0.15
        _WaveCount ("몸을 지나는 물결 수", Range(0, 2)) = 0.8
        _RimColor ("가장자리 물빛", Color) = (0.35, 0.6, 0.75, 1)
        [HideInInspector] _SwimPhase ("헤엄 위상", Float) = 0
        [HideInInspector] _SwimAxis ("머리 방향(xyz) · 몸 중심(w)", Vector) = (0, 0, 1, 0)
        [HideInInspector] _SwimSide ("옆 방향(xyz) · 반길이(w)", Vector) = (1, 0, 0, 0.5)
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
                float _SwimAmount;
                float _WaveCount;
                half4 _RimColor;
                float _SwimPhase;
                float4 _SwimAxis;
                float4 _SwimSide;
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
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 p = input.positionOS.xyz;
                float halfLength = max(_SwimSide.w, 0.0001);
                float s = (dot(p, _SwimAxis.xyz) - _SwimAxis.w) / halfLength;
                float tail = saturate(0.5 - 0.5 * s);
                float wave = sin(_SwimPhase - s * _WaveCount * PI);
                p += _SwimSide.xyz * (wave * _SwimAmount * halfLength * tail * tail);

                output.positionWS = TransformObjectToWorld(p);
                output.positionCS = TransformWorldToHClip(output.positionWS);
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
                float3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(dot(n, v)), 3.0);
                half3 color = albedo * (light.color * ndotl + SampleSH(n)) + _RimColor.rgb * rim * 0.5;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
