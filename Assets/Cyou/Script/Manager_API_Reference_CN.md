# Manager 系统 API 参考

本文档说明如何使用 PieceManager 和 BuildingManager 进行基于 ID 的单位・建筑管理系统。

---

## 目录

1. [概述](#1-概述)
2. [PieceManager - 棋子管理](#2-piecemanager---棋子管理)
   - [棋子生成](#棋子生成)
   - [升级管理](#升级管理)
   - [信息获取](#信息获取)
   - [棋子行动](#棋子行动)
   - [AP管理](#ap管理)
   - [回合处理](#回合处理)
3. [BuildingManager - 建筑管理](#3-buildingmanager---建筑管理)
   - [建筑生成](#建筑生成)
   - [建造处理](#建造处理)
   - [信徒配置](#信徒配置)
   - [建筑升级](#建筑升级)
   - [建筑信息获取](#建筑信息获取)
   - [建筑伤害处理](#建筑伤害处理)
   - [建筑回合处理](#建筑回合处理)

---

## 1. 概述

### 设计思想

PieceManager 和 BuildingManager 是对具体单位类（Farmer, Military, Missionary, Pope）和建筑类进行**基于 ID 管理**的包装层。

**优点:**
- GameManager 无需关注具体类型（Farmer, Military 等）即可操作
- 仅使用整数 ID 管理所有棋子・建筑
- 保持类型安全的同时，简化接口

**架构:**
```
GameManager → PieceManager (ID管理层) → Piece/Farmer/Military/etc (实现层)
GameManager → BuildingManager (ID管理层) → Building (实现层)
```

### 实现文件
- `Assets/Cyou/Script/Manager/PieceManager.cs`
- `Assets/Cyou/Script/Manager/BuildingManager.cs`

---

## 2. PieceManager - 棋子管理

### 棋子生成

#### `CreatePiece()`
生成棋子并返回 ID。

**函数签名:**
```csharp
public int CreatePiece(PieceType pieceType, Religion religion, int playerID, Vector3 position)
```

**参数:**
- `pieceType`: 棋子种类（PieceType.Farmer, Military, Missionary, Pope）
- `religion`: 宗教（Religion.Maya, RedMoon, MadScientist, Silk）
- `playerID`: 玩家 ID
- `position`: 生成位置

**返回值:**
- 成功: 生成的棋子 ID（正整数）
- 失败: -1

**使用示例:**
```csharp
PieceManager pieceManager = // 获取 PieceManager 引用

// 生成农民
int farmerID = pieceManager.CreatePiece(
    PieceType.Farmer,
    Religion.Maya,
    playerID: 1,
    new Vector3(10, 0, 10)
);

if (farmerID >= 0)
{
    Debug.Log($"农民生成成功: ID={farmerID}");
}
```

**实现位置:** `PieceManager.cs:36-74`

---

### 升级管理

#### `UpgradePiece()`
升级棋子的通用项目（HP/AP）。

**函数签名:**
```csharp
public bool UpgradePiece(int pieceID, PieceUpgradeType upgradeType)
```

**参数:**
- `pieceID`: 棋子 ID
- `upgradeType`: 升级项目（PieceUpgradeType.HP 或 AP）

**返回值:**
- `true`: 升级成功
- `false`: 失败（棋子不存在、已达最大等级、资源不足等）

**使用示例:**
```csharp
// 升级 HP
if (pieceManager.UpgradePiece(farmerID, PieceUpgradeType.HP))
{
    Debug.Log("HP升级成功");
}

// 升级 AP
if (pieceManager.UpgradePiece(farmerID, PieceUpgradeType.AP))
{
    Debug.Log("AP升级成功");
}
```

**实现位置:** `PieceManager.cs:86-104`

---

#### `UpgradePieceSpecial()`
升级棋子的职业专属项目。

**函数签名:**
```csharp
public bool UpgradePieceSpecial(int pieceID, SpecialUpgradeType specialUpgradeType)
```

**参数:**
- `pieceID`: 棋子 ID
- `specialUpgradeType`: 职业专属升级项目
  - `SpecialUpgradeType.FarmerSacrifice` - 农民的献祭回复量
  - `SpecialUpgradeType.MilitaryAttackPower` - 军队的攻击力
  - `SpecialUpgradeType.MissionaryOccupy` - 宣教师的占领成功率
  - `SpecialUpgradeType.MissionaryConvertEnemy` - 宣教师的魅惑成功率
  - `SpecialUpgradeType.PopeSwapCooldown` - 教皇的位置交换冷却
  - `SpecialUpgradeType.PopeBuff` - 教皇的增益效果

**返回值:**
- `true`: 升级成功
- `false`: 失败

**使用示例:**
```csharp
// 升级农民的献祭回复量
if (pieceManager.UpgradePieceSpecial(farmerID, SpecialUpgradeType.FarmerSacrifice))
{
    Debug.Log("农民献祭回复量升级成功");
}

// 升级军队的攻击力
if (pieceManager.UpgradePieceSpecial(militaryID, SpecialUpgradeType.MilitaryAttackPower))
{
    Debug.Log("军队攻击力升级成功");
}
```

**实现位置:** `PieceManager.cs:112-150`

---

#### `GetUpgradeCost()`
获取升级费用。

**函数签名:**
```csharp
public int GetUpgradeCost(int pieceID, PieceUpgradeType upgradeType)
```

**参数:**
- `pieceID`: 棋子 ID
- `upgradeType`: 升级项目

**返回值:**
- 成功: 费用（正整数）
- 失败: -1（棋子不存在或无法升级）

**使用示例:**
```csharp
int hpCost = pieceManager.GetUpgradeCost(farmerID, PieceUpgradeType.HP);
if (hpCost > 0)
{
    Debug.Log($"HP升级需要{hpCost}资源");
}
```

**实现位置:** `PieceManager.cs:158-167`

---

#### `CanUpgrade()`
检查是否可以升级。

**函数签名:**
```csharp
public bool CanUpgrade(int pieceID, PieceUpgradeType upgradeType)
```

**参数:**
- `pieceID`: 棋子 ID
- `upgradeType`: 升级项目

**返回值:**
- `true`: 可以升级
- `false`: 无法升级

**使用示例:**
```csharp
if (pieceManager.CanUpgrade(farmerID, PieceUpgradeType.HP))
{
    // 启用 HP 升级按钮
    hpUpgradeButton.interactable = true;
}
```

**实现位置:** `PieceManager.cs:175-183`

---

### 信息获取

#### `GetPieceHP()`
获取棋子当前 HP。

**函数签名:**
```csharp
public float GetPieceHP(int pieceID)
```

**实现位置:** `PieceManager.cs:192-200`

---

#### `GetPieceAP()`
获取棋子当前 AP。

**函数签名:**
```csharp
public float GetPieceAP(int pieceID)
```

**实现位置:** `PieceManager.cs:205-213`

---

#### `GetPiecePlayerID()`
获取棋子所属玩家 ID。

**函数签名:**
```csharp
public int GetPiecePlayerID(int pieceID)
```

**实现位置:** `PieceManager.cs:218-226`

---

#### `GetPieceType()`
获取棋子种类。

**函数签名:**
```csharp
public PieceType GetPieceType(int pieceID)
```

**返回值:**
- PieceType.Farmer, Military, Missionary, Pope, 或 None

**实现位置:** `PieceManager.cs:231-247`

---

#### `DoesPieceExist()`
检查棋子是否存在。

**函数签名:**
```csharp
public bool DoesPieceExist(int pieceID)
```

**实现位置:** `PieceManager.cs:252-255`

---

#### `GetPlayerPieces()`
获取指定玩家的所有棋子 ID。

**函数签名:**
```csharp
public List<int> GetPlayerPieces(int playerID)
```

**返回值:**
- 棋子 ID 列表

**使用示例:**
```csharp
List<int> player1Pieces = pieceManager.GetPlayerPieces(1);
Debug.Log($"玩家1的棋子数: {player1Pieces.Count}");

foreach (int pieceID in player1Pieces)
{
    float hp = pieceManager.GetPieceHP(pieceID);
    Debug.Log($"棋子ID={pieceID}, HP={hp}");
}
```

**实现位置:** `PieceManager.cs:260-266`

---

#### `GetPlayerPiecesByType()`
获取指定玩家的指定种类棋子 ID。

**函数签名:**
```csharp
public List<int> GetPlayerPiecesByType(int playerID, PieceType pieceType)
```

**使用示例:**
```csharp
// 获取玩家1的所有农民
List<int> farmers = pieceManager.GetPlayerPiecesByType(1, PieceType.Farmer);
Debug.Log($"玩家1的农民数: {farmers.Count}");
```

**实现位置:** `PieceManager.cs:271-276`

---

### 棋子行动

#### `AttackEnemy()`
军队攻击敌人。

**函数签名:**
```csharp
public bool AttackEnemy(int attackerID, int targetID)
```

**参数:**
- `attackerID`: 攻击者棋子 ID（必须是军队）
- `targetID`: 目标棋子 ID

**返回值:**
- `true`: 攻击成功
- `false`: 失败（棋子不存在、攻击者不是军队等）

**使用示例:**
```csharp
if (pieceManager.AttackEnemy(militaryID, enemyID))
{
    Debug.Log("攻击成功");
}
```

**实现位置:** `PieceManager.cs:331-352`

---

#### `ConvertEnemy()`
宣教师魅惑敌人。

**函数签名:**
```csharp
public bool ConvertEnemy(int missionaryID, int targetID)
```

**参数:**
- `missionaryID`: 宣教师棋子 ID
- `targetID`: 目标棋子 ID

**返回值:**
- `true`: 魅惑尝试成功（成功率判定在内部进行）
- `false`: 失败

**使用示例:**
```csharp
if (pieceManager.ConvertEnemy(missionaryID, enemyID))
{
    Debug.Log("尝试魅惑");
}
```

**实现位置:** `PieceManager.cs:360-381`

---

#### `OccupyTerritory()`
宣教师占领领地。

**函数签名:**
```csharp
public bool OccupyTerritory(int missionaryID, Vector3 targetPosition)
```

**参数:**
- `missionaryID`: 宣教师棋子 ID
- `targetPosition`: 占领目标领地坐标

**返回值:**
- `true`: 占领尝试成功
- `false`: 失败

**实现位置:** `PieceManager.cs:389-404`

---

#### `SacrificeToPiece()`
农民回复其他棋子（献祭）。

**函数签名:**
```csharp
public bool SacrificeToPiece(int farmerID, int targetID)
```

**参数:**
- `farmerID`: 农民棋子 ID
- `targetID`: 回复目标棋子 ID

**返回值:**
- `true`: 回复成功
- `false`: 失败

**实现位置:** `PieceManager.cs:412-433`

---

#### `SwapPositions()`
教皇与友方棋子交换位置。

**函数签名:**
```csharp
public bool SwapPositions(int popeID, int targetID)
```

**参数:**
- `popeID`: 教皇棋子 ID
- `targetID`: 交换目标棋子 ID

**返回值:**
- `true`: 交换成功
- `false`: 失败

**实现位置:** `PieceManager.cs:441-462`

---

#### `DamagePiece()`
对棋子造成伤害。

**函数签名:**
```csharp
public void DamagePiece(int pieceID, float damage, int attackerID = -1)
```

**参数:**
- `pieceID`: 棋子 ID
- `damage`: 伤害量
- `attackerID`: 攻击者 ID（可选）

**实现位置:** `PieceManager.cs:470-487`

---

#### `HealPiece()`
回复棋子。

**函数签名:**
```csharp
public void HealPiece(int pieceID, float amount)
```

**参数:**
- `pieceID`: 棋子 ID
- `amount`: 回复量

**实现位置:** `PieceManager.cs:494-503`

---

### AP管理

#### `ConsumePieceAP()`
消耗棋子 AP。

**函数签名:**
```csharp
public bool ConsumePieceAP(int pieceID, float amount)
```

**参数:**
- `pieceID`: 棋子 ID
- `amount`: 消耗量

**返回值:**
- `true`: 消耗成功
- `false`: 失败（AP 不足等）

**使用示例:**
```csharp
if (pieceManager.ConsumePieceAP(farmerID, 5.0f))
{
    Debug.Log("消耗了5AP");
}
else
{
    Debug.Log("AP不足");
}
```

**实现位置:** `PieceManager.cs:515-524`

---

#### `RecoverPieceAP()`
回复棋子 AP。

**函数签名:**
```csharp
public void RecoverPieceAP(int pieceID, float amount)
```

**参数:**
- `pieceID`: 棋子 ID
- `amount`: 回复量

**使用示例:**
```csharp
pieceManager.RecoverPieceAP(farmerID, 10.0f);
```

**实现位置:** `PieceManager.cs:531-540`

---

### 回合处理

#### `ProcessTurnStart()`
执行指定玩家的回合开始处理。

**函数签名:**
```csharp
public void ProcessTurnStart(int playerID)
```

**参数:**
- `playerID`: 玩家 ID

**使用示例:**
```csharp
// 回合开始时调用
pieceManager.ProcessTurnStart(currentPlayerID);
```

**实现位置:** `PieceManager.cs:577-590`

---

## 3. BuildingManager - 建筑管理

### 建筑生成

#### `CreateBuilding()`
生成建筑并返回 ID。

**函数签名:**
```csharp
public int CreateBuilding(BuildingDataSO buildingData, int playerID, Vector3 position)
```

**参数:**
- `buildingData`: 建筑数据 SO
- `playerID`: 玩家 ID
- `position`: 生成位置

**返回值:**
- 成功: 生成的建筑 ID（正整数）
- 失败: -1

**使用示例:**
```csharp
BuildingManager buildingManager = // 获取 BuildingManager 引用
BuildingDataSO buildingData = // 获取建筑数据

int buildingID = buildingManager.CreateBuilding(buildingData, playerID: 1, new Vector3(10, 0, 10));

if (buildingID >= 0)
{
    Debug.Log($"建筑生成成功: ID={buildingID}");
}
```

**实现位置:** `BuildingManager.cs:40-77`

---

#### `CreateBuildingByName()`
通过建筑名称生成建筑（便捷方法）。

**函数签名:**
```csharp
public int CreateBuildingByName(string buildingName, int playerID, Vector3 position)
```

**参数:**
- `buildingName`: 建筑名称
- `playerID`: 玩家 ID
- `position`: 生成位置

**返回值:**
- 成功: 生成的建筑 ID
- 失败: -1

**使用示例:**
```csharp
int buildingID = buildingManager.CreateBuildingByName("祭坛", 1, new Vector3(10, 0, 10));
```

**实现位置:** `BuildingManager.cs:85-97`

---

### 建造处理

#### `AddFarmerToConstruction()`
推进建筑建造（投入农民）。

**函数签名:**
```csharp
public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
```

**参数:**
- `buildingID`: 建筑 ID
- `farmerID`: 投入的农民棋子 ID
- `pieceManager`: PieceManager 引用

**返回值:**
- `true`: 建造推进成功
- `false`: 失败

**使用示例:**
```csharp
if (buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager))
{
    Debug.Log("建造推进中");
}
```

**处理流程:**
1. 确认农民的 AP
2. 确认建筑的剩余建造成本
3. 消耗农民的 AP
4. 推进建筑建造
5. 完成时输出日志

**实现位置:** `BuildingManager.cs:106-158`

---

#### `CancelConstruction()`
取消建造。

**函数签名:**
```csharp
public bool CancelConstruction(int buildingID)
```

**参数:**
- `buildingID`: 建筑 ID

**返回值:**
- `true`: 取消成功
- `false`: 失败

**注意:** 消耗的农民和行动力不会返还。

**实现位置:** `BuildingManager.cs:165-175`

---

### 信徒配置

#### `EnterBuilding()`
将农民配置到建筑中。

**函数签名:**
```csharp
public bool EnterBuilding(int buildingID, int farmerID, PieceManager pieceManager)
```

**参数:**
- `buildingID`: 建筑 ID
- `farmerID`: 农民棋子 ID
- `pieceManager`: PieceManager 引用

**返回值:**
- `true`: 配置成功
- `false`: 失败（槽位已满、建筑未完成等）

**使用示例:**
```csharp
if (buildingManager.EnterBuilding(buildingID, farmerID, pieceManager))
{
    Debug.Log("农民已配置到建筑");
}
else
{
    Debug.Log("配置失败（槽位已满或建筑未完成）");
}
```

**实现位置:** `BuildingManager.cs:182-230`

---

### 建筑升级

#### `UpgradeBuilding()`
升级建筑项目。

**函数签名:**
```csharp
public bool UpgradeBuilding(int buildingID, BuildingUpgradeType upgradeType)
```

**参数:**
- `buildingID`: 建筑 ID
- `upgradeType`: 升级项目
  - `BuildingUpgradeType.HP` - 最大 HP
  - `BuildingUpgradeType.AttackRange` - 攻击范围
  - `BuildingUpgradeType.Slots` - 槽位数
  - `BuildingUpgradeType.BuildCost` - 建造成本

**返回值:**
- `true`: 升级成功
- `false`: 失败

**使用示例:**
```csharp
// 升级 HP
if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP))
{
    Debug.Log("建筑 HP 升级成功");
}

// 升级槽位数
if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.Slots))
{
    Debug.Log("建筑槽位数升级成功");
}
```

**实现位置:** `BuildingManager.cs:240-258`

---

#### `GetUpgradeCost()` (Building)
获取建筑升级费用。

**函数签名:**
```csharp
public int GetUpgradeCost(int buildingID, BuildingUpgradeType upgradeType)
```

**实现位置:** `BuildingManager.cs:266-276`

---

#### `CanUpgrade()` (Building)
检查建筑是否可以升级。

**函数签名:**
```csharp
public bool CanUpgrade(int buildingID, BuildingUpgradeType upgradeType)
```

**实现位置:** `BuildingManager.cs:284-293`

---

### 建筑信息获取

#### `GetBuildingHP()`
获取建筑当前 HP。

**函数签名:**
```csharp
public int GetBuildingHP(int buildingID)
```

**实现位置:** `BuildingManager.cs:301-309`

---

#### `GetBuildingState()`
获取建筑状态。

**函数签名:**
```csharp
public BuildingState GetBuildingState(int buildingID)
```

**返回值:**
- `BuildingState.UnderConstruction` - 建造中
- `BuildingState.Active` - 运行中
- `BuildingState.Inactive` - 未运行
- `BuildingState.Ruined` - 废墟

**实现位置:** `BuildingManager.cs:314-322`

---

#### `GetBuildProgress()`
获取建造进度（0.0～1.0）。

**函数签名:**
```csharp
public float GetBuildProgress(int buildingID)
```

**返回值:**
- 0.0～1.0 范围（0.0=未开始，1.0=完成）

**实现位置:** `BuildingManager.cs:327-335`

---

#### `DoesBuildingExist()`
检查建筑是否存在。

**函数签名:**
```csharp
public bool DoesBuildingExist(int buildingID)
```

**实现位置:** `BuildingManager.cs:340-343`

---

#### `GetAllBuildingIDs()`
获取所有建筑 ID。

**函数签名:**
```csharp
public List<int> GetAllBuildingIDs()
```

**实现位置:** `BuildingManager.cs:350-353`

---

#### `GetOperationalBuildings()`
获取运行中的建筑 ID 列表。

**函数签名:**
```csharp
public List<int> GetOperationalBuildings()
```

**实现位置:** `BuildingManager.cs:358-364`

---

#### `GetBuildingsUnderConstruction()`
获取建造中的建筑 ID 列表。

**函数签名:**
```csharp
public List<int> GetBuildingsUnderConstruction()
```

**实现位置:** `BuildingManager.cs:369-375`

---

### 建筑伤害处理

#### `DamageBuilding()`
对建筑造成伤害。

**函数签名:**
```csharp
public bool DamageBuilding(int buildingID, int damage)
```

**参数:**
- `buildingID`: 建筑 ID
- `damage`: 伤害量

**返回值:**
- `true`: 造成伤害成功
- `false`: 失败

**实现位置:** `BuildingManager.cs:413-423`

---

#### `RemoveBuilding()`
强制删除建筑。

**函数签名:**
```csharp
public bool RemoveBuilding(int buildingID)
```

**实现位置:** `BuildingManager.cs:403-418`

---

### 建筑回合处理

#### `ProcessTurnStart()` (Building)
执行所有建筑的回合处理（资源生成等）。

**函数签名:**
```csharp
public void ProcessTurnStart(int currentTurn)
```

**参数:**
- `currentTurn`: 当前回合数

**使用示例:**
```csharp
// 回合开始时调用
buildingManager.ProcessTurnStart(currentTurn);
```

**实现位置:** `BuildingManager.cs:431-443`

---

## 使用示例: 完整的游戏流程

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BuildingManager buildingManager;

    private int currentPlayerID = 1;
    private int currentTurn = 0;

    void Start()
    {
        // 生成农民
        int farmerID = pieceManager.CreatePiece(
            PieceType.Farmer,
            Religion.Maya,
            currentPlayerID,
            new Vector3(0, 0, 0)
        );

        // 生成军队
        int militaryID = pieceManager.CreatePiece(
            PieceType.Military,
            Religion.Maya,
            currentPlayerID,
            new Vector3(5, 0, 0)
        );

        // 生成建筑
        int buildingID = buildingManager.CreateBuildingByName(
            "祭坛",
            currentPlayerID,
            new Vector3(10, 0, 10)
        );

        // 将农民投入建造
        buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager);

        // 建筑升级
        if (buildingManager.CanUpgrade(buildingID, BuildingUpgradeType.HP))
        {
            buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP);
        }

        // 棋子升级
        if (pieceManager.CanUpgrade(militaryID, PieceUpgradeType.HP))
        {
            pieceManager.UpgradePiece(militaryID, PieceUpgradeType.HP);
        }
    }

    void OnTurnStart()
    {
        currentTurn++;

        // 回合处理
        pieceManager.ProcessTurnStart(currentPlayerID);
        buildingManager.ProcessTurnStart(currentTurn);

        // 回复各棋子的 AP
        List<int> playerPieces = pieceManager.GetPlayerPieces(currentPlayerID);
        foreach (int pieceID in playerPieces)
        {
            pieceManager.RecoverPieceAP(pieceID, 5.0f);
        }
    }

    void OnAttackButtonClick(int attackerID, int targetID)
    {
        // 执行攻击
        if (pieceManager.AttackEnemy(attackerID, targetID))
        {
            Debug.Log("攻击成功");
        }
    }
}
```

---

## 枚举类型（Enums）

### PieceType
```csharp
public enum PieceType
{
    None,
    Farmer,      // 农民
    Military,    // 军队
    Missionary,  // 宣教师
    Pope         // 教皇
}
```

### Religion
```csharp
public enum Religion
{
    None,
    SilkReligion,           // 丝织教
    RedMoonReligion,        // 红月教
    MayaReligion,           // 玛雅外星人文明教
    MadScientistReligion    // 疯狂科学家教
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
    FarmerSacrifice,           // 农民: 献祭回复量
    MilitaryAttackPower,       // 军队: 攻击力
    MissionaryOccupy,          // 宣教师: 占领成功率
    MissionaryConvertEnemy,    // 宣教师: 魅惑成功率
    PopeSwapCooldown,          // 教皇: 位置交换冷却
    PopeBuff                   // 教皇: 增益效果
}
```

### BuildingUpgradeType
```csharp
public enum BuildingUpgradeType
{
    HP,             // 最大 HP
    AttackRange,    // 攻击范围
    Slots,          // 槽位数
    BuildCost       // 建造成本
}
```

### BuildingState
```csharp
public enum BuildingState
{
    UnderConstruction,  // 建造中
    Active,            // 运行中
    Inactive,          // 未运行
    Ruined            // 废墟
}
```

---

## 总结

使用 PieceManager 和 BuildingManager，GameManager 可以：
- ✅ 无需关注具体单位类（Farmer, Military 等）即可操作
- ✅ 仅使用整数 ID 管理所有棋子・建筑
- ✅ 保持类型安全的同时使用简单接口
- ✅ 提升代码的可维护性・可扩展性

所有操作都基于 ID 统一管理，使 GameManager 的代码简洁易读。
