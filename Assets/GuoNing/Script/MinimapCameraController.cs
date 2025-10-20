using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCameraController : MonoBehaviour
{
	public int cellCountX, cellCountZ;

	public float heightOffset = 50f;  // 摄像机离地高度
	public float borderPadding = 5f;  // 防止边界被裁掉

	Camera minimapCam;

	//void Start()
	//{
	//	minimapCam = GetComponent<Camera>();
	//	PositionCamera(cellCountX, cellCountZ);
	//}

	public void Init()
	{
		minimapCam = GetComponent<Camera>();
		//PositionCamera(cellCountX, cellCountZ);

	}

	public void PositionCamera(int cellCountX, int cellCountZ)
	{
		float mapWidth = (cellCountX + 0.5f) * (HexMetrics.innerRadius * 2f);
		float mapHeight = (cellCountZ * 0.75f + 0.25f) * (HexMetrics.outerRadius * 2f);

		// 居中位置
		Vector3 centerPos = new Vector3(mapWidth * 0.5f, heightOffset, mapHeight * 0.5f);

		// 设置摄像机位置与角度
		transform.position = centerPos;
		transform.rotation = Quaternion.Euler(90f, 0f, 0f);

		// 使用正交投影
		minimapCam.orthographic = true;
		minimapCam.orthographicSize = Mathf.Max(mapWidth, mapHeight) / 2f + borderPadding;

		// 层级控制：只显示地形层
		//minimapCam.cullingMask = LayerMask.GetMask("Terrain");  // 记得在Unity里给地形指定Terrain层
		minimapCam.clearFlags = CameraClearFlags.SolidColor;
		minimapCam.backgroundColor = Color.black;
	}
}