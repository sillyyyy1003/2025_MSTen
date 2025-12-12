# アップグレードシステム仕様書

**作成日**: 2025年12月10日
**バージョン**: 2.0（個別レベルシステム）

---

## 目次

1. [概要](#概要)
2. [システム構造](#システム構造)
3. [駒（Piece）のアップグレード](#駒pieceのアップグレード)
4. [建物（Building）のアップグレード](#建物buildingのアップグレード)
5. [レベルの管理と反映](#レベルの管理と反映)
6. [API リファレンス](#apiリファレンス)
7. [実装例](#実装例)

---

## 概要

### 変更履歴

**旧システム（v1.0）**
- 単一の`upgradeLevel`変数でレベル管理
- 全ての能力が一括でアップグレード
- 柔軟性に欠ける

**新システム（v2.0）**
- 各項目ごとに個別のレベル管理
- HP、AP、専用スキルを独立してアップグレード可能
- 外部からのレベル設定に対応

### システムの特徴

- ✅ **個別レベル管理**: HP、AP、各専用スキルが独立したレベルを持つ
- ✅ **外部レベル管理対応**: プレイヤーデータから一括でレベルを設定可能
- ✅ **新規生成時の反映**: 生成時にレベルを指定できる
- ✅ **既存オブジェクトの更新**: Setterメソッドで既存のレベルを変更可能

---

## システム構造

### 駒（Piece）の個別レベル

#### 共通レベル（全ての駒）

| レベル名 | 範囲 | 説明 |
|---------|------|------|
| `hpLevel` | 0-3 | HP（体力）レベル |
| `apLevel` | 0-3 | AP（行動力）レベル |

#### 職業別専用レベル

| 職業 | 専用レベル | 範囲 | 説明 |
|------|-----------|------|------|
| **宣教師** | `occupyLevel` | 0-3 | 占領成功率レベル |
| | `convertEnemyLevel` | 0-3 | 魅惑成功率レベル |
| **十字軍** | `attackPowerLevel` | 0-3 | 攻撃力レベル |
| **信徒** | `sacrificeLevel` | 0-2 | 獻祭回復量レベル |
| **教皇** | `swapCooldownLevel` | 0-2 | 位置交換CDレベル |
| | `buffLevel` | 0-3 | バフ効果レベル |

### 建物（Building）の個別レベル

| レベル名 | 範囲 | 説明 |
|---------|------|------|
| `hpLevel` | 0-2 | 建物HPレベル |
| `attackRangeLevel` | 0-2 | 攻撃範囲レベル |
| `slotsLevel` | 0-2 | スロット数レベル |

---

## 駒（Piece）のアップグレード

### 初期化時のレベル設定

#### 基本的な初期化

```csharp
// レベル0で初期化（デフォルト）
piece.Initialize(pieceData, playerID);

// レベルを指定して初期化
piece.Initialize(pieceData, playerID,
    initHPLevel: 1,   // HP レベル1
    initAPLevel: 2);  // AP レベル2
```

#### 職業別の拡張初期化

**宣教師の例**

```csharp
// 専用スキルレベルも指定
missionary.Initialize(missionaryData, playerID,
    initHPLevel: 1,
    initAPLevel: 1,
    initOccupyLevel: 2,        // 占領レベル2
    initConvertEnemyLevel: 1); // 魅惑レベル1
```

**他の職業も同様**

```csharp
// 十字軍
military.Initialize(militaryData, playerID, 1, 1,
    initAttackPowerLevel: 2);

// 信徒
farmer.Initialize(farmerData, playerID, 1, 1,
    initSacrificeLevel: 1);

// 教皇
pope.Initialize(popeData, playerID, 1, 1,
    initSwapCooldownLevel: 1,
    initBuffLevel: 2);
```

### 個別アップグレードメソッド

#### 共通アップグレード

```csharp
// HPを1レベル上げる
bool success = piece.UpgradeHP();

// APを1レベル上げる
bool success = piece.UpgradeAP();
```

#### 職業別専用アップグレード

```csharp
// 宣教師
missionary.UpgradeOccupy();         // 占領レベルアップ
missionary.UpgradeConvertEnemy();   // 魅惑レベルアップ

// 十字軍
military.UpgradeAttackPower();      // 攻撃力レベルアップ

// 信徒
farmer.UpgradeSacrifice();          // 獻祭レベルアップ

// 教皇
pope.UpgradeSwapCooldown();         // 位置交換CDレベルアップ
pope.UpgradeBuff();                 // バフレベルアップ
```

### 既存オブジェクトのレベル設定

```csharp
// 既存の駒のレベルを直接設定
piece.SetHPLevel(2);  // HPレベルを2に設定
piece.SetAPLevel(1);  // APレベルを1に設定

// 専用スキルレベルも設定可能
missionary.SetOccupyLevel(1);
missionary.SetConvertEnemyLevel(2);
military.SetAttackPowerLevel(3);
farmer.SetSacrificeLevel(1);
pope.SetSwapCooldownLevel(1);
pope.SetBuffLevel(2);
```

### レベルの取得

```csharp
// 共通レベル
int hpLevel = piece.HPLevel;
int apLevel = piece.APLevel;

// 専用レベル
int occupyLevel = missionary.OccupyLevel;
int convertLevel = missionary.ConvertEnemyLevel;
int attackLevel = military.AttackPowerLevel;
int sacrificeLevel = farmer.SacrificeLevel;
int swapLevel = pope.SwapCooldownLevel;
int buffLevel = pope.BuffLevel;
```

---

## 建物（Building）のアップグレード

### 初期化時のレベル設定

```csharp
// レベル0で初期化（デフォルト）
building.Initialize(buildingData, playerID);

// レベルを指定して初期化
building.Initialize(buildingData, playerID,
    initHPLevel: 1,           // HP レベル1
    initAttackRangeLevel: 1,  // 攻撃範囲レベル1
    initSlotsLevel: 2);       // スロット数レベル2
```

### 個別アップグレードメソッド

```csharp
// HPを1レベル上げる
bool success = building.UpgradeHP();

// 攻撃範囲を1レベル上げる
bool success = building.UpgradeAttackRange();

// スロット数を1レベル上げる
bool success = building.UpgradeSlots();
```

### 既存オブジェクトのレベル設定

```csharp
// 既存の建物のレベルを直接設定
building.SetHPLevel(2);
building.SetAttackRangeLevel(1);
building.SetSlotsLevel(2);
```

### レベルの取得

```csharp
int hpLevel = building.HPLevel;
int attackRangeLevel = building.AttackRangeLevel;
int slotsLevel = building.SlotsLevel;
```

---

## レベルの管理と反映

### 外部レベル管理の実装パターン

#### 1. プレイヤーデータでレベルを管理

```csharp
public class PlayerUpgradeData
{
    // 駒の共通レベル
    public int pieceHPLevel = 0;
    public int pieceAPLevel = 0;

    // 職業別レベル
    public int missionaryOccupyLevel = 0;
    public int missionaryConvertLevel = 0;
    public int militaryAttackLevel = 0;
    public int farmerSacrificeLevel = 0;
    public int popeSwapLevel = 0;
    public int popeBuffLevel = 0;

    // 建物レベル
    public int buildingHPLevel = 0;
    public int buildingAttackRangeLevel = 0;
    public int buildingSlotsLevel = 0;
}
```

#### 2. 新規生成時の反映

```csharp
// PieceManager での実装例
public Piece CreatePiece(PieceDataSO data, int playerID)
{
    // プレイヤーのアップグレードデータを取得
    var upgradeData = GetPlayerUpgradeData(playerID);

    // レベルを指定して生成
    if (data is MissionaryDataSO)
    {
        missionary.Initialize(data, playerID,
            upgradeData.pieceHPLevel,
            upgradeData.pieceAPLevel,
            upgradeData.missionaryOccupyLevel,
            upgradeData.missionaryConvertLevel);
    }
    else
    {
        piece.Initialize(data, playerID,
            upgradeData.pieceHPLevel,
            upgradeData.pieceAPLevel);
    }

    return piece;
}
```

#### 3. 既存オブジェクトへの反映

```csharp
// レベルが変更されたときの処理
public void OnUpgradeLevelChanged(int playerID, PieceType pieceType, string levelType, int newLevel)
{
    // プレイヤーの全ての該当駒を取得
    var pieces = GetPlayerPiecesByType(playerID, pieceType);

    foreach (var piece in pieces)
    {
        switch (levelType)
        {
            case "HP":
                piece.SetHPLevel(newLevel);
                break;
            case "AP":
                piece.SetAPLevel(newLevel);
                break;
            case "Occupy":
                if (piece is Missionary missionary)
                    missionary.SetOccupyLevel(newLevel);
                break;
            // 他のレベルも同様...
        }
    }
}
```

### PieceManager/BuildingManager の自動反映機能

#### ApplyUpgradeLevelToNew()

新規生成された駒/建物に、保存されているアップグレードレベルを自動適用します。

```csharp
// PieceManager.cs
public bool ApplyUpgradeLevelToNew(Piece piece)
{
    // baseUpgradeData から共通レベルを取得
    int hpLevel = baseUpgradeData[PieceUpgradeType.HP];
    int apLevel = baseUpgradeData[PieceUpgradeType.AP];

    // レベル分だけアップグレードを実行
    for (int i = 0; i < hpLevel; i++)
        piece.UpgradeHP();
    for (int i = 0; i < apLevel; i++)
        piece.UpgradeAP();

    // 職業別レベルも同様に適用
    switch (piece)
    {
        case Missionary missionary:
            for (int i = 0; i < occupyLevel; i++)
                missionary.UpgradeOccupy();
            // ...
            break;
    }
}
```

---

## API リファレンス

### Piece（基底クラス）

#### 初期化

```csharp
public virtual void Initialize(PieceDataSO data, int playerID,
    int initHPLevel = 0, int initAPLevel = 0)
```

#### アップグレード

```csharp
public bool UpgradeHP()  // HPを1レベル上げる
public bool UpgradeAP()  // APを1レベル上げる
```

#### レベル設定

```csharp
public void SetHPLevel(int level)  // HPレベルを直接設定
public void SetAPLevel(int level)  // APレベルを直接設定
```

#### レベル取得

```csharp
public int HPLevel { get; }  // 現在のHPレベル
public int APLevel { get; }  // 現在のAPレベル
```

### Missionary（宣教師）

#### 初期化（拡張版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initOccupyLevel, int initConvertEnemyLevel)
```

#### アップグレード

```csharp
public bool UpgradeOccupy()         // 占領レベルを1上げる
public bool UpgradeConvertEnemy()   // 魅惑レベルを1上げる
```

#### レベル設定

```csharp
public void SetOccupyLevel(int level)
public void SetConvertEnemyLevel(int level)
```

#### レベル取得

```csharp
public int OccupyLevel { get; }
public int ConvertEnemyLevel { get; }
```

### MilitaryUnit（十字軍）

#### 初期化（拡張版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initAttackPowerLevel)
```

#### アップグレード

```csharp
public bool UpgradeAttackPower()  // 攻撃力レベルを1上げる
```

#### レベル設定

```csharp
public void SetAttackPowerLevel(int level)
```

#### レベル取得

```csharp
public int AttackPowerLevel { get; }
```

### Farmer（信徒）

#### 初期化（拡張版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initSacrificeLevel)
```

#### アップグレード

```csharp
public bool UpgradeSacrifice()  // 獻祭レベルを1上げる
```

#### レベル設定

```csharp
public void SetSacrificeLevel(int level)
```

#### レベル取得

```csharp
public int SacrificeLevel { get; }
```

### Pope（教皇）

#### 初期化（拡張版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initSwapCooldownLevel, int initBuffLevel)
```

#### アップグレード

```csharp
public bool UpgradeSwapCooldown()  // 位置交換CDレベルを1上げる
public bool UpgradeBuff()          // バフレベルを1上げる
```

#### レベル設定

```csharp
public void SetSwapCooldownLevel(int level)
public void SetBuffLevel(int level)
```

#### レベル取得

```csharp
public int SwapCooldownLevel { get; }
public int BuffLevel { get; }
```

### Building（建物）

#### 初期化

```csharp
public void Initialize(BuildingDataSO data, int ownerPlayerID,
    int initHPLevel = 0, int initAttackRangeLevel = 0, int initSlotsLevel = 0)
```

#### アップグレード

```csharp
public bool UpgradeHP()           // HPを1レベル上げる
public bool UpgradeAttackRange()  // 攻撃範囲を1レベル上げる
public bool UpgradeSlots()        // スロット数を1レベル上げる
```

#### レベル設定

```csharp
public void SetHPLevel(int level)
public void SetAttackRangeLevel(int level)
public void SetSlotsLevel(int level)
```

#### レベル取得

```csharp
public int HPLevel { get; }
public int AttackRangeLevel { get; }
public int SlotsLevel { get; }
```

---

## 実装例

### 例1: プレイヤーのアップグレード研究システム

```csharp
public class UpgradeResearchSystem
{
    private Dictionary<int, PlayerUpgradeData> playerUpgrades = new();

    // アップグレードを研究
    public void ResearchUpgrade(int playerID, PieceType pieceType, string upgradeType)
    {
        var data = playerUpgrades[playerID];

        // レベルを上げる
        switch (pieceType)
        {
            case PieceType.Missionary:
                if (upgradeType == "Occupy")
                    data.missionaryOccupyLevel++;
                break;
            // 他も同様...
        }

        // 既存の全ての該当駒に反映
        ApplyToExistingPieces(playerID, pieceType, upgradeType);
    }

    // 既存駒への反映
    private void ApplyToExistingPieces(int playerID, PieceType pieceType, string upgradeType)
    {
        var pieces = PieceManager.Instance.GetPlayerPiecesByType(playerID, pieceType);
        var data = playerUpgrades[playerID];

        foreach (var piece in pieces)
        {
            if (pieceType == PieceType.Missionary && upgradeType == "Occupy")
            {
                var missionary = piece as Missionary;
                missionary.SetOccupyLevel(data.missionaryOccupyLevel);
            }
        }
    }

    // 新規駒生成時
    public Piece CreatePieceWithUpgrades(PieceDataSO data, int playerID)
    {
        var upgradeData = playerUpgrades[playerID];

        if (data is MissionaryDataSO)
        {
            var missionary = Instantiate(missionaryPrefab);
            missionary.Initialize(data, playerID,
                upgradeData.pieceHPLevel,
                upgradeData.pieceAPLevel,
                upgradeData.missionaryOccupyLevel,
                upgradeData.missionaryConvertLevel);
            return missionary;
        }

        // 他の職業も同様...
    }
}
```

### 例2: セーブ/ロードシステム

```csharp
[System.Serializable]
public class SaveData
{
    // プレイヤーごとのアップグレードレベル
    public Dictionary<int, PlayerUpgradeData> playerUpgrades;

    // 個別の駒のレベル（特殊なケース用）
    public List<PieceSaveData> pieces;
}

[System.Serializable]
public class PieceSaveData
{
    public int pieceID;
    public int hpLevel;
    public int apLevel;
    public int occupyLevel;  // 宣教師の場合
    // ...
}

public class SaveSystem
{
    public void SaveGame()
    {
        SaveData data = new SaveData();

        // プレイヤーアップグレードデータを保存
        data.playerUpgrades = GetAllPlayerUpgrades();

        // 個別の駒データも保存（必要に応じて）
        foreach (var piece in PieceManager.Instance.GetAllPieces())
        {
            data.pieces.Add(new PieceSaveData
            {
                pieceID = piece.PieceID,
                hpLevel = piece.HPLevel,
                apLevel = piece.APLevel,
                // ...
            });
        }

        // ファイルに保存
        SaveToFile(data);
    }

    public void LoadGame()
    {
        SaveData data = LoadFromFile();

        // プレイヤーアップグレードデータを復元
        RestorePlayerUpgrades(data.playerUpgrades);

        // 駒を生成・復元
        foreach (var pieceData in data.pieces)
        {
            var piece = CreatePiece(pieceData);
            piece.SetHPLevel(pieceData.hpLevel);
            piece.SetAPLevel(pieceData.apLevel);
            // ...
        }
    }
}
```

### 例3: ネットワーク同期

```csharp
public class NetworkUpgradeSystem
{
    // アップグレード研究の同期
    public void SyncUpgradeResearch(int playerID, PieceType pieceType, string upgradeType)
    {
        // サーバーに送信
        SendToServer(new UpgradeMessage
        {
            playerID = playerID,
            pieceType = pieceType,
            upgradeType = upgradeType
        });
    }

    // サーバーから受信
    public void OnUpgradeMessageReceived(UpgradeMessage msg)
    {
        // 全てのクライアントで同じ処理を実行
        var upgradeSystem = GetUpgradeSystem();
        upgradeSystem.ResearchUpgrade(msg.playerID, msg.pieceType, msg.upgradeType);
    }

    // 既存駒のレベル同期
    public void SyncPieceLevel(int pieceID, string levelType, int newLevel)
    {
        SendToServer(new PieceLevelMessage
        {
            pieceID = pieceID,
            levelType = levelType,
            level = newLevel
        });
    }
}
```

---

## まとめ

### 新システムの利点

1. **柔軟性**: 各項目を独立してアップグレード可能
2. **拡張性**: 新しいレベルタイプを簡単に追加できる
3. **外部管理対応**: プレイヤーデータから一括管理が可能
4. **既存オブジェクト対応**: Setterで既存のレベルを変更可能

### 使い分けガイド

| 用途 | 推奨メソッド |
|------|------------|
| 新規生成 | `Initialize()` でレベルを指定 |
| 段階的アップグレード | `UpgradeXX()` を1回ずつ呼ぶ |
| 一括レベル変更 | `SetXXLevel()` で直接設定 |
| レベル確認 | `XXLevel` プロパティを取得 |

---

**ドキュメント終了**
