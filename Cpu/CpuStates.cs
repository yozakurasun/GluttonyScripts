using UnityEngine;

public abstract class CpuStateBase : ICpuState
{
    protected CpuContext Ctx;

    protected CpuStateBase(CpuContext context)
    {
        Ctx = context;
    }

    public abstract string Name { get; }
    public abstract float Evaluate();
    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract PlayerInputData Tick(float deltaTime);

    // 引っかかった目的地はしばらく無視する
    protected void IgnoreIfStuck(GameObject target)
    {
        if (target != null && Ctx.Steering.IsStuck) Ctx.Sensor.Ignore(target, 4f);
    }
}

public class CpuWanderState : CpuStateBase
{
    private const float WANDER_RADIUS = 15f;
    private Vector3 _destination;

    public CpuWanderState(CpuContext context) : base(context) { }

    public override string Name => "Wander";

    public override float Evaluate() => 0.1f;

    public override void Enter() => SetDestination();

    public override PlayerInputData Tick(float deltaTime)
    {
        if (Ctx.Steering.IsStuck || (Ctx.Position - _destination).sqrMagnitude < 4f) SetDestination();
        return Ctx.Steering.MoveTo(_destination);
    }

    private void SetDestination()
    {
        Vector2 random = Random.insideUnitCircle * WANDER_RADIUS;
        _destination = Ctx.Position + new Vector3(random.x, 0f, random.y);
    }
}

public class CpuForageState : CpuStateBase
{
    private GameObject _target;

    public CpuForageState(CpuContext context) : base(context) { }

    public override string Name => "Forage";

    public override float Evaluate()
    {
        IgnoreIfStuck(_target);
        _target = FindTarget(out float cost);
        if (_target == null) return 0f;
        return 1f + 1f / (1f + cost * 0.1f); //近いほど高い
    }

    public override PlayerInputData Tick(float deltaTime)
    {
        if (_target == null || _target.activeInHierarchy == false)
        {
            _target = FindTarget(out _);
            if (_target == null) return PlayerInputData.Empty;
        }
        return Ctx.Steering.MoveTo(_target.transform.position, 0f, _target);
    }

    // 距離÷優先度が一番小さいものを選ぶ
    private GameObject FindTarget(out float cost)
    {
        cost = float.MaxValue;
        GameObject target = null;
        float radius = Ctx.Settings.SearchRadius;

        //ダメージ後でエサが食べられない時はアイテムだけ
        if (Ctx.Self.IsUnEatable == false && Ctx.Sensor.TryFindNearestEat(Ctx.Position, radius, out var eat, out float eatDistance))
        {
            cost = eatDistance;
            target = eat.gameObject;
        }

        if (Ctx.Settings.ItemWeight > 0f && Ctx.Sensor.TryFindNearestItem(Ctx.Position, radius, out var item, out float itemDistance))
        {
            float itemCost = itemDistance / Ctx.Settings.ItemWeight;
            if (itemCost < cost)
            {
                cost = itemCost;
                target = item.gameObject;
            }
        }
        return target;
    }
}

public class CpuBreakState : CpuStateBase
{
    private BreakObject _target;

    public CpuBreakState(CpuContext context) : base(context) { }

    public override string Name => "Break";

    public override float Evaluate()
    {
        if (Ctx.Self.IsBreaking) return 3f; //食べている途中なら最後まで食べる

        if (_target != null) IgnoreIfStuck(_target.gameObject);
        if (Ctx.Sensor.TryFindNearestBreak(Ctx.Position, Ctx.Settings.SearchRadius, out _target, out float distance) == false) return 0f;
        return 0.6f + 0.4f / (1f + distance * 0.1f);
    }

    public override PlayerInputData Tick(float deltaTime)
    {
        if (_target == null || _target.gameObject.activeInHierarchy == false)
        {
            if (Ctx.Sensor.TryFindNearestBreak(Ctx.Position, Ctx.Settings.SearchRadius, out _target, out _) == false) return PlayerInputData.Empty;
        }

        //食べている間は向きだけ合わせる
        if (Ctx.Self.IsBreaking) return Ctx.Steering.FaceTo(_target.transform.position);

        return Ctx.Steering.MoveTo(_target.transform.position, 0f, _target.gameObject);
    }
}

