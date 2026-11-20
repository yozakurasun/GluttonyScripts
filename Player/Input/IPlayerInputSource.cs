using System;
using UnityEngine;

public struct PlayerInputData
{
    public Vector2 Move;
    public float TurnAxis;         //スティック・キーでの回転 -1～1
    public float PointerTurnDelta; //マウスの移動量

    public PlayerInputData(Vector2 move, float turnAxis, float pointerTurnDelta)
    {
        Move = move;
        TurnAxis = turnAxis;
        PointerTurnDelta = pointerTurnDelta;
    }

    public static PlayerInputData Empty => new PlayerInputData(Vector2.zero, 0f, 0f);
}

public interface IPlayerInputSource : IDisposable
{
    bool IsKeyboardMouse { get; }

    PlayerInputData ReadInput();

    void SetCursorLock(bool isLock);
}
