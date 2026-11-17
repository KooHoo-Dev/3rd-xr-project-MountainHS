using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.XRIComponents
{
    public class LayerFilterZone : ZoneReadout
    {
        [Header("구역 5 연결")]
        [SerializeField] private XRSocketInteractor redSocket;
        [SerializeField] private XRSocketInteractor blueSocket;
        [SerializeField] private XRGrabInteractable redKey;
        [SerializeField] private XRGrabInteractable blueKey;
        [SerializeField] private RequireSocketFilter blueFilter;

        [Header("판정")]
        [SerializeField] private float judgeRadius = 0.25f;

        private readonly StringBuilder names = new StringBuilder(48);
        private string lastVerdict = "아직 없음";

        protected override string ButtonALabel => "필터 켜고 끄기";
        protected override string ButtonBLabel => null;

        protected override void Initialize()
        {
            if (redSocket != null) redSocket.selectEntered.AddListener(OnInserted);
            if (blueSocket != null) blueSocket.selectEntered.AddListener(OnInserted);
        }

        private void OnDestroy()
        {
            if (redSocket != null) redSocket.selectEntered.RemoveListener(OnInserted);
            if (blueSocket != null) blueSocket.selectEntered.RemoveListener(OnInserted);
        }

        protected override void OnButtonA()
        {
            if (blueFilter != null) blueFilter.enabled = !blueFilter.enabled;
        }

        private void OnInserted(SelectEnterEventArgs args)
        {
            lastVerdict = args.interactableObject.transform.name + " → " + args.interactorObject.transform.name + " · 통과 (꽂힘)";
        }

        protected override void WriteValues(StringBuilder lines)
        {
            Judge(redSocket, redKey);
            Judge(redSocket, blueKey);
            Judge(blueSocket, redKey);
            Judge(blueSocket, blueKey);

            lines.Append("빨강 열쇠 : ").Append(LayerNames(redKey != null ? redKey.interactionLayers.value : 0))
                 .Append(" · 빨강 소켓 : ").Append(LayerNames(redSocket != null ? redSocket.interactionLayers.value : 0)).Append('\n');
            lines.Append("마지막 판정 : ").Append(lastVerdict).Append('\n');
            int managerFilters = redSocket != null && redSocket.interactionManager != null ? redSocket.interactionManager.selectFilters.count : 0;
            int socketFilters = blueSocket != null ? blueSocket.selectFilters.count : 0;
            lines.Append("필터(빨강 먼저) : ").Append(blueFilter != null && blueFilter.enabled ? "켜짐" : "꺼짐")
                 .Append(" · Manager ").Append(managerFilters).Append(" · 파랑 소켓 ").Append(socketFilters).Append('\n');
            lines.Append("꽂힘 : 빨강 = ").Append(Held(redSocket)).Append(" · 파랑 = ").Append(Held(blueSocket));
        }

        private void Judge(XRSocketInteractor socket, XRGrabInteractable key)
        {
            if (socket == null || key == null || key.isSelected) return;
            if ((socket.transform.position - key.transform.position).sqrMagnitude > judgeRadius * judgeRadius) return;
            string reason;

            if ((socket.interactionLayers.value & key.interactionLayers.value) == 0) reason = "레이어 불일치";
            else if (socket == blueSocket &&
                     blueFilter != null &&
                     blueFilter.canProcess &&
                     blueFilter.Process(socket, key) == false)
                reason = "필터 거부";
            else if (socket.hasSelection) reason = "소켓이 이미 참";
            else return;
            lastVerdict = key.name + " → " + socket.name + " · " + reason;
        }

        private string LayerNames(int bits)
        {
            names.Clear();
            for (int i = 0; i < 32; ++i)
            {
                if ((bits & (1 << i)) == 0) continue;
                if (names.Length > 0) names.Append(" + ");
                names.Append(InteractionLayerMask.LayerToName(i));
            }
            return names.Length > 0 ? names.ToString() : "없음";
        }

        private static string Held(XRSocketInteractor socket)
        {
            return socket != null && socket.hasSelection ? socket.interactablesSelected[0].transform.name : "비어 있음";
        }
    }
}
