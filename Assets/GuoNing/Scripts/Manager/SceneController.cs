using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene controller with scene switch effect
/// </summary>
public class SceneController : MonoBehaviour
{

	//======================================
	// メンバ変数
	//======================================
	public static SceneController Instance;
	[Header("Scene Management")]
	public float minLoadingTime = 2f; // 最小加载时间，避免转场太快

	private string currentSceneName;
	private string targetSceneName;
	private bool isLoading = false;

	/// <summary>
	/// scene loading event
	/// </summary>
	public static event Action<string> OnSceneLoadStarted;
	public static event Action<string> OnSceneLoadCompleted;
	public static event Action<float> OnLoadingProgress; // 加载进度

	//======================================
	// メソッド
	//======================================
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
			currentSceneName = SceneManager.GetActiveScene().name;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void Start()
	{
		if(SoundManager.Instance != null)
		{
			// 当场景加载开始时停止BGM
			OnSceneLoadStarted += (sceneName)=>
			{
				SoundManager.Instance.StopBGM();
			};
		}
	}

	public void SwitchScene(string sceneName, Action onComplete = null)
	{
		if (isLoading || sceneName == currentSceneName) return;

		targetSceneName = sceneName;
		isLoading = true;

		Debug.Log($"开始切换场景: {currentSceneName} -> {sceneName}");

		// 触发场景加载开始事件
		OnSceneLoadStarted?.Invoke(sceneName);

		// 开始转场流程
		StartCoroutine(SceneTransitionRoutine(sceneName, onComplete));

	}
	private IEnumerator SceneTransitionRoutine(string sceneName, Action onComplete)
	{
		float startTime = Time.time;

		// 第一步：淡出效果
		yield return FadeManager.Instance.FadeToBlack().WaitForCompletion();

		//Debug.Log($"场景加载开始: {sceneName}");

		// 第二步：异步加载场景
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
		asyncLoad.allowSceneActivation = false; // 先不激活场景

		float progress = 0f;
		while (!asyncLoad.isDone)
		{
			progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Unity的progress最大到0.9
			OnLoadingProgress?.Invoke(progress);

			if (asyncLoad.progress >= 0.9f)
			{
				// 确保最小加载时间
				float elapsedTime = Time.time - startTime;
				if (elapsedTime < minLoadingTime)
				{
					yield return new WaitForSeconds(minLoadingTime - elapsedTime);
				}

				asyncLoad.allowSceneActivation = true;
			}

			yield return null;
		}

		// 第三步：场景加载完成
		currentSceneName = sceneName;

		// 第四步：淡入效果
		yield return FadeManager.Instance.FadeFromBlack().WaitForCompletion();

		// 完成
		isLoading = false;
		OnSceneLoadCompleted?.Invoke(sceneName);
		onComplete?.Invoke();

		Debug.Log($"场景切换完成: {sceneName}");
	}

	/// <summary>
	///  重新加载当前场景
	/// </summary>
	/// <param name="onComplete"></param>
	public void ReloadCurrentScene(Action onComplete = null)
	{
		SwitchScene(currentSceneName, onComplete);
	}

	/// <summary>
	/// 获取当前场景名
	/// </summary>
	/// <returns></returns>
	public string GetCurrentScene()
	{
		return currentSceneName;
	}

	/// <summary>
	/// 检查是否正在加载
	/// </summary>
	/// <returns></returns>
	public bool IsLoading()
	{
		return isLoading;
	}

	public void SwitchToTitleScene()
	{
		SwitchScene("SelectScene", null);
	}

	public void SwitchToExtraContentScene()
	{
		SwitchScene("ExtraContents", null);
	}
}