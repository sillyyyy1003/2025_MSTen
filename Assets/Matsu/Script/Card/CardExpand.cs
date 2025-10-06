
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardExpand : MonoBehaviour, IPointerClickHandler
{
    [Header("右側パネル")]
    [SerializeField] private RectTransform background;   // 右側パネル
    [SerializeField] private float collapsedWidth = 160f;
    [SerializeField] private float expandedWidth = 320f;
    [SerializeField] private float duration = 0.3f;

    [Header("テキスト")]
    [SerializeField] private CanvasGroup textGroup;       // テキストをまとめたCanvasGroup
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

        if (isExpanded)
        {
            // テキストをフェードアウト（背景と同時に）
            textGroup.DOFade(0f, textFadeDuration);
            // 背景を縮める
            background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.InOutSine);
        }
        else
        {
            // テキストをフェードイン（背景と同時に）
            textGroup.DOFade(1f, textFadeDuration);
            // 背景を広げる
            background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.OutBack);
        }


        isExpanded = !isExpanded;
    }
}