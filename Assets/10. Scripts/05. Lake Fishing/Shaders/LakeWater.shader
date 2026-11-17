Shader "StudyXR/Lake Fishing/Lake Water"
{
    Properties
    {
        _WaterColor ("물색", Color) = (0.05, 0.20, 0.22, 1)
        _SkyColor ("반사 — 하늘 위쪽", Color) = (0.40, 0.60, 0.85, 1)
        _HorizonColor ("반사 — 지평선", Color) = (0.76, 0.85, 0.90, 1)
        [Toggle(_REFLECTION_CUBE)] _UseReflectionCube ("반사에 큐브맵 쓰기", Float) = 0
        [NoScaleOffset] _ReflectionCube ("반사 큐브맵 (파노라마)", Cube) = "" {}
        _ReflectionExposure ("큐브맵 밝기", Range(0, 4)) = 1
        _ReflectionRotation ("큐브맵 회전 (도, 스카이박스와 같게)", Float) = 0
        _ReflectionBlur ("큐브맵 흐림 (밉 단계)", Range(0, 6)) = 1
        _ReflectionMax ("큐브맵 값 상한 (밝기를 곱하기 전, 가장 밝은 채널)", Float) = 1000
        _WaveA ("물결 A (방향 x, 방향 z, 높이, 파장)", Vector) = (1, 0.3, 0.012, 3.0)
        _WaveB ("물결 B (방향 x, 방향 z, 높이, 파장)", Vector) = (-0.4, 1, 0.008, 1.9)
        _WaveC ("물결 C (방향 x, 방향 z, 높이, 파장)", Vector) = (0.7, -0.6, 0.005, 1.2)
        _WaveSpeed ("물결 빠르기 (A, B, C, 사용 안 함)", Vector) = (0.9, 0.7, 0.55, 0)
        [NoScaleOffset] _NormalMap ("노멀맵 (바람 물결)", 2D) = "bump" {}
        _NormalTiling ("노멀맵 반복 (1 m 당)", Float) = 0.35
        _NormalScroll ("노멀맵 흐름 (1겹 x, 1겹 z, 2겹 x, 2겹 z)", Vector) = (0.03, 0.01, -0.02, 0.025)
        _NormalStrength ("노멀맵 세기", Range(0, 1)) = 0.35
        _DetailFade ("잔물결이 잦아드는 거리 (시작 m, 끝 m, 끝에서 남는 세기, 사용 안 함)", Vector) = (8, 60, 0.25, 0)
        _FresnelPower ("프레넬 지수", Range(1, 10)) = 5
        _ReflectionStrength ("반사 세기", Range(0, 1)) = 0.9
        _SpecularStrength ("해 반짝임 세기", Range(0, 4)) = 1.5
        _Gloss ("해 반짝임 좁기", Range(16, 1024)) = 400
        _RippleShape ("찌 물결 (파장 m, 퍼지는 빠르기 m/s, 고리 폭 m, 수명 초)", Vector) = (0.16, 0.45, 0.22, 2.6)
        _RippleSlope ("찌 물결 기울기", Range(0, 1)) = 0.35
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
            #pragma multi_compile_fog
            #pragma shader_feature_local _REFLECTION_CUBE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURECUBE(_ReflectionCube);
            SAMPLER(sampler_ReflectionCube);

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half4 _SkyColor;
                half4 _HorizonColor;
                half _ReflectionExposure;
                float _ReflectionRotation;
                half _ReflectionBlur;
                half _ReflectionMax;
                float4 _WaveA;
                float4 _WaveB;
                float4 _WaveC;
                float4 _WaveSpeed;
                float _NormalTiling;
                float4 _NormalScroll;
                half _NormalStrength;
                float4 _DetailFade;
                half _FresnelPower;
                half _ReflectionStrength;
                half _SpecularStrength;
                half _Gloss;
                float4 _RippleShape;
                half _RippleSlope;
            CBUFFER_END

            #define RIPPLE_COUNT 6
            float4 _WaterRipples[RIPPLE_COUNT];

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 slope : TEXCOORD1;
                float4 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float SineWave(float4 wave, float speed, float2 xz, float t, inout float2 slope)
            {
                float2 d = normalize(wave.xy);
                float k = 6.2831853 / wave.w;
                float phase = k * (dot(d, xz) - speed * t);
                slope += wave.z * k * d * cos(phase);
                return wave.z * sin(phase);
            }

            float2 RippleSlope(float2 xz, float4 ripple, float t)
            {
                float age = t - ripple.z;
                float life = _RippleShape.w;
                if (age <= 0.0 || age >= life || ripple.w <= 0.0) return float2(0, 0);

                float2 d = xz - ripple.xy;
                float r = length(d) + 1e-4;
                float x = r - _RippleShape.y * age;
                float width = _RippleShape.z;
                float envelope = exp(-(x * x) / (width * width)) * (1.0 - age / life) * ripple.w;
                envelope *= saturate(r * 6.0);
                float k = 6.2831853 / _RippleShape.x;
                return (d / r) * (envelope * _RippleSlope * cos(k * x));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 p = input.positionOS.xyz;
                float t = _Time.y;
                float2 slope = float2(0, 0);
                float h = SineWave(_WaveA, _WaveSpeed.x, p.xz, t, slope);
                h += SineWave(_WaveB, _WaveSpeed.y, p.xz, t, slope);
                h += SineWave(_WaveC, _WaveSpeed.z, p.xz, t, slope);
                p.y += h;

                output.positionWS = TransformObjectToWorld(p);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.slope = slope;

                float2 baseUV = output.positionWS.xz * _NormalTiling;
                output.uv.xy = baseUV + _NormalScroll.xy * t;
                output.uv.zw = baseUV * 1.37 + _NormalScroll.zw * t;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half3 SampleSky(half3 r)
            {
            #if defined(_REFLECTION_CUBE)
                float a = radians(_ReflectionRotation);
                float s = sin(a), c = cos(a);
                float3 dir = float3(c * r.x + s * r.z, r.y, -s * r.x + c * r.z);
                dir.y = max(dir.y, 0.02);
                half3 sky = SAMPLE_TEXTURECUBE_LOD(_ReflectionCube, sampler_ReflectionCube, dir, _ReflectionBlur).rgb;
                half peak = max(max(sky.r, sky.g), max(sky.b, 1e-4h));
                sky *= min(1.0h, _ReflectionMax / peak);
                return sky * _ReflectionExposure;
            #else
                return lerp(_HorizonColor.rgb, _SkyColor.rgb, saturate(r.y * 3.0h));
            #endif
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 slope = input.slope;
                [unroll] for (int i = 0; i < RIPPLE_COUNT; i++)
                {
                    slope += RippleSlope(input.positionWS.xz, _WaterRipples[i], _Time.y);
                }

                float3 normalWS = TransformObjectToWorldNormal(float3(-slope.x, 1, -slope.y));
                float3 tangentWS = TransformObjectToWorldDir(float3(1, slope.x, 0));
                float3 bitangentWS = TransformObjectToWorldDir(float3(0, slope.y, 1));

                float distance = length(GetCameraPositionWS() - input.positionWS);
                half detail = lerp(1.0h, (half)_DetailFade.z, saturate((distance - _DetailFade.x) / max(_DetailFade.y - _DetailFade.x, 0.01)));
                half strength = _NormalStrength * detail;

                half3 nA = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv.xy), strength);
                half3 nB = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv.zw), strength);
                half3 nTS = normalize(half3(nA.xy + nB.xy, nA.z * nB.z));
                float3 n = normalize(nTS.x * tangentWS + nTS.y * bitangentWS + nTS.z * normalWS);

                Light light = GetMainLight();
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 r = reflect(-v, (half3)n);

                half3 sky = SampleSky(r);
                half ndotl = saturate(dot((half3)n, light.direction));
                half3 body = _WaterColor.rgb * (light.color * ndotl * 0.5h + SampleSH(n));

                half cosV = saturate(dot((half3)n, v));
                half fresnel = (0.02h + 0.98h * pow(1.0h - cosV, _FresnelPower)) * _ReflectionStrength;
                half spec = pow(saturate(dot(r, light.direction)), _Gloss) * _SpecularStrength;

                half3 color = lerp(body, sky, fresnel) + light.color * spec;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
