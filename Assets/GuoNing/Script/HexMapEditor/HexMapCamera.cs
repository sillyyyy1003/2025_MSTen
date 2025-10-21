using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
	static HexMapCamera instance;

	Transform swivel, stick;
	float zoom = 1f;
	public float stickMinZoom, stickMaxZoom;
	public float swivelMinZoom, swivelMaxZoom;
	public float moveSpeedMinZoom, moveSpeedMaxZoom;
	public HexGrid grid;
	public float rotationSpeed;
	float rotationAngle;
	void Awake()
	{
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

	void Update()
	{
		// Adjust camera zoom
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f)
		{
			AdjustZoom(zoomDelta);
		}

		// Adjust camera rotation
		//float rotationDelta = Input.GetAxis("Rotation");
		//if (rotationDelta != 0f)
		//{
		//	AdjustRotation(rotationDelta);
		//}

		// Adjust camera position
		float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");
		if (xDelta != 0f || zDelta != 0f)
		{
			AdjustPosition(xDelta, zDelta);
		}
	}
	void AdjustRotation(float delta)
	{
		rotationAngle += delta * rotationSpeed * Time.deltaTime;
		if (rotationAngle < 0f)
		{
			rotationAngle += 360f;
		}
		else if (rotationAngle >= 360f)
		{
			rotationAngle -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

	void AdjustPosition(float xDelta, float zDelta)
	{
		Vector3 direction =
			transform.localRotation *
			new Vector3(xDelta, 0f, zDelta).normalized;

		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance =
			Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
			damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;
		transform.localPosition = ClampPosition(position);
	}

	Vector3 ClampPosition(Vector3 position)
	{
		float xMax = (grid.CellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		return position;
	}

	void AdjustZoom(float delta)
	{
		zoom = Mathf.Clamp01(zoom + delta);

		float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	void OnEnable()
	{
		instance = this;
	}

	public static bool Locked
	{
		set
		{
			instance.enabled = !value;
		}
	}

	// 25.10.10 RI 删除Camera相关避免loadMap出错

	//public static void ValidatePosition()
	//{
	//	instance.AdjustPosition(0f, 0f);
	//}
}
