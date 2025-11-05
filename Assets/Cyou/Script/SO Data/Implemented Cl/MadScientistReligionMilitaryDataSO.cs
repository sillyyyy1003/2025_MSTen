// ===== 瘋狂科學家教（Mad Scientist Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瘋狂科學家教の十字軍データ
    /// </summary>
    [CreateAssetMenu(fileName = "MadScientistReligionMilitaryData", menuName = "GameData/Religions/MadScientistReligion/MilitaryData")]
    public class MadScientistReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "瘋狂科學家教_十字軍";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 6;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1;
            attackAPCost = 1;

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 10, 12, 15, 15 };
            maxAPByLevel = new int[4] { 5, 7, 8, 8 };
            attackPowerByLevel = new int[4] { 2, 3, 4, 4 };

            // 各項目のアップグレードコスト (基礎数値と同じ推測)
            hpUpgradeCost = new int[3] { 5, 6, 0 }; // 血量: 10→12, 12→15, 升級3なし
            apUpgradeCost = new int[3] { 7, 8, 0 }; // 行動力: 5→7, 7→8, 升級3なし
            attackPowerUpgradeCost = new int[3] { 5, 6, 0 }; // 攻擊力: 2→3, 3→4, 升級3なし

            // 戦闘パラメータ
            criticalChance = 0.1f;
            damageType = DamageType.Physical;
            attackRange = 2f;

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };
        }
    }
}