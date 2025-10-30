# 单位系统 API 参考文档

本文档说明Unity项目中单位操作相关的主要函数及其使用方法。

---

## 目录

1. [农民的建筑功能](#1-农民的建筑功能)
2. [士兵的攻击功能](#2-士兵的攻击功能)
3. [单位参数的获取](#3-单位参数的获取)
4. [传教士的魅惑功能](#4-传教士的魅惑功能)
5. [单位数据的变更](#5-单位数据的变更)
6. [领地的占领功能](#6-领地的占领功能)
7. [单项升级功能](#7-单项升级功能)

---

## 1. 农民的建筑功能

### 概述
农民（Farmer）具有建造和继续建造建筑物的能力。建造需要消耗AP，如果AP不足，农民会被消耗。

### 实现文件
- `Assets/Cyou/Script/Derived Cl/farmer.cs`
- `Assets/Cyou/Script/Factory/BuildingFactory.cs`

### 主要函数

#### `StartConstruction()`
开始建造新建筑物。

**函数签名:**
```csharp
public bool StartConstruction(BuildingDataSO selectedBuilding, Vector3 position)
```

**参数:**
- `selectedBuilding`: 要建造的建筑物ScriptableObject数据
- `position`: 建筑物放置坐标

**返回值:**
- `true`: 建造开始成功
- `false`: AP不足等原因导致建造开始失败

**使用示例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
BuildingDataSO buildingData = // 获取建筑物数据
Vector3 buildPos = new Vector3(10, 0, 10);

if (farmer.StartConstruction(buildingData, buildPos))
{
    Debug.Log("开始建造");
}
else
{
    Debug.Log("AP不足，无法建造");
}
```

**处理流程:**
1. 消耗`buildStartAPCost`的AP
2. 通过`BuildingFactory.CreateBuilding()`生成建筑物实例
3. 用农民剩余的AP推进建造
4. 如果AP不足，农民被消耗（Die()）

**实现位置:** `farmer.cs:102-140`

---

#### `ContinueConstruction()`
继续建造已在建造中的建筑物。

**函数签名:**
```csharp
public bool ContinueConstruction(Building building)
```

**参数:**
- `building`: 建造中的建筑物对象

**返回值:**
- `true`: 继续建造成功
- `false`: 继续建造失败

**使用示例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
Building existingBuilding = // 获取建造中的建筑物

if (farmer.ContinueConstruction(existingBuilding))
{
    Debug.Log("继续建造");
}
```

**处理流程:**
1. 将当前所有AP投入建造
2. `building.RemainingBuildCost`减少
3. 如果建造完成，农民被消耗

**实现位置:** `farmer.cs:147-186`

---

### 相关数据（FarmerDataSO）

```csharp
public float buildingSpeedModifier = 1.0f;  // 建造速度修正值
public float productEfficiency = 1.0f;       // 生产效率
public int devotionAPCost = 1;               // 献身技能AP消耗
public int[] maxSacrificeLevel = new int[3]; // 回复量（按升级等级）
```

---

## 2. 士兵的攻击功能

### 概述
士兵（MilitaryUnit）可以对敌方单位进行攻击并造成伤害。攻击需要消耗AP，并存在冷却时间。

### 实现文件
- `Assets/Cyou/Script/Derived Cl/military.cs`

### 主要函数

#### `Attack()`
对敌方单位进行攻击。

**函数签名:**
```csharp
public bool Attack(Piece target)
```

**参数:**
- `target`: 攻击目标棋子（Piece对象）

**返回值:**
- `true`: 攻击成功
- `false`: AP不足、冷却中或数据异常导致攻击失败

**使用示例:**
```csharp
MilitaryUnit soldier = GetComponent<MilitaryUnit>();
Piece enemy = // 获取敌方单位

if (soldier.Attack(enemy))
{
    Debug.Log("攻击成功！");
}
else
{
    Debug.Log("无法攻击（AP不足或冷却中）");
}
```

**处理流程:**
1. 数据有效性检查
2. 消耗`attackAPCost`的AP
3. 冷却时间检查
4. 伤害计算（`CalculateDamage()`）
5. 暴击判定（`criticalChance`）
6. 通过`TakeDamage()`对目标造成伤害

**实现位置:** `military.cs:36-50`

---

#### `CalculateDamage()`
计算攻击伤害（内部函数）。

**函数签名:**
```csharp
private float CalculateDamage()
```

**返回值:**
- 计算出的伤害值

**伤害计算公式:**
```csharp
基础伤害 = militaryData.attackPower
暴击时 = 基础伤害 × 2.0f
最终伤害 = 基础伤害 - 目标的armorValue（护甲值）
```

**实现位置:** `military.cs:65-68`

---

### 相关数据（MilitaryDataSO）

```csharp
public float attackPower = 0f;                       // 攻击力
public float attackRange = 0f;                       // 攻击范围
public float attackCooldown = 1f;                    // 攻击冷却时间（秒）
public float attackAPCost = 20f;                     // 攻击AP消耗量
public float armorValue = 10f;                       // 护甲值（减少受到的伤害）
public float criticalChance = 0.1f;                  // 暴击概率（0.0-1.0）
public DamageType damageType;                        // 伤害类型（物理/魔法/特殊）
public bool[] hasAntiConversionSkill = new bool[4];  // 每个升级等级的魅惑抗性
```

**按升级等级的攻击力:**
```csharp
public float[] attackPowerByLevel = new float[4];    // 升级0-3的攻击力
```

---

## 3. 单位参数的获取

### 概述
所有单位都继承自基类`Piece`，拥有共同参数和专用参数。

### 实现文件
- `Assets/Cyou/Script/Base Cl/pieces.cs`（基类）
- `Assets/Cyou/Script/SO Data/Base Cl/PieceDataSO.cs`（SO数据）

### 基本参数（Piece）

#### HP相关
```csharp
public float CurrentHP { get; }                      // 当前HP（只读）
public float CurrentMaxHP { get; }                   // 当前最大HP（只读）
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"HP: {unit.CurrentHP} / {unit.CurrentMaxHP}");

float hpPercentage = unit.CurrentHP / unit.CurrentMaxHP * 100f;
Debug.Log($"HP百分比: {hpPercentage}%");
```

---

#### AP（行动力）相关
```csharp
public float CurrentAP { get; }                      // 当前AP（只读）
public float CurrentMaxAP { get; }                   // 当前最大AP（只读）
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"AP: {unit.CurrentAP} / {unit.CurrentMaxAP}");

if (unit.CurrentAP >= 20f)
{
    Debug.Log("有足够的AP可以攻击");
}
```

---

#### 玩家ID相关
```csharp
public int CurrentPID { get; }                       // 当前所属玩家ID
public int OriginalPID { get; }                      // 原始所属玩家ID
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.CurrentPID != unit.OriginalPID)
{
    Debug.Log("该单位被魅惑了");
}

if (unit.CurrentPID == myPlayerID)
{
    Debug.Log("该单位是友军");
}
```

---

#### 升级等级
```csharp
public int UpgradeLevel { get; }                     // 当前升级等级（0-3）
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"升级等级: {unit.UpgradeLevel}");

if (unit.UpgradeLevel < 3)
{
    Debug.Log("还可以升级");
}
```

---

#### 状态（State）
```csharp
public PieceState CurrentState { get; }              // 当前状态

public enum PieceState
{
    Idle,         // 待机中
    Moving,       // 移动中
    Attacking,    // 攻击中
    Building,     // 建造中/占领中
    InBuilding,   // 建筑物内
    Dead          // 死亡
}
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.CurrentState == PieceState.Idle)
{
    Debug.Log("该单位处于待机状态");
}
else if (unit.CurrentState == PieceState.Attacking)
{
    Debug.Log("该单位正在攻击");
}
```

---

### ScriptableObject 数据（PieceDataSO）

#### 基本参数
```csharp
public int originalPID;                              // 初始所属玩家ID
public string pieceName;                             // 棋子名称
public int populationCost = 1;                       // 人口消耗量
public int resourceCost = 10;                        // 资源消耗量
```

#### 行动力参数
```csharp
public float aPRecoveryRate = 10f;                   // 每回合AP恢复量
public float moveAPCost = 10f;                       // 移动时AP消耗
public float moveSpeed = 1.0f;                       // 移动速度
```

#### 战斗参数
```csharp
public bool canAttack = false;                       // 是否可以攻击
public float attackPower = 0f;                       // 攻击力
public float attackRange = 0f;                       // 攻击范围
public float attackCooldown = 1f;                    // 攻击冷却时间
public float attackAPCost = 20f;                     // 攻击AP消耗量
```

#### 按升级等级的参数
```csharp
public float[] maxHPByLevel = new float[4];          // 升级0-3的最大HP
public float[] maxAPByLevel = new float[4];          // 升级0-3的最大AP
public float[] attackPowerByLevel = new float[4];    // 升级0-3的攻击力
```

#### 升级成本
```csharp
public int[] hpUpgradeCost = new int[3];             // HP升级的资源成本（0→1, 1→2, 2→3）
public int[] apUpgradeCost = new int[3];             // AP升级的资源成本（0→1, 1→2, 2→3）
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
PieceDataSO data = unit.GetPieceData(); // 假设存在数据获取函数

Debug.Log($"棋子名称: {data.pieceName}");
Debug.Log($"移动速度: {data.moveSpeed}");
Debug.Log($"当前等级的最大HP: {data.maxHPByLevel[unit.UpgradeLevel]}");
```

---

## 4. 传教士的魅惑功能

### 概述
传教士（Missionary）可以魅惑敌方单位，使其暂时成为友军。魅惑需要消耗AP，成功率根据升级等级变化。

### 实现文件
- `Assets/Cyou/Script/Derived Cl/missionary.cs`

### 主要函数

#### `ConversionAttack()`
魅惑敌方单位。

**函数签名:**
```csharp
public bool ConversionAttack(Piece target)
```

**参数:**
- `target`: 魅惑目标棋子（Piece对象）

**返回值:**
- `true`: 魅惑尝试成功（成功率判定在内部进行）
- `false`: AP不足等原因导致魅惑尝试失败

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Piece enemy = // 获取敌方单位

if (missionary.ConversionAttack(enemy))
{
    Debug.Log("执行了魅惑攻击");
    // 成功率判定在内部进行
}
else
{
    Debug.Log("AP不足，无法魅惑");
}
```

**处理流程:**
1. 目标有效性检查（教皇不可被魅惑）
2. 消耗`convertAPCost`的AP
3. 按棋子类型获取成功率（`GetConversionChanceByPieceType()`）
4. 成功率判定（Random）
5. 成功时：
   - 升级1以上的传教士、士兵会立即死亡（`Die()`）
   - 其他情况下改变阵营（`ConvertEnemy()`）
6. 失败时：什么都不发生

**实现位置:** `missionary.cs:182-229`

---

#### `GetConversionChanceByPieceType()`
根据目标类型获取魅惑成功率。

**函数签名:**
```csharp
public float GetConversionChanceByPieceType(Piece target)
```

**参数:**
- `target`: 魅惑目标棋子

**返回值:**
- 魅惑成功率（0.0～1.0）

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Piece enemy = // 获取敌方单位

float successRate = missionary.GetConversionChanceByPieceType(enemy);
Debug.Log($"魅惑成功率: {successRate * 100}%");
```

**实现位置:** `missionary.cs:70-105`

---

### 魅惑成功率表（MissionaryDataSO）

#### 按传教士升级等级的成功率

**魅惑传教士:**
```csharp
public float[] convertMissionaryChanceByLevel = new float[4]
{ 0.0f, 0.5f, 0.55f, 0.65f };
// 升级0: 0%,  升级1: 50%,  升级2: 55%,  升级3: 65%
```

**魅惑农民:**
```csharp
public float[] convertFarmerChanceByLevel = new float[4]
{ 0.0f, 0.1f, 0.2f, 0.3f };
// 升级0: 0%,  升级1: 10%,  升级2: 20%,  升级3: 30%
```

**魅惑士兵:**
```csharp
public float[] convertMilitaryChanceByLevel = new float[4]
{ 0.0f, 0.45f, 0.5f, 0.6f };
// 升级0: 0%,  升级1: 45%,  升级2: 50%,  升级3: 60%
```

**注意:** 教皇（Pope）不可被魅惑

---

### 魅惑持续回合数
```csharp
public int[] conversionTurnDuration = new int[4]
{ 2, 3, 4, 5 };
// 升级0: 2回合,  升级1: 3回合,  升级2: 4回合,  升级3: 5回合
```

被魅惑的单位在指定回合数经过后会自动恢复到原来的阵营。

---

### 相关数据（MissionaryDataSO）

```csharp
public int convertAPCost = 3;                        // 魅惑AP消耗量
public float occupyAPCost = 30f;                     // 占领AP消耗量
public float[] convertMissionaryChanceByLevel;       // 魅惑传教士成功率
public float[] convertFarmerChanceByLevel;           // 魅惑农民成功率
public float[] convertMilitaryChanceByLevel;         // 魅惑士兵成功率
public int[] conversionTurnDuration;                 // 魅惑持续回合数
```

---

## 5. 单位数据的变更

### 概述
从外部变更单位参数（HP、AP、阵营等）的方法。所有变更都通过事件进行通知。

### 实现文件
- `Assets/Cyou/Script/Base Cl/pieces.cs`

---

### HP变更

#### `TakeDamage()`
对单位造成伤害。

**函数签名:**
```csharp
public virtual void TakeDamage(float damage, Piece attacker = null)
```

**参数:**
- `damage`: 伤害量
- `attacker`: 攻击者（可选）

**使用示例:**
```csharp
Piece target = // 受到伤害的单位
target.TakeDamage(50f); // 50点伤害

// HP降至0以下时会自动调用Die()
```

**实现位置:** `pieces.cs:181-192`

---

#### `Heal()`
恢复单位HP。

**函数签名:**
```csharp
public virtual void Heal(float amount)
```

**参数:**
- `amount`: 恢复量

**使用示例:**
```csharp
Piece unit = // 要恢复的单位
unit.Heal(30f); // 恢复30点

// 不会超过最大HP
```

**实现位置:** `pieces.cs:194-203`

---

### AP变更

#### `ConsumeAP()`
消耗AP。

**函数签名:**
```csharp
public bool ConsumeAP(float amount)
```

**参数:**
- `amount`: 消耗的AP量

**返回值:**
- `true`: AP消耗成功
- `false`: AP不足，消耗失败

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.ConsumeAP(20f))
{
    Debug.Log("消耗了20AP");
    // 执行动作
}
else
{
    Debug.Log("AP不足");
}
```

**实现位置:** `pieces.cs:156-164`

---

#### `ModifyAP()` (Protected)
增减AP（内部函数）。

**函数签名:**
```csharp
protected void ModifyAP(float amount)
```

**参数:**
- `amount`: 增减量（正值增加，负值减少）

**使用示例（在派生类内）:**
```csharp
// 在派生类内使用
ModifyAP(10f);   // 恢复10AP
ModifyAP(-15f);  // 消耗15AP
```

**实现位置:** `pieces.cs:166-175`

---

### 阵营变更

#### `ChangePID()`
变更单位的所属玩家ID（魅惑时使用）。

**函数签名:**
```csharp
public virtual void ChangePID(int newPlayerID, int charmTurns = 0, Piece charmer = null)
```

**参数:**
- `newPlayerID`: 新的玩家ID
- `charmTurns`: 魅惑持续回合数（0表示永久变更）
- `charmer`: 魅惑者棋子（可选）

**使用示例:**
```csharp
Piece enemy = // 敌方单位
int myPlayerID = 1;

// 永久改变阵营
enemy.ChangePID(myPlayerID);

// 魅惑3回合
Missionary missionary = GetComponent<Missionary>();
enemy.ChangePID(myPlayerID, 3, missionary);
```

**实现位置:** `pieces.cs:97-115`

---

### 升级（Upgrade）

#### `UpgradePiece()`
升级单位。

**函数签名:**
```csharp
public virtual bool UpgradePiece()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级（升级3）

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.UpgradeLevel < 3)
{
    if (unit.UpgradePiece())
    {
        Debug.Log($"升级了！新等级: {unit.UpgradeLevel}");
    }
}
```

**处理流程:**
1. 检查当前升级等级
2. 等级递增
3. 通过`ApplyUpgradeEffects()`更新最大HP・AP・攻击力
4. 事件通知

**实现位置:** `pieces.cs:226-238`

---

### 状态变更

#### `ChangeState()`
变更单位的行动状态。

**函数签名:**
```csharp
protected void ChangeState(PieceState newState)
```

**参数:**
- `newState`: 新状态

**使用示例（在派生类内）:**
```csharp
// 在派生类内使用
ChangeState(PieceState.Attacking); // 变更为攻击中状态
ChangeState(PieceState.Idle);      // 变更为待机状态
```

---

### 事件通知

所有参数变更都通过事件向外部通知。

#### HP变更事件
```csharp
public event Action<float, float> OnHPChanged; // (当前HP, 最大HP)
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnHPChanged += (currentHP, maxHP) =>
{
    Debug.Log($"HP发生了变化: {currentHP}/{maxHP}");
    UpdateHealthBar(currentHP, maxHP);
};
```

---

#### AP变更事件
```csharp
public event Action<float, float> OnAPChanged; // (当前AP, 最大AP)
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnAPChanged += (currentAP, maxAP) =>
{
    Debug.Log($"AP发生了变化: {currentAP}/{maxAP}");
    UpdateAPBar(currentAP, maxAP);
};
```

---

#### 魅惑事件
```csharp
public static event Action<Piece, Piece> OnAnyCharmed;  // (被魅惑的棋子, 魅惑者)
public static event Action<Piece> OnAnyUncharmed;       // (魅惑解除的棋子)
public event Action<Piece, Piece> OnCharmed;            // 实例事件
public event Action<Piece> OnUncharmed;                 // 实例事件
```

**使用示例:**
```csharp
// 全局事件（监视所有魅惑）
Piece.OnAnyCharmed += (charmedPiece, charmer) =>
{
    Debug.Log($"{charmedPiece.name}被{charmer.name}魅惑了");
};

Piece.OnAnyUncharmed += (piece) =>
{
    Debug.Log($"{piece.name}的魅惑解除了");
};

// 实例事件（只监视特定单位）
Piece unit = GetComponent<Piece>();
unit.OnCharmed += (charmedPiece, charmer) =>
{
    Debug.Log($"该单位被魅惑了");
};
```

---

#### 死亡事件
```csharp
public event Action<Piece> OnPieceDeath;
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnPieceDeath += (deadPiece) =>
{
    Debug.Log($"{deadPiece.name}死亡了");
    // 死亡演出或分数更新等
};
```

---

#### 状态变更事件
```csharp
public event Action<PieceState, PieceState> OnStateChanged; // (旧状态, 新状态)
```

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnStateChanged += (oldState, newState) =>
{
    Debug.Log($"状态从{oldState}变为了{newState}");
};
```

---

### 参数变更总结

| 参数 | 变更方法 | 相关函数 | 事件 |
|-----------|---------|---------|---------|
| **当前HP** | 受到伤害/恢复 | `TakeDamage()`, `Heal()` | `OnHPChanged` |
| **当前AP** | 消耗/恢复 | `ConsumeAP()`, `ModifyAP()` | `OnAPChanged` |
| **最大HP/AP** | 升级 | `UpgradePiece()` | - |
| **所属阵营** | 魅惑/恢复 | `ChangePID()` | `OnCharmed`, `OnUncharmed` |
| **状态** | 行动状态变更 | `ChangeState()` | `OnStateChanged` |
| **升级等级** | 升级 | `UpgradePiece()` | - |

---

## 6. 领地的占领功能

### 概述
传教士（Missionary）具有占领领地的能力。空白领地和敌方领地的成功率不同，成功率根据升级等级提升。

### 实现文件
- `Assets/Cyou/Script/Derived Cl/missionary.cs`

### 主要函数

#### `StartOccupy()`
开始占领领地。

**函数签名:**
```csharp
public bool StartOccupy(Vector3 targetPosition)
```

**参数:**
- `targetPosition`: 占领目标领地坐标

**返回值:**
- `true`: 占领尝试成功（成功率判定在内部进行）
- `false`: AP不足或已在占领中

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Vector3 targetTile = new Vector3(15, 0, 20);

if (missionary.StartOccupy(targetTile))
{
    Debug.Log("开始占领");
    // 成功率判定在内部进行
}
else
{
    Debug.Log("AP不足或已在占领中");
}
```

**处理流程:**
1. 检查是否在占领中（`isOccupying`）
2. 消耗`occupyAPCost`的AP（默认30AP）
3. 变更为占领状态（`PieceState.Building`）
4. 成功率判定（`ExecuteOccupy()`）
5. 成功时：变更领地所有权
6. 失败时：只消耗AP

**实现位置:** `missionary.cs:123-140`

---

#### `GetOccupyEmptySuccessRate()`
获取空白领地的占领成功率。

**函数签名:**
```csharp
public float GetOccupyEmptySuccessRate()
```

**返回值:**
- 空白领地占领成功率（0.0～1.0）

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
float successRate = missionary.GetOccupyEmptySuccessRate();
Debug.Log($"空白领地占领成功率: {successRate * 100}%");
```

**实现位置:** `missionary.cs:70-75`

---

#### `GetOccupyEnemySuccessRate()`
获取敌方领地的占领成功率。

**函数签名:**
```csharp
public float GetOccupyEnemySuccessRate()
```

**返回值:**
- 敌方领地占领成功率（0.0～1.0）

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
float successRate = missionary.GetOccupyEnemySuccessRate();
Debug.Log($"敌方领地占领成功率: {successRate * 100}%");
```

**实现位置:** `missionary.cs:77-81`

---

### 占领成功率表（MissionaryDataSO）

#### 空白领地占领成功率
```csharp
public float[] occupyEmptySuccessRateByLevel = new float[4]
{ 0.8f, 0.9f, 1.0f, 1.0f };
// 升级0: 80%,  升级1: 90%,  升级2: 100%,  升级3: 100%
```

#### 敌方领地占领成功率
```csharp
public float[] occupyEnemySuccessRateByLevel = new float[4]
{ 0.5f, 0.6f, 0.7f, 0.7f };
// 升级0: 50%,  升级1: 60%,  升级2: 70%,  升级3: 70%
```

#### AP消耗量
```csharp
public float occupyAPCost = 30f; // 默认30AP
```

---

### 占领事件

```csharp
public event Action<bool> OnOccupyCompleted; // (成功/失败)
```

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
missionary.OnOccupyCompleted += (success) =>
{
    if (success)
    {
        Debug.Log("占领成功！");
        // 更新领地UI等
    }
    else
    {
        Debug.Log("占领失败...");
    }
};
```

---

### 相关数据（MissionaryDataSO）

```csharp
public float occupyAPCost = 30f;                     // 占领AP消耗量
public float[] occupyEmptySuccessRateByLevel;        // 空白领地占领成功率（按升级等级）
public float[] occupyEnemySuccessRateByLevel;        // 敌方领地占领成功率（按升级等级）
```

---

## 补充：通用设计模式

### 1. AP消耗检查模式
所有动作都按以下模式检查AP消耗：

```csharp
if (!ConsumeAP(requiredAP))
{
    // AP不足
    return false;
}

// 执行动作
PerformAction();
return true;
```

### 2. 事件驱动设计
所有参数变更都通过`event Action`进行通知：

```csharp
// 参数变更
currentHP = newValue;

// 触发事件
OnHPChanged?.Invoke(currentHP, currentMaxHP);
```

### 3. 升级等级依赖参数
许多参数根据升级等级通过数组管理：

```csharp
float currentValue = dataArray[UpgradeLevel]; // 0-3
```

---

## 7. 单项升级功能

### 概述
棋子（Piece）和建筑（Building）各项参数可以单独升级。每个项目都有独立的等级和升级成本，升级函数假设资源已经确保的前提下执行升级。

**重要:** 这些升级函数假设资源已经被确保。资源管理由GameManager端进行。

### 实现文件
- `Assets/Cyou/Script/Base Cl/pieces.cs`（棋子基类）
- `Assets/Cyou/Script/Derived Cl/farmer.cs`（农民）
- `Assets/Cyou/Script/Derived Cl/military.cs`（军队）
- `Assets/Cyou/Script/Derived Cl/missionary.cs`（传教士）
- `Assets/Cyou/Script/Derived Cl/pope.cs`（教皇）
- `Assets/Cyou/Script/Base Cl/building.cs`（建筑）

---

### 棋子通用升级（Piece基类）

所有棋子都有以下2个通用升级项目。

#### `UpgradeHP()`
升级最大HP（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public virtual bool UpgradeHP()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
int cost = unit.GetUpgradeCost(PieceUpgradeType.HP);

// GameManager端消耗资源后
if (unit.UpgradeHP())
{
    Debug.Log($"HP升级成功！新等级: {unit.HPLevel}");
}
```

**实现位置:** `pieces.cs:252-286`

---

#### `UpgradeAP()`
升级最大AP（行动力）（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public virtual bool UpgradeAP()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
int cost = unit.GetUpgradeCost(PieceUpgradeType.AP);

// GameManager端消耗资源后
if (unit.UpgradeAP())
{
    Debug.Log($"AP升级成功！新等级: {unit.APLevel}");
}
```

**实现位置:** `pieces.cs:288-322`

---

#### 获取升级成本

**函数签名:**
```csharp
public int GetUpgradeCost(PieceUpgradeType type)
```

**参数:**
- `type`: 升级项目（`PieceUpgradeType.HP` 或 `PieceUpgradeType.AP`）

**返回值:**
- 升级成本（资源量）
- `-1`: 无法升级（已达最大等级或数据未设置）

**使用示例:**
```csharp
Piece unit = GetComponent<Piece>();
int hpCost = unit.GetUpgradeCost(PieceUpgradeType.HP);
int apCost = unit.GetUpgradeCost(PieceUpgradeType.AP);

if (hpCost > 0)
{
    Debug.Log($"HP升级成本: {hpCost}");
}
```

---

### 农民（Farmer）专用升级

#### `UpgradeSacrifice()`
升级献祭回复量（等级0→2，最大2阶段）。

**函数签名:**
```csharp
public bool UpgradeSacrifice()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
int cost = farmer.GetFarmerUpgradeCost(FarmerUpgradeType.Sacrifice);

// GameManager端消耗资源后
if (farmer.UpgradeSacrifice())
{
    Debug.Log($"献祭回复量升级成功！新等级: {farmer.SacrificeLevel}");
}
```

**实现位置:** `farmer.cs:278-308`

---

### 军队（Military）专用升级

#### `UpgradeAttackPower()`
升级攻击力（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public bool UpgradeAttackPower()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
MilitaryUnit soldier = GetComponent<MilitaryUnit>();
int cost = soldier.GetMilitaryUpgradeCost(MilitaryUpgradeType.AttackPower);

// GameManager端消耗资源后
if (soldier.UpgradeAttackPower())
{
    Debug.Log($"攻击力升级成功！新等级: {soldier.AttackPowerLevel}");
}
```

**实现位置:** `military.cs:165-195`

---

### 传教士（Missionary）专用升级

传教士有2个独立的升级项目。

#### `UpgradeOccupy()`
升级领地占领成功率（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public bool UpgradeOccupy()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
int cost = missionary.GetMissionaryUpgradeCost(MissionaryUpgradeType.Occupy);

// GameManager端消耗资源后
if (missionary.UpgradeOccupy())
{
    Debug.Log($"占领成功率升级成功！新等级: {missionary.OccupyLevel}");
}
```

**实现位置:** `missionary.cs:283-313`

---

#### `UpgradeConvertEnemy()`
升级敌方单位魅惑成功率（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public bool UpgradeConvertEnemy()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
int cost = missionary.GetMissionaryUpgradeCost(MissionaryUpgradeType.ConvertEnemy);

// GameManager端消耗资源后
if (missionary.UpgradeConvertEnemy())
{
    Debug.Log($"魅惑成功率升级成功！新等级: {missionary.ConvertEnemyLevel}");
}
```

**实现位置:** `missionary.cs:315-345`

---

### 教皇（Pope）专用升级

教皇有2个独立的升级项目。

#### `UpgradeSwapCooldown()`
升级位置交换冷却时间（等级0→2，最大2阶段）。

**函数签名:**
```csharp
public bool UpgradeSwapCooldown()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Pope pope = GetComponent<Pope>();
int cost = pope.GetPopeUpgradeCost(PopeUpgradeType.SwapCooldown);

// GameManager端消耗资源后
if (pope.UpgradeSwapCooldown())
{
    Debug.Log($"位置交换CD升级成功！新等级: {pope.SwapCooldownLevel}");
}
```

**实现位置:** `pope.cs:128-158`

---

#### `UpgradeBuff()`
升级对周围友军的增益效果（等级0→3，最大3阶段）。

**函数签名:**
```csharp
public bool UpgradeBuff()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级或无法升级

**使用示例:**
```csharp
Pope pope = GetComponent<Pope>();
int cost = pope.GetPopeUpgradeCost(PopeUpgradeType.Buff);

// GameManager端消耗资源后
if (pope.UpgradeBuff())
{
    Debug.Log($"增益效果升级成功！新等级: {pope.BuffLevel}");
}
```

**实现位置:** `pope.cs:165-198`

---

### 建筑（Building）的升级

建筑有4个独立的升级项目（每个项目等级0→2，最大2阶段）。

#### `UpgradeHP()`
升级建筑的最大HP。

**函数签名:**
```csharp
public bool UpgradeHP()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级、建筑未完成或无法升级

**使用示例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.HP);

// GameManager端消耗资源后
if (building.UpgradeHP())
{
    Debug.Log($"建筑HP升级成功！新等级: {building.HPLevel}");
}
```

**实现位置:** `building.cs:386-426`

---

#### `UpgradeAttackRange()`
升级建筑的攻击范围。

**函数签名:**
```csharp
public bool UpgradeAttackRange()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级、建筑未完成或无法升级

**使用示例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.AttackRange);

// GameManager端消耗资源后
if (building.UpgradeAttackRange())
{
    Debug.Log($"攻击范围升级成功！新等级: {building.AttackRangeLevel}");
}
```

**实现位置:** `building.cs:432-470`

---

#### `UpgradeSlots()`
升级建筑的槽位数（可容纳农民数量）。

**函数签名:**
```csharp
public bool UpgradeSlots()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级、建筑未完成或无法升级

**使用示例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.Slots);

// GameManager端消耗资源后
if (building.UpgradeSlots())
{
    Debug.Log($"槽位数升级成功！新等级: {building.SlotsLevel}");
}
```

**实现位置:** `building.cs:476-525`

---

#### `UpgradeBuildCost()`
减少建筑的建造成本（所需AP）。

**函数签名:**
```csharp
public bool UpgradeBuildCost()
```

**返回值:**
- `true`: 升级成功
- `false`: 已达最大等级、建筑未完成或无法升级

**使用示例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.BuildCost);

// GameManager端消耗资源后
if (building.UpgradeBuildCost())
{
    Debug.Log($"建造成本降低升级成功！新等级: {building.BuildCostLevel}");
}
```

**实现位置:** `building.cs:531-569`

---

### 升级系统的设计模式

#### 1. 单项等级管理
每个升级项目都有独立的等级字段：
```csharp
// Piece基类
private int hpLevel = 0;  // 0-3
private int apLevel = 0;  // 0-3

// 农民专用
private int sacrificeLevel = 0;  // 0-2

// 军队专用
private int attackPowerLevel = 0;  // 0-3

// 传教士专用
private int occupyLevel = 0;  // 0-3
private int convertEnemyLevel = 0;  // 0-3

// 教皇专用
private int swapCooldownLevel = 0;  // 0-2
private int buffLevel = 0;  // 0-3

// 建筑
private int hpLevel = 0;  // 0-2
private int attackRangeLevel = 0;  // 0-2
private int slotsLevel = 0;  // 0-2
private int buildCostLevel = 0;  // 0-2
```

#### 2. 获取升级成本模式
每个类都提供获取自身升级成本的函数：
```csharp
// 棋子通用
public int GetUpgradeCost(PieceUpgradeType type)

// 职业专用
public int GetFarmerUpgradeCost(FarmerUpgradeType type)
public int GetMilitaryUpgradeCost(MilitaryUpgradeType type)
public int GetMissionaryUpgradeCost(MissionaryUpgradeType type)
public int GetPopeUpgradeCost(PopeUpgradeType type)

// 建筑
public int GetUpgradeCost(BuildingUpgradeType type)
```

返回值为`-1`时，表示无法升级（已达最大等级或数据未设置）。

#### 3. 资源管理分离
升级函数不消耗资源，只负责执行升级：

```csharp
// GameManager端的实现示例
public bool TryUpgradePieceHP(Piece piece)
{
    int cost = piece.GetUpgradeCost(PieceUpgradeType.HP);

    if (cost <= 0)
    {
        Debug.Log("无法升级");
        return false;
    }

    if (!ConsumeResource(cost))
    {
        Debug.Log("资源不足");
        return false;
    }

    // 消耗资源后执行升级
    return piece.UpgradeHP();
}
```

---

### 升级项目一览

| 类 | 项目 | 等级范围 | 函数名 |
|--------|------|-----------|--------|
| **Piece（通用）** | HP | 0-3 | `UpgradeHP()` |
| **Piece（通用）** | AP | 0-3 | `UpgradeAP()` |
| **Farmer** | 献祭回复量 | 0-2 | `UpgradeSacrifice()` |
| **Military** | 攻击力 | 0-3 | `UpgradeAttackPower()` |
| **Missionary** | 占领成功率 | 0-3 | `UpgradeOccupy()` |
| **Missionary** | 魅惑成功率 | 0-3 | `UpgradeConvertEnemy()` |
| **Pope** | 位置交换CD | 0-2 | `UpgradeSwapCooldown()` |
| **Pope** | 增益效果 | 0-3 | `UpgradeBuff()` |
| **Building** | HP | 0-2 | `UpgradeHP()` |
| **Building** | 攻击范围 | 0-2 | `UpgradeAttackRange()` |
| **Building** | 槽位数 | 0-2 | `UpgradeSlots()` |
| **Building** | 建造成本 | 0-2 | `UpgradeBuildCost()` |

总计: **13个升级函数**

---

## 更新历史
- 2025-10-28: 初版创建
- 2025-10-30: 添加单项升级功能章节
