using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;

/// <summary>
/// 駒管理クラス
/// GameManagerが具体的な駒の型（Farmer, Military等）を見ずに、IDベースでアクセスできるようにする
/// </summary>
public class PieceManager : MonoBehaviour
{
    // ===== 駒の管理 =====
    private Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
    private int nextPieceID = 1;

    // ===== 依存関係 =====
    [SerializeField] private UnitListTable unitListTable;

    // ===== イベント（内部使用・GameManagerには通知しない） =====
    public event Action<int> OnPieceCreated;      // 駒ID
    public event Action<int> OnPieceDied;          // 駒ID
    public event Action<int, int> OnPieceCharmed;  // (駒ID, 新しいplayerID)

    #region 駒の生成

    /// <summary>
    /// 駒を生成（GameManagerから呼び出し）
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="religion">宗教</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された駒のID（失敗時は-1）</returns>
    public int CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
    {
        // UnitListTableからSOデータを取得
        var pieceDetail = new UnitListTable.PieceDetail(pieceType, religion);
        PieceDataSO data = unitListTable.GetPieceDataSO(pieceDetail);

        if (data == null)
        {
            Debug.LogError($"駒データが見つかりません: {pieceType}, {religion}");
            return -1;
        }

        // Prefabから駒を生成
        GameObject pieceObj = Instantiate(data.piecePrefab, position, Quaternion.identity);
        Piece piece = pieceObj.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"Pieceコンポーネントがありません: {pieceType}");
            Destroy(pieceObj);
            return -1;
        }

        // 駒を初期化
        piece.Initialize(data, playerID);

        // IDを割り当てて登録
        int pieceID = nextPieceID++;
        piece.SetPieceID(pieceID);
        pieces[pieceID] = piece;

        // 死亡イベントを購読
        piece.OnPieceDeath += (deadPiece) => HandlePieceDeath(deadPiece.PieceID);

        Debug.Log($"駒を生成しました: ID={pieceID}, Type={pieceType}, Religion={religion}, PlayerID={playerID}");
        OnPieceCreated?.Invoke(pieceID);

        return pieceID;
    }

    #endregion

    #region アップグレード関連

    /// <summary>
    /// 駒の共通項目（HP/AP）をアップグレード
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>成功したらtrue</returns>
    public bool UpgradePiece(int pieceID, PieceUpgradeType upgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return false;
        }

        switch (upgradeType)
        {
            case PieceUpgradeType.HP:
                return piece.UpgradeHP();
            case PieceUpgradeType.AP:
                return piece.UpgradeAP();
            default:
                Debug.LogError($"不明なアップグレードタイプ: {upgradeType}");
                return false;
        }
    }

    /// <summary>
    /// 駒の職業別専用項目をアップグレード
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="specialUpgradeType">職業別アップグレード項目</param>
    /// <returns>成功したらtrue</returns>
    public bool UpgradePieceSpecial(int pieceID, SpecialUpgradeType specialUpgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return false;
        }

        // 型に応じて適切なアップグレードを実行
        switch (piece)
        {
            case Farmer farmer:
                if (specialUpgradeType == SpecialUpgradeType.FarmerSacrifice)
                    return farmer.UpgradeSacrifice();
                break;

            case MilitaryUnit military:
                if (specialUpgradeType == SpecialUpgradeType.MilitaryAttackPower)
                    return military.UpgradeAttackPower();
                break;

            case Missionary missionary:
                if (specialUpgradeType == SpecialUpgradeType.MissionaryOccupy)
                    return missionary.UpgradeOccupy();
                else if (specialUpgradeType == SpecialUpgradeType.MissionaryConvertEnemy)
                    return missionary.UpgradeConvertEnemy();
                break;

            case Pope pope:
                if (specialUpgradeType == SpecialUpgradeType.PopeSwapCooldown)
                    return pope.UpgradeSwapCooldown();
                else if (specialUpgradeType == SpecialUpgradeType.PopeBuff)
                    return pope.UpgradeBuff();
                break;
        }

        Debug.LogError($"駒ID={pieceID}は指定されたアップグレードタイプ={specialUpgradeType}をサポートしていません");
        return false;
    }

    /// <summary>
    /// アップグレードコストを取得
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>コスト（取得失敗時は-1）</returns>
    public int GetUpgradeCost(int pieceID, PieceUpgradeType upgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return -1;
        }

        return piece.GetUpgradeCost(upgradeType);
    }

    /// <summary>
    /// アップグレード可能かチェック
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>アップグレード可能ならtrue</returns>
    public bool CanUpgrade(int pieceID, PieceUpgradeType upgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            return false;
        }

        return piece.CanUpgrade(upgradeType);
    }

    #endregion

    #region 駒の情報取得

    /// <summary>
    /// 駒の現在HPを取得
    /// </summary>
    public float GetPieceHP(int pieceID)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return -1;
        }
        return piece.CurrentHP;
    }

    /// <summary>
    /// 駒の現在APを取得
    /// </summary>
    public float GetPieceAP(int pieceID)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return -1;
        }
        return piece.CurrentAP;
    }

    /// <summary>
    /// 駒の現在プレイヤーIDを取得
    /// </summary>
    public int GetPiecePlayerID(int pieceID)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return -1;
        }
        return piece.CurrentPID;
    }

    /// <summary>
    /// 駒の種類を取得
    /// </summary>
    public PieceType GetPieceType(int pieceID)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return PieceType.None;
        }

        return piece switch
        {
            Farmer => PieceType.Farmer,
            MilitaryUnit => PieceType.Military,
            Missionary => PieceType.Missionary,
            Pope => PieceType.Pope,
            _ => PieceType.None
        };
    }

    /// <summary>
    /// 駒が存在するかチェック
    /// </summary>
    public bool DoesPieceExist(int pieceID)
    {
        return pieces.ContainsKey(pieceID);
    }

    /// <summary>
    /// 指定プレイヤーのすべての駒IDを取得
    /// </summary>
    public List<int> GetPlayerPieces(int playerID)
    {
        return pieces
            .Where(kvp => kvp.Value.CurrentPID == playerID && kvp.Value.IsAlive)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// 指定プレイヤーの指定種類の駒IDを取得
    /// </summary>
    public List<int> GetPlayerPiecesByType(int playerID, PieceType pieceType)
    {
        return GetPlayerPieces(playerID)
            .Where(pieceID => GetPieceType(pieceID) == pieceType)
            .ToList();
    }

    #endregion

    #region 駒の削除

    /// <summary>
    /// 駒を削除（死亡時などに内部から呼び出し）
    /// </summary>
    private void HandlePieceDeath(int pieceID)
    {
        if (pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.Log($"駒が死亡しました: ID={pieceID}");
            pieces.Remove(pieceID);
            OnPieceDied?.Invoke(pieceID);

            if (piece != null && piece.gameObject != null)
            {
                Destroy(piece.gameObject);
            }
        }
    }

    /// <summary>
    /// 駒を強制削除（GameManager側から呼び出し可能）
    /// </summary>
    public bool RemovePiece(int pieceID)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return false;
        }

        pieces.Remove(pieceID);
        if (piece != null && piece.gameObject != null)
        {
            Destroy(piece.gameObject);
        }

        Debug.Log($"駒を削除しました: ID={pieceID}");
        return true;
    }

    #endregion

    #region 駒の行動

    /// <summary>
    /// 軍隊が敵を攻撃
    /// </summary>
    /// <param name="attackerID">攻撃者の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <returns>攻撃成功したらtrue</returns>
    public bool AttackEnemy(int attackerID, int targetID)
    {
        if (!pieces.TryGetValue(attackerID, out Piece attacker))
        {
            Debug.LogError($"攻撃者が見つかりません: ID={attackerID}");
            return false;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return false;
        }

        if (attacker is not MilitaryUnit military)
        {
            Debug.LogError($"駒ID={attackerID}は軍隊ではありません");
            return false;
        }

        return military.Attack(target);
    }

    /// <summary>
    /// 宣教師が敵を魅惑
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <returns>魅惑試行成功したらtrue（成功率判定は内部で実施）</returns>
    public bool ConvertEnemy(int missionaryID, int targetID)
    {
        if (!pieces.TryGetValue(missionaryID, out Piece missionaryPiece))
        {
            Debug.LogError($"宣教師が見つかりません: ID={missionaryID}");
            return false;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return false;
        }

        if (missionaryPiece is not Missionary missionary)
        {
            Debug.LogError($"駒ID={missionaryID}は宣教師ではありません");
            return false;
        }

        return missionary.ConversionAttack(target);
    }

    /// <summary>
    /// 宣教師が領地を占領
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetPosition">占領対象の領地座標</param>
    /// <returns>占領試行成功したらtrue（成功率判定は内部で実施）</returns>
    public bool OccupyTerritory(int missionaryID, Vector3 targetPosition)
    {
        if (!pieces.TryGetValue(missionaryID, out Piece missionaryPiece))
        {
            Debug.LogError($"宣教師が見つかりません: ID={missionaryID}");
            return false;
        }

        if (missionaryPiece is not Missionary missionary)
        {
            Debug.LogError($"駒ID={missionaryID}は宣教師ではありません");
            return false;
        }

        return missionary.StartOccupy(targetPosition);
    }

    /// <summary>
    /// 農民が他の駒を回復（獻祭）
    /// </summary>
    /// <param name="farmerID">農民の駒ID</param>
    /// <param name="targetID">回復対象の駒ID</param>
    /// <returns>回復成功したらtrue</returns>
    public bool SacrificeToPiece(int farmerID, int targetID)
    {
        if (!pieces.TryGetValue(farmerID, out Piece farmerPiece))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return false;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return false;
        }

        if (farmerPiece is not Farmer farmer)
        {
            Debug.LogError($"駒ID={farmerID}は農民ではありません");
            return false;
        }

        return farmer.Sacrifice(target);
    }

    /// <summary>
    /// 教皇が味方駒と位置を交換
    /// </summary>
    /// <param name="popeID">教皇の駒ID</param>
    /// <param name="targetID">交換対象の駒ID</param>
    /// <returns>交換成功したらtrue</returns>
    public bool SwapPositions(int popeID, int targetID)
    {
        if (!pieces.TryGetValue(popeID, out Piece popePiece))
        {
            Debug.LogError($"教皇が見つかりません: ID={popeID}");
            return false;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return false;
        }

        if (popePiece is not Pope pope)
        {
            Debug.LogError($"駒ID={popeID}は教皇ではありません");
            return false;
        }

        return pope.SwapPositionWith(target);
    }

    /// <summary>
    /// 駒にダメージを与える
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="damage">ダメージ量</param>
    /// <param name="attackerID">攻撃者ID（オプション）</param>
    public void DamagePiece(int pieceID, float damage, int attackerID = -1)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return;
        }

        Piece attacker = null;
        if (attackerID >= 0 && pieces.TryGetValue(attackerID, out attacker))
        {
            piece.TakeDamage(damage, attacker);
        }
        else
        {
            piece.TakeDamage(damage);
        }
    }

    /// <summary>
    /// 駒を回復
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="amount">回復量</param>
    public void HealPiece(int pieceID, float amount)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return;
        }

        piece.Heal(amount);
    }

    #endregion

    #region AP管理

    /// <summary>
    /// 駒のAPを消費
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="amount">消費量</param>
    /// <returns>消費成功したらtrue</returns>
    public bool ConsumePieceAP(int pieceID, float amount)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return false;
        }

        return piece.ConsumeAP(amount);
    }

    /// <summary>
    /// 駒のAPを回復
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="amount">回復量</param>
    public void RecoverPieceAP(int pieceID, float amount)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return;
        }

        piece.RecoverAP(amount);
    }

    #endregion

    #region 農民取得（BuildingManager用）

    /// <summary>
    /// 農民インスタンスを取得（BuildingManagerが建物配置に使用）
    /// </summary>
    /// <param name="farmerID">農民の駒ID</param>
    /// <returns>Farmerインスタンス（失敗時はnull）</returns>
    public Farmer GetFarmer(int farmerID)
    {
        if (!pieces.TryGetValue(farmerID, out Piece piece))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return null;
        }

        if (piece is not Farmer farmer)
        {
            Debug.LogError($"駒ID={farmerID}は農民ではありません");
            return null;
        }

        return farmer;
    }

    #endregion

    #region ターン処理

    /// <summary>
    /// 指定プレイヤーのターン開始処理（AP回復など）
    /// </summary>
    public void ProcessTurnStart(int playerID)
    {
        var playerPieces = GetPlayerPieces(playerID);

        foreach (int pieceID in playerPieces)
        {
            if (pieces.TryGetValue(pieceID, out Piece piece))
            {
                // AP回復などの処理があればここで実行
                // piece.RecoverAP(); などを呼び出す
            }
        }

        Debug.Log($"プレイヤー{playerID}のターン開始処理を実行しました（駒数: {playerPieces.Count}）");
    }

    #endregion
}

/// <summary>
/// 駒共通のアップグレード項目
/// </summary>
public enum PieceUpgradeType
{
    HP,
    AP
}

/// <summary>
/// 職業別専用アップグレード項目
/// </summary>
public enum SpecialUpgradeType
{
    // 農民
    FarmerSacrifice,           // 獻祭回復量

    // 軍隊
    MilitaryAttackPower,       // 攻撃力

    // 宣教師
    MissionaryOccupy,          // 占領成功率
    MissionaryConvertEnemy,    // 魅惑成功率

    // 教皇
    PopeSwapCooldown,          // 位置交換CD
    PopeBuff                   // バフ効果
}
