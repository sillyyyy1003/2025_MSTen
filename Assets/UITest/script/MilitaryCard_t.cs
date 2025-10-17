using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 军事卡牌 UI 控制器（dataPanel 从卡牌左侧滑到右侧）
/// 锚点要求：dataPanel 的锚点为左侧居中 (Anchor: Left Middle, Pivot: 0, 0.5)
/// </summary>
public class MilitaryCard_t : MonoBehaviour
{
	[Header("UI References")]
	public Image militaryCardImage;     // 卡牌主图
	public GameObject dataPanel;        // 数据面板（默认隐藏）
	public Text dataText_HpText;        // HP 文本
	public Text dataText_AttackText;    // 攻击力文本


	[Header("Panel Animation Settings")]
	public float panelSlideSpeed = 6f;      // 滑动速度

	private RectTransform dataPanelRect;
	private CanvasGroup dataPanelCanvas;
	private RectTransform cardRect;

	private Vector3 originalPosition;
	private Vector3 targetPosition;
	private bool isHovered = false;
	private bool showDataPanel = false;

	// 滑动起始与结束位置
	private Vector2 panelHiddenPos;
	private Vector2 panelVisiblePos;

	void Start()
	{
		originalPosition = transform.localPosition;
		targetPosition = originalPosition;

		if (dataPanel != null && militaryCardImage != null)
		{
			cardRect = militaryCardImage.GetComponent<RectTransform>();
			dataPanelRect = dataPanel.GetComponent<RectTransform>();

			dataPanelCanvas = dataPanel.GetComponent<CanvasGroup>();
			if (dataPanelCanvas == null)
				dataPanelCanvas = dataPanel.AddComponent<CanvasGroup>();

			dataPanel.SetActive(false);
			dataPanelCanvas.alpha = 0f;

			// === 计算位置 ===
			// 这里的坐标全部在父级局部空间内
			RectTransform parentRect = cardRect.parent as RectTransform;

			// 卡牌的宽度
			float cardWidth = cardRect.rect.width;

			// 卡牌的 anchoredPosition
			Vector2 cardPos = cardRect.anchoredPosition;

			// 卡牌锚点居中
			// 初始隐藏位置：dataPanel 左侧与 card 左侧对齐（重叠）
			panelHiddenPos = new Vector2(cardPos.x - 0f, cardPos.y);
			// 可见位置：dataPanel 左侧贴到 card 右侧
			panelVisiblePos = new Vector2(cardPos.x + cardWidth, cardPos.y);

			dataPanelRect.anchoredPosition = panelHiddenPos;
		}
	}

	void Update()
	{
		
		// 模拟数据更新
		if (showDataPanel)
		{
			dataText_HpText.text = "HP: 100";
			dataText_AttackText.text = "Attack: 20";
		}
	}

	public void PanelEvent()
	{
		if (showDataPanel)
		{
			ClosePanel();
		}
		else
		{
			ShowPanel();
		}
	}

	public void ShowPanel()
	{
		if (showDataPanel) return;

		showDataPanel = true;
		dataPanel.SetActive(true);

		StopAllCoroutines();
		StartCoroutine(SlidePanelIn());
	}

	public void ClosePanel()
	{
		if (!showDataPanel) return;
		showDataPanel = false;
		StopAllCoroutines();
		StartCoroutine(SlidePanelOut());
	}

	private IEnumerator SlidePanelIn()
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * panelSlideSpeed;
			dataPanelRect.anchoredPosition = Vector2.Lerp(panelHiddenPos, panelVisiblePos, t);
			dataPanelCanvas.alpha = Mathf.Lerp(0f, 1f, t);
			yield return null;
		}
		dataPanelRect.anchoredPosition = panelVisiblePos;
		dataPanelCanvas.alpha = 1f;
	}

	private IEnumerator SlidePanelOut()
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * panelSlideSpeed;
			// 变更panel的位置
			dataPanelRect.anchoredPosition = Vector2.Lerp(panelVisiblePos, panelHiddenPos, t);
			dataPanelCanvas.alpha = Mathf.Lerp(1f, 0f, t);
			yield return null;
		}
		// 确保位置最终值
		dataPanelRect.anchoredPosition = panelHiddenPos;
		dataPanelCanvas.alpha = 0f;
		dataPanel.SetActive(false);
	}

	public bool GetPanelOpen()
	{
		return showDataPanel;
	}
}
