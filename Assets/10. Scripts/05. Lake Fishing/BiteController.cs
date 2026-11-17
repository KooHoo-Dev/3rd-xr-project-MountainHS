using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.LakeFishing
{
    public class BiteController : MonoBehaviour
    {
        private const float HookWindow = 1.5f;
        public const float BreakTension = 10f;
        private const float LandDistance = 1f;

        [Header("연결")]
        [SerializeField] private BobberCast cast;
        [SerializeField] private BobberFloat bobberFloat;
        [SerializeField] private FishingRod rod;
        [SerializeField] private ReelCrank reel;
        [SerializeField] private FishFight fight;
        [SerializeField] private CatchDisplay display;
        [SerializeField] private CatchLog catchLog;

        [Header("기다림")]
        [SerializeField] private float minWait = 3f;
        [SerializeField] private float maxWait = 8f;

        [Header("입질")]
        [SerializeField] private float nibbleDip = 0.06f;
        [SerializeField] private float nibbleDipTime = 0.18f;
        [SerializeField] private float nibbleInterval = 0.45f;
        [SerializeField] private float nibblePulse = 0.35f;

        [Header("챔질")]
        [SerializeField] private float hookLiftSpeed = 1.5f;
        [SerializeField] private float hookReelMeters = 0.1f;
        [SerializeField] private float hookPulse = 0.8f;

        [Header("결과")]
        [SerializeField] private float resultHold = 3f;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool enableTestKeys = true;
        [SerializeField] private float testDropDistance = 8f;

        private float nextNibbleTime;
        private float dipUntil;
        private float reelAtEnter;
        private int species;
        private float fishLength;
        private bool forceBiggest;

        public BiteState State { get; private set; }
        public float TimeInState { get; private set; }
        public float WaitTime { get; private set; }
        public string LastResult { get; private set; } = "";

        private void Update()
        {
            bool testHook = ReadTestKeys();
            TimeInState += Time.deltaTime;
            bool lineOut = (cast.CurrentPhase == BobberCast.Phase.Landed);

            if (State == BiteState.Caught || State == BiteState.Escaped)
            {
                bool busy = State == BiteState.Caught && display.IsShowing;

                if (TimeInState >= resultHold && busy == false) Enter(BiteState.Ready);
                return;
            }

            if (State == BiteState.Ready)
            {
                if (lineOut) Enter(BiteState.Waiting);
                return;
            }

            if (lineOut == false)
            {
                Enter(BiteState.Ready);
                return;
            }

            bool hookInput = testHook ||
                             rod.TipVelocity.y > hookLiftSpeed ||
                             reel.TotalMeters - reelAtEnter >= hookReelMeters;
            BiteState next = NextState(State, TimeInState, WaitTime, hookInput, fight.Tension, cast.HorizontalDistance);

            if (next != State) Enter(next);

            if (State == BiteState.Nibble) Nibble();
        }

        private static BiteState NextState(
            BiteState state,
            float timeInState,
            float waitTime,
            bool hookInput,
            float tension,
            float bobberDistance)
        {
            switch (state)
            {
                case BiteState.Waiting:
                    return timeInState >= waitTime ? BiteState.Nibble : BiteState.Waiting;
                case BiteState.Nibble:
                    if (timeInState > HookWindow) return BiteState.Escaped;
                    return hookInput ? BiteState.Hooked : BiteState.Nibble;
                case BiteState.Hooked:
                    if (tension > BreakTension) return BiteState.Escaped;
                    if (bobberDistance <= LandDistance) return BiteState.Caught;
                    return BiteState.Hooked;
                default:
                    return state;
            }
        }

        private void Enter(BiteState next)
        {
            BiteState previous = State;
            State = next;
            TimeInState = 0f;
            reelAtEnter = reel.TotalMeters;
            bobberFloat.DipDepth = 0f;

            switch (next)
            {
                case BiteState.Ready:
                    fight.Stop();
                    break;

                case BiteState.Waiting:
                    WaitTime = Random.Range(minWait, maxWait);
                    LastResult = "";
                    break;

                case BiteState.Nibble:
                    nextNibbleTime = Time.time + 0.2f;
                    break;

                case BiteState.Hooked:
                    display.PickCatch(forceBiggest, out species, out fishLength);
                    forceBiggest = false;
                    fight.Begin(fishLength);
                    rod.Pulse(hookPulse, 0.15f);
                    WaterFx.Splash(cast.Bobber.position, 0.7f);
                    break;

                case BiteState.Caught:
                {
                    Vector3 fromWater = cast.Bobber.position;
                    fight.Stop();
                    cast.Retrieve();
                    display.Show(species, fishLength, fromWater);
                    LastResult = $"잡힘 · {display.SpeciesName(species)} {fishLength * 100f:F0} cm";
                    rod.PulsePattern(0.5f, 0.08f, 3, 0.15f);
                    break;
                }

                case BiteState.Escaped:
                    if (previous == BiteState.Hooked) WaterFx.Splash(cast.Bobber.position, 0.5f);
                    fight.Stop();
                    cast.Retrieve();
                    if (catchLog != null) catchLog.AddMiss();
                    if (previous == BiteState.Hooked)
                    {
                        LastResult = "놓침 · 줄이 끊어짐";
                        rod.Pulse(1f, 0.05f);
                    }
                    else
                    {
                        LastResult = "놓침 · 챔질이 늦음";
                    }
                    break;
            }
        }

        private void Nibble()
        {
            if (Time.time >= nextNibbleTime)
            {
                dipUntil = Time.time + nibbleDipTime;
                nextNibbleTime = Time.time + nibbleInterval * Random.Range(0.7f, 1.3f);
                rod.Pulse(nibblePulse, 0.05f);
                WaterFx.Ripple(cast.Bobber.position, 0.45f);
            }
            bobberFloat.DipDepth = Time.time < dipUntil ? nibbleDip : 0f;
        }

        private bool ReadTestKeys()
        {
            if (enableTestKeys == false) return false;
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return false;

            if (keyboard.f7Key.wasPressedThisFrame && display.IsShowing == false) DropAhead();
            if (keyboard.f8Key.wasPressedThisFrame) forceBiggest = true;
            return keyboard.f6Key.wasPressedThisFrame;
        }

        private void DropAhead()
        {
            Vector3 forward = Vector3.ProjectOnPlane(rod.Tip.position - rod.transform.position, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            display.Hide();
            cast.LandAt(rod.Tip.position + forward * testDropDistance);
            Enter(BiteState.Ready);
        }
    }
}
