using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEManager : MonoBehaviour
{
    // �����o�ϐ�
    //--------------------------------------------------------------------------------

    [SerializeField, Header("SE��AudioSource�̐�"), Tooltip("SE��AudioSource�̐����w�肵�āA�����ɍĐ��ł��鐔�𑝂₷���Ƃ��ł��܂��B")]
    private int sourceCount = 10;

    private AudioSource[] seSources;

    private int currentIndex = 0;



    //--------------------------------------------------------------------------------
    // �v���p�e�B
    //--------------------------------------------------------------------------------

    /// <summary> �Đ�����SE��AudioClip���擾���� </summary>
    public AudioClip CurrentSE => seSources[currentIndex].clip;

    /// <summary> �Đ������ǂ������擾���� </summary>
    public bool IsPlaying => seSources[currentIndex].isPlaying;



    //--------------------------------------------------------------------------------
    // ���\�b�h
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
    /// SE���Đ�����i2D/3D�؂�ւ��Ή��j
    /// </summary>
    /// <param name="clip">�Đ�����SE��AudioClip</param>
    /// <param name="volume">����</param>
    /// <param name="is3D">3D�Đ����邩</param>
    public void PlaySE(AudioClip clip, float volume = 1.0f, bool is3D = false)
    {
        var source = seSources[currentIndex];
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = is3D ? 1.0f : 0.0f; // 0=2D, 1=3D
        source.transform.localPosition = Vector3.zero; // 2D���͌��_
        source.Play();

        currentIndex = (currentIndex + 1) % sourceCount;
    }

    /// <summary>
    /// �w��ʒu��SE���Đ�����i3D�Đ���p�j
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
    /// �����_����SE���Đ�����
    /// </summary>
    /// <param name="clips">�Đ�����SE��AudioClip�̔z��</param>
    /// <param name="volume">����</param>
    public void PlayRandomSE(AudioClip[] clips, float volume = 1.0f)
    {
        int randomIndex = Random.Range(0, clips.Length);    // �����_���ȃC���f�b�N�X���擾
        PlaySE(clips[randomIndex], volume);                 // �����_����SE���Đ�
    }

    /// <summary>
    /// �w��ʒu�Ń����_����3D SE���Đ�����
    /// </summary>
    /// <param name="clips">�Đ�����SE��AudioClip�̔z��</param>
    /// <param name="position">�Đ��ʒu</param>
    /// <param name="volume">����</param>
    /// <param name="minDistance">�ŏ�����</param>
    /// <param name="maxDistance">�ő勗��</param>
    /// <param name="rolloffMode">�����J�[�u</param>
    /// <param name="dopplerLevel">�h�b�v���[����</param>
    /// <param name="spread">�L����</param>
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
    /// SE�̉��ʂ�ݒ肷��
    /// </summary>
    /// <param name="volume">����</param>
    public void SetVolume(float volume)
    {
        foreach (var source in seSources)
        {
            source.volume = Mathf.Clamp01(volume);
        }
    }
}
