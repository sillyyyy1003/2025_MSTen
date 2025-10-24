// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 紅月教の信徒データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionFarmerData", menuName = "GameData/Religions/RedMoonReligion/FarmerData")]
    public class RedMoonReligionFarmerDataSO : FarmerDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_農民";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 1;
            populationCost = 2;

            // 移動AP
            moveAPCost = 0f; // 領地内のみ移動

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 2f, 3f, 4f, 4f };
            maxAPByLevel = new float[4] { 3f, 5f, 5f, 5f };

            // 各項目のアップグレードコスト (Excel行61-63)
            hpUpgradeCost = new int[3] { 3, 4, 0 }; // 血量: 2→3(3資源), 3→4(4資源), 升級3なし
            apUpgradeCost = new int[3] { 4, 0, 0 }; // 行動力: 3→5(4資源), 升級2空白, 升級3空白

            // 獻祭回復量（FarmerDataSO独自の配列）
            maxSacrificeLevel = new int[3] { 1, 2, 2 };
            sacrificeUpgradeCost = new int[2] { 5, 0 }; // 獻祭: 1→2(5資源), 升級2空白

            devotionAPCost = 1;
            buildingSpeedModifier = 1.0f;
            productEfficiency = 1.0f;
        }
    }

    /// <summary>
    /// 紅月教の十字軍データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionMilitaryData", menuName = "GameData/Religions/RedMoonReligion/MilitaryData")]
    public class RedMoonReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_十字軍";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 4;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1f;
            attackAPCost = 1f;

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 8f, 10f, 13f, 13f };
            maxAPByLevel = new float[4] { 5f, 7f, 8f, 8f };
            attackPowerByLevel = new float[4] { 1f, 2f, 4f, 4f };

            // 各項目のアップグレードコスト (Excel行64-66)
            hpUpgradeCost = new int[3] { 6, 7, 0 }; // 血量: 8→10(6資源), 10→13(7資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 8, 0 }; // 行動力: 5→7(5資源), 7→8(8資源), 升級3なし
            attackPowerUpgradeCost = new int[3] { 6, 7, 0 }; // 攻擊力: 1→2(6資源), 2→4(7資源), 升級3なし

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
    /// 紅月教の宣教師データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionMissionaryData", menuName = "GameData/Religions/RedMoonReligion/MissionaryData")]
    public class RedMoonReligionMissionaryDataSO : MissionaryDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_宣教師";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 2;
            populationCost = 3;

            // 移動AP
            moveAPCost = 1f;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 5f, 7f, 10f, 10f };
            maxAPByLevel = new float[4] { 5f, 7f, 8f, 8f };

            // 各項目のアップグレードコスト (Excel行54-60)
            hpUpgradeCost = new int[3] { 5, 6, 0 }; // 血量: 5→7(5資源), 7→10(6資源), 升級3なし
            apUpgradeCost = new int[3] { 5, 6, 0 }; // 行動力: 5→7(5資源), 7→8(6資源), 升級3なし

            // 占領設定
            occupyAPCost = 2f;
            occupyEmptySuccessRateByLevel = new float[4] { 0.8f, 0.9f, 1.0f, 1.0f };
            occupyEnemySuccessRateByLevel = new float[4] { 0.5f, 0.6f, 0.7f, 0.7f };
            occupyUpgradeCost = new int[3] { 4, 5, 0 }; // 佔領空白: 0.8→0.9(4資源), 0.9→1.0(5資源), 升級3なし


            // 魅惑設定
            convertAPCost = 3;
            conversionTurnDuration = new int[4] { 2, 3, 4, 5 };

            // 魅惑成功率（Excel: 無, 0.55, 0.6, 0.7 / 無, 0.15, 0.25, 0.3 / 無, 0.5, 0.55, 0.6）
            convertMissionaryChanceByLevel = new float[4] { 0.0f, 0.55f, 0.6f, 0.7f };
            convertFarmerChanceByLevel = new float[4] { 0.0f, 0.15f, 0.25f, 0.3f };
            convertMilitaryChanceByLevel = new float[4] { 0.0f, 0.5f, 0.55f, 0.6f };
            convertEnemyUpgradeCost = new int[3] { 5, 6, 7 }; // 魅惑傳教士: 無→0.55(5資源), 0.55→0.6(6資源), 0.6→0.7(7資源)


            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            baseConversionDefenseChance = 0.2f;
        }
    }

    /// <summary>
    /// 紅月教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionPopeData", menuName = "GameData/Religions/RedMoonReligion/PopeData")]
    public class RedMoonReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "紅月教_教皇";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10f;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new float[4] { 3f, 5f, 7f, 7f };
            maxAPByLevel = new float[4] { 5f, 3f, 3f, 3f };

            // 各項目のアップグレードコスト (Excel行51-53)
            hpUpgradeCost = new int[3] { 6, 8, 0 }; // 血量: 3→5(6資源), 5→7(8資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 0, 0 }; // 行動力(CD): 5→3(6資源), 升級2空白, 升級3空白

            // 位置交換CD
            swapCooldown = new int[4] { 5, 3, 3, 3 };
            swapCooldownUpgradeCost = new int[2] { 6, 0 }; // CD: 5回合→3回合(6資源), 升級2空白

            // バフ効果（紅月教は魅惑機率バフ）
            hpBuff = new int[4] { 0, 0, 0, 0 }; // 紅月教は血量バフなし
            atkBuff = new int[4] { 0, 0, 0, 0 }; // 紅月教は攻撃バフなし
            convertBuff = new float[4] { 0.03f, 0.05f, 0.08f, 0.08f }; // 魅惑機率+3%/+5%/+8%
            buffUpgradeCost = new int[3] { 6, 8, 0 }; // バフ(魅惑機率): +3%→+5%(6資源), +5%→+8%(8資源), 升級3なし
        }
    }

    /// <summary>
    /// 紅月教の特殊建築データ
    /// </summary>
    [CreateAssetMenu(fileName = "RedMoonReligionBuildingData", menuName = "GameData/Religions/RedMoonReligion/BuildingData")]
    public class RedMoonReligionBuildingDataSO : BuildingDataSO
    {
        private void Reset()
        {
            buildingName = "紅月教_特殊建築";

            // HP（初始, 升級1, 升級2）
            maxHpByLevel = new int[3] { 25, 30, 45 };

            // 祭壇格子数（初始, 升級1, 升級2）
            maxSlotsByLevel = new int[3] { 4, 5, 6 };

            // 攻撃範囲
            attackRangeByLevel = new int[3] { 0, 1, 2 };

            // 建造所需花費
            buildingAPCostByLevel = new int[3] { 9, 6, 3 };

            // 各項目のアップグレードコスト (Excel行67-70)
            hpUpgradeCost = new int[2] { 6, 9 }; // 血量: 25→30(6資源), 30→45(9資源)
            attackRangeUpgradeCost = new int[2] { 8, 10 }; // 攻撃範囲: 無→範囲1(8資源), 範囲1→範囲2(10資源)
            slotsUpgradeCost = new int[2] { 8, 10 }; // 祭壇格子數: 4→5(8資源), 5→6(10資源)
            buildCostUpgradeCost = new int[2] { 6, 8 }; // 建造花費: 9→6(6資源), 6→3(8資源)

            // 資源生成設定
            buildStartAPCost = 1;
            resourceGenInterval = 1;
            apCostperTurn = 1;

            // 生産量（一般領地: 2, 金鉱領地: 5）
            generationType = ResourceGenerationType.Gold;
            baseProductionAmount = 2;
            goldenProductionAmount = 5;

            isSpecialBuilding = true;
        }
    }
}
