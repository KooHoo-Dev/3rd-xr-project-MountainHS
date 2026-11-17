Shader "StudyXR/MR Aquarium/Kelp Sway"
{
    Properties
    {
        _BaseMap ("텍스처", 2D) = "white" {}
        _BaseColor ("색", Color) = (1, 1, 1, 1)
        _Height ("모델 높이 (물체 공간)", Float) = 0.45
        _SwayAmount ("흔들림 (물체 공간 m)", Range(0, 0.2)) = 0.04
        _SwaySpeed ("빠르기", Range(0, 4)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

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
                float _Height;
                float _SwayAmount;
                float _SwaySpeed;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 p = input.positionOS.xyz;
                float h = saturate(p.y / max(_Height, 0.001));
                float bend = h * h;
                float3 originWS = GetObjectToWorldMatrix()._m03_m13_m23;
                float phase = _Time.y * _SwaySpeed + dot(originWS, float3(3.1, 0.0, 2.3));
                p.x += sin(phase) * _SwayAmount * bend;
                p.z += sin(phase * 0.7 + 1.3) * _SwayAmount * 0.6 * bend;

                output.positionCS = TransformObjectToHClip(p);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                float3 n = normalize(input.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                Light light = GetMainLight();
                half ndotl = saturate(dot(n, light.direction));
                half3 color = albedo * (light.color * ndotl + SampleSH(n));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
