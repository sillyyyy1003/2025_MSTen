using GameData;
using GameData.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSkillPanel : MonoBehaviour
{
	[Header("Prefabs")]
	public SimpleSkillButton buttonPrefab;

	[Header("Root")]
	public RectTransform buttonRoot;

	private PieceType currentType;

	// 保存所有按钮（用于刷新）
	private List<SimpleSkillButton> buttons = new List<SimpleSkillButton>();

	public void Initialize(PieceType type)
	{
		currentType = type;

		Dictionary<TechTree, int> currentLevels = SkillTreeUIManager.Instance.UnitSkillLevels[type].currentLevels;

		int i = 0;
		foreach (var kv in currentLevels)
		{
			TechTree tech = kv.Key;
			CreateButton(type, tech,i);
			i++;
		}
	}

	


	private void CreateButton(PieceType type, TechTree tech,int index)
	{
		//生成交错的Button
        float leftX = -72.5f;      // 左列 X
        float rightX = 72.5f;      // 右列 X
        int column = index % 2;        // 0 = 左列，1 = 右列
        int row = index ;           // 行号
        float x = (column == 0) ? leftX : rightX;
        float y = -row * 45/2+20f;

        SimpleSkillButton b = Instantiate(buttonPrefab, buttonRoot);
		b.Initialize(type, tech, this);
		RectTransform rt = b.GetComponent<RectTransform>();
		rt.anchoredPosition = new Vector2(x, y);
		buttons.Add(b);
	}

	/// <summary>
	/// 升级后刷新所有按钮
	/// </summary>
	public void Refresh()
	{
		foreach (var b in buttons)
			b.Refresh();
	}
}