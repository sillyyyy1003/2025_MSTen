using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    // ��D�Ƃ��ĊǗ�����J�[�h
    private readonly List<CardExpand> handCards = new List<CardExpand>();
    private CardExpand activeCard = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// ��D�ɃJ�[�h��ǉ�
    /// </summary>
    public void RegisterCard(CardExpand card)
    {
        if (!handCards.Contains(card))
            handCards.Add(card);
    }

    /// <summary>
    /// ��D����J�[�h���폜
    /// </summary>
    public void UnregisterCard(CardExpand card)
    {
        if (handCards.Contains(card))
            handCards.Remove(card);
    }

    /// <summary>
    /// �J�[�h���W�J���ꂽ�Ƃ��Ăяo��
    /// </summary>
    public void OnCardExpanded(CardExpand card)
    {
        foreach (var c in handCards)
        {
            if (c == card) continue;
            c.Collapse(); // ���̃J�[�h�͕���
            c.SetDimmed(true); // ��A�N�e�B�u�J�[�h���Â�����
        }
        activeCard = card;
    }

    /// <summary>
    /// �J�[�h������ꂽ�Ƃ��Ăяo��
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
    /// ��D�̃J�[�h�𐮗�i��F�����сj
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
    /// ��D��S�ĕ���
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
    /// ��D�̃J�[�h�ꗗ���擾
    /// </summary>
    public IReadOnlyList<CardExpand> GetHandCards() => handCards.AsReadOnly();
}
