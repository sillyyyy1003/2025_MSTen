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