using UnityEngine;


namespace SoundSystem
{
	//--------------------------------------------------------------------------------
	// 列挙型
	// ※ 登録する音源を増やす場合は、ここに列挙型を追加する
	//--------------------------------------------------------------------------------

	/// <summary> BGMの種類を列挙型で定義 </summary>
	public enum TYPE_BGM
	{
		TITLE,          // タイトル画面のBGM
		RESULT,         // リザルト画面のBGM
		GAME_OVER,      // ゲームオーバー時のBGM
		GAME_CLEAR,     // ゲームクリア時のBGM
		CUTSCENE,       // カットシーンのBGM
		EVENT,          // イベントシーンのBGM
		PAUSE,          // ポーズ画面のBGM
		LOADING,        // ローディング画面のBGM
		MENU,           // メインメニューのBGM
		STAGE_SELECT,	// ステージ選択画面のBGM
	}

	/// <summary> SEの種類を列挙型で定義 </summary>
	public enum TYPE_SE
	{
		SELECT,             // メニュー選択時の音
		DECISION,           // 決定時の音
		CANCEL,             // キャンセル時の音
		ATTACK,             // 攻撃時の音
		DAMAGE,             // ダメージを受けた時の音
		DEATH,              // キャラクターが死亡した時の音
		ITEM,               // アイテム取得時の音
		JUMP,               // ジャンプ時の音
		LAND,               // 着地時の音
		LEVEL_UP,           // レベルアップ時の音
		SKILL,              // スキル使用時の音
		EXPLOSION,          // 爆発音
		DOOR_OPEN,          // 扉の開閉音
		FOOTSTEP,           // 足音
		UI_SELECT,          // UI選択時の音
		UI_DECISION,        // UI決定時の音
		UI_CANCEL,          // UIキャンセル時の音
		UI_HOVER,           // UIにマウスカーソルが乗った時の音
		WARNING,            // 警告音
		COUNTDOWN,          // カウントダウンの音
		MOVEWALL,			// 壁の移動音
		STAR,				// 星の取得音
		PUSH_SWITCH,        // スイッチを押した時の音
		DOOR_CLOSE,			// 扉の閉まる音
		TUBO_HIT,			// 壺に当たった時
		BOX_HIT,			//箱に当たった時
		HEAL,               // 回復時の音



	}



	//--------------------------------------------------------------------------------
	// 構造体
	//--------------------------------------------------------------------------------

	/// <summary> BGMの列挙型とAudioClipを紐付ける構造体 </summary>
	[System.Serializable]
	public struct ResourceBGM
	{
        [SearchableEnum]
        public TYPE_BGM type;
		public AudioClip clip;
	}

	/// <summary> SEの列挙型とAudioClipを紐付ける構造体 </summary>
	[System.Serializable]
	public struct ResourceSE
	{
		[SearchableEnum]
		public TYPE_SE type;
		public AudioClip clip;
	}
}
