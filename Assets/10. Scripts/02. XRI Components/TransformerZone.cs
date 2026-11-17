using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace StudyXR.XRIComponents
{
    public class TransformerZone : ZoneReadout
    {
        [Header("구역 4 연결")]
        [SerializeField] private XRGrabInteractable generalSpear;
        [SerializeField] private XRGeneralGrabTransformer generalTransformer;
        [SerializeField] private XRGrabInteractable dualSpear;
        [SerializeField] private XRGrabInteractable slideBook;
        [SerializeField] private XRGeneralGrabTransformer bookTransformer;

        private static readonly XRGeneralGrabTransformer.TwoHandedRotationMode[] RotationModes =
        {
            XRGeneralGrabTransformer.TwoHandedRotationMode.FirstHandOnly,
            XRGeneralGrabTransformer.TwoHandedRotationMode.FirstHandDirectedTowardsSecondHand,
            XRGeneralGrabTransformer.TwoHandedRotationMode.TwoHandedAverage,
        };
        private readonly List<IXRGrabTransformer> found = new List<IXRGrabTransformer>();
        private float spearStartScale = 1f;

        protected override string ButtonALabel => "두 손 회전 방식";
        protected override string ButtonBLabel => "Dynamic Attach";

        protected override void Initialize()
        {
            if (generalSpear != null) spearStartScale = generalSpear.transform.localScale.x;
        }

        protected override void OnButtonA()
        {
            if (generalTransformer == null) return;
            int next = 0;
            for (int i = 0; i < RotationModes.Length; ++i)
                if (RotationModes[i] == generalTransformer.allowTwoHandedRotation)
                    next = (i + 1) % RotationModes.Length;
            generalTransformer.allowTwoHandedRotation = RotationModes[next];
        }

        protected override void OnButtonB()
        {
            bool on = generalSpear == null || generalSpear.useDynamicAttach == false;
            SetDynamicAttach(generalSpear, on);
            SetDynamicAttach(dualSpear, on);
            SetDynamicAttach(slideBook, on);
        }

        private static void SetDynamicAttach(XRGrabInteractable grab, bool on)
        {
            if (grab != null) grab.useDynamicAttach = on;
        }

        protected override void WriteValues(StringBuilder lines)
        {
            XRGrabInteractable focus = Focus();

            if (focus != null)
            {
                int hands = focus.interactorsSelecting.Count;
                lines.Append(focus.name).Append(" : 잡은 손 ").Append(hands).Append(" · ").Append(TransformerText(focus, hands));
            }
            lines.Append('\n');
            if (generalTransformer != null) lines.Append("두 손 회전 : ").Append(generalTransformer.allowTwoHandedRotation);
            lines.Append('\n');
            if (bookTransformer != null) lines.Append("책 허용 축 : ").Append(bookTransformer.permittedDisplacementAxes);
            lines.Append(" · Dynamic Attach : ").Append(generalSpear != null && generalSpear.useDynamicAttach ? "켜짐" : "꺼짐").Append('\n');
            if (generalSpear != null)
                lines.Append("General 창 크기 비율 : ").Append((generalSpear.transform.localScale.x / spearStartScale).ToString("F2"));
        }

        private XRGrabInteractable Focus()
        {
            if (dualSpear != null && dualSpear.isSelected) return dualSpear;
            if (slideBook != null && slideBook.isSelected) return slideBook;
            return generalSpear;
        }

        private string TransformerText(XRGrabInteractable grab, int hands)
        {
            if (hands >= 2) grab.GetMultipleGrabTransformers(found);
            else grab.GetSingleGrabTransformers(found);
            string who = found.Count > 0 ? found[0].GetType().Name : "없음 (처음 잡을 때 기본값이 붙음)";

            return (hands >= 2 ? "두 손 → " : "한 손 → ") + who;
        }
    }
}
