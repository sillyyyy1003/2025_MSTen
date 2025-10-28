using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StoredCard : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;    // ���Ʊ���
    public Image unitCardImage;     // ��ɫ����ͼ
    public Image charaImage;     // ��ɫͼ
    public TextMeshProUGUI DataText;    // �ı�

    [Header("Sprite List")]
    public Sprite missionarySprite;//����ʿ
    public Sprite soliderSprite;//ʿ��
    public Sprite farmerSprite;//ũ��
    public Sprite buildingSprite;//����
    public Sprite popeSprite;//�̻�

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
