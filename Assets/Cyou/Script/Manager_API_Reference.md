# Manager システム API リファレンス

このドキュメントでは、PieceManagerとBuildingManagerを使用したID ベースのユニット・建物管理システムの使用方法を説明します。

---

## 目次

1. [概要](#1-概要)
2. [PieceManager - 駒管理](#2-piecemanager---駒管理)
   - [初期化](#初期化)
   - [駒の生成](#駒の生成)
   - [ネットワーク同期](#ネットワーク同期)
   - [アップグレード管理](#アップグレード管理)
   - [情報取得](#情報取得)
   - [駒の行動](#駒の行動)
   - [AP管理](#ap管理)
   - [駒の削除](#駒の削除)
   - [ターン処理](#ターン処理)
3. [BuildingManager - 建物管理](#3-buildingmanager---建物管理)
4. [列挙型（Enums）](#列挙型enums)

---

## 1. 概要

### 設計思想

PieceManagerとBuildingManagerは、具体的なユニットクラス（Farmer, Military, Missionary, Pope）や建物クラスを**IDベースで管理**するラッパー層です。

**メリット:**
- ✅ GameManagerは具体的な型（Farmer, Military等）を意識せずに操作可能
- ✅ 整数IDのみで全ての駒・建物を管理
- ✅ 型安全性を保ちつつ、インターフェースをシンプルに
- ✅ **ネットワーク同期対応**：己方の駒と敵駒を分離管理

**アーキテクチャ:**
```
GameManager → PieceManager (ID管理層) → Piece/Farmer/Military/etc (実装層)
GameManager → BuildingManager (ID管理層) → Building (実装層)
```

### 実装ファイル
- `Assets/Cyou/Script/Manager/PieceManager.cs`
- `Assets/Cyou/Script/Manager/BuildingManager.cs`

---

## 2. PieceManager - 駒管理

### 初期化

#### `SetLocalPlayerID()`
PieceManagerが管理するプレイヤーIDを設定します（ネットワーク同期に必要）。

**シグネチャ:**
```csharp
public void SetLocalPlayerID(int playerID)
```

**パラメータ:**
- `playerID`: このPieceManagerが管理するローカルプレイヤーのID

**使用例:**
```csharp
// ゲーム開始時に設定
PieceManager.Instance.SetLocalPlayerID(1); // プレイヤー1として設定
```

**実装箇所:** `PieceManager.cs:108-112`

---

#### `GetLocalPlayerID()`
設定されたローカルプレイヤーIDを取得します。

**シグネチャ:**
```csharp
public int GetLocalPlayerID()
```

**戻り値:**
- ローカルプレイヤーID（未設定の場合は-1）

**実装箇所:** `PieceManager.cs:117-120`

---

### 駒の生成

#### `CreatePiece()`
自分の駒を生成し、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
```

**パラメータ:**
- `pieceType`: 駒の種類（PieceType.Farmer, Military, Missionary, Pope）
- `religion`: 宗教（Religion.Maya, RedMoon, MadScientist, Silk）
- `playerID`: プレイヤーID
- `position`: 生成位置

**戻り値:**
- 成功: `syncPieceData`（ネットワークに送信するための同期データ）
- 失敗: null

**使用例:**
```csharp
// 農民を生成
syncPieceData? farmerData = PieceManager.Instance.CreatePiece(
    PieceType.Farmer,
    Religion.Maya,
    playerID: 1,
    new Vector3(10, 0, 10)
);

if (farmerData.HasValue)
{
    Debug.Log($"農民を生成しました: ID={farmerData.Value.pieceID}");
    // ネットワーク経由で相手に送信
    SendToNetwork(farmerData.Value);
}
```

**実装箇所:** `PieceManager.cs:217-266`

---

#### `CreateEnemyPiece()`
ネットワークから受信した同期データを使って敵駒を生成します。

**シグネチャ:**
```csharp
public bool CreateEnemyPiece(syncPieceData spd)
```

**パラメータ:**
- `spd`: 敵駒の同期データ（ネットワークから受信）

**戻り値:**
- `true`: 生成成功
- `false`: 失敗

**使用例:**
```csharp
// ネットワークから敵駒データを受信
syncPieceData enemyData = ReceiveFromNetwork();

// 敵駒を生成
if (PieceManager.Instance.CreateEnemyPiece(enemyData))
{
    Debug.Log($"敵駒を生成しました: ID={enemyData.pieceID}");
}
```

**処理の流れ:**
1. UnitListTableから駒データSOを取得
2. Prefabから駒を生成
3. 駒を初期化
4. enemyPieces辞書に登録
5. syncPieceDataから状態を設定（HP、レベル、職業別スキルレベル等）

**実装箇所:** `PieceManager.cs:273-366`

---

### ネットワーク同期

#### `SyncEnemyPieceState()`
既に存在する敵駒の状態を同期します（アップグレードやHP変更時など）。

**シグネチャ:**
```csharp
public bool SyncEnemyPieceState(syncPieceData spd)
```

**パラメータ:**
- `spd`: 同期データ

**戻り値:**
- `true`: 同期成功
- `false`: 失敗

**使用例:**
```csharp
// 敵がアップグレードした通知を受信
syncPieceData updateData = ReceiveFromNetwork();

if (PieceManager.Instance.SyncEnemyPieceState(updateData))
{
    Debug.Log($"敵駒の状態を同期しました: ID={updateData.pieceID}");
}
```

**実装箇所:** `PieceManager.cs:321-385`

---

#### 各種同期データ生成関数

以下の関数は、各種操作後に同期データを生成してネットワークに送信するために使用します。

##### `ChangeHPData()`
```csharp
public syncPieceData ChangeHPData(int pieceID, int hp)
```

##### `ChangeHPLevelData()`
```csharp
public syncPieceData ChangeHPLevelData(int pieceID, int hplevel)
```

##### `ChangeFarmerLevelData()`
```csharp
public syncPieceData ChangeFarmerLevelData(int pieceID, int sacrificelevel)
```

##### `ChangeMilitaryAtkLevelData()`
```csharp
public syncPieceData ChangeMilitaryAtkLevelData(int pieceID, int atklevel)
```

##### `ChangePopeSwapCDLevelData()`
```csharp
public syncPieceData ChangePopeSwapCDLevelData(int pieceID, int cdlevel)
```

##### `ChangePopeBuffLevelData()`
```csharp
public syncPieceData ChangePopeBuffLevelData(int pieceID, int bufflevel)
```

##### `ChangeMissionaryConvertLevelData()`
```csharp
public syncPieceData ChangeMissionaryConvertLevelData(int pieceID, int convertlevel)
```

##### `ChangeMissionaryOccupyLevelData()`
```csharp
public syncPieceData ChangeMissionaryOccupyLevelData(int pieceID, int occupylevel)
```

##### `ChangePieceCurrentPID()`
```csharp
public syncPieceData ChangePieceCurrentPID(int pieceID, int currentpid)
```

##### `ChangePiecePosData()`
```csharp
public syncPieceData ChangePiecePosData(int pieceID, Vector3 position)
```

**実装箇所:** `PieceManager.cs:110-203`

---

### アップグレード管理

#### `UpgradePiece()`
駒の共通項目（HP/AP）をアップグレードし、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? UpgradePiece(int pieceID, PieceUpgradeType upgradeType)
```

**パラメータ:**
- `pieceID`: 駒ID
- `upgradeType`: アップグレード項目（PieceUpgradeType.HP または AP）

**戻り値:**
- 成功: `syncPieceData`（ネットワークに送信するための同期データ）
- 失敗: null

**使用例:**
```csharp
// HPをアップグレード
syncPieceData? upgradeData = PieceManager.Instance.UpgradePiece(farmerID, PieceUpgradeType.HP);

if (upgradeData.HasValue)
{
    Debug.Log("HPアップグレード成功");
    // ネットワーク経由で相手に送信
    SendToNetwork(upgradeData.Value);
}
```

**実装箇所:** `PieceManager.cs:414-434`

---

#### `UpgradePieceSpecial()`
駒の職業別専用項目をアップグレードし、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? UpgradePieceSpecial(int pieceID, SpecialUpgradeType specialUpgradeType)
```

**パラメータ:**
- `pieceID`: 駒ID
- `specialUpgradeType`: 職業別アップグレード項目
  - `SpecialUpgradeType.FarmerSacrifice` - 農民の獻祭回復量
  - `SpecialUpgradeType.MilitaryAttackPower` - 軍隊の攻撃力
  - `SpecialUpgradeType.MissionaryOccupy` - 宣教師の占領成功率
  - `SpecialUpgradeType.MissionaryConvertEnemy` - 宣教師の魅惑成功率
  - `SpecialUpgradeType.PopeSwapCooldown` - 教皇の位置交換クールダウン
  - `SpecialUpgradeType.PopeBuff` - 教皇のバフ効果

**戻り値:**
- 成功: `syncPieceData`
- 失敗: null

**使用例:**
```csharp
// 農民の獻祭回復量をアップグレード
syncPieceData? upgradeData = PieceManager.Instance.UpgradePieceSpecial(
    farmerID,
    SpecialUpgradeType.FarmerSacrifice
);

if (upgradeData.HasValue)
{
    Debug.Log("農民の獻祭回復量アップグレード成功");
    SendToNetwork(upgradeData.Value);
}
```

**実装箇所:** `PieceManager.cs:442-500`

---

#### `GetUpgradeCost()`
アップグレードコストを取得します。

**シグネチャ:**
```csharp
public int GetUpgradeCost(int pieceID, PieceUpgradeType upgradeType)
```

**パラメータ:**
- `pieceID`: 駒ID
- `upgradeType`: アップグレード項目

**戻り値:**
- 成功: コスト（正の整数）
- 失敗: -1（駒が存在しない、またはアップグレード不可）

**実装箇所:** `PieceManager.cs:508-517`

---

#### `CanUpgrade()`
アップグレード可能かチェックします。

**シグネチャ:**
```csharp
public bool CanUpgrade(int pieceID, PieceUpgradeType upgradeType)
```

**実装箇所:** `PieceManager.cs:525-533`

---

### 情報取得

#### `GetPiece()`
駒のインスタンスを取得します（己方・敵駒どちらでも取得可能）。

**シグネチャ:**
```csharp
public Piece GetPiece(int pieceID)
```

**パラメータ:**
- `pieceID`: 駒ID

**戻り値:**
- 成功: Pieceインスタンス
- 失敗: null

**実装箇所:** `PieceManager.cs:661-672`

---

#### `GetPieceHP()`
駒の現在HPを取得します。

**シグネチャ:**
```csharp
public float GetPieceHP(int pieceID)
```

**実装箇所:** `PieceManager.cs:542-550`

---

#### `GetPieceAP()`
駒の現在APを取得します。

**シグネチャ:**
```csharp
public float GetPieceAP(int pieceID)
```

**実装箇所:** `PieceManager.cs:555-563`

---

#### `GetPiecePlayerID()`
駒の所属プレイヤーIDを取得します。

**シグネチャ:**
```csharp
public int GetPiecePlayerID(int pieceID)
```

**実装箇所:** `PieceManager.cs:568-576`

---

#### `GetPieceType()`
駒の種類を取得します。

**シグネチャ:**
```csharp
public PieceType GetPieceType(int pieceID)
```

**戻り値:**
- PieceType.Farmer, Military, Missionary, Pope, またはNone

**実装箇所:** `PieceManager.cs:674-687`

---

#### `DoesPieceExist()`
駒が存在するかチェックします。

**シグネチャ:**
```csharp
public bool DoesPieceExist(int pieceID)
```

**実装箇所:** `PieceManager.cs:692-695`

---

#### `GetPlayerPieces()`
指定プレイヤーのすべての駒IDを取得します。

**シグネチャ:**
```csharp
public List<int> GetPlayerPieces(int playerID)
```

**戻り値:**
- 駒IDのリスト

**実装箇所:** `PieceManager.cs:700-706`

---

#### `GetPlayerPiecesByType()`
指定プレイヤーの指定種類の駒IDを取得します。

**シグネチャ:**
```csharp
public List<int> GetPlayerPiecesByType(int playerID, PieceType pieceType)
```

**実装箇所:** `PieceManager.cs:711-716`

---

### 駒の行動

#### `AttackEnemy()`
軍隊が敵を攻撃し、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? AttackEnemy(int attackerID, int targetID)
```

**パラメータ:**
- `attackerID`: 攻撃者の駒ID（軍隊である必要がある）
- `targetID`: ターゲットの駒ID

**戻り値:**
- 成功: `syncPieceData`（ターゲットの現在HPを含む）
- 失敗: null

**使用例:**
```csharp
syncPieceData? attackData = PieceManager.Instance.AttackEnemy(militaryID, enemyID);

if (attackData.HasValue)
{
    Debug.Log("攻撃成功");
    SendToNetwork(attackData.Value);
}
```

**実装箇所:** `PieceManager.cs:803-820`

---

#### `ConvertEnemy()`
宣教師が敵を魅惑し、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? ConvertEnemy(int missionaryID, int targetID)
```

**パラメータ:**
- `missionaryID`: 宣教師の駒ID
- `targetID`: ターゲットの駒ID

**戻り値:**
- 成功: `syncPieceData`（ターゲットの現在PIDを含む）
- 失敗: null

**実装箇所:** `PieceManager.cs:828-849`

---

#### `OccupyTerritory()`
宣教師が領地を占領します。

**シグネチャ:**
```csharp
public bool OccupyTerritory(int missionaryID, Vector3 targetPosition)
```

**パラメータ:**
- `missionaryID`: 宣教師の駒ID
- `targetPosition`: 占領対象の領地座標

**戻り値:**
- `true`: 占領試行成功
- `false`: 失敗

**実装箇所:** `PieceManager.cs:857-872`

---

#### `SacrificeToPiece()`
農民が他の駒を回復（獻祭）し、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? SacrificeToPiece(int farmerID, int targetID)
```

**パラメータ:**
- `farmerID`: 農民の駒ID
- `targetID`: 回復対象の駒ID

**戻り値:**
- 成功: `syncPieceData`（ターゲットの現在HPを含む）
- 失敗: null

**実装箇所:** `PieceManager.cs:880-901`

---

#### `SwapPositions()`
教皇が味方駒と位置を交換し、同期データを返します。

**シグネチャ:**
```csharp
public swapPieceData? SwapPositions(int popeID, int targetID)
```

**パラメータ:**
- `popeID`: 教皇の駒ID
- `targetID`: 交換対象の駒ID

**戻り値:**
- 成功: `swapPieceData`（両方の駒の位置データを含む）
- 失敗: null

**実装箇所:** `PieceManager.cs:909-932`

---

#### `DamagePiece()`
駒にダメージを与え、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? DamagePiece(int pieceID, int damage, int attackerID = -1)
```

**パラメータ:**
- `pieceID`: 駒ID
- `damage`: ダメージ量
- `attackerID`: 攻撃者ID（オプション）

**戻り値:**
- `syncPieceData`（ダメージ後のHPを含む）

**実装箇所:** `PieceManager.cs:940-958`

---

#### `HealPiece()`
駒を回復し、同期データを返します。

**シグネチャ:**
```csharp
public syncPieceData? HealPiece(int pieceID, int amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 回復量

**戻り値:**
- `syncPieceData`（回復後のHPを含む）

**実装箇所:** `PieceManager.cs:966-978`

---

### AP管理

#### `ConsumePieceAP()`
駒のAPを消費します。

**シグネチャ:**
```csharp
public bool ConsumePieceAP(int pieceID, int amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 消費量

**戻り値:**
- `true`: 消費成功
- `false`: 失敗（AP不足等）

**実装箇所:** `PieceManager.cs:990-999`

---

#### `RecoverPieceAP()`
駒のAPを回復します。

**シグネチャ:**
```csharp
public void RecoverPieceAP(int pieceID, int amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 回復量

**実装箇所:** `PieceManager.cs:1006-1015`

---

### 駒の削除

#### `HandleEnemyPieceDeath()`
ネットワークから駒の死亡通知を受信した時に呼び出します（受信側）。

**シグネチャ:**
```csharp
public bool HandleEnemyPieceDeath(syncPieceData spd)
```

**パラメータ:**
- `spd`: 死亡した駒の同期データ

**戻り値:**
- `true`: 削除成功
- `false`: 失敗

**使用例:**
```csharp
// ネットワークから駒の死亡通知を受信
syncPieceData deathData = ReceiveFromNetwork();

if (PieceManager.Instance.HandleEnemyPieceDeath(deathData))
{
    Debug.Log($"敵駒を削除しました: ID={deathData.pieceID}");
}
```

**注意:** この関数は送信処理を行いません（無限ループ防止）。

**実装箇所:** `PieceManager.cs:775-793`

---

#### `GetLastDeadPieceData()`
最後に死亡した駒の同期データを取得します（送信側）。

**シグネチャ:**
```csharp
public syncPieceData? GetLastDeadPieceData()
```

**戻り値:**
- 成功: `syncPieceData`（currentHP = 0）
- 失敗: null

**使用例:**
```csharp
void Start()
{
    // OnPieceDeathイベントを購読
    PieceManager.Instance.OnPieceDied += OnPieceDied;
}

void OnPieceDied(int pieceID)
{
    // 死亡した駒のデータを取得
    syncPieceData? deathData = PieceManager.Instance.GetLastDeadPieceData();

    if (deathData.HasValue)
    {
        Debug.Log($"駒が死亡しました: ID={pieceID}");
        // ネットワーク経由で相手に送信
        SendToNetwork(deathData.Value);
    }
}
```

**注意:** 取得後は自動的にクリアされます（1回だけ取得可能）。

**実装箇所:** `PieceManager.cs:784-789`

---

#### `RemovePiece()`
駒を強制削除します（デバッグ・特殊用途）。

**シグネチャ:**
```csharp
public syncPieceData? RemovePiece(int pieceID)
```

**戻り値:**
- 成功: `syncPieceData`
- 失敗: null

**実装箇所:** `PieceManager.cs:836-905`

---

### ターン処理

#### `ProcessTurnStart()`
指定プレイヤーのターン開始処理を実行します。

**シグネチャ:**
```csharp
public void ProcessTurnStart(int playerID)
```

**パラメータ:**
- `playerID`: プレイヤーID

**使用例:**
```csharp
// ターン開始時に呼び出し
PieceManager.Instance.ProcessTurnStart(currentPlayerID);
```

**実装箇所:** `PieceManager.cs:1052-1065`

---

## 3. BuildingManager - 建物管理

### 建物の生成

#### `CreateBuilding()`
建物を生成してIDを返します。

**シグネチャ:**
```csharp
public int CreateBuilding(BuildingDataSO buildingData, int playerID, Vector3 position)
```

**パラメータ:**
- `buildingData`: 建物データSO
- `playerID`: プレイヤーID
- `position`: 生成位置

**戻り値:**
- 成功: 生成された建物のID（正の整数）
- 失敗: -1

**実装箇所:** `BuildingManager.cs:38-76`

---

#### `CreateBuildingByName()`
建物名から建物を生成します（便利メソッド）。

**シグネチャ:**
```csharp
public int CreateBuildingByName(string buildingName, int playerID, Vector3 position)
```

**実装箇所:** `BuildingManager.cs:85-96`

---

### 建築処理

#### `AddFarmerToConstruction()`
建物の建築を進めます（農民を投入）。

**シグネチャ:**
```csharp
public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
```

**実装箇所:** `BuildingManager.cs:109-161`

---

#### `CancelConstruction()`
建築をキャンセルします。

**シグネチャ:**
```csharp
public bool CancelConstruction(int buildingID)
```

**実装箇所:** `BuildingManager.cs:168-177`

---

### 農民配置

#### `EnterBuilding()`
農民を建物に配置します。

**シグネチャ:**
```csharp
public bool EnterBuilding(int buildingID, int farmerID, PieceManager pieceManager)
```

**実装箇所:** `BuildingManager.cs:190-238`

---

### 建物のアップグレード

#### `UpgradeBuilding()`
建物の項目をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradeBuilding(int buildingID, BuildingUpgradeType upgradeType)
```

**実装箇所:** `BuildingManager.cs:250-272`

---

### 建物の情報取得

各種情報取得関数については、BuildingManagerのインターフェースを参照してください。

---

## 列挙型（Enums）

### syncPieceData構造体
```csharp
public struct syncPieceData
{
    public PieceType piecetype;
    public Religion religion;
    public Vector3 piecePos;
    public int playerID;           // 元々のプレイヤーID
    public int pieceID;
    public int currentHP;
    public int currentHPLevel;
    public int currentPID;         // 現在のプレイヤーID（魅惑された場合など）
    public int swapCooldownLevel;  // 教皇専用
    public int buffLevel;          // 教皇専用
    public int occupyLevel;        // 宣教師専用
    public int convertEnemyLevel;  // 宣教師専用
    public int sacrificeLevel;     // 農民専用
    public int attackPowerLevel;   // 軍隊専用
}
```

### swapPieceData構造体
```csharp
public struct swapPieceData
{
    public syncPieceData piece1;
    public syncPieceData piece2;
}
```

### PieceType
```csharp
public enum PieceType
{
    None,
    Farmer,      // 農民
    Military,    // 軍隊
    Missionary,  // 宣教師
    Pope         // 教皇
}
```

### Religion
```csharp
public enum Religion
{
    None,
    SilkReligion,           // 絲織教
    RedMoonReligion,        // 紅月教
    MayaReligion,           // 瑪雅外星人文明教
    MadScientistReligion    // 瘋狂科學家教
}
```

### PieceUpgradeType
```csharp
public enum PieceUpgradeType
{
    HP,
    AP
}
```

### SpecialUpgradeType
```csharp
public enum SpecialUpgradeType
{
    FarmerSacrifice,           // 農民: 獻祭回復量
    MilitaryAttackPower,       // 軍隊: 攻撃力
    MissionaryOccupy,          // 宣教師: 占領成功率
    MissionaryConvertEnemy,    // 宣教師: 魅惑成功率
    PopeSwapCooldown,          // 教皇: 位置交換クールダウン
    PopeBuff                   // 教皇: バフ効果
}
```

---

## ネットワーク同期の使用例

### 完全なネットワーク同期フロー

```csharp
public class NetworkGameManager : MonoBehaviour
{
    void Start()
    {
        // 初期化: ローカルプレイヤーIDを設定
        PieceManager.Instance.SetLocalPlayerID(1);

        // イベント購読
        PieceManager.Instance.OnPieceDied += OnPieceDied;
    }

    // === 送信側（自分の行動） ===

    void CreateMyPiece()
    {
        // 駒を生成
        syncPieceData? pieceData = PieceManager.Instance.CreatePiece(
            PieceType.Farmer,
            Religion.Maya,
            playerID: 1,
            new Vector3(10, 0, 10)
        );

        if (pieceData.HasValue)
        {
            // ネットワーク経由で相手に送信
            NetworkSend("CreatePiece", pieceData.Value);
        }
    }

    void AttackEnemyPiece(int myMilitaryID, int enemyPieceID)
    {
        // 攻撃実行
        syncPieceData? attackData = PieceManager.Instance.AttackEnemy(myMilitaryID, enemyPieceID);

        if (attackData.HasValue)
        {
            // 相手に送信
            NetworkSend("Attack", attackData.Value);
        }
    }

    void OnPieceDied(int pieceID)
    {
        // 駒が死亡したら、死亡データを取得
        syncPieceData? deathData = PieceManager.Instance.GetLastDeadPieceData();

        if (deathData.HasValue)
        {
            // 相手に送信
            NetworkSend("PieceDeath", deathData.Value);
        }
    }

    // === 受信側（相手の行動） ===

    void OnNetworkMessage(string messageType, syncPieceData data)
    {
        switch (messageType)
        {
            case "CreatePiece":
                // 敵駒を生成
                PieceManager.Instance.CreateEnemyPiece(data);
                break;

            case "Attack":
                // 攻撃を受けた駒のHPを同期
                PieceManager.Instance.SyncEnemyPieceState(data);
                break;

            case "PieceDeath":
                // 敵駒を削除
                PieceManager.Instance.HandleEnemyPieceDeath(data);
                break;

            case "Upgrade":
                // 敵駒のアップグレードを同期
                PieceManager.Instance.SyncEnemyPieceState(data);
                break;
        }
    }
}
```

---

## まとめ

PieceManagerは以下の機能を提供します：

- ✅ **IDベース管理**: 具体的な型を意識せずに操作可能
- ✅ **ネットワーク同期対応**: 己方と敵駒を分離管理
- ✅ **同期データ自動生成**: 各種操作後に`syncPieceData`を返す
- ✅ **送信/受信分離**: 無限ループを防ぐ明確な設計
- ✅ **イベント駆動**: 駒の死亡などを自動通知

すべての操作はIDベースで統一されており、ネットワーク同期も簡潔に実装できます。
