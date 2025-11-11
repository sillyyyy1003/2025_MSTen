using GameData;
using GamePieces;

using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 軍隊ユニットクラス
/// </summary>
public class MilitaryUnit : Piece
{
    private MilitaryDataSO militaryData;
    private bool specialSkillAvailable;
    private float lastAttackTime;

    // ===== 軍隊専用の個別レベル =====
    private int attackPowerLevel = 0; // 攻撃力レベル (0-3)


    public override void Initialize(PieceDataSO data, int playerID)
    {
        militaryData = data as MilitaryDataSO;

        if (militaryData == null)
        {
            Debug.LogError("軍隊ユニットにはMilitaryDataSOが必要です");
            return;
        }

        base.Initialize(data, playerID);

        if (UpgradeLevel>1)
            specialSkillAvailable = true;
        else
            specialSkillAvailable = false;
    }

    //25.10.26 RI 添加SOData回调
    public PieceDataSO GetUnitDataSO()
    {
        return militaryData;
    }
    /// <summary>
    /// 攻撃実行
    /// </summary>
    public bool Attack(Piece target)
    {
        if (!militaryData.canAttack || target == null || !target.IsAlive)
            return false;

        if (Time.time - lastAttackTime < militaryData.attackCooldown)
            return false;

        if (!ConsumeAP(militaryData.attackAPCost))
            return false;

        PerformAttack(target);
        lastAttackTime = Time.time;
        return true;
    }

    protected virtual void PerformAttack(Piece target)
    {
        int finalDamage = CalculateDamage();
        Debug.Log("目标为 " + target.GetType() + " 目标HP " + target.CurrentHP+ " 攻击力 "+ finalDamage);
        target.TakeDamage(finalDamage, this);
        Debug.Log("目标为 "+target.GetType()+" 目标HP "+target.CurrentHP);
        // クリティカル判定
        if (UnityEngine.Random.value < militaryData.criticalChance)
        {
            finalDamage *= 2;
            // クリティカルエフェクト表示
        }
    }

    private int CalculateDamage()
    {
        return militaryData.attackPower;
    }

    /// <summary>
    /// ダメージを受ける（アーマー考慮）
    /// </summary>
    public override void TakeDamage(int damage, Piece attacker = null)
    {
        // 魅惑耐性スキル（升級3）: 宣教師による変換を無効化
        if (HasAntiConversionSkill() && attacker is Missionary)
        {
            Debug.Log("魅惑耐性スキル発動：敵の変換を無効化しました");
            int reducedDamage = Mathf.Max(1, damage);
            base.TakeDamage(reducedDamage, attacker);
            return;
        }

        int finalReducedDamage = damage;
        finalReducedDamage = Mathf.Max(1, finalReducedDamage); // 最小1ダメージ

        base.TakeDamage(finalReducedDamage, attacker);
    }

    /// <summary>
    /// レベルに応じた攻撃力を取得
    /// </summary>
    public int GetAttackPowerByLevel()
    {
        return militaryData.GetAttackRangeByLevel(upgradeLevel);
    }

    /// <summary>
    /// 各陣営ごとに特別スキルがあるはず
    /// </summary>
    public void PerformSpecialSkill()
    {
        if (!specialSkillAvailable) return;

    }

    /// <summary>
    /// 魅惑耐性スキルを持っているか（升級3）
    /// </summary>
    public bool HasAntiConversionSkill()
    {
        return militaryData.HasAntiConversionSkill(upgradeLevel);
    }

    #region アップグレード管理

    // ===== プロパティ =====
    public int AttackPowerLevel => attackPowerLevel;

    /// <summary>
    /// アップグレード効果を適用
    /// </summary>
    protected override void ApplyUpgradeEffects()
    {
        if (militaryData == null) return;

        // レベルに応じてHP、AP、攻撃力を更新
        int newMaxHP = militaryData.GetMaxHPByLevel(upgradeLevel);
        int newMaxAP = militaryData.GetMaxAPByLevel(upgradeLevel);
        int newAttackPower = GetAttackPowerByLevel();

        // 現在のHPとAPの割合を保持
        int hpRatio = currentHP / currentMaxHP;
        int apRatio = currentAP / currentMaxAP;

        // 新しい最大値に基づいて現在値を更新
        currentHP = newMaxHP * hpRatio;


        Debug.Log($"十字軍のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHP}, AP={newMaxAP}, 攻撃力={newAttackPower}");

        // 新しいステータスのログ
        if (upgradeLevel == 1)
        {
            Debug.Log("血量: 12, 行動力: 7, 攻撃力: 3");
        }
        else if (upgradeLevel == 2)
        {
            Debug.Log("血量: 15, 行動力: 8, 攻撃力: 5");
        }
        else if (upgradeLevel == 3)
        {
            Debug.Log("血量: 15, 行動力: 8, 攻撃力: 5, 魅惑耐性スキル獲得");
        }
    }

    /// <summary>
    /// 攻撃力をアップグレードする（リソース消費は呼び出し側で行う）
    /// </summary>
    /// <returns>アップグレード成功したらtrue</returns>
    public bool UpgradeAttackPower()
    {
        // 最大レベルチェック
        if (attackPowerLevel >= 3)
        {
            Debug.LogWarning($"{militaryData.pieceName} の攻撃力は既に最大レベル(3)です");
            return false;
        }

        // アップグレードコスト配列の境界チェック
        if (militaryData.attackPowerUpgradeCost == null || attackPowerLevel >= militaryData.attackPowerUpgradeCost.Length)
        {
            Debug.LogError($"{militaryData.pieceName} のattackPowerUpgradeCostが正しく設定されていません");
            return false;
        }

        int cost = militaryData.attackPowerUpgradeCost[attackPowerLevel];

        // コストが0の場合はアップグレード不可
        if (cost <= 0)
        {
            Debug.LogWarning($"{militaryData.pieceName} の攻撃力レベル{attackPowerLevel}→{attackPowerLevel + 1}へのアップグレードは設定されていません（コスト0）");
            return false;
        }

        // レベルアップ実行
        attackPowerLevel++;
        float newAttackPower = militaryData.attackPowerByLevel[attackPowerLevel];

        Debug.Log($"{militaryData.pieceName} の攻撃力がレベル{attackPowerLevel}にアップグレードしました（攻撃力: {newAttackPower}）");
        return true;
    }

    /// <summary>
    /// 指定項目のアップグレードコストを取得
    /// </summary>
    public int GetMilitaryUpgradeCost(MilitaryUpgradeType type)
    {
        switch (type)
        {
            case MilitaryUpgradeType.AttackPower:
                if (attackPowerLevel >= 3 || militaryData.attackPowerUpgradeCost == null || attackPowerLevel >= militaryData.attackPowerUpgradeCost.Length)
                    return -1;
                return militaryData.attackPowerUpgradeCost[attackPowerLevel];
            default:
                return -1;
        }
    }

    /// <summary>
    /// 指定項目がアップグレード可能かチェック
    /// </summary>
    public bool CanUpgradeMilitary(MilitaryUpgradeType type)
    {
        int cost = GetMilitaryUpgradeCost(type);
        return cost > 0;
    }

    #endregion

    #region セッター（同期用）

    /// <summary>
    /// 攻撃力レベルを直接設定（ネットワーク同期用）
    /// </summary>
    public void SetAttackPowerLevel(int level)
    {
        attackPowerLevel = Mathf.Clamp(level, 0, 3);
    }

    #endregion
}

/// <summary>
/// 軍隊のアップグレード項目タイプ
/// </summary>
public enum MilitaryUpgradeType
{
    AttackPower  // 攻撃力
}