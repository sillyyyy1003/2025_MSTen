// ===== 瑪雅外星人文明教（Maya Alien Civilization Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瑪雅外星人文明教の十字軍データ
    /// </summary>
    [CreateAssetMenu(fileName = "MayaReligionMilitaryData", menuName = "GameData/Religions/MayaReligion/MilitaryData")]
    public class MayaReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "瑪雅外星人文明教_十字軍";
            canAttack = true;

            // リソースコストと人口コスト
            resourceCost = 6;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1;
            attackAPCost = 1;

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 10, 12, 15, 15 };
            maxAPByLevel = new int[4] { 5, 7, 7, 7 }; // Excel通り（升級2が空白）
            attackPowerByLevel = new int[4] { 2, 3, 5, 5 };

            // 各項目のアップグレードコスト (Excel行85-87)
            hpUpgradeCost = new int[3] { 7, 8, 0 }; // 血量: 10→12(7資源), 12→15(8資源), 升級3なし
            apUpgradeCost = new int[3] { 7, 0, 0 }; // 行動力: 5→7(7資源), 升級2空白, 升級3空白
            attackPowerUpgradeCost = new int[3] { 6, 8, 0 }; // 攻擊力: 2→3(6資源), 3→5(8資源), 升級3なし

            // 戦闘パラメータ
            criticalChance = 0.1f;
            damageType = DamageType.Physical;
            attackRange = 2f;

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };
        }
    }
}