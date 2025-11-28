// ===== 鏡湖教（Mirror Lake Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 鏡湖教の十字軍データ
    /// 特徴：自分から攻撃できず、反撃のみ可能
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorLakeReligionMilitaryData", menuName = "GameData/Religions/MirrorLakeReligion/MilitaryData")]
    public class MirrorLakeReligionMilitaryDataSO : MilitaryDataSO
    {
        private void Reset()
        {
            pieceName = "鏡湖教_十字軍";
            canAttack = false; // 鏡湖教の十字軍は自分から攻撃できない（反撃のみ）

            // リソースコストと人口コスト
            resourceCost = 5;
            populationCost = 3;

            // 移動・攻撃AP
            moveAPCost = 1;
            attackAPCost = 0; // 攻撃しないため0

            // HP・AP・攻撃力（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 12, 14, 16, 18 };
            maxAPByLevel = new int[4] { 5, 7, 8, 8 };
            attackPowerByLevel = new int[4] { 0, 0, 0, 0 }; // 普攻攻撃力: 無（0）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 5, 6, 7 }; // 血量: 12→14(5資源), 14→16(6資源), 16→18(7資源)
            apUpgradeCost = new int[3] { 5, 6, 0 }; // 行動力: 5→7(5資源), 7→8(6資源), 升級3空白
            attackPowerUpgradeCost = new int[3] { 0, 0, 0 }; // 攻撃力アップグレードなし

            // 戦闘パラメータ
            criticalChance = 0f; // 攻撃しないためクリティカルなし
            damageType = DamageType.Physical;
            attackRange = 0f; // 攻撃範囲なし（反撃は別処理）

            // 魅惑耐性（升級1以降）
            hasAntiConversionSkill = new bool[4] { false, true, true, true };
        }
    }
}
