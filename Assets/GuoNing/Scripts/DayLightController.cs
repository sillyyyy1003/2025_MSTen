using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
	[Header("Time Settings")]
	[Tooltip("完整昼夜的总时长（秒）")]
	public float dayLengthInSeconds = 60f;
	[Range(0f, 1f), Tooltip("当前时间（0=凌晨，0.5=正午，1=回到凌晨）")]
	public float timeOfDay = 0f;
	public bool autoProgressTime = true;

	[Header("Light Settings")]
	public Light sunLight;

	[Tooltip("太阳最低点（午夜））的旋转角度")]
	public Vector3 nightRotation = new Vector3(0f, 0f, 0f);

	[Tooltip("太阳最高点（正午））的旋转角度")]
	public Vector3 dayRotation = new Vector3(170f, 0f, 0f);

	[Header("Color Settings")]
	public Gradient lightColor;   // 使用 Gradient 控制颜色
	public AnimationCurve lightIntensity;  // 强度变化

	private void Update()
	{
		// 自动推进时间
		if (autoProgressTime)
		{
			timeOfDay += Time.deltaTime / dayLengthInSeconds;
			timeOfDay %= 1f;   // 循环
		}

		UpdateLight();
	}

	private void UpdateLight()
	{
		if (sunLight == null) return;

		// 1. 根据时间插值方向
		sunLight.transform.rotation = Quaternion.Lerp(
			Quaternion.Euler(nightRotation),
			Quaternion.Euler(dayRotation),
			Mathf.Sin(timeOfDay * Mathf.PI)    // 正午在中间
		);

		// 2. 设置光颜色（使用 Gradient）
		sunLight.color = lightColor.Evaluate(timeOfDay);

		// 3. 设置光强（AnimationCurve）
		sunLight.intensity = lightIntensity.Evaluate(timeOfDay);
	}
}