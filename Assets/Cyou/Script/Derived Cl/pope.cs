using System;
using GameData;
using GamePieces;
using UnityEngine;

/// <summary>
/// 教皇クラス
/// 味方駒と位置を交換する特殊能力を持つ
/// 教皇が死亡するとプレイヤーは敗北する
/// </summary>
public class Pope : Piece
{
    private PopeDataSO popeData;
    private float lastSwapTime = -999f;

    // ===== 教皇専用の個別レベル =====
    private int swapCooldownLevel = 0; // 位置交換CDレベル (0-2)
    private int buffLevel = 0;          // バフ効果レベル (0-3)

    // イベント
    public event Action OnPopeDeath;
    public PieceDataSO GetUnitDataSO()
    {
        return popeData;
    }
    public override void Initialize(PieceDataSO data, int playerID)
    {
        popeData = data as PopeDataSO;
        if (popeData == null)
        {
            Debug.LogError("教皇にはPopeDataSOが必要です");
            return;
        }

        base.Initialize(data, playerID);
    }

    /// <summary>
    /// 味方駒と位置を交換する
    /// 行動力を消費せず、クールタイムのみ
    /// </summary>
    public bool SwapPositionWith(Piece targetPiece)
    {
        if (targetPiece == null || !targetPiece.IsAlive)
        {
            Debug.LogWarning("交換対象が無効です");
            return false;
        }

        if (targetPiece.CurrentPID != currentPID)
        {
            Debug.LogWarning("味方駒とのみ位置を交換できます");
            return false;
        }

        if (targetPiece == this)
        {
            Debug.LogWarning("自分自身とは交換できません");
            return false;
        }

        ///今後ターン数を使って計算へ移行
        // クールタイムチェック
        if (Time.time - lastSwapTime < popeData.swapCooldown[UpgradeLevel])
        {
            Debug.LogWarning($"クールタイム中です。残り: {popeData.swapCooldown[UpgradeLevel] - (Time.time - lastSwapTime):F1}秒");
            return false;
        }

        // 位置を交換
        Vector3 tempPosition = transform.position;
        transform.position = targetPiece.transform.position;
        targetPiece.transform.position = tempPosition;

        lastSwapTime = Time.time;

        Debug.Log($"教皇が{targetPiece.Data.pieceName}と位置を交換しました");
        return true;
    }

    /// <summary>
    /// 残りクールタイムを取得
    /// ここもターン数へ移行待ち
    /// </summary>
    public float GetRemainingCooldown()
    {
        float elapsed = Time.time - lastSwapTime;
        return Mathf.Max(0, popeData.swapCooldown[UpgradeLevel] - elapsed);
    }

    /// <summary>
    /// 位置交換が可能かチェック
    /// </summary>
    public bool CanSwap()
    {
        return GetRemainingCooldown() <= 0;
    }

    #region アップグレード管理

    // ===== プロパティ =====
    public int SwapCooldownLevel => swapCooldownLevel;
    public int BuffLevel => buffLevel;

    /// <summary>
    /// アップグレード効果を適用
    /// </summary>
    protected override void ApplyUpgradeEffects()
    {
        if (popeData == null) return;

        // レベルに応じてHP、AP、攻撃力を更新
        int newMaxHP = popeData.GetMaxHPByLevel(upgradeLevel);
        int newMaxAP = popeData.GetMaxAPByLevel(upgradeLevel);

        // 現在のHPとAPの割合を保持
        int hpRatio = currentHP / currentMaxHP;
        int apRatio = currentAP / currentMaxAP;

        // 新しい最大値に基づいて現在値を更新
        currentHP = newMaxHP * hpRatio;


        Debug.Log($"教皇のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHP}, AP={newMaxAP}");
    }

    /// <summary>
    /// 位置交換クールダウンをアップグレードする（リソース消費は呼び出し側で行う）
    /// </summary>
    /// <returns>アップグレード成功したらtrue</returns>
    public bool UpgradeSwapCooldown()
    {
        // 最大レベルチェック
        if (swapCooldownLevel >= 2)
        {
            Debug.LogWarning($"{popeData.pieceName} の位置交換CDは既に最大レベル(2)です");
            return false;
        }

        // アップグレードコスト配列の境界チェック
        if (popeData.swapCooldownUpgradeCost == null || swapCooldownLevel >= popeData.swapCooldownUpgradeCost.Length)
        {
            Debug.LogError($"{popeData.pieceName} のswapCooldownUpgradeCostが正しく設定されていません");
            return false;
        }

        int cost = popeData.swapCooldownUpgradeCost[swapCooldownLevel];

        // コストが0の場合はアップグレード不可
        if (cost <= 0)
        {
            Debug.LogWarning($"{popeData.pieceName} の位置交換CDレベル{swapCooldownLevel}→{swapCooldownLevel + 1}へのアップグレードは設定されていません（コスト0）");
            return false;
        }

        // レベルアップ実行
        swapCooldownLevel++;
        int newSwapCooldown = popeData.swapCooldown[swapCooldownLevel];

        Debug.Log($"{popeData.pieceName} の位置交換CDがレベル{swapCooldownLevel}にアップグレードしました（CD: {newSwapCooldown}ターン）");
        return true;
    }