public class CpuAttackState : CpuStateBase
{
    private const float BEHIND_OFFSET = 1.2f;
    private const float FLANK_OFFSET = 3f;
    private bool _isAttack;

    public CpuAttackState(CpuContext context) : base(context) { }

    public override string Name => "Attack";

    public override float Evaluate()
    {
        if (Ctx.HasOpponent == false || Ctx.Settings.AttackChance <= 0f) return 0f;

        PlayerController opponent = Ctx.Opponent;
        if (opponent.IsInvincible || opponent.GetScore() <= 0) return 0f;

        float distance = Vector3.Distance(Ctx.Position, opponent.transform.position);
        if (distance > Ctx.Settings.AttackRange) return 0f;

        //攻めるかどうかを毎回抽選する
        _isAttack = Random.value < Ctx.Settings.AttackChance;
        if (_isAttack == false) return 0f;

        float score = 1.5f + (1f - distance / Ctx.Settings.AttackRange);
        if (opponent.GetScore() > Ctx.Self.GetScore()) score += 0.5f; //負けていたら積極的に狙う
        return score;
    }

    public override PlayerInputData Tick(float deltaTime)
    {
        if (Ctx.HasOpponent == false) return PlayerInputData.Empty;

        Transform opponent = Ctx.Opponent.transform;
        Vector3 toSelf = Ctx.Position - opponent.position;
        toSelf.y = 0f;

        Vector3 behindPos = opponent.position - opponent.forward * BEHIND_OFFSET;
        Vector3 destination = behindPos;

        if (Vector3.Dot(opponent.forward, toSelf) > 0f)
        {//相手の前にいるなら横から回り込む
            float side = Vector3.Dot(opponent.right, toSelf) >= 0f ? 1f : -1f;
            destination = opponent.position + opponent.right * (side * FLANK_OFFSET) - opponent.forward * 0.5f;
        }

        if ((Ctx.Position - behindPos).sqrMagnitude < 2.5f * 2.5f)
        {//背中に近づいたら相手に向かって突っ込む
            return Ctx.Steering.MoveTo(opponent.position, 0f, Ctx.Opponent.gameObject);
        }
        return Ctx.Steering.MoveTo(destination, 0f, Ctx.Opponent.gameObject);
    }
}

public class CpuGuardState : CpuStateBase
{
    private const float GUARD_DISTANCE = 7f;
    private const float THREAT_ANGLE = 35f;
    private float _sideMove = 1f;

    public CpuGuardState(CpuContext context) : base(context) { }

    public override string Name => "Guard";

    public override float Evaluate()
    {
        if (Ctx.Settings.CanGuard == false || Ctx.HasOpponent == false) return 0f;
        if (Ctx.Self.IsInvincible) return 0f;

        Transform opponent = Ctx.Opponent.transform;
        Vector3 opponentToSelf = Ctx.Position - opponent.position;
        opponentToSelf.y = 0f;
        if (opponentToSelf.magnitude > GUARD_DISTANCE) return 0f;

        bool isComing = Vector3.Angle(opponent.forward, opponentToSelf) < THREAT_ANGLE;          //相手がこっちに向かってきている
        bool isBackSide = Vector3.Dot(Ctx.SelfTransform.forward, -opponentToSelf) < 0f;          //相手に背中を向けている
        if (isComing && isBackSide) return 4f;
        return 0f;
    }

    public override void Enter()
    {
        _sideMove = Random.value < 0.5f ? -1f : 1f;
    }

    public override PlayerInputData Tick(float deltaTime)
    {
        if (Ctx.HasOpponent == false) return PlayerInputData.Empty;
        return Ctx.Steering.FaceTo(Ctx.Opponent.transform.position, new Vector2(_sideMove, 0f)); //相手を向いたまま横に動く
    }
}
