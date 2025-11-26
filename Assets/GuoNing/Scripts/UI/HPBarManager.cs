using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBarManager : MonoBehaviour
{
	public static HPBarManager Instance { get; private set; }

	[SerializeField] private HPBar hpBarPrefab;
	[SerializeField] private Canvas uiCanvas;

	public Vector3 defaultOffset = new Vector3(0, 2, 0);

	private Dictionary<int, HPBar> hpBars = new Dictionary<int, HPBar>();

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

	public void CreateHPBar(int id, int maxHP, Transform target, CardType type)
	{
		var hpBar = Instantiate(hpBarPrefab, uiCanvas.transform);
		hpBar.Initialize(maxHP, target, defaultOffset, type);
		hpBars.Add(id, hpBar);
	}

	/// <summary>
	/// 通过ID更新血条
	/// </summary>
	/// <param name="id"></param>
	/// <param name="hp"></param>
	/// <returns></returns>
	public bool UpdateHPBarByID(int id, int hp)
	{
		if (hpBars.ContainsKey(id))
		{
			hpBars[id].UpdateHP(hp);
			return true;
		}
		Debug.LogWarning($"HPBar with ID {id} not found.");
		return false;
	}

	/// <summary>
	/// 通过ID更新血条和最大血量
	/// </summary>
	/// <param name="id"></param>
	/// <param name="hp"></param>
	/// <param name="maxHP"></param>
	/// <returns></returns>
	public bool UpdateHPBarByID(int id, int hp, int maxHP)
	{
		if (hpBars.ContainsKey(id))
		{
			hpBars[id].UpdateHP(hp,maxHP);
			return true;
		}
		Debug.LogWarning($"HPBar with ID {id} not found.");
		return false;
	}

	/// <summary>
	/// 通过ID 移除血条（当单位死亡时）
	/// </summary>
	/// <param name="id"></param>
	public void RemoveHPBar(int id)
	{
		if (hpBars.TryGetValue(id, out HPBar bar))
		{
			Destroy(bar.gameObject);
			hpBars.Remove(id);
		}
		else
		{
			Debug.LogWarning($"Try to remove HPBar but ID {id} not found.");
		}
	}


}
