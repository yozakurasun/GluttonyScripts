using UnityEngine;

public class CpuSteering
{
    private const float FULL_TURN_ANGLE = 25f; //これ以上ずれていたら最大で回る
    private const float AVOID_DISTANCE = 2.5f;
    private const float AVOID_RADIUS = 0.45f;
    private const float AVOID_ANGLE = 70f;
    private const float STUCK_CHECK_INTERVAL = 1f;
    private const float STUCK_MIN_DISTANCE = 0.5f;
    private const float ESCAPE_TIME = 0.6f;

    private Transform _self;
    private CpuDifficultySettings _settings;

    private float _aimOffset;
    private Vector3 _lastCheckPos;
    private float _stuckCheckTime;
    private float _escapeTime;
    private Vector2 _escapeMove;
    private bool _isMoving;

    public bool IsStuck { get; private set; }

    public CpuSteering(Transform self, CpuDifficultySettings settings)
    {
        _self = self;
        _settings = settings;
        _lastCheckPos = self.position;
    }

    // 狙いのブレを決め直す
    public void SetAimError()
    {
        _aimOffset = Random.Range(-_settings.AimError, _settings.AimError);
    }

    public void ResetStuck() => IsStuck = false;

    /// <summary>
    /// 目的地に向かう入力
    /// </summary>
    /// <param name="target">目的地</param>
    /// <param name="stopDistance">この距離まで来たら止まる</param>
    /// <param name="targetObject">目的のオブジェクト よけない</param>
    public PlayerInputData MoveTo(Vector3 target, float stopDistance = 0f, GameObject targetObject = null)
    {
        if (_escapeTime > 0f)
        {
            return new PlayerInputData(_escapeMove, _escapeMove.x >= 0f ? 1f : -1f, 0f);
        }

        Vector3 toTarget = target - _self.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float angle = Vector3.SignedAngle(_self.forward, toTarget, Vector3.up) + _aimOffset;

        float turn = Mathf.Clamp(angle / FULL_TURN_ANGLE, -1f, 1f) * _settings.TurnSkill;

        Vector2 move = Vector2.zero;
        if (distance > stopDistance)
        {//移動はキャラの向き基準なので目的地の方向をローカルにする
            float rad = angle * Mathf.Deg2Rad;
            move = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            move = Avoid(move, targetObject);
        }

        _isMoving = move != Vector2.zero;
        return new PlayerInputData(move, turn, 0f);
    }

    // その場で目的地の方を向く
    public PlayerInputData FaceTo(Vector3 target, Vector2 move = default)
    {
        Vector3 toTarget = target - _self.position;
        toTarget.y = 0f;
        float angle = Vector3.SignedAngle(_self.forward, toTarget, Vector3.up);
        float turn = Mathf.Clamp(angle / FULL_TURN_ANGLE, -1f, 1f) * _settings.TurnSkill;
        _isMoving = move != Vector2.zero;
        return new PlayerInputData(move, turn, 0f);
    }

    public void Tick(float deltaTime)
    {
        if (_escapeTime > 0f)
        {
            _escapeTime -= deltaTime;
            _lastCheckPos = _self.position;
            _stuckCheckTime = 0f;
            return;
        }

        _stuckCheckTime += deltaTime;
        if (_stuckCheckTime < STUCK_CHECK_INTERVAL) return;

        //動こうとしているのにほとんど進んでいなければ引っかかっている
        Vector3 diff = _self.position - _lastCheckPos;
        diff.y = 0f;
        if (_isMoving && diff.magnitude < STUCK_MIN_DISTANCE)
        {
            IsStuck = true;
            StartEscape();
        }
        _lastCheckPos = _self.position;
        _stuckCheckTime = 0f;
    }

    private void StartEscape()
    {//斜め後ろに下がる
        _escapeTime = ESCAPE_TIME;
        _escapeMove = new Vector2(Random.value < 0.5f ? -0.7f : 0.7f, -0.7f);
    }

    private Vector2 Avoid(Vector2 localMove, GameObject targetObject)
    {
        Vector3 worldDir = _self.TransformDirection(new Vector3(localMove.x, 0f, localMove.y));
        if (Physics.SphereCast(_self.position, AVOID_RADIUS, worldDir, out RaycastHit hit, AVOID_DISTANCE,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) == false)
        {
            return localMove;
        }

        if (targetObject != null && hit.transform.IsChildOf(targetObject.transform)) return localMove;
        if (hit.transform.IsChildOf(_self)) return localMove;

        //当たった面の向きでよける方向を決める
        float side = Vector3.Cross(worldDir, hit.normal).y >= 0f ? -1f : 1f;
        Vector3 avoidDir = Quaternion.Euler(0f, AVOID_ANGLE * side, 0f) * worldDir;
        Vector3 local = _self.InverseTransformDirection(avoidDir);
        return new Vector2(local.x, local.z).normalized;
    }
}
