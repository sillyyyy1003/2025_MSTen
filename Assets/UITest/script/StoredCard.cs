using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StoredCard : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;    // 卡牌背景
    public Image unitCardImage;     // 角色背景图
    public Image charaImage;     // 角色图
    public TextMeshProUGUI DataText;    // 文本

    [Header("Sprite List")]
    public Sprite missionarySprite;//传教士
    public Sprite soliderSprite;//士兵
    public Sprite farmerSprite;//农民
    public Sprite buildingSprite;//建筑
    public Sprite popeSprite;//教皇

    public bool showSprite = false;

    private CardType cardType = CardType.None;





    // Start is called before the first frame update
    void Start()
    {
        if(!showSprite)
        {
            charaImage.gameObject.SetActive(false);
            DataText.gameObject.SetActive(false);
        }


    }

    // Update is called once per frame
    void Update()
    {
        
        




    }

    public void SetSprite(CardType type)
    {
        SetCardType(type);

        switch (type)
        {
            case CardType.Missionary:
                charaImage.sprite = missionarySprite;

                break;
            case CardType.Solider:
                charaImage.sprite = soliderSprite;
                break;
            case CardType.Farmer:
                charaImage.sprite = farmerSprite;
                break;
            case CardType.Building:
                charaImage.sprite = buildingSprite;
                break;
            case CardType.Pope:
                charaImage.sprite = popeSprite;
                break;
            default:
                charaImage.sprite = popeSprite;
                break;

        }


    }
    public void SetCardType(CardType type)
    {

        cardType = type;

    }

    public void ShowSprite()
    {

        showSprite = true;

    }

}
