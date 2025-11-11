# ユニットシステム API リファレンス

このドキュメントでは、Unityプロジェクト内のユニット操作に関する主要な関数とその使用方法を説明します。

---

## 目次

1. [農民の建築機能](#1-農民の建築機能)
2. [兵士の攻撃機能](#2-兵士の攻撃機能)
3. [ユニットパラメータの取得](#3-ユニットパラメータの取得)
4. [宣教師の魅了機能](#4-宣教師の魅了機能)
5. [ユニットデータの変動](#5-ユニットデータの変動)
6. [領地の占領機能](#6-領地の占領機能)
7. [個別項目のアップグレード機能](#7-個別項目のアップグレード機能)

---

## 1. 農民の建築機能

### 概要
農民（Farmer）は建築物を作成・継続建築する能力を持ちます。建築にはAPを消費し、不足した場合は農民が消費されます。

### 実装ファイル
- `Assets/Cyou/Script/Derived Cl/farmer.cs`
- `Assets/Cyou/Script/Factory/BuildingFactory.cs`

### 主要関数

#### `StartConstruction()`
新しい建築物の建築を開始します。

**シグネチャ:**
```csharp
public bool StartConstruction(BuildingDataSO selectedBuilding, Vector3 position)
```

**パラメータ:**
- `selectedBuilding`: 建築する建物のScriptableObjectデータ
- `position`: 建物を配置する座標

**戻り値:**
- `true`: 建築開始に成功
- `false`: AP不足などで建築開始に失敗

**使用例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
BuildingDataSO buildingData = // 建物データを取得
Vector3 buildPos = new Vector3(10, 0, 10);

if (farmer.StartConstruction(buildingData, buildPos))
{
    Debug.Log("建築を開始しました");
}
else
{
    Debug.Log("AP不足で建築できません");
}
```

**処理の流れ:**
1. `buildStartAPCost`分のAPを消費
2. `BuildingFactory.CreateBuilding()`で建物インスタンスを生成
3. 農民の残りAPで建築を進行
4. APが不足した場合、農民は消費される（Die()）

**実装箇所:** `farmer.cs:102-140`

---

#### `ContinueConstruction()`
既に建築中の建物の建築を継続します。

**シグネチャ:**
```csharp
public bool ContinueConstruction(Building building)
```

**パラメータ:**
- `building`: 建築中の建物オブジェクト

**戻り値:**
- `true`: 建築継続に成功
- `false`: 建築継続に失敗

**使用例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
Building existingBuilding = // 建築中の建物を取得

if (farmer.ContinueConstruction(existingBuilding))
{
    Debug.Log("建築を継続しました");
}
```

**処理の流れ:**
1. 現在のAPを全て建築に投入
2. `building.RemainingBuildCost`が減少
3. 建築が完了した場合、農民は消費される

**実装箇所:** `farmer.cs:147-186`

---

### 関連データ（FarmerDataSO）

```csharp
public float buildingSpeedModifier = 1.0f;  // 建築速度修正値
public float productEfficiency = 1.0f;       // 生産効率
public int devotionAPCost = 1;               // 献身スキルAP消費
public int[] maxSacrificeLevel = new int[3]; // 回復量（升級別）
```

---

## 2. 兵士の攻撃機能

### 概要
兵士（MilitaryUnit）は敵ユニットに攻撃を行い、ダメージを与えることができます。攻撃にはAPを消費し、クールタイムが存在します。

### 実装ファイル
- `Assets/Cyou/Script/Derived Cl/military.cs`

### 主要関数

#### `Attack()`
敵ユニットに攻撃を行います。

**シグネチャ:**
```csharp
public bool Attack(Piece target)
```

**パラメータ:**
- `target`: 攻撃対象の駒（Pieceオブジェクト）

**戻り値:**
- `true`: 攻撃成功
- `false`: AP不足、クールタイム中、またはデータ不正で攻撃失敗

**使用例:**
```csharp
MilitaryUnit soldier = GetComponent<MilitaryUnit>();
Piece enemy = // 敵ユニットを取得

if (soldier.Attack(enemy))
{
    Debug.Log("攻撃が成功しました！");
}
else
{
    Debug.Log("攻撃できません（AP不足 or クールタイム中）");
}
```

**処理の流れ:**
1. データの有効性チェック
2. `attackAPCost`分のAPを消費
3. クールタイムのチェック
4. ダメージ計算（`CalculateDamage()`）
5. クリティカル判定（`criticalChance`）
6. ターゲットに`TakeDamage()`でダメージを適用

**実装箇所:** `military.cs:36-50`

---

#### `CalculateDamage()`
攻撃ダメージを計算します（内部関数）。

**シグネチャ:**
```csharp
private float CalculateDamage()
```

**戻り値:**
- 計算されたダメージ値

**ダメージ計算式:**
```csharp
基本ダメージ = militaryData.attackPower
クリティカル時 = 基本ダメージ × 2.0f
最終ダメージ = 基本ダメージ - ターゲットのarmorValue（装甲値）
```

**実装箇所:** `military.cs:65-68`

---

### 関連データ（MilitaryDataSO）

```csharp
public float attackPower = 0f;                       // 攻撃力
public float attackRange = 0f;                       // 攻撃範囲
public float attackCooldown = 1f;                    // 攻撃クールタイム（秒）
public float attackAPCost = 20f;                     // 攻撃AP消費量
public float armorValue = 10f;                       // 装甲値（被ダメージ軽減）
public float criticalChance = 0.1f;                  // クリティカル確率（0.0-1.0）
public DamageType damageType;                        // ダメージタイプ（物理/魔法/特殊）
public bool[] hasAntiConversionSkill = new bool[4];  // 升級ごとの魅惑耐性
```

**升級別攻撃力:**
```csharp
public float[] attackPowerByLevel = new float[4];    // 升級0-3の攻撃力
```

---

## 3. ユニットパラメータの取得

### 概要
すべてのユニットは基底クラス`Piece`を継承し、共通のパラメータと専用パラメータを持ちます。

### 実装ファイル
- `Assets/Cyou/Script/Base Cl/pieces.cs`（基底クラス）
- `Assets/Cyou/Script/SO Data/Base Cl/PieceDataSO.cs`（SOデータ）

### 基本パラメータ（Piece）

#### HP関連
```csharp
public float CurrentHP { get; }                      // 現在のHP（読み取り専用）
public float CurrentMaxHP { get; }                   // 現在の最大HP（読み取り専用）
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"HP: {unit.CurrentHP} / {unit.CurrentMaxHP}");

float hpPercentage = unit.CurrentHP / unit.CurrentMaxHP * 100f;
Debug.Log($"HP割合: {hpPercentage}%");
```

---

#### AP（行動力）関連
```csharp
public float CurrentAP { get; }                      // 現在のAP（読み取り専用）
public float CurrentMaxAP { get; }                   // 現在の最大AP（読み取り専用）
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"AP: {unit.CurrentAP} / {unit.CurrentMaxAP}");

if (unit.CurrentAP >= 20f)
{
    Debug.Log("攻撃可能なAPがあります");
}
```

---

#### プレイヤーID関連
```csharp
public int CurrentPID { get; }                       // 現在の所属プレイヤーID
public int OriginalPID { get; }                      // 元の所属プレイヤーID
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.CurrentPID != unit.OriginalPID)
{
    Debug.Log("このユニットは魅了されています");
}

if (unit.CurrentPID == myPlayerID)
{
    Debug.Log("このユニットは味方です");
}
```

---

#### 升級レベル
```csharp
public int UpgradeLevel { get; }                     // 現在の升級レベル（0-3）
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
Debug.Log($"升級レベル: {unit.UpgradeLevel}");

if (unit.UpgradeLevel < 3)
{
    Debug.Log("まだ升級可能です");
}
```

---

#### 状態（State）
```csharp
public PieceState CurrentState { get; }              // 現在の状態

public enum PieceState
{
    Idle,         // 待機中
    Moving,       // 移動中
    Attacking,    // 攻撃中
    Building,     // 建築中/占領中
    InBuilding,   // 建物内
    Dead          // 死亡
}
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.CurrentState == PieceState.Idle)
{
    Debug.Log("このユニットは待機中です");
}
else if (unit.CurrentState == PieceState.Attacking)
{
    Debug.Log("このユニットは攻撃中です");
}
```

---

### ScriptableObject データ（PieceDataSO）

#### 基本パラメータ
```csharp
public int originalPID;                              // 初期所有プレイヤーID
public string pieceName;                             // 駒名
public int populationCost = 1;                       // 人口消費量
public int resourceCost = 10;                        // 資源消費量
```

#### 行動力パラメータ
```csharp
public float aPRecoveryRate = 10f;                   // 毎ターンAP回復量
public float moveAPCost = 10f;                       // 移動時AP消費
public float moveSpeed = 1.0f;                       // 移動速度
```

#### 戦闘パラメータ
```csharp
public bool canAttack = false;                       // 攻撃可能か
public float attackPower = 0f;                       // 攻撃力
public float attackRange = 0f;                       // 攻撃範囲
public float attackCooldown = 1f;                    // 攻撃クールタイム
public float attackAPCost = 20f;                     // 攻撃AP消費量
```

#### 升級別パラメータ
```csharp
public float[] maxHPByLevel = new float[4];          // 升級0-3の最大HP
public float[] maxAPByLevel = new float[4];          // 升級0-3の最大AP
public float[] attackPowerByLevel = new float[4];    // 升級0-3の攻撃力
```

#### 升級コスト
```csharp
public int[] hpUpgradeCost = new int[3];             // HP升級の資源コスト（0→1, 1→2, 2→3）
public int[] apUpgradeCost = new int[3];             // AP升級の資源コスト（0→1, 1→2, 2→3）
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
PieceDataSO data = unit.GetPieceData(); // データ取得関数があると仮定

Debug.Log($"駒名: {data.pieceName}");
Debug.Log($"移動速度: {data.moveSpeed}");
Debug.Log($"現在レベルの最大HP: {data.maxHPByLevel[unit.UpgradeLevel]}");
```

---

## 4. 宣教師の魅了機能

### 概要
宣教師（Missionary）は敵ユニットを魅了（魅惑）して一時的に味方にすることができます。魅了にはAPを消費し、升級レベルによって成功率が変化します。

### 実装ファイル
- `Assets/Cyou/Script/Derived Cl/missionary.cs`

### 主要関数

#### `ConversionAttack()`
敵ユニットを魅了します。

**シグネチャ:**
```csharp
public bool ConversionAttack(Piece target)
```

**パラメータ:**
- `target`: 魅了対象の駒（Pieceオブジェクト）

**戻り値:**
- `true`: 魅了試行成功（成功率判定は内部で実施）
- `false`: AP不足などで魅了試行失敗

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Piece enemy = // 敵ユニットを取得

if (missionary.ConversionAttack(enemy))
{
    Debug.Log("魅了攻撃を実行しました");
    // 成功率判定は内部で行われます
}
else
{
    Debug.Log("AP不足で魅了できません");
}
```

**処理の流れ:**
1. ターゲットの有効性チェック（教皇は魅了不可）
2. `convertAPCost`分のAPを消費
3. 駒の種類別に成功率を取得（`GetConversionChanceByPieceType()`）
4. 成功率判定（Random）
5. 成功時：
   - 升級1以上の宣教師・兵士は即死（`Die()`）
   - それ以外は陣営変更（`ConvertEnemy()`）
6. 失敗時：何も起こらない

**実装箇所:** `missionary.cs:182-229`

---

#### `GetConversionChanceByPieceType()`
ターゲットの種類に応じた魅了成功率を取得します。

**シグネチャ:**
```csharp
public float GetConversionChanceByPieceType(Piece target)
```

**パラメータ:**
- `target`: 魅了対象の駒

**戻り値:**
- 魅了成功率（0.0～1.0）

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Piece enemy = // 敵ユニットを取得

float successRate = missionary.GetConversionChanceByPieceType(enemy);
Debug.Log($"魅了成功率: {successRate * 100}%");
```

**実装箇所:** `missionary.cs:70-105`

---

### 魅了成功率テーブル（MissionaryDataSO）

#### 宣教師升級別の成功率

**宣教師を魅了:**
```csharp
public float[] convertMissionaryChanceByLevel = new float[4]
{ 0.0f, 0.5f, 0.55f, 0.65f };
// 升級0: 0%,  升級1: 50%,  升級2: 55%,  升級3: 65%
```

**農民を魅了:**
```csharp
public float[] convertFarmerChanceByLevel = new float[4]
{ 0.0f, 0.1f, 0.2f, 0.3f };
// 升級0: 0%,  升級1: 10%,  升級2: 20%,  升級3: 30%
```

**兵士を魅了:**
```csharp
public float[] convertMilitaryChanceByLevel = new float[4]
{ 0.0f, 0.45f, 0.5f, 0.6f };
// 升級0: 0%,  升級1: 45%,  升級2: 50%,  升級3: 60%
```

**注意:** 教皇（Pope）は魅了不可

---

### 魅了持続ターン数
```csharp
public int[] conversionTurnDuration = new int[4]
{ 2, 3, 4, 5 };
// 升級0: 2ターン,  升級1: 3ターン,  升級2: 4ターン,  升級3: 5ターン
```

魅了されたユニットは、指定ターン経過後に自動的に元の陣営に復帰します。

---

### 関連データ（MissionaryDataSO）

```csharp
public int convertAPCost = 3;                        // 魅了AP消費量
public float occupyAPCost = 30f;                     // 占領AP消費量
public float[] convertMissionaryChanceByLevel;       // 宣教師魅了成功率
public float[] convertFarmerChanceByLevel;           // 農民魅了成功率
public float[] convertMilitaryChanceByLevel;         // 兵士魅了成功率
public int[] conversionTurnDuration;                 // 魅了持続ターン数
```

---

## 5. ユニットデータの変動

### 概要
ユニットのパラメータ（HP、AP、陣営など）を外部から変更する方法です。すべての変更はイベントを通じて通知されます。

### 実装ファイル
- `Assets/Cyou/Script/Base Cl/pieces.cs`

---

### HP変動

#### `TakeDamage()`
ユニットにダメージを与えます。

**シグネチャ:**
```csharp
public virtual void TakeDamage(float damage, Piece attacker = null)
```

**パラメータ:**
- `damage`: ダメージ量
- `attacker`: 攻撃者（オプション）

**使用例:**
```csharp
Piece target = // ダメージを受けるユニット
target.TakeDamage(50f); // 50ダメージ

// HPが0以下になると自動的にDie()が呼ばれます
```

**実装箇所:** `pieces.cs:181-192`

---

#### `Heal()`
ユニットを回復します。

**シグネチャ:**
```csharp
public virtual void Heal(float amount)
```

**パラメータ:**
- `amount`: 回復量

**使用例:**
```csharp
Piece unit = // 回復するユニット
unit.Heal(30f); // 30回復

// 最大HPを超えて回復することはありません
```

**実装箇所:** `pieces.cs:194-203`

---

### AP変動

#### `ConsumeAP()`
APを消費します。

**シグネチャ:**
```csharp
public bool ConsumeAP(float amount)
```

**パラメータ:**
- `amount`: 消費するAP量

**戻り値:**
- `true`: AP消費成功
- `false`: AP不足で消費失敗

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.ConsumeAP(20f))
{
    Debug.Log("20AP消費しました");
    // アクション実行
}
else
{
    Debug.Log("APが足りません");
}
```

**実装箇所:** `pieces.cs:156-164`

---

#### `ModifyAP()` (Protected)
APを増減します（内部関数）。

**シグネチャ:**
```csharp
protected void ModifyAP(float amount)
```

**パラメータ:**
- `amount`: 増減量（正の値で増加、負の値で減少）

**使用例（派生クラス内）:**
```csharp
// 派生クラス内で使用
ModifyAP(10f);   // 10AP回復
ModifyAP(-15f);  // 15AP消費
```

**実装箇所:** `pieces.cs:166-175`

---

### 陣営変更

#### `ChangePID()`
ユニットの所属プレイヤーIDを変更します（魅了時に使用）。

**シグネチャ:**
```csharp
public virtual void ChangePID(int newPlayerID, int charmTurns = 0, Piece charmer = null)
```

**パラメータ:**
- `newPlayerID`: 新しいプレイヤーID
- `charmTurns`: 魅了持続ターン数（0の場合は永続変更）
- `charmer`: 魅了した駒（オプション）

**使用例:**
```csharp
Piece enemy = // 敵ユニット
int myPlayerID = 1;

// 永続的に陣営変更
enemy.ChangePID(myPlayerID);

// 3ターン魅了
Missionary missionary = GetComponent<Missionary>();
enemy.ChangePID(myPlayerID, 3, missionary);
```

**実装箇所:** `pieces.cs:97-115`

---

### 升級（アップグレード）

#### `UpgradePiece()`
ユニットを升級します。

**シグネチャ:**
```csharp
public virtual bool UpgradePiece()
```

**戻り値:**
- `true`: 升級成功
- `false`: 既に最大レベル（升級3）

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();

if (unit.UpgradeLevel < 3)
{
    if (unit.UpgradePiece())
    {
        Debug.Log($"升級しました！新レベル: {unit.UpgradeLevel}");
    }
}
```

**処理の流れ:**
1. 現在の升級レベルをチェック
2. レベルをインクリメント
3. `ApplyUpgradeEffects()`で最大HP・AP・攻撃力を更新
4. イベント通知

**実装箇所:** `pieces.cs:226-238`

---

### 状態変更

#### `ChangeState()`
ユニットの行動状態を変更します。

**シグネチャ:**
```csharp
protected void ChangeState(PieceState newState)
```

**パラメータ:**
- `newState`: 新しい状態

**使用例（派生クラス内）:**
```csharp
// 派生クラス内で使用
ChangeState(PieceState.Attacking); // 攻撃中状態に変更
ChangeState(PieceState.Idle);      // 待機状態に変更
```

---

### イベント通知

すべてのパラメータ変更はイベントを通じて外部に通知されます。

#### HP変更イベント
```csharp
public event Action<float, float> OnHPChanged; // (現在HP, 最大HP)
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnHPChanged += (currentHP, maxHP) =>
{
    Debug.Log($"HPが変化しました: {currentHP}/{maxHP}");
    UpdateHealthBar(currentHP, maxHP);
};
```

---

#### AP変更イベント
```csharp
public event Action<float, float> OnAPChanged; // (現在AP, 最大AP)
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnAPChanged += (currentAP, maxAP) =>
{
    Debug.Log($"APが変化しました: {currentAP}/{maxAP}");
    UpdateAPBar(currentAP, maxAP);
};
```

---

#### 魅了イベント
```csharp
public static event Action<Piece, Piece> OnAnyCharmed;  // (魅了された駒, 魅了した駒)
public static event Action<Piece> OnAnyUncharmed;       // (魅了解除された駒)
public event Action<Piece, Piece> OnCharmed;            // インスタンスイベント
public event Action<Piece> OnUncharmed;                 // インスタンスイベント
```

**使用例:**
```csharp
// グローバルイベント（すべての魅了を監視）
Piece.OnAnyCharmed += (charmedPiece, charmer) =>
{
    Debug.Log($"{charmedPiece.name}が{charmer.name}に魅了されました");
};

Piece.OnAnyUncharmed += (piece) =>
{
    Debug.Log($"{piece.name}の魅了が解除されました");
};

// インスタンスイベント（特定ユニットのみ監視）
Piece unit = GetComponent<Piece>();
unit.OnCharmed += (charmedPiece, charmer) =>
{
    Debug.Log($"このユニットが魅了されました");
};
```

---

#### 死亡イベント
```csharp
public event Action<Piece> OnPieceDeath;
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnPieceDeath += (deadPiece) =>
{
    Debug.Log($"{deadPiece.name}が死亡しました");
    // 死亡演出やスコア更新など
};
```

---

#### 状態変更イベント
```csharp
public event Action<PieceState, PieceState> OnStateChanged; // (旧状態, 新状態)
```

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
unit.OnStateChanged += (oldState, newState) =>
{
    Debug.Log($"状態が{oldState}から{newState}に変化しました");
};
```

---

### パラメータ変動まとめ

| パラメータ | 変動方法 | 関連関数 | イベント |
|-----------|---------|---------|---------|
| **現在HP** | ダメージ受付/回復 | `TakeDamage()`, `Heal()` | `OnHPChanged` |
| **現在AP** | 消費/回復 | `ConsumeAP()`, `ModifyAP()` | `OnAPChanged` |
| **最大HP/AP** | 升級 | `UpgradePiece()` | - |
| **所属陣営** | 魅了/復帰 | `ChangePID()` | `OnCharmed`, `OnUncharmed` |
| **状態** | 行動状態変更 | `ChangeState()` | `OnStateChanged` |
| **升級レベル** | 升級 | `UpgradePiece()` | - |

---

## 6. 領地の占領機能

### 概要
宣教師（Missionary）は領地を占領する能力を持ちます。空白領地と敵領地で成功率が異なり、升級レベルによって成功率が上昇します。

### 実装ファイル
- `Assets/Cyou/Script/Derived Cl/missionary.cs`

### 主要関数

#### `StartOccupy()`
領地の占領を開始します。

**シグネチャ:**
```csharp
public bool StartOccupy(Vector3 targetPosition)
```

**パラメータ:**
- `targetPosition`: 占領対象の領地座標

**戻り値:**
- `true`: 占領試行成功（成功率判定は内部で実施）
- `false`: AP不足または既に占領中

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
Vector3 targetTile = new Vector3(15, 0, 20);

if (missionary.StartOccupy(targetTile))
{
    Debug.Log("占領を開始しました");
    // 成功率判定は内部で行われます
}
else
{
    Debug.Log("AP不足または既に占領中です");
}
```

**処理の流れ:**
1. 占領中かチェック（`isOccupying`）
2. `occupyAPCost`分のAPを消費（デフォルト30AP）
3. 占領状態に変更（`PieceState.Building`）
4. 成功率判定（`ExecuteOccupy()`）
5. 成功時：領地の所有権を変更
6. 失敗時：APだけ消費される

**実装箇所:** `missionary.cs:123-140`

---

#### `GetOccupyEmptySuccessRate()`
空白領地の占領成功率を取得します。

**シグネチャ:**
```csharp
public float GetOccupyEmptySuccessRate()
```

**戻り値:**
- 空白領地占領成功率（0.0～1.0）

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
float successRate = missionary.GetOccupyEmptySuccessRate();
Debug.Log($"空白領地占領成功率: {successRate * 100}%");
```

**実装箇所:** `missionary.cs:70-75`

---

#### `GetOccupyEnemySuccessRate()`
敵領地の占領成功率を取得します。

**シグネチャ:**
```csharp
public float GetOccupyEnemySuccessRate()
```

**戻り値:**
- 敵領地占領成功率（0.0～1.0）

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
float successRate = missionary.GetOccupyEnemySuccessRate();
Debug.Log($"敵領地占領成功率: {successRate * 100}%");
```

**実装箇所:** `missionary.cs:77-81`

---

### 占領成功率テーブル（MissionaryDataSO）

#### 空白領地占領成功率
```csharp
public float[] occupyEmptySuccessRateByLevel = new float[4]
{ 0.8f, 0.9f, 1.0f, 1.0f };
// 升級0: 80%,  升級1: 90%,  升級2: 100%,  升級3: 100%
```

#### 敵領地占領成功率
```csharp
public float[] occupyEnemySuccessRateByLevel = new float[4]
{ 0.5f, 0.6f, 0.7f, 0.7f };
// 升級0: 50%,  升級1: 60%,  升級2: 70%,  升級3: 70%
```

#### AP消費量
```csharp
public float occupyAPCost = 30f; // デフォルト30AP
```

---

### 占領イベント

```csharp
public event Action<bool> OnOccupyCompleted; // (成功/失敗)
```

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
missionary.OnOccupyCompleted += (success) =>
{
    if (success)
    {
        Debug.Log("占領に成功しました！");
        // 領地UI更新など
    }
    else
    {
        Debug.Log("占領に失敗しました...");
    }
};
```

---

### 関連データ（MissionaryDataSO）

```csharp
public float occupyAPCost = 30f;                     // 占領AP消費量
public float[] occupyEmptySuccessRateByLevel;        // 空白領地占領成功率（升級別）
public float[] occupyEnemySuccessRateByLevel;        // 敵領地占領成功率（升級別）
```

---

## 補足：共通の設計パターン

### 1. AP消費チェックパターン
すべてのアクションは以下のパターンでAP消費をチェックします：

```csharp
if (!ConsumeAP(requiredAP))
{
    // AP不足
    return false;
}

// アクション実行
PerformAction();
return true;
```

### 2. イベント駆動設計
すべてのパラメータ変更は`event Action`を通じて通知されます：

```csharp
// パラメータ変更
currentHP = newValue;

// イベント発火
OnHPChanged?.Invoke(currentHP, currentMaxHP);
```

### 3. 升級レベル依存パラメータ
多くのパラメータは升級レベルに応じて配列で管理されます：

```csharp
float currentValue = dataArray[UpgradeLevel]; // 0-3
```

---

## 7. 個別項目のアップグレード機能

### 概要
駒（Piece）と建物（Building）の各パラメータ項目を個別にアップグレードする機能です。各項目には独立したレベルとアップグレードコストがあり、リソースが確保されている前提でアップグレードを実行します。

**重要:** これらのアップグレード関数は、リソースが既に確保されている前提で動作します。リソース管理はGameManager側で行う設計です。

### 実装ファイル
- `Assets/Cyou/Script/Base Cl/pieces.cs`（駒基底クラス）
- `Assets/Cyou/Script/Derived Cl/farmer.cs`（農民）
- `Assets/Cyou/Script/Derived Cl/military.cs`（軍隊）
- `Assets/Cyou/Script/Derived Cl/missionary.cs`（宣教師）
- `Assets/Cyou/Script/Derived Cl/pope.cs`（教皇）
- `Assets/Cyou/Script/Base Cl/building.cs`（建物）

---

### 駒共通のアップグレード（Piece基底クラス）

すべての駒は以下の2つの共通アップグレード項目を持ちます。

#### `UpgradeHP()`
最大HPをアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public virtual bool UpgradeHP()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
int cost = unit.GetUpgradeCost(PieceUpgradeType.HP);

// GameManager側でリソース消費を行った後
if (unit.UpgradeHP())
{
    Debug.Log($"HPアップグレード成功！新レベル: {unit.HPLevel}");
}
```

**実装箇所:** `pieces.cs:252-286`

---

#### `UpgradeAP()`
最大AP（行動力）をアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public virtual bool UpgradeAP()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
int cost = unit.GetUpgradeCost(PieceUpgradeType.AP);

// GameManager側でリソース消費を行った後
if (unit.UpgradeAP())
{
    Debug.Log($"APアップグレード成功！新レベル: {unit.APLevel}");
}
```

**実装箇所:** `pieces.cs:288-322`

---

#### アップグレードコスト取得

**シグネチャ:**
```csharp
public int GetUpgradeCost(PieceUpgradeType type)
```

**パラメータ:**
- `type`: アップグレード項目（`PieceUpgradeType.HP` または `PieceUpgradeType.AP`）

**戻り値:**
- アップグレードコスト（リソース量）
- `-1`: アップグレード不可（最大レベルまたはデータ未設定）

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();
int hpCost = unit.GetUpgradeCost(PieceUpgradeType.HP);
int apCost = unit.GetUpgradeCost(PieceUpgradeType.AP);

if (hpCost > 0)
{
    Debug.Log($"HPアップグレードコスト: {hpCost}");
}
```

---

#### アップグレード可能かチェック

**シグネチャ:**
```csharp
public bool CanUpgrade(UpgradeType type)
```

**パラメータ:**
- `type`: アップグレード項目（`PieceUpgradeType.HP` または `PieceUpgradeType.AP`）

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達またはアップグレード不可

**使用例:**
```csharp
Piece unit = GetComponent<Piece>();

// UIボタンの有効/無効を切り替え
hpUpgradeButton.interactable = unit.CanUpgrade(PieceUpgradeType.HP);
apUpgradeButton.interactable = unit.CanUpgrade(PieceUpgradeType.AP);

// アップグレード前のチェック
if (unit.CanUpgrade(PieceUpgradeType.HP))
{
    int cost = unit.GetUpgradeCost(PieceUpgradeType.HP);
    // アップグレード処理...
}
else
{
    Debug.Log("HPは既に最大レベルです");
}
```

**主な用途:**
- UIボタンの有効/無効の切り替え
- アップグレード可能な項目のフィルタリング
- 最大レベル到達の簡易チェック

---

### 農民（Farmer）専用アップグレード

#### `UpgradeSacrifice()`
獻祭回復量をアップグレードします（レベル0→2、最大2段階）。

**シグネチャ:**
```csharp
public bool UpgradeSacrifice()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Farmer farmer = GetComponent<Farmer>();
int cost = farmer.GetFarmerUpgradeCost(FarmerUpgradeType.Sacrifice);

// GameManager側でリソース消費を行った後
if (farmer.UpgradeSacrifice())
{
    Debug.Log($"獻祭回復量アップグレード成功！新レベル: {farmer.SacrificeLevel}");
}
```

**実装箇所:** `farmer.cs:278-308`

---

#### アップグレード可能かチェック（農民専用項目）

**シグネチャ:**
```csharp
public bool CanUpgradeFarmer(FarmerUpgradeType type)
```

**パラメータ:**
- `type`: 農民専用アップグレード項目（`FarmerUpgradeType.Sacrifice`）

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達またはアップグレード不可

**使用例:**
```csharp
Farmer farmer = GetComponent<Farmer>();

// UIボタンの制御
sacrificeUpgradeButton.interactable = farmer.CanUpgradeFarmer(FarmerUpgradeType.Sacrifice);

// アップグレード前のチェック
if (farmer.CanUpgradeFarmer(FarmerUpgradeType.Sacrifice))
{
    int cost = farmer.GetFarmerUpgradeCost(FarmerUpgradeType.Sacrifice);
    // アップグレード処理...
}
```

**実装箇所:** `farmer.cs:330-334`

---

### 軍隊（Military）専用アップグレード

#### `UpgradeAttackPower()`
攻撃力をアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public bool UpgradeAttackPower()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
MilitaryUnit soldier = GetComponent<MilitaryUnit>();
int cost = soldier.GetMilitaryUpgradeCost(MilitaryUpgradeType.AttackPower);

// GameManager側でリソース消費を行った後
if (soldier.UpgradeAttackPower())
{
    Debug.Log($"攻撃力アップグレード成功！新レベル: {soldier.AttackPowerLevel}");
}
```

**実装箇所:** `military.cs:165-195`

---

#### アップグレード可能かチェック（軍隊専用項目）

**シグネチャ:**
```csharp
public bool CanUpgradeMilitary(MilitaryUpgradeType type)
```

**パラメータ:**
- `type`: 軍隊専用アップグレード項目（`MilitaryUpgradeType.AttackPower`）

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達またはアップグレード不可

**使用例:**
```csharp
MilitaryUnit soldier = GetComponent<MilitaryUnit>();

// UIボタンの制御
attackPowerUpgradeButton.interactable = soldier.CanUpgradeMilitary(MilitaryUpgradeType.AttackPower);
```

**実装箇所:** `military.cs:217-221`

---

### 宣教師（Missionary）専用アップグレード

宣教師は2つの独立したアップグレード項目を持ちます。

#### `UpgradeOccupy()`
領地占領成功率をアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public bool UpgradeOccupy()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
int cost = missionary.GetMissionaryUpgradeCost(MissionaryUpgradeType.Occupy);

// GameManager側でリソース消費を行った後
if (missionary.UpgradeOccupy())
{
    Debug.Log($"占領成功率アップグレード成功！新レベル: {missionary.OccupyLevel}");
}
```

**実装箇所:** `missionary.cs:283-313`

---

#### `UpgradeConvertEnemy()`
敵ユニット魅了成功率をアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public bool UpgradeConvertEnemy()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();
int cost = missionary.GetMissionaryUpgradeCost(MissionaryUpgradeType.ConvertEnemy);

// GameManager側でリソース消費を行った後
if (missionary.UpgradeConvertEnemy())
{
    Debug.Log($"魅惑成功率アップグレード成功！新レベル: {missionary.ConvertEnemyLevel}");
}
```

**実装箇所:** `missionary.cs:315-345`

---

#### アップグレード可能かチェック（宣教師専用項目）

**シグネチャ:**
```csharp
public bool CanUpgradeMissionary(MissionaryUpgradeType type)
```

**パラメータ:**
- `type`: 宣教師専用アップグレード項目
  - `MissionaryUpgradeType.Occupy` - 占領成功率
  - `MissionaryUpgradeType.ConvertEnemy` - 魅惑成功率

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達またはアップグレード不可

**使用例:**
```csharp
Missionary missionary = GetComponent<Missionary>();

// UIボタンの制御
occupyUpgradeButton.interactable = missionary.CanUpgradeMissionary(MissionaryUpgradeType.Occupy);
convertUpgradeButton.interactable = missionary.CanUpgradeMissionary(MissionaryUpgradeType.ConvertEnemy);
```

**実装箇所:** `missionary.cs:361-365`

---

### 教皇（Pope）専用アップグレード

教皇は2つの独立したアップグレード項目を持ちます。

#### `UpgradeSwapCooldown()`
位置交換クールダウンをアップグレードします（レベル0→2、最大2段階）。

**シグネチャ:**
```csharp
public bool UpgradeSwapCooldown()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Pope pope = GetComponent<Pope>();
int cost = pope.GetPopeUpgradeCost(PopeUpgradeType.SwapCooldown);

// GameManager側でリソース消費を行った後
if (pope.UpgradeSwapCooldown())
{
    Debug.Log($"位置交換CDアップグレード成功！新レベル: {pope.SwapCooldownLevel}");
}
```

**実装箇所:** `pope.cs:128-158`

---

#### `UpgradeBuff()`
周囲への味方バフ効果をアップグレードします（レベル0→3、最大3段階）。

**シグネチャ:**
```csharp
public bool UpgradeBuff()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、またはアップグレード不可

**使用例:**
```csharp
Pope pope = GetComponent<Pope>();
int cost = pope.GetPopeUpgradeCost(PopeUpgradeType.Buff);

// GameManager側でリソース消費を行った後
if (pope.UpgradeBuff())
{
    Debug.Log($"バフ効果アップグレード成功！新レベル: {pope.BuffLevel}");
}
```

**実装箇所:** `pope.cs:165-198`

---

#### アップグレード可能かチェック（教皇専用項目）

**シグネチャ:**
```csharp
public bool CanUpgradePope(PopeUpgradeType type)
```

**パラメータ:**
- `type`: 教皇専用アップグレード項目
  - `PopeUpgradeType.SwapCooldown` - 位置交換クールダウン
  - `PopeUpgradeType.Buff` - バフ効果

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達またはアップグレード不可

**使用例:**
```csharp
Pope pope = GetComponent<Pope>();

// UIボタンの制御
swapCooldownUpgradeButton.interactable = pope.CanUpgradePope(PopeUpgradeType.SwapCooldown);
buffUpgradeButton.interactable = pope.CanUpgradePope(PopeUpgradeType.Buff);
```

**実装箇所:** `pope.cs:226-230`

---

### 建物（Building）のアップグレード

建物は4つの独立したアップグレード項目を持ちます（各項目レベル0→2、最大2段階）。

#### `UpgradeHP()`
建物の最大HPをアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradeHP()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、建物未完成、またはアップグレード不可

**使用例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.HP);

// GameManager側でリソース消費を行った後
if (building.UpgradeHP())
{
    Debug.Log($"建物HPアップグレード成功！新レベル: {building.HPLevel}");
}
```

**実装箇所:** `building.cs:386-426`

---

#### `UpgradeAttackRange()`
建物の攻撃範囲をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradeAttackRange()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、建物未完成、またはアップグレード不可

**使用例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.AttackRange);

// GameManager側でリソース消費を行った後
if (building.UpgradeAttackRange())
{
    Debug.Log($"攻撃範囲アップグレード成功！新レベル: {building.AttackRangeLevel}");
}
```

**実装箇所:** `building.cs:432-470`

---

#### `UpgradeSlots()`
建物のスロット数（農民配置可能数）をアップグレードします。

**シグネチャ:**
```csharp
public bool UpgradeSlots()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、建物未完成、またはアップグレード不可

**使用例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.Slots);

// GameManager側でリソース消費を行った後
if (building.UpgradeSlots())
{
    Debug.Log($"スロット数アップグレード成功！新レベル: {building.SlotsLevel}");
}
```

**実装箇所:** `building.cs:476-525`

---

#### `UpgradeBuildCost()`
建物の建築コスト（必要AP）を削減します。

**シグネチャ:**
```csharp
public bool UpgradeBuildCost()
```

**戻り値:**
- `true`: アップグレード成功
- `false`: 最大レベル到達、建物未完成、またはアップグレード不可

**使用例:**
```csharp
Building building = GetComponent<Building>();
int cost = building.GetUpgradeCost(BuildingUpgradeType.BuildCost);

// GameManager側でリソース消費を行った後
if (building.UpgradeBuildCost())
{
    Debug.Log($"建築コスト削減アップグレード成功！新レベル: {building.BuildCostLevel}");
}
```

**実装箇所:** `building.cs:531-569`

---

#### アップグレード可能かチェック（建物）

**シグネチャ:**
```csharp
public bool CanUpgrade(BuildingUpgradeType type)
```

**パラメータ:**
- `type`: 建物アップグレード項目
  - `BuildingUpgradeType.HP` - 最大HP
  - `BuildingUpgradeType.AttackRange` - 攻撃範囲
  - `BuildingUpgradeType.Slots` - スロット数
  - `BuildingUpgradeType.BuildCost` - 建築コスト

**戻り値:**
- `true`: アップグレード可能
- `false`: 最大レベル到達、建物未完成、またはアップグレード不可

**使用例:**
```csharp
Building building = GetComponent<Building>();

// UIボタンの制御
hpUpgradeButton.interactable = building.CanUpgrade(BuildingUpgradeType.HP);
attackRangeUpgradeButton.interactable = building.CanUpgrade(BuildingUpgradeType.AttackRange);
slotsUpgradeButton.interactable = building.CanUpgrade(BuildingUpgradeType.Slots);
buildCostUpgradeButton.interactable = building.CanUpgrade(BuildingUpgradeType.BuildCost);

// アップグレード前のチェック
if (!building.CanUpgrade(BuildingUpgradeType.HP))
{
    Debug.Log("建物HPはアップグレードできません");
}
```

**注意:** 建物の場合、`BuildingState.Inactive`または`BuildingState.Active`状態でのみアップグレード可能です。建築中（`UnderConstruction`）や廃墟（`Ruined`）状態ではアップグレードできません。

**実装箇所:** `building.cs:606-613`

---

### アップグレードシステムの設計パターン

#### 1. 個別レベル管理
各アップグレード項目は独立したレベルフィールドを持ちます：
```csharp
// Piece基底クラス
private int hpLevel = 0;  // 0-3
private int apLevel = 0;  // 0-3

// 農民専用
private int sacrificeLevel = 0;  // 0-2

// 軍隊専用
private int attackPowerLevel = 0;  // 0-3

// 宣教師専用
private int occupyLevel = 0;  // 0-3
private int convertEnemyLevel = 0;  // 0-3

// 教皇専用
private int swapCooldownLevel = 0;  // 0-2
private int buffLevel = 0;  // 0-3

// 建物
private int hpLevel = 0;  // 0-2
private int attackRangeLevel = 0;  // 0-2
private int slotsLevel = 0;  // 0-2
private int buildCostLevel = 0;  // 0-2
```

#### 2. アップグレードコスト取得パターン
各クラスは自身のアップグレードコストを取得する関数を提供します：
```csharp
// 駒共通
public int GetUpgradeCost(PieceUpgradeType type)

// 職業別
public int GetFarmerUpgradeCost(FarmerUpgradeType type)
public int GetMilitaryUpgradeCost(MilitaryUpgradeType type)
public int GetMissionaryUpgradeCost(MissionaryUpgradeType type)
public int GetPopeUpgradeCost(PopeUpgradeType type)

// 建物
public int GetUpgradeCost(BuildingUpgradeType type)
```

戻り値が`-1`の場合、アップグレード不可（最大レベルまたはデータ未設定）を示します。

#### 3. リソース管理の分離
アップグレード関数はリソース消費を行わず、アップグレード実行のみを担当します：

```csharp
// GameManager側の実装例
public bool TryUpgradePieceHP(Piece piece)
{
    int cost = piece.GetUpgradeCost(PieceUpgradeType.HP);

    if (cost <= 0)
    {
        Debug.Log("アップグレード不可");
        return false;
    }

    if (!ConsumeResource(cost))
    {
        Debug.Log("リソース不足");
        return false;
    }

    // リソース消費後にアップグレード実行
    return piece.UpgradeHP();
}
```

---

### アップグレード項目一覧

| クラス | 項目 | レベル範囲 | 関数名 |
|--------|------|-----------|--------|
| **Piece（共通）** | HP | 0-3 | `UpgradeHP()` |
| **Piece（共通）** | AP | 0-3 | `UpgradeAP()` |
| **Farmer** | 獻祭回復量 | 0-2 | `UpgradeSacrifice()` |
| **Military** | 攻撃力 | 0-3 | `UpgradeAttackPower()` |
| **Missionary** | 占領成功率 | 0-3 | `UpgradeOccupy()` |
| **Missionary** | 魅惑成功率 | 0-3 | `UpgradeConvertEnemy()` |
| **Pope** | 位置交換CD | 0-2 | `UpgradeSwapCooldown()` |
| **Pope** | バフ効果 | 0-3 | `UpgradeBuff()` |
| **Building** | HP | 0-2 | `UpgradeHP()` |
| **Building** | 攻撃範囲 | 0-2 | `UpgradeAttackRange()` |
| **Building** | スロット数 | 0-2 | `UpgradeSlots()` |
| **Building** | 建築コスト | 0-2 | `UpgradeBuildCost()` |

合計: **13個のアップグレード関数**

---

## バージョン情報
- ドキュメント作成日: 2025-10-28
- プロジェクト: MSTen (2025)
- Unity Version: (プロジェクト設定に依存)

---

## 更新履歴
- 2025-10-28: 初版作成
- 2025-10-30: 個別項目のアップグレード機能セクション追加
