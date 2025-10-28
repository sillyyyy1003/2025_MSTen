using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;




/// <summary>
/// 卡牌 UI 控制器（dataPanel 从卡牌左侧滑到右侧）
/// 锚点要求：dataPanel、background 的锚点为左侧居中 (Anchor: Left Middle, Pivot: 0, 0.5)
/// </summary>
public class UnitCard : MonoBehaviour
{
	[Header("UI References")]
    public Image backgroundImage;    // 卡牌背景
    public Image unitCardImage;     // 角色背景图
    public Image charaImage;     // 角色图
    public GameObject dataPanel;        // 数据面板（默认隐藏）
	public Text dataText_HpText;        // HP 文本
	public Text dataText_AttackText;    // 攻击力文本

	[Header("Sprite List")]
	public Sprite missionarySprite;//传教士
	public Sprite soliderSprite;//士兵
	public Sprite farmerSprite;//农民
	public Sprite buildingSprite;//建筑
	public Sprite popeSprite;//教皇

    [Header("Panel Animation Settings")]
	public float panelSlideSpeed = 6f;      // 滑动速度

    private RectTransform backgroundRect;
    private RectTransform cardRect;
    private RectTransform dataPanelRect;
	private CanvasGroup dataPanelCanvas;

    private bool showDataPanel = false;
	private CardType cardType = CardType.None;


    // 滑动起始与结束位置
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector2 panelHiddenPos;
	private Vector2 panelVisiblePos;

    //单张卡的宽度,面板宽度和展开后宽度
    private float normalWidth = 1f;
    private float panelWidth = 1f;
    private float expandWidth = 1f;


    void Start()
	{
        originalPosition = transform.localPosition;
		targetPosition = originalPosition;

		if (dataPanel != null && unitCardImage != null)
		{
            backgroundRect = backgroundImage.GetComponent<RectTransform>();
            cardRect = unitCardImage.GetComponent<RectTransform>();
			dataPanelRect = dataPanel.GetComponent<RectTransform>();

			dataPanelCanvas = dataPanel.GetComponent<CanvasGroup>();
			if (dataPanelCanvas == null)
				dataPanelCanvas = dataPanel.AddComponent<CanvasGroup>();

			dataPanel.SetActive(false);
			dataPanelCanvas.alpha = 0f;

			// === 计算位置 ===
			// 这里的坐标全部在父级局部空间内
			RectTransform parentRect = cardRect.parent as RectTransform;


            //单张卡的宽度,面板宽度和展开后宽度
            normalWidth = cardRect.rect.width;
            panelWidth = dataPanelRect.rect.width;
            expandWidth = cardRect.rect.width + dataPanelRect.rect.width;

            // 卡牌的 anchoredPosition
            Vector2 cardPos = cardRect.anchoredPosition;

			// 卡牌锚点最左
			// 初始隐藏位置：dataPanel 左侧与 card 左侧对齐（重叠）
			panelHiddenPos = new Vector2(cardPos.x - 0f, cardPos.y);
			// 可见位置：dataPanel 左侧贴到 card 右侧
			panelVisiblePos = new Vector2(cardPos.x + normalWidth, cardPos.y);

            dataPanelRect.anchoredPosition = cardPos;
            backgroundRect.anchoredPosition = cardPos;

        }
	}

	void Update()
	{
        // 模拟数据更新
        if (showDataPanel)
		{
			dataText_HpText.text = "HP: 100";
			dataText_AttackText.text = "Attack: 20";

			


        }





	}

	public void PanelEvent()
	{

		if (showDataPanel)
		{
			ClosePanel();
		}
		else
		{
			ShowPanel();
		}
	}

	public void ShowPanel()
	{
		if (showDataPanel) return;
        UnitCardManager.Instance.CloseAllCards();

        showDataPanel = true;
		dataPanel.SetActive(true);

        StartCoroutine(SlidePanelIn());

    }

	public void ClosePanel()
	{
		if (!showDataPanel) return;
		showDataPanel = false;
		StopAllCoroutines();
		StartCoroutine(SlidePanelOut());

	}

	private IEnumerator SlidePanelIn()
	{
		float startWidth = normalWidth;
		float targetWidth = expandWidth;

        float t = 0f;
		while (t < 1f)
		{
			//panel滑出
			t += Time.deltaTime * panelSlideSpeed;
			dataPanelRect.anchoredPosition = Vector2.Lerp(panelHiddenPos, panelVisiblePos, t);
			dataPanelCanvas.alpha = Mathf.Lerp(0f, 1f, t);

			//背景展开
            float i = Mathf.Clamp01(t / 1);
            float newWidth = Mathf.Lerp(startWidth, targetWidth, i);
            backgroundRect.sizeDelta = new Vector2(newWidth, backgroundRect.sizeDelta.y);

            yield return null;
		}
		dataPanelRect.anchoredPosition = panelVisiblePos;
		dataPanelCanvas.alpha = 1f;
	}

	private IEnumerator SlidePanelOut()
	{
        float startWidth = expandWidth;
        float targetWidth = normalWidth;

        float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * panelSlideSpeed;
			// panel滑回
			dataPanelRect.anchoredPosition = Vector2.Lerp(panelVisiblePos, panelHiddenPos, t);
			dataPanelCanvas.alpha = Mathf.Lerp(1f, 0f, t);

            //背景缩小
            float i = Mathf.Clamp01(t / 1);
            float newWidth = Mathf.Lerp(startWidth, targetWidth, i);
            backgroundRect.sizeDelta = new Vector2(newWidth, backgroundRect.sizeDelta.y);


            yield return null;
		}
		// 确保位置最终值
		dataPanelRect.anchoredPosition = panelHiddenPos;
		dataPanelCanvas.alpha = 0f;
		dataPanel.SetActive(false);
	}

	public bool GetPanelOpen()
	{
		return showDataPanel;
	}

    public void SetSprite(CardType type)
	{
		SetCardType(type);

        switch (type)
		{
			case CardType.Missionary:
				charaImage.sprite = missionarySprite;

                break;
			case CardType.Solider:
                charaImage.sprite = soliderSprite;
                break;
			case CardType.Farmer:
                charaImage.sprite = farmerSprite;
                break;
			case CardType.Building:
                charaImage.sprite = buildingSprite;
                break;
			case CardType.Pope:
                charaImage.sprite = popeSprite;
                break;
            default:
                charaImage.sprite = popeSprite;
                break;

        }


	}
    public void SetCardType(CardType type)
	{

		cardType = type;

	}
}
