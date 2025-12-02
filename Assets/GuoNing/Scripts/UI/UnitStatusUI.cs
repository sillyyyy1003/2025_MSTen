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

	//================================
	// メソッド
	//================================
	public void Initialize(
		int maxHP,
		int maxAP,
		Transform target,
		Vector3 offset,
		CardType type)
	{
		this.pieceTarget = target;
		this.offset = offset;

		this.maxHP = maxHP;
		this.currentHP = maxHP;

		this.maxAP = maxAP;
		this.currentAP = maxAP;

		// 初始刷新
		UpdateHPUI();
		UpdateAPUI();
		UpdatePieceIcon(type);
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
