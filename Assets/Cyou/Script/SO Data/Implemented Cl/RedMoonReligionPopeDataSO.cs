// ===== 紅月教（Red Moon Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
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
}