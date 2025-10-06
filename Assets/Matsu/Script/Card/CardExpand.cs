
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardExpand : MonoBehaviour, IPointerClickHandler
{
    [Header("�E���p�l��")]
    [SerializeField] private RectTransform background;   // �E���p�l��
    [SerializeField] private float collapsedWidth = 160f;
    [SerializeField] private float expandedWidth = 320f;
    [SerializeField] private float duration = 0.3f;

    [Header("�e�L�X�g")]
    [SerializeField] private CanvasGroup textGroup;       // �e�L�X�g���܂Ƃ߂�CanvasGroup
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

        if (isExpanded)
        {
            // �e�L�X�g���t�F�[�h�A�E�g�i�w�i�Ɠ����Ɂj
            textGroup.DOFade(0f, textFadeDuration);
            // �w�i���k�߂�
            background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.InOutSine);
        }
        else
        {
            // �e�L�X�g���t�F�[�h�C���i�w�i�Ɠ����Ɂj
            textGroup.DOFade(1f, textFadeDuration);
            // �w�i���L����
            background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.OutBack);
        }


        isExpanded = !isExpanded;
    }
}