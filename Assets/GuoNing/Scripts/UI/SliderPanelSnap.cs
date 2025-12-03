using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

		// 一页宽度 = Viewport 宽度
		pageWidth = scrollRect.viewport.rect.width;
	}

	void Update()
	{
		if (!dragging)
		{
			float targetX = -pageWidth * currentPage; // anchoredPosition 目标位置
			Vector2 pos = content.anchoredPosition;
			pos.x = Mathf.Lerp(pos.x, targetX, snapSpeed * Time.deltaTime);
			content.anchoredPosition = pos;
		}
	}

	public void OnBeginDrag(PointerEventData e)
	{
		dragging = true;
	}

	public void OnEndDrag(PointerEventData e)
	{
		dragging = false;

		// 计算当前 Content 的 X
		float currentX = content.anchoredPosition.x;

		// 计算距离每个 page 的误差
		float closestDistance = Mathf.Abs(currentX + pageWidth * 0);
		int closestPage = 0;

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
}
