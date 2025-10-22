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

            // --- ��ʾ���ƣ�ButtonMenuManager �����㣩---
            if (btnData.contentType == GameData.UI.ButtonContentType.Text)
            {
                label.text = btnData.labelText;     // ��̬��������
                icon.enabled = false;
            }
            else
            {
                icon.sprite = btnData.iconSprite;   // ��̬����ͼ��
                icon.enabled = true;
                label.text = string.Empty;
            }

            // --- �󶨵���¼� ---
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

