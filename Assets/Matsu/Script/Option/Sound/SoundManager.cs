using SoundSystem;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    //--------------------------------------------------------------------------------
    // ��`
    //--------------------------------------------------------------------------------
    /// <summary> ���ʐݒ���Ǘ�����\���� </summary>
    [System.Serializable]
    public struct VolumeSetting
    {
        [Range(0f, 1f), Tooltip("�S�̂̉��ʂ�ݒ�")]
        public float masterVolume;

        [Range(0f, 1f), Tooltip("BGM�̉��ʂ�ݒ�")]
        public float bgmVolume;

        [Range(0f, 1f), Tooltip("SE�̉��ʂ�ݒ�")]
        public float seVolume;

        public VolumeSetting(float master, float bgm, float se)
        {
            masterVolume = master;
            bgmVolume = bgm;
            seVolume = se;

        }
    }
    // �ۑ��f�[�^�̃t�@�C����
    private const string fileName = "AudioSettings.json";


    //--------------------------------------------------------------------------------
    // �����o�ϐ�
    //--------------------------------------------------------------------------------

    //[SerializeField]
    private BGMManager bgmManager;
    //[SerializeField]
    private SEManager seManager;


    [SerializeField, Header("���ʐݒ�")]
    private VolumeSetting volumeSettings;


    [SerializeField, Header("BGM���\�[�X"), Tooltip(
        "BGM��AudioClip��񋓌^�ƕR�t���郊�X�g�ł��B" +
        "\n���X�g�ɓo�^���邱�ƂŁA�񋓌^���g�p����BGM���Đ��ł��܂��B" +
        "\n�o�^�ł��鉹���𑝂₷�ꍇ�́ASoundDefines.cs�ɂ���񋓌^��ǉ����Ă��������B")]
    private List<ResourceBGM> bgmResources;

    [SerializeField, Header("SE���\�[�X"), Tooltip(
        "SE��AudioClip��񋓌^�ƕR�t���郊�X�g�ł��B" +
        "\n���X�g�ɓo�^���邱�ƂŁA�񋓌^���g�p����SE���Đ��ł��܂��B" +
        "\n�o�^�ł��鉹���𑝂₷�ꍇ�́ASoundDefines.cs�ɂ���񋓌^��ǉ����Ă��������B")]
    private List<ResourceSE> seResources;

    private Dictionary<TYPE_BGM, AudioClip> bgmClips;
    private Dictionary<TYPE_SE, AudioClip> seClips;


    //--------------------------------------------------------------------------------
    // �v���p�e�B
    //--------------------------------------------------------------------------------

    /// <summary> BGMManager�̃C���X�^���X���擾 </summary>
    public BGMManager BGMManager => bgmManager;

    /// <summary> SEManager�̃C���X�^���X���擾 </summary>
    public SEManager SEManager => seManager;

    /// <summary> BGM��AudioClip���擾 </summary>
    public AudioClip CurrentBGM => bgmManager.CurrentBGM;

    /// <summary> SE��AudioClip���擾 </summary>
    public AudioClip CurrentSE => seManager.CurrentSE;

    /// <summary> BGM���Đ������ǂ������擾 </summary>
    public bool IsBGMPlaying => bgmManager.IsPlaying;

    /// <summary> SE���Đ������ǂ������擾 </summary>
    public bool IsSEPlaying => seManager.IsPlaying;


    /// <summary> ���ʐݒ���擾 </summary>
    public VolumeSetting VolumeSettings => volumeSettings;

    /// <summary> �S�̉��ʂ��擾 </summary>
    public float MasterVolume => volumeSettings.masterVolume;

    /// <summary> BGM���ʂ��擾 </summary>
    public float BGMVolume => volumeSettings.bgmVolume;

    /// <summary> SE���ʂ��擾 </summary>
    public float SEVolume => volumeSettings.seVolume;

    //--------------------------------------------------------------------------------
    // ���\�b�h
    //--------------------------------------------------------------------------------

    private void Awake()
    {

        // BGMManager��SEManager�̃C���X�^���X���擾
        if (BGMManager == null)
        {
            // BGMManager�����݂��Ȃ��ꍇ�͐V�����쐬
            var bgmObj = new GameObject("BGMManager");
            bgmManager = bgmObj.AddComponent<BGMManager>();
        }
        else
        {
            bgmManager = BGMManager;
        }

        if (SEManager == null)
        {
            // SEManager�����݂��Ȃ��ꍇ�͐V�����쐬
            var seObj = new GameObject("SEManager");
            seManager = seObj.AddComponent<SEManager>();
        }
        else
        {
            seManager = SEManager;
        }

        // ���\�[�X������������
        InitializeAudioDictionaries();
    }

    private void Start()
    {
        //SaveLoadManager saveLoadManager = SaveLoadManager.Instance;

        //if (saveLoadManager.Exists(fileName))
        //    saveLoadManager.Load(ref volumeSettings, fileName);
        //else
        //    volumeSettings = new VolumeSetting(1, 0.2f, 0.6f);

        // �������ʂ�K�p
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
    //    // �A�v���P�[�V�����I�����ɉ��ʐݒ��ۑ�
    //    SaveLoadManager.Instance.Save(volumeSettings, fileName);
    //}


    private void Update()
    {
        // ���ʐݒ��K�p
        ApplyVolumes();
    }


    /// <summary>
    /// BGM��SE�̃��\�[�X����������������
    /// </summary>
    private void InitializeAudioDictionaries()
    {
        bgmClips = new Dictionary<TYPE_BGM, AudioClip>();
        foreach (var resource in bgmResources)
        {
            if (!bgmClips.ContainsKey(resource.type))
                bgmClips.Add(resource.type, resource.clip);
            else
                Debug.LogError($"BGM�̎�� {resource.type} ���d�����Ă��܂��B");
        }

        seClips = new Dictionary<TYPE_SE, AudioClip>();
        foreach (var resource in seResources)
        {
            if (!seClips.ContainsKey(resource.type))
                seClips.Add(resource.type, resource.clip);
            else
                Debug.LogError($"SE�̎�� {resource.type} ���d�����Ă��܂��B");
        }
    }


    public void ResetData()
    {
        // ���ʐݒ��������
        volumeSettings = new VolumeSetting(1, 0.2f, 0.6f);
        //SaveLoadManager.Instance.Save(volumeSettings, fileName);
    }


    //--------------------------------------------------------------------------------
    // ���\�b�h�iBGM�֘A�j
    //--------------------------------------------------------------------------------
    #region BGM�֘A���\�b�h

    /// <summary>
    /// BGM���Đ�����iAudioClip�𒼐ڎw��j
    /// </summary>
    /// <param name="clip">�Đ�����BGM��AudioClip</param>
    /// <param name="volume">����</param>
    /// <param name="loop">���[�v�Đ����邩�ǂ���</param>
    /// <param name="fadeDuration">�t�F�[�h����</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        bgmManager.PlayBGM(clip, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    }

    /// <summary>
    /// BGM���Đ�����i�񋓌^���g�p�j
    /// </summary>
    /// <param name="type">�Đ�����BGM�̎��</param>
    /// <param name="volume">����</param>
    /// <param name="loop">���[�v�Đ����邩�ǂ���</param>
    /// <param name="fadeDuration">�t�F�[�h����</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    public void PlayBGM(TYPE_BGM type, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        if (bgmClips.TryGetValue(type, out var clip))
            bgmManager.PlayBGM(clip, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
        else
            Debug.LogWarning($"�w�肳�ꂽBGM��������܂���: {type}");
    }

    /// <summary>
    /// �z�񂩂烉���_����BGM���Đ�����iAudioClip�̔z��j
    /// </summary>
    /// <param name="clips">�Đ�����BGM��AudioClip�̔z��</param>
    /// <param name="volume">����</param>
    /// <param name="loop">���[�v�Đ����邩�ǂ���</param>
    /// <param name="fadeDuration">�t�F�[�h����</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    //public void RandomBGM(AudioClip[] clips, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    //{
    //    if (clips.Length == 0) return;

    //    bgmManager.PlayRandomBGM(clips, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    //}

    /// <summary>
    /// �z�񂩂烉���_����BGM���Đ�����i�񋓌^���g�p�j
    /// </summary>
    /// <param name="clips">�Đ�����BGM�̎�ނ̔z��</param>
    /// <param name="volume">����</param>
    /// <param name="loop">���[�v�Đ����邩�ǂ���</param>
    /// <param name="fadeDuration">�t�F�[�h����</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    //public void RandomBGM(TYPE_BGM[] types, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    //{
    //    if (types.Length == 0) return;
    //    var clips = new AudioClip[types.Length];
    //    for (int i = 0; i < types.Length; i++)
    //    {
    //        if (bgmClips.TryGetValue(types[i], out var clip))
    //            clips[i] = clip;
    //        else
    //            Debug.LogWarning($"�w�肳�ꂽBGM��������܂���: {types[i]}");
    //    }

    //    bgmManager.PlayRandomBGM(clips, volumeSettings.masterVolume * volumeSettings.bgmVolume * volume, loop, fadeDuration, delay, easing);
    //}

    /// <summary>
    /// BGM���~����
    /// </summary>
    [ContextMenu("BGM���~")]
    public void StopBGM()
    {
        bgmManager.StopBGM();
    }

    /// <summary>
    /// BGM���~����
    /// </summary>
    /// <param name="fadeDuration">�t�F�[�h�A�E�g�̎���</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�</param>
    public void StopBGM(float fadeDuration = 0.0f, float delay = 0f, System.Func<float, float> easing = null)
    {
        bgmManager.StopBGM(fadeDuration, delay, easing);
    }

    #endregion BGM�֘A���\�b�h


    //--------------------------------------------------------------------------------
    // ���\�b�h�iSE�֘A�j
    //--------------------------------------------------------------------------------
    #region SE�֘A���\�b�h

    /// <summary>
    /// SE���Đ�����iAudioClip�𒼐ڎw��j
    /// </summary>
    /// <param name="clip">�Đ�����SE��AudioClip</param>
    /// <param name="volume">����</param>
    public void PlaySE(AudioClip clip, float volume = 1.0f)
    {
        seManager.PlaySE(clip, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// SE���Đ�����i�񋓌^���g�p�j
    /// </summary>
    /// <param name="type">�Đ�����SE�̎��</param>
    /// <param name="volume">����</param>
    public void PlaySE(TYPE_SE type, float volume = 1.0f)
    {
        if (seClips.TryGetValue(type, out var clip))
            seManager.PlaySE(clip, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
        else
            Debug.LogWarning($"�w�肳�ꂽSE��������܂���: {type}");
    }

    /// <summary>
    /// SE���Đ�����iAudioClip�𒼐ڎw��A3D�ʒu�w��j
    /// </summary>
    /// <param name="clip">�Đ�����SE��AudioClip</param>
    /// <param name="position">�Đ��ʒu</param>
    /// <param name="volume">����</param>
    /// <param name="minDistance">�ŏ�����</param>
    /// <param name="maxDistance">�ő勗��</param>
    /// <param name="rolloffMode">�����J�[�u</param>
    /// <param name="dopplerLevel">�h�b�v���[����</param>
    /// <param name="spread">�L����</param>
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
    /// SE���Đ�����i�񋓌^���g�p�A3D�ʒu�w��j
    /// </summary>
    /// <param name="type">�Đ�����SE�̎��</param>
    /// <param name="position">�Đ��ʒu</param>
    /// <param name="volume">����</param>
    /// <param name="minDistance">�ŏ�����</param>
    /// <param name="maxDistance">�ő勗��</param>
    /// <param name="rolloffMode">�����J�[�u</param>
    /// <param name="dopplerLevel">�h�b�v���[����</param>
    /// <param name="spread">�L����</param>
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
            Debug.LogWarning($"�w�肳�ꂽSE��������܂���: {type}");
    }

    /// <summary>
    /// �z�񂩂烉���_����SE���Đ�����iAudioClip�̔z��j
    /// </summary>
    /// <param name="clips">�Đ�����SE��AudioClip�̔z��</param>
    /// <param name="volume">����</param>
    public void PlayRandomSE(AudioClip[] clips, float volume = 1.0f)
    {
        if (clips.Length == 0) return;

        seManager.PlayRandomSE(clips, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// �z�񂩂烉���_����SE���Đ�����i�񋓌^���g�p�j
    /// </summary>
    /// <param name="clips">�Đ�����SE�̎�ނ̔z��</param>
    /// <param name="volume">����</param>
    public void PlayRandomSE(TYPE_SE[] types, float volume = 1.0f)
    {
        if (types.Length == 0) return;

        var clips = new AudioClip[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            if (seClips.TryGetValue(types[i], out var clip))
                clips[i] = clip;
            else
                Debug.LogWarning($"�w�肳�ꂽSE��������܂���: {types[i]}");
        }

        seManager.PlayRandomSE(clips, volumeSettings.masterVolume * volumeSettings.seVolume * volume);
    }

    /// <summary>
    /// �z�񂩂烉���_����SE���Đ�����iAudioClip�̔z��A3D�ʒu�w��j
    /// </summary>
    /// <param name="clips">�Đ�����SE��AudioClip�̔z��</param>
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
                Debug.LogWarning($"�w�肳�ꂽSE��������܂���: {types[i]}");
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

    #endregion SE�֘A���\�b�h


    //--------------------------------------------------------------------------------
    // ���\�b�h�i���ʐݒ�֘A�j
    //--------------------------------------------------------------------------------
    #region ���ʐݒ�֘A���\�b�h

    [ContextMenu("���ʐݒ��K�p")]
    /// <summary>
    /// ���ʐݒ��K�p����
    /// </summary>
    public void ApplyVolumes()
    {
        bgmManager.SetVolume(volumeSettings.masterVolume * volumeSettings.bgmVolume);
        seManager.SetVolume(volumeSettings.masterVolume * volumeSettings.seVolume);
    }

    #endregion ���ʐݒ�֘A���\�b�h


    //--------------------------------------------------------------------------------
    // �A�N�Z�T
    //--------------------------------------------------------------------------------

    /// <summary>
    /// �e�퉹�ʂ̐ݒ�
    /// </summary>
    /// <param name="_volumeSetting">���ʐݒ�</param>
    public void SetVolumeSetting(in VolumeSetting _volumeSetting) 
    { volumeSettings = _volumeSetting;
    }

    /// <summary>
    /// �S�̉��ʂ�ݒ�
    /// </summary>
    /// <param name="_volume"></param>
    public void SetMasterVolume(float _volume) { volumeSettings.masterVolume = _volume; }

    /// <summary>
    ///	BGM���ʂ�ݒ�
    /// </summary>
    /// <param name="_volume"></param>
    public void SetBGMVolume(float _volume) { volumeSettings.bgmVolume = _volume; }

    /// <summary>
    /// SE���ʂ�ݒ�
    /// </summary>
    /// <param name="_volume"></param>
    public void SetSEVolume(float _volume) { volumeSettings.seVolume = _volume; }

}
