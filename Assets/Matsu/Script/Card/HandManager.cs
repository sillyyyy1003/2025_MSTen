using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    private readonly List<CardExpand> handCards = new List<CardExpand>();
    private readonly Dictionary<CardExpand, Vector3> originalPositions = new Dictionary<CardExpand, Vector3>();

    [SerializeField] private float firstOffset = 10f;      // 1枚目右側カードの間隔
    [SerializeField] private float overlapHalf = 80f;      // 2枚目以降の半重なり幅
    [SerializeField] private float tweenDuration = 0.3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterCard(CardExpand card)
    {
        if (!handCards.Contains(card))
        {
            handCards.Add(card);
            originalPositions[card] = card.GetComponent<RectTransform>().localPosition;
        }
    }

    public void OnCardExpanded(CardExpand expandedCard, Vector3 expandedRightEdgeLocal)
    {
        // 右側カードを抽出
        List<RectTransform> rightCards = new List<RectTransform>();
        foreach (var card in handCards)
        {
            if (card == expandedCard) continue;
            var rt = card.GetComponent<RectTransform>();
            if (rt != null && rt.localPosition.x > expandedCard.GetComponent<RectTransform>().localPosition.x)
            {
                rightCards.Add(rt);
            }
        }

        // 左から右にソート（左端基準）
        rightCards.Sort((a, b) =>
        {
            float aLeft = a.localPosition.x - a.rect.width * a.pivot.x;
            float bLeft = b.localPosition.x - b.rect.width * b.pivot.x;
            return aLeft.CompareTo(bLeft);
        });

        float currentX = expandedRightEdgeLocal.x + firstOffset;

        for (int i = 0; i < rightCards.Count; i++)
        {
            var rt = rightCards[i];
            rt.GetComponent<CardExpand>().Collapse(false);

            float targetX;
            if (i == 0)
            {
                // 1枚目：展開カード右端＋間隔
                targetX = currentX + rt.rect.width * rt.pivot.x;
            }
            else
            {
                // 2枚目以降：前のカードに半重なり
                targetX = currentX + overlapHalf;
            }

            Vector3 targetPos = new Vector3(targetX, rt.localPosition.y, rt.localPosition.z);
            rt.DOLocalMove(targetPos, tweenDuration).SetEase(Ease.OutSine);
            rt.SetAsLastSibling();

            currentX = targetX; // 次カードの基準位置更新
        }
    }

    public void OnCardCollapsed(CardExpand card)
    {
        foreach (var c in handCards)
        {
            var rt = c.GetComponent<RectTransform>();
            if (rt != null && originalPositions.ContainsKey(c))
            {
                rt.DOLocalMove(originalPositions[c], tweenDuration).SetEase(Ease.OutSine);
            }
        }
    }
}
