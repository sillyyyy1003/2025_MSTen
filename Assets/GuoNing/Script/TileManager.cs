using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    private Dictionary<Vector3Int,HexTile> tileDic =new Dictionary<Vector3Int,HexTile>();
    public Tilemap tilemap;

    void Awake()
    {
        if(!Instance)Instance =this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Register tile with cell coordinate
    /// </summary>
    /// <param name="cellPos">cell pos e.g.[0,1]</param>
    /// <param name="tile">tile data</param>
    public void RegisterTile(Vector3Int cellPos, HexTile tile)
    {
	    if (!tileDic.ContainsKey(cellPos))
	    {
		    tileDic[cellPos] = tile;
	    }
		Debug.Log("Tile Registered");
    }

	/// <summary>
	/// Find tile data with cell coordinate
	/// </summary>
	/// <param name="cellPos"></param>
	/// <returns></returns>
	public HexTile GetTile(Vector3Int cellPos)
    {
	    tileDic.TryGetValue(cellPos, out HexTile tile);
	    return tile;
    }

	public void OutputTiles()
	{
		foreach (var tile in tileDic)
		{
			Debug.Log($"tile pos: {tile.Key}");
		}
	}

	public int GetTiles()
	{
		return tileDic.Count;
	}


}
