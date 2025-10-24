using UnityEngine;
using GameData;
using GamePieces;

/// <summary>
/// 各職業の駒生成を行うクラス
/// </summary>
public class Instantiater : MonoBehaviour
{
    [Header("ScriptableObject Data")]
    [SerializeField] private UnitListTable unitListTable;

    /// <summary>
    /// 農民を生成
    /// </summary>
    public Piece CreateFarmer(Religion religion, int playerID, Vector3 position)
    {
        return CreatePiece(PieceType.Farmer, religion, playerID, position);
    }

    /// <summary>
    /// 宣教師を生成
    /// </summary>
    public Piece CreateMissionary(Religion religion, int playerID, Vector3 position)
    {
        return CreatePiece(PieceType.Missionary, religion, playerID, position);
    }

    /// <summary>
    /// 十字軍を生成
    /// </summary>
    public Piece CreateMilitary(Religion religion, int playerID, Vector3 position)
    {
        return CreatePiece(PieceType.Military, religion, playerID, position);
    }

    /// <summary>
    /// 汎用的な駒生成メソッド（PieceFactoryを活用）
    /// </summary>
    public Piece CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
    {
        if (unitListTable == null)
        {
            Debug.LogError("エラー: UnitListTableが設定されていません");
            return null;
        }

        // UnitListTableから対応するPieceDataSOを取得
        var pieceDetail = new UnitListTable.PieceDetail(pieceType, religion);
        PieceDataSO pieceData = unitListTable.GetPieceDataSO(pieceDetail);

        if (pieceData == null)
        {
            Debug.LogError($"エラー: {pieceType} ({religion}) のデータが見つかりません");
            return null;
        }

        // PieceFactoryを使って駒を生成
        Piece piece = PieceFactory.CreatePiece(pieceData, position, playerID);

        if (piece != null)
        {
            Debug.Log($"{pieceType} ({religion}) を生成しました (位置: {position}, PID: {playerID})");
        }
        else
        {
            Debug.LogError($"エラー: {pieceType} ({religion}) の生成に失敗しました");
        }

        return piece;
    }
}
