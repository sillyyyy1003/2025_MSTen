# Manager 系统 API 参考

本文档说明如何使用 PieceManager 和 BuildingManager 进行基于 ID 的单位・建筑管理系统。

---

## 目录

1. [概述](#1-概述)
2. [PieceManager - 棋子管理](#2-piecemanager---棋子管理)
   - [初始化](#初始化)
   - [棋子生成](#棋子生成)
   - [网络同步](#网络同步)
   - [棋子删除](#棋子删除)
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
4. [网络同步流程示例](#网络同步流程示例)
5. [数据结构](#数据结构)
6. [枚举类型](#枚举类型enums)
7. [总结](#总结)

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

### 初始化

#### `SetLocalPlayerID()`
设置本地玩家 ID（用于区分己方棋子和敌方棋子）。

**函数签名:**
```csharp
public void SetLocalPlayerID(int playerID)
```

**参数:**
- `playerID`: 本地玩家的 ID

**使用示例:**
```csharp
PieceManager pieceManager = // 获取 PieceManager 引用

// 设置本地玩家 ID
pieceManager.SetLocalPlayerID(1);
```

**注意:**
- 必须在创建棋子前调用此方法
- 用于网络同步中区分己方棋子（pieces）和敌方棋子（enemyPieces）

**实现位置:** `PieceManager.cs:20-24`

---

#### `GetLocalPlayerID()`
获取本地玩家 ID。

**函数签名:**
```csharp
public int GetLocalPlayerID()
```

**返回值:**
- 本地玩家 ID（未设置时返回 -1）

**实现位置:** `PieceManager.cs:26-29`

---

### 棋子生成

#### `CreatePiece()`
生成己方棋子并返回 ID。

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

**注意:**
- 此方法生成的棋子会被添加到 `pieces` 字典（己方棋子）
- 如需生成敌方棋子，请使用 `CreateEnemyPiece()`

**实现位置:** `PieceManager.cs:36-74`

---

#### `CreateEnemyPiece()`
根据同步数据生成敌方棋子。

**函数签名:**
```csharp
public bool CreateEnemyPiece(syncPieceData spd)
```

**参数:**
- `spd`: 棋子同步数据（包含所有状态信息）

**返回值:**
- `true`: 生成成功
- `false`: 失败

**使用示例:**
```csharp
// 接收到敌方棋子数据时
syncPieceData enemyData = new syncPieceData
{
    pieceID = 101,
    pieceType = PieceType.Military,
    religion = Religion.RedMoon,
    playerID = 2,
    position = new Vector3(20, 0, 20),
    currentHP = 80,
    currentHPLevel = 1,
    currentAPLevel = 1,
    attackPowerLevel = 2
};

if (pieceManager.CreateEnemyPiece(enemyData))
{
    Debug.Log("敌方棋子生成成功");
}
```

**注意:**
- 此方法会自动设置棋子的所有状态（HP、AP、等级等）
- 生成的棋子会被添加到 `enemyPieces` 字典
- 用于网络游戏中接收并创建对方的棋子

**实现位置:** `PieceManager.cs:76-183`

---

### 网络同步

#### `SyncEnemyPieceState()`
同步敌方棋子的状态。

**函数签名:**
```csharp
public bool SyncEnemyPieceState(syncPieceData spd)
```

**参数:**
- `spd`: 棋子同步数据

**返回值:**
- `true`: 同步成功
- `false`: 失败（棋子不存在）

**使用示例:**
```csharp
// 接收到棋子状态更新时
syncPieceData updateData = new syncPieceData
{
    pieceID = 101,
    currentHP = 60,  // HP 发生变化
    currentHPLevel = 2,  // HP 等级提升
    position = new Vector3(25, 0, 25)  // 位置变化
};

if (pieceManager.SyncEnemyPieceState(updateData))
{
    Debug.Log("敌方棋子状态同步成功");
}
```

**注意:**
- 仅更新已存在的棋子状态
- 如果棋子不存在，应先调用 `CreateEnemyPiece()`
- 会自动根据 localPlayerID 判断是己方还是敌方棋子

**实现位置:** `PieceManager.cs:191-269`

---

#### `CreateCompleteSyncData()`
创建包含完整状态的同步数据。

**函数签名:**
```csharp
public syncPieceData CreateCompleteSyncData(int pieceID, Religion religion = Religion.None)
```

**参数:**
- `pieceID`: 棋子 ID
- `religion`: 宗教（如果无法从 PieceDataSO 获取则使用此参数）

**返回值:**
- 包含完整状态的 `syncPieceData`
- 失败时返回 null

**使用示例:**
```csharp
// 发送己方棋子的完整状态给对方
syncPieceData myPieceData = pieceManager.CreateCompleteSyncData(farmerID, Religion.Maya);
if (myPieceData != null)
{
    // 通过网络发送 myPieceData
    NetworkManager.Send(myPieceData);
}
```

**包含的信息:**
- 基本信息：pieceID, pieceType, religion, playerID
- 位置和状态：position, currentPID, currentHP
- 等级信息：currentHPLevel, currentAPLevel
- 职业特定等级：攻击力、献祭、占领、魅惑、位置交换CD、增益等

**实现位置:** `PieceManager.cs:277-361`

---

### 棋子删除

#### `HandleEnemyPieceDeath()`
处理接收到的敌方棋子死亡通知。

**函数签名:**
```csharp
public bool HandleEnemyPieceDeath(syncPieceData spd)
```

**参数:**
- `spd`: 死亡棋子的同步数据（currentHP = 0）

**返回值:**
- `true`: 删除成功
- `false`: 失败（棋子不存在）

**使用示例:**
```csharp
// 接收到敌方棋子死亡通知
syncPieceData deathData = new syncPieceData
{
    pieceID = 101,
    currentHP = 0
};

if (pieceManager.HandleEnemyPieceDeath(deathData))
{
    Debug.Log("敌方棋子已删除");
}
```

**注意:**
- 此方法仅删除棋子，不会触发 `OnPieceDied` 事件
- 用于接收端处理死亡通知，避免无限循环

**实现位置:** `PieceManager.cs:598-612`

---

#### `GetLastDeadPieceData()`
获取最后一个死亡棋子的同步数据（发送端使用）。

**函数签名:**
```csharp
public syncPieceData? GetLastDeadPieceData()
```

**返回值:**
- 最后一个死亡棋子的同步数据
- 如果没有缓存数据则返回 null

**使用示例:**
```csharp
// 在 GameManager 中订阅死亡事件
pieceManager.OnPieceDied += (pieceID) =>
{
    // 获取死亡棋子的完整数据
    syncPieceData? deathData = pieceManager.GetLastDeadPieceData();
    if (deathData.HasValue)
    {
        // 发送给对方玩家
        NetworkManager.SendPieceDeath(deathData.Value);
    }
};
```

**注意:**
- 数据获取后会被清空（单次使用）
- 必须在 `OnPieceDied` 事件触发后立即调用
- 用于发送端获取死亡数据并通知对方

**实现位置:** `PieceManager.cs:620-627`

---

#### `OnPieceDied` 事件
棋子死亡时触发的事件。

**事件签名:**
```csharp
public event Action<int> OnPieceDied;
```

**参数:**
- `int`: 死亡棋子的 ID

**使用示例:**
```csharp
// 订阅棋子死亡事件
pieceManager.OnPieceDied += HandlePieceDeath;

void HandlePieceDeath(int deadPieceID)
{
    Debug.Log($"棋子 {deadPieceID} 已死亡");

    // 获取死亡数据并发送给对方
    syncPieceData? deathData = pieceManager.GetLastDeadPieceData();
    if (deathData.HasValue)
    {
        NetworkManager.SendPieceDeath(deathData.Value);
    }
}
```

**实现位置:** `PieceManager.cs:13`

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

### 初始化

#### `SetLocalPlayerID()`
设置本地玩家 ID（用于区分己方建筑和敌方建筑）。

**函数签名:**
```csharp
public void SetLocalPlayerID(int playerID)
```

**参数:**
- `playerID`: 本地玩家的 ID

**使用示例:**
```csharp
buildingManager.SetLocalPlayerID(1);
```

**注意:**
- 必须在生成建筑前调用此方法
- 用于网络同步中区分己方建筑（buildings）和敌方建筑（enemyBuildings）

**实现位置:** `BuildingManager.cs:41-45`

---

#### `GetLocalPlayerID()`
获取本地玩家 ID。

**函数签名:**
```csharp
public int GetLocalPlayerID()
```

**返回值:**
- 本地玩家 ID（未设置时返回 -1）

**实现位置:** `BuildingManager.cs:50-53`

---

### 建筑生成

#### `CreateBuilding()`
生成己方建筑并返回 ID。

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

**注意:**
- 此方法生成的建筑会被添加到 `buildings` 字典（己方建筑）
- 如需生成敌方建筑，请使用 `CreateEnemyBuilding()`

**实现位置:** `BuildingManager.cs:62-102`

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

**实现位置:** `BuildingManager.cs:113-124`

---

#### `CreateEnemyBuilding()`
根据同步数据生成敌方建筑。

**函数签名:**
```csharp
public bool CreateEnemyBuilding(syncBuildingData sbd)
```

**参数:**
- `sbd`: 建筑同步数据（包含所有状态信息）

**返回值:**
- `true`: 生成成功
- `false`: 失败

**使用示例:**
```csharp
// 接收到敌方建筑数据时
syncBuildingData enemyBuildingData = new syncBuildingData
{
    buildingID = 201,
    buildingName = "祭坛",
    playerID = 2,
    position = new Vector3(50, 0, 50),
    currentHP = 100,
    state = BuildingState.Active,
    hpLevel = 1,
    slotsLevel = 1
};

if (buildingManager.CreateEnemyBuilding(enemyBuildingData))
{
    Debug.Log("敌方建筑生成成功");
}
```

**注意:**
- 此方法会自动设置建筑的所有状态（HP、等级、建造进度等）
- 生成的建筑会被添加到 `enemyBuildings` 字典
- 用于网络游戏中接收并创建对方的建筑

**实现位置:** `BuildingManager.cs:131-201`

---

### 网络同步

#### `SyncEnemyBuildingState()`
同步敌方建筑的状态。

**函数签名:**
```csharp
public bool SyncEnemyBuildingState(syncBuildingData sbd)
```

**参数:**
- `sbd`: 建筑同步数据

**返回值:**
- `true`: 同步成功
- `false`: 失败（建筑不存在）

**使用示例:**
```csharp
// 接收到建筑状态更新时
syncBuildingData updateData = new syncBuildingData
{
    buildingID = 201,
    currentHP = 80,  // HP 发生变化
    hpLevel = 2,     // HP 等级提升
    state = BuildingState.Active
};

if (buildingManager.SyncEnemyBuildingState(updateData))
{
    Debug.Log("敌方建筑状态同步成功");
}
```

**注意:**
- 仅更新已存在的建筑状态
- 如果建筑不存在，应先调用 `CreateEnemyBuilding()`
- 会自动根据 localPlayerID 判断是己方还是敌方建筑

**实现位置:** `BuildingManager.cs:354-413`

---

#### `CreateCompleteSyncData()`
创建包含完整状态的同步数据。

**函数签名:**
```csharp
public syncBuildingData? CreateCompleteSyncData(int buildingID)
```

**参数:**
- `buildingID`: 建筑 ID

**返回值:**
- 包含完整状态的 `syncBuildingData`
- 失败时返回 null

**使用示例:**
```csharp
// 发送己方建筑的完整状态给对方
syncBuildingData? myBuildingData = buildingManager.CreateCompleteSyncData(buildingID);
if (myBuildingData.HasValue)
{
    // 通过网络发送
    NetworkManager.Send(myBuildingData.Value);
}
```

**包含的信息:**
- 基本信息：buildingID, buildingName, playerID
- 位置和状态：position, currentHP, state
- 建造信息：remainingBuildCost
- 升级等级：hpLevel, attackRangeLevel, slotsLevel, buildCostLevel

**实现位置:** `BuildingManager.cs:420-458`

---

#### CreateCompleteSyncData()的使用场景

`CreateCompleteSyncData()` 用于获取建筑的完整状态信息以便网络发送。以下是具体使用场景：

##### 1. 建筑完成时

己方建筑完成时，用于通知对方：

```csharp
void OnBuildingCompleted(int buildingID)
{
    Debug.Log($"己方建筑完成: ID={buildingID}");

    // 获取完成建筑的完整信息并发送
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        networkManager.SendBuildingComplete(data.Value);
    }
}
```

##### 2. 状态改变时（升级、受损等）

建筑状态改变时，发送变更后的完整状态给对方：

```csharp
// 建筑升级后
void UpgradeMyBuilding(int buildingID)
{
    if (buildingManager.UpgradeBuilding(buildingID, BuildingUpgradeType.HP))
    {
        Debug.Log("建筑HP升级成功");

        // 获取升级后的完整状态并发送
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendBuildingUpdate(data.Value);
        }
    }
}

// 建筑受损后
void OnBuildingDamaged(int buildingID)
{
    // 获取受损后的完整状态并发送
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        networkManager.SendBuildingUpdate(data.Value);
    }
}
```

##### 3. 游戏中途加入时的状态同步

玩家中途加入游戏时，需要发送所有现有建筑的状态：

```csharp
// 新玩家加入时
void OnNewPlayerJoined(int newPlayerID)
{
    Debug.Log("新玩家加入，正在发送现有建筑状态...");

    // 发送所有己方建筑的状态
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

##### 4. 重连时的状态恢复

网络断开后重连时，重新发送所有当前状态：

```csharp
// 玩家重连时
void OnPlayerReconnected(int playerID)
{
    Debug.Log($"玩家{playerID}重连，正在重新同步状态...");
    SyncAllBuildingsToPlayer(playerID);
}

void SyncAllBuildingsToPlayer(int targetPlayerID)
{
    // 发送所有建筑状态
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

##### 5. 调试・状态确认时

开发中用于确认当前状态：

```csharp
// 调试用：输出建筑的完整状态日志
void DebugBuildingState(int buildingID)
{
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
    if (data.HasValue)
    {
        Debug.Log($"=== 建筑状态: ID={data.Value.buildingID} ===");
        Debug.Log($"名称: {data.Value.buildingName}");
        Debug.Log($"玩家ID: {data.Value.playerID}");
        Debug.Log($"HP: {data.Value.currentHP}");
        Debug.Log($"状态: {data.Value.state}");
        Debug.Log($"剩余建造成本: {data.Value.remainingBuildCost}");
        Debug.Log($"HP等级: {data.Value.hpLevel}");
        Debug.Log($"攻击范围等级: {data.Value.attackRangeLevel}");
        Debug.Log($"槽位等级: {data.Value.slotsLevel}");
    }
}
```

##### 使用场景总结

| 场景 | 用途 |
|-----|------|
| ✅ 建筑完成时 | 将完成建筑的所有信息通知对方 |
| ✅ 状态改变后 | 同步升级、受损后的最新状态 |
| ✅ 游戏中途加入 | 向新玩家发送所有现有建筑 |
| ✅ 重连时 | 重新同步断线期间可能改变的状态 |
| ✅ 定期同步 | 定期发送完整状态防止同步偏差 |
| ✅ 调试 | 确认并输出当前完整状态 |

**要点：**

1. **包含完整状态** - HP、等级、建造进度等所有信息都包含
2. **一致性保证** - 一次调用获取一致的状态快照
3. **发送就绪** - 返回的 `syncBuildingData` 可直接用于网络发送
4. **NULL 安全** - 建筑不存在时返回 null（`?` 返回值类型）

---

### 建筑删除

#### `HandleEnemyBuildingDestruction()`
处理接收到的敌方建筑破坏通知。

**函数签名:**
```csharp
public bool HandleEnemyBuildingDestruction(syncBuildingData sbd)
```

**参数:**
- `sbd`: 被破坏建筑的同步数据（currentHP = 0）

**返回值:**
- `true`: 删除成功
- `false`: 失败（建筑不存在）

**使用示例:**
```csharp
// 接收到敌方建筑破坏通知
syncBuildingData destructionData = new syncBuildingData
{
    buildingID = 201,
    currentHP = 0,
    state = BuildingState.Ruined
};

if (buildingManager.HandleEnemyBuildingDestruction(destructionData))
{
    Debug.Log("敌方建筑已删除");
}
```

**注意:**
- 此方法仅删除建筑，不会触发 `OnBuildingDestroyed` 事件
- 用于接收端处理破坏通知，避免无限循环

**实现位置:** `BuildingManager.cs:689-706`

---

#### `GetLastDestroyedBuildingData()`
获取最后一个被破坏建筑的同步数据（发送端使用）。

**函数签名:**
```csharp
public syncBuildingData? GetLastDestroyedBuildingData()
```

**返回值:**
- 最后一个被破坏建筑的同步数据
- 如果没有缓存数据则返回 null

**使用示例:**
```csharp
// 在 GameManager 中订阅建筑破坏事件
buildingManager.OnBuildingDestroyed += (buildingID) =>
{
    // 获取被破坏建筑的完整数据
    syncBuildingData? destructionData = buildingManager.GetLastDestroyedBuildingData();
    if (destructionData.HasValue)
    {
        // 发送给对方玩家
        NetworkManager.SendBuildingDestruction(destructionData.Value);
    }
};
```

**注意:**
- 数据获取后会被清空（单次使用）
- 必须在 `OnBuildingDestroyed` 事件触发后立即调用
- 用于发送端获取破坏数据并通知对方

**实现位置:** `BuildingManager.cs:712-717`

---

#### `OnBuildingDestroyed` 事件
建筑被破坏时触发的事件。

**事件签名:**
```csharp
public event Action<int> OnBuildingDestroyed;
```

**参数:**
- `int`: 被破坏建筑的 ID

**使用示例:**
```csharp
// 订阅建筑破坏事件
buildingManager.OnBuildingDestroyed += HandleBuildingDestruction;

void HandleBuildingDestruction(int destroyedBuildingID)
{
    Debug.Log($"建筑 {destroyedBuildingID} 已被破坏");

    // 获取破坏数据并发送给对方
    syncBuildingData? destructionData = buildingManager.GetLastDestroyedBuildingData();
    if (destructionData.HasValue)
    {
        NetworkManager.SendBuildingDestruction(destructionData.Value);
    }
}
```

**实现位置:** `BuildingManager.cs:33`

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

### 建筑建造流程详解

本节详细说明从建筑建造开始到完成，再到将完成的建筑信息传递给 GameManager 进行同步的完整流程。

#### 流程图

```
GameManager
    │
    ├─ CreateBuilding() → BuildingManager
    │                         │
    │                         ├─ 生成建筑（UnderConstruction状态）
    │                         └─ Building.Initialize()
    │                               │
    │                               └─ 设置 remainingBuildCost
    │
    ├─ AddFarmerToConstruction() → BuildingManager
    │                                  │
    │                                  ├─ 消耗农民 AP
    │                                  └─ Building.ProgressConstruction()
    │                                        │
    │                                        ├─ 减少 remainingBuildCost
    │                                        └─ 归零后调用 CompleteConstruction()
    │                                              │
    │                                              ├─ 状态改为 Inactive
    │                                              └─ 触发 OnBuildingCompleted 事件
    │                                                    ↓
    │                        BuildingManager.HandleBuildingCompleted()
    │                                                    │
    │                                                    └─ 触发 OnBuildingCompleted 事件
    │                                                          ↓
    ├─ OnBuildingCompleted() ← GameManager 接收事件
    │        │
    │        ├─ CreateCompleteSyncData() → BuildingManager
    │        │                                  │
    │        │                                  └─ 生成 syncBuildingData
    │        │
    │        └─ 网络发送
```

#### 步骤详解

**步骤1: 开始建造（GameManager → BuildingManager）**

GameManager 调用 `CreateBuilding()` 生成建筑。此时建筑以 `UnderConstruction` 状态创建。

```csharp
// GameManager 代码
BuildingDataSO buildingData = // 获取建筑数据
int buildingID = buildingManager.CreateBuilding(buildingData, playerID, position);
```

**步骤2: 建筑初始化（Building 内部）**

BuildingManager 生成建筑 Prefab 并调用 `Building.Initialize()`。此时：
- `playerID` 设置为建筑所有者
- `remainingBuildCost` 设置为初始建造成本
- 建筑状态设为 `UnderConstruction`
- 订阅 `OnBuildingCompleted` 事件

```csharp
// Building.cs 内部
public void Initialize(BuildingDataSO data, int ownerPlayerID)
{
    buildingData = data;
    playerID = ownerPlayerID; // 设置所有者

    currentHp = data.maxHp;
    remainingBuildCost = data.buildingAPCost;
    currentState = BuildingState.UnderConstruction;

    // 槽位初始化等...
}
```

**注意:** Building 自身保存 PlayerID，因此 BuildingManager 无需单独维护 `buildingOwners` 字典。

**步骤3: 推进建造（GameManager → BuildingManager → Building）**

GameManager 投入农民推进建造：

```csharp
// GameManager 代码
bool success = buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager);
```

BuildingManager 消耗农民 AP 后调用 `Building.ProgressConstruction()`：

```csharp
// BuildingManager.cs 内部
public bool AddFarmerToConstruction(int buildingID, int farmerID, PieceManager pieceManager)
{
    // 确认并消耗农民 AP
    if (pieceManager.ConsumePieceAP(farmerID, farmer.Data.devotionAPCost))
    {
        // 推进建造
        building.ProgressConstruction(progressAmount);
    }
}
```

**步骤4: 建造成本递减（Building 内部）**

`Building.ProgressConstruction()` 减少 `remainingBuildCost`，归零后调用完成处理：

```csharp
// Building.cs 内部
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

**步骤5: 建造完成（Building 内部）**

调用 `Building.CompleteConstruction()` 时：
- 状态改为 `Inactive`
- 触发 `OnBuildingCompleted` 事件

```csharp
// Building.cs 内部
private void CompleteConstruction()
{
    currentState = BuildingState.Inactive;
    Debug.Log($"建筑建造完成: {Data.buildingName} (ID: {buildingID})");

    // 触发事件
    OnBuildingCompleted?.Invoke(buildingID);
}
```

**步骤6: BuildingManager 接收事件**

BuildingManager 在 `HandleBuildingCompleted()` 中接收建筑完成事件，并通知 GameManager：

```csharp
// BuildingManager.cs 内部
private void HandleBuildingCompleted(int buildingID)
{
    Debug.Log($"BuildingManager: 检测到建筑完成 ID={buildingID}");

    // 向 GameManager 触发事件
    OnBuildingCompleted?.Invoke(buildingID);
}
```

**步骤7: GameManager 接收完成通知并网络发送**

GameManager 订阅 `OnBuildingCompleted` 事件，获取完成建筑的信息并通过网络发送给对方：

```csharp
// GameManager 代码
void OnBuildingCompleted(int buildingID)
{
    Debug.Log($"己方建筑完成: ID={buildingID}");

    // 获取完成建筑的完整同步数据
    syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);

    if (data.HasValue)
    {
        // 通过网络发送给对方
        networkManager.SendBuildingComplete(data.Value);
        Debug.Log($"已发送建筑完成通知: ID={buildingID}");
    }
}
```

#### 完整实现示例

以下是 GameManager 的完整实现示例：

```csharp
public class NetworkGameManager : MonoBehaviour
{
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private PieceManager pieceManager;
    private NetworkManager networkManager;
    private int localPlayerID = 1;

    void Start()
    {
        // 初始化
        buildingManager.SetLocalPlayerID(localPlayerID);
        pieceManager.SetLocalPlayerID(localPlayerID);

        // 订阅建筑完成事件
        buildingManager.OnBuildingCompleted += OnBuildingCompleted;

        // 订阅建筑破坏事件
        buildingManager.OnBuildingDestroyed += OnBuildingDestroyed;

        // 设置网络消息接收
        networkManager.OnBuildingCompleteReceived += OnEnemyBuildingCompleteReceived;
    }

    // === 己方建筑建造流程 ===

    /// <summary>
    /// 开始建筑建造
    /// </summary>
    void StartBuildingConstruction()
    {
        // 1. 生成建筑（UnderConstruction状态）
        BuildingDataSO buildingData = GetBuildingData("祭坛");
        int buildingID = buildingManager.CreateBuilding(buildingData, localPlayerID, new Vector3(10, 0, 10));

        if (buildingID >= 0)
        {
            Debug.Log($"建筑建造开始: ID={buildingID}");

            // 向对方发送建筑生成通知
            syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
            if (data.HasValue)
            {
                networkManager.SendBuildingCreate(data.Value);
            }
        }
    }

    /// <summary>
    /// 投入农民推进建造
    /// </summary>
    void ProgressBuildingConstruction(int buildingID, int farmerID)
    {
        // 2. 投入农民（消耗 AP 推进建造）
        if (buildingManager.AddFarmerToConstruction(buildingID, farmerID, pieceManager))
        {
            Debug.Log($"建造推进中: buildingID={buildingID}, farmerID={farmerID}");

            // 向对方发送建造进度
            syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
            if (data.HasValue)
            {
                networkManager.SendBuildingUpdate(data.Value);
            }
        }
    }

    /// <summary>
    /// 建筑完成时的处理（由事件自动调用）
    /// </summary>
    void OnBuildingCompleted(int buildingID)
    {
        Debug.Log($"己方建筑完成: ID={buildingID}");

        // 3. 向对方发送完成通知
        syncBuildingData? data = buildingManager.CreateCompleteSyncData(buildingID);
        if (data.HasValue)
        {
            networkManager.SendBuildingComplete(data.Value);
            Debug.Log($"已发送建筑完成通知: ID={buildingID}");
        }
    }

    /// <summary>
    /// 建筑破坏时的处理（由事件自动调用）
    /// </summary>
    void OnBuildingDestroyed(int buildingID)
    {
        Debug.Log($"己方建筑被破坏: ID={buildingID}");

        // 获取破坏数据并发送
        syncBuildingData? data = buildingManager.GetLastDestroyedBuildingData();
        if (data.HasValue)
        {
            networkManager.SendBuildingDestruction(data.Value);
        }
    }

    // === 敌方建筑接收流程 ===

    /// <summary>
    /// 接收敌方建筑完成通知
    /// </summary>
    void OnEnemyBuildingCompleteReceived(syncBuildingData data)
    {
        Debug.Log($"收到敌方建筑完成通知: ID={data.buildingID}");

        // 如果建筑已存在则同步状态
        if (buildingManager.DoesBuildingExist(data.buildingID))
        {
            buildingManager.SyncEnemyBuildingState(data);
        }
        else
        {
            // 建筑不存在则新建
            buildingManager.CreateEnemyBuilding(data);
        }
    }
}
```

#### 要点总结

1. **建造状态转换**
   - `UnderConstruction` → `Inactive` → `Active`
   - 完成后立即为 `Inactive`（配置农民后变为 `Active`）

2. **事件驱动设计**
   - Building → BuildingManager → GameManager 层级事件传播
   - 各层松耦合独立运作

3. **syncBuildingData 的使用**
   - `remainingBuildCost` 包含建造进度
   - `state` 包含当前建筑状态
   - 用一个结构体表达完整状态

4. **网络同步时机**
   - 建造开始时：通知建筑生成
   - 建造进行时：更新进度（可选）
   - 建造完成时：发送完成通知（重要）

5. **初始化两个 Manager**
   - 需要同时调用 `buildingManager.SetLocalPlayerID()` 和 `pieceManager.SetLocalPlayerID()`
   - 这样才能正确区分己方/敌方的建筑・棋子

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

**实现位置:** `BuildingManager.cs:352-355`

---

---

#### `GetPlayerBuildings()`
获取指定玩家的所有建筑 ID。

**函数签名:**
```csharp
public List<int> GetPlayerBuildings(int playerID)
```

**参数:**
- `playerID`: 玩家 ID

**返回值:**
- 建筑 ID 列表

**使用示例:**
```csharp
List<int> player1Buildings = buildingManager.GetPlayerBuildings(1);
Debug.Log($"玩家1的建筑数: {player1Buildings.Count}");

foreach (int buildingID in player1Buildings)
{
    BuildingState state = buildingManager.GetBuildingState(buildingID);
    Debug.Log($"建筑ID={buildingID}, 状态={state}");
}
```

**实现位置:** `BuildingManager.cs:373-379`

---

#### `GetAllBuildingIDs()`
获取所有建筑 ID。

**函数签名:**
```csharp
public List<int> GetAllBuildingIDs()
```

**实现位置:** `BuildingManager.cs:384-387`

---

#### `GetOperationalBuildings()`
获取运行中的建筑 ID 列表。

**函数签名:**
```csharp
public List<int> GetOperationalBuildings()
```

**实现位置:** `BuildingManager.cs:392-398`

---

#### `GetBuildingsUnderConstruction()`
获取建造中的建筑 ID 列表。

**函数签名:**
```csharp
public List<int> GetBuildingsUnderConstruction()
```

**实现位置:** `BuildingManager.cs:403-409`

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

## 数据结构

### `syncBuildingData`
建筑同步数据结构。

```csharp
[System.Serializable]
public struct syncBuildingData
{
    // 基本信息
    public int buildingID;
    public string buildingName;
    public int playerID;
    public Vector3 position;

    // 状态信息
    public int currentHP;
    public BuildingState state;
    public int remainingBuildCost; // 剩余建造成本

    // 升级等级
    public int hpLevel;            // HP 等级 (0-3)
    public int attackRangeLevel;   // 攻击范围等级 (0-3)
    public int slotsLevel;         // 槽位数等级 (0-3)
    public int buildCostLevel;     // 建造成本等级 (0-3)
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

## 网络同步流程示例

以下是完整的网络同步实现示例：

> **⚠️ 重要: 关于初始化**
>
> 使用网络同步时，**必须对两个 Manager 都调用 `SetLocalPlayerID()`**。
> 如果只设置其中一个，同步将无法正常工作。
>
> ```csharp
> // ✅ 正确的初始化
> pieceManager.SetLocalPlayerID(localPlayerID);
> buildingManager.SetLocalPlayerID(localPlayerID);
>
> // ❌ 只设置一个会导致同步失败
> pieceManager.SetLocalPlayerID(localPlayerID);
> // 忘记设置 buildingManager
> ```

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BuildingManager buildingManager;
    private int localPlayerID = 1;  // 本地玩家 ID
    private NetworkManager networkManager;  // 网络管理器（示例）

    void Start()
    {
        // ⚠️ 重要: 必须对两个 Manager 都设置 localPlayerID
        pieceManager.SetLocalPlayerID(localPlayerID);
        buildingManager.SetLocalPlayerID(localPlayerID);

        // 订阅事件
        pieceManager.OnPieceDied += OnOwnPieceDied;
        buildingManager.OnBuildingDestroyed += OnOwnBuildingDestroyed;

        // 注册网络回调
        networkManager.OnReceivePieceCreate += OnReceiveEnemyPieceCreate;
        networkManager.OnReceivePieceUpdate += OnReceiveEnemyPieceUpdate;
        networkManager.OnReceivePieceDeath += OnReceiveEnemyPieceDeath;
    }

    // ===== 发送端逻辑 =====

    /// <summary>
    /// 创建己方棋子并发送给对方
    /// </summary>
    void CreateMyPiece()
    {
        // 1. 创建己方棋子
        int pieceID = pieceManager.CreatePiece(
            PieceType.Farmer,
            Religion.Maya,
            localPlayerID,
            new Vector3(10, 0, 10)
        );

        if (pieceID >= 0)
        {
            // 2. 创建完整同步数据
            syncPieceData data = pieceManager.CreateCompleteSyncData(pieceID, Religion.Maya);

            // 3. 发送给对方
            networkManager.SendPieceCreate(data);
        }
    }

    /// <summary>
    /// 己方棋子状态改变时发送更新
    /// </summary>
    void UpdateMyPiece(int pieceID)
    {
        // 1. 创建完整同步数据
        syncPieceData data = pieceManager.CreateCompleteSyncData(pieceID, Religion.Maya);

        // 2. 发送给对方
        networkManager.SendPieceUpdate(data);
    }

    /// <summary>
    /// 己方棋子死亡时的处理（自动触发）
    /// </summary>
    void OnOwnPieceDied(int deadPieceID)
    {
        Debug.Log($"己方棋子死亡: ID={deadPieceID}");

        // 1. 获取死亡棋子的数据（HP=0）
        syncPieceData? deathData = pieceManager.GetLastDeadPieceData();

        // 2. 发送死亡通知给对方
        if (deathData.HasValue)
        {
            networkManager.SendPieceDeath(deathData.Value);
            Debug.Log($"已发送死亡通知: 棋子ID={deathData.Value.pieceID}");
        }
    }

    // ===== 接收端逻辑 =====

    /// <summary>
    /// 接收并创建敌方棋子
    /// </summary>
    void OnReceiveEnemyPieceCreate(syncPieceData data)
    {
        // 直接根据同步数据创建敌方棋子
        if (pieceManager.CreateEnemyPiece(data))
        {
            Debug.Log($"敌方棋子创建成功: ID={data.pieceID}");
        }
        else
        {
            Debug.LogError($"敌方棋子创建失败: ID={data.pieceID}");
        }
    }

    /// <summary>
    /// 接收并更新敌方棋子状态
    /// </summary>
    void OnReceiveEnemyPieceUpdate(syncPieceData data)
    {
        // 同步敌方棋子状态
        if (pieceManager.SyncEnemyPieceState(data))
        {
            Debug.Log($"敌方棋子状态更新成功: ID={data.pieceID}");
        }
        else
        {
            Debug.LogWarning($"敌方棋子不存在，尝试创建: ID={data.pieceID}");
            pieceManager.CreateEnemyPiece(data);
        }
    }

    /// <summary>
    /// 接收敌方棋子死亡通知
    /// </summary>
    void OnReceiveEnemyPieceDeath(syncPieceData data)
    {
        // 删除敌方棋子（不触发 OnPieceDied 事件）
        if (pieceManager.HandleEnemyPieceDeath(data))
        {
            Debug.Log($"敌方棋子已删除: ID={data.pieceID}");
        }
        else
        {
            Debug.LogWarning($"要删除的敌方棋子不存在: ID={data.pieceID}");
        }
    }

    // ===== 示例：攻击流程 =====

    /// <summary>
    /// 攻击敌方棋子的完整流程
    /// </summary>
    void AttackEnemyPiece(int myMilitaryID, int enemyPieceID)
    {
        // 1. 执行攻击
        if (pieceManager.AttackEnemy(myMilitaryID, enemyPieceID))
        {
            Debug.Log("攻击成功");

            // 2. 攻击成功后，发送己方棋子状态（AP消耗）
            syncPieceData myData = pieceManager.CreateCompleteSyncData(myMilitaryID, Religion.Maya);
            networkManager.SendPieceUpdate(myData);

            // 3. 如果敌方棋子死亡，OnOwnPieceDied 会自动触发并发送死亡通知
            // （这里不需要手动处理）
        }
    }
}
```

### 同步流程总结

**创建棋子流程:**
1. 发送端：`CreatePiece()` → `CreateCompleteSyncData()` → 发送网络消息
2. 接收端：接收消息 → `CreateEnemyPiece()`

**更新棋子流程:**
1. 发送端：修改状态 → `CreateCompleteSyncData()` → 发送网络消息
2. 接收端：接收消息 → `SyncEnemyPieceState()`

**棋子死亡流程:**
1. 发送端：棋子死亡 → `OnPieceDied` 事件触发 → `GetLastDeadPieceData()` → 发送死亡通知
2. 接收端：接收死亡通知 → `HandleEnemyPieceDeath()`

**关键设计:**
- ✅ 使用 `localPlayerID` 区分己方和敌方棋子
- ✅ 死亡处理分离（发送端触发事件，接收端直接删除）
- ✅ 防止无限循环（接收端不触发 OnPieceDied 事件）
- ✅ 数据完整性（syncPieceData 包含所有必要状态）

---

## 数据结构

### `syncPieceData`
棋子同步数据结构。

```csharp
public struct syncPieceData
{
    // 基本信息
    public int pieceID;
    public PieceType pieceType;
    public Religion religion;
    public int playerID;
    public Vector3 position;

    // 状态信息
    public int currentPID;          // 当前所属玩家 ID（魅惑后可能改变）
    public int currentHP;

    // 等级信息
    public int currentHPLevel;      // HP 等级 (0-3)
    public int currentAPLevel;      // AP 等级 (0-3)

    // 职业特定等级
    public int attackPowerLevel;    // 军队: 攻击力等级 (0-3)
    public int sacrificeLevel;      // 农民: 献祭等级 (0-2)
    public int occupyLevel;         // 宣教师: 占领等级 (0-3)
    public int convertEnemyLevel;   // 宣教师: 魅惑等级 (0-3)
    public int swapCooldownLevel;   // 教皇: 位置交换CD等级 (0-2)
    public int buffLevel;           // 教皇: 增益等级 (0-3)
}
```

### `swapPieceData`
棋子位置交换数据结构。

```csharp
public struct swapPieceData
{
    public int pieceID1;
    public Vector3 position1;
    public int pieceID2;
    public Vector3 position2;
}
```

---

## 总结

### PieceManager的功能

- ✅ **ID 基础管理**: 无需关注具体类型即可操作
- ✅ **网络同步对应**: 己方和敌方棋子分离管理
- ✅ **同步数据自动生成**: 各种操作后返回 `syncPieceData`
- ✅ **发送/接收分离**: 防止无限循环的清晰设计
- ✅ **事件驱动**: 棋子死亡等自动通知

### BuildingManager的功能

- ✅ **ID 基础管理**: 建筑不依赖类型进行管理
- ✅ **网络同步对应**: 己方和敌方建筑分离管理
- ✅ **完整状态同步**: `syncBuildingData` 包含建筑的所有信息
- ✅ **破坏处理分离**: 发送端和接收端不同的处理流程
- ✅ **事件驱动**: 建筑破坏・完成自动通知

所有操作都基于 ID 统一管理，使 GameManager 的代码简洁易读。网络同步通过 `syncPieceData` 和 `syncBuildingData` 结构实现完整状态传输，避免了数据不一致问题。
