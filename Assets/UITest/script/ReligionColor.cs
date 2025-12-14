using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReligionColor : MonoBehaviour
{
    public Image Icon;
    public TMP_Text Text;


    // Start is called before the first frame update
    void Start()
    {
        // 订阅UI事件
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ReligionInfoSetOver += ReligionInfoSetOver;
        }

        if(Icon) Icon.color = GameUIManager.Instance.MainColor;
        if(Text) Text.color = GameUIManager.Instance.MainColor;

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
        if (Icon) Icon.color = GameUIManager.Instance.MainColor;
        if (Text) Text.color = GameUIManager.Instance.MainColor;
    }
}
