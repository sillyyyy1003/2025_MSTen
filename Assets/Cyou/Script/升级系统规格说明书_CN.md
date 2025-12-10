# 升级系统规格说明书

**创建日期**: 2025年12月10日
**版本**: 2.0（独立等级系统）

---

## 目录

1. [概述](#概述)
2. [系统架构](#系统架构)
3. [棋子（Piece）升级](#棋子piece升级)
4. [建筑（Building）升级](#建筑building升级)
5. [等级管理与应用](#等级管理与应用)
6. [API 参考](#api参考)
7. [实现示例](#实现示例)

---

## 概述

### 变更历史

**旧系统（v1.0）**
- 使用单一的`upgradeLevel`变量管理等级
- 所有能力批量升级
- 缺乏灵活性

**新系统（v2.0）**
- 每个项目独立管理等级
- HP、AP、专属技能可独立升级
- 支持外部等级设置

### 系统特点

- ✅ **独立等级管理**: HP、AP、各专属技能拥有独立等级
- ✅ **支持外部等级管理**: 可从玩家数据批量设置等级
- ✅ **新建时应用**: 生成时可指定等级
- ✅ **更新现有对象**: 通过Setter方法更改现有等级

---

## 系统架构

### 棋子（Piece）的独立等级

#### 通用等级（所有棋子）

| 等级名称 | 范围 | 说明 |
|---------|------|------|
| `hpLevel` | 0-3 | HP（生命值）等级 |
| `apLevel` | 0-3 | AP（行动力）等级 |

#### 职业专属等级

| 职业 | 专属等级 | 范围 | 说明 |
|------|---------|------|------|
| **传教士** | `occupyLevel` | 0-3 | 占领成功率等级 |
| | `convertEnemyLevel` | 0-3 | 魅惑成功率等级 |
| **十字军** | `attackPowerLevel` | 0-3 | 攻击力等级 |
| **信徒** | `sacrificeLevel` | 0-2 | 献祭恢复量等级 |
| **教皇** | `swapCooldownLevel` | 0-2 | 位置交换CD等级 |
| | `buffLevel` | 0-3 | Buff效果等级 |

### 建筑（Building）的独立等级

| 等级名称 | 范围 | 说明 |
|---------|------|------|
| `hpLevel` | 0-2 | 建筑HP等级 |
| `attackRangeLevel` | 0-2 | 攻击范围等级 |
| `slotsLevel` | 0-2 | 插槽数量等级 |

---

## 棋子（Piece）升级

### 初始化时设置等级

#### 基础初始化

```csharp
// 以等级0初始化（默认）
piece.Initialize(pieceData, playerID);

// 指定等级初始化
piece.Initialize(pieceData, playerID,
    initHPLevel: 1,   // HP等级1
    initAPLevel: 2);  // AP等级2
```

#### 职业扩展初始化

**传教士示例**

```csharp
// 同时指定专属技能等级
missionary.Initialize(missionaryData, playerID,
    initHPLevel: 1,
    initAPLevel: 1,
    initOccupyLevel: 2,        // 占领等级2
    initConvertEnemyLevel: 1); // 魅惑等级1
```

**其他职业类似**

```csharp
// 十字军
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

### 独立升级方法

#### 通用升级

```csharp
// HP提升1等级
bool success = piece.UpgradeHP();

// AP提升1等级
bool success = piece.UpgradeAP();
```

#### 职业专属升级

```csharp
// 传教士
missionary.UpgradeOccupy();         // 占领等级提升
missionary.UpgradeConvertEnemy();   // 魅惑等级提升

// 十字军
military.UpgradeAttackPower();      // 攻击力等级提升

// 信徒
farmer.UpgradeSacrifice();          // 献祭等级提升

// 教皇
pope.UpgradeSwapCooldown();         // 位置交换CD等级提升
pope.UpgradeBuff();                 // Buff等级提升
```

### 设置现有对象的等级

```csharp
// 直接设置现有棋子的等级
piece.SetHPLevel(2);  // 设置HP等级为2
piece.SetAPLevel(1);  // 设置AP等级为1

// 也可设置专属技能等级
missionary.SetOccupyLevel(1);
missionary.SetConvertEnemyLevel(2);
military.SetAttackPowerLevel(3);
farmer.SetSacrificeLevel(1);
pope.SetSwapCooldownLevel(1);
pope.SetBuffLevel(2);
```

### 获取等级

```csharp
// 通用等级
int hpLevel = piece.HPLevel;
int apLevel = piece.APLevel;

// 专属等级
int occupyLevel = missionary.OccupyLevel;
int convertLevel = missionary.ConvertEnemyLevel;
int attackLevel = military.AttackPowerLevel;
int sacrificeLevel = farmer.SacrificeLevel;
int swapLevel = pope.SwapCooldownLevel;
int buffLevel = pope.BuffLevel;
```

---

## 建筑（Building）升级

### 初始化时设置等级

```csharp
// 以等级0初始化（默认）
building.Initialize(buildingData, playerID);

// 指定等级初始化
building.Initialize(buildingData, playerID,
    initHPLevel: 1,           // HP等级1
    initAttackRangeLevel: 1,  // 攻击范围等级1
    initSlotsLevel: 2);       // 插槽数量等级2
```

### 独立升级方法

```csharp
// HP提升1等级
bool success = building.UpgradeHP();

// 攻击范围提升1等级
bool success = building.UpgradeAttackRange();

// 插槽数量提升1等级
bool success = building.UpgradeSlots();
```

### 设置现有对象的等级

```csharp
// 直接设置现有建筑的等级
building.SetHPLevel(2);
building.SetAttackRangeLevel(1);
building.SetSlotsLevel(2);
```

### 获取等级

```csharp
int hpLevel = building.HPLevel;
int attackRangeLevel = building.AttackRangeLevel;
int slotsLevel = building.SlotsLevel;
```

---

## 等级管理与应用

### 外部等级管理实现模式

#### 1. 玩家数据管理等级

```csharp
public class PlayerUpgradeData
{
    // 棋子通用等级
    public int pieceHPLevel = 0;
    public int pieceAPLevel = 0;

    // 职业专属等级
    public int missionaryOccupyLevel = 0;
    public int missionaryConvertLevel = 0;
    public int militaryAttackLevel = 0;
    public int farmerSacrificeLevel = 0;
    public int popeSwapLevel = 0;
    public int popeBuffLevel = 0;

    // 建筑等级
    public int buildingHPLevel = 0;
    public int buildingAttackRangeLevel = 0;
    public int buildingSlotsLevel = 0;
}
```

#### 2. 新建时应用等级

```csharp
// PieceManager 实现示例
public Piece CreatePiece(PieceDataSO data, int playerID)
{
    // 获取玩家升级数据
    var upgradeData = GetPlayerUpgradeData(playerID);

    // 指定等级生成
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

#### 3. 应用到现有对象

```csharp
// 等级变更时的处理
public void OnUpgradeLevelChanged(int playerID, PieceType pieceType, string levelType, int newLevel)
{
    // 获取玩家所有对应棋子
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
            // 其他等级同理...
        }
    }
}
```

### PieceManager/BuildingManager 的自动应用功能

#### ApplyUpgradeLevelToNew()

对新生成的棋子/建筑自动应用保存的升级等级。

```csharp
// PieceManager.cs
public bool ApplyUpgradeLevelToNew(Piece piece)
{
    // 从 baseUpgradeData 获取通用等级
    int hpLevel = baseUpgradeData[PieceUpgradeType.HP];
    int apLevel = baseUpgradeData[PieceUpgradeType.AP];

    // 执行对应次数的升级
    for (int i = 0; i < hpLevel; i++)
        piece.UpgradeHP();
    for (int i = 0; i < apLevel; i++)
        piece.UpgradeAP();

    // 职业专属等级同理
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

## API 参考

### Piece（基类）

#### 初始化

```csharp
public virtual void Initialize(PieceDataSO data, int playerID,
    int initHPLevel = 0, int initAPLevel = 0)
```

#### 升级

```csharp
public bool UpgradeHP()  // HP提升1等级
public bool UpgradeAP()  // AP提升1等级
```

#### 设置等级

```csharp
public void SetHPLevel(int level)  // 直接设置HP等级
public void SetAPLevel(int level)  // 直接设置AP等级
```

#### 获取等级

```csharp
public int HPLevel { get; }  // 当前HP等级
public int APLevel { get; }  // 当前AP等级
```

### Missionary（传教士）

#### 初始化（扩展版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initOccupyLevel, int initConvertEnemyLevel)
```

#### 升级

```csharp
public bool UpgradeOccupy()         // 占领等级提升1
public bool UpgradeConvertEnemy()   // 魅惑等级提升1
```

#### 设置等级

```csharp
public void SetOccupyLevel(int level)
public void SetConvertEnemyLevel(int level)
```

#### 获取等级

```csharp
public int OccupyLevel { get; }
public int ConvertEnemyLevel { get; }
```

### MilitaryUnit（十字军）

#### 初始化（扩展版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initAttackPowerLevel)
```

#### 升级

```csharp
public bool UpgradeAttackPower()  // 攻击力等级提升1
```

#### 设置等级

```csharp
public void SetAttackPowerLevel(int level)
```

#### 获取等级

```csharp
public int AttackPowerLevel { get; }
```

### Farmer（信徒）

#### 初始化（扩展版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initSacrificeLevel)
```

#### 升级

```csharp
public bool UpgradeSacrifice()  // 献祭等级提升1
```

#### 设置等级

```csharp
public void SetSacrificeLevel(int level)
```

#### 获取等级

```csharp
public int SacrificeLevel { get; }
```

### Pope（教皇）

#### 初始化（扩展版）

```csharp
public void Initialize(PieceDataSO data, int playerID,
    int initHPLevel, int initAPLevel,
    int initSwapCooldownLevel, int initBuffLevel)
```

#### 升级

```csharp
public bool UpgradeSwapCooldown()  // 位置交换CD等级提升1
public bool UpgradeBuff()          // Buff等级提升1
```

#### 设置等级

```csharp
public void SetSwapCooldownLevel(int level)
public void SetBuffLevel(int level)
```

#### 获取等级

```csharp
public int SwapCooldownLevel { get; }
public int BuffLevel { get; }
```

### Building（建筑）

#### 初始化

```csharp
public void Initialize(BuildingDataSO data, int ownerPlayerID,
    int initHPLevel = 0, int initAttackRangeLevel = 0, int initSlotsLevel = 0)
```

#### 升级

```csharp
public bool UpgradeHP()           // HP提升1等级
public bool UpgradeAttackRange()  // 攻击范围提升1等级
public bool UpgradeSlots()        // 插槽数量提升1等级
```

#### 设置等级

```csharp
public void SetHPLevel(int level)
public void SetAttackRangeLevel(int level)
public void SetSlotsLevel(int level)
```

#### 获取等级

```csharp
public int HPLevel { get; }
public int AttackRangeLevel { get; }
public int SlotsLevel { get; }
```

---

## 实现示例

### 示例1: 玩家升级研究系统

```csharp
public class UpgradeResearchSystem
{
    private Dictionary<int, PlayerUpgradeData> playerUpgrades = new();

    // 研究升级
    public void ResearchUpgrade(int playerID, PieceType pieceType, string upgradeType)
    {
        var data = playerUpgrades[playerID];

        // 提升等级
        switch (pieceType)
        {
            case PieceType.Missionary:
                if (upgradeType == "Occupy")
                    data.missionaryOccupyLevel++;
                break;
            // 其他同理...
        }

        // 应用到现有的所有对应棋子
        ApplyToExistingPieces(playerID, pieceType, upgradeType);
    }

    // 应用到现有棋子
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

    // 生成新棋子时
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

        // 其他职业同理...
    }
}
```

### 示例2: 存档/读档系统

```csharp
[System.Serializable]
public class SaveData
{
    // 每个玩家的升级等级
    public Dictionary<int, PlayerUpgradeData> playerUpgrades;

    // 单个棋子的等级（特殊情况用）
    public List<PieceSaveData> pieces;
}

[System.Serializable]
public class PieceSaveData
{
    public int pieceID;
    public int hpLevel;
    public int apLevel;
    public int occupyLevel;  // 传教士情况下
    // ...
}

public class SaveSystem
{
    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 保存玩家升级数据
        data.playerUpgrades = GetAllPlayerUpgrades();

        // 保存单个棋子数据（如需要）
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

        // 保存到文件
        SaveToFile(data);
    }

    public void LoadGame()
    {
        SaveData data = LoadFromFile();

        // 恢复玩家升级数据
        RestorePlayerUpgrades(data.playerUpgrades);

        // 生成·恢复棋子
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

### 示例3: 网络同步

```csharp
public class NetworkUpgradeSystem
{
    // 同步升级研究
    public void SyncUpgradeResearch(int playerID, PieceType pieceType, string upgradeType)
    {
        // 发送到服务器
        SendToServer(new UpgradeMessage
        {
            playerID = playerID,
            pieceType = pieceType,
            upgradeType = upgradeType
        });
    }

    // 从服务器接收
    public void OnUpgradeMessageReceived(UpgradeMessage msg)
    {
        // 所有客户端执行相同处理
        var upgradeSystem = GetUpgradeSystem();
        upgradeSystem.ResearchUpgrade(msg.playerID, msg.pieceType, msg.upgradeType);
    }

    // 同步现有棋子等级
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

## 总结

### 新系统的优点

1. **灵活性**: 可独立升级各项目
2. **扩展性**: 易于添加新的等级类型
3. **支持外部管理**: 可从玩家数据批量管理
4. **支持现有对象**: Setter可更改现有等级

### 使用场景指南

| 用途 | 推荐方法 |
|------|---------|
| 新建生成 | 用`Initialize()`指定等级 |
| 逐步升级 | 逐次调用`UpgradeXX()` |
| 批量等级变更 | 用`SetXXLevel()`直接设置 |
| 等级确认 | 获取`XXLevel`属性 |

---

**文档结束**
