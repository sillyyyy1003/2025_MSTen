using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ReligionColor : MonoBehaviour
{
    public Image Icon;

    // Start is called before the first frame update
    void Start()
    {
        // 订阅UI事件
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ReligionInfoSetOver += ReligionInfoSetOver;
        }

        Icon.color = GameUIManager.Instance.Backgroundcolor;


    }

    private void OnDestroy()
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ReligionInfoSetOver -= ReligionInfoSetOver;
        }


    }

    private void ReligionInfoSetOver()
    {
        Icon.color = GameUIManager.Instance.Backgroundcolor;
    }
}
