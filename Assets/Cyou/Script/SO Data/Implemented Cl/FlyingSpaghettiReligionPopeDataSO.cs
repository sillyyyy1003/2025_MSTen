// ===== 飛天擔擔麵教（Flying Spaghetti Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 飛天擔擔麵教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingSpaghettiReligionPopeData", menuName = "GameData/Religions/FlyingSpaghettiReligion/PopeData")]
    public class FlyingSpaghettiReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "飛天擔擔麵教_教皇";
            canAttack = false;

            // リソースコストと人口コスト
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            maxHPByLevel = new int[4] { 5, 7, 9, 9 };
            maxAPByLevel = new int[4] { 5, 4, 4, 4 };

            // 各項目のアップグレードコスト (Excel行109-111)
            hpUpgradeCost = new int[3] { 6, 8, 0 }; // 血量: 5→7(6資源), 7→9(8資源), 升級3なし
            apUpgradeCost = new int[3] { 8, 10, 0 }; // 行動力(CD): 5回合→4回合(8資源), 升級2(10資源), 升級3なし

            // 位置交換CD（Excel: 5回合→4回合）
            swapCooldown = new int[4] { 5, 4, 4, 4 };
            swapCooldownUpgradeCost = new int[2] { 8, 10 }; // CD: 5回合→4回合(8資源), 升級2(10資源)

            // バフ効果（飛天擔擔麵教は攻擊力バフ+10%/+15%/+20%）
            hpBuff = new int[4] { 0, 0, 0, 0 }; // 飛天擔擔麵教は血量バフなし
            atkBuff = new int[4] { 1, 1, 1, 1 }; // 攻擊力バフ（注：％表記だが実装では整数値として扱う。実際の適用時に％計算）
            convertBuff = new float[4] { 0f, 0f, 0f, 0f }; // 飛天擔擔麵教は魅惑バフなし
            buffUpgradeCost = new int[3] { 10, 12, 0 }; // バフ(攻擊力): +10%→+15%(10資源), +15%→+20%(12資源), 升級3なし
        }
    }
}
