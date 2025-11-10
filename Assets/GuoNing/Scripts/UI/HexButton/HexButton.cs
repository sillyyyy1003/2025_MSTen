using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class HexButton : MonoBehaviour
{
	private HexButtonCollider Collider;
	private TextMeshProUGUI text;
	private Image background;
	private Image shadow;

	[Header("点击事件")]
	public UnityEvent onClick;
	[Header("Toggle 事件")]
	public UnityEvent<bool> onToggleChanged; // 参数为 true=选中, false=未选中

	[Header("颜色设置")]
	public Color normalColor = Color.white;
	public Color hoverColor = new Color(0.9f, 0.9f, 1f);
	public Color pressedColor = new Color(0.8f, 0.8f, 0.8f);
	public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
	public Color selectedColor = new Color(0.6f, 0.8f, 1f); // 选中状态颜色

	[Header("状态设置")]
	public bool interactable = true;
	public bool isToggle = false;  // 是否为切换按钮

	private bool isHover = false;
	private bool isPressed = false;
	public bool isOn = false;     // 当前是否选中

	void Awake()
	{
		Collider = GetComponentInChildren<HexButtonCollider>();
		text = GetComponentInChildren<TextMeshProUGUI>();
		shadow = transform.Find("Shadow")?.GetComponent<Image>();
		background = transform.Find("Background")?.GetComponent<Image>();

		if (Collider == null)
		{
			Debug.LogWarning($"[{name}] HexButtonCollider not found.");
			return;
		}

		UpdateVisual(normalColor);
	}

	void Update()
	{
		if (!interactable || Collider == null || Collider.hexCollider2D == null)
			return;

		Vector3 mousePos = Input.mousePosition;
		mousePos.z = 100f; // Canvas 距离 Camera 的距离
		Vector3 worldPos =Camera.main.ScreenToWorldPoint(mousePos);
		bool inside = Collider.hexCollider2D.OverlapPoint(worldPos);
		
		bool mouseDown = Input.GetMouseButton(0);
		bool mouseUp = Input.GetMouseButtonUp(0);


		// ---------------- Hover ----------------
		if (inside && !isHover)
		{
			isHover = true;
			if (!isOn)
				UpdateVisual(hoverColor);
		}
		else if (!inside && isHover)
		{
			isHover = false;
			UpdateVisual(isOn ? selectedColor : normalColor);
		}

		// ---------------- Press ----------------
		if (inside && mouseDown && !isPressed)
		{
			isPressed = true;
			shadow.enabled = false;
			UpdateVisual(pressedColor);
		}

		if (isPressed && mouseUp)
		{
			// 点击确认
			if (inside)
			{
				shadow.gameObject.SetActive(true);  // 启用阴影
				// 如果是切换状态
				if (isToggle)
				{
					isOn = !isOn;
					UpdateVisual(isOn ? selectedColor : normalColor);
					onToggleChanged?.Invoke(isOn);
				}
				else
				{
					UpdateVisual(hoverColor);
					onClick?.Invoke();
				}

			}
			else
			{
				UpdateVisual(isOn ? selectedColor : normalColor);
				

			}
		
			isPressed = false;
		}
	}

	/// <summary>
	/// 按钮文字赋值
	/// </summary>
	public void SetText(string newText)
	{
		if (text != null)
			text.text = newText;
	}

	private void UpdateVisual(Color targetColor)
	{
		if (background != null)
			background.color = targetColor;
	}
}
