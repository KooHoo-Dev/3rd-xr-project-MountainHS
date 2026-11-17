using TMPro;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.XRInteraction
{
    public class EscapeDesk : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private XRSocketInteractor keySocket;
        [SerializeField] private XRGrabInteractable keyA;
        [SerializeField] private XRGrabInteractable keyB;
        [SerializeField] private XRLever lever;
        [SerializeField] private CandleLight candle;
        [SerializeField] private OpenClose lid;
        [SerializeField] private OpenClose shackle;

        [Header("표시")]
        [SerializeField] private TMP_Text guideText;
        [SerializeField] private string header = "";

        private bool dirty = true;
        private float startTime;
        private float solvedSeconds = -1f;

        public DeskState State { get; private set; }
        public DeskStep Step { get; private set; }

        private void OnEnable()
        {
            if (keySocket != null)
            {
                keySocket.selectEntered.AddListener(OnSocketEntered);
                keySocket.selectExited.AddListener(OnSocketExited);
            }
            if (lever != null)
            {
                lever.onLeverActivate.AddListener(MarkDirty);
                lever.onLeverDeactivate.AddListener(MarkDirty);
            }
            if (candle != null) candle.LitChanged += OnCandleChanged;
            dirty = true;
        }

        private void OnDisable()
        {
            if (keySocket != null)
            {
                keySocket.selectEntered.RemoveListener(OnSocketEntered);
                keySocket.selectExited.RemoveListener(OnSocketExited);
            }
            if (lever != null)
            {
                lever.onLeverActivate.RemoveListener(MarkDirty);
                lever.onLeverDeactivate.RemoveListener(MarkDirty);
            }
            if (candle != null) candle.LitChanged -= OnCandleChanged;
        }

        private void Start()
        {
            startTime = Time.time;
        }

        public void RestartTimer()
        {
            startTime = Time.time;
            solvedSeconds = -1f;
            dirty = true;
        }

        private void OnSocketEntered(SelectEnterEventArgs args)
            => MarkDirty();
        private void OnSocketExited(SelectExitEventArgs args)
            => MarkDirty();
        private void OnCandleChanged(bool lit)
            => MarkDirty();
        private void MarkDirty()
            => dirty = true;

        private void LateUpdate()
        {
            if (dirty == false) return;
            dirty = false;

            State = ReadState();
            Step = NextStep(State);

            if (lid != null) lid.SetOpen(Step == DeskStep.Open);
            if (shackle != null) shackle.SetOpen(Step == DeskStep.PullLever || Step == DeskStep.Open);
            if (Step == DeskStep.Open && solvedSeconds < 0f) solvedSeconds = Time.time - startTime;
            ShowText();
        }

        private DeskState ReadState()
        {
            XRGrabInteractable held = null;

            if (keySocket != null && keySocket.hasSelection)
                held = keySocket.interactablesSelected[0] as XRGrabInteractable;

            bool keyIn = held != null && (held == keyA || held == keyB);
            bool candleLit = candle != null && candle.IsLit;
            bool leverOn = lever != null && lever.value;

            return new DeskState(candleLit, keyIn, keyIn && held == keyA, leverOn);
        }

        private static DeskStep NextStep(DeskState state)
        {
            if (state.CandleLit == false) return DeskStep.LightCandle;
            if (state.KeyInSocket == false) return DeskStep.FindKey;
            if (state.CorrectKey == false) return DeskStep.WrongKey;
            if (state.LeverPulled == false) return DeskStep.PullLever;
            return DeskStep.Open;
        }

        private void ShowText()
        {
            if (guideText == null) return;
            string key = State.KeyInSocket == false ?
                "없음"
                : State.CorrectKey
                    ? "A"
                    : "B";
            guideText.text = header
                + "\n힌트 : " + Hint(Step)
                + "\n촛불 " + (State.CandleLit ? "켜짐" : "꺼짐") + " · 열쇠 " + key
                + " · 레버 " + (State.LeverPulled ? "당김" : "올림") + "  →  " + StepName(Step)
                + "\n<size=85%>" + DeskTestKeys.ControlsText + "</size>";
        }

        private string Hint(DeskStep step)
        {
            switch (step)
            {
                case DeskStep.LightCandle: return "촛대를 잡고(그립) 트리거로 불을 켜세요";
                case DeskStep.FindKey: return "상자 자물쇠에 열쇠를 꽂으세요";
                case DeskStep.WrongKey: return "이 열쇠는 맞지 않습니다. 방 안의 다른 열쇠를 찾아보세요";
                case DeskStep.PullLever: return "자물쇠가 풀렸습니다. 오른쪽 레버를 당기세요";
                case DeskStep.Open: return $"상자가 열렸습니다! 걸린 시간 {(int)(solvedSeconds / 60f)}분 {(int)(solvedSeconds % 60f)}초";
                default: return "";
            }
        }

        private static string StepName(DeskStep step)
        {
            switch (step)
            {
                case DeskStep.LightCandle: return "촛불 켜기";
                case DeskStep.FindKey: return "열쇠 꽂기";
                case DeskStep.WrongKey: return "틀린 열쇠";
                case DeskStep.PullLever: return "레버 당기기";
                case DeskStep.Open: return "열림";
                default: return "";
            }
        }
    }
}
