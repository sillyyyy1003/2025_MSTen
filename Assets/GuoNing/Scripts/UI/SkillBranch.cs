using GameData;
using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillBranch : MonoBehaviour
{
	//=========================================
	//プロパティ
	//=========================================
	public SkillNode nodePrefab;
	public float VerticalSpacing = 200;
	public Image linePrefab;

	public RectTransform lineRoot;
	public RectTransform NodeRoot;

	private List<SkillNode> skillNodes = new List<SkillNode>();
	public void Initialize(Sprite sprite, TechTree type, PieceType pieceType,  int maxLevel, float verticalSpacing, RectTransform panel, LevelUpButton button)
	{

		if(maxLevel <= 0) { Debug.LogWarning("SkillBranch slots is under 0"); return; }
		// 设定纵向的距离
		VerticalSpacing = verticalSpacing;
		for (int i = 0; i < maxLevel; i++) 
		{ 
			// 实例化预制件
			SkillNode node = Instantiate(nodePrefab,NodeRoot);
			// 设定文字
			string skillDescription = GetTechText(type,i);
			// 初始化Node
			node.Initialize(sprite, skillDescription,i, pieceType, type, panel, button);
			// 设定Node位置
			RectTransform rt = node.GetComponent<RectTransform>();
			rt.anchoredPosition = new Vector2(0, -i * VerticalSpacing);
			skillNodes.Add(node);

			// ----如果不是第 0 个 Node，则连接上面的 Node----
			if (i > 0)
			{
				CreateLineBetween(skillNodes[i - 1], skillNodes[i]);
			}
		}
	}

	/// <summary>
	/// 创建两个 Node 之间的连线
	/// </summary>
	private void CreateLineBetween(SkillNode upperNode, SkillNode lowerNode)
	{
		Image line = Instantiate(linePrefab, lineRoot);

		RectTransform upper = upperNode.GetComponent<RectTransform>();
		RectTransform lower = lowerNode.GetComponent<RectTransform>();
		RectTransform lineRT = line.GetComponent<RectTransform>();

		// 设置为竖线模式
		float lineWidth = lineRT.sizeDelta.x;
		float distance = Mathf.Abs(lower.anchoredPosition.y - upper.anchoredPosition.y);

		// 线段长度 = 节点中心到节点中心
		lineRT.sizeDelta = new Vector2(lineWidth, distance);

		// 线段位置 = 两点中间
		float midY = (upper.anchoredPosition.y + lower.anchoredPosition.y) * 0.5f;
		lineRT.anchoredPosition = new Vector2(0, midY);

		// 确保是竖直方向
		lineRT.localRotation = Quaternion.identity;
	}


	/// <summary>
	/// 返回技能的简短描述
	/// </summary>
	/// <param name="type"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	private string GetTechText(TechTree type, int index)
	{
		int lv = index + 1;

		switch (type)
		{
			case TechTree.HP:                 // 体力
				return $"体力 Lv.{lv}";

			case TechTree.AP:                 // 行動力
				return $"行動力 Lv.{lv}";

			case TechTree.Occupy:             // 占領成功率
				return $"占領 Lv.{lv}";

			case TechTree.Conversion:         // 魅惑成功率
				return $"魅惑 Lv.{lv}";

			case TechTree.ATK:                // 攻撃力
				return $"攻撃力 Lv.{lv}";

			case TechTree.Sacrifice:          // 献祭関連
				return $"献祭 Lv.{lv}";

			case TechTree.AttackPosition:     // 攻撃口
				return $"攻撃口 Lv.{lv}";

			case TechTree.AltarCount:         // 祭壇数
				return $"祭壇数 Lv.{lv}";

			case TechTree.ConstructionCost:   // 建設費用減少
				return $"建設費用 Lv.{lv}";

			case TechTree.MovementCD:         // 移動クール
				return $"移動クール Lv.{lv}";

			case TechTree.Buff:               // 強化/弱体効果
				return $"強化効果 Lv.{lv}";

			case TechTree.Heresy:             // 異端（デバフ系）
				return $"異端 Lv.{lv}";

			default:
				return $"Lv.{lv}";
		}
	}
}
