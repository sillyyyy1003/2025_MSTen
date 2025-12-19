using GameData;
using SoundSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class SoundManager : MonoBehaviour
{
    //--------------------------------------------------------------------------------
    // 定義
    //--------------------------------------------------------------------------------
    /// <summary> 音量設定を管理する構造体 </summary>
    [System.Serializable]
    public struct VolumeSetting
    {
        [Range(0f, 1f), Tooltip("全体の音量を設定")]
        public float masterVolume;

        [Range(0f, 1f), Tooltip("BGMの音量を設定")]
        public float bgmVolume;

        [Range(0f, 1f), Tooltip("SEの音量を設定")]
        public float seVolume;

        public VolumeSetting(float master, float bgm, float se)
        {
            masterVolume = master;
            bgmVolume = bgm;
            seVolume = se;

        }
    }
    // 保存データのファイル名
    private const string fileName = "AudioSettings.json";


    //--------------------------------------------------------------------------------
    // メンバ変数
    //--------------------------------------------------------------------------------

    //[SerializeField]
    private BGMManager bgmManager;
    //[SerializeField]
    private SEManager seManager;


    [SerializeField, Header("音量設定")]
    private VolumeSetting volumeSettings;


    [SerializeField, Header("BGMリソース"), Tooltip(
        "BGMのAudioClipを列挙型と紐付けるリストです。" +
        "\nリストに登録することで、列挙型を使用してBGMを再生できます。" +
        "\n登録できる音源を増やす場合は、SoundDefines.csにある列挙型を追加してください。")]
    private List<ResourceBGM> bgmResources;

    [SerializeField, Header("SEリソース"), Tooltip(
        "SEのAudioClipを列挙型と紐付けるリストです。" +
        "\nリストに登録することで、列挙型を使用してSEを再生できます。" +
        "\n登録できる音源を増やす場合は、SoundDefines.csにある列挙型を追加してください。")]
    private List<ResourceSE> seResources;

    private Dictionary<TYPE_BGM, AudioClip> bgmClips;
    private Dictionary<TYPE_SE, AudioClip> seClips;


    //--------------------------------------------------------------------------------
    // プロパティ
    //--------------------------------------------------------------------------------

    /// <summary> BGMManagerのインスタンスを取得 </summary>
    public BGMManager BGMManager => bgmManager;

    /// <summary> SEManagerのインスタンスを取得 </summary>
    public SEManager SEManager => seManager;

    /// <summary> BGMのAudioClipを取得 </summary>
    public AudioClip CurrentBGM => bgmManager.CurrentBGM;

    /// <summary> SEのAudioClipを取得 </summary>
    public AudioClip CurrentSE => seManager.CurrentSE;

    /// <summary> BGMが再生中かどうかを取得 </summary>
    public bool IsBGMPlaying => bgmManager.IsPlaying;

    /// <summary> SEが再生中かどうかを取得 </summary>
    public bool IsSEPlaying => seManager.IsPlaying;


    /// <summary> 音量設定を取得 </summary>
    public VolumeSetting VolumeSettings => volumeSettings;

    /// <summary> 全体音量を取得 </summary>
    public float MasterVolume => volumeSettings.masterVolume;

    /// <summary> BGM音量を取得 </summary>
    public float BGMVolume => volumeSettings.bgmVolume;

    /// <summary> SE音量を取得 </summary>
    public float SEVolume => volumeSettings.seVolume;

	/// <summary>
	/// シングルトンインスタンス
	/// </summary>
	public static SoundManager Instance { get; private set; }



	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	private void Awake()
    {
		//====2025.11.12 Kaku Singleton Pattern====
		if (Instance != null && Instance != this)
	    {
		    Destroy(gameObject); // 防止重复存在
		    return;
	    }

	    Instance = this;
	    DontDestroyOnLoad(gameObject); // シーン切り替え時に破棄されないようにする


		// BGMManagerとSEManagerのインスタンスを取得
		if (BGMManager == null)
        {
            // BGMManagerが存在しない場合は新しく作成
            var bgmObj = new GameObject("BGMManager");
            bgmManager = bgmObj.AddComponent<BGMManager>();
        }
        else
        {
            bgmManager = BGMManager;
        }

        if (SEManager == null)
        {
            // SEManagerが存在しない場合は新しく作成
            var seObj = new GameObject("SEManager");
            seManager = seObj.AddComponent<SEManager>();
        }
        else
        {
            seManager = SEManager;
        }

        bgmManager.transform.parent = this.transform;
        seManager.transform.parent = this.transform;

		// リソース辞書を初期化
		InitializeAudioDictionaries();
    }

    private void Start()
    {
        //SaveLoadManager saveLoadManager = SaveLoadManager.Instance;

        //if (saveLoadManager.Exists(fileName))
        //    saveLoadManager.Load(ref volumeSettings, fileName);
        //else
        //    volumeSettings = new VolumeSetting(1, 0.2f, 0.6f);

        // 初期音量を適用
        ApplyVolumes();
    }



   private void OnDestroy()
    {
        //if (fadeCoroutine != null)
        //{
        //    StopCoroutine(fadeCoroutine);
        //    fadeCoroutine = null;
        //}
    }

    //private void OnApplicationQuit()
    //{
    //    // アプリケーション終了時に音量設定を保存
    //    SaveLoadManager.Instance.Save(volumeSettings, fileName);
    //}



    /// <summary>
    /// BGMとSEのリソース辞書を初期化する
    /// </summary>
    private void InitializeAudioDictionaries()
    {
        bgmClips = new Dictionary<TYPE_BGM, AudioClip>();
        foreach (var resource in bgmResources)
        {
            if (!bgmClips.ContainsKey(resource.type))
                bgmClips.Add(resource.type, resource.clip);
            else
                Debug.LogError($"BGMの種類 {resource.type} が重複しています。");
        }

        seClips = new Dictionary<TYPE_SE, AudioClip>();
        foreach (var resource in seResources)
        {
            if (!seClips.ContainsKey(resource.type))
                seClips.Add(resource.type, resource.clip);
            else
                Debug.LogError($"SEの種類 {resource.type} が重複しています。");
        }
    }


    public void ResetData()
    {
        // 音量設定を初期化
        volumeSettings = new VolumeSetting(1, 0.2f, 0.6f);
        //SaveLoadManager.Instance.Save(volumeSettings, fileName);
    }


    //--------------------------------------------------------------------------------
    // メソッド（BGM関連）
    //--------------------------------------------------------------------------------
    #region BGM関連メソッド

    /// <summary>
    /// BGMを再生する（AudioClipを直接指定）
    /// </summary>
    /// <param name="clip">再生するBGMのAudioClip</param>
    /// <param name="volume">音量</param>
    /// <param name="loop">ループ再生するかどうか</param>
    /// <param name="fadeDuration">フェード時間</param>
    /// <param name="delay">遅延時間</param>
    /// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
    public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        bgmManager.PlayBGM(clip, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    }

    /// <summary>
    /// BGMを再生する（列挙型を使用）
    /// </summary>
    /// <param name="type">再生するBGMの種類</param>
    /// <param name="volume">音量</param>
    /// <param name="loop">ループ再生するかどうか</param>
    /// <param name="fadeDuration">フェード時間</param>
    /// <param name="delay">遅延時間</param>
    /// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
    public void PlayBGM(TYPE_BGM type, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        if (bgmClips.TryGetValue(type, out var clip))
            bgmManager.PlayBGM(clip, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
        else
            Debug.LogWarning($"指定されたBGMが見つかりません: {type}");
    }

    /// <summary>
    /// 配列からランダムにBGMを再生する（AudioClipの配列）
    /// </summary>
    /// <param name="clips">再生するBGMのAudioClipの配列</param>
    /// <param name="volume">音量</param>
    /// <param name="loop">ループ再生するかどうか</param>
    /// <param name="fadeDuration">フェード時間</param>
    /// <param name="delay">遅延時間</param>
    /// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
    //public void RandomBGM(AudioClip[] clips, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    //{
    //    if (clips.Length == 0) return;

    //    bgmManager.PlayRandomBGM(clips, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    //}

    /// <summary>
    /// 配列からランダムにBGMを再生する（列挙型を使用）
    /// </summary>
    /// <param name="clips">再生するBGMの種類の配列</param>
    /// <param name="volume">音量</param>
    /// <param name="loop">ループ再生するかどうか</param>
    /// <param name="fadeDuration">フェード時間</param>
    /// <param name="delay">遅延時間</param>
    /// <param name="easing">イージング関数(EasingFunctions.csを参照)</param>
    //public void RandomBGM(TYPE_BGM[] types, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    //{
    //    if (types.Length == 0) return;
    //    var clips = new AudioClip[types.Length];
    //    for (int i = 0; i < types.Length; i++)
    //    {
    //        if (bgmClips.TryGetValue(types[i], out var clip))
    //            clips[i] = clip;
    //        else
    //            Debug.LogWarning($"指定されたBGMが見つかりません: {types[i]}");
    //    }

    //    bgmManager.PlayRandomBGM(clips, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    //}

    /// <summary>
    /// BGMを停止する
    /// </summary>
    [ContextMenu("BGMを停止")]
    public void StopBGM()
    {
        bgmManager.StopBGM();
    }

    /// <summary>
    /// BGMを停止する
    /// </summary>
    /// <param name="fadeDuration">フェードアウトの時間</param>
    /// <param name="delay">遅延時間</param>
    /// <param name="easing">イージング関数</param>
    public void StopBGM(float fadeDuration = 0.0f, float delay = 0f, System.Func<float, float> easing = null)
    {
        bgmManager.StopBGM(fadeDuration, delay, easing);
    }

    public void PlayBGMLoop(TYPE_BGM type)
    {
		if (bgmClips.TryGetValue(type, out var clip))
			bgmManager.PlayBGM(clip, volumeSettings.masterVolume * volumeSettings.bgmVolume, true, 5f,5f, null);
	}

    #endregion BGM関連メソッド


    //--------------------------------------------------------------------------------
    // メソッド（SE関連）
    //--------------------------------------------------------------------------------
    #region SE関連メソッド

    /// <summary>
    /// SEを再生する（AudioClipを直接指定）
    /// </summary>
    /// <param name="clip">再生するSEのAudioClip</param>
    /// <param name="volume">音量</param>
    public void PlaySE(AudioClip clip, float volume = 1.0f)
    {
        seManager.PlaySE(clip, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// SEを再生する（列挙型を使用）
    /// </summary>
    /// <param name="type">再生するSEの種類</param>
    /// <param name="volume">音量</param>
    public void PlaySE(TYPE_SE type, float volume = 1.0f)
    {
        if (seClips.TryGetValue(type, out var clip))
            seManager.PlaySE(clip, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
        else
            Debug.LogWarning($"指定されたSEが見つかりません: {type}");
    }

    /// <summary>
    /// SEを再生する（AudioClipを直接指定、3D位置指定）
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
        seManager.PlayPositionSE(clip, position, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// SEを再生する（列挙型を使用、3D位置指定）
    /// </summary>
    /// <param name="type">再生するSEの種類</param>
    /// <param name="position">再生位置</param>
    /// <param name="volume">音量</param>
    /// <param name="minDistance">最小距離</param>
    /// <param name="maxDistance">最大距離</param>
    /// <param name="rolloffMode">減衰カーブ</param>
    /// <param name="dopplerLevel">ドップラー効果</param>
    /// <param name="spread">広がり</param>
    public void PlayPositionSE(
        TYPE_SE type,
        Vector3 position,
        float volume = 1.0f,
        float minDistance = 1.0f,
        float maxDistance = 500.0f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
        float dopplerLevel = 0.0f,
        float spread = 0.0f)
    {
        if (seClips.TryGetValue(type, out var clip))
            seManager.PlayPositionSE(
                clip,
                position,
                volumeSettings.masterVolume * volumeSettings.seVolume * volume,
                minDistance,
                maxDistance,
                rolloffMode,
                dopplerLevel,
                spread);
        else
            Debug.LogWarning($"指定されたSEが見つかりません: {type}");
    }

    /// <summary>
    /// 配列からランダムにSEを再生する（AudioClipの配列）
    /// </summary>
    /// <param name="clips">再生するSEのAudioClipの配列</param>
    /// <param name="volume">音量</param>
    public void PlayRandomSE(AudioClip[] clips, float volume = 1.0f)
    {
        if (clips.Length == 0) return;

        seManager.PlayRandomSE(clips, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// 配列からランダムにSEを再生する（列挙型を使用）
    /// </summary>
    /// <param name="clips">再生するSEの種類の配列</param>
    /// <param name="volume">音量</param>
    public void PlayRandomSE(TYPE_SE[] types, float volume = 1.0f)
    {
        if (types.Length == 0) return;

        var clips = new AudioClip[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            if (seClips.TryGetValue(types[i], out var clip))
                clips[i] = clip;
            else
                Debug.LogWarning($"指定されたSEが見つかりません: {types[i]}");
        }

        seManager.PlayRandomSE(clips, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// 配列からランダムにSEを再生する（AudioClipの配列、3D位置指定）
    /// </summary>
    /// <param name="clips">再生するSEのAudioClipの配列</param>
    public void PlayRandomPositionSE(
        AudioClip[] clips,
        float volume = 1.0f,
        float minDistance = 1.0f,
        float maxDistance = 500.0f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
        float dopplerLevel = 0.0f,
        float spread = 0.0f)
    {
        if (clips.Length == 0) return;

        seManager.PlayRandomPositionSE(
            clips,
            Vector3.zero,
            volumeSettings.masterVolume * volumeSettings.seVolume * volume,
            minDistance,
            maxDistance,
            rolloffMode,
            dopplerLevel,
            spread);
    }

    public void PlayRandomPositionSE(
        TYPE_SE[] types,
        Vector3 position,
        float volume = 1.0f,
        float minDistance = 1.0f,
        float maxDistance = 500.0f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
        float dopplerLevel = 0.0f,
        float spread = 0.0f)
    {
        if (types.Length == 0) return;

        var clips = new AudioClip[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            if (seClips.TryGetValue(types[i], out var clip))
                clips[i] = clip;
            else
                Debug.LogWarning($"指定されたSEが見つかりません: {types[i]}");
        }

        seManager.PlayRandomPositionSE(
            clips,
            position,
            volumeSettings.masterVolume * volumeSettings.seVolume * volume,
            minDistance,
            maxDistance,
            rolloffMode,
            dopplerLevel,
            spread);
    }

    #endregion SE関連メソッド


    //--------------------------------------------------------------------------------
    // メソッド（音量設定関連）
    //--------------------------------------------------------------------------------
    #region 音量設定関連メソッド

    [ContextMenu("音量設定を適用")]
    /// <summary>
    /// 音量設定を適用する
    /// </summary>
    public void ApplyVolumes()
    {
        bgmManager.SetVolume(volumeSettings.masterVolume * volumeSettings.bgmVolume);
        seManager.SetVolume(volumeSettings.masterVolume * volumeSettings.seVolume);

        // 将更改保存在SaveLoadManager
        if(SaveLoadManager.Instance!=null)
	        SaveLoadManager.Instance.UpdateVolume(MasterVolume,BGMVolume,SEVolume);
    }

    #endregion 音量設定関連メソッド


    //--------------------------------------------------------------------------------
    // アクセサ
    //--------------------------------------------------------------------------------

    /// <summary>
    /// 各種音量の設定
    /// </summary>
    /// <param name="_volumeSetting">音量設定</param>
    public void SetVolumeSetting(in VolumeSetting _volumeSetting) 
    { volumeSettings = _volumeSetting;
    }

    /// <summary>
    /// 全体音量を設定
    /// </summary>
    /// <param name="_volume"></param>
    public void SetMasterVolume(float _volume) { volumeSettings.masterVolume = _volume; }

    /// <summary>
    ///	BGM音量を設定
    /// </summary>
    /// <param name="_volume"></param>
    public void SetBGMVolume(float _volume) { volumeSettings.bgmVolume = _volume; }

    /// <summary>
    /// SE音量を設定
    /// </summary>
    /// <param name="_volume"></param>
    public void SetSEVolume(float _volume) { volumeSettings.seVolume = _volume; }


	/// <summary>
	/// 整数设定BGM音量
	/// </summary>
	/// <param name="_volume"></param>

	public void SetIntBGMVolume(int _volume)
    {
	    float volume = _volume / 10f;
        SetBGMVolume(volume);
    }


	//--------------------------------------------------------------------------------
	// 指定サウンド再生
	//--------------------------------------------------------------------------------
    public void PlayButtonClickedSound()
    {
        PlaySE(TYPE_SE.BUTTONCLICKED);
	}

    public void PlayGameMusic()
    {

        // 根据不同宗教选择不同的音乐
		int localPlayerId = GameManage.Instance.LocalPlayerID;
		Religion playerReligion = PlayerDataManager.Instance.GetPlayerData(localPlayerId).PlayerReligion;
		switch (playerReligion)
        {
            case Religion.RedMoonReligion: PlayBGM(SoundSystem.TYPE_BGM.REDMOON_THEME); break;
            case Religion.SilkReligion: PlayBGM(SoundSystem.TYPE_BGM.SILK_THEME); break;
            case Religion.MadScientistReligion:PlayBGM(SoundSystem.TYPE_BGM.MAD_SCIENTIST_THEME); break;
            case Religion.MayaReligion: PlayBGM(SoundSystem.TYPE_BGM.MAYA_THEME); break;
			//...等待追加
			default: PlayBGM(SoundSystem.TYPE_BGM.TITLE);break;
		}
		
		
	}

	public void ApplyLoadedVolume(float master, float bgm, float se)
	{
		SetBGMVolume(bgm);
        SetSEVolume(se);
        SetMasterVolume(master);
	}
}
