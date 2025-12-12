using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SliderPanelSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect;
    public int pageCount = 3;
    public float snapSpeed = 10f;

    private RectTransform content;
    private float pageWidth;
    private bool dragging = false;
    private int currentPage = 0;

    void Start()
    {
        content = scrollRect.content;
        pageWidth = scrollRect.viewport.rect.width;
    }

    void Update()
    {
        if (!dragging)
        {
            float targetX = -pageWidth * currentPage;
            Vector2 pos = content.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, targetX, snapSpeed * Time.deltaTime);
            content.anchoredPosition = pos;
        }
    }

    public void OnBeginDrag(PointerEventData e) => dragging = true;

    public void OnEndDrag(PointerEventData e)
    {
        dragging = false;

        float currentX = content.anchoredPosition.x;

        int closestPage = 0;
        float closestDistance = Mathf.Abs(currentX + pageWidth * 0);

        for (int i = 1; i < pageCount; i++)
        {
            float dist = Mathf.Abs(currentX + pageWidth * i);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPage = i;
            }
        }

        currentPage = closestPage;
    }

    // ——按钮翻页：改 currentPage，让 Update 去吸附——
    public void Next()
    {
        currentPage = Mathf.Min(currentPage + 1, pageCount - 1);
        dragging = false;
    }

    public void Prev()
    {
        currentPage = Mathf.Max(currentPage - 1, 0);
        dragging = false;
    }
}