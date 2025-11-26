using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 用于显示玩家的血量
/// </summary>
public class HPBar : MonoBehaviour
{
	public Transform PieceTarget;   // 血条的对象
    public Vector3 Offset;          // 血条的偏移位置

    [Header("SegmentPrefab")] 
    public Image SegmentPrefab;     // 血条分段预制体

    public RectTransform SegmentRoot;   // HorizonLayoutGroup节点

    private List<Image> segments = new();   // 血条分段列表

    public void InitSegments(int maxHP, Transform target, Vector3 offset)
	{
		PieceTarget = target;
		Offset = offset;

		// 清除旧的格子
		foreach (var seg in segments)
			Destroy(seg.gameObject);
		segments.Clear();

		// 生成新格子
		for (int i = 0; i < maxHP; i++)
		{
			var seg = Instantiate(SegmentPrefab, SegmentRoot);
			seg.color = Color.white; // 满血颜色
			segments.Add(seg);
		}
	}

	public void SetHP(int hp)
	{
		for (int i = 0; i < segments.Count; i++)
		{
			segments[i].enabled = (i < hp);
		}
	}

	void LateUpdate()
	{
		if (!PieceTarget) return;

		// 位置跟随
		transform.position = PieceTarget.position + Offset;

		// 面向相机
		transform.forward = Camera.main.transform.forward;
	}
}
