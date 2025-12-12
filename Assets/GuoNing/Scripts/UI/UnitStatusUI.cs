using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 负责管理玩家的单位状态UI（血量和行动力）
/// </summary>
public class UnitStatusUI : MonoBehaviour
{
	//================================
	// 変数
	//================================
	private Transform pieceTarget;   // 对应的棋子
	private Vector3 offset;          // 头顶偏移

	private int maxHP;
	private int currentHP;
	public int CurrentHP => currentHP;

	private int maxAP;
	private int currentAP;
	public int CurrentAP => currentAP;

	//================================
	// プロパティ
	//================================
	[Header("HP UI")]
	[SerializeField] private Image hpImage;
	[SerializeField] private TMP_Text hpText;
	[SerializeField] private RectTransform hpBarTransform;

	[Header("AP UI")]
	[SerializeField] private Image apImage;
	[SerializeField] private RectTransform apBarTransform;
	[SerializeField] private TMP_Text apText;

	[Header("PieceIcon")]
	[SerializeField] private Image pieceIcon;

	[Header("BuildingIcon")]
	[SerializeField] private BuildingSlot slotPrefab;

	[Header("UIBaseScale")]
	[SerializeField] private float uiBaseScale = 0.01f;
	[SerializeField] private float minOffsetFactor = 0.3f; // 最近时 offset 缩放比例
	[SerializeField] private float minDistance = 5f;  // 最近距离
	[SerializeField] private float maxDistance = 30f; // 最远距离

	List<BuildingSlot> buildingSlots = new List<BuildingSlot>();



	//================================
	// メソッド
	//================================
	public void Initialize(
		int maxHP,
		int maxAP,
		Transform target,
		Vector3 offset,
		CardType type, bool isEnemy = false, int buildingSlot = 0)
	{
		this.pieceTarget = target;
		this.offset = offset;

		this.maxHP = maxHP;
		this.currentHP = maxHP;

		this.maxAP = maxAP;
		this.currentAP = maxAP;

		// 敌我颜色区分
		if(isEnemy)
		{
			if (hpImage != null)
				hpImage.color =new Color(0.89f, 0.0f, 0.0f);
		}
		else
		{
			if (hpImage != null)
				hpImage.color = new Color(0.1215f, 0.6431f, 0.8666f);
		}
		if (type == CardType.Building)
		{
			buildingSlots.Clear();
			buildingSlots.Add(slotPrefab);

			RectTransform prefabRT = slotPrefab.GetComponent<RectTransform>();
			float width = prefabRT.rect.width;

			for (int i = 0; i < buildingSlot - 1; i++)
			{
				BuildingSlot slot = Instantiate(slotPrefab, transform);
				buildingSlots.Add(slot);
			}

			RefreshSlotLayout();
		}
		else
		{
			slotPrefab.gameObject.SetActive(false);
		}
		
		// 初始刷新
		UpdateHPUI();
		UpdateAPUI();
		UpdatePieceIcon(type);
	}

	/// <summary>
	/// 激活建筑槽位
	/// </summary>
	/// <param name="index"></param>
	/// <param name="isActive"></param>
	public void ActivateSlot(int index)
    {
        //25.12.9 ri change index logic 
        if (index < 0 || index > buildingSlots.Count) return;

		for (int i = 0; i < buildingSlots.Count; i++)
		{
            if (buildingSlots[i].IsActivated)
			{
				continue;
			}
			else
			{
                buildingSlots[i].ActivateSlot();
				break;
            }

            //buildingSlots[index].ActivateSlot();
        } 
	}

	/// <summary>
	/// 关闭指定的建筑槽位
	/// </summary>
	/// <param name="index"></param>
	public void CloseSlot(int index)
	{
		if (index < 0 || index >= buildingSlots.Count) return;

        // 关闭并删除
        //// 25.12.9 RI change destory logic
        for (int i=0;i< buildingSlots.Count;i++)
		{
			if (buildingSlots[i].IsActivated && buildingSlots[i].IsActivated)
			{
                buildingSlots[i].CloseSlot();
                buildingSlots.RemoveAt(i);
                break;
            }

        }
		//buildingSlots[index].CloseSlot();
		////Destroy(buildingSlots[index].gameObject);
		////buildingSlots[index].gameObject.SetActive(false);


		// 重新排列剩余槽位
		RefreshSlotLayout();
	}

