using System;
using System.Collections;
using GameData;
using GamePieces;
using UnityEngine;

/// <summary>
/// 宣教師クラス
/// 占領・特殊攻撃（駒変換）・特殊防御能力を持つ
/// </summary>
public class Missionary : Piece
{
    private MissionaryDataSO missionaryData;
    private int currentSkillLevel = 1; // 現在のスキルレベル

    // 占領状態
    private bool isOccupying = false;

    // 変換した駒の管理はGMに任せた

    // イベント
    public event Action<bool> OnOccupyCompleted; // 占領完了(成功/失敗)
    public event Action<Piece, float> OnPieceConverted; // 駒変換(対象駒, 持続時間)
    public event Action<int> OnSkillLevelChanged; // スキルレベル変更

    public bool IsOccupying => isOccupying;
    public int SkillLevel => currentSkillLevel;

    public override void Initialize(PieceDataSO data, int playerID)
    {
        missionaryData = data as MissionaryDataSO;
        if (missionaryData == null)
        {
            Debug.LogError("宣教師にはMissionaryDataSOが必要です");
            return;
        }

        base.Initialize(data, playerID);
    }

    #region スキルレベル管理

    /// <summary>
    /// スキルレベルを設定
    /// </summary>
    public void SetSkillLevel(int level)
    {
        int oldLevel = currentSkillLevel;
        currentSkillLevel = Mathf.Clamp(level, 1, missionaryData.convertFarmerChanceByLevel.Length);

        if (oldLevel != currentSkillLevel)
        {
            OnSkillLevelChanged?.Invoke(currentSkillLevel);
            Debug.Log($"宣教師のスキルレベルが{currentSkillLevel}になりました");
        }
    }

    /// <summary>
    /// スキルレベルをレベルアップ
    /// </summary>
    public void LevelUp()
    {
        if (currentSkillLevel < missionaryData.convertFarmerChanceByLevel.Length)
        {
            SetSkillLevel(currentSkillLevel + 1);
        }
    }

    /// <summary>
    /// アップグレードレベルに基づく自領地占領成功率を取得
    /// </summary>
    public float GetOccupyEmptySuccessRate()
    {
        return missionaryData.GetOccupyEmptySuccessRate(upgradeLevel);
    }

    /// <summary>
    /// アップグレードレベルに基づく敵領地占領成功率を取得
    /// </summary>
    public float GetOccupyEnemySuccessRate()
    {
        return missionaryData.GetOccupyEnemySuccessRate(upgradeLevel);
    }

    /// <summary>
    /// ターゲットの駒の種類に応じた魅惑成功率を取得
    /// </summary>
    public float GetConversionChanceByPieceType(Piece target)
    {
        if (target is Missionary)
        {
            return missionaryData.GetConvertMissionaryChance(upgradeLevel);
        }
        else if (target is Farmer)
        {
            return missionaryData.GetConvertFarmerChance(upgradeLevel);
        }
        else if (target is MilitaryUnit)
        {
            return missionaryData.GetConvertMilitaryChance(upgradeLevel);
        }
        else
        {
            // 教皇や他の駒タイプの場合は0%（魅惑不可）
            return 0f;
        }
    }

    /// <summary>
    /// 魅惑敵性スキルを持っているか（升級1以降）
    /// </summary>
    public bool HasAntiConversionSkill()
    {
        return missionaryData.HasAntiConversionSkill(upgradeLevel);
    }

    #endregion


    #region 占領機能

    /// <summary>
    /// マスの占領を試みる
    /// </summary>
    public bool StartOccupy(Vector3 targetPosition)
    {
        if (isOccupying)
        {
            Debug.LogWarning("既に占領中です");
            return false;
        }

        if (!ConsumeAP(missionaryData.occupyAPCost))
        {
            Debug.LogWarning("占領に必要な行動力が不足しています");
            return false;
        }

        // 占領処理開始
        ExecuteOccupy(targetPosition);
        return true;
    }

