# Manager システム API リファレンス

このドキュメントでは、PieceManagerとBuildingManagerを使用したID ベースのユニット・建物管理システムの使用方法を説明します。

---

## 目次

1. [概要](#1-概要)
2. [PieceManager - 駒管理](#2-piecemanager---駒管理)
   - [駒の生成](#駒の生成)
   - [アップグレード管理](#アップグレード管理)
   - [情報取得](#情報取得)
   - [駒の行動](#駒の行動)
   - [AP管理](#ap管理)
   - [ターン処理](#ターン処理)
3. [BuildingManager - 建物管理](#3-buildingmanager---建物管理)
   - [建物の生成](#建物の生成)
   - [建築処理](#建築処理)
   - [農民配置](#農民配置)
   - [建物のアップグレード](#建物のアップグレード)
   - [建物の情報取得](#建物の情報取得)
   - [建物のダメージ処理](#建物のダメージ処理)
   - [建物のターン処理](#建物のターン処理)

---

## 1. 概要

### 設計思想

PieceManagerとBuildingManagerは、具体的なユニットクラス（Farmer, Military, Missionary, Pope）や建物クラスを**IDベースで管理**するラッパー層です。

**メリット:**
- GameManagerは具体的な型（Farmer, Military等）を意識せずに操作可能
- 整数IDのみで全ての駒・建物を管理
- 型安全性を保ちつつ、インターフェースをシンプルに

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

### 駒の生成

#### `CreatePiece()`
駒を生成してIDを返します。

**シグネチャ:**
```csharp
public int CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
```

**パラメータ:**
- `pieceType`: 駒の種類（PieceType.Farmer, Military, Missionary, Pope）
- `religion`: 宗教（Religion.Maya, RedMoon, MadScientist, Silk）
- `playerID`: プレイヤーID
- `position`: 生成位置

**戻り値:**
- 成功: 生成された駒のID（正の整数）
- 失敗: -1

**使用例:**
```csharp
PieceManager pieceManager = // PieceManagerの参照を取得

// 農民を生成
int farmerID = pieceManager.CreatePiece(
    PieceType.Farmer,
    Religion.Maya,
    playerID: 1,
    new Vector3(10, 0, 10)
);

if (farmerID >= 0)
{
    Debug.Log($"農民を生成しました: ID={farmerID}");
}
```

**実装箇所:** `PieceManager.cs:36-74`

---

### アップグレード管理

#### `UpgradePiece()`
駒の共通項目（HP/AP）をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradePiece(int pieceID, PieceUpgradeType upgradeType)
```

**パラメータ:**
- `pieceID`: 駒ID
- `upgradeType`: アップグレード項目（PieceUpgradeType.HP または AP）

**戻り値:**
- `true`: アップグレード成功
- `false`: 失敗（駒が存在しない、最大レベル、コスト不足等）

**使用例:**
```csharp
// HPをアップグレード
if (pieceManager.UpgradePiece(farmerID, PieceUpgradeType.HP))
{
    Debug.Log("HPアップグレード成功");
}

// APをアップグレード
if (pieceManager.UpgradePiece(farmerID, PieceUpgradeType.AP))
{
    Debug.Log("APアップグレード成功");
}
```

**実装箇所:** `PieceManager.cs:86-104`

---

#### `UpgradePieceSpecial()`
駒の職業別専用項目をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradePieceSpecial(int pieceID, SpecialUpgradeType specialUpgradeType)
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
- `true`: アップグレード成功
- `false`: 失敗

**使用例:**
```csharp
// 農民の獻祭回復量をアップグレード
if (pieceManager.UpgradePieceSpecial(farmerID, SpecialUpgradeType.FarmerSacrifice))
{
    Debug.Log("農民の獻祭回復量アップグレード成功");
}

// 軍隊の攻撃力をアップグレード
if (pieceManager.UpgradePieceSpecial(militaryID, SpecialUpgradeType.MilitaryAttackPower))
{
    Debug.Log("軍隊の攻撃力アップグレード成功");
}
```

**実装箇所:** `PieceManager.cs:112-150`

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

**使用例:**
```csharp
int hpCost = pieceManager.GetUpgradeCost(farmerID, PieceUpgradeType.HP);
if (hpCost > 0)
{
    Debug.Log($"HPアップグレードには{hpCost}の資源が必要です");
}
```

**実装箇所:** `PieceManager.cs:158-167`

---

#### `CanUpgrade()`
アップグレード可能かチェックします。

**シグネチャ:**
```csharp
public bool CanUpgrade(int pieceID, PieceUpgradeType upgradeType)
```

**パラメータ:**
- `pieceID`: 駒ID
- `upgradeType`: アップグレード項目

**戻り値:**
- `true`: アップグレード可能
- `false`: アップグレード不可

**使用例:**
```csharp
if (pieceManager.CanUpgrade(farmerID, PieceUpgradeType.HP))
{
    // HPアップグレードボタンを有効化
    hpUpgradeButton.interactable = true;
}
```

**実装箇所:** `PieceManager.cs:175-183`

---

### 情報取得

#### `GetPieceHP()`
駒の現在HPを取得します。

**シグネチャ:**
```csharp
public float GetPieceHP(int pieceID)
```

**実装箇所:** `PieceManager.cs:192-200`

---

#### `GetPieceAP()`
駒の現在APを取得します。

**シグネチャ:**
```csharp
public float GetPieceAP(int pieceID)
```

**実装箇所:** `PieceManager.cs:205-213`

---

#### `GetPiecePlayerID()`
駒の所属プレイヤーIDを取得します。

**シグネチャ:**
```csharp
public int GetPiecePlayerID(int pieceID)
```

**実装箇所:** `PieceManager.cs:218-226`

---

#### `GetPieceType()`
駒の種類を取得します。

**シグネチャ:**
```csharp
public PieceType GetPieceType(int pieceID)
```

**戻り値:**
- PieceType.Farmer, Military, Missionary, Pope, またはNone

**実装箇所:** `PieceManager.cs:231-247`

---

#### `DoesPieceExist()`
駒が存在するかチェックします。

**シグネチャ:**
```csharp
public bool DoesPieceExist(int pieceID)
```

**実装箇所:** `PieceManager.cs:252-255`

---

#### `GetPlayerPieces()`
指定プレイヤーのすべての駒IDを取得します。

**シグネチャ:**
```csharp
public List<int> GetPlayerPieces(int playerID)
```

**戻り値:**
- 駒IDのリスト

**使用例:**
```csharp
List<int> player1Pieces = pieceManager.GetPlayerPieces(1);
Debug.Log($"プレイヤー1の駒数: {player1Pieces.Count}");

foreach (int pieceID in player1Pieces)
{
    float hp = pieceManager.GetPieceHP(pieceID);
    Debug.Log($"駒ID={pieceID}, HP={hp}");
}
```

**実装箇所:** `PieceManager.cs:260-266`

---

#### `GetPlayerPiecesByType()`
指定プレイヤーの指定種類の駒IDを取得します。

**シグネチャ:**
```csharp
public List<int> GetPlayerPiecesByType(int playerID, PieceType pieceType)
```

**使用例:**
```csharp
// プレイヤー1の農民をすべて取得
List<int> farmers = pieceManager.GetPlayerPiecesByType(1, PieceType.Farmer);
Debug.Log($"プレイヤー1の農民数: {farmers.Count}");
```

**実装箇所:** `PieceManager.cs:271-276`

---

### 駒の行動

#### `AttackEnemy()`
軍隊が敵を攻撃します。

**シグネチャ:**
```csharp
public bool AttackEnemy(int attackerID, int targetID)
```

**パラメータ:**
- `attackerID`: 攻撃者の駒ID（軍隊である必要がある）
- `targetID`: ターゲットの駒ID

**戻り値:**
- `true`: 攻撃成功
- `false`: 失敗（駒が存在しない、攻撃者が軍隊ではない等）

**使用例:**
```csharp
if (pieceManager.AttackEnemy(militaryID, enemyID))
{
    Debug.Log("攻撃成功");
}
```

**実装箇所:** `PieceManager.cs:331-352`

---

#### `ConvertEnemy()`
宣教師が敵を魅惑します。

**シグネチャ:**
```csharp
public bool ConvertEnemy(int missionaryID, int targetID)
```

**パラメータ:**
- `missionaryID`: 宣教師の駒ID
- `targetID`: ターゲットの駒ID

**戻り値:**
- `true`: 魅惑試行成功（成功率判定は内部で実施）
- `false`: 失敗

**使用例:**
```csharp
if (pieceManager.ConvertEnemy(missionaryID, enemyID))
{
    Debug.Log("魅惑を試みました");
}
```

**実装箇所:** `PieceManager.cs:360-381`

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

**実装箇所:** `PieceManager.cs:389-404`

---

#### `SacrificeToPiece()`
農民が他の駒を回復（獻祭）します。

**シグネチャ:**
```csharp
public bool SacrificeToPiece(int farmerID, int targetID)
```

**パラメータ:**
- `farmerID`: 農民の駒ID
- `targetID`: 回復対象の駒ID

**戻り値:**
- `true`: 回復成功
- `false`: 失敗

**実装箇所:** `PieceManager.cs:412-433`

---

#### `SwapPositions()`
教皇が味方駒と位置を交換します。

**シグネチャ:**
```csharp
public bool SwapPositions(int popeID, int targetID)
```

**パラメータ:**
- `popeID`: 教皇の駒ID
- `targetID`: 交換対象の駒ID

**戻り値:**
- `true`: 交換成功
- `false`: 失敗

**実装箇所:** `PieceManager.cs:441-462`

---

#### `DamagePiece()`
駒にダメージを与えます。

**シグネチャ:**
```csharp
public void DamagePiece(int pieceID, float damage, int attackerID = -1)
```

**パラメータ:**
- `pieceID`: 駒ID
- `damage`: ダメージ量
- `attackerID`: 攻撃者ID（オプション）

**実装箇所:** `PieceManager.cs:470-487`

---

#### `HealPiece()`
駒を回復します。

**シグネチャ:**
```csharp
public void HealPiece(int pieceID, float amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 回復量

**実装箇所:** `PieceManager.cs:494-503`

---

### AP管理

#### `ConsumePieceAP()`
駒のAPを消費します。

**シグネチャ:**
```csharp
public bool ConsumePieceAP(int pieceID, float amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 消費量

**戻り値:**
- `true`: 消費成功
- `false`: 失敗（AP不足等）

**使用例:**
```csharp
if (pieceManager.ConsumePieceAP(farmerID, 5.0f))
{
    Debug.Log("5APを消費しました");
}
else
{
    Debug.Log("AP不足");
}
```

**実装箇所:** `PieceManager.cs:515-524`

---

#### `RecoverPieceAP()`
駒のAPを回復します。

**シグネチャ:**
```csharp
public void RecoverPieceAP(int pieceID, float amount)
```

**パラメータ:**
- `pieceID`: 駒ID
- `amount`: 回復量

**使用例:**
```csharp
pieceManager.RecoverPieceAP(farmerID, 10.0f);
```

**実装箇所:** `PieceManager.cs:531-540`

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
pieceManager.ProcessTurnStart(currentPlayerID);
```

**実装箇所:** `PieceManager.cs:577-590`

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

**使用例:**
```csharp
BuildingManager buildingManager = // BuildingManagerの参照を取得
BuildingDataSO buildingData = // 建物データを取得

int buildingID = buildingManager.CreateBuilding(buildingData, playerID: 1, new Vector3(10, 0, 10));

if (buildingID >= 0)
{
    Debug.Log($"建物を生成しました: ID={buildingID}");
}
```

**実装箇所:** `BuildingManager.cs:40-77`

---

#### `CreateBuildingByName()`
建物名から建物を生成します（便利メソッド）。

**シグネチャ:**
```csharp
public int CreateBuildingByName(string buildingName, int playerID, Vector3 position)
```

**パラメータ:**
- `buildingName`: 建物名
- `playerID`: プレイヤーID
- `position`: 生成位置

**戻り値:**
- 成功: 生成された建物のID
- 失敗: -1

**使用例:**
```csharp
int buildingID = buildingManager.CreateBuildingByName("祭壇", 1, new Vector3(10, 0, 10));
```

**実装箇所:** `BuildingManager.cs:85-97`

---

### 建築処理

#### `AddFarmerToConstruction()`
建物の建築を進めます（農民を投入）。

**シグネチャ:**
```csharp
public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
```

**パラメータ:**
- `buildingID`: 建物ID
- `farmerID`: 投入する農民の駒ID
- `pieceManager`: PieceManagerの参照

**戻り値:**
- `true`: 建築進行成功
- `false`: 失敗

**使用例:**
```csharp
if (buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager))
{
    Debug.Log("建築が進みました");
}
```

**処理の流れ:**
1. 農民のAPを確認
2. 建物の残り建築コストを確認
3. 農民のAPを消費
4. 建物の建築を進行
5. 完成した場合はログ出力

**実装箇所:** `BuildingManager.cs:106-158`

---

#### `CancelConstruction()`
建築をキャンセルします。

**シグネチャ:**
```csharp
public bool CancelConstruction(int buildingID)
```

**パラメータ:**
- `buildingID`: 建物ID

**戻り値:**
- `true`: キャンセル成功
- `false`: 失敗

**注意:** 消耗された農民と行動力は返されません。

**実装箇所:** `BuildingManager.cs:165-175`

---

### 農民配置

#### `EnterBuilding()`
農民を建物に配置します。

**シグネチャ:**
```csharp
public bool EnterBuilding(int buildingID, int farmerID, PieceManager pieceManager)
```

**パラメータ:**
- `buildingID`: 建物ID
- `farmerID`: 農民の駒ID
- `pieceManager`: PieceManagerの参照

**戻り値:**
- `true`: 配置成功
- `false`: 失敗（スロットが満員、建物が未完成等）

**使用例:**
```csharp
if (buildingManager.EnterBuilding(buildingID, farmerID, pieceManager))
{
    Debug.Log("農民を建物に配置しました");
}
else
{
    Debug.Log("配置失敗（スロットが満員または建物が未完成）");
}
```

**実装箇所:** `BuildingManager.cs:182-230`

---

### 建物のアップグレード

#### `UpgradeBuilding()`
建物の項目をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradeBuilding(int buildingID, BuildingUpgradeType upgradeType)
```

**パラメータ:**
- `buildingID`: 建物ID
- `upgradeType`: アップグレード項目
  - `BuildingUpgradeType.HP` - 最大HP
  - `BuildingUpgradeType.AttackRange` - 攻撃範囲
  - `BuildingUpgradeType.Slots` - スロット数
  - `BuildingUpgradeType.BuildCost` - 建築コスト

**戻り値:**
- `true`: アップグレード成功
- `false`: 失敗

**使用例:**
```csharp
// HPをアップグレード
if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP))
{
    Debug.Log("建物のHPをアップグレードしました");
}

// スロット数をアップグレード
if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.Slots))
{
    Debug.Log("建物のスロット数をアップグレードしました");
}
```

**実装箇所:** `BuildingManager.cs:240-258`

---

#### `GetUpgradeCost()` (Building)
建物のアップグレードコストを取得します。

**シグネチャ:**
```csharp
public int GetUpgradeCost(int buildingID, BuildingUpgradeType upgradeType)
```

**実装箇所:** `BuildingManager.cs:266-276`

---

#### `CanUpgrade()` (Building)
建物がアップグレード可能かチェックします。

**シグネチャ:**
```csharp
public bool CanUpgrade(int buildingID, BuildingUpgradeType upgradeType)
```

**実装箇所:** `BuildingManager.cs:284-293`

---

### 建物の情報取得

#### `GetBuildingHP()`
建物の現在HPを取得します。

**シグネチャ:**
```csharp
public int GetBuildingHP(int buildingID)
```

**実装箇所:** `BuildingManager.cs:301-309`

---

#### `GetBuildingState()`
建物の状態を取得します。

**シグネチャ:**
```csharp
public BuildingState GetBuildingState(int buildingID)
```

**戻り値:**
- `BuildingState.UnderConstruction` - 建築中
- `BuildingState.Active` - 稼働中
- `BuildingState.Inactive` - 未稼働
- `BuildingState.Ruined` - 廃墟

**実装箇所:** `BuildingManager.cs:314-322`

---

#### `GetBuildProgress()`
建築進捗を取得します（0.0～1.0）。

**シグネチャ:**
```csharp
public float GetBuildProgress(int buildingID)
```

**戻り値:**
- 0.0～1.0の範囲（0.0=未着手、1.0=完成）

**実装箇所:** `BuildingManager.cs:327-335`

---

#### `DoesBuildingExist()`
建物が存在するかチェックします。

**シグネチャ:**
```csharp
public bool DoesBuildingExist(int buildingID)
```

**実装箇所:** `BuildingManager.cs:340-343`

---

#### `GetAllBuildingIDs()`
すべての建物IDを取得します。

**シグネチャ:**
```csharp
public List<int> GetAllBuildingIDs()
```

**実装箇所:** `BuildingManager.cs:350-353`

---

#### `GetOperationalBuildings()`
稼働中の建物IDリストを取得します。

**シグネチャ:**
```csharp
public List<int> GetOperationalBuildings()
```

**実装箇所:** `BuildingManager.cs:358-364`

---

#### `GetBuildingsUnderConstruction()`
建築中の建物IDリストを取得します。

**シグネチャ:**
```csharp
public List<int> GetBuildingsUnderConstruction()
```

**実装箇所:** `BuildingManager.cs:369-375`

---

### 建物のダメージ処理

#### `DamageBuilding()`
建物にダメージを与えます。

**シグネチャ:**
```csharp
public bool DamageBuilding(int buildingID, int damage)
```

**パラメータ:**
- `buildingID`: 建物ID
- `damage`: ダメージ量

**戻り値:**
- `true`: ダメージ付与成功
- `false`: 失敗

**実装箇所:** `BuildingManager.cs:413-423`

---

#### `RemoveBuilding()`
建物を強制削除します。

**シグネチャ:**
```csharp
public bool RemoveBuilding(int buildingID)
```

**実装箇所:** `BuildingManager.cs:403-418`

---

### 建物のターン処理

#### `ProcessTurnStart()` (Building)
すべての建物のターン処理（資源生成など）を実行します。

**シグネチャ:**
```csharp
public void ProcessTurnStart(int currentTurn)
```

**パラメータ:**
- `currentTurn`: 現在のターン数

**使用例:**
```csharp
// ターン開始時に呼び出し
buildingManager.ProcessTurnStart(currentTurn);
```

**実装箇所:** `BuildingManager.cs:431-443`

---

## 使用例: 完全なゲームフロー

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BuildingManager buildingManager;

    private int currentPlayerID = 1;
    private int currentTurn = 0;

    void Start()
    {
        // 農民を生成
        int farmerID = pieceManager.CreatePiece(
            PieceType.Farmer,
            Religion.Maya,
            currentPlayerID,
            new Vector3(0, 0, 0)
        );

        // 軍隊を生成
        int militaryID = pieceManager.CreatePiece(
            PieceType.Military,
            Religion.Maya,
            currentPlayerID,
            new Vector3(5, 0, 0)
        );

        // 建物を生成
        int buildingID = buildingManager.CreateBuildingByName(
            "祭壇",
            currentPlayerID,
            new Vector3(10, 0, 10)
        );

        // 農民を建築に投入
        buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager);

        // 建物のアップグレード
        if (buildingManager.CanUpgrade(buildingID, BuildingUpgradeType.HP))
        {
            buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP);
        }

        // 駒のアップグレード
        if (pieceManager.CanUpgrade(militaryID, PieceUpgradeType.HP))
        {
            pieceManager.UpgradePiece(militaryID, PieceUpgradeType.HP);
        }
    }

    void OnTurnStart()
    {
        currentTurn++;

        // ターン処理
        pieceManager.ProcessTurnStart(currentPlayerID);
        buildingManager.ProcessTurnStart(currentTurn);

        // 各駒のAPを回復
        List<int> playerPieces = pieceManager.GetPlayerPieces(currentPlayerID);
        foreach (int pieceID in playerPieces)
        {
            pieceManager.RecoverPieceAP(pieceID, 5.0f);
        }
    }

    void OnAttackButtonClick(int attackerID, int targetID)
    {
        // 攻撃実行
        if (pieceManager.AttackEnemy(attackerID, targetID))
        {
            Debug.Log("攻撃成功");
        }
    }
}
```

---

## 列挙型（Enums）

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

### BuildingUpgradeType
```csharp
public enum BuildingUpgradeType
{
    HP,             // 最大HP
    AttackRange,    // 攻撃範囲
    Slots,          // スロット数
    BuildCost       // 建築コスト
}
```

### BuildingState
```csharp
public enum BuildingState
{
    UnderConstruction,  // 建築中
    Active,            // 稼働中
    Inactive,          // 未稼働
    Ruined            // 廃墟
}
```

---

## まとめ

PieceManagerとBuildingManagerを使用することで、GameManagerは：
- ✅ 具体的なユニットクラス（Farmer, Military等）を意識せずに操作可能
- ✅ 整数IDのみで全ての駒・建物を管理
- ✅ 型安全性を保ちつつ、シンプルなインターフェースで実装可能
- ✅ コードの保守性・拡張性が向上

すべての操作はIDベースで統一されており、GameManagerのコードがシンプルで読みやすくなります。
