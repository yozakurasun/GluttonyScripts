using System;
using UnityEngine;

public class PlayerEatTrigger : MonoBehaviour
{
    [SerializeField] private BoxCollider _boxCollider;
    private BreakObject _currentBreakObject;
    private bool _isUnEatable;

    public int PlayerIndex { get; private set; }
    public bool IsBreaking => _currentBreakObject != null && _currentBreakObject.gameObject.activeInHierarchy;

    public event Action Ate;
    public event Action<UpType> ItemAte;
    public event Action<float> BreakStarted;    //最大破壊時間
    public event Action<float> BreakProgressed; //残り破壊時間
    public event Action BreakEnded;

    public void Initialize(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    private void Update()
    {
        if (_currentBreakObject == null) return;

        if (_currentBreakObject.gameObject.activeSelf == false)
        {
            EndBreak();
            return;
        }
        _currentBreakObject.SubBreakTime();
        BreakProgressed?.Invoke(_currentBreakObject.GetCurrentBreakTime());
    }

    public void Eat() => Ate?.Invoke();

    public void ItemEat(UpType upType) => ItemAte?.Invoke(upType);

    public void SetEatRange(float range)
    {
        _boxCollider.size = new Vector3(range, _boxCollider.size.y, range);
    }

    public void SetUnEatable(bool isUnEatable)
    {
        _isUnEatable = isUnEatable;
    }

    public bool GetUnEatable() => _isUnEatable;

    private bool IsBreakable(Collider other) => other.CompareTag("BreakObject") || other.CompareTag("NPC");

    private void OnTriggerEnter(Collider other)
    {
        if (IsBreakable(other) == false) return;

        if (other.gameObject.TryGetComponent<BreakObject>(out var obj))
        {
            _currentBreakObject = obj;
            BreakStarted?.Invoke(obj.GetMaxBreakTime());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsBreakable(other) == false) return;
        if (_currentBreakObject == null || other.gameObject != _currentBreakObject.gameObject) return;

        EndBreak();
    }

    private void EndBreak()
    {
        _currentBreakObject = null;
        BreakEnded?.Invoke();
    }
}
