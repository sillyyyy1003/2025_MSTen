using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField, Header("音量設定のスライダー")]
	private Slider masterSlider;
	[SerializeField, Header("BGM音量のスライダー")]
	private Slider bgmSlider;
	[SerializeField, Header("SE音量のスライダー")]
	private Slider seSlider;

    private SoundManager soundManager;


    private void Awake()
    {
        // シーン内からSoundManagerを探す
        soundManager = FindObjectOfType<SoundManager>();

        // なければ自動生成
        if (soundManager == null)
        {
            var obj = new GameObject("SoundManager");
            soundManager = obj.AddComponent<SoundManager>();
            DontDestroyOnLoad(obj); // シーンをまたぐ場合のみ
        }
    }
    private void Start()
	{
		// 音量設定のスライダーを初期化
		masterSlider.value = soundManager.MasterVolume;
		bgmSlider.value = soundManager.BGMVolume;
		seSlider.value = soundManager.SEVolume;
	}

	//public void OnVolumeUpdate()
	//{
	//	// 音量設定を更新
	//	//volumeSetting.masterVolume = masterSlider.value;
	//	//volumeSetting.bgmVolume = bgmSlider.value;
	//	//volumeSetting.seVolume = seSlider.value;

	//	SoundManager.VolumeSetting volumeSetting = new SoundManager.VolumeSetting
	//	{
	//		masterVolume = masterSlider.value,
	//		bgmVolume = bgmSlider.value,
	//		seVolume = seSlider.value
	//	};

	//	SoundManager soundManager = SoundManager.Instance;
	//	soundManager.SetVolumeSetting(in volumeSetting);

	//	soundManager = null;
	//}

	public void SetMasterVolume(float value)
	{
        if (soundManager != null)
            soundManager.SetMasterVolume(value);
    }

	public void SetBGMVolume(float value)
	{
        if (soundManager != null)
            soundManager.SetBGMVolume(value);
    }

	public void SetSeVolume(float value)
	{
        if (soundManager != null)
            soundManager.SetSEVolume(value);
    }

	public void ResetData()
	{
        if (soundManager != null)
        {
            soundManager.ResetData();

            // スライダーの値を反映
            masterSlider.value = soundManager.MasterVolume;
            bgmSlider.value = soundManager.BGMVolume;
            seSlider.value = soundManager.SEVolume;
        }
    }

	///// <summary>
	///// 音量設定を保存する
	///// </summary>
	//public void Save()
	//{
	//	SoundManager soundManager = SoundManager.Instance;

	//	//// 音量設定を更新
	//	//volumeSetting.masterVolume = masterSlider.value;
	//	//volumeSetting.bgmVolume = bgmSlider.value;
	//	//volumeSetting.seVolume = seSlider.value;

	//	SaveLoadManager.Instance.Save(soundManager.VolumeSettings, fileName);
	//}

	///// <summary>
	///// 音量設定を読み込む
	///// </summary>
	//public void Load()
	//{
	//	SoundManager.VolumeSetting volumeSetting = new SoundManager.VolumeSetting();

	//	SaveLoadManager.Instance.Load(ref volumeSetting, fileName);

	//	// 音量設定を更新
	//	SoundManager soundManager = SoundManager.Instance;
	//	soundManager.SetVolumeSetting(in volumeSetting);

	//	//// スライダーの値を更新
	//	//masterSlider.value = volumeSetting.masterVolume;
	//	//bgmSlider.value = volumeSetting.bgmVolume;
	//	//seSlider.value = volumeSetting.seVolume;
	//}
}
