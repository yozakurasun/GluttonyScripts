using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStateId
{
    Wait,    //開始待ち
    Play,    //プレイ中
    Damaged, //背中を攻撃された後
    Finish,  //ゲーム終了
}

public interface IPlayerState
{
    void Enter();
    void Exit();
    void Tick(float deltaTime);           //Updateで呼ぶ
    void FixedTick(float fixedDeltaTime); //FixedUpdateで呼ぶ
}

public class PlayerStateMachine
{
    private Dictionary<PlayerStateId, IPlayerState> _states = new Dictionary<PlayerStateId, IPlayerState>();
    private IPlayerState _currentState;

    public PlayerStateId CurrentId { get; private set; }
    public event Action<PlayerStateId, PlayerStateId> StateChanged; //前のステート, 次のステート

    public void Register(PlayerStateId id, IPlayerState state) => _states[id] = state;

    public void ChangeState(PlayerStateId nextId)
    {
        if (_states.TryGetValue(nextId, out var nextState) == false)
        {
            Debug.LogError($"ステートが登録されていません {nextId}");
            return;
        }

        PlayerStateId prevId = CurrentId;
        if (_currentState != null) _currentState.Exit();
        _currentState = nextState;
        CurrentId = nextId;
        _currentState.Enter();
        StateChanged?.Invoke(prevId, nextId);
    }

    public void Tick(float deltaTime)
    {
        if (_currentState != null) _currentState.Tick(deltaTime);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        if (_currentState != null) _currentState.FixedTick(fixedDeltaTime);
    }
}
