using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 用于显示玩家的血量
/// </summary>
public class HPBar : MonoBehaviour
{
	//================================
	// 変数
	//================================
	private Transform PieceTarget;   // 血条的对象
	private Vector3 Offset;          // 血条的偏移位置
	private int MaxHp;
	private int CurrentHp;          // 当前血量

	//================================
	// プロパティ
	//================================
	[Header("UIPrefab")] 
    public Image HPImage;			// 血条分段预制体
	public Image PieceIcon;			// 血条图标
	public TMP_Text HPText;         // 血量文本

	//================================
	// メソッド
	//================================
	public void Initialize(int maxHP, Transform target, Vector3 offset,CardType type)
	{
		PieceTarget = target;
		Offset = offset;

		CurrentHp = MaxHp = maxHP;
		HPText.text = $"{maxHP}/{maxHP}";

		// Set PieceIcon Type
		// todo: 修改名称 暂时用占位图标
		switch (type)
		{
			case CardType.Missionary:
				PieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_missionary");
				break;
			case CardType.Solider:
				PieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_soldier");
				break;
			case CardType.Farmer:
				PieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_farmer");
				break;
			case CardType.Pope:
				PieceIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.HPBar_Icon, "Temp_pope");
				break;
		}

	}

	/// <summary>
	/// 设定血量
	/// </summary>
	/// <param name="hp"></param>
	public void UpdateHP(int hp)
	{
		CurrentHp = hp;
	}

	/// <summary>
	/// 设定血量和血量上限 升级时可用
	/// </summary>
	/// <param name="currentHP"></param>
	/// <param name="maxHp"></param>
	public void UpdateHP(int currentHP, int maxHp)
	{
		CurrentHp = currentHP;
		MaxHp = maxHp;
	}

	void LateUpdate()
	{
		if (!PieceTarget) return;

		// 位置跟随
		transform.position = PieceTarget.position + Offset;

		// 面向相机
		transform.forward = Camera.main.transform.forward;

		// 更新画面表现
		HPImage.fillAmount = (float)CurrentHp / MaxHp;
		HPText.text = $"{CurrentHp}/{MaxHp}";
	}
}
