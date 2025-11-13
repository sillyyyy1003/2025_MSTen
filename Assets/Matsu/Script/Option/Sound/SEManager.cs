using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEManager : MonoBehaviour
{
	// メンバ変数
	//--------------------------------------------------------------------------------

	[SerializeField, Header("SEのAudioSourceの数"), Tooltip("SEのAudioSourceの数を指定して、同時に再生できる数を増やすことができます。")]
	private int sourceCount = 10;

	private AudioSource[] seSources;

	private int currentIndex = 0;



	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------

	/// <summary> 再生中のSEのAudioClipを取得する </summary>
	public AudioClip CurrentSE => seSources[currentIndex].clip;

	/// <summary> 再生中かどうかを取得する </summary>
	public bool IsPlaying => seSources[currentIndex].isPlaying;



	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	private void Awake()
	{
		seSources = new AudioSource[sourceCount];
		for (int i = 0; i < sourceCount; i++)
		{
			seSources[i] = gameObject.AddComponent<AudioSource>();
		}
	}


	/// <summary>
	/// SEを再生する（2D/3D切り替え対応）
	/// </summary>
	/// <param name="clip">再生するSEのAudioClip</param>
	/// <param name="volume">音量</param>
	/// <param name="is3D">3D再生するか</param>
	public void PlaySE(AudioClip clip, float volume = 1.0f, bool is3D = false)
	{
		var source = seSources[currentIndex];
		source.clip = clip;
		source.volume = volume;
		source.spatialBlend = is3D ? 1.0f : 0.0f; // 0=2D, 1=3D
		source.transform.localPosition = Vector3.zero; // 2D時は原点
		source.Play();

		currentIndex = (currentIndex + 1) % sourceCount;
	}

	/// <summary>
	/// 指定位置でSEを再生する（3D再生専用）
	/// </summary>
	/// <param name="clip">再生するSEのAudioClip</param>
	/// <param name="position">再生位置</param>
	/// <param name="volume">音量</param>
	/// <param name="minDistance">最小距離</param>
	/// <param name="maxDistance">最大距離</param>
	/// <param name="rolloffMode">減衰カーブ</param>
	/// <param name="dopplerLevel">ドップラー効果</param>
	/// <param name="spread">広がり</param>
	public void PlayPositionSE(
		AudioClip clip,
		Vector3 position,
		float volume = 1.0f,
		float minDistance = 1.0f,
		float maxDistance = 500.0f,
		AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
		float dopplerLevel = 0.0f,
		float spread = 0.0f)
	{
		var source = seSources[currentIndex];
		source.clip = clip;
		source.volume = volume;
		source.spatialBlend = 1.0f; // 3D
		source.transform.position = position;
		source.minDistance = minDistance;
		source.maxDistance = maxDistance;
		source.rolloffMode = rolloffMode;
		source.dopplerLevel = dopplerLevel;
		source.spread = spread;
		source.Play();

		currentIndex = (currentIndex + 1) % sourceCount;
	}

	/// <summary>
	/// ランダムなSEを再生する
	/// </summary>
	/// <param name="clips">再生するSEのAudioClipの配列</param>
	/// <param name="volume">音量</param>
	public void PlayRandomSE(AudioClip[] clips, float volume = 1.0f)
	{
		int randomIndex = Random.Range(0, clips.Length);    // ランダムなインデックスを取得
		PlaySE(clips[randomIndex], volume);                 // ランダムなSEを再生
	}

	/// <summary>
	/// 指定位置でランダムな3D SEを再生する
	/// </summary>
	/// <param name="clips">再生するSEのAudioClipの配列</param>
	/// <param name="position">再生位置</param>
	/// <param name="volume">音量</param>
	/// <param name="minDistance">最小距離</param>
	/// <param name="maxDistance">最大距離</param>
	/// <param name="rolloffMode">減衰カーブ</param>
	/// <param name="dopplerLevel">ドップラー効果</param>
	/// <param name="spread">広がり</param>
	public void PlayRandomPositionSE(
		AudioClip[] clips,
		Vector3 position,
		float volume = 1.0f,
		float minDistance = 1.0f,
		float maxDistance = 500.0f,
		AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
		float dopplerLevel = 0.0f,
		float spread = 0.0f)
	{
		int randomIndex = Random.Range(0, clips.Length);
		PlayPositionSE(
			clips[randomIndex],
			position,
			volume,
			minDistance,
			maxDistance,
			rolloffMode,
			dopplerLevel,
			spread);
	}

	/// <summary>
	/// SEの音量を設定する
	/// </summary>
	/// <param name="volume">音量</param>
	public void SetVolume(float volume)
	{
		foreach (var source in seSources)
		{
			source.volume = Mathf.Clamp01(volume);
		}
	}
}