using GameData;
using GamePieces;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct ResultData
{
	public string PlayerId;                // 玩家ID
	public int CellNumber;                 // 占领的格子的数量
	public int PieceNumber;                // 棋子的数量
	public int BuildingNumber;             // 建筑数量
	public int PieceDestroyedNumber;       // 消灭的棋子数量
	public int BuildingDestroyedNumber;    // 摧毁的建筑的数量
	public int CharmSucceedNumber;         // 成功魅惑棋子的数量
	public int ResourceGet;                // 获得的资源数量
	public int ResourceUsed;               // 使用的资源数量

	public ResultData(
		string playerId,
		int cellNumber,
		int pieceNumber,
		int buildingNumber,
		int pieceDestroyedNumber,
		int buildingDestroyedNumber,
		int charmSucceedNumber,
		int resourceGet,
		int resourceUsed)
	{
		PlayerId = playerId;
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
/// <summary>
/// 
/// </summary>
public class ResultDetailUI : MonoBehaviour
{
    [Header("UIComponent")]
    public Image ReligionIcon;
    public TMP_Text UserIdTxt;
    public TMP_Text CellNumberTxt;
    public TMP_Text PieceBuildingNumberTxt;
	public TMP_Text PieceDestroyedNumberTxt;
	public TMP_Text BuildingDestroyedNumberTxt;
	public TMP_Text CharmSucceedNumberTxt;
	public TMP_Text ResourceUsedGetTxt;



	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame


    public void Initialize(ResultData data,Religion religion)
    {
		// 设定玩家宗教Icon
	    int spriteSerial = (int)religion - 1;
	    ReligionIcon.sprite = UISpriteHelper.Instance.GetSubSprite(UISpriteID.IconList_Religion, spriteSerial);
	    UserIdTxt.text = data.PlayerId;
		// 处理所有数据
		CellNumberTxt.text = data.CellNumber.ToString();
		PieceBuildingNumberTxt.text = data.PieceNumber + "/" + data.BuildingNumber;
        PieceDestroyedNumberTxt.text = data.PieceDestroyedNumber.ToString();
        BuildingDestroyedNumberTxt.text = data.BuildingDestroyedNumber.ToString();
        CharmSucceedNumberTxt.text = data.CharmSucceedNumber.ToString();
        ResourceUsedGetTxt.text = data.ResourceUsed + "/" + data.ResourceGet;

    }
}
