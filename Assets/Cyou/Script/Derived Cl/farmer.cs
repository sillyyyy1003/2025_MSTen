using Buildings;
using GameData;
using GamePieces;

using UnityEngine;

/// <summary>
/// 農民クラス
/// </summary>
public class Farmer : Piece
{
    private FarmerDataSO farmerData;
    private Building currentBuilding; //現在在籍中の建物

    // ===== 農民専用の個別レベル =====
    private int sacrificeLevel = 0; // 獻祭レベル (0-2)


    public override void Initialize(PieceDataSO data, int playerID)
    {
        farmerData = data as FarmerDataSO;
        if (farmerData == null)
        {
            Debug.LogError("農民にはFarmerDataSOが必要です");
            return;
        }

        base.Initialize(data, playerID);
    }
    //25.10.26 RI 添加SOData回调
    public PieceDataSO GetUnitDataSO()
    {
        return farmerData;
    }


    /// <summary>
    /// スキルレベルを設定
    /// </summary>
    public void SetSkillLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 1, farmerData.maxAPByLevel.Length);
    }

    /// <summary>
    /// スキルレベルをレベルアップ
    /// </summary>
    public void LevelUp()
    {
        if (UpgradeLevel < farmerData.maxAPByLevel.Length)
        {
            upgradeLevel++;
        }
    }

    /// <summary>
    /// AP消費し他駒のHPを回復するスキル（通常の獻祭）
    /// </summary>
    public bool Sacrifice(Piece target)
    {
        if (currentState != PieceState.Idle)
        {
            Debug.LogWarning("農民がIdle状態ではありません");
            return false;
        }

        if (!target.IsAlive)
        {
            Debug.LogWarning("ターゲットが生存していません");
            return false;
        }

        // AP不足チェック
        if (currentAP < farmerData.devotionAPCost)
        {
            Debug.LogWarning($"農民の行動力が不足しています (必要: {farmerData.devotionAPCost}, 現在: {currentAP})");
            return false;
        }

        // 回復量を取得（配列範囲外アクセス防止）
        int healAmount = farmerData.maxSacrificeLevel[Mathf.Clamp(sacrificeLevel, 0, farmerData.maxSacrificeLevel.Length - 1)];

        if (healAmount <= 0)
        {
            Debug.LogWarning($"回復量が0以下です (Sacrificeレベル: {sacrificeLevel}, 回復量: {healAmount})");
            return false;
        }

        // AP消費
        ConsumeAP(farmerData.devotionAPCost);

        // ターゲットのHP記録
        float targetOldHP = target.CurrentHP;

        // ターゲットを回復
        target.Heal(healAmount);

        Debug.Log($"農民が{target.Data.pieceName}のHPを{healAmount}回復しました (HP: {targetOldHP:F1} → {target.CurrentHP:F1})");

        // APを使い切ったら自分は死亡
        if (currentAP <= 0)
        {
            Debug.Log($"農民がSacrificeスキルによりAPを使い切り死亡しました");
            Die();
        }

        return true;
    }

    /// <summary>
    /// AP消費し他駒のAPを回復するスキル（鏡湖教専用の獻祭）
    /// </summary>
    public bool SacrificeAPRecovery(Piece target)
    {
        if (currentState != PieceState.Idle)
        {
            Debug.LogWarning("農民がIdle状態ではありません");
            return false;
        }

        if (!target.IsAlive)
        {
            Debug.LogWarning("ターゲットが生存していません");
            return false;
        }

        // AP不足チェック
        if (currentAP < farmerData.devotionAPCost)
        {
            Debug.LogWarning($"農民の行動力が不足しています (必要: {farmerData.devotionAPCost}, 現在: {currentAP})");
            return false;
        }

        // AP回復量を取得（配列範囲外アクセス防止）
        int apRecoveryAmount = farmerData.maxAPRecoveryLevel[Mathf.Clamp(sacrificeLevel, 0, farmerData.maxAPRecoveryLevel.Length - 1)];

        if (apRecoveryAmount <= 0)
        {
            Debug.LogWarning($"AP回復量が0以下です (Sacrificeレベル: {sacrificeLevel}, AP回復量: {apRecoveryAmount})");
            return false;
        }

        // AP消費
        ConsumeAP(farmerData.devotionAPCost);

        // ターゲットのAP記録
        float targetOldAP = target.CurrentAP;

        // ターゲットのAPを回復
        target.RecoverAP(apRecoveryAmount);

        Debug.Log($"鏡湖教農民が{target.Data.pieceName}のAPを{apRecoveryAmount}回復しました (AP: {targetOldAP:F1} → {target.CurrentAP:F1})");

        // APを使い切ったら自分は死亡
        if (currentAP <= 0)
        {
            Debug.Log($"農民がSacrificeスキルによりAPを使い切り死亡しました");
            Die();
        }

        return true;
    }



    /// <summary>
    /// 生産倍率を取得（スキルレベルに基づく）←（廃止）
    /// </summary>



    /// <summary>
    /// 建物に入る
    /// </summary>
    public bool EnterBuilding(Building building)
    {
        if (building == null || !building.IsOperational)
        {
            Debug.Log("建物が無いまたは建物が稼働可能な状態になっていません。");
            return false;
        }
            

        if (building.AssignFarmer(this))
        {
            currentBuilding = building;
            ChangeState(PieceState.InBuilding);
            Debug.Log("農民を建物の中に入れました");
            // 視覚的に非表示に
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 建物から出る際の処理
    /// </summary>
    public void OnExitBuilding()
    {
        currentBuilding = null;

        // 行動力が0の場合は死亡
        if (currentAP <= 0)
        {
            // 死亡処理前にGameObjectをアクティブにする
            gameObject.SetActive(true);
            Die();
        }
        else
        {
            ChangeState(PieceState.Idle);
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// AP回復をオーバーライド（農民はAPを回復できない）
    /// </summary>
    public new void RecoverAP(int amount)
    {
        // 農民はAPを回復できないため、何もしない
        Debug.Log($"農民ID={PieceID}はAPを回復できません");
    }

    #region アップグレード管理

    // ===== プロパティ =====
    public int SacrificeLevel => sacrificeLevel;

    /// <summary>
    /// アップグレード効果を適用
    /// </summary>
    protected override void ApplyUpgradeEffects()
    {
        if (farmerData == null) return;

        // レベルに応じてHP、AP、攻撃力を更新
        int newMaxHP = farmerData.GetMaxHPByLevel(upgradeLevel);
        int newMaxAP = farmerData.GetMaxAPByLevel(upgradeLevel);

        // 現在のHPとAPの割合を保持
        int hpRatio = currentHP / currentMaxHP;
        int apRatio = currentAP / currentMaxAP;

        // 新しい最大値に基づいて現在値を更新
        currentHP = newMaxHP * hpRatio;
        currentMaxHP=newMaxHP;

        Debug.Log($"信徒のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHP}, AP={newMaxAP}");

        // 新しいステータスのログ
        if (upgradeLevel == 1)
        {
            Debug.Log("血量: 4, 行動力: 5");
        }
        else if (upgradeLevel == 2)
        {
            Debug.Log("血量: 5, 行動力: 未記載");
        }
    }

    /// <summary>
    /// 獻祭回復量をアップグレードする（リソース消費は呼び出し側で行う）
    /// 鏡湖教の場合はAP回復量、それ以外の陣営はHP回復量をアップグレード
    /// </summary>
    /// <returns>アップグレード成功したらtrue</returns>
    public bool UpgradeSacrifice()
    {
        // 最大レベルチェック
        if (sacrificeLevel >= 2)
        {
            Debug.LogWarning($"{farmerData.pieceName} の獲祭回復量は既に最大レベル(2)です");
            return false;
        }

        // 鏡湖教の場合とそれ以外で異なるコスト配列を使用
        int[] upgradeCostArray;
        if (farmerData.religion == GameData.Religion.MirrorLakeReligion)
        {
            upgradeCostArray = farmerData.apRecoveryUpgradeCost;
        }
        else
        {
            upgradeCostArray = farmerData.sacrificeUpgradeCost;
        }

        // アップグレードコスト配列の境界チェック
        if (upgradeCostArray == null || sacrificeLevel >= upgradeCostArray.Length)
        {
            Debug.LogError($"{farmerData.pieceName} のアップグレードコストが正しく設定されていません");
            return false;
        }

        int cost = upgradeCostArray[sacrificeLevel];

        // コストが0の場合はアップグレード不可
        if (cost <= 0)
        {
            Debug.LogWarning($"{farmerData.pieceName} の獲祭回復量レベル{sacrificeLevel}→{sacrificeLevel + 1}へのアップグレードは設定されていません（コスト0）");
            return false;
        }

        // レベルアップ実行
        sacrificeLevel++;

        // 鏡湖教の場合とそれ以外で異なるログを出力
        if (farmerData.religion == GameData.Religion.MirrorLakeReligion)
        {
            int newAPRecoveryAmount = farmerData.maxAPRecoveryLevel[sacrificeLevel];
            Debug.Log($"{farmerData.pieceName} の獲祭AP回復量がレベル{sacrificeLevel}にアップグレードしました（AP回復量: {newAPRecoveryAmount}）");
        }
        else
        {
            int newSacrificeAmount = farmerData.maxSacrificeLevel[sacrificeLevel];
            Debug.Log($"{farmerData.pieceName} の獲祭HP回復量がレベル{sacrificeLevel}にアップグレードしました（HP回復量: {newSacrificeAmount}）");
        }

        return true;
    }

    /// <summary>
    /// 指定項目のアップグレードコストを取得
    /// 鏡湖教の場合はAP回復コスト、それ以外の陣営はHP回復コストを返す
    /// </summary>
    public int GetFarmerUpgradeCost(int level, SpecialUpgradeType type)
    {
        switch (type)
        {
            case SpecialUpgradeType.FarmerSacrifice:
                if (level >= 2)
                    return -1;

                // 鏡湖教の場合とそれ以外で異なるコスト配列を使用
                int[] upgradeCostArray;
                if (farmerData.religion == GameData.Religion.MirrorLakeReligion)
                {
                    upgradeCostArray = farmerData.apRecoveryUpgradeCost;
                }
                else
                {
                    upgradeCostArray = farmerData.sacrificeUpgradeCost;
                }

                if (upgradeCostArray == null || sacrificeLevel >= upgradeCostArray.Length)
                    return -1;

                return upgradeCostArray[level];
            default:
                return -1;
        }
    }

    /// <summary>
    /// 指定項目がアップグレード可能かチェック
    /// </summary>
    public bool CanUpgradeFarmer(int level, SpecialUpgradeType type)
    {
        int cost = GetFarmerUpgradeCost(level, type);
        return cost > 0;
    }

    #endregion

    #region セッター（同期用）

    /// <summary>
    /// 獻祭レベルを直接設定（ネットワーク同期用）
    /// </summary>
    public void SetSacrificeLevel(int level)
    {
        sacrificeLevel = Mathf.Clamp(level, 0, 2);
    }

    #endregion
}
