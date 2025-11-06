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

**重要な機能:**
- **駒ID範囲の自動割り当て**: プレイヤーIDに基づいて駒IDの範囲が自動的に設定されます
  - Player 0: 0～9999
  - Player 1: 10000～19999
  - Player 2: 20000～29999
  - Player 3: 30000～39999
- これにより、魅惑時などに駒IDの重複が発生しません
- 駒IDを見ればどのプレイヤーの駒か一目瞭然

**実装箇所:** `PieceManager.cs:108-114`

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
宣教師が敵を魅惑し、一時的に支配します。魅惑は一定ターン数で自動的に解除されます。

**シグネチャ:**
```csharp
public syncPieceData? ConvertEnemy(int missionaryID, int targetID)
```

**パラメータ:**
- `missionaryID`: 宣教師の駒ID
- `targetID`: ターゲットの敵駒ID（enemyPieces辞書から取得）

**戻り値:**
- 魅惑成功: `syncPieceData`（ターゲットの現在PIDと魅惑ターン数を含む）
- 即死の場合: null（OnPieceDeathイベントで処理）
- 失敗: null

**使用例:**
```csharp
syncPieceData? charmData = PieceManager.Instance.ConvertEnemy(missionaryID, enemyPieceID);

if (charmData.HasValue)
{
    Debug.Log($"魅惑成功！{charmData.Value.charmedTurnsRemaining}ターン支配");
    SendToNetwork(charmData.Value);
}
```

**重要な動作:**
1. 魅惑試行を実行（成功率は宣教師の`convertEnemyLevel`に依存）
2. 魅惑成功時：
   - ターゲットに魅惑状態を設定（`Piece.SetCharmed()`）
   - 魅惑ターン数は`MissionaryDataSO.conversionTurnDuration[convertEnemyLevel]`から取得
   - ターゲットを`enemyPieces`から`pieces`に移動（自分の駒として扱える）
   - `OnPieceCharmed`イベントを発火
3. 即死の場合（レベル1以上の宣教師・軍隊）：
   - ターゲットが即死し、`OnPieceDeath`イベントで処理される
4. 魅惑解除：
   - 毎ターン`ProcessTurnStart()`で自動的にカウンター減算
   - カウンターが0になると元の所有者に復帰（`pieces` → `enemyPieces`）

**実装箇所:** `PieceManager.cs:959-1006`

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
指定プレイヤーのターン開始処理を実行します（AP回復、魅惑カウンター処理）。

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

**処理内容:**
1. **AP回復**: 指定プレイヤーのすべての駒のAPを回復
2. **魅惑カウンター処理**:
   - 各駒の`ProcessCharmedTurn()`を呼び出し
   - 魅惑カウンターを-1
   - カウンターが0になった駒は自動的に元の所有者に復帰
   - 辞書間移動：`pieces` → `enemyPieces`

**魅惑解除の自動処理:**
```csharp
// 魅惑が解除されると自動的に以下が実行される：
// 1. 駒のcurrentPIDがOriginalPIDに戻る
// 2. pieces辞書から削除
// 3. enemyPieces辞書に追加
// 4. OnUncharmedイベントが発火
```

**実装箇所:** `PieceManager.cs:1218-1254`

---

## 3. BuildingManager - 建物管理

### 初期化

#### `SetLocalPlayerID()`
ローカルプレイヤーIDを設定します（己方/敵方の建物を区別するため）。

**シグネチャ:**
```csharp
public void SetLocalPlayerID(int playerID)
```

**パラメータ:**
- `playerID`: ローカルプレイヤーのID

**使用例:**
```csharp
buildingManager.SetLocalPlayerID(1);
```

**注意:**
- 建物生成前に呼び出す必要があります
- ネットワーク同期で己方建物（buildings）と敵方建物（enemyBuildings）を区別するために使用

**実装箇所:** `BuildingManager.cs:41-45`

---

#### `GetLocalPlayerID()`
ローカルプレイヤーIDを取得します。

**シグネチャ:**
```csharp
public int GetLocalPlayerID()
```

**戻り値:**
- ローカルプレイヤーID（未設定時は-1）

