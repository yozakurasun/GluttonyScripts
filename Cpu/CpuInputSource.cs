using UnityEngine;

public class CpuInputSource : IPlayerInputSource
{
    private CpuBrain _brain;

    public bool IsKeyboardMouse => false;
    public CpuBrain Brain => _brain;

    public CpuInputSource(PlayerController self, CpuDifficulty difficulty)
    {
        _brain = new CpuBrain(self, CpuDifficultySettings.Create(difficulty));
    }

    public void SetOpponent(PlayerController opponent) => _brain.SetOpponent(opponent);

    public PlayerInputData ReadInput()
    {
        if (InGameManager.IsExist == false || InGameManager.Instance.IsStarted == false) return PlayerInputData.Empty; //カウントダウン中は動かない
        return _brain.Tick(Time.deltaTime);
    }

    public void SetCursorLock(bool isLock) { }

    public void Dispose() { }
}
