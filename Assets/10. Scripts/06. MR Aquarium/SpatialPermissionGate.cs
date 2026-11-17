using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace StudyXR.MRAquarium
{
    public class SpatialPermissionGate : MonoBehaviour
    {
        private const string ScenePermission = "com.oculus.permission.USE_SCENE";

        [Header("허용되면 켤 것 (ARPlaneManager — 씬에서는 꺼 둠)")]
        [SerializeField] private Behaviour[] managers;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool simulateDenied;

        private bool decided;
        private bool granted;

        private void Start()
        {
            Request();
        }

        private void Request()
        {
            decided = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(ScenePermission)) { OnGranted(ScenePermission); return; }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += OnGranted;
            callbacks.PermissionDenied += OnDenied;
            Permission.RequestUserPermission(ScenePermission, callbacks);
#else
            if (simulateDenied) OnDenied(ScenePermission);
            else OnGranted(ScenePermission);
#endif
        }

        private void OnGranted(string permission)
        {
            decided = true;
            granted = true;
            SetManagers(true);
        }

        private void OnDenied(string permission)
        {
            decided = true;
            granted = false;
            SetManagers(false);
        }

        private void SetManagers(bool on)
        {
            if (managers == null) return;
            foreach (Behaviour manager in managers) if (manager != null) manager.enabled = on;
        }

        public string Describe()
        {
            if (decided == false) return "권한 기다리는 중";
            return granted ? "권한 허용" : "권한 거부 — 공중 창만";
        }
    }
}
