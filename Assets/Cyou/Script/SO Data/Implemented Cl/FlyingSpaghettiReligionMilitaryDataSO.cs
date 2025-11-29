// ===== 飛天擔擔麵教（Flying Spaghetti Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 飛天擔擔麵教の十字軍データ
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingSpaghettiReligionMilitaryData", menuName = "GameData/Religions/FlyingSpaghettiReligion/MilitaryData")]
    public class FlyingSpaghettiReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "飛天擔擔麵教_十字軍";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 5;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1;
            attackAPCost = 1;

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 10, 13, 18, 18 };
            maxAPByLevel = new int[4] { 5, 6, 7, 7 };
            attackPowerByLevel = new int[4] { 3, 4, 5, 5 };

            // 各項目のアップグレードコスト (Excel行122-124)
            hpUpgradeCost = new int[3] { 6, 9, 0 }; // 血量: 10→13(6資源), 13→18(9資源), 升級3なし
            apUpgradeCost = new int[3] { 8, 10, 0 }; // 行動力: 5→6(8資源), 6→7(10資源), 升級3なし
            attackPowerUpgradeCost = new int[3] { 8, 12, 0 }; // 攻擊力: 3→4(8資源), 4→5(12資源), 升級3なし

            // 戦闘パラメータ
            criticalChance = 0.1f;
            damageType = DamageType.Physical;
            attackRange = 2f;

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };

            // 特殊技能（初始數.csv では空欄のため、スキルなし）
            skillAPCost = 0;
        }
    }
}
