
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardExpand : MonoBehaviour, IPointerClickHandler
{
    private static CardExpand expandedCard = null; // 現在展開中のカード

    [Header("右側パネル")]
    [SerializeField] private RectTransform background;   // 右側パネル
    [SerializeField] private float collapsedWidth = 160f;
    [SerializeField] private float expandedWidth = 320f;
    [SerializeField] private float duration = 0.3f;

    [Header("テキスト")]
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private float textFadeDuration = 0.3f;

    private bool isExpanded = false;

    private void Start()
    {
        if (background != null)
            background.sizeDelta = new Vector2(collapsedWidth, background.sizeDelta.y);

        if (textGroup != null)
            textGroup.alpha = 0f; // 初期非表示
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (background == null || textGroup == null) return;

        // すでに他のカードが展開中なら閉じる
        if (!isExpanded && expandedCard != null && expandedCard != this)
        {
            expandedCard.Collapse();
        }

        if (isExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
            expandedCard = this;
        }
    }

    private void Expand()
    {
        textGroup.DOFade(1f, textFadeDuration);
        background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                  .SetEase(Ease.OutBack);
        isExpanded = true;
    }

    private void Collapse()
    {
        textGroup.DOFade(0f, textFadeDuration);
        background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                  .SetEase(Ease.InOutSine);
        isExpanded = false;
        if (expandedCard == this) expandedCard = null;
    }
}