using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeature : MonoBehaviour
{
	public int HexCellIndex { get; private set; }
	private List<Material> materials=new List<Material>();

	private bool isTransparent = false;

	public void Init(int index)
	{
		HexCellIndex = index;

		Renderer[] renderers = GetComponentsInChildren<Renderer>();

		// 仅在第一次初始化时实例化材质
		foreach (var r in renderers)
		{
			materials.AddRange(r.materials);
		}
		Debug.Log(materials.Count);
	}

	public void SetTransparency(bool transparent)
	{
		if (materials.Count == 0)
			return;

		// 避免重复设置（减少性能浪费）
		if (transparent == isTransparent)
			return;

		isTransparent = transparent;

		foreach (var material in materials)
		{
			Color color = material.color;
			color.a = transparent ? 0.6f : 1f;
			material.color = color;
			SetMaterialTransparent(material, transparent);
		}
	}

	private void SetMaterialTransparent(Material mat, bool transparent)
	{
		if (transparent)
		{
			mat.SetFloat("_Surface", 1); // 切成 Transparent
			mat.SetFloat("_AlphaClip", 0);

			mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");

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
			mat.SetFloat("_Surface", 0);
			mat.SetFloat("_AlphaClip", 0);

			mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");
			mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
		}
	}
}
