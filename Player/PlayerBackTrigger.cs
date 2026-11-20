using System;
using UnityEngine;

public class PlayerBackTrigger : MonoBehaviour
{
    private float _damageInvincibleTime = 3f;
    private float _invincibleTime;

    public bool IsInvincible => _invincibleTime > 0f;

    public event Action Damaged;

    public void Initialize(float damageInvincibleTime)
    {
        _damageInvincibleTime = damageInvincibleTime;
        _invincibleTime = 0f;
    }

    void Update()
    {
        if (_invincibleTime > 0f) _invincibleTime -= Time.deltaTime;
    }

    public void OnDamage()
    {
        if (IsInvincible) return;
        SetInvincibleTime(_damageInvincibleTime);
        Damaged?.Invoke();
    }

    public void SetInvincibleTime(float invincibleTime)
    {
        _invincibleTime = Mathf.Max(_invincibleTime, invincibleTime);
    }
}
