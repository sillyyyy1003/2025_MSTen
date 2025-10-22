using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ButtonMenuManager : MonoBehaviour
{

    [Header("References")]

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
        foreach (var menu in allMenus)
            menuDict[menu.menuId] = menu;

        LoadMenu("ButtonMenu_Root");

        unitChoosed = 0;
    }


    // Update is called once per frame
    void Update()
    {


    }

    public void LoadMenu(string id)
    {
        if (!menuDict.TryGetValue(id, out currentMenuData))
        {
            Debug.LogError($"Menu not found: {id}");
            return;
        }


        if (!menuDict.TryGetValue(id, out currentMenuData))
        {
            Debug.LogError($"Menu not found: {id}");
            return;
        }


        currentMenuData = menuDict[id];

        if(id == "ButtonMenu_Root"&& unitChoosed!=CardType.None)
        {
            EventSystem.current.SetSelectedGameObject(null);
            unitChoosed = CardType.None;
            UnitCardManager.Instance.SetTargetCardType(unitChoosed);

        }

        //if(id == "ButtonMenu_Root"&& unitChoosed!=CardType.None)
        //{
        //    EventSystem.current.SetSelectedGameObject(uiButtons[(int)unitChoosed].gameObject);
        //}
        //else
        //{
        //
        //    EventSystem.current.SetSelectedGameObject(null);
        //}





        Debug.Log($"[LoadMenu] currentMenuData = {(currentMenuData == null ? "NULL" : currentMenuData.menuId)}");

        uiBackground.sprite = currentMenuData.backgroundSprite;

        for (int i = 0; i < uiButtons.Length; i++)
        {
            var btnData = currentMenuData.buttons[i];
            var button = uiButtons[i];
            var label = uiButtonLabels[i];
            var icon = uiButtonIcons[i];


            button.gameObject.SetActive(btnData.isActive);

            if (!btnData.isActive) continue;

            // --- 显示控制（ButtonMenuManager 的灵活点）---
            if (btnData.contentType == GameData.UI.ButtonContentType.Text)
            {
                label.text = btnData.labelText;     // 动态设置文字
                icon.enabled = false;
            }
            else
            {
                icon.sprite = btnData.iconSprite;   // 动态设置图标
                icon.enabled = true;
                label.text = string.Empty;
            }

            // --- 绑定点击事件 ---
            int index = i;
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

        string currentMenuId = currentMenuData.menuId;

        var data = currentMenuData.buttons[index];
        Debug.Log($"[OnButtonClicked] triggerEvent = {data.triggerEvent}, " +
            $"nextMenuId = {data.nextMenuId}");

        

        switch (data.triggerEvent)
        {
            case MenuEventType.NextMenu:
                if (string.IsNullOrEmpty(data.nextMenuId))
                    break;
                if (currentMenuId == "ButtonMenu_Root")
                {
                    unitChoosed = (CardType)index;
                    UnitCardManager.Instance.SetTargetCardType(unitChoosed);
                    Debug.Log("CardTypeChanged");


                }
                UnitCardManager.Instance.SetDeckSelected(false);
                LoadMenu(data.nextMenuId);


                break;
            case MenuEventType.Purchase:


                UnitCardManager.Instance.AddCardCount(1);
                break;
            case MenuEventType.UseCardSkill:




                UnitCardManager.Instance.SetDeckSelected(false);
                break;
            case MenuEventType.UpdateCardParameter:




                break;
            default:
                break;



        }

    }


}

