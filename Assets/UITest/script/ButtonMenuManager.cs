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
    ///当前的面板层的设定
    /// </summary>
    public ButtonMenuData currentMenuData;

    /// <summary>
    /// 所有面板层的设定集（从外部初始化）
    /// </summary>
    public List<ButtonMenuData> allMenus;

    /// <summary>
    /// 六个按钮本体
    /// </summary>
    public Button[] uiButtons;

    /// <summary>
    /// 每个按钮的文字
    /// </summary>
    public TextMeshProUGUI[] uiButtonLabels;

    /// <summary>
    /// 每个按钮的图片
    /// </summary>
    public Image[] uiButtonIcons;

    /// <summary>
    /// 整个面板的背景
    /// </summary>
    public Image uiBackground;

    /// <summary>
    /// 内部使用的面板设定集
    /// </summary>
    private Dictionary<string, ButtonMenuData> menuDict = new Dictionary<string, ButtonMenuData>();

    /// <summary>
    ///在根面板选择的对象类型
    ///0=Missionary传教士
    ///1=Solider士兵
    ///2=Farmer农民
    ///3=Building建筑
	///4=Pope教皇
	///5=None
    /// </summary>
    private CardType unitChoosed = CardType.None;

    // === Event 定义区域 ===
    public event System.Action<CardType> OnCardTypeSelected;
    public event System.Action<CardType> OnCardPurchasedIntoDeck;
    public event System.Action<CardType> OnCardPurchasedIntoMap;
    public event System.Action<CardType, CardSkill> OnCardSkillUsed;
    public event System.Action<CardType, TechTree> OnTechUpdated;


    //单例
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

        Debug.Log($"[ButtonMenuManager] 全メニュー生成完了: {menuDict.Count}件");

        // === 载入根目录 ===
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

        //目标目录为根目录
        if(currentMenuData.level==MenuLevel.Root)
        {
            EventSystem.current.SetSelectedGameObject(null);
            unitChoosed = CardType.None;
            UnitCardManager.Instance.SetTargetCardType(unitChoosed);
            UnitCardManager.Instance.EnableSingleMode(true);
        }
        else if (currentMenuData.level == MenuLevel.Second)//目标目录为第二目录
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


            // --- 设定显示 ---
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

            // --- 绑定点击事件 ---
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

        // === 判断是哪种子类 ===
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

                //UnitCardManager.Instance.GetChoosedUnitId();
                Debug.Log($"[Broadcast] CardSkillUsed: {skill.cardType} - {skill.cardSkill}");
                OnCardSkillUsed?.Invoke(skill.cardType, skill.cardSkill);

                //PlayerUnitDataInterface.Instance.UseCardSkill(skill.cardType, skill.cardSkill);

                break;

            case ParamUpdateButtonData param:
                //UnitCardManager.Instance.GetChoosedUnitId()
                Debug.Log($"[Broadcast] TechUpdated: {param.cardType} - {param.targetParameter}");
                OnTechUpdated?.Invoke(param.cardType, param.targetParameter);

                //PlayerUnitDataInterface.Instance.UpgradeCard(param.cardType, param.targetParameter);

                break;

            case PurchaseButtonData purchase:

                if (UnitCardManager.Instance.IsDeckSelected())
                {
                    UnitCardManager.Instance.AddCardCount(1);
                    Debug.Log($"[Broadcast] CardPurchasedIntoDeck: {purchase.cardType}");
                    OnCardPurchasedIntoDeck?.Invoke(purchase.cardType);

                    //PlayerUnitDataInterface.Instance.AddDeckNumByType(purchase.cardType);

                }
                else
                {
                    Debug.Log($"[Broadcast] CardPurchasedIntoMap: {purchase.cardType}");
                    OnCardPurchasedIntoMap?.Invoke(purchase.cardType);

                    //bool isCreateSuccess = PlayerUnitDataInterface.Instance.BuyUnitToMapByType(purchase.cardType);

                }

                break;


        }



    

    }


}

