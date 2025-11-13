using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// �J�[�h��N���b�N�����Ƃ��ɉE�����L�����Đ�����o�鉉�o
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("�")]
    [SerializeField] private RectTransform background;   // �E���̔w�i�p�l��
    [SerializeField] private CanvasGroup textGroup;      // ����e�L�X�g�S�́iCanvasGroup�t���j

    [Header("�A�j���[�V�����ݒ�")]
    [SerializeField] private float collapsedWidth = 160f;  // �ʏ펞�̕�
    [SerializeField] private float expandedWidth = 320f;   // �J�������̕�
    [SerializeField] private float duration = 0.3f;        // �A�j���[�V��������

    private bool isExpanded = false;

    private void Start()
    {
        // ������Ԃ�ݒ�
        if (background != null)
        {
            background.sizeDelta = new Vector2(collapsedWidth, background.sizeDelta.y);
        }

        if (textGroup != null)
        {
            textGroup.alpha = 0f; // �e�L�X�g��\��
        }
    }

    /// <summary>
    /// �J�[�h�N���b�N���ɌĂяo��
    /// </summary>
    public void OnClickCard()
    {
        if (background == null || textGroup == null) return;

        if (isExpanded)
        {
            // ����A�j���[�V����
            background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.InOutSine);

            textGroup.DOFade(0f, 0.2f);

            Debug.Log("CardUI:�Ƃ�����");
        }
        else
        {
            // �J���A�j���[�V����
            background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.OutBack);

            textGroup.DOFade(1f, 0.3f).SetDelay(0.1f);

            Debug.Log("CardUI:�J������");
        }

        isExpanded = !isExpanded;
    }
}
