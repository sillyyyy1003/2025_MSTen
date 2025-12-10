using GameData;
using GameData.UI;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class SimpleSkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Header("UI")]
	public TMP_Text skillNameText;
	public TMP_Text levelText;
    public Image Outline;
    public Button upgradeButton;
    public GameObject hoverPrefab;

    private GameObject hoverInstance;
    private PieceType pieceType;
	private TechTree tech;
	private SimpleSkillPanel panel;
    private Color normalColor;
    private Color highlightedColor;
    private Color disabledColor;


    public void Initialize(PieceType type, TechTree techTree, SimpleSkillPanel panel)
	{
		this.pieceType = type;
		this.tech = techTree;
		this.panel = panel;

		//20251207 Lu改变字符取用
		skillNameText.text = PlayerUnitDataInterface.Instance.GetTechNameByTechTree(tech);

		upgradeButton.onClick.AddListener(OnClickUpgrade);

        SetupButtonColors(0);
        if (pieceType == PieceType.Pope) SetupButtonColors(1);

        Refresh();


	}

	/// <summary>
	/// 刷新 UI 显示
	/// </summary>
	public void Refresh()
	{

        EventSystem.current.SetSelectedGameObject(null);

        levelText.color = normalColor;
		skillNameText.color = normalColor;


        int currentLv = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, tech);
        levelText.text = $"Lv.{currentLv+1}";

        // 获取最大等级
        int maxLv = GetMaxLevel(pieceType, tech);

        if (currentLv >= maxLv-1)
        {
            levelText.text = $"MAX";
            levelText.color = disabledColor;
            skillNameText.color = disabledColor;
            upgradeButton.interactable = false;
            return;
        }

        // 获取升级消耗
        int cost = GetUpgradeCost(pieceType, tech, currentLv);

		// 判断资源够不够
		int playerID = GameManage.Instance.LocalPlayerID;
		int resource = PlayerDataManager.Instance.GetPlayerData(playerID).Resources;

		upgradeButton.interactable = resource >= cost;
		if(!upgradeButton.interactable)
		{
            levelText.color = disabledColor;
            skillNameText.color = disabledColor;
        }

    }

    void Update()
    {
        if (hoverInstance != null)
        {
            // 跟随当前鼠标
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GetComponentInParent<Canvas>().transform as RectTransform,
                Input.mousePosition,
                null,
                out pos
            );

            hoverInstance.GetComponent<RectTransform>().anchoredPosition = pos + new Vector2(80f, -30f);
        }
    }


    private void OnClickUpgrade()
	{
		int currentLv = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, tech);
		int cost = GetUpgradeCost(pieceType, tech, currentLv);

		// 扣资源
		int playerID = GameManage.Instance.LocalPlayerID;
		int res = PlayerDataManager.Instance.GetPlayerData(playerID).Resources;
		res -= cost;
		PlayerDataManager.Instance.SetPlayerResourses(res);

		// 提升等级
		SkillTreeUIManager.Instance.UpgradeCurrentLevel(pieceType, tech);
		SkillTreeUIManager.Instance.RefreshSkillTreeByPieceType(pieceType);

        // 升级单位 & 建筑等
        UpgradeRelatedUnits();

        // 刷新所有按钮
        panel.Refresh();

        // 刷新资源 UI
        GameUIManager.Instance.UpdateResourcesData();

		//关闭Cost小图标
        HideHoverPrefab();
    }

	//==============================================================
	// 工具函数：调用你现有的升级函数
	//==============================================================

	private void UpgradeRelatedUnits()
	{
		SkillNode dummy = new SkillNode();
		dummy.UpgradePieces(tech, pieceType);
	}

	private int GetMaxLevel(PieceType type, TechTree techTree)
	{
		if (type != PieceType.Building)
		{

			var so = UnitListTable.Instance.GetPieceDataSO(type,
				new UnitListTable.PieceDetail(type, SceneStateManager.Instance.PlayerReligion));

			switch (techTree)
			{
				case TechTree.HP: return GetActualCount(so.maxHPByLevel);
				case TechTree.AP: return GetActualCount(so.maxAPByLevel);

				case TechTree.Conversion:
					return GetActualCount((so as MissionaryDataSO).convertMissionaryChanceByLevel);

				case TechTree.Occupy:
					if (type == PieceType.Missionary)
						return GetActualCount((so as MissionaryDataSO).occupyEmptySuccessRateByLevel);
					if (type == PieceType.Farmer)
						return GetActualCount((so as FarmerDataSO).apUpgradeCost);
					break;

				case TechTree.ATK:
					return GetActualCount((so as MilitaryDataSO).attackPowerByLevel);

				case TechTree.Sacrifice:
					return GetActualCount((so as FarmerDataSO).maxSacrificeLevel);

				case TechTree.MovementCD:
					return GetActualCount((so as PopeDataSO).maxAPByLevel);

				case TechTree.AltarCount:
					return GetActualCount(GameManage.Instance._BuildingManager
						.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion)
						.slotsUpgradeCost);
			}

			return 1;
		}
		else
		{
			var buildingData =
				GameManage.Instance._BuildingManager.GetBuildingDataByReligion(SceneStateManager.Instance.PlayerReligion);
			switch (techTree)
			{
				case TechTree.HP:
					return GetActualCount(buildingData.maxHpByLevel);
				case TechTree.AltarCount:
					return GetActualCount(buildingData.maxSlotsByLevel);
			}

			return 1;
		}
	}

	private int GetUpgradeCost(PieceType type, TechTree techTree, int level)
	{
		return SkillTreeCostHelper.GetUpgradeCost(type, techTree, level);
	}

	private int GetActualCount<T>(T[] arr)
	{
		if (arr == null || arr.Length == 0) return 1;

		int count = 1;
		var cmp = EqualityComparer<T>.Default;
		for (int i = 1; i < arr.Length; i++)
		{
			if (cmp.Equals(arr[i], arr[i - 1])) break;
			count++;
		}
		return count;
	}



    // ========== Highlight 检测 ==========

    //颜色设定预组
    private void SetupButtonColors(int id)
    {
        ColorBlock cb = upgradeButton.colors;

        switch (id)
		{
			case 0://黑白预组

                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(96 / 255f, 96 / 255f, 96 / 255f, 225 / 255f);
                cb.pressedColor = Color.black;
                cb.disabledColor = new Color(1f, 1f, 1f, 0f);

                normalColor= new Color(56 / 255f, 56 / 255f, 56 / 255f, 225 / 255f);
                highlightedColor = Color.white;
                disabledColor = new Color(56 / 255f, 56 / 255f, 56 / 255f, 225 / 255f);


                upgradeButton.colors = cb;
                levelText.color = normalColor;
                skillNameText.color = normalColor;
				Outline.color = disabledColor;

                break;
            case 1://金色预组

                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(174 / 255f, 77 / 255f, 24 / 255f, 225 / 255f);
                cb.pressedColor = new Color(140 / 255f, 61 / 255f, 18 / 255f, 225 / 255f);
                cb.disabledColor = new Color(1f, 1f, 1f, 0f);

                normalColor = new Color(231 / 255f, 189 / 255f, 127 / 255f, 225 / 255f);
                highlightedColor = Color.white;
                disabledColor = Color.white;


                upgradeButton.colors = cb;
                levelText.color = normalColor;
                skillNameText.color = normalColor;
                Outline.color = disabledColor;

                break;

			default:
				break;


        }


    }

    // 鼠标移入（Highlight 开始）
    public void OnPointerEnter(PointerEventData eventData)
    {
		if (!upgradeButton.interactable) return;

        // --- 显示 Prefab ---
        ShowHoverPrefab(eventData);

        levelText.color = highlightedColor;
        skillNameText.color = highlightedColor;


    }

    // 鼠标移出（Highlight 结束）
    public void OnPointerExit(PointerEventData eventData)
    {
        HideHoverPrefab();

        if (!upgradeButton.interactable) return;

        Refresh();

    }

	//Cost小图标的开关和位置设定
    private void ShowHoverPrefab(PointerEventData eventData)
    {
        if (hoverPrefab == null) return;
        int currentLv = SkillTreeUIManager.Instance.GetCurrentLevel(pieceType, tech);
        int cost = GetUpgradeCost(pieceType, tech, currentLv);

        // 生成实例，并加入 Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        hoverInstance = Instantiate(hoverPrefab, canvas.transform);
        TMP_Text innerText = hoverInstance.GetComponentInChildren<TMP_Text>();
        innerText.text = $"{cost}";

        // 初始位置放到鼠标旁边
        UpdateHoverPosition(eventData);
    }
    private void HideHoverPrefab()
    {
        if (hoverInstance != null)
            Destroy(hoverInstance);
    }

    private void UpdateHoverPosition(PointerEventData eventData)
    {
        if (hoverInstance == null) return;

        RectTransform canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
        RectTransform hoverRect = hoverInstance.GetComponent<RectTransform>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // 偏移让提示框不遮住鼠标
        hoverRect.anchoredPosition = localPoint + new Vector2(80f, -30f);
    }
}


