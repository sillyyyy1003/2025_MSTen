using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("カードの浮き方")]
    private Vector2 originalPos;
    [SerializeField] private float hoverOffsetY = 20f;
    [SerializeField] private float duration = 0.2f;

    [Header("カードの詳細表示")]
    [SerializeField] private float expandedWidth = 300f; // カードを広げる幅
    [SerializeField] private GameObject detailText; // 詳細テキストオブジェクト
    [SerializeField] private RectTransform background; // 背景オブジェクト
    private Vector2 originalBackgroundSize; // 背景の元のサイズ
    private bool isExpanded = false; // カードが展開されているかどうか

    private RectTransform rectTransform;
    private static CardMotion currentlyExpandedCard; // 現在展開されているカード

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;

        if (background != null)
        {
            originalBackgroundSize = background.sizeDelta; // 背景の元のサイズを保存
        }

        if (detailText != null)
        {
            detailText.SetActive(false); // 初期状態では非表示
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null && !isExpanded)
        {
            // カード本体を浮かせる
            rectTransform.DOAnchorPos(originalPos + new Vector2(0, hoverOffsetY), duration);

            // 背景も一緒に浮かせる
            if (background != null)
            {
                background.DOAnchorPos(background.anchoredPosition + new Vector2(0, hoverOffsetY), duration);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null && !isExpanded)
        {
            // カード本体を元の位置に戻す
            rectTransform.DOAnchorPos(originalPos, duration);

            // 背景も元の位置に戻す
            if (background != null)
            {
                background.DOAnchorPos(background.anchoredPosition - new Vector2(0, hoverOffsetY), duration);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isExpanded)
        {
            CloseCard();
        }
        else
        {
            if (currentlyExpandedCard != null && currentlyExpandedCard != this)
            {
                currentlyExpandedCard.CloseCard(); // 他のカードを閉じる
            }

            ExpandCard();
        }
    }

    private void ExpandCard()
    {
        isExpanded = true;
        currentlyExpandedCard = this;

        // カードを最前面に移動
        transform.SetAsLastSibling();

        // 背景を表示
        if (background != null)
        {
            background.gameObject.SetActive(true); // 背景を表示
            background.DOSizeDelta(new Vector2(expandedWidth, originalBackgroundSize.y), duration);
        }

        // カードを左に移動する
        float moveLeftOffset = expandedWidth / 2; // カードの幅が広がる分の半分だけ左に移動
        rectTransform.DOAnchorPos(originalPos - new Vector2(moveLeftOffset, 0), duration);

        // 詳細テキストを表示
        if (detailText != null)
        {
            detailText.SetActive(true);
        }

        // 他のカードを左右に移動
        //AdjustOtherCards();
    }

    private void CloseCard()
    {
        isExpanded = false;
        currentlyExpandedCard = null;

        // 背景を非表示
        if (background != null)
        {
            background.DOSizeDelta(originalBackgroundSize, duration).OnComplete(() =>
            {
                background.gameObject.SetActive(false); // 背景を非表示
            });
        }

        // カードを元の位置に戻す
        rectTransform.DOAnchorPos(originalPos, duration);

        // 詳細テキストを非表示
        if (detailText != null)
        {
            detailText.SetActive(false);
        }

        // 他のカードを元の位置に戻す
        ResetOtherCards();
    }

    private void AdjustOtherCards()
    {
        // Panelの右端の位置を取得
        RectTransform panelRect = transform.parent.GetComponent<RectTransform>();
        float panelRightEdge = panelRect.rect.width;

        // 他のカードを左右に移動するロジックを実装
        foreach (var card in FindObjectsOfType<CardMotion>())
        {
            if (card != this)
            {
                // クリックされたカードの右側にあるカードのみ移動
                if (card.transform.position.x > transform.position.x)
                {
                    // カードをPanelの右端に移動
                    card.rectTransform.DOAnchorPos(new Vector2(panelRightEdge, card.originalPos.y), duration);

                    // 背景もカード本体に合わせて移動
                    if (card.background != null)
                    {
                        card.background.DOAnchorPos(new Vector2(panelRightEdge, card.originalPos.y), duration);
                    }
                }
            }
        }
    }

    private void ResetOtherCards()
    {
        // 他のカードを元の位置に戻すロジックを実装
        foreach (var card in FindObjectsOfType<CardMotion>())
        {
            if (card != this)
            {
                card.rectTransform.DOAnchorPos(card.originalPos, duration);
            }
        }
    }
}


