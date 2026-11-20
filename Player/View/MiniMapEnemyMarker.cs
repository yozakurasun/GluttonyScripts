using UnityEngine;
using UnityEngine.UI;

public class MiniMapEnemyMarker
{
    private const float VIEW_HEIGHT_OFFSET = 0.5f;
    private const float EDGE_MARGIN = 10f; //ミニマップの外にいる時の縁からの余白

    private Transform _self;
    private Transform _enemy;
    private Camera _playerCamera;
    private Camera _miniMapCamera;
    private RectTransform _miniMapRect;
    private Image _icon;
    private float _sightRange;
    private float _lostGraceTime;
    private float _lostTimer;
    private RaycastHit[] _hits = new RaycastHit[8];

    public bool IsEnemyVisible { get; private set; }

    public MiniMapEnemyMarker(Transform self, Camera playerCamera, Camera miniMapCamera, Image ownIcon,
        Color enemyColor, float sightRange, float lostGraceTime)
    {
        _self = self;
        _playerCamera = playerCamera;
        _miniMapCamera = miniMapCamera;
        _miniMapRect = (RectTransform)ownIcon.transform.parent;
        _sightRange = sightRange;
        _lostGraceTime = lostGraceTime;

        //自分のアイコンをコピーして敵のアイコンにする
        _icon = Object.Instantiate(ownIcon, _miniMapRect);
        _icon.name = "EnemyIcon";
        _icon.color = enemyColor;
        _icon.raycastTarget = false;
        _icon.gameObject.SetActive(false);
    }

    public void SetEnemy(Transform enemy) => _enemy = enemy;

    public void Tick(float deltaTime)
    {
        if (_enemy == null || _playerCamera == null || _playerCamera.enabled == false)
        {
            IsEnemyVisible = false;
            SetIconActive(false);
            return;
        }

        if (IsInSight()) _lostTimer = _lostGraceTime;
        else _lostTimer -= deltaTime;

        IsEnemyVisible = _lostTimer > 0f;
        SetIconActive(IsEnemyVisible);
        if (IsEnemyVisible) SetIconPosition();
    }

    private bool IsInSight()
    {
        Vector3 target = _enemy.position + Vector3.up * VIEW_HEIGHT_OFFSET;
        Vector3 eye = _playerCamera.transform.position;
        Vector3 toTarget = target - eye;
        float distance = toTarget.magnitude;
        if (distance > _sightRange) return false;

        //カメラに映っているか
        Vector3 viewport = _playerCamera.WorldToViewportPoint(target);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;

        //建物などに隠れていないか
        int count = Physics.RaycastNonAlloc(eye, toTarget / distance, _hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = _hits[i].transform;
            if (hit.IsChildOf(_self) || hit.IsChildOf(_enemy)) continue;
            return false;
        }
        return true;
    }

    private void SetIconPosition()
    {
        Vector3 viewport = _miniMapCamera.WorldToViewportPoint(_enemy.position);
        Vector2 size = _miniMapRect.rect.size;
        Vector2 pos = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);

        //ミニマップの外にいたら縁に出す
        float halfWidth = size.x * 0.5f - EDGE_MARGIN;
        float halfHeight = size.y * 0.5f - EDGE_MARGIN;
        float scale = Mathf.Max(Mathf.Abs(pos.x) / halfWidth, Mathf.Abs(pos.y) / halfHeight, 1f);
        pos /= scale;

        _icon.rectTransform.anchoredPosition = pos;
    }

    private void SetIconActive(bool isActive)
    {
        if (_icon.gameObject.activeSelf != isActive) _icon.gameObject.SetActive(isActive);
    }
}
