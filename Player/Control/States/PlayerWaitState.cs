public class PlayerWaitState : PlayerStateBase
{
    public PlayerWaitState(PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Ctx.ClearInput();
        Ctx.Input.SetCursorLock(true);
    }

    public override void Tick(float deltaTime)
    {
        if (CheckFinish()) return;
        if (Ctx.IsGameStarted)
        {
            ChangeState(PlayerStateId.Play);
            return;
        }
        Ctx.ReadInput();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        Ctx.Motor.Rotate(Ctx.GetYaw(fixedDeltaTime));
        Ctx.Motor.Stop();
    }
}
