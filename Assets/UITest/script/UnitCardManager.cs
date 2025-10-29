﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject storedCardPrefab;

    public RectTransform singleContainer;
    public RectTransform doubleContainer;
    public RectTransform cardContainer;
    public RectTransform deckContainer;
    public GameObject deckCount;

    [Header("Layout Settings")]
	public float cardSpacing = 5f;//没有卡展开的时候卡牌与卡牌的间距
	public float stackedSpacing = 60f;   // 点击后右侧卡牌的堆叠间距
    public float openSpacing = 5f;//展开的时候卡牌最右侧与卡牌的间距
    public float startSpacing = 5f;//框最左侧与卡牌的间距
    public float deckSpacing = 5f;//仓库里的卡牌与卡牌的间距
    public float deckstartSpacing = 1f;//仓库里的卡牌与卡牌的间距
    public bool isDeckSelected = false;


    [Header("Fake Player Data")]
	public int currentCardCount = 5;
    public int targetCardCount = 5;
    public int currentDeckCount = 5;
    public int targetDeckCount = 5;

    //内部保存用
    private List<UnitCard> cards = new List<UnitCard>();//目前场上显示的卡牌的列表
    private List<StoredCard> deck = new List<StoredCard>();//目前场上显示的卡组的列表


    //内部计算用
	private float cardWidth = 1f;//卡牌宽度
    private float detailCardWidth = 1f;//展开卡牌宽度
    private float openOffset = 1f;      // 展开卡牌导致的右移距离(从卡牌最左边为起点开始计算)
    private float containerWidth = 1f;//卡牌槽的原始宽度，当大于这个宽度的时候按照最大宽度重新拉长
    private float viewWidth = 1f;//卡牌滚动窗口的固定大小
    private Vector2 startPosition_Container = new Vector2(0, 0);

    private CardType currentCardType = CardType.None;
    private CardType targetCardType = CardType.None;

    private bool enableSingleMode = true;

    public int openIndex = -1;

    //fake data






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


        currentCardType = CardType.None;
        targetCardType = CardType.None;

        enableSingleMode = true;
        doubleContainer.gameObject.SetActive(false);
        singleContainer.gameObject.SetActive(true);


        cardWidth = cardPrefab.transform.Find("Card").GetComponent<RectTransform>().sizeDelta.x;
        detailCardWidth = cardPrefab.transform.Find("DetailCard").GetComponent<RectTransform>().sizeDelta.x;
		openOffset = detailCardWidth + openSpacing + cardWidth;
        containerWidth = cardContainer.sizeDelta.x;
        viewWidth = cardContainer.sizeDelta.x;
        startPosition_Container = new Vector2(startSpacing, 0);


    }

    void Start()
	{


    }

    void Update()
	{
        UpdateCards();

    }

    #region ==== 卡牌生成与销毁 ====

    /// <summary>根据玩家数据生成卡牌（暂留）PlayerData==>UIData</summary>
    void GenerateCardList(PlayerData playerData)
    {








    }

    /// <summary>生成全部的激活的卡牌</summary>
    void GenerateActiveCards(int count,CardType type)
	{
        if (type == CardType.None) return;

        for (int i = 0; i < count; i++)
            AddActiveCard(type, i);

    }

    /// <summary>生成全部的储存区的卡牌</summary>
    void GenerateStoredCards(int count, CardType type)
    {
        if (count <= 0)
        {
            deckCount.SetActive(false);
            return;
        }


        for (int i = 0; i < count; i++)
            AddStoredCard(type, i);



        deckCount.SetActive(true);
        deckCount.transform.SetAsLastSibling();
        deckCount.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = count.ToString();

    }


    /// <summary>添加单张激活的卡牌</summary>
    public void AddActiveCard(CardType type,int? fakeIndex = null)
	{
        if (type == CardType.None) return;

        int i = fakeIndex ?? cards.Count;
		GameObject cardObj = Instantiate(cardPrefab, type==CardType.Pope? singleContainer:cardContainer);
        UnitCard card = cardObj.GetComponent<UnitCard>();
		card.SetSprite(type);

        if (type == CardType.Pope) card.alwaysOpen = true;


        RectTransform rect = cardObj.GetComponent<RectTransform>();
		rect.anchoredPosition = startPosition_Container + new Vector2(i * (cardSpacing+cardWidth), 0);


        cards.Add(card);


        // 点击事件
        int index = i;
		Button cardBtn = card.unitCardImage.GetComponent<Button>();
		cardBtn.onClick.AddListener(() => OnCardClicked(index));


    }

    /// <summary>添加单张储存区卡牌</summary>
    public void AddStoredCard(CardType type, int? fakeIndex = null)
    {
        if (type == CardType.None|| type == CardType.Pope) return;


        int i = fakeIndex ?? deck.Count;
        // 生成卡牌对象
        GameObject cardObj = Instantiate(storedCardPrefab, deckContainer);
        StoredCard storedCard = cardObj.GetComponent<StoredCard>();

        // 设置卡牌显示
        storedCard.SetSprite(type);


        // 计算堆叠偏移位置（右堆叠）
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        Vector2 basePos = new Vector2(deckstartSpacing + i * deckSpacing, 0f);
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

    /// <summary>清空所有卡牌的数据和显示</summary>
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
        openIndex = -1;
    }


    /// <summary>更新卡牌显示</summary>
    public void UpdateCards()
	{
        if (!enableSingleMode)
        {
            if (targetCardType != currentCardType || targetCardCount != currentCardCount || targetDeckCount != currentDeckCount)
            {
                ClearAllCards();

                GenerateActiveCards(targetCardCount, targetCardType);
                GenerateStoredCards(targetDeckCount, targetCardType);

                currentCardType = targetCardType;
                currentCardCount = targetCardCount;
                currentDeckCount = targetDeckCount;
            }


        }
        else
        {
            if (targetCardType != currentCardType)
            {
                ClearAllCards();
                GenerateActiveCards(targetCardCount, targetCardType);
                currentCardType = targetCardType;
                currentCardCount = targetCardCount;
            }


        }

        AdjustContainerWidth();


    }

    #endregion

    #region ==== 卡牌点击与交互 ====

    void OnCardClicked(int clickedIndex)
	{
        if (cards[clickedIndex].alwaysOpen) return;

        if (cards[clickedIndex].GetPanelOpen())
		{
			Debug.Log(cards[clickedIndex].GetPanelOpen());
			RearrangeCards(clickedIndex, false);
            openIndex = clickedIndex;

        }
		else
		{
			RearrangeCards(clickedIndex, true);
            openIndex = -1;

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

    #endregion

    #region ==== 布局与位置控制 ====

    /// <summary>调整容器宽度与视野位置</summary>
    private void AdjustContainerWidth()
    {
        if (cardContainer == null || cards.Count == 0) return;

        RectTransform rt = cards[currentCardCount - 1].GetComponent<RectTransform>();
        float rightMostX = rt.anchoredPosition.x;
        bool isOpen = cards[currentCardCount - 1].GetPanelOpen();

        if (isOpen)
        {
            // 展开状态使用扩展后的宽度
            rightMostX = rightMostX + openOffset + startSpacing;
        }
        else
        {
            // 未展开使用普通宽度
            rightMostX = rightMostX + cardWidth + startSpacing;
        }

        // === 设置容器宽度 + 自动定位到打开的卡牌===
        if (rightMostX != containerWidth)
        {
            Vector2 size = cardContainer.sizeDelta;
            cardContainer.sizeDelta = new Vector2(rightMostX, size.y);

            containerWidth = rightMostX;

            if (openIndex >= 0 && openIndex < cards.Count)
            {
                RectTransform targetRT = cards[openIndex].GetComponent<RectTransform>();

                // 卡牌右边界 = 位置 + 宽度展开
                float targetRight = targetRT.anchoredPosition.x + openOffset;

                // 如果内容超出视口宽才需要滚动,滚动到最右侧
                if (targetRight > viewWidth)
                {
                    Vector2 pos = cardContainer.anchoredPosition;
                    cardContainer.anchoredPosition = new Vector2(viewWidth - rightMostX, pos.y);
                }
            }
        }



        // 可选：强制刷新布局
        //Canvas.ForceUpdateCanvases();
    }


    /// <summary>自动聚焦到展开的卡牌  根据CardId去取 CardType</summary>
    private void SetContainerLookAt()
    {
        

    }

    /// <summary>计算卡牌的基线位置（不随动画变化）</summary>
    Vector2 GetBasePosition(int index)
    {

        return startPosition_Container + new Vector2(index * (cardSpacing + cardWidth), 0f);
    }

    /// <summary>重排卡牌：只移动右侧的卡牌</summary>
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
                Vector2 targetPos = GetBasePosition(i);

                StartCoroutine(MoveTo(rect, startPos, targetPos));
            }
        }
        else
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (i <= openedIndex) continue;

                RectTransform rect = cards[i].GetComponent<RectTransform>();
                Vector2 startPos = GetBasePosition(i);
                Vector2 targetPos = GetBasePosition(openedIndex) + new Vector2(openOffset + (i - openedIndex - 1) * stackedSpacing, 0f);

                StartCoroutine(MoveTo(rect, startPos, targetPos));
            }


        }


    }

    /// <summary>关闭所有展开的卡牌</summary>
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

    /// <summary>平滑移动到目标位置</summary>
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


    #region ==== 模式与状态 ====

    /// <summary>设置牌山选中与否：牌山选中的时候，背景变色，点击购买会买到牌山里</summary>
    /// <param name="tf"></param>
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

    /// <summary>牌山是否处于选中状态</summary>
    public bool IsDeckSelected()
    {
        return isDeckSelected;
    }

    /// <summary>教皇的单张卡显示模式</summary>
    public void EnableSingleMode(bool tf)
    {
        if (enableSingleMode == tf) return;

        if (tf)
        {
            doubleContainer.gameObject.SetActive(false);
            singleContainer.gameObject.SetActive(true);

        }
        else
        {
            doubleContainer.gameObject.SetActive(true);
            singleContainer.gameObject.SetActive(false);

        }


        enableSingleMode = tf;

        UpdateCards();

    }

    /// <summary>设置目前被选中表示的卡牌种类</summary>
    public void SetTargetCardType(CardType type)
    {

        targetCardType = type;
        if (type == CardType.Pope) targetCardCount = 1;

        //targetCardCount=PlayerDataManager.Instance.GetPl

    }

    /// <summary>设置目前被选中表示的卡牌种类 需要PlayerDataManager去用</summary>
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







    #endregion















}

