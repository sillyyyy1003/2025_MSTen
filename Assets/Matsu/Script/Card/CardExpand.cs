using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardExpand : MonoBehaviour, IPointerClickHandler
{
    private static CardExpand expandedCard = null; // ���ݓW�J���̃J�[�h

    [Header("�E���p�l��")]
    [SerializeField] private RectTransform background;   // �E���p�l��
    [SerializeField] private float collapsedWidth = 160f;
    [SerializeField] private float expandedWidth = 320f;
    [SerializeField] private float duration = 0.3f;

    [Header("�e�L�X�g")]
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private float textFadeDuration = 0.3f;

    private bool isExpanded = false;

    private void Start()
    {
        if (background != null)
            background.sizeDelta = new Vector2(collapsedWidth, background.sizeDelta.y);

        if (textGroup != null)
            textGroup.alpha = 0f; // ������\��
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (background == null || textGroup == null) return;

        // ���łɑ��̃J�[�h���W�J���Ȃ����
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

    public void Collapse()
    {
        textGroup.DOFade(0f, textFadeDuration);
        background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                  .SetEase(Ease.InOutSine);
        isExpanded = false;
        if (expandedCard == this) expandedCard = null;
    }

    public void SetDimmed(bool dimmed)
    {
        // ��: CanvasGroup�œ����x��ς���ꍇ
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = dimmed ? 0.5f : 1f;
            cg.interactable = !dimmed;
            cg.blocksRaycasts = !dimmed;
        }
        // �������͐F�ύX�ȂǁAUI�ɍ��킹�Ď���
    }
}