**実装箇所:** `BuildingManager.cs:50-53`

---

### 建物の生成

#### `CreateBuilding()`
己方の建物を生成してIDを返します。

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

**注意:**
- このメソッドで生成された建物は `buildings` 辞書（己方の建物）に追加されます
- 敵方の建物を生成する場合は `CreateEnemyBuilding()` を使用してください

**実装箇所:** `BuildingManager.cs:62-102`

---

#### `CreateBuildingByName()`
建物名から建物を生成します（便利メソッド）。

**シグネチャ:**
```csharp
public int CreateBuildingByName(string buildingName, int playerID, Vector3 position)
```

**実装箇所:** `BuildingManager.cs:113-124`

---

#### `CreateEnemyBuilding()`
同期データから敵方の建物を生成します。

**シグネチャ:**
```csharp
public bool CreateEnemyBuilding(syncBuildingData sbd)
```

**パラメータ:**
- `sbd`: 建物同期データ（すべての状態情報を含む）

**戻り値:**
- `true`: 生成成功
- `false`: 失敗

**使用例:**
```csharp
// 敵方の建物データを受信した時
syncBuildingData enemyBuildingData = new syncBuildingData
{
    buildingID = 201,
    buildingName = "祭壇",
    playerID = 2,
    position = new Vector3(50, 0, 50),
    currentHP = 100,
    state = BuildingState.Active,
    hpLevel = 1,
    slotsLevel = 1
};

if (buildingManager.CreateEnemyBuilding(enemyBuildingData))
{
    Debug.Log("敵方の建物生成成功");
}
```

**注意:**
- このメソッドは建物のすべての状態（HP、レベル、建築進捗等）を自動的に設定します
- 生成された建物は `enemyBuildings` 辞書に追加されます
- ネットワークゲームで相手の建物を受信して作成する際に使用

**実装箇所:** `BuildingManager.cs:131-201`

---

### ネットワーク同期

#### `SyncEnemyBuildingState()`
敵方の建物の状態を同期します。

**シグネチャ:**
```csharp
public bool SyncEnemyBuildingState(syncBuildingData sbd)
```

**パラメータ:**
- `sbd`: 建物同期データ

**戻り値:**
- `true`: 同期成功
- `false`: 失敗（建物が存在しない）

**使用例:**
```csharp
// 建物の状態更新を受信した時
syncBuildingData updateData = new syncBuildingData
{
    buildingID = 201,
    currentHP = 80,  // HPが変化
    hpLevel = 2,     // HPレベルが上昇
    state = BuildingState.Active
};

if (buildingManager.SyncEnemyBuildingState(updateData))
{
    Debug.Log("敵方の建物状態同期成功");
}
```

**注意:**
- 既存の建物の状態のみ更新します
- 建物が存在しない場合は、先に `CreateEnemyBuilding()` を呼び出す必要があります
- localPlayerIDに基づいて己方/敵方を自動判定します

**実装箇所:** `BuildingManager.cs:354-413`

---

#### `CreateCompleteSyncData()`
完全な状態を含む同期データを作成します。

**シグネチャ:**
```csharp
public syncBuildingData? CreateCompleteSyncData(int buildingID)
```

**パラメータ:**
- `buildingID`: 建物ID

**戻り値:**
- 完全な状態を含む `syncBuildingData`
- 失敗時は null

**使用例:**
```csharp
// 己方の建物の完全な状態を相手に送信
syncBuildingData? myBuildingData = buildingManager.CreateCompleteSyncData(buildingID);
if (myBuildingData.HasValue)
{
    // ネットワーク経由で送信
    NetworkManager.Send(myBuildingData.Value);
}
```

**含まれる情報:**
- 基本情報：buildingID, buildingName, playerID
- 位置と状態：position, currentHP, state
- 建築情報：remainingBuildCost
- アップグレードレベル：hpLevel, attackRangeLevel, slotsLevel, buildCostLevel

**実装箇所:** `BuildingManager.cs:420-458`

---

#### CreateCompleteSyncData()の使用状況

`CreateCompleteSyncData()` は、建物の完全な状態情報をネットワーク送信用に取得するための関数です。以下の状況で使用します：

