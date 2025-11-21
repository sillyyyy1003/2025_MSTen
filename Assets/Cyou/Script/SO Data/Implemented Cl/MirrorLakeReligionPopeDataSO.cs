// ===== 鏡湖教（Mirror Lake Religion）専用データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// 鏡湖教の教皇データ
    /// </summary>
    [CreateAssetMenu(fileName = "MirrorLakeReligionPopeData", menuName = "GameData/Religions/MirrorLakeReligion/PopeData")]
    public class MirrorLakeReligionPopeDataSO : PopeDataSO
    {
        private void Reset()
        {
            pieceName = "鏡湖教_教皇";
            canAttack = false;

            // リソースコストと人口コスト（教皇は通常購入不可だが念のため設定）
            resourceCost = 10;
            populationCost = 5;

            // 移動AP
            moveAPCost = 10;

            // HP・AP（初始, 升級1, 升級2, 升級3）
            // 注：教皇のAPは移動CDを意味する
            maxHPByLevel = new int[4] { 5, 7, 9, 9 };
            maxAPByLevel = new int[4] { 5, 4, 3, 3 }; // これはCDを表示用に設定（実際の移動CDはswapCooldown）

            // 各項目のアップグレードコスト
            hpUpgradeCost = new int[3] { 5, 6, 6 }; // 血量: 5→7(5資源), 7→9(6資源), 9→9(10資源、升級3なし)
            apUpgradeCost = new int[3] { 6, 8, 8 }; // 行動力(CD): 5回合→4回合(6資源), 4回合→3回合(8資源), 升級3空白

            // 位置交換CD
            swapCooldown = new int[4] { 5, 4, 3, 3 };
            swapCooldownUpgradeCost = new int[2] { 6, 8 }; // CD: 5回合→4回合(6資源), 4回合→3回合(8資源)

            // バフ効果（鏡湖教は防御力バフ）
            hpBuff = new int[4] { 0, 0, 0, 0 }; // 鏡湖教は血量バフなし
            atkBuff = new int[4] { 0, 0, 0, 0 }; // 鏡湖教は攻撃力バフなし
            convertBuff = new float[4] { 0f, 0f, 0f, 0f }; // 鏡湖教は魅惑バフなし
            defenseBuff = new float[4] { 0.10f, 0.15f, 0.20f, 0.25f }; // 防禦力+10%/+15%/+20%/+25%
            buffUpgradeCost = new int[3] { 6, 8, 10 }; // バフ(防禦力): +10%→+15%(6資源), +15%→+20%(8資源), +20%→+25%(10資源)
        }
    }
}
