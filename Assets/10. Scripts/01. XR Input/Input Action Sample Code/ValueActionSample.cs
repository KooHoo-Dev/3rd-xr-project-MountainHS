using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ValueActionSample : MonoBehaviour
{
    private InputAction moveAction;
    
    private void OnEnable()
    {
        moveAction = new InputAction(
            "Move",
            InputActionType.Value,
            expectedControlType: "Vector2");
        // Value로 Action하는 타입을 어떻게(expectedControlType) 해석할거냐? 입니다.
        
        // 합성하여 묶어줍니다(W,A,S,D)
        // 빌더패턴의 호출구조를 사용합니다.
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        
        // performed는 동작할때(값이 들어올 때) 호출됩니다.
        moveAction.performed += OnMove;
        // canceled는 멈출때(값이 없어질 때) 호출됩니다.
        moveAction.canceled += OnStop;
        
        // 각각의 값이 들어오는 임계점과 멈출때의 임계점은 데드존으로 관리 가능합니다.
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnStop;
        moveAction.Disable();
        moveAction.Dispose();
        moveAction = null;
    }

    // ctx = context의 줄임말 입니다
    private void OnMove(InputAction.CallbackContext ctx)
    {
        // 우리가 알고싶은것은 moveAction(InputAction)에 들어온 Vector2 값입니다.
        // ctx에 접근해서 ReadValue<T>()를 호출하여 반환받아서 현재 값을 확인할 수 있습니다.
        Vector2 value = ctx.ReadValue<Vector2>();
        Debug.Log($"[Value] OnMove - {value}");
    }

    private void OnStop(InputAction.CallbackContext ctx)
    {
        Vector2 value = ctx.ReadValue<Vector2>();
        Debug.Log($"[Value] OnStop - {value}");
    }
}
