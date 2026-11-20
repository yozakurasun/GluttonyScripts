using System;
using UnityEngine;

[Serializable]
public class CpuDifficultySettings
{
    public float ThinkInterval = 0.4f; //行動を考え直す間隔
    public float SearchRadius = 35f;   //エサや建物を探す範囲
    public float AimError = 10f;       //狙いのブレ（度）
    public float TurnSkill = 1f;       //回転入力の強さ 0～1
    public float ItemWeight = 1f;      //アイテムの優先度
    [UnityEngine.Range(0f, 1f)] public float AttackChance = 0.3f; //背中を狙う確率
    public float AttackRange = 12f;    //背中を狙いにいく距離
    public bool CanGuard = false;      //背中を守るか
    [UnityEngine.Range(0f, 1f)] public float IdleChance = 0f;     //止まる確率

    public static CpuDifficultySettings Create(CpuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CpuDifficulty.Easy:
                return new CpuDifficultySettings
                {
                    ThinkInterval = 0.8f,
                    SearchRadius = 20f,
                    AimError = 25f,
                    TurnSkill = 0.6f,
                    ItemWeight = 0.5f,
                    AttackChance = 0f,
                    AttackRange = 0f,
                    CanGuard = false,
                    IdleChance = 0.15f,
                };
            case CpuDifficulty.Hard:
                return new CpuDifficultySettings
                {
                    ThinkInterval = 0.15f,
                    SearchRadius = 60f,
                    AimError = 0f,
                    TurnSkill = 1f,
                    ItemWeight = 2f,
                    AttackChance = 0.7f,
                    AttackRange = 20f,
                    CanGuard = true,
                    IdleChance = 0f,
                };
            default:
                return new CpuDifficultySettings();
        }
    }
}