	/// <summary>
	/// 增加制定数量的建筑槽位
	/// </summary>
	/// <param name="slotNumber"></param>
	public void IncreaseSlot(int slotNumber)
	{
		if (slotNumber <= 0) return;

		RectTransform prefabRT = slotPrefab.GetComponent<RectTransform>();

		for (int i = 0; i < slotNumber; i++)
		{
			BuildingSlot slot = Instantiate(slotPrefab, transform);
			slot.gameObject.SetActive(true);
			buildingSlots.Add(slot);
		}

		// 重新排列所有槽位
		RefreshSlotLayout();
	}


	public void SetHP(int hp)
	{
		currentHP = Mathf.Clamp(hp, 0, maxHP);
		UpdateHPUI();
	}

	public void SetHP(int hp, int newMaxHP)
	{
		maxHP = Mathf.Max(1, newMaxHP);
		currentHP = Mathf.Clamp(hp, 0, maxHP);
		UpdateHPUI();
	}

	public void SetAP(int ap)
	{
		currentAP = Mathf.Clamp(ap, 0, maxAP);
		UpdateAPUI();
	}

	public void SetAP(int ap, int newMaxAP)
	{
		maxAP = Mathf.Max(0, newMaxAP);
		currentAP = Mathf.Clamp(ap, 0, maxAP);
		UpdateAPUI();
	}

	private void UpdateHPUI()
	{
		if (hpImage != null)
			hpImage.fillAmount = (float)currentHP / maxHP;

		if (hpText != null)
			hpText.text = $"{currentHP}/{maxHP}";
	}

	private void UpdateAPUI()
	{
		if (maxAP == 0)
		{
			apBarTransform.gameObject.SetActive(false);
			return;
		}

		if (apImage != null)
			apImage.fillAmount = maxAP > 0 ? (float)currentAP / maxAP : 0f;

		if (apText != null)
			apText.text = $"{currentAP}/{maxAP}";
	}

	private void UpdatePieceIcon(CardType type)
	{
		if (pieceIcon == null) return;

		switch (type)
		{
			case CardType.Missionary:
				pieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_missionary");
				break;
			case CardType.Soldier:
				pieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_soldier");
				break;
			case CardType.Farmer:
				pieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_farmer");
				break;
			case CardType.Pope:
				pieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_pope");
				break;
		}
	}

	private void LateUpdate()
	{

		if (!pieceTarget) return;

		UpdateAPUI();
		UpdateHPUI();

		// 跟随 + 朝向摄像机
		transform.position = pieceTarget.position + offset;
		transform.forward = Camera.main.transform.forward;

		// ----------- 新增：根据距离缩放 UI -----------
		float distance = Vector3.Distance(Camera.main.transform.position, transform.position);

		// 距离越远越大（调节 scaleFactor 来控制）
		float scaleFactor = distance * uiBaseScale; // 可调整
		transform.localScale = Vector3.one * scaleFactor;
	}


	private void RefreshSlotLayout()
	{
		float spacing = 5f;

		if (buildingSlots == null || buildingSlots.Count == 0)
			return;

		RectTransform prefabRT = slotPrefab.GetComponent<RectTransform>();
		float width = prefabRT.rect.width;
		float baseX = prefabRT.anchoredPosition.x;
		float baseY = prefabRT.anchoredPosition.y;

		for (int i = 0; i < buildingSlots.Count; i++)
		{
			RectTransform rt = buildingSlots[i].GetComponent<RectTransform>();

			rt.anchoredPosition = new Vector2(
				baseX + (width + spacing) * i,
				baseY
			);
		}
	}
}
