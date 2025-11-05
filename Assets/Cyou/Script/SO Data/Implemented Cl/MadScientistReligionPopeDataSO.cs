// ===== 瘋狂科學家教（Mad Scientist Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 瘋狂科學家教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "MadScientistReligionPopeData", menuName = "GameData/Religions/MadScientistReligion/PopeData")]
    public class MadScientistReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "瘋狂科學家教_教皇";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 5, 7, 9, 9 };
            maxAPByLevel = new int[4] { 5, 3, 3, 3 };

            // 各項目のアップグレードコスト (Excel行94-96)
            hpUpgradeCost = new int[3] { 6, 8, 0 }; // 血量: 5→7(6資源), 7→9(8資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 0, 0 }; // 行動力(CD): 5→3(6資源), 升級2空白, 升級3空白

            // 位置交換CD
            swapCooldown = new int[4] { 5, 3, 3, 3 };
            swapCooldownUpgradeCost = new int[2] { 6, 0 }; // CD: 5回合→3回合(6資源), 升級2空白

            // バフ効果（瘋狂科學家教は血量バフ）
            hpBuff = new int[4] { 1, 2, 3, 3 }; // 血量+1/+2/+3
            atkBuff = new int[4] { 0, 0, 0, 0 }; // 瘋狂科學家教は攻撃バフなし
            convertBuff = new float[4] { 0f, 0f, 0f, 0f }; // 瘋狂科學家教は魅惑バフなし
            buffUpgradeCost = new int[3] { 6, 8, 0 }; // バフ(血量): +1→+2(6資源), +2→+3(8資源), 升級3なし
        }
    }
}