public static class SkillTreeCostHelper
{
	public static int GetUpgradeCost(PieceType pieceType, TechTree techTree, int level)
	{
		if (pieceType != PieceType.Building)
		{
			UnitListTable.PieceDetail detail =
				new UnitListTable.PieceDetail(pieceType, SceneStateManager.Instance.PlayerReligion);

			var so = UnitListTable.Instance.GetPieceDataSO(pieceType, detail);

			switch (pieceType)
			{
				case PieceType.Pope:
					var pope = so as PopeDataSO;
					if (techTree == TechTree.HP) return pope.hpUpgradeCost[level];
					if (techTree == TechTree.MovementCD) return pope.swapCooldownUpgradeCost[level];
					return 0;

				case PieceType.Missionary:
					var m = so as MissionaryDataSO;
					if (techTree == TechTree.HP) return m.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return m.apUpgradeCost[level];
					if (techTree == TechTree.Occupy) return m.occupyUpgradeCost[level];
					if (techTree == TechTree.Conversion) return m.convertEnemyUpgradeCost[level];
					return 0;

				case PieceType.Farmer:
					var f = so as FarmerDataSO;
					if (techTree == TechTree.HP) return f.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return f.apUpgradeCost[level];
					if (techTree == TechTree.Sacrifice) return f.sacrificeUpgradeCost[level];
					return 0;

				case PieceType.Military:
					var s = so as MilitaryDataSO;
					if (techTree == TechTree.HP) return s.hpUpgradeCost[level];
					if (techTree == TechTree.AP) return s.apUpgradeCost[level];
					if (techTree == TechTree.ATK) return s.attackPowerUpgradeCost[level];
					return 0;
			}

			return 0;
		}
		else
		{
		
			var b = GameManage.Instance._BuildingManager.GetBuildingDataByReligion(
				SceneStateManager.Instance.PlayerReligion);

			if (techTree == TechTree.HP) return b.hpUpgradeCost[level];
			if (techTree == TechTree.AltarCount) return b.slotsUpgradeCost[level];
			return 0;

		}
	}
	




}

