// ===== 絲織教（Silk Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
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
}