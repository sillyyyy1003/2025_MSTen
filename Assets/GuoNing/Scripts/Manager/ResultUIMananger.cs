using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ResultUIManager : MonoBehaviour
{
	[Header("UIImage")]
	public Image ReligionIcon;  // 获胜宗教Icon
	public Image ReusltBar;     // 结果条

	[Header("UILayers")]
	public RectTransform ResultLayer;
	public RectTransform ResultDetail;
	public RectTransform GameUI;

	[Header("Buttons")]
	public Button ResultDetailButton;
	public Button GameExitButton;



	public static ResultUIManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}

	}

	private void Start()
	{
		ResultLayer.gameObject.SetActive(false);

		ResultDetailButton.onClick.AddListener(OnClickResultDetailButton);
		GameExitButton.onClick.AddListener(OnClickGameExitButton);
	}

	private void Update()
	{
		
		
	}

	public void Inititialize(int victoryID)
	{
		// 设定胜利还是失败
		int victoryPlayerID = victoryID;	
		int localPlayerID = GameManage.Instance.LocalPlayerID;

		if (victoryPlayerID == localPlayerID)
		{
			ReusltBar.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.Result_Bar, "Result_VictoryBar");
		}
		else
		{
			ReusltBar.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.Result_Bar, "Result_DefeatBar");
		}

		// 设定宗教Icon
		int SpriteSerial = (int)GameManage.Instance.GetPlayerData(victoryPlayerID).PlayerReligion - 1;
		ReligionIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.IconList_Religion, SpriteSerial);

		OpenResultPanel();

	}

	private void OnClickResultDetailButton()
	{
		// 设定Icon向上移动 大小变化

		// 设定Bar位置变动 向上移动

		// 出现Detail界面
		ResultDetail.gameObject.SetActive(true);
		ResultDetailButton.gameObject.SetActive(false);

	}

	private void OnClickGameExitButton()
	{
		Debug.Log("Exit to Title Scene");
		SceneController.Instance.SwitchToTitleScene();
	}

	private int GetVictoryPlayerIDBySurrender()
	{
		int localID = GameManage.Instance.LocalPlayerID;
		int enemyID = GameManage.Instance.OtherPlayerID;
		return enemyID;

	}

	public void Surrender()
	{
		int localID = GameManage.Instance.LocalPlayerID;
		NetGameSystem.Instance.SendGameOverMessage(GetVictoryPlayerIDBySurrender(),localID,"surrender");
	}

	public void OpenResultPanel()
	{
		ResultLayer.gameObject.SetActive(true);

		// 关闭GameUI	
		GameUI.gameObject.SetActive(false);

		// 关闭Detail界面
		ResultDetail.gameObject.SetActive(false);

		// ResultButton显示
		ResultDetailButton.gameObject.SetActive(true);
	}
}

[System.Serializable]
public struct ResultData
{
	public int PlayerID;            // 玩家ID
	public int CellNumber;          // 占领的格子的数量
	public int PieceNumber;         // 棋子的数量
	public int BuildingNumber;      // 建筑数量
	public int PieceDestroyedNumber; // 消灭的棋子数量
	public int BuildingDestroyedNumber; // 摧毁的建筑的数量
	public int CharmSucceedNumber;  // 成功魅惑棋子的数量
	public int ResourceGet;     // 获得的资源数量
	public int ResourceUsed;     // 使用的资源数量

	public ResultData(int playerID, int cellNumber, int pieceNumber, int buildingNumber,
		int pieceDestroyedNumber, int buildingDestroyedNumber, int charmSucceedNumber,
		int resourceGet, int resourceUsed)
	{
		PlayerID = playerID;
		CellNumber = cellNumber;
		PieceNumber = pieceNumber;
		BuildingNumber = buildingNumber;
		PieceDestroyedNumber = pieceDestroyedNumber;
		BuildingDestroyedNumber = buildingDestroyedNumber;
		CharmSucceedNumber = charmSucceedNumber;
		ResourceGet = resourceGet;
		ResourceUsed = resourceUsed;
	}
}