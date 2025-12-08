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
			// 把预制体本身当作第一个格子
			buildingSlots.Clear();
			buildingSlots.Add(slotPrefab);

			RectTransform prefabRT = slotPrefab.GetComponent<RectTransform>();

			float width = prefabRT.rect.width;                 // 单个格子的宽度
			float spacing = 5f;                                  // 格子间距
			float baseX = prefabRT.anchoredPosition.x;         // prefab 当前的 X 位置
			float baseY = prefabRT.anchoredPosition.y;         // prefab 当前的 Y 位置

			// 再生成剩下的格子，让它们“跟在 prefab 后面”
			for (int i = 0; i < buildingSlot - 1; i++)
			{
				BuildingSlot slot = Instantiate(slotPrefab, transform);
				RectTransform rt = slot.GetComponent<RectTransform>();

				// i=0 的时候，紧挨着 prefab 放在右边
				rt.anchoredPosition = new Vector2(
					baseX + (width + spacing) * (i + 1),
					baseY
				);

				buildingSlots.Add(slot);
			}
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

	public void ActivateSlot(int index, bool isActive)
	{
		if (index < 0 || index >= buildingSlots.Count) return;
		buildingSlots[index].SetActiveSlot(isActive);
	}

	public void CloseSlot(int index)
	{
		if (index < 0 || index >= buildingSlots.Count) return;
		buildingSlots[index].CloseSlot();
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

		// 跟随 + 朝向摄像机
		transform.position = pieceTarget.position + offset;
		transform.forward = Camera.main.transform.forward;
	}
}
