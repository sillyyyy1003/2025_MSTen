// ===== 絲織教（Silk Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 絲織教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionFarmerData", menuName = "GameData/Religions/SilkReligion/FarmerData")]
    public class SilkReligionFarmerDataSO : FarmerDataSO
    {
        // アセット作成時に一度だけ呼ばれる初期化メソッド（Inspectorでリセット時も呼ばれる）
        private void Reset()
        {
            pieceName = "絲織教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0f; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 3f, 4f, 5f, 5f };
            maxAPByLevel = new float[4] { 3f, 5f, 5f, 5f };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 3→4(3資源), 4→5(4資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 0, 0 }; // 行動力: 3→5(5資源), 升級2空白, 升級3空白

            // 獻祭回復量（FarmerDataSO独自の配列）
            maxSacrificeLevel = new int[3] { 1, 2, 2 };
            sacrificeUpgradeCost = new int[2] { 6, 0 }; // 獻祭: 1→2(6資源), 升級2空白

            devotionAPCost = 1;
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }

    /// <summary>
    /// 絲織教の十字軍データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionMilitaryData", menuName = "GameData/Religions/SilkReligion/MilitaryData")]
    public class SilkReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "絲織教_十字軍";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 5;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1f;
            attackAPCost = 1f;

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 10f, 12f, 15f, 15f };
            maxAPByLevel = new float[4] { 5f, 7f, 7f, 7f }; // Excel通り（升級2が空白のため升級1の値を使用）
            attackPowerByLevel = new float[4] { 2f, 3f, 4f, 4f };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 5, 6, 0 }; // 血量: 10→12(5資源), 12→15(6資源), 升級3なし
            apUpgradeCost = new int[3] { 7, 8, 0 }; // 行動力: 5→7(7資源), 7→空白(8資源), 升級3空白
            attackPowerUpgradeCost = new int[3] { 5, 6, 0 }; // 攻擊力: 2→3(5資源), 3→4(6資源), 升級3なし

            // 戦闘パラメータ
            armorValue = 10f;
            criticalChance = 0.1f;
            damageType = DamageType.Physical;
            attackRange = 2f;

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };
        }
    }

    /// <summary>
    /// 絲織教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionMissionaryData", menuName = "GameData/Religions/SilkReligion/MissionaryData")]
    public class SilkReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "絲織教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 3;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1f;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 7f, 9f, 12f, 12f };
            maxAPByLevel = new float[4] { 5f, 7f, 8f, 8f };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 4, 5, 0 }; // 血量: 7→9(4資源), 9→12(5資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 8, 0 }; // 行動力: 5→7(6資源), 7→8(8資源), 升級3なし

            // 占領設定
            occupyAPCost = 2f;
            occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f };
            occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f };
            occupyUpgradeCost = new int[3] { 5, 6, 0 }; // 佔領空白: 0.8→0.9(5資源), 0.9→1.0(6資源), 升級3なし

            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 無, 0.5, 0.6, 0.7 / 無, 0.1, 0.2, 0.3 / 無, 0.7, 0.8, 0.9）
            convertMissionaryChanceByLevel = new float[4] { 0.0f, 0.5f, 0.6f, 0.7f };
            convertFarmerChanceByLevel = new float[4] { 0.0f, 0.1f, 0.2f, 0.3f };
            convertMilitaryChanceByLevel = new float[4] { 0.0f, 0.7f, 0.8f, 0.9f };
            convertEnemyUpgradeCost = new int[3] { 6, 7, 8 }; // 魅惑傳教士: 無→0.5(6資源), 0.5→0.6(7資源), 0.6→0.7(8資源)

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }

    /// <summary>
    /// 絲織教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionPopeData", menuName = "GameData/Religions/SilkReligion/PopeData")]
    public class SilkReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "絲織教_教皇";
            canAttack = false;

            // リソースコストと人口コスト（教皇は通常購入不可だが念のため設定）
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10f;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            // 注：教皇のAPは移動CDを意味する
            maxHPByLevel = new float[4] { 5f, 7f, 9f, 9f };
            maxAPByLevel = new float[4] { 5f, 3f, 3f, 3f }; // これはCDを表示用に設定（実際の移動CDはswapCooldown）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 6, 8, 0 }; // 血量: 5→7(6資源), 7→9(8資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 0, 0 }; // 行動力(CD): 5→3(6資源), 升級2空白, 升級3空白

            // 位置交換CD
            swapCooldown = new int[4] { 5, 3, 3, 3 };
            swapCooldownUpgradeCost = new int[2] { 6, 0 }; // CD: 5回合→3回合(6資源), 升級2空白

            // バフ効果（絲織教は攻撃力バフ）
            hpBuff = new int[4] { 0, 0, 0, 0 }; // 絲織教は血量バフなし
            atkBuff = new int[4] { 1, 1, 1, 1 }; // 攻撃力+1
            convertBuff = new float[4] { 0f, 0f, 0f, 0f }; // 絲織教は魅惑バフなし
            buffUpgradeCost = new int[3] { 6, 8, 0 }; // バフ(攻擊力): +1→+1(6資源), +1→+1(8資源), 升級3なし
        }
    }

    /// <summary>
    /// 絲織教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionBuildingData", menuName = "GameData/Religions/SilkReligion/BuildingData")]
    public class SilkReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            buildingName = "絲織教_特殊建築";

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 45 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 3, 4, 5 };

            // 攻撃範囲
            attackRangeByLevel = new int[3] { 0, 1, 2 };

            // 建造所需花費（村民の建築AP消費量）
            buildingAPCostByLevel = new int[3] { 9, 6, 3 };

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[2] { 6, 8 }; // 血量: 25→30(6資源), 30→45(8資源)
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 攻撃範囲: 無→範囲1(8資源), 範囲1→範囲2(10資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 3→4(8資源), 4→5(10資源)
            buildCostUpgradeCost = new int[2] { 6, 8 }; // 建造花費: 9→6(6資源), 6→3(8資源)

            // 資源生成設定
            buildStartAPCost = 1;
            resourceGenInterval = 1; // 毎回合
            apCostperTurn = 1; // 1信徒につき1APコスト

            // 生産量（一般領地: 2, 金鉱領地: 4）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 4; // Excelでは金鉱領地は4資源と記載

            isSpecialBuilding = true;
        }
    }
}
