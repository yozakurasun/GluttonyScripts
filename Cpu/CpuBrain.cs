using System.Collections.Generic;
using UnityEngine;

public class CpuContext
{
    public PlayerController Self { get; private set; }
    public Transform SelfTransform { get; private set; }
    public PlayerController Opponent { get; set; }
    public CpuDifficultySettings Settings { get; private set; }
    public CpuWorldSensor Sensor { get; private set; }
    public CpuSteering Steering { get; private set; }

    public Vector3 Position => SelfTransform.position;
    public bool HasOpponent => Opponent != null;

    public CpuContext(PlayerController self, CpuDifficultySettings settings)
    {
        Self = self;
        SelfTransform = self.transform;
        Settings = settings;
        Sensor = new CpuWorldSensor();
        Steering = new CpuSteering(self.transform, settings);
    }
}

public interface ICpuState
{
    string Name { get; }
    float Evaluate(); //やりたい度合い 0以下なら選ばない
    void Enter();
    void Exit();
    PlayerInputData Tick(float deltaTime);
}

public class CpuBrain
{
    private const float KEEP_STATE_BONUS = 0.15f; //すぐ行動が変わらないように今のステートを少し優先する

    private CpuContext _context;
    private List<ICpuState> _states = new List<ICpuState>();
    private ICpuState _currentState;
    private float _thinkTime;
    private float _idleTime;

    public CpuContext Context => _context;

    public string CurrentStateName
    {
        get
        {
            if (_currentState == null) return "None";
            return _currentState.Name;
        }
    }

    public CpuBrain(PlayerController self, CpuDifficultySettings settings)
    {
        _context = new CpuContext(self, settings);

        _states.Add(new CpuWanderState(_context));
        _states.Add(new CpuForageState(_context));
        _states.Add(new CpuBreakState(_context));
        _states.Add(new CpuAttackState(_context));
        _states.Add(new CpuGuardState(_context));
    }

    public void SetOpponent(PlayerController opponent) => _context.Opponent = opponent;

    public PlayerInputData Tick(float deltaTime)
    {
        _context.Sensor.Refresh();
        _context.Steering.Tick(deltaTime);

        if (_context.Steering.IsStuck) _thinkTime = 0f; //引っかかったらすぐ考え直す

        _thinkTime -= deltaTime;
        if (_thinkTime <= 0f)
        {
            _thinkTime = _context.Settings.ThinkInterval;
            Think();
        }

        if (_idleTime > 0f)
        {
            _idleTime -= deltaTime;
            return PlayerInputData.Empty;
        }

        if (_currentState == null) return PlayerInputData.Empty;
        return _currentState.Tick(deltaTime);
    }

    private void Think()
    {
        _context.Steering.SetAimError();

        //弱いCPUはたまに止まる
        if (_context.Settings.IdleChance > 0f && Random.value < _context.Settings.IdleChance)
        {
            _idleTime = _context.Settings.ThinkInterval;
        }

        ICpuState nextState = null;
        float maxScore = float.MinValue;
        for (int i = 0; i < _states.Count; i++)
        {
            float score = _states[i].Evaluate();
            if (score <= 0f) continue;
            if (_states[i] == _currentState) score += KEEP_STATE_BONUS;
            if (score > maxScore)
            {
                maxScore = score;
                nextState = _states[i];
            }
        }

        //引っかかった判定は各ステートのEvaluateで使い終わったので戻す
        _context.Steering.ResetStuck();

        if (nextState == null || nextState == _currentState) return;
        if (_currentState != null) _currentState.Exit();
        _currentState = nextState;
        _currentState.Enter();
    }
}
