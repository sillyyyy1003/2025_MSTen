using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStatusUIManager : MonoBehaviour
{
	public static UnitStatusUIManager Instance { get; private set; }

	[SerializeField] private UnitStatusUI statusUIPrefab;
	[SerializeField] private Canvas uiCanvas;
	public Vector3 defaultOffset = new Vector3(0, 2, 0);

	private Dictionary<int, UnitStatusUI> units = new();

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	public void CreateStatusUI(
		int id,
		int maxHP,
		int maxAP,
		Transform target,
		CardType type)
	{
		var ui = Instantiate(statusUIPrefab, uiCanvas.transform);
		ui.Initialize(maxHP, maxAP, target, defaultOffset, type);
		units.Add(id, ui);
	}

	/// <summary>
	/// 更新HPUI显示
	/// </summary>
	/// <param name="id"></param>
	/// <param name="hp"></param>
	/// <returns></returns>
	public bool UpdateHPByID(int id, int hp)
	{
		if (units.TryGetValue(id, out var ui))
		{
			ui.SetHP(hp);
			return true;
		}
		Debug.LogWarning($"StatusUI with ID {id} not found (HP).");
		return false;
	}

	/// <summary>
	/// 更新HPUI显示 包括最大HP
	/// </summary>
	/// <param name="id"></param>
	/// <param name="hp"></param>
	/// <param name="maxHP"></param>
	/// <returns></returns>
	public bool UpdateHPByID(int id, int hp, int maxHP)
	{
		if (units.TryGetValue(id, out var ui))
		{
			ui.SetHP(hp, maxHP);
			return true;
		}
		Debug.LogWarning($"StatusUI with ID {id} not found (HP+MaxHP).");
		return false;
	}
	
	/// <summary>
	/// 更新行动力UI显示
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ap"></param>
	/// <returns></returns>
	public bool UpdateAPByID(int id, int ap)
	{
		if (units.TryGetValue(id, out var ui))
		{
			ui.SetAP(ap);
			return true;
		}
		Debug.LogWarning($"StatusUI with ID {id} not found (AP).");
		return false;
	}

	/// <summary>
	/// 更新行动力UI显示 包括最大行动力
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ap"></param>
	/// <param name="maxAP"></param>
	/// <returns></returns>
	public bool UpdateAPByID(int id, int ap, int maxAP)
	{
		if (units.TryGetValue(id, out var ui))
		{
			ui.SetAP(ap, maxAP);
			return true;
		}
		Debug.LogWarning($"StatusUI with ID {id} not found (AP+MaxAP).");
		return false;
	}

	/// <summary>
	/// 当角色死亡时移除其状态UI
	/// </summary>
	/// <param name="id"></param>
	public void RemoveStatusUI(int id)
	{
		if (units.TryGetValue(id, out var ui))
		{
			Destroy(ui.gameObject);
			units.Remove(id);
		}
		else
		{
			Debug.LogWarning($"Try to remove StatusUI but ID {id} not found.");
		}
	}
}