using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    public Image avatarImage;     // 角色图
    public Image backgroundImage;    // 卡牌背景
    public TextMeshProUGUI statusText;    // 文本

    public void Setup(UIPlayerData data)
    {

        avatarImage.sprite = UISpriteHelper.Instance.GetPlayerIconByID(data.avatarSpriteId);

        if (data.isAlive)
        {
            if (data.isOperating)
            {
                statusText.text = "行動中...";
            }
            else
            {
                statusText.text = "待ち";
            }
            backgroundImage.color = Color.white;
        }
        else
        {
            statusText.text = "敗北";
            backgroundImage.color = Color.red;
        }


    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