    private bool ExecuteOccupy(Vector3 targetPosition)
    {
        isOccupying = true;
        ChangeState(PieceState.Building); // 占領中状態

        // 占領判定（アップグレードレベルに応じた成功率）
        // TODO: 領地タイプに応じてGetOccupyEmptySuccessRate()かGetOccupyEnemySuccessRate()を使い分ける
        float successRate = GetOccupyEmptySuccessRate();
        bool success = UnityEngine.Random.value < successRate;

        if (success)
        {
            Debug.Log($"占領成功！位置: {targetPosition}");
            // TODO: マップシステムに占領完了を通知
            isOccupying = false;
            ChangeState(PieceState.Idle);
            OnOccupyCompleted?.Invoke(success);
            return true;
        }
        else
        {
            Debug.Log("占領失敗");
            isOccupying = false;
            ChangeState(PieceState.Idle);
            return false;
        }

       
    }


    #endregion

    ///※※特殊攻撃・防御・コマ変換共通処理は一応仕様不明瞭の為使用不可
    ///↓↓↓
    #region 特殊攻撃（駒変換）

    /// <summary>
    /// 特殊攻撃: 敵駒を自軍駒に変換
    /// </summary>
    public bool ConversionAttack(Piece target)
    {
        if (target == null || !target.IsAlive)
        {
            Debug.LogWarning("攻撃対象が無効です");
            return false;
        }

        if (target.CurrentPID == currentPID)
        {
            Debug.LogWarning("味方には攻撃できません");
            return false;
        }

        if (!ConsumeAP(missionaryData.attackAPCost))
        {
            Debug.LogWarning("攻撃に必要な行動力が不足しています");
            return false;
        }

        ChangeState(PieceState.Attacking);

        // 変換判定（ターゲットの種類に応じた成功率）
        float conversionChance = GetConversionChanceByPieceType(target);
        if (UnityEngine.Random.value < conversionChance)
        {
            // 升級1以上の宣教師・十字軍に対しては即死
            bool shouldInstantKill = target.UpgradeLevel >= 1 && (target is Missionary || target is MilitaryUnit);

            if (shouldInstantKill)
            {
                Debug.Log($"魅惑成功！{target.Data.pieceName}（升級{target.UpgradeLevel}）は即死しました！（成功率: {conversionChance * 100:F0}%）");
                target.TakeDamage(target.CurrentHP, this); // 即死ダメージ
            }
            else
            {
                ConvertEnemy(target);
                Debug.Log($"{target.Data.pieceName}を自軍に変換しました！（成功率: {conversionChance * 100:F0}%）");
            }
        }
        else
        {
            Debug.Log($"変換失敗（成功率: {conversionChance * 100:F0}%）");
        }

        ChangeState(PieceState.Idle);
        return true;
    }

    #endregion

    #region 特殊防御（駒変換）

    /// <summary>
    /// ダメージを受ける際に特殊防御を発動
    /// </summary>
    public override void TakeDamage(float damage, Piece attacker = null)
    {
        if (HasAntiConversionSkill() && attacker is Missionary)
        {
            Debug.Log("魅惑敵性スキル発動：敵の変換を無効化しました");
            base.TakeDamage(damage, attacker);
            return;
        }

        // 特殊防御判定（攻撃者の種類に応じた成功率）
        if (attacker != null && attacker.CurrentPID != currentPID)
        {
            float defenseChance = GetConversionChanceByPieceType(attacker);
            if (UnityEngine.Random.value < defenseChance)
            {
                // 升級1以上の宣教師・十字軍に対しては即死
                bool shouldInstantKill = attacker.UpgradeLevel >= 1 && (attacker is Missionary || attacker is MilitaryUnit);

                if (shouldInstantKill)
                {
                    Debug.Log($"防御時魅惑成功！{attacker.Data.pieceName}（升級{attacker.UpgradeLevel}）は即死しました！（成功率: {defenseChance * 100:F0}%）");
                    attacker.TakeDamage(attacker.CurrentHP, this); // 即死ダメージ
                }
                else
                {
                    ConvertEnemy(attacker);
                    Debug.Log($"防御時に{attacker.Data.pieceName}を自軍に変換しました！（成功率: {defenseChance * 100:F0}%）");
                }
                return;
            }
        }

        base.TakeDamage(damage, attacker);
    }

