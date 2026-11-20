using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerView
{
    private const float UI_OFFSET = 960f;
    private const int SE_EAT = 3;

    private Transform _root;
    private EatingUI _eatingUI;
    private TextMeshProUGUI _scoreText;
    private RectTransform _miniMap;
    private PlayerHeadRot _headRot;

    public PlayerView(Transform root, EatingUI eatingUI, TextMeshProUGUI scoreText, RectTransform miniMap, PlayerHeadRot headRot)
    {
        _root = root;
        _eatingUI = eatingUI;
        _scoreText = scoreText;
        _miniMap = miniMap;
        _headRot = headRot;
    }

    public void Setup(int playerIndex)
    {
        _eatingUI.transform.localPosition += new Vector3(playerIndex * UI_OFFSET, 0, 0);
        _scoreText.rectTransform.localPosition += new Vector3(playerIndex * UI_OFFSET, 0, 0);
        _miniMap.localPosition += new Vector3(playerIndex * UI_OFFSET, 0, 0);
        SetScore(0);
    }

    // 1人プレイ用 全画面にしてスコアと食べゲージを真ん中に寄せる
    public void ApplyFullScreenLayout()
    {
        foreach (var cam in _root.GetComponentsInChildren<Camera>(true))
        {
            if (cam.targetTexture != null) continue; //ミニマップのカメラ
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }
        SetCenterX(_eatingUI.transform);
        SetCenterX(_scoreText.rectTransform);
    }

    private void SetCenterX(Transform target)
    {
        Vector3 pos = target.localPosition;
        target.localPosition = new Vector3(0f, pos.y, pos.z);
    }

    public void SetScreenVisible(bool isVisible)
    {
        foreach (var cam in _root.GetComponentsInChildren<Camera>(true)) cam.enabled = isVisible;
        foreach (var canvas in _root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = isVisible;
        foreach (var listener in _root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = isVisible;
    }

    public void SetScore(int score) => _scoreText.text = $"食べた数 {score}";

    public void PlayEat()
    {
        _headRot.Paku();
        ProximitySound.PlaySE(SE_EAT, _root.position);
    }

    public void PlayItemGet() => ProximitySound.PlaySE(SE_EAT, _root.position);

    public void SetBuffs(IReadOnlyList<UpTypeInfo> upTypeInfos) => _eatingUI.SetUpTypeText(upTypeInfos);

    #region 食べゲージ
    public void ShowBreakGauge(float maxBreakTime)
    {
        _eatingUI.InitializeGauge(maxBreakTime);
        _eatingUI.DisplayEatingUI(true);
    }

    public void UpdateBreakGauge(float currentBreakTime) => _eatingUI.SetGauge(currentBreakTime);

    public void HideBreakGauge() => _eatingUI.DisplayEatingUI(false);
    #endregion
}
