using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParameter
{
    private PlayerParameterSettings _settings;
    // EatingUIの並び順に合わせる
    private List<UpTypeInfo> _upTypeInfos = new List<UpTypeInfo>
    {
        new UpTypeInfo { UpType = UpType.Speed },
        new UpTypeInfo { UpType = UpType.Range },
        new UpTypeInfo { UpType = UpType.Invincible },
    };

    public int PlayerIndex { get; private set; }
    public int Score { get; private set; }
    public float MoveSpeed { get; private set; }
    public float SpeedRate { get; private set; } = 1f;
    public float EatRangeScale { get; private set; } = 1f;
    public float CurrentSpeed => MoveSpeed * SpeedRate;
    public IReadOnlyList<UpTypeInfo> UpTypeInfos => _upTypeInfos;

    public event Action<int> ScoreChanged;
    public event Action<UpType> BuffApplied;
    public event Action<UpType> BuffExpired;
    public event Action<IReadOnlyList<UpTypeInfo>> BuffListChanged;

    public PlayerParameter(int playerIndex, PlayerParameterSettings settings)
    {
        PlayerIndex = playerIndex;
        _settings = settings;
        Score = 0;
        MoveSpeed = settings.BaseSpeed;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _upTypeInfos.Count; i++)
        {
            if (_upTypeInfos[i].IsUp == false) continue;

            _upTypeInfos[i].UpTimeLimit -= deltaTime;
            if (_upTypeInfos[i].UpTimeLimit < 0f)
            {
                _upTypeInfos[i].IsUp = false;
                RemoveUpType(_upTypeInfos[i].UpType);
                BuffExpired?.Invoke(_upTypeInfos[i].UpType);
                BuffListChanged?.Invoke(_upTypeInfos);
            }
        }
    }

    public void AddScoreByEat()
    {
        Score++;
        MoveSpeed = ClampSpeed(MoveSpeed + _settings.SpeedGainPerEat);
        ScoreChanged?.Invoke(Score);
    }

    // 吐き出した数を返す
    public int Vomit()
    {
        int vomitCount = Mathf.Min((int)(Score * _settings.VomitRate), Score);
        Score -= vomitCount;
        MoveSpeed = ClampSpeed(MoveSpeed - _settings.SpeedGainPerEat * vomitCount);
        ScoreChanged?.Invoke(Score);
        return vomitCount;
    }

    #region アイテム
    public void ApplyBuff(UpType type)
    {
        for (int i = 0; i < _upTypeInfos.Count; i++)
        {
            if (_upTypeInfos[i].UpType != type) continue;
            _upTypeInfos[i].UpTimeLimit = _settings.BuffDuration;
            _upTypeInfos[i].IsUp = true;
        }

        switch (type)
        {
            case UpType.Speed:
                SpeedRate = _settings.SpeedUpRate;
                break;
            case UpType.Range:
                EatRangeScale = _settings.RangeUpScale;
                break;
            default:
                break;
        }

        BuffApplied?.Invoke(type);
        BuffListChanged?.Invoke(_upTypeInfos);
    }

    private void RemoveUpType(UpType type)
    {
        switch (type)
        {
            case UpType.Speed:
                SpeedRate = 1f;
                break;
            case UpType.Range:
                EatRangeScale = 1f;
                break;
            default:
                break;
        }
    }
    #endregion

    private float ClampSpeed(float speed) => Mathf.Clamp(speed, _settings.BaseSpeed, _settings.MaxSpeed);
}

[Serializable]
public class UpTypeInfo
{
    public UpType UpType;
    public float UpTimeLimit;
    public bool IsUp;
}