    /// <summary>
    /// バフ効果をアップグレードする（リソース消費は呼び出し側で行う）
    /// </summary>
    /// <returns>アップグレード成功したらtrue</returns>
    public bool UpgradeBuff()
    {
        // 最大レベルチェック
        if (buffLevel >= 3)
        {
            Debug.LogWarning($"{popeData.pieceName} のバフ効果は既に最大レベル(3)です");
            return false;
        }

        // アップグレードコスト配列の境界チェック
        if (popeData.buffUpgradeCost == null || buffLevel >= popeData.buffUpgradeCost.Length)
        {
            Debug.LogError($"{popeData.pieceName} のbuffUpgradeCostが正しく設定されていません");
            return false;
        }

        int cost = popeData.buffUpgradeCost[buffLevel];

        // コストが0の場合はアップグレード不可
        if (cost <= 0)
        {
            Debug.LogWarning($"{popeData.pieceName} のバフ効果レベル{buffLevel}→{buffLevel + 1}へのアップグレードは設定されていません（コスト0）");
            return false;
        }

        // レベルアップ実行
        buffLevel++;
        int newHpBuff = popeData.hpBuff[buffLevel];
        int newAtkBuff = popeData.atkBuff[buffLevel];
        float newConvertBuff = popeData.convertBuff[buffLevel];

        Debug.Log($"{popeData.pieceName} のバフ効果がレベル{buffLevel}にアップグレードしました");
        Debug.Log($"HPバフ: +{newHpBuff}, 攻撃バフ: +{newAtkBuff}, 魅惑バフ: +{newConvertBuff * 100:F0}%");
        return true;
    }

    /// <summary>
    /// 指定項目のアップグレードコストを取得
    /// </summary>
    public int GetPopeUpgradeCost(PopeUpgradeType type)
    {
        switch (type)
        {
            case PopeUpgradeType.SwapCooldown:
                if (swapCooldownLevel >= 2 || popeData.swapCooldownUpgradeCost == null || swapCooldownLevel >= popeData.swapCooldownUpgradeCost.Length)
                    return -1;
                return popeData.swapCooldownUpgradeCost[swapCooldownLevel];

            case PopeUpgradeType.Buff:
                if (buffLevel >= 3 || popeData.buffUpgradeCost == null || buffLevel >= popeData.buffUpgradeCost.Length)
                    return -1;
                return popeData.buffUpgradeCost[buffLevel];

            default:
                return -1;
        }
    }

    /// <summary>
    /// 指定項目がアップグレード可能かチェック
    /// </summary>
    public bool CanUpgradePope(PopeUpgradeType type)
    {
        int cost = GetPopeUpgradeCost(type);
        return cost > 0;
    }

    #endregion

    #region セッター（同期用）

    /// <summary>
    /// 位置交換CDレベルを直接設定（ネットワーク同期用）
    /// </summary>
    public void SetSwapCooldownLevel(int level)
    {
        swapCooldownLevel = Mathf.Clamp(level, 0, 2);
    }

    /// <summary>
    /// バフレベルを直接設定（ネットワーク同期用）
    /// </summary>
    public void SetBuffLevel(int level)
    {
        buffLevel = Mathf.Clamp(level, 0, 3);
    }

    #endregion

    /// <summary>
    /// 教皇の死亡処理
    /// ゲームオーバーイベントを発火
    /// </summary>
    protected override void Die()
    {
        Debug.LogWarning("教皇が死亡しました！ゲームオーバー");
        OnPopeDeath?.Invoke();
        base.Die();
    }
}

/// <summary>
/// 教皇のアップグレード項目タイプ
/// </summary>
public enum PopeUpgradeType
{
    SwapCooldown,   // 位置交換CD
    Buff            // バフ効果
}