##### 1. 建物完成時

己方で建物が完成した時、相手に通知するために使います：

```csharp
void OnBuildingCompleted(int buildingID)
{
    Debug.Log($"己方の建物が完成: ID={buildingID}");

    // 完成した建物の完全な情報を取得して送信
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        networkManager.SendBuildingComplete(data.Value);
    }
}
```

##### 2. 状態変更時（アップグレード、ダメージなど）

建物の状態が変わった時、変更後の完全な状態を相手に送信します：

```csharp
// 建物をアップグレードした後
void UpgradeMyBuilding(int buildingID)
{
    if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP))
    {
        Debug.Log("建物HPアップグレード成功");

        // アップグレード後の完全な状態を取得して送信
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendBuildingUpdate(data.Value);
        }
    }
}

// 建物がダメージを受けた後
void OnBuildingDamaged(int buildingID)
{
    // ダメージ後の完全な状態を取得して送信
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        networkManager.SendBuildingUpdate(data.Value);
    }
}
```

##### 3. ゲーム途中参加時の状態同期

プレイヤーがゲームに途中参加した時、既存のすべての建物の状態を送る必要があります：

```csharp
// 新しいプレイヤーが参加した時
void OnNewPlayerJoined(int newPlayerID)
{
    Debug.Log("新しいプレイヤーが参加。既存の建物状態を送信中...");

    // すべての己方建物の状態を送信
    List<int> myBuildings = buildingManager.GetPlayerBuildings(localPlayerID);
    foreach (int buildingID in myBuildings)
    {
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendToPlayer(newPlayerID, data.Value);
        }
    }
}
```

##### 4. 再接続時の状態回復

ネットワーク切断後の再接続時、現在の状態をすべて再送信します：

```csharp
// プレイヤーが再接続した時
void OnPlayerReconnected(int playerID)
{
    Debug.Log($"プレイヤー{playerID}が再接続。状態を再同期中...");
    SyncAllBuildingsToPlayer(playerID);
}

void SyncAllBuildingsToPlayer(int targetPlayerID)
{
    // すべての建物状態を送信
    List<int> allBuildings = buildingManager.GetAllBuildingIDs();
    foreach (int buildingID in allBuildings)
    {
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendToPlayer(targetPlayerID, data.Value);
        }
    }
}
```

##### 5. デバッグ・状態確認時

開発中、現在の状態を確認したい時にも使えます：

```csharp
// デバッグ用：建物の完全な状態をログ出力
void DebugBuildingState(int buildingID)
{
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        Debug.Log($"=== 建物状態: ID={data.Value.buildingID} ===");
        Debug.Log($"名前: {data.Value.buildingName}");
        Debug.Log($"プレイヤーID: {data.Value.playerID}");
        Debug.Log($"HP: {data.Value.currentHP}");
        Debug.Log($"状態: {data.Value.state}");
        Debug.Log($"残り建築コスト: {data.Value.remainingBuildCost}");
        Debug.Log($"HPレベル: {data.Value.hpLevel}");
        Debug.Log($"攻撃範囲レベル: {data.Value.attackRangeLevel}");
        Debug.Log($"スロットレベル: {data.Value.slotsLevel}");
    }
}
```

##### 使用状況のまとめ

| 状況 | 用途 |
|-----|------|
| ✅ 建物完成時 | 完成した建物のすべての情報を相手に通知 |
| ✅ 状態変更後 | アップグレード、ダメージ後の最新状態を同期 |
| ✅ ゲーム途中参加 | 既存のすべての建物を新プレイヤーに送信 |
| ✅ 再接続時 | 切断中に変わった可能性のある状態を再同期 |
| ✅ 定期同期 | 念のため定期的に完全な状態を送って同期ズレを防止 |
| ✅ デバッグ | 現在の完全な状態を確認・ログ出力 |

**重要なポイント:**

1. **完全な状態を含む** - HP、レベル、建築進捗など、すべての情報が含まれる
2. **一貫性保証** - 一度の呼び出しで一貫した状態のスナップショットを取得
3. **送信準備済み** - 返される`syncBuildingData`はそのままネットワーク送信可能
4. **NULL安全** - 建物が存在しない場合はnullを返す（`?`付き戻り値）

