using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;


public enum CardType
{
    Missionary,//传教士
    Solider,//士兵
    Farmer,//农民
    Building,//建筑
	Pope,//教皇
	None
}




public class UnitCardManager : MonoBehaviour
{
	[Header("Prefabs & References")]
	public GameObject cardPrefab;
	public RectTransform cardContainer;
    public GameObject storedCardPrefab;
    public RectTransform deckContainer;
    public GameObject deckCount;

    [Header("Layout Settings")]
	public float cardSpacing = 5f;//没有卡展开的时候卡牌与卡牌的间距
	public float stackedSpacing = 60f;   // 点击后右侧卡牌的堆叠间距
    public float openSpacing = 5f;//展开的时候卡牌最右侧与卡牌的间距
    public float startSpacing = 5f;//框最左侧与卡牌的间距
    public float deckSpacing = 1f;//仓库里的卡牌与卡牌的间距
    public bool isDeckSelected = false;


    [Header("Fake Player Data")]
	public int currentCardCount = 5;
    public int targetCardCount = 5;
    public int currentDeckCount = 5;
    public int targetDeckCount = 5;

    private List<UnitCard> cards = new List<UnitCard>();//目前场上显示的卡牌的列表
    private List<StoredCard> deck = new List<StoredCard>();//目前场上显示的卡组的列表
    private List<SoliderData>  soliderDatas = new List<SoliderData>();

	private float cardWidth = 1f;//卡牌宽度
    private float detailCardWidth = 1f;//展开卡牌宽度
    private float openOffset = 1f;      // 展开卡牌导致的右移距离(从卡牌最左边为起点开始计算)
    private Vector2 startPosition = new Vector2(0, 0);


	private CardType currentCardType = CardType.None;
    private CardType targetCardType = CardType.None;

    public int playerID { get; set; }   // 开局时赋予玩家ID

    public static UnitCardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        deckCount.SetActive(false);
        cardWidth = cardPrefab.transform.Find("Card").GetComponent<RectTransform>().sizeDelta.x;
        detailCardWidth = cardPrefab.transform.Find("DetailCard").GetComponent<RectTransform>().sizeDelta.x;
		openOffset = detailCardWidth + openSpacing + cardWidth;
		startPosition = new Vector2(-cardContainer.sizeDelta.x/2 + startSpacing+ cardWidth/2, 0);



    }

    void Start()
	{


    }

    void Update()
	{
        

        if (targetCardType != currentCardType || targetCardCount != currentCardCount || targetDeckCount != currentDeckCount)
        {
            ClearAllCards();
            if (targetCardType != CardType.None)
            {
                GenerateActiveCards(targetCardCount, targetCardType);
                GenerateStoredCards(targetDeckCount, targetCardType);
                
            }
                

            currentCardType = targetCardType;
            currentCardCount = targetCardCount;
            currentDeckCount = targetDeckCount;
        }


    }

    #region ---- 生成与销毁 ----
    void GenerateCardList(PlayerData playerData)
    {








    }

    void GenerateActiveCards(int count,CardType type)
	{
		for (int i = 0; i < count; i++)
            AddActiveCard(type,i);
	}

    /// <summary>
    /// 添加单张激活的卡牌
    /// </summary>
    public void AddActiveCard(CardType type,int? fakeIndex = null)
	{
		int i = fakeIndex ?? cards.Count;
		GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        UnitCard card = cardObj.GetComponent<UnitCard>();
		card.SetSprite(type);

        RectTransform rect = cardObj.GetComponent<RectTransform>();
		rect.anchoredPosition = startPosition + new Vector2(i * (cardSpacing+cardWidth), 0);

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
		Button cardBtn = card.unitCardImage.GetComponent<Button>();
		cardBtn.onClick.AddListener(() => OnCardClicked(index));

		cards.Add(card);
	}


    void GenerateStoredCards(int count, CardType type)
    {
        deckCount.SetActive(false);
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
            AddStoredCard(type, i);


        
        deckCount.SetActive(true);
        deckCount.transform.SetAsLastSibling();
        deckCount.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = count.ToString();

    }

    public void AddStoredCard(CardType type, int? fakeIndex = null)
    {
        int i = fakeIndex ?? deck.Count;
        // 生成卡牌对象
        GameObject cardObj = Instantiate(storedCardPrefab, deckContainer);
        StoredCard storedCard = cardObj.GetComponent<StoredCard>();

        // 设置卡牌显示
        storedCard.SetSprite(type);


        // 计算堆叠偏移位置（右堆叠）
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        Vector2 basePos = new Vector2(-deckContainer.sizeDelta.x / 2 + 10f + cardWidth / 2f + i * deckSpacing*5f, 0f);
        rect.anchoredPosition = basePos;


        //顶层显示+能点击
        if (i == targetDeckCount - 1)
        {
            storedCard.ShowSprite();
            int index = i;
            Button cardBtn = storedCard.unitCardImage.GetComponent<Button>();
            cardBtn.onClick.AddListener(() => OnDeckClicked());

        }
            
        

        deck.Add(storedCard);

    }




    public void UpdateCards()
	{
        if(targetCardType != currentCardType)
		{
			ClearAllCards();
            GenerateActiveCards(soliderDatas.Count,targetCardType);
			currentCardType = targetCardType;

        }
		else
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
			ClearAllCards();
            // 重新生成卡牌
            GenerateActiveCards(soliderDatas.Count, currentCardType);

        }





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

    void OnDeckClicked()
	{

        if (isDeckSelected)
        {
            isDeckSelected = false;
            deckContainer.GetComponent<Image>().color = new Color(159f / 255f, 159f / 255f, 159f / 255f, 150f/255f);
        }
        else
        {
            isDeckSelected = true;
            deckContainer.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.5f);
        }


    }

    /// <summary>
    /// 计算卡牌的基线位置（不随动画变化）
    /// </summary>
    Vector2 GetBasePosition(int index)
	{
        return startPosition + new Vector2(index * (cardSpacing + cardWidth), 0f);
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

    public void CloseAllCards()
    {
		bool findOpenCard = false;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            // 如果卡牌当前是展开的，就让它关闭
            if (card.GetPanelOpen())
            {
				card.ClosePanel();
				findOpenCard = true;
            }

			// 重置位置（回到默认位置）
			if (findOpenCard)
			{
                RectTransform rect = card.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 targetPos = GetBasePosition(i);
                    rect.anchoredPosition = targetPos;
                }

            }
            
        }

    }

    public void ResetStartPosition()
	{


        startPosition = new Vector2(-cardContainer.sizeDelta.x / 2 + startSpacing + cardWidth / 2, 0);


    }

    public void SetTargetCardType(CardType type)
	{

		targetCardType = type;

    }

	public void AddCardCount(int num)
	{
        if (isDeckSelected)
        {
            targetDeckCount = currentDeckCount + num;
        }
        else
        {
            targetCardCount = currentCardCount + num;
        }
		

    }


    public void ClearAllCards()
	{
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            if (obj.name.EndsWith("(Clone)"))
            {
                Destroy(obj);
            }
        }
        cards.Clear();
        deck.Clear();
        deckCount.SetActive(false);
    }

    public void SetDeckSelected(bool tf)
    {

        isDeckSelected = tf;
        if (!isDeckSelected)
        {
            deckContainer.GetComponent<Image>().color = new Color(159f / 255f, 159f / 255f, 159f / 255f, 150f / 255f);
        }
        else
        {
            deckContainer.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.5f);
        }
    }

}
