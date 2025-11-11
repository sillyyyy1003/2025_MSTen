using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeature : MonoBehaviour
{
	public int HexCellIndex { get; private set; }
	private MeshRenderer render;
	private Material material;

	private bool isTransparent = false;

	public void Init(int index)
	{
		HexCellIndex = index;

		if (render == null)
			render = GetComponentInChildren<MeshRenderer>();

		// 仅在第一次初始化时实例化材质
		if (material == null && render != null)
			material = render.material;
	}

	public void SetTransparency(bool transparent)
	{
		if (material == null)
			return;

		// 避免重复设置（减少性能浪费）
		if (transparent == isTransparent)
			return;

		isTransparent = transparent;

		Color color = material.color;
		color.a = transparent ? 0.6f : 1f;
		material.color = color;

		SetMaterialTransparent(material, transparent);
	}

	private void SetMaterialTransparent(Material mat, bool transparent)
	{
		if (transparent)
		{
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			mat.SetInt("_ZWrite", 0);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.EnableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}
		else
		{
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
		}
	}
}
