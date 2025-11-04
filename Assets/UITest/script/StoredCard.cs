using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StoredCard : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;    // ¿¨ÅÆ±³¾°
    public Image unitCardImage;     // ½ÇÉ«±³¾°Í¼
    public Image charaImage;     // ½ÇÉ«Í¼
    public TextMeshProUGUI DataText;    // ÎÄ±¾



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
        cardType = type;
        charaImage.sprite = UISpriteHelper.Instance.GetIconByCardType(type);



    }

    public void ShowSprite()
    {

        showSprite = true;

    }

}
