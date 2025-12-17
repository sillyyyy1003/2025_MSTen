using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ImageBouncer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform targetImage; // 跳ねさせたい背後の画像
    private Vector2 originalPosition;
    public float liftAmount=30f;
    public float duration=0.2f;


    void Start()
    {
        if (targetImage != null)
        {
            originalPosition = targetImage.anchoredPosition;
        }
    }

    // マウスがボタンに入った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage == null) return;

        // すでに動いているアニメーションを止めてリセット
        targetImage.DOKill();
        targetImage.anchoredPosition = originalPosition;

        // 跳ねる演出 (上に30ピクセル移動して、弾むように戻る)
        targetImage.DOAnchorPosY(originalPosition.y +liftAmount, duration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage == null) return;

        targetImage.DOKill();
        targetImage.DOAnchorPosY(originalPosition.y, duration).SetEase(Ease.OutQuad);
    }
}