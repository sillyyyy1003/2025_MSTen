using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{


	//--------------------------------------------------------------------------------
	// プロパティ
	//--------------------------------------------------------------------------------
	[Header("Menus")]
	public RectTransform LeftMenu;
	public RectTransform RightMenu;
	public Material mat;
	// World UI
	[Header("Left Menu")]
	public HexButton Button_EndGame;
	public HexButton Button_GameTutorial;
	public HexButton Button_SinglePlayer;
	public HexButton Button_OnlineGame;
	public HexButton Button_Setting;

	[Header("Right Menu")]
	public RectTransform OptionMenu;
	public RectTransform OnlineMenu;
	public RectTransform RightDetailMenu;

	// Screen UI
	[Header("OnlineButton")]
	public HexButton Button_CreateGame;
	public HexButton Button_AddGame;

	/// <summary>
	/// 画面に表示する建物モデル
	/// </summary>
	public Transform Building;//todo: make it a model manager later



	//--------------------------------------------------------------------------------
	// メソッド
	//--------------------------------------------------------------------------------

	void Update()
	{
		// Right mouse button click
		if (Input.GetMouseButton(1))
		{
			//  Close all option menu& online menu for next usage
			OptionMenu.gameObject.SetActive(false);
			OnlineMenu.gameObject.SetActive(false);
			UpdateBackground(false);

			// Reset button state
			Button_Setting.ResetHexButton();
			Button_OnlineGame.ResetHexButton();
			
		}

		//=========Building model update
		if (Building)
		{
			Building.Rotate(Vector3.up, 20f * Time.deltaTime);
		}

		
	}

	// Start is called before the first frame update
	void Start()
	{
		Button_EndGame.onClick.AddListener(() => OnClickEndGame());
		Button_SinglePlayer.onClick.AddListener(() => OnClickSinglePlayer());
		Button_OnlineGame.onClick.AddListener(() => OnClickOnlineGame());
		Button_Setting.onClick.AddListener(() => OnClickSetting());


		Button_CreateGame.onClick.AddListener(() => OnClickCreateGame());
		Button_AddGame.onClick.AddListener(() => OnClickAddGame());

		UpdateBackground(false);
	}

	/// <summary>
	/// End Game button event
	/// </summary>
	private void OnClickEndGame()
	{
		Debug.Log("EndGame");
	}

	/// <summary>
	/// Single play button event
	/// </summary>
	private void OnClickSinglePlayer()
	{
		SceneStateManager.Instance.bIsSingle = true;
		SceneManager.LoadScene("MainGame");
		Debug.Log("SinglePlayer");
	}

	/// <summary>
	/// Online game button event
	/// </summary>
	private void OnClickOnlineGame()
	{
		
		//  Set option menu active
		OnlineMenu.gameObject.SetActive(true);

		// Change material
		UpdateBackground(true);

	}

	/// <summary>
	/// Setting event
	/// </summary>
	private void OnClickSetting()
	{
		//  Set option menu active
		OptionMenu.gameObject.SetActive(true);
		OnlineMenu.gameObject.SetActive(false);

		// Change material
		UpdateBackground(true);

	}

	/// <summary>
	/// Create online game button
	/// </summary>
	private void OnClickCreateGame()
	{
		if (SceneStateManager.Instance != null)
		{
			//string playerName = SceneStateManager.Instance.PlayerName;


			// SceneStateManager.Instance.PlayerName = playerName;

			// ﾉ靹ﾃﾎｪｷﾎ｣ﾊｽ
			SceneStateManager.Instance.SetAsServer(true);
		}
		else
		{
			Debug.LogError("SceneStateManager.Instance ﾎｪｿﾕ!");
		}


		SceneManager.LoadScene("MainGame");
	}


	/// <summary>
	/// Join game button event
	/// </summary>
	private void OnClickAddGame()
	{
		if (SceneStateManager.Instance != null)
		{
			SceneStateManager.Instance.SetAsServer(false);
		}
		else
		{
			Debug.LogError("SceneStateManager.Instance is null!");
		}
		SceneManager.LoadScene("MainGame");

	}


	/// <summary>
	/// Update game background blur effect
	/// </summary>
	/// <param name="isOn"></param>
	private void UpdateBackground(bool isOn)
	{
		if (isOn)
		{
			mat.SetFloat("_UseMask", 1);
			mat.SetFloat("_BlurSize", 5);
		}
		else
		{
			mat.SetFloat("_UseMask", 0);
			mat.SetFloat("_BlurSize", 0);
		}
	}

}


