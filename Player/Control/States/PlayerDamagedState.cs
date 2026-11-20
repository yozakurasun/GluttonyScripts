using UnityEngine;

public class PlayerDamagedState : PlayerStateBase
{
    private float _unEatableTime;

    public PlayerDamagedState(PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        _unEatableTime = Ctx.Settings.UnEatableTime;
        Ctx.EatTrigger.SetUnEatable(true);

        int vomitCount = Ctx.Parameter.Vomit();
        GenerateVomit(vomitCount);
    }

    public override void Exit()
    {
        Ctx.EatTrigger.SetUnEatable(false);
    }

    public override void Tick(float deltaTime)
    {
        if (CheckFinish()) return;

        Ctx.ReadInput();
        _unEatableTime -= deltaTime;
        if (_unEatableTime <= 0f) ChangeState(PlayerStateId.Play);
    }

    public override void FixedTick(float fixedDeltaTime) => MoveAndRotate(fixedDeltaTime);

    private void GenerateVomit(int vomitCount)
    {
        Vector3 centerPos = Ctx.Motor.Position;
        for (int i = 0; i < vomitCount; i++)
        {
            Vector3 generatePos;
            generatePos.x = Random.Range(centerPos.x - 2, centerPos.x + 2);
            generatePos.z = Random.Range(centerPos.z + 1, centerPos.z + 3);
            generatePos.y = 0.5f;
            EatObjectManager.Instance.GenerateEatObject(centerPos, generatePos);
        }
    }
}
