using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 테스트 전용. #1 주사위를 시뮬레이터에서 확인하는 키.
    /// F4 = 시험 던지기, F5 = 집기 허용 토글, F6 = 튜닝 다시 적용. 화면 왼쪽 위에 주사위별 윗면을 표시한다.
    /// </summary>
    public class TestDiceKeys : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private DicePhysicsSettings settings;
        [SerializeField] private DiceScenePhysics scenePhysics;
        [SerializeField] private Dice[] testDice;
        [Tooltip("시험 던지기 출발 위치·방향 (보통 XR 카메라)")]
        [SerializeField] private Transform throwOrigin;

        [Tooltip("#2 경기장. 연결하면 주사위별 장외 표시와 F7(바닥 무작위 점 위에서 떨어뜨리기)을 쓴다")]
        [SerializeField] private DiceArena arena;

        [Header("시험 던지기")]
        [Tooltip("던지는 속도 (m/s). throwVelocityScale이 곱해진다")]
        [SerializeField] private float testThrowSpeed = 2.5f;
        [Tooltip("위로 드는 각도 (도)")]
        [SerializeField] private float testLoftDegrees = 20f;
        [Tooltip("무작위 회전 속도 (rad/s)")]
        [SerializeField] private float testSpinSpeed = 15f;
        [Tooltip("출발점을 카메라 앞으로 띄우는 거리 (m)")]
        [SerializeField] private float testStartForward = 0.3f;

        private InputAction throwAction;
        private InputAction selectableAction;
        private InputAction reapplyAction;
        private InputAction dropOnFloorAction;
        private bool throwQueued;
        private bool dropQueued;
        private int nextDiceIndex;
        private bool diceSelectable = true;

        private void OnEnable()
        {
            throwAction = QuestButtons.Create(QuestButton.LeftStickClick, "Test Dice Throw", "<Keyboard>/f4");
            selectableAction = QuestButtons.Create(QuestButton.X, "Test Dice Selectable", "<Keyboard>/f5");
            reapplyAction = QuestButtons.Create(QuestButton.Y, "Test Dice Reapply", "<Keyboard>/f6");
            dropOnFloorAction = QuestButtons.Create(QuestButton.A, "Test Dice Drop On Floor", "<Keyboard>/f7");
            throwAction.Enable();
            selectableAction.Enable();
            reapplyAction.Enable();
            dropOnFloorAction.Enable();
        }

        private void OnDisable()
        {
            throwAction.Dispose();
            selectableAction.Dispose();
            reapplyAction.Dispose();
            dropOnFloorAction.Dispose();
        }

        private void Start()
        {
            SetAllSelectable(diceSelectable);
        }

        private void Update()
        {
            if (throwAction.WasPressedThisFrame()) throwQueued = true;
            if (selectableAction.WasPressedThisFrame())
            {
                diceSelectable = diceSelectable == false;
                SetAllSelectable(diceSelectable);
            }
            if (reapplyAction.WasPressedThisFrame()) ReapplySettings();
            if (dropOnFloorAction.WasPressedThisFrame()) dropQueued = true;
        }

        private void FixedUpdate()
        {
            if (throwQueued)
            {
                throwQueued = false;
                ThrowNextDice();
            }
            if (dropQueued)
            {
                dropQueued = false;
                DropNextDiceOnArenaFloor();
            }
        }

        private void ThrowNextDice()
        {
            Dice dice = testDice[nextDiceIndex];
            nextDiceIndex = (nextDiceIndex + 1) % testDice.Length;
            if (dice.GetComponent<XRGrabInteractable>().isSelected) return;

            Rigidbody body = dice.GetComponent<Rigidbody>();
            Vector3 direction = Quaternion.AngleAxis(-testLoftDegrees, throwOrigin.right) * throwOrigin.forward;
            body.position = throwOrigin.position + throwOrigin.forward * testStartForward;
            body.rotation = Random.rotation;
            body.linearVelocity = direction * testThrowSpeed * settings.throwVelocityScale;
            body.angularVelocity = Random.onUnitSphere * testSpinSpeed;
        }

        /// <summary>
        /// 경기장의 RandomFloorPoint 위 10cm에서 다음 주사위를 가만히 떨어뜨린다. 점이 바닥 안쪽에 고르게 나오는지 보는 시험.
        /// </summary>
        private void DropNextDiceOnArenaFloor()
        {
            Dice dice = testDice[nextDiceIndex];
            nextDiceIndex = (nextDiceIndex + 1) % testDice.Length;
            if (dice.GetComponent<XRGrabInteractable>().isSelected) return;

            Rigidbody body = dice.GetComponent<Rigidbody>();
            body.position = arena.RandomFloorPoint() + Vector3.up * 0.1f;
            body.rotation = Random.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        [ContextMenu("튜닝 다시 적용 (F6)")]
        private void ReapplySettings()
        {
            foreach (Dice dice in testDice) dice.ApplySettings(settings);
            scenePhysics.ApplySettings(settings);
            Debug.Log($"[TestDiceKeys] 튜닝 다시 적용: {settings.name}");
        }

        private void SetAllSelectable(bool selectable)
        {
            foreach (Dice dice in testDice) dice.SetSelectable(selectable);
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 22;
            GUI.Label(new Rect(10, 10, 900, 30), $"F4 던지기 · F5 집기 허용({diceSelectable}) · F6 튜닝 다시 적용 · F7 바닥 무작위 점에 떨어뜨리기");
            for (int i = 0; i < testDice.Length; i++)
            {
                Dice dice = testDice[i];
                string state = dice.IsResting ? $"멈춤 · 윗면 {dice.ReadTopFace()}" : $"구르는 중 · 지금 윗면 {dice.ReadTopFace()}";
                string place = arena.Contains(dice.transform.position) ? "경기장 안" : "장외";
                state += $" · {place}";
                GUI.Label(new Rect(10, 44 + i * 30, 900, 30), $"{dice.name}: {state}");
            }
        }
    }
}
