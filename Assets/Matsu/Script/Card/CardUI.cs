using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// カードをクリックしたときに右側が広がって説明が出る演出
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("構成要素")]
    [SerializeField] private RectTransform background;   // 右側の背景パネル
    [SerializeField] private CanvasGroup textGroup;      // 説明テキスト全体（CanvasGroup付き）

    [Header("アニメーション設定")]
    [SerializeField] private float collapsedWidth = 160f;  // 通常時の幅
    [SerializeField] private float expandedWidth = 320f;   // 開いた時の幅
    [SerializeField] private float duration = 0.3f;        // アニメーション時間

    private bool isExpanded = false;

    private void Start()
    {
        // 初期状態を設定
        if (background != null)
        {
            background.sizeDelta = new Vector2(collapsedWidth, background.sizeDelta.y);
        }

        if (textGroup != null)
        {
            textGroup.alpha = 0f; // テキスト非表示
        }
    }

    /// <summary>
    /// カードクリック時に呼び出す
    /// </summary>
    public void OnClickCard()
    {
        if (background == null || textGroup == null) return;

        if (isExpanded)
        {
            // 閉じるアニメーション
            background.DOSizeDelta(new Vector2(collapsedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.InOutSine);

            textGroup.DOFade(0f, 0.2f);

            Debug.Log("CardUI:とじたよ");
        }
        else
        {
            // 開くアニメーション
            background.DOSizeDelta(new Vector2(expandedWidth, background.sizeDelta.y), duration)
                      .SetEase(Ease.OutBack);

            textGroup.DOFade(1f, 0.3f).SetDelay(0.1f);

            Debug.Log("CardUI:開いたよ");
        }

        isExpanded = !isExpanded;
    }
}