---

### 建物の削除

#### `HandleEnemyBuildingDestruction()`
受信した敵方の建物破壊通知を処理します。

**シグネチャ:**
```csharp
public bool HandleEnemyBuildingDestruction(syncBuildingData sbd)
```

**パラメータ:**
- `sbd`: 破壊された建物の同期データ（currentHP = 0）

**戻り値:**
- `true`: 削除成功
- `false`: 失敗（建物が存在しない）

**使用例:**
```csharp
// 敵方の建物破壊通知を受信
syncBuildingData destructionData = new syncBuildingData
{
    buildingID = 201,
    currentHP = 0,
    state = BuildingState.Ruined
};

if (buildingManager.HandleEnemyBuildingDestruction(destructionData))
{
    Debug.Log("敵方の建物を削除しました");
}
```

**注意:**
- このメソッドは建物を削除するのみで、`OnBuildingDestroyed` イベントを発火しません
- 受信側での破壊通知処理に使用し、無限ループを防ぎます

**実装箇所:** `BuildingManager.cs:689-706`

---

#### `GetLastDestroyedBuildingData()`
最後に破壊された建物の同期データを取得します（送信側用）。

**シグネチャ:**
```csharp
public syncBuildingData? GetLastDestroyedBuildingData()
```

**戻り値:**
- 最後に破壊された建物の同期データ
- キャッシュがない場合は null

**使用例:**
```csharp
// GameManagerで建物破壊イベントを購読
buildingManager.OnBuildingDestroyed += (buildingID) =>
{
    // 破壊された建物の完全なデータを取得
    syncBuildingData? destructionData = buildingManager.GetLastDestroyedBuildingData();
    if (destructionData.HasValue)
    {
        // 相手プレイヤーに送信
        NetworkManager.SendBuildingDestruction(destructionData.Value);
    }
};
```

**注意:**
- データ取得後は自動的にクリアされます（単回使用）
- `OnBuildingDestroyed` イベント発火後すぐに呼び出す必要があります
- 送信側で破壊データを取得して相手に通知する際に使用

**実装箇所:** `BuildingManager.cs:712-717`

---

#### `OnBuildingDestroyed` イベント
建物が破壊された時に発火するイベント。

**イベントシグネチャ:**
```csharp
public event Action<int> OnBuildingDestroyed;
```

**パラメータ:**
- `int`: 破壊された建物のID

**使用例:**
```csharp
// 建物破壊イベントを購読
buildingManager.OnBuildingDestroyed += HandleBuildingDestruction;

void HandleBuildingDestruction(int destroyedBuildingID)
{
    Debug.Log($"建物 {destroyedBuildingID} が破壊されました");

    // 破壊データを取得して相手に送信
    syncBuildingData? destructionData = buildingManager.GetLastDestroyedBuildingData();
    if (destructionData.HasValue)
    {
        NetworkManager.SendBuildingDestruction(destructionData.Value);
    }
}
```

**実装箇所:** `BuildingManager.cs:33`

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

### 建物建築フローの詳細

このセクションでは、建物の建築開始から完成まで、そして完成した建物情報をGameManagerに渡して同期するまでの完全なフローを詳しく説明します。

#### フロー図

```
GameManager
    │
    ├─ CreateBuilding() → BuildingManager
    │                         │
    │                         ├─ 建物生成（UnderConstruction状態）
    │                         └─ Building.Initialize()
    │                               │
    │                               └─ remainingBuildCost設定
    │
    ├─ AddFarmerToConstruction() → BuildingManager
    │                                  │
    │                                  ├─ 農民のAP消費
    │                                  └─ Building.ProgressConstruction()
    │                                        │
    │                                        ├─ remainingBuildCost を減少
    │                                        └─ 0になったら CompleteConstruction()
    │                                              │
    │                                              ├─ state を Inactive に変更
    │                                              └─ OnBuildingCompleted イベント発火
    │                                                    ↓
    │                        BuildingManager.HandleBuildingCompleted()
    │                                                    │
    │                                                    └─ OnBuildingCompleted イベント発火
    │                                                          ↓
    ├─ OnBuildingCompleted() ← GameManager がイベント受信
    │        │
    │        ├─ CreateCompleteSyncData() → BuildingManager
    │        │                                  │
    │        │                                  └─ syncBuildingData 生成
    │        │
    │        └─ ネットワーク送信
```

