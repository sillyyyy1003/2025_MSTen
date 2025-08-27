using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridLayout : MonoBehaviour
{
	[Header("Grid Settings")]
	public Vector2Int gridSize;

	[Header("Tile Settings")] 
	public float outerSize = 1f;
	public float innerSize = 0f;
	public float height = 1f;
	public bool isFlatTopped;
	public Material material;

	private void OnEnable()
	{
		LayoutGrid();
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
			LayoutGrid();
	}

	private void LayoutGrid()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			Destroy(transform.GetChild(i).gameObject);
		}


		for (int y = 0; y < gridSize.y; y++)
		{
			for (int x = 0; x < gridSize.x; x++)
			{
				GameObject tile = new GameObject($"Hex{x},{y}", typeof(HexRenderer));
				tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));

				HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
				hexRenderer.isFlatTopped = isFlatTopped;
				hexRenderer.outerSize = outerSize;
				hexRenderer.innerSize = innerSize;
				hexRenderer.height = height;
				hexRenderer.SetMaterial(material);
				hexRenderer.DrawMesh();
				;
				tile.transform.SetParent(transform, false);
			}
		}
	}

	public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
	{
		int column = coordinate.x;
		int row =coordinate.y;

		float width;
		float height;
		float xPos;
		float yPos;
		bool shouldOffset;
		float horizontalDistance;
		float verticalDistance;
		float offset;
		float size = outerSize;

		if (!isFlatTopped)
		{
			shouldOffset = (row % 2) == 0;
			width = Mathf.Sqrt(3) * size;
			height = 2f * size;
			 
			horizontalDistance = width;
			verticalDistance=height * 0.75f;

			offset = shouldOffset ? width / 2f : 0f;
			xPos = column * horizontalDistance + offset;
			yPos = row * verticalDistance; 
		}
		else
		{
			shouldOffset = (column % 2) == 0;
			width = 2f * size;
			height = Mathf.Sqrt(3) * size;

			horizontalDistance = width * 0.75f;
			verticalDistance = height;

			offset = shouldOffset ? height / 2f : 0f;
			xPos = column * horizontalDistance;
			yPos = row * verticalDistance - offset;
		}

		return new Vector3(xPos, 0, -yPos);

	}
}
