using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CardExpand : MonoBehaviour, IPointerClickHandler
{
    private static CardExpand expandedCard = null;

    [Header("右側パネル")]
    [SerializeField] private RectTransform background;
    [SerializeField] private float collapsedWidth = 160f;
    [SerializeField] private float expandedWidth = 320f;
    [SerializeField] private float duration = 0.3f;

    [Header("テキスト")]
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private float textFadeDuration = 0.3f;

    private bool isExpanded = false;
    private RectTransform rect;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        HandManager.Instance.RegisterCard(this);

        if (background != null)
        {
            background.sizeDelta = new Vector2(collapsedWidth, background.sizeDelta.y);
            background.pivot = new Vector2(0f, 0.5f);
            background.anchorMin = new Vector2(0f, 0.5f);
            background.anchorMax = new Vector2(0f, 0.5f);
        }

        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);

        if (textGroup != null)
            textGroup.alpha = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (background == null || textGroup == null) return;

        if (!isExpanded && expandedCard != null && expandedCard != this)
            expandedCard.Collapse();

        if (isExpanded) Collapse();
        else
        {
            Expand();
            expandedCard = this;
        }
    }

    private void Expand()
    {
        background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                  .SetEase(Ease.OutBack)
                  .OnComplete(() =>
                  {
                      Vector3 rightEdgeLocal = background.localPosition + new Vector3(background.rect.width * (1f - background.pivot.x), 0, 0);
                      HandManager.Instance.OnCardExpanded(this, rightEdgeLocal);
                  });

        textGroup.DOFade(1f, textFadeDuration);
        isExpanded = true;
    }

    public void Collapse(bool notifyHandManager = true)
    {
        background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                  .SetEase(Ease.InOutSine);
        textGroup.DOFade(0f, textFadeDuration);

        isExpanded = false;
        if (expandedCard == this) expandedCard = null;

        if (notifyHandManager)
            HandManager.Instance.OnCardCollapsed(this);
    }

    public void SetDimmed(bool dimmed) { }

    public bool IsExpanded => isExpanded;
}
