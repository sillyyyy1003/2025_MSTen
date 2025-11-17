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
	private Toggle toggle;
	

	[Header("点击事件")]
	public UnityEvent onClick;


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

	void Awake()
	{
		Collider = GetComponentInChildren<HexButtonCollider>();
		text = GetComponentInChildren<TextMeshProUGUI>();
		shadow = transform.Find("Shadow")?.GetComponent<Image>();
		background = transform.Find("Background")?.GetComponent<Image>();
		toggle = GetComponent<Toggle>();

		if (Collider == null)
		{
			Debug.LogWarning($"[{name}] HexButtonCollider not found.");
			return;
		}

		if (!isToggle)
		{
			// 禁用Toggle组件
			toggle.enabled = false;
			UpdateVisual(normalColor);
		}
		else
		{
			// 根据是否被选中 设置初始颜色
			UpdateVisual(toggle.isOn ? selectedColor : normalColor);
		}
		
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
			if (!toggle.isOn)
				UpdateVisual(hoverColor);
			
		}
		else if (!inside && isHover)
		{
			isHover = false;
			UpdateVisual(toggle.isOn ? selectedColor : normalColor);
		}

		// ---------------- Press ----------------
		if (inside && mouseDown && !isPressed)
		{
			isPressed = true;
			shadow.gameObject.SetActive(false);
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
					// 如果已经被选中 则不会变化
					if (!toggle.isOn)
					{
						// Update other toggle color if has toggle group
						if(toggle.group)
						{
							// 如果有其他被选中的 toggle 则重置颜色 并且重置状态
							if (toggle.group.GetFirstActiveToggle()) toggle.group.GetFirstActiveToggle().GetComponent<HexButton>().ResetHexButton();
						}
						// switch toggle
						toggle.isOn = true;
						// Update color
						UpdateVisual(selectedColor);

						onClick?.Invoke();
					}
				}
				else
				{
					UpdateVisual(hoverColor);
					onClick?.Invoke();
				}

			}
			else
			{
				UpdateVisual(toggle.isOn ? selectedColor : normalColor);
				
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

	public void ResetHexButton()
	{
		// Reset color
		UpdateVisual(normalColor);

		if (toggle != null) toggle.isOn = false;
	}
}