    #endregion

    #region 駒変換共通処理
    /// <summary>
    /// 今陣営変更関数が実装されてない為一応放置
    /// </summary>
    /// <param name="enemy"></param>

    private void ConvertEnemy(Piece enemy)
    {
        // 元のプレイヤーIDを保存
        int originalPlayerID = enemy.CurrentPID;

        ///ここでもう一度相手駒の職業で判定する必要はない。
        //↓switch式の練習としては良かったのだが↓
        //float convertSuccessRate = enemy.Data switch
        //{
        //    MissionaryDataSO => missionaryData.GetConvertMissionaryChance(UpgradeLevel),
        //    MilitaryDataSO => missionaryData.GetConvertMilitaryChance(UpgradeLevel),
        //    FarmerDataSO => missionaryData.GetConvertFarmerChance(UpgradeLevel),
        //    _ => default
        //};


        // プレイヤーIDを変更（陣営変更）
        enemy.ChangePID(currentPID,this);

        // 変換情報を記録
        var convertInfo = new ConvertedPieceInfo
        {
            convertedPiece = enemy,
            originalPlayerID = originalPlayerID,
            convertedTurn = Time.time //これからGMから現在のターンを取得。
        };

        OnPieceConverted?.Invoke(enemy, missionaryData.conversionTurnDuration[UpgradeLevel]);
    }


    #endregion

    #region アップグレード管理

    /// <summary>
    /// アップグレード効果を適用
    /// </summary>
    protected override void ApplyUpgradeEffects()
    {
        if (missionaryData == null) return;

        // レベルに応じてHP、AP、攻撃力を更新
        float newMaxHP = missionaryData.GetMaxHPByLevel(upgradeLevel);
        float newMaxAP = missionaryData.GetMaxAPByLevel(upgradeLevel);

        // 現在のHPとAPの割合を保持
        float hpRatio = currentHP / currentMaxHP;
        float apRatio = currentAP / currentMaxAP;

        // 新しい最大値に基づいて現在値を更新
        currentHP = newMaxHP * hpRatio;
        

        // 現在のアップグレードレベルに応じたスキル効果を取得
        float occupyOwnRate = GetOccupyEmptySuccessRate();
        float occupyEnemyRate = GetOccupyEnemySuccessRate();
        float convertMissionaryChance = missionaryData.GetConvertMissionaryChance(upgradeLevel);
        float convertFarmerChance = missionaryData.GetConvertFarmerChance(upgradeLevel);
        float convertMilitaryChance = missionaryData.GetConvertMilitaryChance(upgradeLevel);

        Debug.Log($"宣教師のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHP}, AP={newMaxAP}");
        Debug.Log($"自領地占領成功率: {occupyOwnRate * 100:F0}%, 敵領地占領成功率: {occupyEnemyRate * 100:F0}%");
        Debug.Log($"魅惑成功率 - 宣教師: {convertMissionaryChance * 100:F0}%, 信徒: {convertFarmerChance * 100:F0}%, 十字軍: {convertMilitaryChance * 100:F0}%");

        // 新しいスキルのログ
        if (upgradeLevel == 1)
        {
            Debug.Log("新スキル獲得: 魅惑敵性（敵の宣教師による変換を無効化）");
        }
    }

    #endregion

    #region クリーンアップ

    protected override void Die()
    {
        ///判定は瞬間で行われる為キャンセル一説がなくなった↓

        ///宣教師が死んでも変換したコマは規定ターン数経たないと元陣営に
        ///復帰しない

        base.Die();
    }

    #endregion

    /// <summary>
    /// 変換した駒の情報を保持する構造体
    /// </summary>
    private class ConvertedPieceInfo
    {
        public Piece convertedPiece;
        public int originalPlayerID;
        public float convertedTurn;
    }
}
