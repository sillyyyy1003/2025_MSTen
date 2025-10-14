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

    // イベント
    public event Action OnPopeDeath;

    public override void Initialize(PieceDataSO data, Team team)
    {
        popeData = data as PopeDataSO;
        if (popeData == null)
        {
            Debug.LogError("教皇にはPopeDataSOが必要です");
            return;
        }

        base.Initialize(data, team);
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

        if (targetPiece.OwnerTeam != ownerTeam)
        {
            Debug.LogWarning("味方駒とのみ位置を交換できます");
            return false;
        }

        if (targetPiece == this)
        {
            Debug.LogWarning("自分自身とは交換できません");
            return false;
        }

        // クールタイムチェック
        if (Time.time - lastSwapTime < popeData.swapCooldown)
        {
            Debug.LogWarning($"クールタイム中です。残り: {popeData.swapCooldown - (Time.time - lastSwapTime):F1}秒");
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
    /// </summary>
    public float GetRemainingCooldown()
    {
        float elapsed = Time.time - lastSwapTime;
        return Mathf.Max(0, popeData.swapCooldown - elapsed);
    }

    /// <summary>
    /// 位置交換が可能かチェック
    /// </summary>
    public bool CanSwap()
    {
        return GetRemainingCooldown() <= 0;
    }

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
