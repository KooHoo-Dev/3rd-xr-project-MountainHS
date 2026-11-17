// ─────────────────────────────────────────────
// OptimizationSwitcher — Quest 최적화 네 가지(그리는 거리 · 그림자 거리 · 렌더 해상도 · 포비티드)를 실행 중에 하나씩 켜고 끈다
// 학습 포인트
//  - 네 가지 모두 실행 중에 바꿀 수 있는 값입니다. 같은 자리 · 같은 시야에서 켜고 끄며 프레임 차이를 바로 봅니다
//  - 그림자 거리 · 렌더 스케일은 씬이 아니라 파이프라인 에셋(Mobile_RPAsset)의 값입니다. 씬을 나갈 때 · 앱을 끌 때 원래 값으로 되돌려야
//    다른 씬과 프로젝트 파일이 바뀌지 않습니다
//  - 포비티드 렌더링은 XR 디스플레이가 돌고 있을 때(기기)만 동작합니다. 그리고 OpenXR 프로젝트 설정에서 Foveated Rendering
//    피처가 켜져 있고 API 가 SRP Foveation 이어야 단계 값이 실제로 먹힙니다. 아니면 '프로젝트 설정에서 꺼짐'으로 막습니다
//    (값을 써도 무시되는데 안내판에 '켜짐'이 뜨면 가짜 결과가 됩니다)
// 조작
//  - 없음 (OptimizationBoard 가 Quest 버튼으로 부름)
// ─────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace StudyXR.ScenePort
{
    public class OptimizationSwitcher : MonoBehaviour
    {
        public const int ItemCount = 4;

        [Header("연결")]
        [SerializeField] private Camera xrCamera;

        [Header("켰을 때 값")]
        [SerializeField] private float farClip = 250f;                         // m
        [SerializeField] private float fogStart = 60f;                         // m — 먼 거리 바로 앞에서 완전히 가림
        [SerializeField] private float fogEnd = 230f;
        [SerializeField] private float shadowDistance = 20f;                   // m
        [SerializeField, Range(0.5f, 1f)] private float renderScale = 0.8f;
        [SerializeField, Range(0f, 1f)] private float foveationLevel = 0.66f;

        [Header("시작")]
        [SerializeField] private bool startAllOn;                              // 끔 = 원본에서 시작 (켜면서 얻는 것을 봄)

        private readonly bool[] on = new bool[ItemCount];
        private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        private UniversalRenderPipelineAsset pipeline;
        private bool captured;
        private float originalFar;
        private float originalFogStart;
        private float originalFogEnd;
        private float originalShadow;
        private float originalScale;
        private float originalFoveation;

        public event Action Changed;

        public int OnCount
        {
            get
            {
                int count = 0;
                foreach (bool value in on) if (value) count++;
                return count;
            }
        }

        // 지금 켤 수 있는 항목 수. '전체 켜짐'은 이 수만큼 켜진 상태입니다 (쓸 수 없는 항목은 켜지 않음)
        public int AvailableCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ItemCount; i++) if (IsAvailable((OptimizationItem)i)) count++;
                return count;
            }
        }

        private void OnEnable()
        {
            Capture();
            Application.quitting += Restore;
            SetAll(startAllOn);
        }

        // 씬을 나갈 때(런처로) · Play 를 멈출 때 · 앱을 끌 때 모두 원래 값으로. Restore 는 두 번 불려도 한 번만 일함
        private void OnDisable()
        {
            Application.quitting -= Restore;
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }

        public bool IsOn(OptimizationItem item) => on[(int)item];

        public bool IsAvailable(OptimizationItem item)
        {
            if (item == OptimizationItem.DrawDistance) return xrCamera != null;
            if (item == OptimizationItem.Foveation) return RunningDisplay() != null && FoveationConfigured();
            return pipeline != null;
        }

        // 쓸 수 없는 항목을 안내판에 적는 말
        public string UnavailableText(OptimizationItem item)
        {
            if (item != OptimizationItem.Foveation) return "없음";
            return RunningDisplay() == null ? "기기에서만" : "프로젝트 설정에서 꺼짐";
        }

        // OpenXR 설정의 Foveated Rendering 피처가 켜져 있고 API 가 SRP Foveation 일 때만 XRDisplaySubsystem 의 단계가 적용됩니다
        // (Legacy API 는 Meta Core XR SDK 의 OVRManager 경로라 이 프로젝트에서는 단계를 바꿀 수 없음)
        private static bool FoveationConfigured()
        {
            OpenXRSettings settings = OpenXRSettings.Instance;
            if (settings == null) return false;
            FoveatedRenderingFeature feature = settings.GetFeature<FoveatedRenderingFeature>();
            return feature != null && feature.enabled
                && settings.foveatedRenderingApi == OpenXRSettings.BackendFovationApi.SRPFoveation;
        }

        public void SetAll(bool value)
        {
            if (!captured) return;
            for (int i = 0; i < ItemCount; i++)
            {
                on[i] = value && IsAvailable((OptimizationItem)i);   // 쓸 수 없는 항목은 켜지 않음
                Apply((OptimizationItem)i);
            }
            Debug.Log($"[ScenePort] 최적화 전체 {(value ? "켜짐" : "꺼짐")}");
            if (Changed != null) Changed();
        }

        public void SetOn(OptimizationItem item, bool value)
        {
            if (!captured) return;
            bool available = IsAvailable(item);
            on[(int)item] = value && available;
            Apply(item);
            Debug.Log($"[ScenePort] 최적화 {item} : {(on[(int)item] ? "켜짐" : "꺼짐")} · {(available ? ValueText(item, on[(int)item]) : UnavailableText(item))}");
            if (Changed != null) Changed();
        }

        // 안내판에 쓰는 값. state = 켰을 때 값인지
        public string ValueText(OptimizationItem item, bool state)
        {
            switch (item)
            {
                case OptimizationItem.DrawDistance:
                    return state ? $"{farClip:F0} m · 안개 {fogStart:F0}~{fogEnd:F0} m"
                                 : $"{originalFar:F0} m · 안개 {originalFogStart:F0}~{originalFogEnd:F0} m";
                case OptimizationItem.ShadowDistance:
                    return $"{(state ? shadowDistance : originalShadow):F0} m";
                case OptimizationItem.RenderScale:
                    return $"{(state ? renderScale : originalScale) * 100f:F0} %";
                default:
                    return $"단계 {(state ? foveationLevel : originalFoveation):F2}";
            }
        }

        private void Apply(OptimizationItem item)
        {
            bool value = on[(int)item];
            switch (item)
            {
                case OptimizationItem.DrawDistance:
                    // 먼 거리를 줄이면 그 밖은 아예 그리지 않습니다(컬링). 잘린 경계가 보이지 않게 안개를 먼 거리 앞에서 짙게 합니다
                    if (xrCamera != null) xrCamera.farClipPlane = value ? farClip : originalFar;
                    RenderSettings.fogStartDistance = value ? fogStart : originalFogStart;
                    RenderSettings.fogEndDistance = value ? fogEnd : originalFogEnd;
                    break;
                case OptimizationItem.ShadowDistance:
                    if (pipeline != null) pipeline.shadowDistance = value ? shadowDistance : originalShadow;
                    break;
                case OptimizationItem.RenderScale:
                    // URP 가 매 프레임 이 값을 XR 디스플레이(눈 텍스처 크기)에 넘깁니다
                    if (pipeline != null) pipeline.renderScale = value ? renderScale : originalScale;
                    break;
                default:
                    XRDisplaySubsystem display = RunningDisplay();
                    if (display != null) display.foveatedRenderingLevel = value ? foveationLevel : originalFoveation;
                    break;
            }
        }

        private void Capture()
        {
            pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            originalFar = xrCamera != null ? xrCamera.farClipPlane : 1000f;
            originalFogStart = RenderSettings.fogStartDistance;
            originalFogEnd = RenderSettings.fogEndDistance;
            if (pipeline != null)
            {
                originalShadow = pipeline.shadowDistance;
                originalScale = pipeline.renderScale;
            }
            XRDisplaySubsystem display = RunningDisplay();
            originalFoveation = display != null ? display.foveatedRenderingLevel : 0f;
            captured = true;
        }

        // 에디터 Play 중 바꾼 파이프라인 에셋 값은 Play 를 멈춰도 메모리에 남아 파일에 저장될 수 있습니다 — 꼭 되돌립니다
        private void Restore()
        {
            if (!captured) return;
            captured = false;
            if (xrCamera != null) xrCamera.farClipPlane = originalFar;
            // 안개는 '지금 활성 씬'의 값입니다. 런처로 넘어가는 중이면 이미 런처 씬의 안개라 건드리지 않습니다
            if (SceneManager.GetActiveScene() == gameObject.scene)
            {
                RenderSettings.fogStartDistance = originalFogStart;
                RenderSettings.fogEndDistance = originalFogEnd;
            }
            if (pipeline != null)
            {
                pipeline.shadowDistance = originalShadow;
                pipeline.renderScale = originalScale;
            }
            XRDisplaySubsystem display = RunningDisplay();
            if (display != null) display.foveatedRenderingLevel = originalFoveation;
            for (int i = 0; i < ItemCount; i++) on[i] = false;
        }

        private XRDisplaySubsystem RunningDisplay()
        {
            SubsystemManager.GetSubsystems(displays);
            foreach (XRDisplaySubsystem display in displays)
                if (display.running) return display;
            return null;
        }
    }
}
