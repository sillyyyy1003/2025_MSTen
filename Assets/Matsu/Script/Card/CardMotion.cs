using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector2 originalPos;
    [SerializeField] private float hoverOffsetY = 20f;
    [SerializeField] private float duration = 0.2f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null)
        {
            rectTransform.DOAnchorPos(originalPos + new Vector2(0, hoverOffsetY), duration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null)
        {
            rectTransform.DOAnchorPos(originalPos, duration);
        }
    }
}

