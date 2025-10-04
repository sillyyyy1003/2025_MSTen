using GameData;
using GamePieces;

using UnityEngine;

/// <summary>
/// 軍隊ユニットクラス
/// </summary>
public class MilitaryUnit : Piece
{
    private MilitaryDataSO militaryData;
    private float lastAttackTime;

    public override void Initialize(PieceDataSO data, Team team)
    {
        militaryData = data as MilitaryDataSO;
        if (militaryData == null)
        {
            Debug.LogError("軍隊ユニットにはMilitaryDataSOが必要です");
            return;
        }

        base.Initialize(data, team);
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
        float finalDamage = CalculateDamage();
        target.TakeDamage(finalDamage, this);

        // クリティカル判定
        if (UnityEngine.Random.value < militaryData.criticalChance)
        {
            finalDamage *= 2f;
            // クリティカルエフェクト表示
        }
    }

    private float CalculateDamage()
    {
        return militaryData.attackPower;
    }

    /// <summary>
    /// ダメージを受ける（アーマー考慮）
    /// </summary>
    public override void TakeDamage(float damage, Piece attacker = null)
    {
        float reducedDamage = damage - militaryData.armorValue;
        reducedDamage = Mathf.Max(1f, reducedDamage); // 最小1ダメージ

        base.TakeDamage(reducedDamage, attacker);
    }
}