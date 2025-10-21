using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using SoundSystem;

public class testSound : MonoBehaviour
{
    [SerializeField]
    public SoundManager soundManager;

    void Start()
    {
        soundManager.PlayBGM(SoundSystem.TYPE_BGM.TITLE,loop:true);
    }

}

