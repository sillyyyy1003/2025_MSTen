# Inspector操作マニュアル

このマニュアルでは、UnityのInspectorを使用してゲームデータを設定・管理する方法について説明します。

## 目次

1. [BuildingRegistry（建物レジストリ）](#buildingregistry建物レジストリ)
2. [PieceDataSO（駒データ）](#piecedataso駒データ)
3. [BuildingDataSO（建物データ）](#buildingdataso建物データ)
4. [SkillDataSO（スキルデータ）](#skilldatasoskillデータ)
5. [宗教別データの作成](#宗教別データの作成)

---

## BuildingRegistry（建物レジストリ）

### 概要
BuildingRegistryは、ゲーム内で建築可能な全ての建物データを一元管理するレジストリシステムです。

### 作成方法

1. Projectウィンドウで右クリック
2. `Create > GameData > BuildingRegistry` を選択
3. ファイル名を設定（例：`RI_BuildingRegistry`）

### Inspector設定項目

#### Building Data SO List（建物データリスト）
- **説明**: 建築可能な全ての建物のScriptableObjectを登録するリスト
- **設定方法**:
  1. リストのサイズを指定
  2. 各要素に対応するBuildingDataSOをドラッグ&ドロップ
  3. または、右の◎ボタンをクリックしてアセットを選択

### 使用方法

```csharp
// 特定の宗教の建物を取得
List<BuildingDataSO> silkBuildings = buildingRegistry.GetBuildingsByReligion(Religion.SilkReligion);

// 複数の宗教の建物を取得
List<BuildingDataSO> buildings = buildingRegistry.GetBuildingsByReligions(
    Religion.SilkReligion,
    Religion.RedMoonReligion
);
```

### 注意事項
- BuildingRegistryは必ず各BuildingDataSOに正しいReligion値が設定されている必要があります
- 複数のプレイヤーが異なる宗教を使用する場合は、全宗教の建物を登録してください

---

## PieceDataSO（駒データ）

### 基本クラス - PieceData

#### 作成方法
1. Projectウィンドウで右クリック
2. `Create > GameData > BasePieces > PieceData` を選択

#### Inspector設定項目

##### Prefab Path
- **piecePrefabResourcePath**: Resourcesフォルダからの相対パス（例：`Cyou/Prefab/farmer`）

##### 基本パラメータ
- **religion**: この駒の宗教（0=None, 1=SilkReligion, 2=RedMoonReligion, 3=MayaReligion, 4=MadScientistReligion）
- **originalPID**: ※非推奨フィールド（使用されていません）
- **pieceName**: 駒の名前（例：「絲織教_農民」）
- **populationCost**: この駒を生成する際の人口消費量
- **resourceCost**: この駒を生成する際の資源消費量
- **initialLevel**: 初期レベル（通常は0）

##### 行動力パラメータ
- **aPRecoveryRate**: 毎ターンのAP回復量
- **moveAPCost**: 移動に必要なAP
- **moveSpeed**: 移動速度

##### 戦闘パラメータ
- **canAttack**: 攻撃可能かどうか
- **attackPower**: 基本攻撃力（※レベル別配列を使用推奨）
- **attackRange**: 攻撃範囲
- **attackCooldown**: 攻撃のクールダウン時間（秒）
- **attackAPCost**: 攻撃に必要なAP

##### レベル別パラメータ（配列）
各配列は4つの要素を持ち、レベル0〜3のステータスを表します。

- **maxHPByLevel**: レベル別最大HP
  - 例：`[3, 4, 5, 5]` = レベル0:3HP、レベル1:4HP、レベル2:5HP、レベル3:5HP
- **maxAPByLevel**: レベル別最大AP
- **attackPowerByLevel**: レベル別攻撃力

##### アップグレードコスト（配列）
各配列は3つの要素を持ち、レベル0→1、1→2、2→3のコストを表します。

- **hpUpgradeCost**: HP強化の資源コスト
  - 例：`[3, 4, 0]` = 0→1:3資源、1→2:4資源、2→3:不可（0）
- **apUpgradeCost**: AP強化の資源コスト

##### Prefab
- **piecePrefab**: 実際のゲームオブジェクトPrefab

---

### 専用クラス

#### 1. FarmerData（農民データ）

**作成方法**: `Create > GameData > BasePieces > FarmerData`

**追加の設定項目**:

##### 建築能力
- **buildingSpeedModifier**: 建築速度修正値（通常は1.0）

##### 資源生産効率
- **productEfficiency**: 作業効率（通常は1.0）

##### 献身の治癒
- **devotionAPCost**: 他駒を回復するスキルのAP消費量

##### アップグレードレベルごとのデータ
- **maxSacrificeLevel**: レベル別の回復量（配列3要素）
  - 例：`[1, 2, 2]` = レベル0:1回復、レベル1:2回復、レベル2:2回復

##### 農民専用アップグレードコスト
- **sacrificeUpgradeCost**: 獻祭スキル強化の資源コスト（配列2要素）

---

#### 2. MilitaryData（軍隊データ）

**作成方法**: `Create > GameData > BasePieces > MilitaryData`

**追加の設定項目**:

##### 軍隊特有パラメータ
- **criticalChance**: クリティカル発生確率（0.0〜1.0）
- **damageType**: ダメージタイプ（Physical/Magical等）

##### スキル
- **availableSkills**: 使用可能なスキルの配列
- **skillAPCost**: スキル使用のAP消費量

##### アップグレードレベルごとのスキル効果
- **hasAntiConversionSkill**: レベル別の魅惑抵抗スキル有無（配列4要素、bool型）
  - 例：`[false, true, true, true]` = レベル0:無効、レベル1以降:有効

##### 軍隊専用アップグレードコスト
- **attackPowerUpgradeCost**: 攻撃力強化の資源コスト（配列3要素）

---

#### 3. MissionaryData（宣教師データ）

**作成方法**: `Create > GameData > BasePieces > MissionaryData`

**追加の設定項目**:

##### 占領設定
- **occupyAPCost**: 占領試行のAP消費量

##### 特殊攻撃設定
- **convertAPCost**: 変換攻撃のAP消費量
- **conversionTurnDuration**: 変換した駒が敵に戻るまでのターン数（配列4要素）

##### 特殊防御設定
- **baseConversionDefenseChance**: 基礎防御時変換確率（0.0〜1.0）

##### スキルレベル設定
すべて配列4要素（レベル0〜3）、float型（0.0〜1.0）

- **occupyEmptySuccessRateByLevel**: 空地占領成功率
  - 例：`[0.8, 0.9, 1.0, 1.0]`
- **occupyEnemySuccessRateByLevel**: 敵地占領成功率
- **convertMissionaryChanceByLevel**: 宣教師変換確率
  - レベル0は通常0.0（スキル未習得）
- **convertFarmerChanceByLevel**: 農民変換確率
- **convertMilitaryChanceByLevel**: 軍隊変換確率

##### アップグレード設定
- **hasAntiConversionSkill**: 魅惑抵抗スキル有無（配列4要素、bool型）
- **occupyUpgradeCost**: 占領スキル強化の資源コスト（配列3要素）
- **convertEnemyUpgradeCost**: 変換スキル強化の資源コスト（配列3要素）

---

#### 4. PopeData（教皇データ）

**作成方法**: `Create > GameData > BasePieces > PopeData`

**追加の設定項目**:

##### 位置交換能力
- **swapCooldown**: 位置交換のクールダウン（ターン数、配列4要素）
  - 例：`[5, 3, 3, 3]`

##### バフ効果一覧
すべて配列4要素（レベル0〜3）

- **hpBuff**: 周囲駒への体力バフ（int型）
  - 例：`[1, 2, 3, 3]` = +1HP、+2HP、+3HP、+3HP
- **atkBuff**: 周囲駒への攻撃力バフ（int型）
- **convertBuff**: 周囲宣教師への魅惑成功率バフ（float型）
  - 例：`[0.03, 0.05, 0.08, 0.08]` = +3%、+5%、+8%、+8%

##### 教皇専用アップグレードコスト
- **swapCooldownUpgradeCost**: 位置交換CD強化の資源コスト（配列2要素）
- **buffUpgradeCost**: バフ効果強化の資源コスト（配列3要素）

---

## BuildingDataSO（建物データ）

### 基本クラス - BuildingData

#### 作成方法
1. Projectウィンドウで右クリック
2. `Create > GameData > BaseBuilding > BuildingData` を選択

#### Inspector設定項目

##### Prefab Path
- **piecePrefabResourcePath**: Resourcesフォルダからの相対パス

##### 宗教
- **religion**: この建物の宗教（PieceDataSOと同じ値）

##### 基本属性
- **buildingName**: 建物名（例：「絲織教_特殊建築」）
- **maxHp**: 最大HP
- **buildStartAPCost**: 建築開始に必要なAP
- **buildingAPCost**: 建築完了に必要なAP
- **resourceGenInterval**: 資源生成間隔（ターン数）
- **cellType**: 配置可能な地形タイプ（通常/金鉱等）

##### 特殊建物属性
- **isSpecialBuilding**: 特別な建物か（バフ効果を持つ等）
- **maxUnitCapacity**: 駒を配置可能なスロット数

##### 生産設定
- **generationType**: 資源生成タイプ（通常/特殊等）
- **baseProductionAmount**: 基本生産量
- **goldenProductionAmount**: 金鉱地での生産量
- **apCostperTurn**: 毎ターンの農民AP消費量
- **productionMultiplier**: 生産量倍率（通常は1.0）

##### アップグレードレベルごとのデータ
各配列は3つの要素を持ち、レベル0〜2のステータスを表します。

- **maxHpByLevel**: レベル別最大HP
  - 例：`[25, 30, 45]`
- **attackRangeByLevel**: レベル別攻撃範囲
  - 例：`[0, 1, 2]` = 無し、範囲1、範囲2
- **maxSlotsByLevel**: レベル別最大スロット数
  - 例：`[3, 5, 5]`
- **buildingAPCostByLevel**: レベル別建築コスト
  - 例：`[9, 6, 3]`

##### 各項目のアップグレードコスト
各配列は2つの要素を持ち、レベル0→1、1→2のコストを表します。

- **hpUpgradeCost**: HP強化の資源コスト
  - 例：`[6, 8]` = 0→1:6資源、1→2:8資源
- **attackRangeUpgradeCost**: 攻撃範囲強化の資源コスト
- **slotsUpgradeCost**: スロット数強化の資源コスト
- **buildCostUpgradeCost**: 建築コスト軽減の資源コスト

##### Prefab
- **buildingPrefab**: 実際のゲームオブジェクトPrefab

---

## SkillDataSO（スキルデータ）

### 作成方法
1. Projectウィンドウで右クリック
2. `Create > GameData > Skills > SkillData` を選択

### Inspector設定項目

- **skillName**: スキル名
- **description**: スキルの説明文
- **aPCost**: スキル使用のAP消費量
- **coolDown**: スキルのクールダウン時間（秒）
- **effectValue**: スキルの効果値
- **skillType**: スキルタイプ（攻撃/回復/バフ等）
- **skillIcon**: スキルアイコンのSprite

---

## 宗教別データの作成

各宗教専用のデータを作成する際は、宗教別のメニューを使用します。

### 宗教一覧と対応する値

| 宗教名 | Religion値 | メニューパス |
|--------|-----------|-------------|
| 絲織教（SilkReligion） | 1 | `GameData/Religions/SilkReligion/...` |
| 紅月教（RedMoonReligion） | 2 | `GameData/Religions/RedMoonReligion/...` |
| マヤ教（MayaReligion） | 3 | `GameData/Religions/MayaReligion/...` |
| 瘋狂科學家教（MadScientistReligion） | 4 | `GameData/Religions/MadScientistReligion/...` |

### 宗教別データの作成手順

#### 例：絲織教の農民データを作成

1. Projectウィンドウで右クリック
2. `Create > GameData > Religions > SilkReligion > FarmerData` を選択
3. ファイル名を設定（例：`RI_SilkReligionFarmerData`）
4. Inspectorで **religion** フィールドを **1（SilkReligion）** に設定
5. その他のパラメータを設定

#### 利用可能な宗教別メニュー

各宗教について、以下のメニューが用意されています：

- `FarmerData` - 農民データ
- `MilitaryData` - 軍隊データ
- `MissionaryData` - 宣教師データ
- `PopeData` - 教皇データ
- `BuildingData` - 建物データ

### 注意事項

1. **Religion値の設定は必須**
   - すべてのPieceDataSOとBuildingDataSOには、正しいReligion値を設定してください
   - Religion値が0（None）のままだと、BuildingRegistryで正しく取得できません

2. **ファイル命名規則**
   - 推奨命名規則：`RI_{宗教名}{駒/建物タイプ}Data`
   - 例：`RI_SilkReligionFarmerData`、`RI_RedMoonReligionBuildingData`

3. **配列のサイズに注意**
   - 各レベル別配列やコスト配列は、正しいサイズを保つこと
   - サイズが不足すると実行時エラーが発生します

4. **Prefabパスの設定**
   - `piecePrefabResourcePath`は、必ず`Resources`フォルダからの相対パスを指定
   - 例：`Cyou/Prefab/farmer` → `Resources/Cyou/Prefab/farmer.prefab`

---

## よくある質問（FAQ）

### Q1: BuildingRegistryに建物を追加したのに、ゲーム内で表示されない

**A**: 以下を確認してください：
1. BuildingDataSOの`religion`フィールドが正しく設定されているか
2. BuildingManagerで正しいBuildingRegistryが参照されているか
3. `GetBuildingsByReligion()`に正しい宗教値を渡しているか

### Q2: レベルアップしてもステータスが変わらない

**A**: 以下を確認してください：
1. `maxHPByLevel`、`maxAPByLevel`等の配列に正しい値が設定されているか
2. 配列のサイズが4（Piece）または3（Building）になっているか
3. アップグレードコスト配列に0以外の値が設定されているか（0=アップグレード不可）

### Q3: 新しい宗教を追加したい

**A**: 以下の手順を実施してください：
1. `GameData/Religion.cs`に新しいenum値を追加
2. 各宗教専用DataSOクラスを作成（例：`NewReligionFarmerDataSO.cs`）
3. `CreateAssetMenu`属性で適切なメニューパスを設定
4. 新しい宗教のデータアセットを作成し、religion値を設定

### Q4: Inspectorで配列のサイズを変更したら、値がリセットされた

**A**: Unity標準の動作です。配列サイズ変更時は以下に注意：
1. サイズ変更前に現在の値をメモしておく
2. サイズ変更後、値を再設定する
3. または、最初から正しいサイズで作成する

---

## まとめ

このマニュアルでは、Unityのインスペクターを使用したゲームデータの設定方法について説明しました。

**重要なポイント**:
1. BuildingRegistryで全ての建物を一元管理
2. 各データには必ず正しいReligion値を設定
3. レベル別配列とアップグレードコスト配列のサイズに注意
4. 宗教別メニューを活用して、組織的にデータを作成

何か問題が発生した場合は、まずInspectorの設定値を確認し、FAQを参照してください。
