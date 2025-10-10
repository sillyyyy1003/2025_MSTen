using UnityEngine;

public class NewMapMenu : MonoBehaviour
{

	public HexGrid hexGrid;
	[SerializeField]
	[Header("Map Size")]
	public Vector2Int smallMapSize = new Vector2Int(20, 15);

	public Vector2Int mediumMapSize = new Vector2Int(40, 30);
	public Vector2Int largeMapSize = new Vector2Int(80, 60);


	public void Open()
	{
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	void CreateMap(int x, int z)
	{
		hexGrid.CreateMap(x, z);

        // 25.10.10 RI 删除Camera相关避免loadMap出错
        //HexMapCamera.ValidatePosition();

        Close();
	}

	public void CreateSmallMap()
	{
		//CreateMap(20, 15);
		CreateMap(smallMapSize.x,smallMapSize.y);
	}

	public void CreateMediumMap()
	{
		//CreateMap(40, 30);
		CreateMap(mediumMapSize.x, mediumMapSize.y);
	}

	public void CreateLargeMap()
	{
		//CreateMap(80, 60);
		CreateMap(largeMapSize.x, largeMapSize.y);
	}
}