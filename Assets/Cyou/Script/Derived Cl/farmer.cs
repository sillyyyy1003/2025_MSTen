using Buildings;
using GameData;
using GamePieces;

using UnityEngine;

/// <summary>
/// 農民クラス
/// </summary>
public class Farmer : Piece
{
    private FarmerDataSO farmerData;
    private Building currentBuilding; //現在在籍中の建物



    public override void Initialize(PieceDataSO data, int playerID)
    {
        farmerData = data as FarmerDataSO;
        if (farmerData == null)
        {
            Debug.LogError("農民にはFarmerDataSOが必要です");
            return;
        }

        base.Initialize(data, playerID);
    }

    /// <summary>
    /// スキルレベルを設定
    /// </summary>
    public void SetSkillLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 1, farmerData.maxAPByLevel.Length);
    }

    /// <summary>
    /// スキルレベルをレベルアップ
    /// </summary>
    public void LevelUp()
    {
        if (UpgradeLevel < farmerData.maxAPByLevel.Length)
        {
            upgradeLevel++;
        }
    }

    /// <summary>
    /// AP消費し他駒を回復するスキル
    /// </summary>
    public void Sacrifice(Piece target)
    {
        if (currentState==PieceState.Idle&&target.IsAlive)
        {
            currentAP--;
            target.Heal(farmerData.maxSacrificeLevel[UpgradeLevel]);
        }
    }





    /// <summary>
    /// 生産倍率を取得（スキルレベルに基づく）←（廃止）
    /// </summary>

    /// <summary>
    /// 建物を建築（仮）
    /// </summary>
    public bool StartConstruction(int buildingIndex, Vector3 position)
    {
        if (farmerData == null || currentState != PieceState.Idle)
            return false;

        if (buildingIndex < 0 || buildingIndex >= farmerData.Buildings.Length)
        {
            Debug.LogError($"無効な建物インデックス: {buildingIndex}");
            return false;
        }

        ///現時点SOデータにてハードコーディングされているが
        ///今後GameManagerにリストを要求するように移行する。
        var buildingData = farmerData.Buildings[buildingIndex];

        // 行動力チェック
        if (currentAP < buildingData.buildStartAPCost)
            return false;

        // 行動力消費
        ConsumeAP(buildingData.buildStartAPCost);

        // 建物生成
        var building = BuildingFactory.CreateBuilding(buildingData, position);
        if (building != null)
        {
            ChangeState(PieceState.Building);
            if (CurrentAP >0&&CurrentAP<building.RemainingBuildCost)
            {
                building.ProgressConstruction((int)CurrentAP);//
                currentAP = 0;
                ChangeState(PieceState.Dead);
            }
            else if (CurrentAP > 0 && currentAP >= building.RemainingBuildCost)
            {
                int buildCos = building.RemainingBuildCost;
                building.ProgressConstruction((int)CurrentAP);
                currentAP-=buildCos;
                ChangeState(PieceState.Idle);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 既存の建築中建物の建築を継続
    /// 複数の農民を選択してこれを実行させることが必要だと思うが
    /// それはGMに任せる
    /// </summary>
    public bool ContinueConstruction(Building building)
    {
        if (building == null || currentState != PieceState.Idle)
            return false;

        if (building.State != Buildings.BuildingState.UnderConstruction)
        {
            Debug.LogWarning("建物は建築中ではありません");
            return false;
        }

        if (currentAP <= 0)
        {
            Debug.LogWarning("農民の行動力が不足しています");
            return false;
        }

        // 現在の行動力を建築に投入
        int aPToInvest = Mathf.RoundToInt(currentAP);
        int remainingCost = building.RemainingBuildCost;

        if (aPToInvest >= remainingCost)
        {
            // 建築完了
            building.ProgressConstruction(remainingCost);
            ConsumeAP(remainingCost);
            ChangeState(PieceState.Idle);
            Debug.Log($"農民が建築を完了しました。使用行動力: {remainingCost}, 残り行動力: {currentAP}");
        }
        else
        {
            // 行動力を全て使って建築進行、農民は死亡
            building.ProgressConstruction(aPToInvest);
            ConsumeAP(aPToInvest);
            ChangeState(PieceState.Dead);
            Debug.Log($"農民が建築に全力を注ぎました。使用行動力: {aPToInvest}, 残り建築コスト: {building.RemainingBuildCost}");
        }

        return true;
    }

    /// <summary>
    /// 建物に入る
    /// </summary>
    public bool EnterBuilding(Building building)
    {
        if (building == null || !building.IsOperational)
        {
            Debug.Log("建物が無いまたは建物が稼働可能な状態になっていません。");
            return false;
        }
            

        if (building.AssignFarmer(this))
        {
            currentBuilding = building;
            ChangeState(PieceState.InBuilding);
            Debug.Log("農民を建物の中に入れました");
            // 視覚的に非表示に
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 建物から出る際の処理
    /// </summary>
    public void OnExitBuilding()
    {
        currentBuilding = null;

        // 行動力が0の場合は死亡
        if (currentAP <= 0)
        {
            // 死亡処理前にGameObjectをアクティブにする
            gameObject.SetActive(true);
            Die();
        }
        else
        {
            ChangeState(PieceState.Idle);
            gameObject.SetActive(true);
        }
    }

    #region アップグレード管理

    /// <summary>
    /// アップグレード効果を適用
    /// </summary>
    protected override void ApplyUpgradeEffects()
    {
        if (farmerData == null) return;

        // レベルに応じてHP、AP、攻撃力を更新
        float newMaxHP = farmerData.GetMaxHPByLevel(upgradeLevel);
        float newMaxAP = farmerData.GetMaxAPByLevel(upgradeLevel);

        // 現在のHPとAPの割合を保持
        float hpRatio = currentHP / currentMaxHP;
        float apRatio = currentAP / currentMaxAP;

        // 新しい最大値に基づいて現在値を更新
        currentHP = newMaxHP * hpRatio;
        

        Debug.Log($"信徒のアップグレード効果適用: レベル{upgradeLevel} HP={newMaxHP}, AP={newMaxAP}");

        // 新しいステータスのログ
        if (upgradeLevel == 1)
        {
            Debug.Log("血量: 4, 行動力: 5");
        }
        else if (upgradeLevel == 2)
        {
            Debug.Log("血量: 5, 行動力: 未記載");
        }
    }

    #endregion
}