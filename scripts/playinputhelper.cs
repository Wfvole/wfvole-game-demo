using UnityEngine;
using UnityEngine.InputSystem;

public class InputBindingDetailed : MonoBehaviour
{
    private PlayerInput playerInput;
    private ThirdPersonShooterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction runAction;
    private InputAction crouchAction;
    private InputAction jumpAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        controller = ThirdPersonShooterController.Instance;

        // 通过动作名称获取具体的 InputAction
        moveAction = playerInput.actions["Player Move"];
        lookAction = playerInput.actions["Look"];
        runAction = playerInput.actions["Run"];
        crouchAction = playerInput.actions["Crouch"];
        jumpAction = playerInput.actions["Jump"];
    }

    private void OnEnable()
    {
        // 绑定每个动作的事件
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMovePerformed;

        lookAction.performed += OnLookPerformed;

        runAction.performed += OnRunPerformed;

        crouchAction.performed += OnCrouchPerformed;

        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMovePerformed;
        jumpAction.performed -= OnJumpPerformed;
        lookAction.performed -= OnLookPerformed;
        runAction.performed -= OnRunPerformed;
        crouchAction.performed -= OnCrouchPerformed;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        controller.GetMoveInput(ctx);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        controller.GetJumpInput(ctx); 
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        //controller.GetLookInput(ctx);
    }

    private void OnRunPerformed(InputAction.CallbackContext ctx)
    {
        controller.GetRunInput(ctx);
    }

    private void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        controller.GetCrouchInput(ctx);
    }
}