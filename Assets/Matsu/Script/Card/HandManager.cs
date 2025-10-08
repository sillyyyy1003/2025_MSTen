using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    // 手札として管理するカード
    private readonly List<CardExpand> handCards = new List<CardExpand>();
    private CardExpand activeCard = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 手札にカードを追加
    /// </summary>
    public void RegisterCard(CardExpand card)
    {
        if (!handCards.Contains(card))
            handCards.Add(card);
    }

    /// <summary>
    /// 手札からカードを削除
    /// </summary>
    public void UnregisterCard(CardExpand card)
    {
        if (handCards.Contains(card))
            handCards.Remove(card);
    }

    /// <summary>
    /// カードが展開されたとき呼び出す
    /// </summary>
    public void OnCardExpanded(CardExpand card)
    {
        foreach (var c in handCards)
        {
            if (c == card) continue;
            c.Collapse(); // 他のカードは閉じる
            c.SetDimmed(true); // 非アクティブカードを暗くする
        }
        activeCard = card;
    }

    /// <summary>
    /// カードが閉じられたとき呼び出す
    /// </summary>
    public void OnCardCollapsed(CardExpand card)
    {
        if (activeCard == card)
            activeCard = null;

        foreach (var c in handCards)
        {
            c.SetDimmed(false);
        }
    }

    /// <summary>
    /// 手札のカードを整列（例：横並び）
    /// </summary>
    public void ArrangeHand(float startX, float spacing)
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            var card = handCards[i];
            var rt = card.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(startX + i * spacing, rt.anchoredPosition.y);
            }
        }
    }

    /// <summary>
    /// 手札を全て閉じる
    /// </summary>
    public void CollapseAll()
    {
        foreach (var c in handCards)
        {
            c.Collapse();
            c.SetDimmed(false);
        }
        activeCard = null;
    }

    /// <summary>
    /// 手札のカード一覧を取得
    /// </summary>
    public IReadOnlyList<CardExpand> GetHandCards() => handCards.AsReadOnly();
}
