using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameData;


public class ButtonMenuManager : MonoBehaviour
{

    [Header("Button Menu Elements")]

    /// <summary>
    ///��ǰ��������趨
    /// </summary>
    public ButtonMenuData currentMenuData;

    /// <summary>
    /// ����������趨�������ⲿ��ʼ����
    /// </summary>
    public List<ButtonMenuData> allMenus;

    /// <summary>
    /// ������ť����
    /// </summary>
    public Button[] uiButtons;

    /// <summary>
    /// ÿ����ť������
    /// </summary>
    public TextMeshProUGUI[] uiButtonLabels;

    /// <summary>
    /// ÿ����ť��ͼƬ
    /// </summary>
    public Image[] uiButtonIcons;

    /// <summary>
    /// �������ı���
    /// </summary>
    public Image uiBackground;

    /// <summary>
    /// �ڲ�ʹ�õ�����趨��
    /// </summary>
    private Dictionary<string, ButtonMenuData> menuDict = new Dictionary<string, ButtonMenuData>();

    /// <summary>
    ///�ڸ����ѡ��Ķ�������
    ///0=Missionary����ʿ
    ///1=Soliderʿ��
    ///2=Farmerũ��
    ///3=Building����
	///4=Pope�̻�
	///5=None
    /// </summary>
    private CardType unitChoosed = CardType.None;

    // === Event �������� ===
    public event System.Action<CardType> OnCardTypeSelected;
    public event System.Action<CardType> OnCardPurchasedIntoDeck;
    public event System.Action<CardType> OnCardPurchasedIntoMap;
    public event System.Action<CardType, CardSkill> OnCardSkillUsed;
    public event System.Action<CardType, TechTree> OnTechUpdated;


    //����
    public static ButtonMenuManager Instance { get; private set; }


    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }


    // Start is called before the first frame update
    void Start()
    {
        menuDict.Clear();

        foreach (var entry in ButtonMenuFactory.GetAllMenuKeys())
        {
            string id = entry;
            ButtonMenuData data = ButtonMenuFactory.CreateButtonMenuData(id, Religion.None);

            if (data != null)
            {
                menuDict[id] = data;
            }
        }

        Debug.Log($"[ButtonMenuManager] ȫ��˥�`��������: {menuDict.Count}��");

        // === �����Ŀ¼ ===
        LoadMenu("ButtonMenu_Root");


        unitChoosed = 0;

    }


    // Update is called once per frame
    void Update()
    {


    }

    public void LoadMenu(string id)
    {
        UnitCardManager.Instance.SetDeckSelected(false);

        if (!menuDict.TryGetValue(id, out currentMenuData))
        {
            Debug.LogError($"Menu not found: {id}");
            return;
        }


        currentMenuData = menuDict[id];

        //Ŀ��Ŀ¼Ϊ��Ŀ¼
        if(currentMenuData.level==MenuLevel.Root)
        {
            EventSystem.current.SetSelectedGameObject(null);
            unitChoosed = CardType.None;
            UnitCardManager.Instance.SetTargetCardType(unitChoosed);
            UnitCardManager.Instance.EnableSingleMode(true);
        }
        else if (currentMenuData.level == MenuLevel.Second)//Ŀ��Ŀ¼Ϊ�ڶ�Ŀ¼
        {
            UnitCardManager.Instance.SetTargetCardType(unitChoosed);
            if (unitChoosed == CardType.Pope|| unitChoosed == CardType.None)
            {

                UnitCardManager.Instance.EnableSingleMode(true);

            }
            else
            {

                UnitCardManager.Instance.EnableSingleMode(false);
            }

        }


        Debug.Log($"[LoadMenu] currentMenuData = {(currentMenuData == null ? "NULL" : currentMenuData.menuId)}");

        uiBackground.sprite = currentMenuData.backgroundSprite;

        for (int i = 0; i < uiButtons.Length; i++)
        {

            var btnData = currentMenuData.buttons[i];
            var index = btnData.slotNo;

            var button = uiButtons[index];
            var label = uiButtonLabels[index];
            var icon = uiButtonIcons[index];
            ColorBlock cb = button.colors;
            Image backgroundimage = button.GetComponent<Image>();

            button.gameObject.SetActive(true);


            // --- �趨��ʾ ---
            backgroundimage.sprite = btnData.backgroundSprite;
            if (btnData.contentType == GameData.UI.ButtonContentType.Text)
            {
                label.text = btnData.labelText;
                icon.enabled = false;
            }
            else
            {
                icon.sprite = btnData.iconSprite;
                icon.enabled = true;
                label.text = string.Empty;
            }

            cb.normalColor = btnData.baseColor;
            cb.highlightedColor = btnData.hoverColor;
            cb.pressedColor = btnData.pressedColor;
            cb.selectedColor = btnData.selectedColor;
            cb.disabledColor = btnData.disabledColor;
            backgroundimage.color = btnData.backgroundColor;

            if (!btnData.isActive) {
                button.interactable = false;
                continue;
            }

            // --- �󶨵���¼� ---
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(index));
            
        }

    }


    public void OnButtonClicked(int index)
    {
        EventSystem.current.SetSelectedGameObject(null);
        Debug.Log($"[OnButtonClicked] Button {index} clicked");

        if (currentMenuData == null || index >= currentMenuData.buttons.Length)
            return;

        // === �ж����������� ===
        ButtonData btn = currentMenuData.GetButtonBySlot(index);
        switch (btn)
        {
            case NaviButtonData navi:
                MenuLevel currentMenuLevel = currentMenuData.level;
                CardType type = navi.cardType;

                if (currentMenuLevel == MenuLevel.Root)
                {
                    unitChoosed = type;
                    Debug.Log($"[Broadcast] CardTypeSelected: {type}");
                    OnCardTypeSelected?.Invoke(type);
                }

                string nextMenuId = ButtonMenuFactory.GetMenuId(navi.nextLevel, type);

                LoadMenu(nextMenuId);

                break;

            case CardSkillButtonData skill:
                UnitCardManager.Instance.SetDeckSelected(false);

                //UnitCardManager.Instance.GetChoosedCardId()
                Debug.Log($"[Broadcast] CardSkillUsed: {skill.cardType} - {skill.cardSkill}");
                OnCardSkillUsed?.Invoke(skill.cardType, skill.cardSkill);

                break;

            case ParamUpdateButtonData param:
                //UnitCardManager.Instance.GetChoosedCardId()
                Debug.Log($"[Broadcast] TechUpdated: {param.cardType} - {param.targetParameter}");
                OnTechUpdated?.Invoke(param.cardType, param.targetParameter);

                break;

            case PurchaseButtonData purchase:

                if (UnitCardManager.Instance.IsDeckSelected())
                {
                    UnitCardManager.Instance.AddCardCount(1);
                    Debug.Log($"[Broadcast] CardPurchasedIntoDeck: {purchase.cardType}");
                    OnCardPurchasedIntoDeck?.Invoke(purchase.cardType);
                }
                else
                {
                    Debug.Log($"[Broadcast] CardPurchasedIntoMap: {purchase.cardType}");
                    OnCardPurchasedIntoMap?.Invoke(purchase.cardType);

                }

                break;


        }



    

    }


}

