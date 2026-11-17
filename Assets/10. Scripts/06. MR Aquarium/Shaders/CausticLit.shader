Shader "StudyXR/MR Aquarium/Caustic Lit"
{
    Properties
    {
        _BaseMap ("텍스처", 2D) = "white" {}
        _BaseColor ("색", Color) = (1, 1, 1, 1)
        _CausticColor ("빛 무늬 색", Color) = (0.7, 0.95, 1, 1)
        _CausticScale ("무늬 수 (1 m 당)", Range(1, 20)) = 7
        _CausticSpeed ("흐름 빠르기 (m/초)", Range(0, 0.5)) = 0.05
        _CausticSharpness ("선 가늘기", Range(2, 30)) = 10
        _CausticStrength ("밝기", Range(0, 2)) = 0.8
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
                half4 _CausticColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticSharpness;
                float _CausticStrength;
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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float2 Hash2(float2 cell)
            {
                float2 p = float2(dot(cell, float2(127.1, 311.7)), dot(cell, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float CausticLayer(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float d1 = 8.0;
                float d2 = 8.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 diff = g + Hash2(cell + g) - f;
                        float d = dot(diff, diff);
                        if (d < d1) { d2 = d1; d1 = d; }
                        else if (d < d2) { d2 = d; }
                    }
                }
                float edge = sqrt(d2) - sqrt(d1);
                return pow(saturate(1.0 - edge), _CausticSharpness);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                float3 n = normalize(input.normalWS);
                Light light = GetMainLight();
                half ndotl = saturate(dot(n, light.direction));

                float2 xz = input.positionWS.xz * _CausticScale;
                float t = _Time.y * _CausticSpeed * _CausticScale;
                float a = CausticLayer(xz + float2(t, t * 0.6));
                float b = CausticLayer(xz * 1.37 + float2(-t * 0.8, t * 0.3) + 17.0);
                float caustic = min(a, b) * _CausticStrength * saturate(n.y);

                half3 color = albedo * (light.color * ndotl + SampleSH(n)) + _CausticColor.rgb * caustic * (0.4 + 0.6 * ndotl);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
