using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerParameterSettings _settings = new PlayerParameterSettings();

    [SerializeField] private PlayerEatTrigger _eatTrigger;
    [SerializeField] private PlayerBackTrigger _backTrigger;
    [SerializeField] private PlayerHeadRot _headRot;

    [SerializeField] private EatingUI _eatingUI;
    [SerializeField] private TextMeshProUGUI _playerScoreText;
    [SerializeField] private RectTransform _miniMap;

    [Header("ミニマップの敵表示 未設定なら子から取得")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Camera _miniMapCamera;
    [SerializeField] private Image _miniMapPlayerIcon;
    [SerializeField] private Color _enemyIconColor = new Color(1f, 0.15f, 0.1f, 1f);
    [SerializeField] private float _sightRange = 80f; // 敵が見える距離
    [SerializeField] private float _enemyLostGraceTime = 0.3f; // 見失ってからアイコンを消すまでの時間

    private IPlayerInputSource _input;
    private PlayerStateMachine _stateMachine;
    private PlayerMotor _motor;
    private PlayerView _view;
    private PlayerParameter _parameter;
    private MiniMapEnemyMarker _enemyMarker;

    private bool _isInitialized;

    public PlayerParameter Parameter => _parameter;
    public IPlayerInputSource InputSource => _input;
    public PlayerController Opponent { get; private set; }

    public int PlayerIndex
    {
        get
        {
            if (_parameter == null) return -1;
            return _parameter.PlayerIndex;
        }
    }

    public PlayerStateId CurrentState
    {
        get
        {
            if (_stateMachine == null) return PlayerStateId.Wait;
            return _stateMachine.CurrentId;
        }
    }

    public bool IsBreaking => _eatTrigger != null && _eatTrigger.IsBreaking;
    public bool IsUnEatable => _eatTrigger != null && _eatTrigger.GetUnEatable();
    public bool IsInvincible => _backTrigger != null && _backTrigger.IsInvincible;

    #region 初期化
    public void Initialize(PlayerInput playerInput, int playerIndex, Vector3 playerPos)
    {
        Initialize(new PlayerInputHandler(playerInput), playerIndex, playerPos);
    }

    // CPUはこっちで初期化する
    public void Initialize(IPlayerInputSource input, int playerIndex, Vector3 playerPos)
    {
        if (_isInitialized) return;

        transform.SetPositionAndRotation(playerPos, transform.rotation * Quaternion.Euler(0f, playerIndex * 180f, 0f));

        _input = input;
        _parameter = new PlayerParameter(playerIndex, _settings);
        _motor = new PlayerMotor(GetComponent<Rigidbody>());
        _view = new PlayerView(transform, _eatingUI, _playerScoreText, _miniMap, _headRot);
        _view.Setup(playerIndex);
        _enemyMarker = CreateEnemyMarker();

        _eatTrigger.Initialize(playerIndex);
        _backTrigger.Initialize(_settings.DamageInvincibleTime);

        SetEvents();
        CreateStateMachine();

        _isInitialized = true;
    }

    private void CreateStateMachine()
    {
        _stateMachine = new PlayerStateMachine();
        PlayerStateContext context = new PlayerStateContext(
            _input, _motor, _parameter, _settings, _view, _eatTrigger, _stateMachine,
            () => InGameManager.IsExist && InGameManager.Instance.IsStarted,
            () => InGameManager.IsExist && InGameManager.Instance.IsFinish);

        _stateMachine.Register(PlayerStateId.Wait, new PlayerWaitState(context));
        _stateMachine.Register(PlayerStateId.Play, new PlayerPlayState(context));
        _stateMachine.Register(PlayerStateId.Damaged, new PlayerDamagedState(context));
        _stateMachine.Register(PlayerStateId.Finish, new PlayerFinishState(context));
        _stateMachine.ChangeState(PlayerStateId.Wait);
    }

    private void SetEvents()
    {
        _parameter.ScoreChanged += _view.SetScore;
        _parameter.BuffListChanged += _view.SetBuffs;
        _parameter.BuffApplied += OnBuffApplied;
        _parameter.BuffExpired += OnBuffExpired;

        _eatTrigger.Ate += EatEvent;
        _eatTrigger.ItemAte += GetItem;
        _eatTrigger.BreakStarted += _view.ShowBreakGauge;
        _eatTrigger.BreakProgressed += _view.UpdateBreakGauge;
        _eatTrigger.BreakEnded += _view.HideBreakGauge;
        _backTrigger.Damaged += VomitEvent;
    }

    private void RemoveEvents()
    {
        if (_parameter != null)
        {
            _parameter.ScoreChanged -= _view.SetScore;
            _parameter.BuffListChanged -= _view.SetBuffs;
            _parameter.BuffApplied -= OnBuffApplied;
            _parameter.BuffExpired -= OnBuffExpired;
        }

        if (_eatTrigger != null && _view != null)
        {
            _eatTrigger.Ate -= EatEvent;
            _eatTrigger.ItemAte -= GetItem;
            _eatTrigger.BreakStarted -= _view.ShowBreakGauge;
            _eatTrigger.BreakProgressed -= _view.UpdateBreakGauge;
            _eatTrigger.BreakEnded -= _view.HideBreakGauge;
        }

        if (_backTrigger != null) _backTrigger.Damaged -= VomitEvent;
    }
    #endregion

    private void Update()
    {
        if (_isInitialized == false) return;
        _parameter.Tick(Time.deltaTime);
        _stateMachine.Tick(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (_isInitialized == false) return;
        if (_enemyMarker != null) _enemyMarker.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_isInitialized == false) return;
        _stateMachine.FixedTick(Time.fixedDeltaTime);
    }

    public void EatEvent()
    {
        if (_isInitialized == false) return;
        _parameter.AddScoreByEat();
        _view.PlayEat();
    }

    public void VomitEvent()
    {
        if (_isInitialized == false) return;
        if (_stateMachine.CurrentId != PlayerStateId.Play) return; //プレイ中しかダメージを受けない
        _stateMachine.ChangeState(PlayerStateId.Damaged);
    }

    #region アイテムイベント
    public void GetItem(UpType type)
    {
        if (_isInitialized == false) return;
        _parameter.ApplyBuff(type);
        _view.PlayItemGet();
    }

    private void OnBuffApplied(UpType type)
    {
        switch (type)
        {
            case UpType.Range:
                _eatTrigger.SetEatRange(_parameter.EatRangeScale);
                break;
            case UpType.Invincible:
                _backTrigger.SetInvincibleTime(_settings.BuffDuration);
                break;
            default:
                break;
        }
    }

    private void OnBuffExpired(UpType type)
    {
        if (type == UpType.Range) _eatTrigger.SetEatRange(_parameter.EatRangeScale);
    }
    #endregion

    public int GetScore()
    {
        if (_parameter == null) return 0;
        return _parameter.Score;
    }

    public void SetOpponent(PlayerController opponent)
    {
        Opponent = opponent;
        if (_enemyMarker == null) return;
        _enemyMarker.SetEnemy(opponent != null ? opponent.transform : null);
    }

    #region 1人プレイ用
    // 自分を音の聞き手にして、他のAudioListenerは切る
    public AudioListener SetAsAudioListener()
    {
        if (TryGetComponent<AudioListener>(out var listener) == false) listener = gameObject.AddComponent<AudioListener>();
        listener.enabled = true;

        foreach (var other in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (other != listener) other.enabled = false;
        }
        return listener;
    }

    public void ApplyFullScreenLayout()
    {
        if (_isInitialized == false) return;
        _view.ApplyFullScreenLayout();
    }

    // CPUはカメラとUIを非表示にする
    public void SetScreenVisible(bool isVisible)
    {
        if (_isInitialized == false) return;
        _view.SetScreenVisible(isVisible);
    }
    #endregion

    private MiniMapEnemyMarker CreateEnemyMarker()
    {
        if (_playerCamera == null) _playerCamera = FindChildComponent<Camera>("PlayerCamera");
        if (_miniMapCamera == null) _miniMapCamera = FindChildComponent<Camera>("MiniMapCamera");
        if (_miniMapPlayerIcon == null && _miniMap != null) _miniMapPlayerIcon = _miniMap.GetComponentInChildren<Image>(true);

        if (_playerCamera == null || _miniMapCamera == null || _miniMapPlayerIcon == null)
        {
            Debug.LogWarning($"{name} ミニマップの敵表示に必要な参照がありません");
            return null;
        }
        return new MiniMapEnemyMarker(transform, _playerCamera, _miniMapCamera, _miniMapPlayerIcon,
            _enemyIconColor, _sightRange, _enemyLostGraceTime);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null) return null;
        return child.GetComponent<T>();
    }

    private void OnDestroy()
    {
        RemoveEvents();
        if (_input != null) _input.Dispose();
    }
}
