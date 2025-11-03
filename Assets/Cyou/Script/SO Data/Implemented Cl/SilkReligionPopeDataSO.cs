// ===== 絲織教（Silk Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 絲織教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "SilkReligionPopeData", menuName = "GameData/Religions/SilkReligion/PopeData")]
    public class SilkReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "絲織教_教皇";
            canAttack = false;

            // リソースコストと人口コスト（教皇は通常購入不可だが念のため設定）
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            // 注：教皇のAPは移動CDを意味する
            maxHPByLevel = new int[4] { 5, 7, 9, 9 };
            maxAPByLevel = new int[4] { 5, 3, 3, 3 }; // これはCDを表示用に設定（実際の移動CDはswapCooldown）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 6, 8, 0 }; // 血量: 5→7(6資源), 7→9(8資源), 升級3なし
            apUpgradeCost = new int[3] { 6, 0, 0 }; // 行動力(CD): 5→3(6資源), 升級2空白, 升級3空白

            // 位置交換CD
            swapCooldown = new int[4] { 5, 3, 3, 3 };
            swapCooldownUpgradeCost = new int[2] { 6, 0 }; // CD: 5回合→3回合(6資源), 升級2空白

            // バフ効果（絲織教は攻撃力バフ）
            hpBuff = new int[4] { 0, 0, 0, 0 }; // 絲織教は血量バフなし
            atkBuff = new int[4] { 1, 1, 1, 1 }; // 攻撃力+1
            convertBuff = new float[4] { 0f, 0f, 0f, 0f }; // 絲織教は魅惑バフなし
            buffUpgradeCost = new int[3] { 6, 8, 0 }; // バフ(攻擊力): +1→+1(6資源), +1→+1(8資源), 升級3なし
        }
    }
}