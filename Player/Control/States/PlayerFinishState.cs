public class PlayerFinishState : PlayerStateBase
{
    public PlayerFinishState(PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Ctx.ClearInput();
        Ctx.Input.SetCursorLock(false);
    }

    public override void FixedTick(float fixedDeltaTime) => Ctx.Motor.Stop();
}
