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
		TITLE,				// タイトル画面のBGM
		GAME,				// ゲームプレイ中のBGM
		REDMOON_THEME,      // Red Moon Theme
		SILK_THEME          // Silk Theme
	}

	/// <summary> SEの種類を列挙型で定義 </summary>
	public enum TYPE_SE
	{
		BUTTONCLICKED,		// ボタンをクリックしたときのSE
		UPGRADE,            // コマがレベルアップしたときのSE
		BUYCARD,			// カードを購入したときのSE
		SPAWNUNIT,			// ユニット出てきたとき
		SPREADCARD,			// Open card panel
		CHARMED,            // ユニットが魅了されたとき
		HEAL,
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