#### ステップバイステップの説明

**ステップ1: 建築開始（GameManager → BuildingManager）**

GameManagerが `CreateBuilding()` を呼び出して建物を生成します。この時点で建物は `UnderConstruction` 状態で作成されます。

```csharp
// GameManager側のコード
BuildingDataSO buildingData = // 建物データを取得
int buildingID = buildingManager.CreateBuilding(buildingData, playerID, position);
```

**ステップ2: 建物の初期化（Building内部）**

BuildingManagerは建物Prefabを生成し、`Building.Initialize()` を呼び出します。この時に：
- `playerID` が建物の所有者として設定される
- `remainingBuildCost` が初期建築コストに設定される
- 建物の状態が `UnderConstruction` になる
- `OnBuildingCompleted` イベントに購読する

```csharp
// Building.cs内部
public void Initialize(BuildingDataSO data, int ownerPlayerID)
{
    buildingData = data;
    playerID = ownerPlayerID; // 所有者を設定

    currentHp = data.maxHp;
    remainingBuildCost = data.buildingAPCost;
    currentState = BuildingState.UnderConstruction;

    // スロット初期化など...
}
```

**注意:** Building は自身の PlayerID を保持するため、BuildingManager で別途 `buildingOwners` 辞書を管理する必要はありません。

**ステップ3: 建築進行（GameManager → BuildingManager → Building）**

GameManagerが農民を投入して建築を進めます：

```csharp
// GameManager側のコード
bool success = buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager);
```

BuildingManagerは農民のAPを消費してから、`Building.ProgressConstruction()` を呼び出します：

```csharp
// BuildingManager.cs内部
public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
{
    // 農民のAPを確認・消費
    if (pieceManager.ConsumePieceAP(farmerID, farmer.Data.devotionAPCost))
    {
        // 建築を進行
        building.ProgressConstruction(progressAmount);
    }
}
```

**ステップ4: 建築コスト減少（Building内部）**

`Building.ProgressConstruction()` は `remainingBuildCost` を減少させ、0になったら完成処理を呼び出します：

```csharp
// Building.cs内部
public void ProgressConstruction(int amount)
{
    remainingBuildCost -= amount;

    if (remainingBuildCost <= 0)
    {
        remainingBuildCost = 0;
        CompleteConstruction();
    }
}
```

**ステップ5: 建築完成（Building内部）**

`Building.CompleteConstruction()` が呼ばれると：
- 状態を `Inactive` に変更
- `OnBuildingCompleted` イベントを発火

```csharp
// Building.cs内部
private void CompleteConstruction()
{
    currentState = BuildingState.Inactive;
    Debug.Log($"建物建築完了: {Data.buildingName} (ID: {buildingID})");

    // イベント発火
    OnBuildingCompleted?.Invoke(buildingID);
}
```

**ステップ6: BuildingManagerでイベント受信**

BuildingManagerは `HandleBuildingCompleted()` で建物完成イベントを受け取り、GameManagerに通知します：

```csharp
// BuildingManager.cs内部
private void HandleBuildingCompleted(int buildingID)
{
    Debug.Log($"BuildingManager: 建物完成を検知 ID={buildingID}");

    // GameManagerにイベントを発火
    OnBuildingCompleted?.Invoke(buildingID);
}
```

**ステップ7: GameManagerで完成通知を受信し、ネットワーク送信**

GameManagerは `OnBuildingCompleted` イベントを購読して、完成した建物の情報を取得し、ネットワーク経由で相手に送信します：

```csharp
// GameManager側のコード
void OnBuildingCompleted(int buildingID)
{
    Debug.Log($"己方の建物が完成: ID={buildingID}");

    // 完成した建物の完全な同期データを取得
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);

    if (data.HasValue)
    {
        // ネットワーク経由で相手に送信
        networkManager.SendBuildingComplete(data.Value);
        Debug.Log($"建物完成通知を送信: ID={buildingID}");
    }
}
```

