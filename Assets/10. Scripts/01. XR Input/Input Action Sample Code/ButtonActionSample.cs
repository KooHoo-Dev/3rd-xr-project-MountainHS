using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonActionSample : MonoBehaviour
{
    // 이번에 다룰 예제는 InputAction을 실시간으로 생성하여
    // 동적으로 묶어봅니다(런타임 바인딩)
    // jump 액션을 만들어서 로그를 찍어 봅시다.
    
    private InputAction jumpAction;
    private float preedAt;

    // InputAction을 연동할때는
    // Awake, Start를 사용하는게 아니라 보통 OnEnable, OnDisable을 사용합니다.
    // 보통 껏다 켯다 하면서 입력을 제어 합니다.
    
    private void OnEnable()
    {
        // InputAction의 동적 생성 및 바인딩
        jumpAction = new InputAction(
            "Jump", 
            InputActionType.Button,
            "<Keyboard>/space");
        
        // 특정 바인딩을 동적으로 추가 하고 싶으면 아래처럼 코드를 입력합니다
        jumpAction.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
        jumpAction.AddBinding("<Mouse>/leftButton");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        
        // Path 읽는법
        //<XRController>{RightHand}/{PrimaryButton}
        //───────────── ─────────── ───────────────
        // ① 장치 종류    ② 장치 용도    ③ 컨트롤
        
        // 마지막에 활성화 처리를 합니다.
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        jumpAction.Disable();
        // jumpAction에 할당된 객체를 메모리에서 해제(예약)합니다.
        jumpAction.Dispose(); 
        jumpAction = null;
    }
    
    // 아래부터는 사용할때 코드 입니다.

    private void Update()
    {
        if (jumpAction.WasPressedThisFrame())
        {
            preedAt = Time.time;
            // jumpAction.activeControl? 인 이유?
            // : 활성화된 디바이스가 없을 수 있기 때문
            //  키보드나 마우스의 무선연결이 끊겼다거나
            //  게임패드의 배터리가 방전되었거나
            Debug.Log($"[Button] Pressed - {jumpAction.activeControl?.path}");
        }

        if (jumpAction.WasReleasedThisFrame())
        {
            Debug.Log($"[Button] Released - {(Time.time - preedAt):F2}");
        }
    }
}
