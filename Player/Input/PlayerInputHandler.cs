using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : IPlayerInputSource
{
    private const string MOVE_ACTION = "Move";
    private const string LOOK_ACTION = "Look";
    private const float TURN_DEAD_ZONE = 0.1f;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private bool _isCursorLocked;

    public bool IsKeyboardMouse { get; private set; }

    public PlayerInputHandler(PlayerInput playerInput)
    {
        _playerInput = playerInput;
        _moveAction = playerInput.actions[MOVE_ACTION];
        _lookAction = playerInput.actions[LOOK_ACTION];
        IsKeyboardMouse = playerInput.devices.Any(d => d is Keyboard || d is Mouse);
    }

    public PlayerInputData ReadInput()
    {
        if (_playerInput == null || _playerInput.inputIsActive == false) return PlayerInputData.Empty;

        Vector2 move = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
        Vector2 look = _lookAction.ReadValue<Vector2>();

        float turnAxis = 0f;
        float pointerTurn = 0f;
        if (_lookAction.activeControl != null && _lookAction.activeControl.device is Pointer)
        {//マウスは動かした分だけ回す
            pointerTurn = look.x;
        }
        else if (Mathf.Abs(look.x) > TURN_DEAD_ZONE)
        {//スティックは一定速度で回す
            turnAxis = Mathf.Sign(look.x);
        }

        return new PlayerInputData(move, turnAxis, pointerTurn);
    }

    public void SetCursorLock(bool isLock)
    {
        if (IsKeyboardMouse == false) return;
        if (_isCursorLocked == isLock) return;

        _isCursorLocked = isLock;
        Cursor.lockState = isLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLock;
    }

    public void Dispose()
    {
        SetCursorLock(false);
    }
}
