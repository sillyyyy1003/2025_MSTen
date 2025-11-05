using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(PolygonCollider2D))]
public class HexButtonCollider : MonoBehaviour, IPointerClickHandler
{
	private PolygonCollider2D hexCollider2D;

	[Header("点击事件")]
	public UnityEvent onClick;
	void Awake()
	{
		//======= 设置六边形碰撞器 =======
		hexCollider2D = GetComponent<PolygonCollider2D>();

		Vector2[] hexPoints = new Vector2[6];
		hexPoints[0] = new Vector2(0f, 1f);
		hexPoints[1] = new Vector2(0.866f, 0.5f);
		hexPoints[2] = new Vector2(0.866f, -0.5f);
		hexPoints[3] = new Vector2(0f, -1f);
		hexPoints[4] = new Vector2(-0.866f, -0.5f);
		hexPoints[5] = new Vector2(-0.866f, 0.5f);

		hexCollider2D.pathCount = 1;      // 一个路径
		hexCollider2D.SetPath(0, hexPoints);
	}

	/// <summary>
	/// 点击事件（由 EventSystem 调用）
	/// </summary>
	public void OnPointerClick(PointerEventData eventData)
	{
		// 判断点击是否在六边形范围内（一般由 Collider 自动判断，但这里保险）
		if (hexCollider2D.OverlapPoint(eventData.pointerCurrentRaycast.worldPosition))
		{
			onClick?.Invoke();
		}
	}

	public void SetString()
	{
		GetComponentInChildren<Image>().color = Color.red;
	}


}
