using System.Text;
using StudyXR.Common;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace StudyXR.XRIComponents
{
    public class LocomotionZone : ZoneReadout
    {
        [Header("구역 7 연결")]
        [SerializeField] private LocomotionProvider[] providers;
        [SerializeField] private GravityProvider gravity;
        [SerializeField] private JumpProvider jump;
        [SerializeField] private TwoHandedGrabMoveProvider twoHanded;

        private readonly StringBuilder list = new StringBuilder(96);

        protected override string ButtonALabel => null;
        protected override string ButtonBLabel => "한 손 / 두 손 Grab Move";

        protected override string Controls()
            => QuestButtons.Label(QuestButton.A) + " : 점프 · " + base.Controls();

        protected override void OnButtonB()
        {
            if (twoHanded != null) twoHanded.enabled = !twoHanded.enabled;
        }

        protected override void OnZoneExit()
        {
            if (twoHanded != null) twoHanded.enabled = true;
        }

        protected override void WriteValues(StringBuilder lines)
        {
            lines.Append("움직이는 Provider : ").Append(Collect(true)).Append('\n');
            lines.Append("접지 : ").Append(gravity != null && gravity.isGrounded ? "예" : "아니오")
                 .Append(" · 중력 : ").Append(gravity != null && gravity.useGravity ? "켬" : "끔")
                 .Append(jump != null && jump.isJumping ? " · 점프 중" : "").Append('\n');
            lines.Append("Grab Move : ").Append(twoHanded != null && twoHanded.enabled ? "두 손 (두 손이면 회전까지)" : "한 손씩 (이동만)").Append('\n');
            lines.Append("꺼진 Provider : ").Append(Collect(false));
        }

        private string Collect(bool moving)
        {
            list.Clear();
            if (providers != null)
            {
                foreach (LocomotionProvider p in providers)
                {
                    if (p == null) continue;
                    if (moving ? p.locomotionState == LocomotionState.Idle : p.isActiveAndEnabled) continue;
                    if (list.Length > 0) list.Append(" · ");
                    list.Append(p.GetType().Name.Replace("Provider", ""));
                    if (moving) list.Append(' ').Append(p.locomotionState);
                }
            }
            if (list.Length > 0) return list.ToString();
            return moving ? "없음 (모두 Idle)" : "없음";
        }
    }
}
