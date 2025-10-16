using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MilitaryCardManager : MonoBehaviour
{
	[Header("Prefabs & References")]
	public GameObject cardPrefab;
	public RectTransform cardContainer;

	[Header("Layout Settings")]
	public float cardSpacing = 200f;
	public float stackedSpacing = 60f;   // 点击后右侧卡牌的堆叠间距
	public float openOffset = 300f;      // 展开卡牌导致的右移距离
	public Vector2 startPosition = new Vector2(0, 0);

	[Header("Fake Player Data")]
	public int playerCardCount = 5;

	private List<MilitaryCard> cards = new List<MilitaryCard>();
	private List<SoliderData>  soliderDatas = new List<SoliderData>();

	public int playerID { get; set; }	// 开局时赋予玩家ID

	void Start()
	{
		GenerateCards(playerCardCount);
	}

	#region ---- 生成与销毁 ----

	void GenerateCards(int count)
	{
		for (int i = 0; i < count; i++)
			AddCard(i);
	}

	/// <summary>
	/// 添加单张卡牌
	/// </summary>
	public void AddCard(int? fakeIndex = null)
	{
		int i = fakeIndex ?? cards.Count;
		GameObject cardObj = Instantiate(cardPrefab, cardContainer);
		MilitaryCard card = cardObj.GetComponent<MilitaryCard>();

		RectTransform rect = cardObj.GetComponent<RectTransform>();
		rect.anchoredPosition = startPosition + new Vector2(i * cardSpacing, 0);

		// 如果有士兵数据则使用数据，否则使用默认值
		if (soliderDatas.Count != 0)
		{
			card.dataText_HpText.text = $"HP: {soliderDatas[i].hp}";
			card.dataText_AttackText.text = $"Attack: {soliderDatas[i].attack}";
		}
		else
		{   // 虚拟数据
			card.dataText_HpText.text = $"HP: {100 + i * 10}";
			card.dataText_AttackText.text = $"Attack: {20 + i * 5}";
		}

		// 点击事件
		int index = i;
		Button cardBtn = card.militaryCardImage.GetComponent<Button>();
		cardBtn.onClick.AddListener(() => OnCardClicked(index));

		cards.Add(card);
	}

	/// <summary>
	/// 删除指定索引的卡牌
	/// </summary>
	//public void RemoveCard(int index)
	//{
	//	if (index < 0 || index >= cards.Count)
	//		return;

	//	Destroy(cards[index].gameObject);
	//	cards.RemoveAt(index);

	//	// 更新点击索引
	//	if (currentOpenedIndex == index)
	//		currentOpenedIndex = -1;
	//	else if (currentOpenedIndex > index)
	//		currentOpenedIndex--;

	//	// 重新布局
	//	RearrangeCards(currentOpenedIndex);
	//}

	
	public void UpdateCards()
	{
		// clear old data
		soliderDatas.Clear();
		// Get Player data
		PlayerData data = PlayerDataManager.Instance.GetPlayerData(playerID);
	
		// 更新士兵数据
		foreach (var playerUnit in data.PlayerUnits)
		{
			// 筛选士兵单位
			if (playerUnit.UnitType == PlayerUnitType.Soldier)
			{
				SoliderData solider = new SoliderData();
				// 假设SoliderData有Level, HP, Attack属性
				//solider.level = playerUnit.Level;
				//solider.hp = playerUnit.HP;
				//solider.attack = playerUnit.Attack;
				soliderDatas.Add(solider);
			}
		}
		// 清空卡牌
		cards.Clear();
		// 重新生成卡牌
		GenerateCards(soliderDatas.Count);

	}

	#endregion

	#region ---- 点击与布局 ----


	void OnCardClicked(int clickedIndex)
	{
		if (cards[clickedIndex].GetPanelOpen())
		{
			Debug.Log(cards[clickedIndex].GetPanelOpen());
			RearrangeCards(clickedIndex, false);
		}
		else
		{
			RearrangeCards(clickedIndex, true);
		}
	}

	/// <summary>
	/// 计算卡牌的基线位置（不随动画变化）
	/// </summary>
	Vector2 GetBasePosition(int index)
	{
		return startPosition + new Vector2(index * cardSpacing, 0f);
	}


	/// <summary>
	/// 重排卡牌：只移动右侧的卡牌
	/// </summary>
	/// <param name="openedIndex">当前点击的卡牌索引</param>
	/// <param name="isClosing">是否是在关闭展开卡牌</param>
	void RearrangeCards(int openedIndex, bool isClosing)
	{
		StopAllCoroutines();
		if (isClosing)
		{
			for (int i = 0; i < cards.Count; i++)
			{
				RectTransform rect = cards[i].GetComponent<RectTransform>();
				Vector2 startPos = rect.anchoredPosition;
				Vector2 targetPos= GetBasePosition(i);

				StartCoroutine(MoveTo(rect, startPos, targetPos));
			}
		}
		else
		{
			for (int i = 0; i < cards.Count; i++)
			{
				if(i<=openedIndex)continue;

				RectTransform rect = cards[i].GetComponent<RectTransform>();
				Vector2 startPos = GetBasePosition(i);
				Vector2 targetPos = GetBasePosition(openedIndex)+ new Vector2(openOffset + (i - openedIndex - 1) * stackedSpacing, 0f);
		
				StartCoroutine(MoveTo(rect, startPos, targetPos));
			}


		}
	}

	private IEnumerator MoveTo(RectTransform rect, Vector2 start, Vector2 target)
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * 6f;
			rect.anchoredPosition = Vector2.Lerp(start, target, t);
			yield return null;
		}
		rect.anchoredPosition = target;
	}

	
	#endregion

	struct SoliderData
	{
		public int level { get; set; }
		public int hp { get; set; }
		public int attack { get; set; }
	}
}
