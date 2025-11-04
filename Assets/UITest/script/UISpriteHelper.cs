using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum UISpriteID
{
    Icon_Missionary,//传教士
    Icon_Solider,//士兵
    Icon_Farmer,//农民
    Icon_Building,//建筑
    Icon_Pope,//教皇
    Icon_Resource,//资源
    Icon_AllUnit,//总人数
    IconList_Religion,//宗教
    IconList_Player,//玩家头像
    Background_CardMenu,//按钮菜单
    Background_CardContainer,
    Background_CardUnused,
    Background_Circle,
    Background_Square,
    Background_Value,
    Background_ValueLong,

}

[System.Serializable]
public class UISpriteSlot
{
    [Tooltip("SpriteID")]
    public UISpriteID id;

    [Tooltip("SpriteResourse")]
    public Sprite singleSprite;
    public Sprite spriteSheet;

    [Tooltip("Note")]
    [TextArea]
    public string note;

    [Tooltip("SpriteSheetのSprite名リスト（自動取得）")]
    [HideInInspector]
    public string[] subSpriteNames;

    /// <summary>
    /// SpriteSheet内のサブスプライトを取得
    /// </summary>
    public Sprite GetSubSprite(string name)
    {
        if (spriteSheet == null)
        {
            Debug.LogWarning($"❌ SpriteSheet未設定（ID: {id}）");
            return null;
        }

        // Resources.LoadAll は SpriteAtlas ではなくスライス済み PNG から読み取る
        var sprites = Resources.LoadAll<Sprite>(spriteSheet.name);
        foreach (var s in sprites)
        {
            if (s.name == name)
                return s;
        }

        Debug.LogWarning($"❌ Sprite '{name}' not found in '{spriteSheet.name}'");
        return null;
    }

}



/// <summary>
/// UI共通Sprite管理クラス（唯一インスタンス）
/// </summary>
public class UISpriteHelper : MonoBehaviour
{
    [Header("=== Sprite登録リスト ===")]
    public List<UISpriteSlot> spriteSlots = new List<UISpriteSlot>();

    // ID→Sprite のマップ
    private Dictionary<UISpriteID, UISpriteSlot> spriteMap = new Dictionary<UISpriteID, UISpriteSlot>();

    private bool initialized = false;

    public static UISpriteHelper Instance { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    /// <summary>
    /// 登録リストから辞書を構築
    /// </summary>
    private void Initialize()
    {
        if (initialized) return;

        spriteMap.Clear();
        foreach (var slot in spriteSlots)
        {
            if (slot == null) continue;

            if (!spriteMap.ContainsKey(slot.id))
                spriteMap.Add(slot.id, slot);
            else
                Debug.LogWarning($"Duplicate Sprite ID detected: {slot.id}");
        }

        initialized = true;
    }


    /// <summary>
    /// ID から単一 Sprite を取得
    /// </summary>
    public Sprite GetSprite(UISpriteID id)
    {
        if (!initialized) Initialize();

        if (spriteMap.TryGetValue(id, out var slot))
        {
            if (slot.singleSprite != null)
                return slot.singleSprite;
            if (slot.spriteSheet != null)
                return slot.spriteSheet; // 図全体を返す場合
        }

        Debug.LogWarning($"Sprite ID '{id}' not found or empty.");
        return null;
    }

    /// <summary>
    /// SpriteSheet から特定のサブスプライトを取得
    /// </summary>
    public Sprite GetSubSprite(UISpriteID id, string subName)
    {
        if (!initialized) Initialize();

        if (spriteMap.TryGetValue(id, out var slot))
            return slot.GetSubSprite(subName);

        Debug.LogWarning($"SpriteSheet '{id}' not found.");
        return null;
    }

    /// <summary>
    /// Sprite が存在するか確認
    /// </summary>
    public bool HasSprite(UISpriteID id)
    {
        return spriteMap.ContainsKey(id);
    }

    public Sprite GetIconByCardType(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                return GetSprite(UISpriteID.Icon_Missionary);
            case CardType.Solider:
                return GetSprite(UISpriteID.Icon_Solider);
            case CardType.Farmer:
                return GetSprite(UISpriteID.Icon_Farmer);
            case CardType.Building:
                return GetSprite(UISpriteID.Icon_Building);
            case CardType.Pope:
                return GetSprite(UISpriteID.Icon_Pope);
            default:
                return null;
        }

    }
}




