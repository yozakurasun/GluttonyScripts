public class PlayerPlayState : PlayerStateBase
{
    public PlayerPlayState(PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Ctx.Input.SetCursorLock(true);
    }

    public override void Tick(float deltaTime)
    {
        if (CheckFinish()) return;
        Ctx.ReadInput();
    }

    public override void FixedTick(float fixedDeltaTime) => MoveAndRotate(fixedDeltaTime);
}