#### 完全な実装例

以下は、GameManagerでの完全な実装例です：

```csharp
public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private PieceManager pieceManager;
    private NetworkManager networkManager;
    private int localPlayerID = 1;

    void Start()
    {
        // 初期化
        buildingManager.SetLocalPlayerID(localPlayerID);
        pieceManager.SetLocalPlayerID(localPlayerID);

        // 建物完成イベントを購読
        buildingManager.OnBuildingCompleted += OnBuildingCompleted;

        // 建物破壊イベントを購読
        buildingManager.OnBuildingDestroyed += OnBuildingDestroyed;

        // ネットワークメッセージ受信を設定
        networkManager.OnBuildingCompleteReceived += OnEnemyBuildingCompleteReceived;
    }

    // === 己方の建物建築フロー ===

    /// <summary>
    /// 建物の建築を開始
    /// </summary>
    void StartBuildingConstruction()
    {
        // 1. 建物を生成（UnderConstruction状態）
        BuildingDataSO buildingData = GetBuildingData("祭壇");
        int buildingID = buildingManager.CreateBuilding(buildingData, localPlayerID, new Vector3(10, 0, 10));

        if (buildingID >= 0)
        {
            Debug.Log($"建物建築開始: ID={buildingID}");

            // 建物生成通知を相手に送信
            syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
            if (data.HasValue)
            {
                networkManager.SendBuildingCreate(data.Value);
            }
        }
    }

    /// <summary>
    /// 農民を投入して建築を進行
    /// </summary>
    void ProgressBuildingConstruction(int buildingID, int farmerID)
    {
        // 2. 農民を投入（APを消費して建築進行）
        if (buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager))
        {
            Debug.Log($"建築進行中: buildingID={buildingID}, farmerID={farmerID}");

            // 建築進行状況を相手に送信
            syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
            if (data.HasValue)
            {
                networkManager.SendBuildingUpdate(data.Value);
            }
        }
    }

    /// <summary>
    /// 建物完成時の処理（イベントから自動呼び出し）
    /// </summary>
    void OnBuildingCompleted(int buildingID)
    {
        Debug.Log($"己方の建物が完成: ID={buildingID}");

        // 3. 完成通知を相手に送信
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendBuildingComplete(data.Value);
            Debug.Log($"建物完成通知を送信: ID={buildingID}");
        }
    }

    /// <summary>
    /// 建物破壊時の処理（イベントから自動呼び出し）
    /// </summary>
    void OnBuildingDestroyed(int buildingID)
    {
        Debug.Log($"己方の建物が破壊: ID={buildingID}");

        // 破壊データを取得して送信
        syncBuildingData? data = buildingManager.GetLastDestroyedBuildingData();
        if (data.HasValue)
        {
            networkManager.SendBuildingDestruction(data.Value);
        }
    }

    // === 敵方の建物受信フロー ===

    /// <summary>
    /// 敵方の建物完成通知を受信
    /// </summary>
    void OnEnemyBuildingCompleteReceived(syncBuildingData data)
    {
        Debug.Log($"敵方の建物完成通知受信: ID={data.buildingID}");

        // 既に建物が存在する場合は状態を同期
        if (buildingManager.DoesBuildingExist(data.buildingID))
        {
            buildingManager.SyncEnemyBuildingState(data);
        }
        else
        {
            // 建物が存在しない場合は新規作成
            buildingManager.CreateEnemyBuilding(data);
        }
    }
}
```

#### 重要なポイント

1. **建築状態の遷移**
   - `UnderConstruction` → `Inactive` → `Active`
   - 完成直後は `Inactive`（農民が配置されると `Active` になる）

2. **イベント駆動設計**
   - Building → BuildingManager → GameManager という階層でイベントが伝播
   - 各層が疎結合で独立して動作

3. **syncBuildingDataの使用**
   - `remainingBuildCost` で建築進捗を含む
   - `state` で現在の建物状態を含む
   - 完全な状態を一つの構造体で表現

