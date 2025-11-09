using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StoredCard : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("UI References")]
    public Image backgroundImage;    // 卡牌背景
    public Image unitCardImage;     // 角色背景图
    public Image charaImage;     // 角色图
    public TextMeshProUGUI DataText;    // 文本

    [Header("Drag Settings")]
    public float longPressTime = 0.2f;    // 长按多久进入拖拽
    public System.Action<CardType> OnCardDraggedUp;  // 被拖出触发的事件

    private bool showSprite = false;
    private bool isDragging = false;
    private float pressTimer = 0f;

    private RectTransform rect;
    private Canvas canvas;
    private Vector2 originalPos;


    private CardType cardType = CardType.None;


    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }


    // Start is called before the first frame update
    void Start()
    {
        if(!showSprite)
        {
            charaImage.gameObject.SetActive(false);
            DataText.gameObject.SetActive(false);
        }


    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButton(0))
        {
            pressTimer += Time.deltaTime;

            if (!isDragging && pressTimer >= longPressTime)
            {
                isDragging = true;
                Debug.Log("开始拖拽…");
            }
        }




    }

    // =======================
    //  Setter / API
    // =======================

    public void SetSprite(CardType type)
    {
        cardType = type;
        charaImage.sprite = UISpriteHelper.Instance.GetIconByCardType(type);



    }

    public void ShowSprite()
    {

        showSprite = true;

    }

    // =======================
    //  Pointer Events
    // =======================

    public void OnPointerDown(PointerEventData eventData)
    {
        pressTimer = 0f;
        originalPos = rect.anchoredPosition;
        isDragging = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // 松开后恢复位置
        rect.anchoredPosition = originalPos;
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // 跟随鼠标
        rect.position = eventData.position;

        // 触发 1/4 屏幕高度事件
        float threshold = Screen.height * 0.25f;
        if (eventData.position.y > threshold)
        {
            Debug.Log("卡牌被拖到激活区域！");
            OnCardDraggedUp?.Invoke(cardType);
        }
    }

    public void ReturnToOriginPos()
    {

        StartCoroutine(ReturnToOrigin());

    }

    private IEnumerator ReturnToOrigin()
    {
        float t = 0f;
        Vector2 start = rect.anchoredPosition;

        while (t < 0.2f)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, originalPos, t / 0.2f);
            yield return null;
        }

        rect.anchoredPosition = originalPos;
        isDragging = false;
    }

}
