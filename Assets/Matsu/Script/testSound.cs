using System.Collections;
using System.Collections.Generic;
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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
           // soundManager.PlaySE(SoundSystem.TYPE_SE.SELECT);
        }
    }

}

