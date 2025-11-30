using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillBranch : MonoBehaviour
{
	public SkillNode nodePrefab;
	public float VerticalSpacing = 200;

	private List<SkillNode> skillNodes = new List<SkillNode>();
	public void Initialize(Sprite sprite, TechTree type, int maxLevel , RectTransform panel,LevelUpButton button)
	{

		if(maxLevel <= 0) { Debug.LogWarning("SkillBranch slots is under 0"); return; }

		for (int i = 0; i < maxLevel; i++) 
		{ 
			// 实例化预制件
			SkillNode node = Instantiate(nodePrefab,this.transform);
			string skillDescription = "";
			node.Initialize(sprite, skillDescription,i, panel, button);
			RectTransform rt = node.GetComponent<RectTransform>();
			rt.anchoredPosition = new Vector2(0, -i * VerticalSpacing);
			skillNodes.Add(node);
		}

	}
}
