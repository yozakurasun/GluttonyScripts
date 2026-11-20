using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerGenerateManager : MonoBehaviour
{
    private const int HUMAN_INDEX = 0;
    private const int CPU_INDEX = 1;

    [SerializeField] private PlayerInput _player;
    [SerializeField] private PlayerDeviceInfo _playerDeviceInfo;
    [SerializeField] private List<GameObject> _player1FirstPositions;
    [SerializeField] private List<GameObject> _player2FirstPositions;
    [SerializeField] private float _soundHearingRange = 25f; // 1人プレイ時にCPU側の音が聞こえる距離
    [SerializeField] private JoinPromptView _joinPrompt;

    private int _playerCount = 0;
    private List<PlayerController> _players = new List<PlayerController>();
    private bool _isFinishGeneratePlayer = false;
    public bool IsFinishGeneratePlayer => _isFinishGeneratePlayer;

    private void Awake()
    {
        if (GameSession.IsSingle && PlayerInputManager.instance != null)
        {//1人プレイは画面分割しない
            PlayerInputManager.instance.splitScreen = false;
        }

        ProximitySound.ClearListener();

        //コールバック関数の登録
        _playerDeviceInfo.SetOnJoinPlayerEvent(OnJoinPlayer);
    }

    private void OnJoinPlayer(PlayerDeviceInfo.DeviceInfo deviceInfo)
    {
        if (_isFinishGeneratePlayer) return;
        SoundManager.Instance.PlaySE(0);

        if (GameSession.IsSingle)
        {
            GenerateSingleMode(deviceInfo);
            return;
        }

        GenerateHuman(deviceInfo, deviceInfo.PlayerIndex);
        if (_playerCount >= PlayerDeviceInfo.MAX_PLAYER_COUNT) FinishGenerate();
    }

    // 1人プレイ 参加した人がP1、CPUがP2
    private void GenerateSingleMode(PlayerDeviceInfo.DeviceInfo deviceInfo)
    {
        PlayerController human = GenerateHuman(deviceInfo, HUMAN_INDEX);
        human.ApplyFullScreenLayout();
        PlayerController cpu = GenerateCpu(CPU_INDEX, GameSession.Difficulty, human);
        cpu.SetScreenVisible(false);

        // 音はプレイヤーの位置から聞こえるようにする
        AudioListener listener = human.SetAsAudioListener();
        ProximitySound.SetListener(listener.transform, _soundHearingRange);

        FinishGenerate();
    }

    private PlayerController GenerateHuman(PlayerDeviceInfo.DeviceInfo deviceInfo, int playerIndex)
    {
        Debug.Log($"Player {playerIndex + 1} joined with device: {deviceInfo.Device.displayName} scheme: {deviceInfo.ControlScheme}");
        PlayerInput player = PlayerInput.Instantiate(prefab: _player.gameObject
                                                   , playerIndex: playerIndex
                                                   , controlScheme: deviceInfo.ControlScheme
                                                   , pairWithDevices: deviceInfo.Devices);
        //2人目が参加するまでに別のデバイスに切り替わらないようにする
        player.neverAutoSwitchControlSchemes = true;

        if (_joinPrompt != null) _joinPrompt.OnJoined(playerIndex);

        PlayerController playerController = player.gameObject.GetComponent<PlayerController>();
        playerController.Initialize(player, playerIndex, GetPlayerGeneratePos(playerIndex));
        AddPlayer(playerController, playerIndex);
        return playerController;
    }

    private PlayerController GenerateCpu(int playerIndex, CpuDifficulty difficulty, PlayerController opponent)
    {
        Debug.Log($"CPU {playerIndex + 1} joined difficulty: {difficulty}");

        //PlayerInputが動く前に消したいので、非アクティブの親の下で生成する
        GameObject holder = new GameObject("CpuSpawnHolder");
        holder.SetActive(false);
        GameObject cpuObject = Instantiate(_player.gameObject, holder.transform);
        cpuObject.name = $"CPU_{difficulty}";
        DestroyImmediate(cpuObject.GetComponent<PlayerInput>());
        cpuObject.transform.SetParent(null, false);
        Destroy(holder);
        cpuObject.SetActive(true);

        PlayerController cpu = cpuObject.GetComponent<PlayerController>();
        CpuInputSource input = new CpuInputSource(cpu, difficulty);
        input.SetOpponent(opponent);
        cpu.Initialize(input, playerIndex, GetPlayerGeneratePos(playerIndex));
        AddPlayer(cpu, playerIndex);
        return cpu;
    }

    private void AddPlayer(PlayerController playerController, int playerIndex)
    {
        InGameManager.Instance.SetPlayerController(playerController, playerIndex);
        _players.Add(playerController);
        _playerCount++;
    }

    private void FinishGenerate()
    {//プレイヤーの生成が完了したらゲーム開始
        _isFinishGeneratePlayer = true;
        if (_joinPrompt != null) _joinPrompt.HideAll();
        SetOpponents();
        InGameManager.Instance.StartGame();
    }

    private void SetOpponents()
    {
        if (_players.Count < 2) return;
        _players[0].SetOpponent(_players[1]);
        _players[1].SetOpponent(_players[0]);
    }

    private Vector3 GetPlayerGeneratePos(int playerIndex)
    {
        if (playerIndex == 0) return _player1FirstPositions[Random.Range(0, _player1FirstPositions.Count)].transform.position;
        else return _player2FirstPositions[Random.Range(0, _player2FirstPositions.Count)].transform.position;
    }

    private void OnDestroy()
    {
        ProximitySound.ClearListener();
        _playerDeviceInfo.RemoveOnJoinPlayerEvent(OnJoinPlayer);
    }
}
