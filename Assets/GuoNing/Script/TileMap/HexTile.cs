using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	Occupiable,      // Default type: can be passed & occupied
	PassableOnly,    // Can be passed but cannot be occupied
	Impassable       // Cannot be passed or occupied
}

/// <summary>
/// Tile data class for hexagonal tiles.
/// </summary>
public class HexTile : MonoBehaviour
{
	[Header("Tile property")] 
	public TileType tileType;
	public int cost = 1;
	public bool isOccupied = false;     // Whether a player occupies this til
	
	//public float bounusPoint = 1f;  // Player gets this point when occupy this tile

	void Start()
	{
		var tilemap = GetComponentInParent<UnityEngine.Tilemaps.Tilemap>();
		if (tilemap)
		{
			//Vector3Int cellPos = tilemap.WorldToCell(transform.position);
			//TileManager.Instance.RegisterTile(cellPos, this);
			Vector3Int cellPos = tilemap.WorldToCell(transform.position);
			TileManager.Instance.RegisterTile(cellPos, this);
			Debug.Log($"[Register] cell={cellPos}, world={transform.position}");
		}
		else
		{
			Debug.Log("No tile map");
		}
	}
}
