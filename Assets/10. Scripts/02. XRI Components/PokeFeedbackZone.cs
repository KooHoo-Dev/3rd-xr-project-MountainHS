using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactables.Visuals;

namespace StudyXR.XRIComponents
{
    public class PokeFeedbackZone : ZoneReadout
    {
        [Header("구역 2 연결")]
        [SerializeField] private XRPokeFilter pokeFilter;
        [SerializeField] private XRBaseInteractable button;
        [SerializeField] private XRTintInteractableVisual tint;
        [SerializeField] private SimpleAudioFeedback[] sounds;
        [SerializeField] private SimpleHapticFeedback[] haptics;

        private static readonly string[] Modes = { "색 · 소리 · 진동", "색만", "소리만", "진동만" };
        private int mode;
        private string lastPress = "없음";
        private float lastPressTime = -1f;

        protected override string ButtonALabel => "각도 제한";
        protected override string ButtonBLabel => "피드백 종류";

        protected override void Initialize()
        {
            if (button != null) button.selectEntered.AddListener(OnPressed);
            ApplyMode();
        }

        private void OnDestroy()
        {
            if (button != null) button.selectEntered.RemoveListener(OnPressed);
        }

        protected override void OnButtonA()
        {
            if (pokeFilter == null) return;
            PokeThresholdData data = pokeFilter.pokeConfiguration.Value;

            if (data != null) data.enablePokeAngleThreshold = !data.enablePokeAngleThreshold;
        }

        protected override void OnButtonB()
        {
            mode = (mode + 1) % Modes.Length;
            ApplyMode();
        }

        protected override void OnZoneExit()
        {
            mode = 0;
            ApplyMode();
        }

        private void ApplyMode()
        {
            bool color = mode == 0 || mode == 1;
            bool sound = mode == 0 || mode == 2;
            bool haptic = mode == 0 || mode == 3;

            if (tint != null)
            {
                tint.tintOnHover = color;
                tint.tintOnSelection = color;
            }
            if (sounds != null)
                foreach (SimpleAudioFeedback s in sounds) if (s != null) s.enabled = sound;
            if (haptics != null)
                foreach (SimpleHapticFeedback h in haptics) if (h != null) h.enabled = haptic;
        }

        private void OnPressed(SelectEnterEventArgs args)
        {
            lastPress = HandName(args.interactorObject) + " " + args.interactorObject.transform.name + " · " + Modes[mode];
            lastPressTime = Time.time;
        }

        protected override void WriteValues(StringBuilder lines)
        {
            PokeStateData state = default;

            if (pokeFilter != null && pokeFilter.pokeStateData != null) state = pokeFilter.pokeStateData.Value;
            lines.Append("누름 강도 ").Append(state.interactionStrength.ToString("F2"))
                 .Append(" · 조건 충족 : ").Append(state.meetsRequirements ? "예" : "아니오").Append('\n');

            PokeThresholdData data = pokeFilter != null ? pokeFilter.pokeConfiguration.Value : null;
            lines.Append("각도 제한 : ");
            if (data == null) lines.Append('—');
            else if (data.enablePokeAngleThreshold)
                lines.Append("켜짐 (누르는 방향 ±").Append(data.pokeAngleThreshold.ToString("F0")).Append("도)");
            else lines.Append("꺼짐 (비스듬히 밀어도 눌림)");
            lines.Append('\n');

            lines.Append("피드백 : ").Append(Modes[mode]).Append('\n');
            lines.Append("마지막 누름 : ").Append(lastPress);
            if (lastPressTime >= 0f)
                lines.Append(" · ").Append((Time.time - lastPressTime).ToString("F1")).Append("초 전");
        }
    }
}
