using GameData;
using GamePieces;

using UnityEngine;

/// <summary>
/// 駒ファクトリー
/// </summary>
public static class PieceFactory
{
    /// <summary>
    /// 駒を生成（プレイヤーIDで管理）
    /// </summary>
    /// <param name="data">駒のデータ</param>
    /// <param name="position">生成位置</param>
    /// <param name="playerID">所属するプレイヤーID</param>
    /// <returns>生成された駒</returns>
    public static Piece CreatePiece(PieceDataSO data, Vector3 position, int playerID)
    {
        if (data == null || data.piecePrefab == null)
        {
            Debug.LogError("駒データまたはPrefabが設定されていません");
            return null;
        }

        GameObject pieceObj = GameObject.Instantiate(data.piecePrefab, position, Quaternion.identity);
        Piece piece = null;

        // データ型に応じて適切なコンポーネントを追加
        ///全てのprefabには事前に対応するスクリプトを付けておく事で
        ///ここのAddComponentを回避することが出来る。（AddComponentは重いから）
        if (data is FarmerDataSO)
        {
            piece = pieceObj.GetComponent<Farmer>() ?? pieceObj.AddComponent<Farmer>();
        }
        else if (data is MilitaryDataSO)
        {
            piece = pieceObj.GetComponent<MilitaryUnit>() ?? pieceObj.AddComponent<MilitaryUnit>();
        }
        else if (data is MissionaryDataSO)
        {
            piece=pieceObj.GetComponent<Missionary>()??pieceObj.AddComponent<Missionary>();
        }
        else if(data is PopeDataSO)
        {
            piece = pieceObj.GetComponent <Pope>()?? pieceObj.AddComponent<Pope>();
        }
        else
        {
            Debug.LogError("未知の駒データタイプ");
            GameObject.Destroy(pieceObj);
            return null;
        }

        piece.Initialize(data, playerID);
        return piece;
    }
}