using GameData;
using GameData.UI;
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

        // ローカルプレイヤーの駒の場合、SkillTreeUIManagerからレベルを取得
        if (playerID == GameManage.Instance.LocalPlayerID)
        {
            SetHPLevel(SkillTreeUIManager.Instance.GetCurrentLevel(PieceType.Military, TechTree.HP));
            SetAPLevel(SkillTreeUIManager.Instance.GetCurrentLevel(PieceType.Military, TechTree.AP));
            attackPowerLevel = SkillTreeUIManager.Instance.GetCurrentLevel(PieceType.Military, TechTree.ATK);

            currentAP = currentMaxAP;
            currentHP = currentMaxHP;

        }
        else
        {
            // 敵プレイヤーの駒はデフォルトレベル0
            attackPowerLevel = 0;
        }

        // 特殊スキルは攻撃力レベル2以上で利用可能（将来の拡張用）
        specialSkillAvailable = attackPowerLevel >= 2;
    }

    //25.10.26 RI 添加SOData回调
    public PieceDataSO GetUnitDataSO()
    {
        return militaryData;
    }
    /// <summary>
    /// 攻撃実行（駒を攻撃）
    /// </summary>
    public bool Attack(Piece target)
    {
        if (!militaryData.canAttack || target == null || !target.IsAlive)
            return false;

        //if (Time.time - lastAttackTime < militaryData.attackCooldown)
        //    return false;

        if (!ConsumeAP(militaryData.attackAPCost))
            return false;

        //25.12.15 RI add check ap 
        //Debug.Log("Soilder atk ap cost is "+ militaryData.attackAPCost+" and  current ap is "+currentAP);

        PerformAttack(target);
        //lastAttackTime = Time.time;
        return true;
    }

    /// <summary>
    /// 建物を攻撃
    /// </summary>
    public bool AttackBuilding(Buildings.Building target)
    {
        if (!militaryData.canAttack || target == null || !target.IsAlive)
            return false;

        //if (Time.time - lastAttackTime < militaryData.attackCooldown)
        //    return false;

        if (!ConsumeAP(militaryData.attackAPCost))
            return false;

        PerformAttackOnBuilding(target);
        //lastAttackTime = Time.time;
        return true;
    }

    protected virtual void PerformAttack(Piece target)
    {
        int finalDamage = CalculateDamage();
        //Debug.Log("目标为 " + target.GetType() + " 目标HP " + target.CurrentHP+ " 攻击力 "+ finalDamage);
        target.TakeDamage(finalDamage, this);
        //Debug.Log("目标为 "+target.GetType()+" 目标HP "+target.CurrentHP);
        // クリティカル判定
        //if (UnityEngine.Random.value < militaryData.criticalChance)
        //{
        //    finalDamage *= 2;
        //    // クリティカルエフェクト表示
        //}
    }

    protected virtual void PerformAttackOnBuilding(Buildings.Building target)
    {
        int finalDamage = CalculateDamage();
        Debug.Log($"十字軍が建物を攻撃: 建物={target.Data.buildingName}, 建物HP={target.CurrentHP}, 攻撃力={finalDamage}");
        target.TakeDamage(finalDamage);
        Debug.Log($"攻撃後の建物HP: {target.CurrentHP}");

        // クリティカル判定
        //if (UnityEngine.Random.value < militaryData.criticalChance)
        //{
        //    finalDamage *= 2;
        //    Debug.Log($"クリティカルヒット！ダメージ: {finalDamage}");
        //    // クリティカルエフェクト表示
        //}
    }

    private int CalculateDamage()
    {
        return (int)militaryData.attackPowerByLevel[attackPowerLevel];
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
        return militaryData.GetAttackRangeByLevel(attackPowerLevel);
    }

    /// <summary>
    /// 各陣営ごとに特別スキルがあるはず
    /// </summary>
    public void PerformSpecialSkill()
    {
        if (!specialSkillAvailable) return;

    }

    /// <summary>
    /// 魅惑耐性スキルを持っているか（攻撃力レベル3）
    /// </summary>
    public bool HasAntiConversionSkill()
    {
        return militaryData.HasAntiConversionSkill(attackPowerLevel);
    }

    #region アップグレード管理

    // ===== プロパティ =====
    public int AttackPowerLevel => attackPowerLevel;

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
    public int GetMilitaryUpgradeCost(int level, SpecialUpgradeType type)
    {
        switch (type)
        {
            case SpecialUpgradeType.MilitaryAttackPower:
                if (level >= 3 || militaryData.attackPowerUpgradeCost == null || level >= militaryData.attackPowerUpgradeCost.Length)
                    return -1;
                return militaryData.attackPowerUpgradeCost[level];
            default:
                return -1;
        }
    }

    /// <summary>
    /// 指定項目がアップグレード可能かチェック
    /// </summary>
    public bool CanUpgradeMilitary(int level, SpecialUpgradeType type)
    {
        int cost = GetMilitaryUpgradeCost(level, type);
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

