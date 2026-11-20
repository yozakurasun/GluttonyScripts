public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerStateContext Ctx;

    protected PlayerStateBase(PlayerStateContext context)
    {
        Ctx = context;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void FixedTick(float fixedDeltaTime) { }

    protected void ChangeState(PlayerStateId nextId) => Ctx.StateMachine.ChangeState(nextId);

    // ゲームが終わっていたらFinishにする
    protected bool CheckFinish()
    {
        if (Ctx.IsGameFinished == false) return false;
        ChangeState(PlayerStateId.Finish);
        return true;
    }

    protected void MoveAndRotate(float fixedDeltaTime)
    {
        Ctx.Motor.Rotate(Ctx.GetYaw(fixedDeltaTime));
        Ctx.Motor.Move(Ctx.MoveInput, Ctx.Parameter.CurrentSpeed);
    }
}
