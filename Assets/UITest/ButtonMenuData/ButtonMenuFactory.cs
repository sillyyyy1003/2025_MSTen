using GameData;
using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ButtonMenuFactory
{
    private static readonly Dictionary<string, (MenuLevel level, CardType cardType)> MenuMap =
        new Dictionary<string, (MenuLevel, CardType)>()
        {
            { "ButtonMenu_Root",       (MenuLevel.Root,CardType.None) },
            { "ButtonMenu_Second_Missionary", (MenuLevel.Second, CardType.Missionary) },
            { "ButtonMenu_Second_Solider",   (MenuLevel.Second, CardType.Solider) },
            { "ButtonMenu_Second_Farmer",     (MenuLevel.Second, CardType.Farmer) },
            { "ButtonMenu_Second_Building",    (MenuLevel.Second, CardType.Building) },
            { "ButtonMenu_Second_Pope",       (MenuLevel.Second, CardType.Pope) },
            { "ButtonMenu_Third_Missionary", (MenuLevel.Third, CardType.Missionary) },
            { "ButtonMenu_Third_Solider",   (MenuLevel.Third, CardType.Solider) },
            { "ButtonMenu_Third_Farmer",     (MenuLevel.Third, CardType.Farmer) },
            { "ButtonMenu_Third_Building",    (MenuLevel.Third, CardType.Building) },
            { "ButtonMenu_Third_Pope",       (MenuLevel.Third, CardType.Pope) },

        };

    /// <summary>
    /// (MenuLevel, CardType) から menuId を取得
    /// </summary>
    public static string GetMenuId(MenuLevel level, CardType cardType)
    {
        foreach (var kvp in MenuMap)
        {
            var (lvl, type) = kvp.Value;
            if (lvl == level && type == cardType)
                return kvp.Key;
        }

        Debug.LogError($"ButtonMenuFactory: No menuId found for ({level}, {cardType})");
        return "ButtonMenu_Root";
    }

    /// <summary>提取MenuIdList</summary>
    public static IEnumerable<string> GetAllMenuKeys()
    {
        return MenuMap.Keys;
    }

    /// <summary>基于menuId + religion 生成 ButtonMenuData</summary>
    public static ButtonMenuData CreateButtonMenuData(string menuId, Religion religion)
    {
        if (!MenuMap.TryGetValue(menuId, out var entry))
        {
            Debug.LogError($"ButtonMenuFactory: menuId '{menuId}' not found.");
            return null;
        }

        MenuLevel level = entry.level;
        CardType cardType = entry.cardType;

        // === ScriptableObject の生成 ===
        var data = ScriptableObject.CreateInstance<ButtonMenuData>();
        data.menuId = menuId;
        data.level = level;
        data.cardType = cardType;
        data.religion = religion;
        data.backgroundSprite = UISpriteHelper.Instance.GetSprite(UISpriteID.Background_CardMenu);
        //↑可以根据religion需求改成SpriteSheet取用

        // === ボタン配列生成 ===
        data.buttons = GenerateButtons(level, cardType, religion);


        return data;
    }

    /// <summary>
    /// ======== 按规则生成按钮集 ========
    /// </summary>
    /// <param name="level"></param>
    /// <param name="cardType"></param>
    /// <param name="religion"></param>
    /// <returns></returns>
    private static ButtonData[] GenerateButtons(MenuLevel level, CardType cardType, Religion religion)
    {
        var list = new List<ButtonData>();

        switch (level)
        {
            case MenuLevel.Root:
                list.Add(new NaviButtonData(0, CardType.Missionary, MenuLevel.Second));
                list.Add(new NaviButtonData(1, CardType.Solider, MenuLevel.Second));
                list.Add(new NaviButtonData(2, CardType.Farmer, MenuLevel.Second));
                list.Add(new NaviButtonData(3, CardType.Building, MenuLevel.Second));
                list.Add(new NaviButtonData(4, CardType.Pope, MenuLevel.Second));
                list.Add(new NaviButtonData(5, CardType.None,MenuLevel.Second,false));
                break;

            case MenuLevel.Second:

                AddCardSkillButtons(list, cardType);

                break;

            case MenuLevel.Third:
                AddTechTreeButtons(list, cardType);
                break;
        }

        return list.ToArray();
    }

    /// <summary>
    /// ======== 单个技能按钮生成 ========
    /// </summary>
    /// <param name="list"></param>
    /// <param name="type"></param>
    /// <param name="religion"></param>
    private static void AddCardSkillButtons(List<ButtonData> list, CardType type, Religion religion = Religion.None)
    {
        
        switch (type)
        {
            case CardType.Missionary:
                list.Add(new CardSkillButtonData(0, type, CardSkill.Occupy));
                list.Add(new CardSkillButtonData(1, type, CardSkill.Conversion));
                list.Add(new NaviButtonData(2, type, MenuLevel.Third));
                list.Add(new PurchaseButtonData(3, type));
                list.Add(new CardSkillButtonData(4, type, CardSkill.None, false));
                list.Add(new NaviButtonData(5, CardType.None, MenuLevel.Root));
                break;
            case CardType.Solider:
                list.Add(new CardSkillButtonData(0, type, CardSkill.NormalAttack));
                list.Add(new CardSkillButtonData(1, type, CardSkill.SpecialAttack));
                list.Add(new NaviButtonData(2, type, MenuLevel.Third));
                list.Add(new PurchaseButtonData(3, type));
                list.Add(new CardSkillButtonData(4, type, CardSkill.None, false));
                list.Add(new NaviButtonData(5, CardType.None, MenuLevel.Root));
                break;
            case CardType.Farmer:
                list.Add(new CardSkillButtonData(0, type, CardSkill.EnterBuilding));
                list.Add(new CardSkillButtonData(1, type, CardSkill.Sacrifice));
                list.Add(new NaviButtonData(2, type, MenuLevel.Third));
                list.Add(new PurchaseButtonData(3, type));
                list.Add(new CardSkillButtonData(4, type, CardSkill.None,false));
                list.Add(new NaviButtonData(5, CardType.None, MenuLevel.Root));
                break;
            case CardType.Building:
                list.Add(new CardSkillButtonData(0, type, CardSkill.Construction));
                list.Add(new CardSkillButtonData(1, type, CardSkill.None, false));
                list.Add(new NaviButtonData(2, type, MenuLevel.Third));
                list.Add(new PurchaseButtonData(3, type));
                list.Add(new CardSkillButtonData(4, type, CardSkill.None, false));
                list.Add(new NaviButtonData(5, CardType.None, MenuLevel.Root));
                break;
            case CardType.Pope:
                list.Add(new CardSkillButtonData(0, type, CardSkill.SwapPosition));
                list.Add(new CardSkillButtonData(1, type, CardSkill.None, false));
                list.Add(new NaviButtonData(2, type, MenuLevel.Third));
                list.Add(new PurchaseButtonData(3, type,false));
                list.Add(new CardSkillButtonData(4, type, CardSkill.None, false));
                list.Add(new NaviButtonData(5, CardType.None, MenuLevel.Root));
                break;
        }
    }

    /// <summary>
    /// ======== 单个科技树按钮生成 ========
    /// </summary>
    /// <param name="list"></param>
    /// <param name="type"></param>
    /// <param name="religion"></param>
    private static void AddTechTreeButtons(List<ButtonData> list, CardType type, Religion religion = Religion.None)
    {
        list.Add(new NaviButtonData(5, type, MenuLevel.Second));

        switch (type)
        {
            case CardType.Missionary:
                list.Add(new ParamUpdateButtonData(0, type, TechTree.HP));
                list.Add(new ParamUpdateButtonData(1, type, TechTree.AP));
                list.Add(new ParamUpdateButtonData(2, type, TechTree.Occupy));
                list.Add(new ParamUpdateButtonData(3, type, TechTree.Conversion));
                list.Add(new ParamUpdateButtonData(4, type, TechTree.Heresy));
                break;
            case CardType.Solider:
                list.Add(new ParamUpdateButtonData(0, type, TechTree.HP));
                list.Add(new ParamUpdateButtonData(1, type, TechTree.AP));
                list.Add(new ParamUpdateButtonData(2, type, TechTree.ATK));
                list.Add(new ParamUpdateButtonData(3, type, TechTree.Heresy));
                list.Add(new ParamUpdateButtonData(4, type, TechTree.None,false));
                break;
            case CardType.Farmer:
                list.Add(new ParamUpdateButtonData(0, type, TechTree.HP));
                list.Add(new ParamUpdateButtonData(1, type, TechTree.AP));
                list.Add(new ParamUpdateButtonData(2, type, TechTree.Sacrifice));
                list.Add(new ParamUpdateButtonData(3, type, TechTree.None, false));
                list.Add(new ParamUpdateButtonData(4, type, TechTree.None, false)); 
                break;
            case CardType.Building:
                list.Add(new ParamUpdateButtonData(0, type, TechTree.HP));
                list.Add(new ParamUpdateButtonData(1, type, TechTree.AttackPosition));
                list.Add(new ParamUpdateButtonData(2, type, TechTree.AltarCount));
                list.Add(new ParamUpdateButtonData(3, type, TechTree.ConstructionCost));
                list.Add(new ParamUpdateButtonData(4, type, TechTree.None, false)); 
                break;
            case CardType.Pope:
                list.Add(new ParamUpdateButtonData(0, type, TechTree.HP));
                list.Add(new ParamUpdateButtonData(1, type, TechTree.MovementCD));
                list.Add(new ParamUpdateButtonData(2, type, TechTree.Buff));
                list.Add(new ParamUpdateButtonData(3, type, TechTree.ConstructionCost));
                list.Add(new ParamUpdateButtonData(4, type, TechTree.None, false)); 
                break;
        }
    }
}

