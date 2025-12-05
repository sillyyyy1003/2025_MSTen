using UnityEngine;
using GameData;

/// <summary>
/// GetBuildingDataByReligion関数のテストスクリプト
/// 使い方：このスクリプトを適当なGameObjectにアタッチして、InspectorでBuildingManagerを設定し、「Run Test」ボタンを押す
/// </summary>
public class BuildingDataByReligionTest : MonoBehaviour
{
    [Header("必須設定")]
    [SerializeField] private BuildingManager buildingManager;

    [Header("テスト設定")]
    [SerializeField] private bool runOnStart = false;

    private void Start()
    {
        if (runOnStart)
        {
            RunTest();
        }
    }

    /// <summary>
    /// テストを実行
    /// </summary>
    [ContextMenu("Run Test")]
    public void RunTest()
    {
        Debug.Log("=== GetBuildingDataByReligion テスト開始 ===");

        if (buildingManager == null)
        {
            Debug.LogError("BuildingManager が設定されていません。InspectorでBuildingManagerを設定してください。");
            return;
        }

        int passCount = 0;
        int failCount = 0;

        // 各宗教をテスト
        passCount += TestReligion(Religion.SilkReligion, "絲織教_特殊建築") ? 1 : 0;
        failCount += TestReligion(Religion.SilkReligion, "絲織教_特殊建築") ? 0 : 1;

        passCount += TestReligion(Religion.RedMoonReligion, "紅月教_特殊建築") ? 1 : 0;
        failCount += TestReligion(Religion.RedMoonReligion, "紅月教_特殊建築") ? 0 : 1;

        passCount += TestReligion(Religion.MayaReligion, "瑪雅外星人文明教_特殊建築") ? 1 : 0;
        failCount += TestReligion(Religion.MayaReligion, "瑪雅外星人文明教_特殊建築") ? 0 : 1;

        passCount += TestReligion(Religion.MadScientistReligion, "瘋狂科學家教_特殊建築") ? 1 : 0;
        failCount += TestReligion(Religion.MadScientistReligion, "瘋狂科學家教_特殊建築") ? 0 : 1;

        passCount += TestReligion(Religion.MirrorLakeReligion, "鏡湖教_特殊建築") ? 1 : 0;
        failCount += TestReligion(Religion.MirrorLakeReligion, "鏡湖教_特殊建築") ? 0 : 1;

        // None宗教のテスト（nullが返されるべき）
        passCount += TestNoneReligion() ? 1 : 0;
        failCount += TestNoneReligion() ? 0 : 1;

        Debug.Log($"=== テスト完了 ===");
        Debug.Log($"成功: {passCount}, 失敗: {failCount}");

        if (failCount == 0)
        {
            Debug.Log("<color=green>全てのテストが成功しました！</color>");
        }
        else
        {
            Debug.LogError($"<color=red>{failCount}個のテストが失敗しました</color>");
        }
    }

    /// <summary>
    /// 指定された宗教のテスト
    /// </summary>
    private bool TestReligion(Religion religion, string expectedName)
    {
        Debug.Log($"\n--- {religion} のテスト ---");

        BuildingDataSO buildingData = buildingManager.GetBuildingDataByReligion(religion);

        if (buildingData == null)
        {
            Debug.LogError($"❌ {religion}: BuildingDataSO が null です");
            return false;
        }

        bool allPassed = true;

        // buildingName のチェック
        if (buildingData.buildingName == expectedName)
        {
            Debug.Log($"✓ buildingName: {buildingData.buildingName}");
        }
        else
        {
            Debug.LogError($"❌ buildingName: 期待値={expectedName}, 実際={buildingData.buildingName}");
            allPassed = false;
        }

        // buildingResourceCost のチェック
        if (buildingData.buildingResourceCost == 12)
        {
            Debug.Log($"✓ buildingResourceCost: {buildingData.buildingResourceCost}");
        }
        else
        {
            Debug.LogError($"❌ buildingResourceCost: 期待値=12, 実際={buildingData.buildingResourceCost}");
            allPassed = false;
        }

        // religion のチェック
        if (buildingData.religion == religion)
        {
            Debug.Log($"✓ religion: {buildingData.religion}");
        }
        else
        {
            Debug.LogError($"❌ religion: 期待値={religion}, 実際={buildingData.religion}");
            allPassed = false;
        }

        // maxHpByLevel のチェック
        if (buildingData.maxHpByLevel != null && buildingData.maxHpByLevel.Length == 3)
        {
            Debug.Log($"✓ maxHpByLevel: [{buildingData.maxHpByLevel[0]}, {buildingData.maxHpByLevel[1]}, {buildingData.maxHpByLevel[2]}]");
        }
        else
        {
            Debug.LogError($"❌ maxHpByLevel が正しく設定されていません");
            allPassed = false;
        }

        if (allPassed)
        {
            Debug.Log($"<color=green>✓ {religion} のテストが成功しました</color>");
        }
        else
        {
            Debug.LogError($"<color=red>❌ {religion} のテストが失敗しました</color>");
        }

        return allPassed;
    }

    /// <summary>
    /// None宗教のテスト（nullが返されるべき）
    /// </summary>
    private bool TestNoneReligion()
    {
        Debug.Log($"\n--- Religion.None のテスト ---");

        BuildingDataSO buildingData = buildingManager.GetBuildingDataByReligion(Religion.None);

        if (buildingData == null)
        {
            Debug.Log($"<color=green>✓ Religion.None は正しく null を返しました</color>");
            return true;
        }
        else
        {
            Debug.LogError($"<color=red>❌ Religion.None が null を返しませんでした。実際: {buildingData.buildingName}</color>");
            return false;
        }
    }

    /// <summary>
    /// 宗教別コスト一覧を表示
    /// </summary>
    [ContextMenu("Show All Building Costs")]
    public void ShowAllBuildingCosts()
    {
        Debug.Log("=== 全宗教の建物コスト ===");

        if (buildingManager == null)
        {
            Debug.LogError("BuildingManager が設定されていません。InspectorでBuildingManagerを設定してください。");
            return;
        }

        Religion[] religions = new Religion[]
        {
            Religion.SilkReligion,
            Religion.RedMoonReligion,
            Religion.MayaReligion,
            Religion.MadScientistReligion,
            Religion.MirrorLakeReligion
        };

        foreach (Religion religion in religions)
        {
            BuildingDataSO buildingData = buildingManager.GetBuildingDataByReligion(religion);
            if (buildingData != null)
            {
                Debug.Log($"{religion}: {buildingData.buildingName} = {buildingData.buildingResourceCost} 資源");
            }
        }
    }
}
