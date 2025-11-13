using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGMを管理するクラス
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// メンバ変数
	//--------------------------------------------------------------------------------

	private AudioSource bgmSource;
	private Coroutine fadeCoroutine;



	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------

	/// <summary> 再生中のBGMのAudioClipを取得する </summary>
	public AudioClip CurrentBGM => bgmSource.clip;

	public AudioSource AudioSource => bgmSource;

	/// <summary> 再生中かどうかを取得する </summary>
	public bool IsPlaying => bgmSource.isPlaying;



	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	private void Awake()
	{
		bgmSource = GetComponent<AudioSource>();
		bgmSource.loop = true; // デフォルトはループ再生
	}


	/// <summary>
	/// BGMを再生する
	/// </summary>
	/// <param name="clip">再生するBGMのAudioClip</param>
	/// <param name="volume">音量</param>
	/// <param name="loop">ループ再生するかどうか</param>
	/// <param name="fadeDuration">フェードインの時間</param>
	/// <param name="delay">遅延時間</param>
	/// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
	public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
	{
		// 再生中のBGMが同じ場合は何もしない
		if (bgmSource.clip == clip) return;

		// フェードイン中のコルーチンがあれば停止
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}

		// 値を0~1の範囲に制限
		volume = Mathf.Clamp01(volume);
		// 値の最小値を0に制限
		delay = Mathf.Max(0f, delay);
		fadeDuration = Mathf.Max(0f, fadeDuration);


		// フェードイン処理を開始
		fadeCoroutine = StartCoroutine(DelayedAction(delay, () =>
		{
			bgmSource.clip = clip;
			bgmSource.loop = loop;
			bgmSource.volume = 0f;
			bgmSource.Play();
			fadeCoroutine = StartCoroutine(FadeVolume(volume, fadeDuration, easing));
		}));
	}

	///// <summary>
	///// ランダムなBGMを再生する
	///// </summary>
	///// <param name="clips">再生するSEのAudioClipの配列</param>
	///// <param name="volume">音量</param>
	//public void PlayRandomBGM(AudioClip[] clips, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
	//{
	//    int randomIndex = Random.Range(0, clips.Length);    // ランダムなインデックスを取得
	//    PlayBGM(clips[randomIndex], volume, loop, fadeDuration, delay, easing);
	//}



	/// <summary>
	/// BGMを停止する
	/// </summary>
	/// <param name="fadeDuration">フェードアウトの時間</param>
	/// <param name="delay">遅延時間</param>
	/// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
	public void StopBGM(float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
	{
		// 再生中でない場合は何もしない
		if (!bgmSource.isPlaying) return;

		// フェードアウト中のコルーチンがあれば停止
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}

		// 値の最小値を0に制限
		fadeDuration = Mathf.Max(0f, fadeDuration);
		delay = Mathf.Max(0f, delay);

		// フェードアウト処理を開始
		fadeCoroutine = StartCoroutine(DelayedAction(delay, () =>
		{
			fadeCoroutine = StartCoroutine(FadeVolume(0f, fadeDuration, easing, () =>
			{
				bgmSource.Stop();
				bgmSource.clip = null;
			}));
		}));
	}


	/// <summary>
	/// 音量をフェードさせる
	/// </summary>
	/// <param name="targetVolume">目標音量</param>
	/// <param name="duration">フェード時間</param>
	/// <param name="easing">イージング関数</param>
	/// <param name="onComplete">フェード完了時のコールバック</param>
	private IEnumerator FadeVolume(float targetVolume, float duration, System.Func<float, float> easing = null, System.Action onComplete = null)
	{
		float startVolume = bgmSource.volume;
		float elapsedTime = 0f;

		// デフォルトのイージング関数（線形補間）
		if (easing == null) easing = t => t;

		while (elapsedTime < duration)
		{
			float t = elapsedTime / duration;
			bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, easing(t));
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		bgmSource.volume = targetVolume;
		onComplete?.Invoke();
	}

	/// <summary>
	/// 遅延処理を実行
	/// </summary>
	/// <param name="delay">遅延時間</param>
	/// <param name="action">遅延後に実行する処理</param>
	private IEnumerator DelayedAction(float delay, System.Action action)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		action?.Invoke();
	}


	//--------------------------------------------------------------------------------
	// アクセサ
	//--------------------------------------------------------------------------------

	/// <summary>
	/// BGMの音量を取得する
	/// </summary>
	public float GetVolume()
	{
		return bgmSource.volume;
	}

	/// <summary>
	/// BGMの音量を設定する
	/// </summary>
	/// <param name="volume">音量</param>
	public void SetVolume(float volume)
	{
		bgmSource.volume = Mathf.Clamp01(volume);
	}
}