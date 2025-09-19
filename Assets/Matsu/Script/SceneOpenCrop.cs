using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SceneOpenCrop : MonoBehaviour
{
    [Header("黒帯パネル")]
    public RectTransform TopPanel;
    public RectTransform BottomPanel;

    [Header("設定")]
    public float duration = 1.0f;
    public Ease easeType = Ease.InOutQuad;

    [Header("ディレイの秒数")]
    public float DelayTime = 1.0f;


    public void Start()
    {
        // パネルを非表示にする
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
    /// 黒帯を外してオープンにする
    /// </summary>
    public void PlayOpen()
    {
        // パネルを表示状態にする
        TopPanel.gameObject.SetActive(true);
        BottomPanel.gameObject.SetActive(true);

        // TopPanelとBottomPanelの現在の高さを取得
        float topHeight = TopPanel.sizeDelta.y;
        float bottomHeight = BottomPanel.sizeDelta.y;

        // TopPanelとBottomPanelの高さを0にアニメーションで変更
        TopPanel.DOSizeDelta(new Vector2(TopPanel.sizeDelta.x, 0), duration).SetEase(easeType).SetDelay(DelayTime);
        BottomPanel.DOSizeDelta(new Vector2(BottomPanel.sizeDelta.x, 0), duration).SetEase(easeType).SetDelay(DelayTime);
    }
}
