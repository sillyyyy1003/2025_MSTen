using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GameData.UI
{
    /// <summary>
    /// ボタン表示のタイプ
    /// </summary>
    public enum ButtonContentType
    {
        Text,
        Image
    }

    /// <summary>
    /// 起こすイベントのタイプ
    /// </summary>
    public enum MenuEventType
    {
        None,               // 何も起きない
        NextMenu,           // 次のメニューへ遷移
        Purchase,           // 購入処理
        UseCardSkill,       // カードスキルを使用
        UpdateCardParameter // カードパラメータ更新

    }

    /// <summary>
    /// メニューの階層
    /// </summary>
    public enum MenuLevel
    {
        Root,
        Second,
        Third
    }

    /// <summary>
    /// カードスキル
    /// </summary>
    public enum CardSkill
    {
        None,
        Occupy,//占領 Missionary
        Conversion,//魅惑 Missionary
        NormalAttack,//一般攻撃 Military
        SpecialAttack,//特殊攻撃 Military
        EnterBuilding,//建物に入る Farmer Building
        Construction,//建物を建築 Building
        Sacrifice,//献祭 AP消費し他駒を回復するスキル Farmer
        SwapPosition,//味方駒と位置を交換する Pope

    }

    /// <summary>
    /// 科技树
    /// </summary>
    public enum TechTree
    {
        None,
        HP,//体力
        AP,//行動力
        Occupy,//占領
        Conversion,//魅惑
        ATK,//攻撃力
        Sacrifice,//献祭
        AttackPosition,//攻撃口
        AltarCount,//祭壇数
        ConstructionCost,//建設費用
        MovementCD,//移動クール
        Buff,//強化効果 / 弱体効果
        Heresy,//異端邪説
    }

    /// <summary>
    /// ボタンメニューの階層データ定義
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonMenuData", menuName = "GameData/UI/ButtonMenuData")]
    public class ButtonMenuData : ScriptableObject
    {
        [Header("基本情報")]
        public string menuId;
        public MenuLevel level;
        public Religion religion;
        public CardType cardType;

        [Header("ボタン設定")]
        public ButtonData[] buttons = new ButtonData[6];

        [Header("表示設定")]
        public Sprite backgroundSprite;

        /// <summary>
        /// 指定されたスロット番号に対応する ButtonData を取得する
        /// </summary>
        public ButtonData GetButtonBySlot(int slotNo)
        {
            if (buttons == null || buttons.Length == 0)
            {
                Debug.LogWarning($"[ButtonMenuData] {menuId} にボタンが定義されていません。");
                return null;
            }

            foreach (var btn in buttons)
            {
                if (btn != null && btn.slotNo == slotNo)
                    return btn;
            }

            Debug.LogWarning($"[ButtonMenuData] スロット {slotNo} のボタンが見つかりません。");
            return null;
        }

    }

    /// <summary>
    /// 単一ボタンのデータ構造
    /// 基底クラス
    /// </summary>
    [System.Serializable]
    public abstract class ButtonData
    {
        [Header("基本情報")]
        public int slotNo = 0;
        public bool isActive = false;
        public MenuEventType triggerEvent;
        public CardType cardType;

        [Header("表示設定")]
        public ButtonContentType contentType;
        public string labelText;
        public Sprite iconSprite;
        public Sprite backgroundSprite;

        [Header("ボタンカラー設定")]
        [Tooltip("基本色（通常時）")]
        public Color baseColor = new Color(0.90f, 0.90f, 0.90f, 1f);
        [Tooltip("ホバー時の色")]
        public Color hoverColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("クリック時の色")]
        public Color pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [Tooltip("選択時の色")]
        public Color selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [Tooltip("ロック時（使用不可）の色")]
        public Color disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
        [Tooltip("背景色")]
        public Color backgroundColor = new Color(1f, 1f, 1f, 1f);


        [Header("音声設定")]
        public AudioClip onClickSound;

        public MenuEventType GetMenuEventType()
        {
            return triggerEvent;
        }



    }

    /// <summary>
    /// 画面移動用ボタン
    /// </summary>
    [System.Serializable]
    public class NaviButtonData : ButtonData
    {
        [Header("画面遷移設定")]
        public MenuLevel nextLevel;

        public NaviButtonData(int slot ,CardType type, MenuLevel level, bool active = true)
        {
            slotNo = slot;
            cardType = type;
            isActive = active;

            nextLevel = level;

            backgroundSprite = UISpriteHelper.Instance.GetSprite(UISpriteID.Background_Square);

            if (isActive)
            {
                triggerEvent = MenuEventType.NextMenu;

                switch (nextLevel)
                {
                    case MenuLevel.Root:
                        contentType = ButtonContentType.Text;
                        labelText = "戻る";
                        backgroundColor = new Color(0.90f, 0.25f, 0.25f, 1f);
                        cardType = CardType.None;
                        break;
                    case MenuLevel.Second:
                        if (slot == 5)
                        {
                            contentType = ButtonContentType.Text;
                            labelText = "戻る";
                            backgroundColor = new Color(0.90f, 0.25f, 0.25f, 1f);
                        }
                        else
                        {
                            contentType = ButtonContentType.Image;
                            iconSprite = UISpriteHelper.Instance.GetIconByCardType(type);
                        }


                        break;
                    case MenuLevel.Third:
                        contentType = ButtonContentType.Text;
                        labelText = "技術樹";
                        backgroundColor = new Color(0.45f, 0.60f, 0.90f, 1f);
                        break;
                    default:
                        break;

                }

            }
            else
            {
                triggerEvent = MenuEventType.None;

                contentType = ButtonContentType.Text;
                backgroundColor = new Color(0.90f, 0.25f, 0.25f, 1f);
            }



        }


    }

    /// <summary>
    /// カード機能用ボタン
    /// </summary>
    [System.Serializable]
    public class CardSkillButtonData : ButtonData
    {
        [Header("カードスキル設定")]
        public CardSkill cardSkill;

        public CardSkillButtonData(int slot, CardType type, CardSkill skill, bool active=true)
        {
            slotNo = slot;
            cardType = type;
            isActive = active;

            cardSkill = skill;

            backgroundSprite = UISpriteHelper.Instance.GetSprite(UISpriteID.Background_Square);

            if (isActive)
            {
                triggerEvent = MenuEventType.UseCardSkill;

                contentType = ButtonContentType.Text;
                labelText = GetCardSkillString(skill);
            }
            else
            {
                triggerEvent = MenuEventType.None;

                contentType = ButtonContentType.Text;
            }
        }

        public static string GetCardSkillString(CardSkill skill)
        {
            switch (skill)
            {
                case CardSkill.Occupy:
                    return "土地\n占領";
                case CardSkill.Conversion:
                    return "魅惑\n宣教";
                case CardSkill.NormalAttack:
                    return "一般\n攻撃";
                case CardSkill.SpecialAttack:
                    return "特殊\n攻撃";
                case CardSkill.EnterBuilding:
                    return "拠点\n守備";
                case CardSkill.Construction:
                    return "施設\n建設";
                case CardSkill.Sacrifice:
                    return "生命\n供儀";
                case CardSkill.SwapPosition:
                    return "位置\n転換";
                default:
                    return skill.ToString();
            }
        }



    }

    /// <summary>
    /// 数値更新用ボタン
    /// </summary>
    [System.Serializable]
    public class ParamUpdateButtonData : ButtonData
    {

        [Header("数値更新設定")]
        public TechTree targetParameter;
        public int valueChange = 1;


        public ParamUpdateButtonData(int slot, CardType type, TechTree parameter, bool active = true)
        {
            slotNo = slot;
            cardType = type;
            isActive = active;

            targetParameter = parameter;

            backgroundSprite = UISpriteHelper.Instance.GetSprite(UISpriteID.Background_Square);

            if (isActive)
            {
                triggerEvent = MenuEventType.UpdateCardParameter;

                contentType = ButtonContentType.Text;
                labelText = GetTechTreeString(parameter);
            }
            else
            {
                triggerEvent = MenuEventType.None;

                contentType = ButtonContentType.Text;

            }

        }

        public static string GetTechTreeString(TechTree tech)
        {
            switch (tech)
            {
                case TechTree.HP:
                    return "HP";
                case TechTree.AP:
                    return "行動力";
                case TechTree.Occupy:
                    return "占領";
                case TechTree.Conversion:
                    return "魅惑";
                case TechTree.ATK:
                    return "攻撃力";
                case TechTree.Sacrifice:
                    return "生命供儀";
                case TechTree.AttackPosition:
                    return "攻撃口";
                case TechTree.AltarCount:
                    return "祭壇数";
                case TechTree.ConstructionCost:
                    return "建設費用";
                case TechTree.MovementCD:
                    return "移動クール";
                case TechTree.Buff:
                    return "BUFF";
                case TechTree.Heresy:
                    return "異端邪説";
                default:
                    return tech.ToString();
            }
        }

    }



    /// <summary>
    /// カード機能用ボタン
    /// </summary>
    [System.Serializable]
    public class PurchaseButtonData : ButtonData
    {

        /// <summary>
        /// 生成购买Button的Data
        /// 可以用于制作空Button槽
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="type"></param>
        /// <param name="active"></param>
        public PurchaseButtonData(int slot, CardType type,bool active = true)
        {
            slotNo = slot;
            cardType = type;
            isActive = active;

            backgroundSprite = UISpriteHelper.Instance.GetSprite(UISpriteID.Background_Square);

            if (isActive)
            {
                triggerEvent = MenuEventType.Purchase;

                contentType = ButtonContentType.Text;
                labelText = "購入";
            }
            else
            {
                triggerEvent = MenuEventType.None;

                contentType = ButtonContentType.Text;
            }


        }

    }



}