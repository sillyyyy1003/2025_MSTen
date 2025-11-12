using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(PolygonCollider2D))]
public class HexButtonCollider : MonoBehaviour
{
	public PolygonCollider2D hexCollider2D { get; private set; }

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


}