4. **ネットワーク同期のタイミング**
   - 建築開始時: 建物生成を通知
   - 建築進行時: 進捗状況を更新（オプション）
   - 建築完成時: 完成通知を送信（重要）

5. **両方のManagerを初期化する**
   - `buildingManager.SetLocalPlayerID()` と `pieceManager.SetLocalPlayerID()` の両方が必要
   - これにより己方/敵方の建物・駒を正しく区別できる

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
    public int playerID;              // 元々のプレイヤーID
    public int pieceID;
    public int currentHP;
    public int currentHPLevel;
    public int currentPID;            // 現在のプレイヤーID（魅惑された場合など）
    public int swapCooldownLevel;     // 教皇専用
    public int buffLevel;             // 教皇専用
    public int occupyLevel;           // 宣教師専用
    public int convertEnemyLevel;     // 宣教師専用
    public int sacrificeLevel;        // 農民専用
    public int attackPowerLevel;      // 軍隊専用
    public int charmedTurnsRemaining; // 魅惑残りターン数（ネットワーク同期用）
}
```

**魅惑関連フィールドの説明:**
- `currentPID`: 魅惑された駒の場合、魅惑したプレイヤーのIDが設定される
- `charmedTurnsRemaining`: 魅惑が解除されるまでの残りターン数（0の場合は魅惑されていない）

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

> **⚠️ 重要: 初期化について**
>
> ネットワーク同期を使用する際は、**必ず両方のManagerに対して `SetLocalPlayerID()` を呼び出してください**。
> 片方だけ設定すると、正しく同期されません。
>
> ```csharp
> // ✅ 正しい初期化
> pieceManager.SetLocalPlayerID(localPlayerID);
> buildingManager.SetLocalPlayerID(localPlayerID);
>
> // ❌ 片方だけ設定すると同期が動作しない
> pieceManager.SetLocalPlayerID(localPlayerID);
> // buildingManager の設定を忘れている！
> ```

```csharp
public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BuildingManager buildingManager;
    private int localPlayerID = 1;

    void Start()
    {
        // ⚠️ 重要: 両方のManagerに対してlocalPlayerIDを設定する必要があります
        pieceManager.SetLocalPlayerID(localPlayerID);
        buildingManager.SetLocalPlayerID(localPlayerID);

        // イベント購読
        pieceManager.OnPieceDied += OnPieceDied;
        buildingManager.OnBuildingDestroyed += OnBuildingDestroyed;
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

## データ構造

### `syncBuildingData`構造体
建物の同期データ構造体（ネットワーク同期用）。

```csharp
[System.Serializable]
public struct syncBuildingData
{
    // 基本情報
    public int buildingID;
    public string buildingName;
    public int playerID;
    public Vector3 position;

    // 状態情報
    public int currentHP;
    public BuildingState state;
    public int remainingBuildCost; // 残り建築コスト

    // アップグレードレベル
    public int hpLevel;            // HP等級 (0-3)
    public int attackRangeLevel;   // 攻撃範囲等級 (0-3)
    public int slotsLevel;         // スロット数等級 (0-3)
    public int buildCostLevel;     // 建造コスト等級 (0-3)
}
```

---

## まとめ

### PieceManagerの機能

- ✅ **IDベース管理**: 具体的な型を意識せずに操作可能
- ✅ **ネットワーク同期対応**: 己方と敵駒を分離管理
- ✅ **同期データ自動生成**: 各種操作後に`syncPieceData`を返す
- ✅ **送信/受信分離**: 無限ループを防ぐ明確な設計
- ✅ **イベント駆動**: 駒の死亡などを自動通知

### BuildingManagerの機能

- ✅ **IDベース管理**: 建物を型に依存せず管理
- ✅ **ネットワーク同期対応**: 己方と敵方の建物を分離管理
- ✅ **完全な状態同期**: `syncBuildingData`で建物のすべての情報を含む
- ✅ **破壊処理の分離**: 送信側と受信側で異なる処理フロー
- ✅ **イベント駆動**: 建物の破壊・完成を自動通知

すべての操作はIDベースで統一されており、ネットワーク同期も簡潔に実装できます。
