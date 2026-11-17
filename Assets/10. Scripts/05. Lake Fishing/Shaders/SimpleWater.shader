// "간단한 물 셰이더"를 만들어 봅시다.
// 평평한 격자 메시를 물처럼 보이게 하기 위해 아래의 기능이 들어갑니다.
// 1. 높이 : 정점 셰이더가 사인파 세 개를 더한 만큼 정점을 위 아래로 움직이게 합니다.
// 2. 법선(노말) : 같은 사인파를 잘게 잘라서 픽셀마다 물결 면의 방향(법선)을 구합니다.
// 3. 프레넬 : 비스듬히 볼수록 하늘(광량)이 많이 비치고,  내려다 볼수록 물속이 보입니다.
// 4. 반사광 : 해와 눈 사이 가운데 방향(하프 벡터)와 법선이 겹치는 곳만 반짝이게 처리합니다.

// 원본은 세이더 갤러리에 있습니다.
// PS : 강의용으로 두 파일을 합쳐놓은거서, 실제로 사용하기에는 적절치 않습니다.
//     셰이더 갤러리것을 사용하십시요 

Shader "StudyXR/Practice/My Water"
{
    // Properties : Material을 클릭하면 Inspector에 노출되는 값.
    //  이름("_DeepColor")가 아래 HLSL CBUFFER에 선언된 변수 이름과 같아야 연결이 됩니다. 
    Properties
    {
        [Header(Wave)]
        _WaveHeight ("파도 높이", Range(0,0.15)) = 0.04
        _WaveLength ("파장", Range(0.5, 8)) = 2.2
        _WaveSpeed ("파도 속도", Range(0,3)) = 0.8
        _WaveAngle ("파도 방향", Range(0,360)) = 225
        _FadeDistance("물결이 사라지는 거리", Range(5,200)) = 50
        _WaveMask("파도 더하기", Vector) = (1,1,1,0)
        _VertexWave("정점 밀기", Float) = 1
        
        [Header(Color)]
        _DeepColor ("물빛 (내려다볼 때)", Color) = (0.016, 0.165, 0.271, 1)
        _SkyColor ("하늘빛", Color) = (0.439, 0.643, 0.8, 1)
        _BaseAlpha ("내려다볼 때 불투명도", Range(0, 1)) = 0.35

        [Header(Specular)]
        _SpecStrength ("반사광 세기", Range(0, 3)) = 1.2
        _SpecPower ("반사광 날카로움", Range(4, 512)) = 200
        
    }

    SubShader
    {
        // 이 셰이더가 갖고 있는 Tag입니다. 유형이라고 생각하시면 좋을 것같아요
        // RenderType, Queue = 어떻게 렌더링이 될 건지
        // RenderPipeline = 어떤 타입의 렌더 파이프라인에서 동작하는지.
        //Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Pass 
        // : GPU에게 전달할 작업 묶음입니다. 드로우콜을 실행하는 단위가 됩니다
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 알파 혼합 : 결과 = 이 색 X 알파 + 이미 그려진 색 x (1-알파).
            // - 카메라의 위치와 각도에 따라 어떨때는 투명하게 비치고
            // - 이미 그려진(흰색, 태양광 반사)색이 있을경우 알파가 빠집니다.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off // 투명 물체는 깊이를 쓰지 않습니다. 
             
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // VR(Single Pass Instanced)는 두 눈을 한번에 그립니다. 요 줄이랑 Varyings.UNITY_VERTEX_OUTPUT_STEREO 들어가있는것은
            // VR 케이스라고 보시면 됩니다
            #pragma multi_compile_instancing

            // include : C#에서 using namespace를 사용하는것과 유사합니다.
            // 결국 Core.hlsl과 Lighting.hlsl을 사용하겠다는 선언 입니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // CBUFFER 
            // : 상수 버퍼(Constant Buffer) OS에서도 있는 개념. 바뀌지않을 
            // 데이터를 빠르게 전달하기 위해 묵어두는 메모리 버퍼.
            
            // Inspector를 이용해서 바꿀 Material의 변수들은
            // Properties와 CBUFFER_START <-> CBUFFER_END 영역에 동시에 선언한다.
            
            CBUFFER_START(UnityPerMaterial)
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                float _WaveAngle;
                float _FadeDistance;
                float4 _DeepColor;
                float4 _SkyColor;
                float _BaseAlpha;
                float _SpecStrength;
                float _SpecPower;
                float _Step;
                float4 _WaveMask;
                float _VertexWave;
            CBUFFER_END

            // 메시에서 정점마다 들어오느 값.
            // 들어오는 값인데 Input으로 들어오는 값
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 정점 셰이더 -> 픽셀셰이더로 넘기는 값.
            // PS : 픽셀 셰이더에서는 정점들을 섞어서 보간하여 화면 출력합니다. 잊지 마십쇼
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fade : TEXCOORD1;
                float2 waveXZ : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // 타우라는 상수를 하나 선언합니다.
            // 원의 둘레에서 반지름 나눈 수학 상수 입니다.
            // (파이의 2배임)
            #define TAU 6.2831853

            // 파도 하나
            float3 Wave(float2 xz, float angleDeg, float waveLength, float amplitude, float speed)
            {
                float a = radians(angleDeg);
                float2 dir = float2(sin(a), cos(a));
                float k = TAU / waveLength;
                float phase = k * (dot(dir, xz) - speed * _Time.y);
                float slope = amplitude * k * cos(phase);
                return float3(amplitude * sin(phase), slope * dir.x, slope * dir.y);
            }
            
            // 파도 3개의 합
            // : 하나만 쓰면 골판지처럼 보이니까, 방향을 엇갈린 파도를 더해서 자연스럽게 만들어 줍니다.
            // 첫번째 계산한 파도를 기준으로 두번째, 세번째 파도를 각도, 파도 길이, 파도 높이에 변화를 줘서
            // 첫번째에 보정해서 넣어줍니다.
            float3 WaveSum(float2 xz)
            {
                float3 w1 = Wave(xz, _WaveAngle, _WaveLength, _WaveHeight, _WaveSpeed) * _WaveMask.x ;
                w1 += Wave(xz, _WaveAngle + 55.0, _WaveLength * 0.7, _WaveHeight * 0.6, _WaveSpeed * 0.8) * _WaveMask.y;
                w1 += Wave(xz, _WaveAngle - 45.0, _WaveLength * 0.58, _WaveHeight * 0.4, _WaveSpeed * 0.7) * _WaveMask.z;
                return w1;
            }
            
            // 정점 셰이딩을 처리하는 함수입니다.
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // 오브젝트 좌표 -> 월드 좌표 -> 파도를 월드 xz로 계산하므로 메시를 어디에 놓아도 물결이 이어집니다.
                // TransformObjectToWorld
                // : 매개변수의 좌표를 World로 변환해 줍니다.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // 카메라에서 거리가 멀수록 물결을 줄인다.
                output.fade = saturate(1.0f - distance(positionWS, GetCameraPositionWS()) / _FadeDistance);
                // 파도를 계산할 xz 위치
                output.waveXZ = positionWS.xz - TransformObjectToWorld(float3(0, 0, 0)).xz;
                // 파도 높이만큼 y를 위로 민다. x에 진폭이 담겨 있음
                positionWS.y += WaveSum(output.waveXZ).x * output.fade * _VertexWave;
                
                output.positionWS = positionWS;
                // 월드 좌표 -> 화면(클립) 좌표로 변환
                output.positionCS = TransformWorldToHClip(positionWS);
                
                return output;
            }

            // 픽셀 셰이딩을 처리하는 함수입니다.
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // 정점은 y만 밀었고, 화면에 렌더링되는 단계에서 나머지 xz와 사라짐을 처리해줍니다.
                float3 w = WaveSum(input.waveXZ) * input.fade;
                
                // 법선(노말) 계산
                // w의 y,z에는 기울기가 들어가 있습니다.
                float3 n = normalize(float3(-w.y, 1.0, -w.z));
                
                // 프레넬 계산
                float3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = 0.02 + 0.98 * pow(1.0 - saturate(dot(n, v)), 5.0);
                
                // 반사광 계산
                Light light = GetMainLight();
                float3 halfDir = normalize(light.direction + v);
                float spec = pow(saturate(dot(n, halfDir)), _SpecPower) * _SpecStrength;
                float3 specColor = light.color * spec;
                
                // 적용후 리턴
                float3 color = lerp(_DeepColor.rgb, _SkyColor.rgb, fresnel) + specColor;
                float alpha = saturate(_BaseAlpha + fresnel * 1.2 + max(specColor.r, max(specColor.g, specColor.b)));
                return half4(color, alpha);
            }
            
            ENDHLSL
        }
    }
}