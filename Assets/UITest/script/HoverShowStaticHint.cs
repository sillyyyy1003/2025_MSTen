using UnityEngine;
using UnityEngine.EventSystems;

public class HoverShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject tooltipPanel;     // 小框

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!tooltipPanel) return;

        tooltipPanel.SetActive(true);


        var rt = tooltipPanel.transform as RectTransform;


        tooltipPanel.transform.SetAsLastSibling(); // 置顶显示（不被别的UI盖住）
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!tooltipPanel) return;
        tooltipPanel.SetActive(false);
    }
}