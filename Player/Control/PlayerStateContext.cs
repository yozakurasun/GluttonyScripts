using System;
using UnityEngine;

public class PlayerStateContext
{
    public IPlayerInputSource Input { get; private set; }
    public PlayerMotor Motor { get; private set; }
    public PlayerParameter Parameter { get; private set; }
    public PlayerParameterSettings Settings { get; private set; }
    public PlayerView View { get; private set; }
    public PlayerEatTrigger EatTrigger { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }

    private Func<bool> _isGameStarted;
    private Func<bool> _isGameFinished;

    public bool IsGameStarted => _isGameStarted();
    public bool IsGameFinished => _isGameFinished();

    //Updateで読んだ入力をFixedUpdateで使う
    public Vector2 MoveInput { get; private set; }
    public float TurnAxis { get; private set; }
    private float _pendingYaw;

    public PlayerStateContext(IPlayerInputSource input, PlayerMotor motor, PlayerParameter parameter,
        PlayerParameterSettings settings, PlayerView view, PlayerEatTrigger eatTrigger,
        PlayerStateMachine stateMachine, Func<bool> isGameStarted, Func<bool> isGameFinished)
    {
        Input = input;
        Motor = motor;
        Parameter = parameter;
        Settings = settings;
        View = view;
        EatTrigger = eatTrigger;
        StateMachine = stateMachine;
        _isGameStarted = isGameStarted;
        _isGameFinished = isGameFinished;
    }

    public void ReadInput()
    {
        PlayerInputData input = Input.ReadInput();
        MoveInput = input.Move;
        TurnAxis = input.TurnAxis;
        //マウスは毎フレームの移動量なのでFixedUpdateまで貯めておく
        _pendingYaw += input.PointerTurnDelta * Settings.MouseSensitivity;
    }

    public void ClearInput()
    {
        MoveInput = Vector2.zero;
        TurnAxis = 0f;
        _pendingYaw = 0f;
    }

    public float GetYaw(float fixedDeltaTime)
    {
        float yaw = TurnAxis * Settings.TurnSpeed * fixedDeltaTime + _pendingYaw;
        _pendingYaw = 0f;
        return yaw;
    }
}
