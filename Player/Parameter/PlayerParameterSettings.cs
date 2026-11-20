using System;
using UnityEngine;

[Serializable]
public class PlayerParameterSettings
{
    [Header("移動")]
    [SerializeField] private float _baseSpeed = 15f;
    [SerializeField] private float _maxSpeed = 30f;
    [SerializeField] private float _speedGainPerEat = 0.25f; // 1個食べるごとに上がる速度

    [Header("回転")]
    [SerializeField] private float _turnSpeed = 108f; // スティック・キーでの回転速度（度/秒）
    [SerializeField] private float _mouseSensitivity = 0.15f; // マウス1pxあたりの回転（度）

    [Header("アイテム")]
    [SerializeField] private float _buffDuration = 10f;
    [SerializeField] private float _speedUpRate = 1.5f;
    [SerializeField] private float _rangeUpScale = 1.5f;

    [Header("ダメージ")]
    [SerializeField] private float _vomitRate = 0.3f; // 吐き出すスコアの割合
    [SerializeField] private float _unEatableTime = 2f;
    [SerializeField] private float _damageInvincibleTime = 3f;

    public float BaseSpeed => _baseSpeed;
    public float MaxSpeed => _maxSpeed;
    public float SpeedGainPerEat => _speedGainPerEat;
    public float TurnSpeed => _turnSpeed;
    public float MouseSensitivity => _mouseSensitivity;
    public float BuffDuration => _buffDuration;
    public float SpeedUpRate => _speedUpRate;
    public float RangeUpScale => _rangeUpScale;
    public float VomitRate => _vomitRate;
    public float UnEatableTime => _unEatableTime;
    public float DamageInvincibleTime => _damageInvincibleTime;
}
