using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;
using DG.Tweening.Core.Easing;




public struct syncPieceData
{
    public PieceType piecetype;
    public Religion religion;
    public Vector3 piecePos;
    public int playerID;
    public int pieceID;
    public int currentHP;
    public int currentHPLevel;
    public int currentPID;
    public int swapCooldownLevel;
    public int buffLevel;
    public int occupyLevel;
    public int convertEnemyLevel;
    public int sacrificeLevel;
    public int attackPowerLevel;
}

/// <summary>
/// 用于SwapPositions的同步数据结构，包含两个棋子的同步数据
/// </summary>
public struct swapPieceData
{
    public syncPieceData piece1;
    public syncPieceData piece2;
}


/// <summary>
/// 駒管理クラス
/// GameManagerが具体的な駒の型（Farmer, Military等）を見ずに、IDベースでアクセスできるようにする
/// </summary>
public class PieceManager : MonoBehaviour
{

    private static PieceManager _instance;
    public static PieceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PieceManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    _instance = obj.AddComponent<PieceManager>();
                }
            }
            return _instance;
        }
    }


    // ===== 駒の管理 =====
    private Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
    private Dictionary<int ,Piece> enemyPieces = new Dictionary<int ,Piece>();
    private int nextPieceID = 0;
    private int localPlayerID = -1; // このPieceManagerが管理するプレイヤーのID

    // ===== 依存関係 =====
    [SerializeField] private UnitListTable unitListTable;

    // ===== イベント=====
    public event Action<int> OnPieceCreated;          // 駒ID
    public event Action<int> OnPieceDied;              // 駒ID
    public event Action<int, int> OnPieceCharmed;      // (駒ID, 新しいplayerID)
    public event Action<int> OnEnemyPieceCreated;      // 敵駒ID


    public event Action<int, int> OnPieceHPChanged;
    public event Action<int, int> OnPieceHPLevelUpgraded;
    public event Action<int, int> OnPopeSwapCDLevelUpgraded;
    public event Action<int, int> OnPopeBuffLevelUpgraded;
    public event Action<int, int> OnMissionaryOccupyLevelUpgraded;
    public event Action<int, int> OnMissionaryConvertLevelUpgraded;
    public event Action<int, int> OnFarmerSacrificeLevelUpgraded;
    public event Action<int, int> OnMilitaryAttackLevelUpgraded;



    void Awake()
    {
        // 既にインスタンスが存在する場合は破棄
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // シーン遷移で破棄されない
    }

    /// <summary>
    /// このPieceManagerが管理するプレイヤーIDを設定
    /// </summary>
    public void SetLocalPlayerID(int playerID)
    {
        localPlayerID = playerID;
        Debug.Log($"PieceManagerのローカルプレイヤーIDを設定しました: {playerID}");
    }

    /// <summary>
    /// ローカルプレイヤーIDを取得
    /// </summary>
    public int GetLocalPlayerID()
    {
        return localPlayerID;
    }


    #region 駒の同期
    ///<summary>
    ///
    ///</summary>
    public syncPieceData ChangeHPData(int pieceID, int hp)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            currentHP = hp
        };
        return spd;
    }


    public syncPieceData ChangeHPLevelData(int pieceID, int hplevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            currentHPLevel = hplevel
        };
        return spd;
    }
    public syncPieceData ChangeFarmerLevelData(int pieceID, int sacrificelevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            sacrificeLevel = sacrificelevel
        };
        return spd;
    }
    public syncPieceData ChangeMilitaryAtkLevelData(int pieceID, int atklevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            attackPowerLevel = atklevel
        };
        return spd;
    }
    public syncPieceData ChangePopeSwapCDLevelData(int pieceID, int cdlevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            swapCooldownLevel = cdlevel
        };
        return spd;
    }
    public syncPieceData ChangePopeBuffLevelData(int pieceID, int bufflevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            buffLevel = bufflevel
        };
        return spd;
    }

    public syncPieceData ChangeMissionaryConvertLevelData(int pieceID, int convertlevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            convertEnemyLevel = convertlevel
        };
        return spd;
    }
    public syncPieceData ChangeMissionaryOccupyLevelData(int pieceID, int occupylevel)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            occupyLevel = occupylevel
        };
        return spd;
    }
    public syncPieceData ChangePieceCurrentPID(int pieceID, int currentpid)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            currentPID = currentpid
        };
        return spd;
    }

    public syncPieceData ChangePiecePosData(int pieceID, Vector3 position)
    {
        syncPieceData spd = new syncPieceData
        {
            pieceID = pieceID,
            piecePos = position
        };
        return spd;
    }

    #endregion

    #region 駒の生成

    /// <summary>
    /// 駒を生成（GameManagerから呼び出し）
    /// </summary>
    /// <param name="pieceType">駒の種類</param>
    /// <param name="religion">宗教</param>
    /// <param name="playerID">プレイヤーID</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成された駒の同期データ（失敗時はnull）</returns>
    public syncPieceData? CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
    {
        // UnitListTableからSOデータを取得
        var pieceDetail = new UnitListTable.PieceDetail(pieceType, religion);
        PieceDataSO data = unitListTable.GetPieceDataSO(pieceDetail);

        if (data == null)
        {
            Debug.LogError($"駒データが見つかりません: {pieceType}, {religion}");
            return null;
        }

        // Prefabから駒を生成
        GameObject pieceObj = Instantiate(data.piecePrefab, position, Quaternion.identity);
        Piece piece = pieceObj.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"Pieceコンポーネントがありません: {pieceType}");
            Destroy(pieceObj);
            return null;
        }

        // 駒を初期化
        piece.Initialize(data, playerID);

        // IDを割り当てて登録
        int pieceID = nextPieceID;
        piece.SetPieceID(pieceID);
        pieces[pieceID] = piece;
        nextPieceID++;

        // 死亡イベントを購読
        piece.OnPieceDeath += (deadPiece) => HandlePieceDeath(deadPiece.PieceID);

        Debug.Log($"駒を生成しました: ID={pieceID}, Type={pieceType}, Religion={religion}, PlayerID={playerID}");
        OnPieceCreated?.Invoke(pieceID);

        // 只需要返回基本信息的同步数据
        return new syncPieceData
        {
            pieceID = pieceID,
            piecetype = pieceType,
            religion = religion,
            playerID = playerID,
            piecePos = position,
            currentHP = (int)piece.CurrentHP,
            currentPID = playerID
        };
    }

    /// <summary>
    /// 基于同步数据创建敌人棋子（用于网络同步）
    /// </summary>
    /// <param name="spd">敌人棋子的同步数据</param>
    /// <returns>成功创建则返回true，失败返回false</returns>
    public bool CreateEnemyPiece(syncPieceData spd)
    {
        // UnitListTableからSOデータを取得
        var pieceDetail = new UnitListTable.PieceDetail(spd.piecetype, spd.religion);
        PieceDataSO data = unitListTable.GetPieceDataSO(pieceDetail);

        if (data == null)
        {
            Debug.LogError($"敵駒データが見つかりません: {spd.piecetype}, {spd.religion}");
            return false;
        }

        // Prefabから駒を生成
        GameObject pieceObj = Instantiate(data.piecePrefab, spd.piecePos, Quaternion.identity);
        Piece piece = pieceObj.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"Pieceコンポーネントがありません: {spd.piecetype}");
            Destroy(pieceObj);
            return false;
        }

        // 駒を初期化
        piece.Initialize(data, spd.playerID);

        // 同步ID（使用来自网络的ID）
        piece.SetPieceID(spd.pieceID);
        
        // 记录到敌人棋子集合
        enemyPieces[spd.pieceID] = piece;

        // 死亡イベントを購読
        piece.OnPieceDeath += (deadPiece) => HandlePieceDeath(deadPiece.PieceID);

        // syncPieceDataから状態を設定
        try
        {
            // 基本ステータスを同期
            if (spd.currentHP > 0)
            {
                piece.SetHP(spd.currentHP);
            }

            if (spd.currentHPLevel > 0)
            {
                piece.SetHPLevel(spd.currentHPLevel);
            }

            // プレイヤーIDを同期（魅惑された場合など）
            if (spd.currentPID != spd.playerID)
            {
                piece.SetPlayerID(spd.currentPID);
            }

            // 職業別の専用レベルを同期
            switch (piece)
            {
                case Pope pope:
                    if (spd.swapCooldownLevel > 0)
                        pope.SetSwapCooldownLevel(spd.swapCooldownLevel);
                    if (spd.buffLevel > 0)
                        pope.SetBuffLevel(spd.buffLevel);
                    break;

                case Missionary missionary:
                    if (spd.occupyLevel > 0)
                        missionary.SetOccupyLevel(spd.occupyLevel);
                    if (spd.convertEnemyLevel > 0)
                        missionary.SetConvertEnemyLevel(spd.convertEnemyLevel);
                    break;

                case Farmer farmer:
                    if (spd.sacrificeLevel > 0)
                        farmer.SetSacrificeLevel(spd.sacrificeLevel);
                    break;

                case MilitaryUnit military:
                    if (spd.attackPowerLevel > 0)
                        military.SetAttackPowerLevel(spd.attackPowerLevel);
                    break;
            }

            Debug.Log($"敵駒を生成しました: ID={spd.pieceID}, Type={spd.piecetype}, Religion={spd.religion}, PlayerID={spd.playerID}");
            OnEnemyPieceCreated?.Invoke(spd.pieceID);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"敵駒の状態設定中にエラーが発生しました: {e.Message}");
            // エラーが発生しても駒は作成されているので、初期状態で使用可能
            return true;
        }
    }

    /// <summary>
    /// 敵駒の詳細な状態を同期する（ネットワーク同期用）
    /// </summary>
    /// <param name="spd">同期データ</param>
    /// <returns>同期成功したらtrue</returns>
    public bool SyncEnemyPieceState(syncPieceData spd)
    {
        // 敵駒を取得（敵駒辞書から検索）
        if (!enemyPieces.TryGetValue(spd.pieceID, out Piece piece))
        {
            Debug.LogError($"敵駒が見つかりません: ID={spd.pieceID}");
            return false;
        }

        try
        {
            // 基本ステータスを同期
            if (spd.currentHP > 0)
            {
                piece.SetHP(spd.currentHP);
            }

            if (spd.currentHPLevel > 0)
            {
                piece.SetHPLevel(spd.currentHPLevel);
            }

            // プレイヤーIDを同期（魅惑された場合など）
            if (spd.currentPID != piece.CurrentPID)
            {
                piece.SetPlayerID(spd.currentPID);
            }

            // 職業別の専用レベルを同期
            switch (piece)
            {
                case Pope pope:
                    if (spd.swapCooldownLevel > 0)
                        pope.SetSwapCooldownLevel(spd.swapCooldownLevel);
                    if (spd.buffLevel > 0)
                        pope.SetBuffLevel(spd.buffLevel);
                    break;

                case Missionary missionary:
                    if (spd.occupyLevel > 0)
                        missionary.SetOccupyLevel(spd.occupyLevel);
                    if (spd.convertEnemyLevel > 0)
                        missionary.SetConvertEnemyLevel(spd.convertEnemyLevel);
                    break;

                case Farmer farmer:
                    if (spd.sacrificeLevel > 0)
                        farmer.SetSacrificeLevel(spd.sacrificeLevel);
                    break;

                case MilitaryUnit military:
                    if (spd.attackPowerLevel > 0)
                        military.SetAttackPowerLevel(spd.attackPowerLevel);
                    break;
            }

            Debug.Log($"敵駒の状態を同期しました: ID={spd.pieceID}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"敵駒の状態同期中にエラーが発生しました: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 駒の完全な同期データを生成
    /// 注意: この方法では宗教情報を取得できないため、呼び出し側で宗教情報を提供する必要があります
    /// </summary>
    private syncPieceData CreateCompleteSyncData(Piece piece, Religion religion = Religion.None)
    {
        // 駒の種類を取得
        PieceType pieceType = GetPieceTypeFromPiece(piece);

        return new syncPieceData
        {
            piecetype = pieceType,
            religion = religion,
            piecePos = piece.transform.position,
            playerID = piece.CurrentPID,
            pieceID = piece.PieceID,
            currentHP = piece.CurrentHP,
            currentHPLevel = piece.HPLevel,
            currentPID = piece.CurrentPID,
            swapCooldownLevel = (piece is Pope pope) ? pope.SwapCooldownLevel : 0,
            buffLevel = (piece is Pope pope2) ? pope2.BuffLevel : 0,
            occupyLevel = (piece is Missionary missionary) ? missionary.OccupyLevel : 0,
            convertEnemyLevel = (piece is Missionary missionary2) ? missionary2.ConvertEnemyLevel : 0,
            sacrificeLevel = (piece is Farmer farmer) ? farmer.SacrificeLevel : 0,
            attackPowerLevel = (piece is MilitaryUnit military) ? military.AttackPowerLevel : 0
        };
    }

    private PieceType GetPieceTypeFromPiece(Piece piece)
    {
        return piece switch
        {
            Farmer => PieceType.Farmer,
            MilitaryUnit => PieceType.Military,
            Missionary => PieceType.Missionary,
            Pope => PieceType.Pope,
            _ => PieceType.None
        };
    }

    #endregion

    #region アップグレード関連

    /// <summary>
    /// 駒の共通項目（HP/AP）をアップグレード
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="upgradeType">アップグレード項目</param>
    /// <returns>成功したらsyncPieceDataを返す</returns>
    public syncPieceData? UpgradePiece(int pieceID, PieceUpgradeType upgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        switch (upgradeType)
        {
            case PieceUpgradeType.HP:
                piece.UpgradeHP();
                return ChangeHPData(pieceID, (int)piece.CurrentHP);
            case PieceUpgradeType.AP:
                piece.UpgradeAP();
                return null;
            default:
                Debug.LogError($"不明なアップグレードタイプ: {upgradeType}");
                return null;
        }
    }

    /// <summary>
    /// 駒の職業別専用項目をアップグレード
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="specialUpgradeType">職業別アップグレード項目</param>
    /// <returns>成功したらsyncPieceDataを返す</returns>
    public syncPieceData? UpgradePieceSpecial(int pieceID, SpecialUpgradeType specialUpgradeType)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        // 型に応じて適切なアップグレードを実行
        switch (piece)
        {
            case Farmer farmer:
                if (specialUpgradeType == SpecialUpgradeType.FarmerSacrifice)
                    if (farmer.UpgradeSacrifice())
                    {
                        return ChangeFarmerLevelData(pieceID, farmer.SacrificeLevel);
                    }
                    else
                        return null;
                break;

            case MilitaryUnit military:
                if (specialUpgradeType == SpecialUpgradeType.MilitaryAttackPower)
                    if (military.UpgradeAttackPower())
                    {
                        return ChangeMilitaryAtkLevelData(pieceID, military.AttackPowerLevel);
                    }
                break;

            case Missionary missionary:
                if (specialUpgradeType == SpecialUpgradeType.MissionaryOccupy)
                    if (missionary.UpgradeOccupy())
                    {
                        return ChangeMissionaryOccupyLevelData(pieceID, missionary.OccupyLevel);
                    }
                else if (specialUpgradeType == SpecialUpgradeType.MissionaryConvertEnemy)
                    if (missionary.UpgradeConvertEnemy())
                    {
                        return ChangeMissionaryConvertLevelData(pieceID, missionary.ConvertEnemyLevel);
                    }
                break;

            case Pope pope:
                if (specialUpgradeType == SpecialUpgradeType.PopeSwapCooldown)
                    if (pope.UpgradeSwapCooldown())
                    {
                        return ChangePopeSwapCDLevelData(pieceID, pope.SwapCooldownLevel);
                    }
                else if (specialUpgradeType == SpecialUpgradeType.PopeBuff)
                    if (pope.UpgradeBuff())
                    {
                        return ChangePopeBuffLevelData(pieceID, pope.BuffLevel);
                    }
                break;
        }

        Debug.LogError($"駒ID={pieceID}は指定されたアップグレードタイプ={specialUpgradeType}をサポートしていません");
        return null;
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
    /// <summary>
    /// 获取任意棋子（己方或敌方）
    /// </summary>
    /// <param name="pieceID">棋子ID</param>
    /// <returns>找到的棋子，如果未找到返回null</returns>
    public Piece GetPiece(int pieceID)
    {
        if (pieces.TryGetValue(pieceID, out Piece piece))
        {
            return piece;
        }
        if (enemyPieces.TryGetValue(pieceID, out Piece enemyPiece))
        {
            return enemyPiece;
        }
        return null;
    }

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

    // 最後に死亡した駒のデータ（GMが取得できるようにキャッシュ）
    private syncPieceData? lastDeadPieceData = null;

    /// <summary>
    /// 駒を削除（自分の行動で駒が死んだ場合・送信側）
    /// イベントから呼び出される
    /// </summary>
    private void HandlePieceDeath(int pieceID)
    {
        // 両方の辞書を確認
        Piece piece = null;
        bool isLocalPiece = pieces.TryGetValue(pieceID, out piece);
        bool isEnemyPiece = !isLocalPiece && enemyPieces.TryGetValue(pieceID, out piece);

        if (!isLocalPiece && !isEnemyPiece)
        {
            Debug.LogWarning($"死亡した駒が見つかりません: ID={pieceID}");
            return;
        }

        // 死亡した駒のsyncPieceDataを作成してキャッシュ
        lastDeadPieceData = new syncPieceData
        {
            pieceID = pieceID,
            currentHP = 0 // 死亡を明示
        };

        // 駒を削除（内部処理）
        RemovePieceInternal(pieceID, piece);

        // GameManagerに通知（相手に送信するため）
        OnPieceDied?.Invoke(pieceID);

        Debug.Log($"駒が死亡しました（送信通知）: ID={pieceID}, IsLocal={isLocalPiece}");
    }

    /// <summary>
    /// 最後に死亡した駒のsyncPieceDataを取得
    /// GameManagerがOnPieceDeathイベント受信時に呼び出す
    /// </summary>
    /// <returns>死亡した駒のsyncPieceData、無い場合はnull</returns>
    public syncPieceData? GetLastDeadPieceData()
    {
        syncPieceData? data = lastDeadPieceData;
        lastDeadPieceData = null; // 取得後はクリア
        return data;
    }

    /// <summary>
    /// ネットワークから駒の死亡通知を受信した場合（受信側）
    /// GameManagerから呼び出される
    /// </summary>
    /// <param name="spd">死亡した駒の同期データ</param>
    /// <returns>削除成功したらtrue</returns>
    public bool HandleEnemyPieceDeath(syncPieceData spd)
    {
        // 両方の辞書を確認
        Piece piece = null;
        bool found = pieces.TryGetValue(spd.pieceID, out piece) ||
                     enemyPieces.TryGetValue(spd.pieceID, out piece);

        if (!found)
        {
            Debug.LogWarning($"死亡通知を受け取ったが、駒が見つかりません: ID={spd.pieceID}");
            return false;
        }

        // 駒を削除（内部処理のみ、送信は行わない）
        RemovePieceInternal(spd.pieceID, piece);

        Debug.Log($"敵駒の死亡通知を受信しました: ID={spd.pieceID}");
        return true;
    }

    /// <summary>
    /// 駒を辞書から削除してGameObjectを破棄（共通処理）
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="piece">駒のインスタンス</param>
    private void RemovePieceInternal(int pieceID, Piece piece)
    {
        if (piece == null)
        {
            Debug.LogError($"RemovePieceInternalに渡された駒がnullです: ID={pieceID}");
            return;
        }

        // OriginalPIDで自分の駒か敵駒かを判定
        if (localPlayerID != -1 && piece.OriginalPID == localPlayerID)
        {
            // 自分の駒
            if (pieces.ContainsKey(pieceID))
            {
                pieces.Remove(pieceID);
                Debug.Log($"自分の駒を削除しました: ID={pieceID}, Type={piece.Data?.pieceName}");
            }
        }
        else
        {
            // 敵の駒
            if (enemyPieces.ContainsKey(pieceID))
            {
                enemyPieces.Remove(pieceID);
                Debug.Log($"敵駒を削除しました: ID={pieceID}, Type={piece.Data?.pieceName}");
            }
        }

        // GameObjectを破棄
        if (piece.gameObject != null)
        {
            Destroy(piece.gameObject);
        }
    }

    /// <summary>
    /// 駒を強制削除（GameManager側から呼び出し可能）
    /// </summary>
    public syncPieceData? RemovePiece(int pieceID)
    {
        Piece piece;
        bool isEnemyPiece = false;

        // 先尝试从己方棋子中移除
        if (pieces.TryGetValue(pieceID, out piece))
        {
            isEnemyPiece = false;
        }
        // 如果在己方棋子中没找到，再尝试从敌人棋子中移除
        else if (enemyPieces.TryGetValue(pieceID, out piece))
        {
            isEnemyPiece = true;
        }
        else
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        // 删除只同步pieceID信息就足够了（双方棋子创建顺序一致，pieceID可唯一标识棋子）
        syncPieceData syncData = new syncPieceData
        {
            pieceID = pieceID
        };
        
        if (isEnemyPiece)
        {
            enemyPieces.Remove(pieceID);
        }
        else
        {
            pieces.Remove(pieceID);
        }
        
        if (piece != null && piece.gameObject != null)
        {
            Destroy(piece.gameObject);
        }

        Debug.Log($"駒を削除しました: ID={pieceID}");
        return syncData;
    }

    #endregion

    #region 駒の行動

    /// <summary>
    /// 軍隊が敵を攻撃
    /// </summary>
    /// <param name="attackerID">攻撃者の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <returns>攻撃成功したら攻撃された側の現HPをGameManagerに渡す</returns>
    public syncPieceData? AttackEnemy(int attackerID, int targetID)
    {
        if (!pieces.TryGetValue(attackerID, out Piece attacker))
        {
            Debug.LogError($"攻撃者が見つかりません: ID={attackerID}");
            return null;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return null;
        }

        if (attacker is not MilitaryUnit military)
        {
            Debug.LogError($"駒ID={attackerID}は軍隊ではありません");
            return null;
        }

        if (military.Attack(target))
        {
            return ChangeHPData(targetID, (int)target.CurrentHP);
        }
        return null;
    }

    /// <summary>
    /// 宣教師が敵を魅惑
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <returns>魅惑試行成功したらtrue（成功率判定は内部で実施）</returns>
    public syncPieceData? ConvertEnemy(int missionaryID, int targetID)
    {
        if (!pieces.TryGetValue(missionaryID, out Piece missionaryPiece))
        {
            Debug.LogError($"宣教師が見つかりません: ID={missionaryID}");
            return null;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return null;
        }

        if (missionaryPiece is not Missionary missionary)
        {
            Debug.LogError($"駒ID={missionaryID}は宣教師ではありません");
            return null;
        }

        if (missionary.ConversionAttack(target))
        {
            return ChangePieceCurrentPID(targetID, missionary.CurrentPID);
        }
        return null;
    }

    /// <summary>
    /// 宣教師が領地を占領
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetPosition">占領対象の領地座標</param>
    /// <returns>占領試行成功したらtrueを返す</returns>
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
    /// <returns>回復成功したらtargetの同期データを返す</returns>
    public syncPieceData? SacrificeToPiece(int farmerID, int targetID)
    {
        if (!pieces.TryGetValue(farmerID, out Piece farmerPiece))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return null;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return null;
        }

        if (farmerPiece is not Farmer farmer)
        {
            Debug.LogError($"駒ID={farmerID}は農民ではありません");
            return null;
        }

        if (farmer.Sacrifice(target))
        {
            return ChangeHPData(targetID, (int)target.CurrentHP);
        }
        return null;
    }

    /// <summary>
    /// 教皇が味方駒と位置を交換
    /// </summary>
    /// <param name="popeID">教皇の駒ID</param>
    /// <param name="targetID">交換対象の駒ID</param>
    /// <returns>交換成功したら両方の棋子の同期データを返す</returns>
    public swapPieceData? SwapPositions(int popeID, int targetID)
    {
        if (!pieces.TryGetValue(popeID, out Piece popePiece))
        {
            Debug.LogError($"教皇が見つかりません: ID={popeID}");
            return null;
        }

        if (!pieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"ターゲットが見つかりません: ID={targetID}");
            return null;
        }

        if (popePiece is not Pope pope)
        {
            Debug.LogError($"駒ID={popeID}は教皇ではありません");
            return null;
        }

        if (pope.SwapPositionWith(target))
        {
            swapPieceData swapData = new swapPieceData
            {
                piece1 = ChangePiecePosData(popeID, popePiece.transform.position),
                piece2 = ChangePiecePosData(targetID, target.transform.position)
            };
            return swapData;
        }
        return null;
    }

    /// <summary>
    /// 駒にダメージを与える
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="damage">ダメージ量</param>
    /// <param name="attackerID">攻撃者ID（オプション）</param>
    /// <returns>ダメージ後の同棋ProcessHPDelta</returns>
    public syncPieceData? DamagePiece(int pieceID, int damage, int attackerID = -1)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
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

        return ChangeHPData(pieceID, (int)piece.CurrentHP);
    }

    /// <summary>
    /// 駒を回復
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="amount">回復量</param>
    /// <returns>回復後の棋ProcessHPDelta</returns>
    public syncPieceData? HealPiece(int pieceID, int amount)
    {
        if (!pieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        piece.Heal(amount);
        return ChangeHPData(pieceID, (int)piece.CurrentHP);
    }

    #endregion

    #region AP管理

    /// <summary>
    /// 駒のAPを消費
    /// </summary>
    /// <param name="pieceID">駒ID</param>
    /// <param name="amount">消費量</param>
    /// <returns>消費成功したらtrue</returns>
    public bool ConsumePieceAP(int pieceID, int amount)
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
    public void RecoverPieceAP(int pieceID, int amount)
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
