using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.UI
{
    /// <summary>
    /// ボタンメニューの階層データ定義
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonMenuData", menuName = "GameData/UI/ButtonMenuData")]
    public class ButtonMenuData : ScriptableObject
    {
        [Header("基本情報")]
        public string menuId;

        [Header("ボタン設定")]
        public ButtonData[] buttons = new ButtonData[6];

        [Header("表示設定")]
        public string menuTitleText;
        public Sprite backgroundSprite;
        public AudioClip titleVoice;

        public bool isRootPage = false;

    }


    /// <summary>
    /// 単一ボタンのデータ構造
    /// </summary>
    [System.Serializable]
    public class ButtonData
    {
        public bool isActive = true;          // ボタンが有効かどうか
        public ButtonContentType contentType; // 文字か画像か
        public string labelText;              // テキスト（contentTypeがTextの時のみ有効）
        public Sprite iconSprite;             // アイコン（contentTypeがImageの時のみ有効）

        [Header("遷移・イベント設定")]
        public string nextMenuId;             // 次のメニュー（なければnull）
        public MenuEventType triggerEvent;         // トリガーID（スクリプト側でイベントを紐付け）

        [Tooltip("押下時の効果設定")]
        public AudioClip onClickSound;

    }


    /// <summary>
    /// ボタン表示のタイプ
    /// </summary>
    public enum ButtonContentType
    {
        Text,
        Image
    }

    public enum MenuEventType
    {
        None,               // 何も起きない
        NextMenu,           // 次のメニューへ遷移
        Purchase,           // 購入処理
        UseCardSkill,       // カードスキルを使用
        UpdateCardParameter // カードパラメータ更新

    }







}