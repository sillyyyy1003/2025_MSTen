using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SceneOpenCrop : MonoBehaviour
{
    [Header("���уp�l��")]
    public RectTransform TopPanel;
    public RectTransform BottomPanel;

    [Header("�ݒ�")]
    public float duration = 1.0f;
    public Ease easeType = Ease.InOutQuad;

    [Header("�f�B���C�̕b��")]
    public float DelayTime = 1.0f;


    public void Start()
    {
        // �p�l�����\���ɂ���
        TopPanel.gameObject.SetActive(false);
        BottomPanel.gameObject.SetActive(false);

        PlayOpen();
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }

    /// <summary>
    /// ���т��O���ăI�[�v���ɂ���
    /// </summary>
    public void PlayOpen()
    {
        // �p�l����\����Ԃɂ���
        TopPanel.gameObject.SetActive(true);
        BottomPanel.gameObject.SetActive(true);

        // TopPanel��BottomPanel�̌��݂̍������擾
        float topHeight = TopPanel.sizeDelta.y;
        float bottomHeight = BottomPanel.sizeDelta.y;

        // TopPanel��BottomPanel�̍�����0�ɃA�j���[�V�����ŕύX
        TopPanel.DOSizeDelta(new Vector2(TopPanel.sizeDelta.x, 0), duration).SetEase(easeType).SetDelay(DelayTime);
        BottomPanel.DOSizeDelta(new Vector2(BottomPanel.sizeDelta.x, 0), duration).SetEase(easeType).SetDelay(DelayTime);
    }
}
