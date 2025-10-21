using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM���Ǘ�����N���X
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    //--------------------------------------------------------------------------------
    // �����o�ϐ�
    //--------------------------------------------------------------------------------

    private AudioSource bgmSource;
    private Coroutine fadeCoroutine;



    //--------------------------------------------------------------------------------
    // �v���p�e�B
    //--------------------------------------------------------------------------------

    /// <summary> �Đ�����BGM��AudioClip���擾���� </summary>
    public AudioClip CurrentBGM => bgmSource.clip;

    public AudioSource AudioSource => bgmSource;

    /// <summary> �Đ������ǂ������擾���� </summary>
    public bool IsPlaying => bgmSource.isPlaying;



    //--------------------------------------------------------------------------------
    // ���\�b�h
    //--------------------------------------------------------------------------------

    private void Awake()
    {
        bgmSource = GetComponent<AudioSource>();
        bgmSource.loop = true; // �f�t�H���g�̓��[�v�Đ�
    }


    /// <summary>
    /// BGM���Đ�����
    /// </summary>
    /// <param name="clip">�Đ�����BGM��AudioClip</param>
    /// <param name="volume">����</param>
    /// <param name="loop">���[�v�Đ����邩�ǂ���</param>
    /// <param name="fadeDuration">�t�F�[�h�C���̎���</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    public void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        // �Đ�����BGM�������ꍇ�͉������Ȃ�
        if (bgmSource.clip == clip) return;

        // �t�F�[�h�C�����̃R���[�`��������Β�~
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // �l��0~1�͈̔͂ɐ���
        volume = Mathf.Clamp01(volume);
        // �l�̍ŏ��l��0�ɐ���
        delay = Mathf.Max(0f, delay);
        fadeDuration = Mathf.Max(0f, fadeDuration);


        // �t�F�[�h�C���������J�n
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
    ///// �����_����BGM���Đ�����
    ///// </summary>
    ///// <param name="clips">�Đ�����SE��AudioClip�̔z��</param>
    ///// <param name="volume">����</param>
    //public void PlayRandomBGM(AudioClip[] clips, float volume = 1.0f, bool loop = true, float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    //{
    //    int randomIndex = Random.Range(0, clips.Length);    // �����_���ȃC���f�b�N�X���擾
    //    PlayBGM(clips[randomIndex], volume, loop, fadeDuration, delay, easing);
    //}



    /// <summary>
    /// BGM���~����
    /// </summary>
    /// <param name="fadeDuration">�t�F�[�h�A�E�g�̎���</param>
    /// <param name="delay">�x������</param>
    /// <param name="easing">�C�[�W���O�֐�(EasingFunctions.cs���Q��)</param>
    public void StopBGM(float fadeDuration = 0.0f, float delay = 0.0f, System.Func<float, float> easing = null)
    {
        // �Đ����łȂ��ꍇ�͉������Ȃ�
        if (!bgmSource.isPlaying) return;

        // �t�F�[�h�A�E�g���̃R���[�`��������Β�~
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // �l�̍ŏ��l��0�ɐ���
        fadeDuration = Mathf.Max(0f, fadeDuration);
        delay = Mathf.Max(0f, delay);

        // �t�F�[�h�A�E�g�������J�n
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
    /// ���ʂ��t�F�[�h������
    /// </summary>
    /// <param name="targetVolume">�ڕW����</param>
    /// <param name="duration">�t�F�[�h����</param>
    /// <param name="easing">�C�[�W���O�֐�</param>
    /// <param name="onComplete">�t�F�[�h�������̃R�[���o�b�N</param>
    private IEnumerator FadeVolume(float targetVolume, float duration, System.Func<float, float> easing = null, System.Action onComplete = null)
    {
        float startVolume = bgmSource.volume;
        float elapsedTime = 0f;

        // �f�t�H���g�̃C�[�W���O�֐��i���`��ԁj
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
    /// �x�����������s
    /// </summary>
    /// <param name="delay">�x������</param>
    /// <param name="action">�x����Ɏ��s���鏈��</param>
    private IEnumerator DelayedAction(float delay, System.Action action)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        action?.Invoke();
    }


    //--------------------------------------------------------------------------------
    // �A�N�Z�T
    //--------------------------------------------------------------------------------

    /// <summary>
    /// BGM�̉��ʂ��擾����
    /// </summary>
    public float GetVolume()
    {
        return bgmSource.volume;
    }

    /// <summary>
    /// BGM�̉��ʂ�ݒ肷��
    /// </summary>
    /// <param name="volume">����</param>
    public void SetVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }
}

