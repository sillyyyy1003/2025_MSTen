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

    // 変換した駒の管理
    private System.Collections.Generic.List<ConvertedPieceInfo> convertedPieces =
        new System.Collections.Generic.List<ConvertedPieceInfo>();

    // イベント
    public event Action<bool> OnOccupyCompleted; // 占領完了(成功/失敗)
    public event Action<Piece, float> OnPieceConverted; // 駒変換(対象駒, 持続時間)
    public event Action<int> OnSkillLevelChanged; // スキルレベル変更

    public bool IsOccupying => isOccupying;
    public int SkillLevel => currentSkillLevel;

    public override void Initialize(PieceDataSO data, Team team)
    {
        missionaryData = data as MissionaryDataSO;
        if (missionaryData == null)
        {
            Debug.LogError("宣教師にはMissionaryDataSOが必要です");
            return;
        }

        base.Initialize(data, team);
    }

    #region スキルレベル管理

    /// <summary>
    /// スキルレベルを設定
    /// </summary>
    public void SetSkillLevel(int level)
    {
        int oldLevel = currentSkillLevel;
        currentSkillLevel = Mathf.Clamp(level, 1, missionaryData.maxSkillLevel);

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
        if (currentSkillLevel < missionaryData.maxSkillLevel)
        {
            SetSkillLevel(currentSkillLevel + 1);
        }
    }

    /// <summary>
    /// スキルレベルに基づく占領成功率を計算
    /// </summary>
    public float GetOccupySuccessRate()
    {
        float rate = missionaryData.baseOccupySuccessRate +
                     (currentSkillLevel - 1) * missionaryData.skillSuccessRateBonus;
        return Mathf.Clamp01(rate); // 0〜1の範囲にクランプ
    }

    /// <summary>
    /// スキルレベルに基づく攻撃時変換確率を計算
    /// </summary>
    public float GetConversionAttackChance()
    {
        float chance = missionaryData.baseConversionAttackChance +
                       (currentSkillLevel - 1) * missionaryData.skillSuccessRateBonus;
        return Mathf.Clamp01(chance);
    }

    /// <summary>
    /// スキルレベルに基づく防御時変換確率を計算
    /// </summary>
    public float GetConversionDefenseChance()
    {
        float chance = missionaryData.baseConversionDefenseChance +
                       (currentSkillLevel - 1) * missionaryData.skillSuccessRateBonus;
        return Mathf.Clamp01(chance);
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

        // 占領判定（スキルレベルに応じた成功率）
        float successRate = GetOccupySuccessRate();
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

        if (target.OwnerTeam == ownerTeam)
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

        // 変換判定（スキルレベルに応じた成功率）
        float conversionChance = GetConversionAttackChance();
        if (UnityEngine.Random.value < conversionChance)
        {
            ConvertEnemy(target);
            Debug.Log($"{target.Data.pieceName}を自軍に変換しました！（成功率: {conversionChance * 100:F1}%）");
        }
        else
        {
            Debug.Log($"変換失敗（成功率: {conversionChance * 100:F1}%）");
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
        // 特殊防御判定（スキルレベルに応じた成功率）
        if (attacker != null && attacker.OwnerTeam != ownerTeam)
        {
            float defenseChance = GetConversionDefenseChance();
            if (UnityEngine.Random.value < defenseChance)
            {
                ConvertEnemy(attacker);
                Debug.Log($"防御時に{attacker.Data.pieceName}を自軍に変換しました！（成功率: {defenseChance * 100:F1}%）");
                // 変換成功時はダメージを受けない
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
        // 元のチームを保存
        Team originalTeam = enemy.OwnerTeam;

        // TODO: 実際のチーム変更処理（何処に陣営変更関数を実装するかはまた後日）
        // enemy.ChangeTeam(ownerTeam);

        // 変換情報を記録
        var convertInfo = new ConvertedPieceInfo
        {
            convertedPiece = enemy,
            originalTeam = originalTeam,
            conversionTime = Time.time
        };
        convertedPieces.Add(convertInfo);

        // 一定時間後に敵に戻すコルーチンを開始
        StartCoroutine(RevertConversionCoroutine(convertInfo));

        OnPieceConverted?.Invoke(enemy, missionaryData.conversionDuration);
    }

    private IEnumerator RevertConversionCoroutine(ConvertedPieceInfo convertInfo)
    {
        yield return new WaitForSeconds(missionaryData.conversionDuration);

        if (convertInfo.convertedPiece != null && convertInfo.convertedPiece.IsAlive)
        {
            // TODO: チームを元に戻す
            // convertInfo.convertedPiece.ChangeTeam(convertInfo.originalTeam);
            Debug.Log($"{convertInfo.convertedPiece.Data.pieceName}が元のチームに戻りました");
        }

        convertedPieces.Remove(convertInfo);
    }

    #endregion

    #region クリーンアップ

    protected override void Die()
    {
        ///判定は瞬間で行われる為キャンセル一説がなくなった↓
        // 占領中の場合はキャンセル
        //if (isOccupying)
        //{
      
        //}

        ///宣教師が死んでも変換したコマは規定ターン数経たないと元陣営に
        ///復帰しない
        
    //これいるのかな↓
        convertedPieces.Clear();

        base.Die();
    }

    #endregion

    /// <summary>
    /// 変換した駒の情報を保持する構造体（※今現在使えない）
    /// </summary>
    private class ConvertedPieceInfo
    {
        public Piece convertedPiece;
        public Team originalTeam;
        public float conversionTime;
    }
}
