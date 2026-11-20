using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerDeviceInfo : MonoBehaviour
{
    public const string KEYBOARD_MOUSE_SCHEME = "Keyboard&Mouse";
    public const string GAMEPAD_SCHEME = "Gamepad";
    public const string JOYSTICK_SCHEME = "Joystick";
    public const int MAX_PLAYER_COUNT = 2;

    public class DeviceInfo
    {
        public int PlayerIndex { get; private set; }
        public InputDevice[] Devices { get; private set; } //キーボードならキーボードとマウス
        public string ControlScheme { get; private set; } //nullならPlayerInputに任せる
        public InputDevice Device => Devices[0];
        public bool IsKeyboardMouse => ControlScheme == KEYBOARD_MOUSE_SCHEME;

        public DeviceInfo(int playerIndex, InputDevice[] devices, string controlScheme)
        {
            PlayerIndex = playerIndex;
            Devices = devices;
            ControlScheme = controlScheme;
        }

        public bool Contains(InputDevice device) => Devices.Contains(device);
    }
    private static List<DeviceInfo> _players = new List<DeviceInfo>();
    public static IReadOnlyList<DeviceInfo> Players => _players;

    [SerializeField] private InputAction _inputAction;
    [SerializeField] private string[] _defaultJoinBindings = //シーンで設定されてなければ追加する
    {
        "<Gamepad>/buttonSouth",
        "<Gamepad>/start",
        "<Keyboard>/enter",
        "<Keyboard>/space",
    };
    [SerializeField] private float _joinGuardTime = 0.5f; //タイトルの決定ボタンで参加しないように少し待つ

    private float _acceptJoinTime;
    private IDisposable _anyButtonListener;

    private event Action<DeviceInfo> _onJoinPlayer;

    private void Awake()
    {
        //staticなので2戦目以降に前の情報が残らないように消す
        _players.Clear();

        SetDefaultJoinBindings();
        _inputAction.performed += OnJoinPlayer;
        _inputAction.Enable();

        //どのボタンでも参加できるようにする
        _acceptJoinTime = Time.unscaledTime + _joinGuardTime;
        _anyButtonListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);
    }

    private void SetDefaultJoinBindings()
    {
        foreach (var path in _defaultJoinBindings)
        {
            if (_inputAction.bindings.Any(b => b.path == path)) continue;
            _inputAction.AddBinding(path);
        }
    }

    #region OnJoinEvent
    /// <summary>
    /// 参加プレイヤーのデバイス情報登録
    /// </summary>
    /// <param name="context">InputActionのコールバックコンテキスト</param>
    public void OnJoinPlayer(InputAction.CallbackContext context)
    {
        if (Time.unscaledTime < _acceptJoinTime) return;
        Join(context.control.device);
    }

    private void OnAnyButtonPress(InputControl control)
    {
        if (Time.unscaledTime < _acceptJoinTime) return;
        if (control.device is Mouse) return; //マウスでは参加しない
        Join(control.device);
    }

    private void Join(InputDevice pressedDevice)
    {
        if (_players.Count >= MAX_PLAYER_COUNT) return;

        InputDevice[] devices = GetPairDevices(pressedDevice, out string controlScheme);
        if (_players.Any(p => devices.Any(p.Contains))) return; //参加済みのデバイス

        var deviceInfo = new DeviceInfo(_players.Count, devices, controlScheme);
        _players.Add(deviceInfo);
        _onJoinPlayer?.Invoke(deviceInfo);
    }

    /// <summary>
    /// 押されたデバイスからペアリングするデバイスとコントロールスキームを決める
    /// </summary>
    /// <param name="pressedDevice">押されたデバイス</param>
    /// <param name="controlScheme">使うコントロールスキーム</param>
    private static InputDevice[] GetPairDevices(InputDevice pressedDevice, out string controlScheme)
    {
        switch (pressedDevice)
        {
            case Keyboard keyboard:
                return GetKeyboardMouse(keyboard, Mouse.current, out controlScheme);
            case Mouse mouse:
                return GetKeyboardMouse(Keyboard.current, mouse, out controlScheme);
            case Gamepad _:
                controlScheme = GAMEPAD_SCHEME;
                return new[] { pressedDevice };
            case Joystick _:
                controlScheme = JOYSTICK_SCHEME;
                return new[] { pressedDevice };
            default:
                controlScheme = null;
                return new[] { pressedDevice };
        }
    }

    private static InputDevice[] GetKeyboardMouse(Keyboard keyboard, Mouse mouse, out string controlScheme)
    {
        //Keyboard&Mouseはキーボードとマウスどっちも必要
        if (keyboard != null && mouse != null)
        {
            controlScheme = KEYBOARD_MOUSE_SCHEME;
            return new InputDevice[] { keyboard, mouse };
        }
        controlScheme = null;
        if (keyboard != null) return new InputDevice[] { keyboard };
        return new InputDevice[] { mouse };
    }

    /// <summary>
    /// プレイヤーが参加時のイベント登録
    /// </summary>
    /// <param name="setEvent">登録するイベント</param>
    public void SetOnJoinPlayerEvent(Action<DeviceInfo> setEvent)
    {
        _onJoinPlayer += setEvent;
    }

    /// <summary>
    /// イベント解除
    /// </summary>
    /// <param name="setEvent"></param>
    public void RemoveOnJoinPlayerEvent(Action<DeviceInfo> setEvent)
    {
        _onJoinPlayer -= setEvent;
    }
    #endregion

    /// <summary>
    /// 指定のプレイヤーインデックスのデバイス情報を取得
    /// </summary>
    /// <param name="playerIndex">取得するプレイヤーのインデックス</param>
    /// <returns>指定のプレイヤーインデックスのデバイス情報</returns>
    public static DeviceInfo GetPlayerDeviceInfo(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= MAX_PLAYER_COUNT)
        {
            Debug.LogWarning($"プレイヤーの最大数は{MAX_PLAYER_COUNT}です {playerIndex}");
            return null;
        }

        var deviceInfo = _players.FirstOrDefault(p => p.PlayerIndex == playerIndex);
        if (deviceInfo == null)
        {
            Debug.LogWarning($"指定のインデックスのデバイスは存在しません {playerIndex}");
            return null;
        }
        return deviceInfo;
    }

    private void OnDestroy()
    {
        if (_anyButtonListener != null) _anyButtonListener.Dispose();
        _anyButtonListener = null;
        _inputAction.performed -= OnJoinPlayer;
        _inputAction.Disable();
        _onJoinPlayer = null;
    }
}
