using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("�J�[�h�̕�����")]
    private Vector2 originalPos;
    [SerializeField] private float hoverOffsetY = 20f;
    [SerializeField] private float duration = 0.2f;

    [Header("�J�[�h�̏ڍ")]
    [SerializeField] private float expandedWidth = 300f; // �J�[�h��L���镝
    [SerializeField] private GameObject detailText; // �ڍ׃e�L�X�g�I�u�W�F�N�g
    [SerializeField] private RectTransform background; // �w�i�I�u�W�F�N�g
    private Vector2 originalBackgroundSize; // �w�i�̌��̃T�C�Y
    private bool isExpanded = false; // �J�[�h���W�J����Ă��邩�ǂ���

    private RectTransform rectTransform;
    private static CardMotion currentlyExpandedCard; // ���ݓW�J����Ă���J�[�h

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;

        if (background != null)
        {
            originalBackgroundSize = background.sizeDelta; // �w�i�̌��̃T�C�Y��ۑ�
        }

        if (detailText != null)
        {
            detailText.SetActive(false); // ������Ԃł͔�\��
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null && !isExpanded)
        {
            // �J�[�h�{�̂𕂂�����
            rectTransform.DOAnchorPos(originalPos + new Vector2(0, hoverOffsetY), duration);

            // �w�i��ꏏ�ɕ�������
            if (background != null)
            {
                background.DOAnchorPos(background.anchoredPosition + new Vector2(0, hoverOffsetY), duration);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CompareTag("Card") && rectTransform != null && !isExpanded)
        {
            // �J�[�h�{�̂���̈ʒu�ɖ߂�
            rectTransform.DOAnchorPos(originalPos, duration);

            // �w�i����̈ʒu�ɖ߂�
            if (background != null)
            {
                background.DOAnchorPos(background.anchoredPosition - new Vector2(0, hoverOffsetY), duration);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isExpanded)
        {
            CloseCard();
        }
        else
        {
            if (currentlyExpandedCard != null && currentlyExpandedCard != this)
            {
                currentlyExpandedCard.CloseCard(); // ���̃J�[�h�����
            }

            ExpandCard();
        }
    }

    private void ExpandCard()
    {
        isExpanded = true;
        currentlyExpandedCard = this;

        // �J�[�h��őO�ʂɈړ�
        transform.SetAsLastSibling();

        // �w�i��\��
        if (background != null)
        {
            background.gameObject.SetActive(true); // �w�i��\��
            background.DOSizeDelta(new Vector2(expandedWidth, originalBackgroundSize.y), duration);
        }

        // �J�[�h����Ɉړ�����
        float moveLeftOffset = expandedWidth / 2; // �J�[�h�̕����L���镪�̔����������Ɉړ�
        rectTransform.DOAnchorPos(originalPos - new Vector2(moveLeftOffset, 0), duration);

        // �ڍ׃e�L�X�g��\��
        if (detailText != null)
        {
            detailText.SetActive(true);
        }

        // ���̃J�[�h����E�Ɉړ�
        //AdjustOtherCards();
    }

    private void CloseCard()
    {
        isExpanded = false;
        currentlyExpandedCard = null;

        // �w�i���\��
        if (background != null)
        {
            background.DOSizeDelta(originalBackgroundSize, duration).OnComplete(() =>
            {
                background.gameObject.SetActive(false); // �w�i���\��
            });
        }

        // �J�[�h����̈ʒu�ɖ߂�
        rectTransform.DOAnchorPos(originalPos, duration);

        // �ڍ׃e�L�X�g���\��
        if (detailText != null)
        {
            detailText.SetActive(false);
        }

        // ���̃J�[�h����̈ʒu�ɖ߂�
        ResetOtherCards();
    }

    private void AdjustOtherCards()
    {
        // Panel�̉E�[�̈ʒu��擾
        RectTransform panelRect = transform.parent.GetComponent<RectTransform>();
        float panelRightEdge = panelRect.rect.width;

        // ���̃J�[�h����E�Ɉړ����郍�W�b�N�����
        foreach (var card in FindObjectsOfType<CardMotion>())
        {
            if (card != this)
            {
                // �N���b�N���ꂽ�J�[�h�̉E���ɂ���J�[�h�݈̂ړ�
                if (card.transform.position.x > transform.position.x)
                {
                    // �J�[�h��Panel�̉E�[�Ɉړ�
                    card.rectTransform.DOAnchorPos(new Vector2(panelRightEdge, card.originalPos.y), duration);

                    // �w�i��J�[�h�{�̂ɍ��킹�Ĉړ�
                    if (card.background != null)
                    {
                        card.background.DOAnchorPos(new Vector2(panelRightEdge, card.originalPos.y), duration);
                    }
                }
            }
        }
    }

    private void ResetOtherCards()
    {
        // ���̃J�[�h����̈ʒu�ɖ߂����W�b�N�����
        foreach (var card in FindObjectsOfType<CardMotion>())
        {
            if (card != this)
            {
                card.rectTransform.DOAnchorPos(card.originalPos, duration);
            }
        }
    }
}


