using System.Text;
using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.XRIComponents
{
    public abstract class ZoneReadout : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TMP_Text board;

        [Header("안내")]
        [SerializeField] private string title = "";
        [SerializeField] [TextArea(2, 4)] private string description = "";

        [Header("갱신")]
        [SerializeField] private float refreshInterval = 0.1f;

        [Header("Quest 버튼")]
        [SerializeField] private bool useButtons = true;

        private InputAction actionA;
        private InputAction actionB;
        private float nextRefresh;
        private readonly StringBuilder text = new StringBuilder(256);

        public bool IsActiveZone { get; private set; }

        protected abstract string ButtonALabel { get; }
        protected abstract string ButtonBLabel { get; }

        private void OnEnable()
        {
            if (useButtons == false) return;
            if (ButtonALabel != null) actionA = Make(QuestButton.A, " A", "<Keyboard>/f3");
            if (ButtonBLabel != null) actionB = Make(QuestButton.B, " B", "<Keyboard>/f4");
        }

        private void OnDisable()
        {
            Drop(ref actionA);
            Drop(ref actionB);
        }

        private InputAction Make(
            QuestButton button,
            string suffix,
            string keyboardFallback)
        {
            InputAction action = QuestButtons.Create(button, name + suffix, keyboardFallback);
            action.Enable();
            return action;
        }

        private static void Drop(ref InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
            action = null;
        }

        private void Start()
        {
            Initialize();
            Refresh();
        }

        public void SetZoneActive(bool active)
        {
            bool changed = (IsActiveZone != active);
            IsActiveZone = active;
            if (changed && active) OnZoneEnter();
            if (changed && active == false) OnZoneExit();
            Refresh();
        }

        private void PressA()
        {
            if (ButtonALabel != null) OnButtonA();
            Refresh();
        }

        private void PressB()
        {
            if (ButtonBLabel != null) OnButtonB();
            Refresh();
        }

        private void Update()
        {
            if (IsActiveZone == false) return;
            if (actionA != null && actionA.WasPressedThisFrame()) PressA();
            if (actionB != null && actionB.WasPressedThisFrame()) PressB();
            if (Time.unscaledTime >= nextRefresh) Refresh();
        }

        protected void Refresh()
        {
            nextRefresh = Time.unscaledTime + refreshInterval;
            if (board == null) return;
            text.Clear();
            text.Append("<b>").Append(title).Append("</b>\n").Append(description).Append("\n<color=#9FE0FF>");
            WriteValues(text);
            text.Append("</color>\n<size=85%>").Append(Controls()).Append("</size>");
            board.text = text.ToString();
        }

        protected virtual string Controls()
        {
            string a = ButtonALabel != null ? QuestButtons.Label(QuestButton.A) + " : " + ButtonALabel : null;
            string b = ButtonBLabel != null ? QuestButtons.Label(QuestButton.B) + " : " + ButtonBLabel : null;

            if (a != null && b != null) return a + " · " + b;
            return a ?? b ?? "";
        }

        protected static string HandName(IXRInteractor interactor)
        {
            if (interactor == null) return "없음";
            switch (interactor.handedness)
            {
                case InteractorHandedness.Left: return "왼손";
                case InteractorHandedness.Right: return "오른손";
                default: return interactor.transform.name;
            }
        }

        protected abstract void WriteValues(StringBuilder lines);
        protected virtual void Initialize() { }
        protected virtual void OnButtonA() { }
        protected virtual void OnButtonB() { }
        protected virtual void OnZoneEnter() { }
        protected virtual void OnZoneExit() { }
    }
}
