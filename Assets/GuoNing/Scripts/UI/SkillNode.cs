using GameData;
using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillNode : MonoBehaviour
{
	[SerializeField]
	private Button skillButton;
	[SerializeField]
	private Image mask;
	[SerializeField]
	private TMP_Text text;

	private RectTransform levelUpPanel;
	private LevelUpButton levelUpButton;

	public PieceType type { get; private set; }
	public TechTree techTree {get;private set;}

	private int skillIndex;
	public int SkillIndex => skillIndex;

	private void Start()
	{
		skillButton.onClick.AddListener(OnSkillButtonClick);
	}

	public void Initialize(Sprite sprite, string skillDescription, int _skillIndex, RectTransform _levelUpPanel, LevelUpButton _button)
	{
		skillButton.image.sprite = sprite;
		text.text = skillDescription;
		skillIndex = _skillIndex;

		levelUpPanel = _levelUpPanel;
		levelUpButton = _button;
	}

	public void UnlockSkillNode()
	{
		mask.gameObject.SetActive(false);
	}

	private void OnSkillButtonClick()
	{
		// 显示面板
		levelUpPanel.gameObject.SetActive(true);
		
		// 绑定相关升级事件到levelUpButton上

	}

	public void OnLevelUpButtonClicked()
	{
		UnlockSkillNode();

		//UnitListTable.PieceDetail pd =
		//	new UnitListTable.PieceDetail(PieceType, SceneStateManager.Instance.PlayerReligion);
	}
	
}
