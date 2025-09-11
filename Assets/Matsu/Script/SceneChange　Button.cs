using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeButton: MonoBehaviour
{
    [SerializeField]
    [Header("���Ɉړ�����V�[��")]
    public SceneObject m_nextscene;

    public void LoadNextScene()
    {
        SceneManager.LoadScene(m_nextscene);
    }

}
