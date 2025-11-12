using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GamePieces;
using DG.Tweening.Core.Easing;
using UnityEngine.UIElements;

//25.11.4 RI 添加序列化Vector3 变量

/// <summary>
/// 可序列化的Vector3包装类，用于网络传输
/// 避免Unity Vector3的循环引用问题
/// </summary>
[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(Vector3 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
        this.z = vector.z;
    }

    // 隐式转换：SerializableVector3 -> Vector3
    public static implicit operator Vector3(SerializableVector3 sv)
    {
        return new Vector3(sv.x, sv.y, sv.z);
    }

    // 隐式转换：Vector3 -> SerializableVector3
    public static implicit operator SerializableVector3(Vector3 v)
    {
        return new SerializableVector3(v.x, v.y, v.z);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}

public struct syncPieceData
{
    public PieceType piecetype;
    public Religion religion;
    public SerializableVector3 piecePos;
    public int pieceID;
    public int currentHP;
    public int currentHPLevel;
    public int currentAP;
    public int currentAPLevel;
    public int currentPID;
    public int swapCooldownLevel;
    public int buffLevel;
    public int occupyLevel;
    public int convertEnemyLevel;
    public int sacrificeLevel;
    public int attackPowerLevel;
    public int charmedTurnsRemaining; // 魅惑残りターン数（ネットワーク同期用）

    // ヘルパープロパティ：pieceIDから元の所有者を計算
    public int OriginalPlayerID => pieceID / 10000;

    /// <summary>
    /// Pieceインスタンスから完全なsyncPieceDataを生成
    /// </summary>
    public static syncPieceData CreateFromPiece(Piece piece)
    {
        // 駒の種類を取得
        PieceType pieceType = piece switch
        {
            Farmer => PieceType.Farmer,
            MilitaryUnit => PieceType.Military,
            Missionary => PieceType.Missionary,
            Pope => PieceType.Pope,
            _ => PieceType.None
        };

        return new syncPieceData
        {
            piecetype = pieceType,
            religion = piece.Data.religion,
            piecePos = piece.transform.position,
            pieceID = piece.PieceID,
            currentHP = piece.CurrentHP,
            currentHPLevel = piece.HPLevel,
            currentAP = piece.CurrentAP,
            currentAPLevel = piece.APLevel,
            currentPID = piece.CurrentPID,
            swapCooldownLevel = (piece is Pope pope) ? pope.SwapCooldownLevel : 0,
            buffLevel = (piece is Pope pope2) ? pope2.BuffLevel : 0,
            occupyLevel = (piece is Missionary missionary) ? missionary.OccupyLevel : 0,
            convertEnemyLevel = (piece is Missionary missionary2) ? missionary2.ConvertEnemyLevel : 0,
            sacrificeLevel = (piece is Farmer farmer) ? farmer.SacrificeLevel : 0,
            attackPowerLevel = (piece is MilitaryUnit military) ? military.AttackPowerLevel : 0,
            charmedTurnsRemaining = piece.CharmedTurnsRemaining
        };
    }
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
    private Dictionary<int, Piece> allPieces = new Dictionary<int, Piece>(); // 全ての駒（自駒と敵駒を統合）
    private int nextPieceID = 0;
    private int localPlayerID = -1; // このPieceManagerが管理するプレイヤーのID

    //25.11.1 RI add GameObject
    private GameObject pieceObject;

    //25.11.9 RI add syncPieceData List
    private Dictionary<int, syncPieceData> allPiecesSyncData = new Dictionary<int, syncPieceData>();
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
        // プレイヤーIDベースのID範囲を設定（Player 1: 10000~19999, Player 2: 20000~29999, ...）
        nextPieceID = playerID * 10000;
        Debug.Log($"PieceManagerのローカルプレイヤーIDを設定しました: {playerID}, ID範囲: {nextPieceID}～{nextPieceID + 9999}");
    }

    /// <summary>
    /// ローカルプレイヤーIDを取得
    /// </summary>
    public int GetLocalPlayerID()
    {
        return localPlayerID;
    }


    #region 駒の同期
    /// <summary>
    /// HPが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeHPData(int pieceID, int hp)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// HPレベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeHPLevelData(int pieceID, int hplevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 農民のサクリファイスレベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeFarmerLevelData(int pieceID, int sacrificelevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 軍隊の攻撃力レベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeMilitaryAtkLevelData(int pieceID, int atklevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 教皇のスワップクールダウンレベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangePopeSwapCDLevelData(int pieceID, int cdlevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 教皇のバフレベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangePopeBuffLevelData(int pieceID, int bufflevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 宣教師の魅惑レベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeMissionaryConvertLevelData(int pieceID, int convertlevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 宣教師の占領レベルが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangeMissionaryOccupyLevelData(int pieceID, int occupylevel)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// プレイヤーIDが変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangePieceCurrentPID(int pieceID, int currentpid)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    /// <summary>
    /// 位置が変更された駒の完全な同期データを取得
    /// </summary>
    public syncPieceData? ChangePiecePosData(int pieceID, Vector3 position)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }
        return syncPieceData.CreateFromPiece(piece);
    }

    #endregion

    #region 駒の生成

    // 25.11.1 RI add return piece gameObject
    public GameObject GetPieceGameObject()
    {
        if (pieceObject != null)
            return pieceObject;
        return null;
    }

    // 25.11.1 RI add return piece syncPieceData
    public syncPieceData GetPieceSyncPieceData(int unitID)
    {
        return allPiecesSyncData[unitID];
    }

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
        pieceObject = Instantiate(data.piecePrefab, position, Quaternion.identity);
        Piece piece = pieceObject.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"Pieceコンポーネントがありません: {pieceType}");
            Destroy(pieceObject);
            return null;
        }

        // 駒を初期化
        piece.Initialize(data, playerID);

        // IDを割り当てて登録
        int baseId = playerID * 10000;

        int pieceID = baseId + nextPieceID;
        piece.SetPieceID(pieceID);
        allPieces[pieceID] = piece;
        nextPieceID++;

        // 死亡イベントを購読
        piece.OnPieceDeath += (deadPiece) => HandlePieceDeath(deadPiece.PieceID);

        Debug.Log($"駒を生成しました: ID={pieceID}, Type={pieceType}, Religion={religion}, PlayerID={playerID}");
        OnPieceCreated?.Invoke(pieceID);


        // 25.11.12 RI change return data
        syncPieceData pieceData=syncPieceData.CreateFromPiece(piece);
        allPiecesSyncData.Add(pieceID, pieceData);
        
        // 只需要返回基本信息的同步数据
        return pieceData;

    }

    /// <summary>
    /// 基于同步数据创建敌人棋子（用于网络同步）
    /// </summary>
    /// <param name="spd">敌人棋子的同步数据</param>
    /// <returns>成功创建则返回true，失败返回false</returns>
    public bool CreateEnemyPiece(syncPieceData spd)
    {
        // UnitListTableからSOデータを取得

        // 25.11.5 RI add test
        Debug.Log($"敵駒データ: {spd.piecetype}, {spd.religion}");
        var pieceDetail = new UnitListTable.PieceDetail(spd.piecetype, spd.religion);
        PieceDataSO data = unitListTable.GetPieceDataSO(pieceDetail);

        if (data == null)
        {
            Debug.LogError($"敵駒データが見つかりません: {spd.piecetype}, {spd.religion}");
            return false;
        }

        // Prefabから駒を生成
        pieceObject = Instantiate(data.piecePrefab, spd.piecePos, Quaternion.identity);

        //25.11.5 RI add test debug
        Debug.Log("piece name is " + pieceObject.name);
        Piece piece = pieceObject.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"Pieceコンポーネントがありません: {spd.piecetype}");
            Destroy(pieceObject);
            return false;
        }

        //pieceIDから元の所有者を計算
        int originalPlayerID = spd.pieceID / 10000;
        piece.Initialize(data, originalPlayerID);

        // 同步ID（使用来自网络的ID）
        piece.SetPieceID(spd.pieceID);

        // 记录到全駒集合
        allPieces[spd.pieceID] = piece;
        //25.11.5 RI add syncPieceData 
        if(!allPiecesSyncData.ContainsKey(spd.pieceID))
            allPiecesSyncData.Add(spd.pieceID, spd);

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
            if (spd.currentPID != originalPlayerID)
            {
                piece.SetPlayerID(spd.currentPID);
            }

            // 魅惑ターン数を同期
            if (spd.charmedTurnsRemaining > 0)
            {
                piece.SetCharmed(spd.charmedTurnsRemaining, spd.currentPID);
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

            Debug.Log($"敵駒を生成しました: ID={spd.pieceID}, Type={spd.piecetype}, Religion={spd.religion}, OriginalPlayerID={originalPlayerID}");
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
        // 敵駒を取得（全駒辞書から検索）
        if (!allPieces.TryGetValue(spd.pieceID, out Piece piece))
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

            // 魅惑ターン数を同期
            if (spd.charmedTurnsRemaining > 0)
            {
                piece.SetCharmed(spd.charmedTurnsRemaining, spd.currentPID);
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        switch (upgradeType)
        {
            case PieceUpgradeType.HP:
                piece.UpgradeHP();
                return syncPieceData.CreateFromPiece(piece);
            case PieceUpgradeType.AP:
                piece.UpgradeAP();
                return syncPieceData.CreateFromPiece(piece);
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return -1;
        }
        return piece.CurrentHP;
    }

    /// <summary>
    /// 駒の現在APを取得
    /// </summary>
    public int GetPieceAP(int pieceID)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (allPieces.TryGetValue(pieceID, out Piece piece))
        {
            return piece;
        }
        return null;
    }

    public PieceType GetPieceType(int pieceID)
    {
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        return allPieces.ContainsKey(pieceID);
    }

    /// <summary>
    /// 指定プレイヤーのすべての駒IDを取得
    /// </summary>
    public List<int> GetPlayerPieces(int playerID)
    {
        return allPieces
            .Where(kvp => kvp.Value.CurrentPID == playerID && kvp.Value.IsAlive)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    //25.11.9 RI 添加设置玩家id下的魅惑单位归还
    //public void SetPlayerPieces(int playerID,int unitID)
    //{
    //   for()
       
    //}


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
        // 駒を取得
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogWarning($"死亡した駒が見つかりません: ID={pieceID}");
            return;
        }

        // 駒が自分の駒かどうかを判定（OriginalPIDで判定）
        bool isLocalPiece = (piece.OriginalPID == localPlayerID);

        // 死亡した駒のsyncPieceDataを作成してキャッシュ（己方の駒の場合のみ）
        if (isLocalPiece)
        {
            lastDeadPieceData = new syncPieceData
            {
                pieceID = pieceID,
                currentHP = 0 // 死亡を明示
            };
        }

        // 駒を削除（内部処理）
        RemovePieceInternal(pieceID, piece);

        // GameManagerに通知（己方の駒の場合のみ）
        if (isLocalPiece)
        {
            OnPieceDied?.Invoke(pieceID);
            Debug.Log($"己方の駒が死亡しました（送信通知）: ID={pieceID}");
        }
        else
        {
            Debug.Log($"敵駒が死亡しました（通知なし）: ID={pieceID}");
        }
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
        // 駒を取得
        if (!allPieces.TryGetValue(spd.pieceID, out Piece piece))
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

        // 辞書から削除
        if (allPieces.ContainsKey(pieceID))
        {
            allPieces.Remove(pieceID);

            // OriginalPIDで自分の駒か敵駒かを判定してログ出力
            if (localPlayerID != -1 && piece.OriginalPID == localPlayerID)
            {
                Debug.Log($"自分の駒を削除しました: ID={pieceID}, Type={piece.Data?.pieceName}");
            }
            else
            {
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
        // 駒を取得
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        // 删除只同步pieceID信息就足够了（双方棋子创建顺序一致，pieceID可唯一标识棋子）
        syncPieceData syncData = new syncPieceData
        {
            pieceID = pieceID
        };

        // 辞書から削除
        allPieces.Remove(pieceID);

        // GameObjectを破棄
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
        if (!allPieces.TryGetValue(attackerID, out Piece attacker))
        {
            Debug.LogError($"攻撃者が見つかりません: ID={attackerID}");
            return null;
        }

        if (!allPieces.TryGetValue(targetID, out Piece target))
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
            return syncPieceData.CreateFromPiece(target);
        }
        return null;
    }

    /// <summary>
    /// 軍隊が建物を攻撃
    /// </summary>
    /// <param name="attackerID">攻撃者の駒ID</param>
    /// <param name="targetBuilding">ターゲットの建物</param>
    /// <returns>攻撃成功したらtrue</returns>
    public bool AttackBuilding(int attackerID, Buildings.Building targetBuilding)
    {
        if (!allPieces.TryGetValue(attackerID, out Piece attacker))
        {
            Debug.LogError($"攻撃者が見つかりません: ID={attackerID}");
            return false;
        }

        if (targetBuilding == null || !targetBuilding.IsAlive)
        {
            Debug.LogError($"ターゲットの建物が見つからないか、既に破壊されています");
            return false;
        }

        if (attacker is not MilitaryUnit military)
        {
            Debug.LogError($"駒ID={attackerID}は軍隊ではありません");
            return false;
        }

        if (military.AttackBuilding(targetBuilding))
        {
            Debug.Log($"軍隊ID={attackerID}が建物ID={targetBuilding.BuildingID}を攻撃しました（残りHP: {targetBuilding.CurrentHP}）");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 宣教師が敵を魅惑
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <returns>魅惑試行成功したらsyncPieceData、失敗時はnull</returns>
    public syncPieceData? ConvertEnemy(int missionaryID, int targetID)
    {
        if (!allPieces.TryGetValue(missionaryID, out Piece missionaryPiece))
        {
            Debug.LogError($"宣教師が見つかりません: ID={missionaryID}");
            return null;
        }

        if (!allPieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"敵駒が見つかりません: ID={targetID}");
            return null;
        }

        if (missionaryPiece is not Missionary missionary)
        {
            Debug.LogError($"駒ID={missionaryID}は宣教師ではありません");
            return null;
        }

        // 魅惑試行（out パラメータで魅惑ターン数を取得）
        if (missionary.ConversionAttack(target, out int charmDuration))
        {
            // 魅惑成功時のみ処理（即死の場合は charmDuration = 0）
            if (charmDuration > 0)
            {
                // 魅惑状態を設定（currentPIDを変更）
                target.SetCharmed(charmDuration, missionary.CurrentPID);

                Debug.Log($"駒ID={targetID}を{charmDuration}ターン魅惑しました（currentPIDを{missionary.CurrentPID}に変更）");

                // イベント発火
                OnPieceCharmed?.Invoke(targetID, missionary.CurrentPID);

                return ChangePieceCurrentPID(targetID, missionary.CurrentPID);
            }
            else
            {
                // 即死の場合は同期データ不要（OnPieceDeathで処理される）
                Debug.Log($"駒ID={targetID}は魅惑により即死しました");
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// 宣教師が敵を魅惑（bool版）
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetID">ターゲットの駒ID</param>
    /// <param name="charmDuration">魅惑ターン数（0の場合は即死、失敗時は-1）</param>
    /// <returns>魅惑試行が成功したか（即死含む）</returns>
    public bool ConvertEnemy(int missionaryID, int targetID, out int charmDuration)
    {
        charmDuration = -1;

        if (!allPieces.TryGetValue(missionaryID, out Piece missionaryPiece))
        {
            Debug.LogError($"宣教師が見つかりません: ID={missionaryID}");
            return false;
        }

        if (!allPieces.TryGetValue(targetID, out Piece target))
        {
            Debug.LogError($"敵駒が見つかりません: ID={targetID}");
            return false;
        }

        if (missionaryPiece is not Missionary missionary)
        {
            Debug.LogError($"駒ID={missionaryID}は宣教師ではありません");
            return false;
        }

        // 魅惑試行（out パラメータで魅惑ターン数を取得）
        if (missionary.ConversionAttack(target, out charmDuration))
        {
            // 魅惑成功時のみ処理（即死の場合は charmDuration = 0）
            if (charmDuration > 0)
            {
                // 魅惑状態を設定（currentPIDを変更）
                target.SetCharmed(charmDuration, missionary.CurrentPID);

                Debug.Log($"駒ID={targetID}を{charmDuration}ターン魅惑しました（currentPIDを{missionary.CurrentPID}に変更）");

                // イベント発火
                OnPieceCharmed?.Invoke(targetID, missionary.CurrentPID);
            }
            else
            {
                // 即死の場合
                Debug.Log($"駒ID={targetID}は魅惑により即死しました");
            }
            return true;
        }
        return false;
    }


    // 25.11.9 RI 添加被魅惑单位归还后的特殊数据处理
    public void AddConvertedUnit(int playerID,int pieceID)
    {
        if (allPieces.ContainsKey(pieceID))
        {
            allPieces[pieceID].SetPlayerID(playerID);
        }
       
    }


    /// <summary>
    /// 宣教師が領地を占領
    /// </summary>
    /// <param name="missionaryID">宣教師の駒ID</param>
    /// <param name="targetPosition">占領対象の領地座標</param>
    /// <returns>占領試行成功したらtrueを返す</returns>
    public bool OccupyTerritory(int missionaryID, Vector3 targetPosition)
    {
        if (!allPieces.TryGetValue(missionaryID, out Piece missionaryPiece))
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
        if (!allPieces.TryGetValue(farmerID, out Piece farmerPiece))
        {
            Debug.LogError($"農民が見つかりません: ID={farmerID}");
            return null;
        }

        if (!allPieces.TryGetValue(targetID, out Piece target))
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
        if (!allPieces.TryGetValue(popeID, out Piece popePiece))
        {
            Debug.LogError($"教皇が見つかりません: ID={popeID}");
            return null;
        }

        if (!allPieces.TryGetValue(targetID, out Piece target))
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
            var piece1Data = syncPieceData.CreateFromPiece(popePiece);
            var piece2Data = syncPieceData.CreateFromPiece(target);

            swapPieceData swapData = new swapPieceData
            {
                piece1 = piece1Data,
                piece2 = piece2Data
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
        {
            Debug.LogError($"駒が見つかりません: ID={pieceID}");
            return null;
        }

        Piece attacker = null;
        if (attackerID >= 0 && allPieces.TryGetValue(attackerID, out attacker))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(pieceID, out Piece piece))
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
        if (!allPieces.TryGetValue(farmerID, out Piece piece))
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
    /// 指定プレイヤーのターン開始処理（AP回復、魅惑カウンター処理など）
    /// </summary>
    public void ProcessTurnStart(int playerID)
    {
        var playerPieces = GetPlayerPieces(playerID);
        int charmedPiecesCount = 0; // 魅惑解除された駒の数

        foreach (int pieceID in playerPieces)
        {
            if (allPieces.TryGetValue(pieceID, out Piece piece))
            {
                //25.11.9  RI 添加测试debug
                Debug.Log($"駒ID={pieceID} ");

                // AP回復
                piece.RecoverAP(piece.Data.aPRecoveryRate);

                // 魅惑カウンター処理（ProcessCharmedTurn内でcurrentPIDが元に戻される）
                if (piece.ProcessCharmedTurn())
                {
                    // 魅惑解除された（currentPIDが元のOriginalPIDに戻された）
                    charmedPiecesCount++;
                    Debug.Log($"駒ID={pieceID}が魅惑解除により元の所有者（PID={piece.CurrentPID}）に戻りました");

                    // GameManagerに通知（必要なら）
                    // OnCharmExpired?.Invoke(pieceID, piece.CurrentPID);
                }
            }
        }

        Debug.Log($"プレイヤー{playerID}のターン開始処理を実行しました（駒数: {playerPieces.Count}、魅惑解除: {charmedPiecesCount}）");
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
