// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
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
}