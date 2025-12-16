using GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
	None,

	Piece_Charm_RedMoon_Fail,
	Piece_Charm_RedMoon_Continue,
	Piece_Charm_Silk_Fail,
	Piece_Charm_Silk_Continue,
	Piece_Charm_Maya_Fail,
	Piece_Charm_Maya_Continue,
	Piece_Charm_Mad_Fail,
	Piece_Charm_Mad_Continue,

	// 追加更多特效


	Piece_Occupy_RedMoon_Success,
	Piece_Occupy_Silk_Sucess,
	Piece_Occupy_Maya_Success,
	Piece_Occupy_Mad_Success,

	Piece_Occupy_RedMoon_Fail,
	Piece_Occupy_Silk_Fail,
	Piece_Occupy_Maya_Fail,
	Piece_Occupy_Mad_Fail,
	// 追加更多特效

	Piece_Hit,
    Piece_Heal,
	Building_Build,

    LevelUp_HP,
    LevelUp_AP,
	LevelUp_ATK,
	LevelUp_Occupy,
	LevelUp_Charm,
	LevelUp_Cure,
	
}

/// <summary>
/// 用于管理所有的Effect
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    [System.Serializable]
    public class Effect
    {
       public EffectType type;
       public GameObject prefab;

    }

    public List<Effect> effects = new List<Effect>();
    private Dictionary<EffectType, Queue<GameObject>> pool = new();
	private Dictionary<Transform, Dictionary<EffectType, GameObject>> activeLoopEffects = new();

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



	// Start is called before the first frame update
	void Start()
    {
        pool= new Dictionary<EffectType, Queue<GameObject>>();
        foreach (var effect in effects)
		{
			pool[effect.type] = new Queue<GameObject>();
		}
    }
	
	// 普通升级特效播放
	public GameObject PlayEffect(PieceUpgradeType type, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		switch (type)
		{
			case PieceUpgradeType.HP:
				return PlayEffect(EffectType.LevelUp_HP, position, rotation, parent);

			case PieceUpgradeType.AP:
				return PlayEffect(EffectType.LevelUp_AP, position, rotation, parent);
			default:
				return null;
		}
	}

	// 特殊升级播放特效
	public GameObject PlayEffect(SpecialUpgradeType type, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		switch (type)
		{
			case SpecialUpgradeType.MilitaryAttackPower:
				return PlayEffect(EffectType.LevelUp_ATK, position, rotation, parent);
			case SpecialUpgradeType.MissionaryConvertEnemy:
				return PlayEffect(EffectType.LevelUp_Charm, position, rotation, parent);
			case SpecialUpgradeType.MissionaryOccupy:
				return PlayEffect(EffectType.LevelUp_Occupy, position, rotation, parent);
			case SpecialUpgradeType.FarmerSacrifice:
				return PlayEffect(EffectType.LevelUp_Cure, position, rotation, parent);
			default:
				return null;
		}
	}


	// 播放特效
	public GameObject PlayEffect(EffectType type, Vector3 position, Quaternion rotation, bool isLoop = false, Transform parent = null)
    {
        if (!pool.ContainsKey(type))
        {
            Debug.LogWarning($"Effect type {type} not found!");
			return null;
        }

        GameObject obj;

		if (pool[type].Count > 0)
        {
			obj = pool[type].Dequeue();
			obj.SetActive(true);
		}
        else
        {
            var entry =effects.Find(x => x.type == type);
            obj = Instantiate(entry.prefab);
        }

		// Set transform
		obj.transform.position= position;
		obj.transform.rotation = rotation;
        obj.transform.SetParent(parent);
		var instance = obj.GetComponent<EffectInstance>();
		instance.SetUp(type, isLoop);


		if (isLoop && parent != null)
		{
			if (!activeLoopEffects.ContainsKey(parent))
				activeLoopEffects[parent] = new Dictionary<EffectType, GameObject>();

			// 不允许同类型重复
			if (!activeLoopEffects[parent].ContainsKey(type))
				activeLoopEffects[parent].Add(type, obj);
		}

		return obj;


	}


    // 回收特效
    public void Recycle(EffectType type,GameObject prefab)
    {
		/*
		var ps = prefab.GetComponentsInChildren<ParticleSystem>();
		foreach (var p in ps)
		{
			p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		prefab.SetActive(false);
		prefab.transform.SetParent(transform);
		pool[type].Enqueue(prefab);
		*/
		// ⭐ 先从 activeLoopEffects 中移除
		if (prefab.transform.parent != null)
		{
			var parent = prefab.transform.parent;
			if (activeLoopEffects.TryGetValue(parent, out var dict))
			{
				if (dict.ContainsKey(type))
					dict.Remove(type);

				if (dict.Count == 0)
					activeLoopEffects.Remove(parent);
			}
		}

		var ps = prefab.GetComponentsInChildren<ParticleSystem>();
		foreach (var p in ps)
		{
			p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		prefab.SetActive(false);
		prefab.transform.SetParent(transform);
		pool[type].Enqueue(prefab);
	}

	public void StopEffect(Transform target, EffectType type)
	{
		if (!activeLoopEffects.TryGetValue(target, out var dict))
			return;

		if (!dict.TryGetValue(type, out var obj))
			return;

		Recycle(type, obj);
	}


	private EffectType GetOccupyEffectType(Religion religion, bool isSuccess)
	{
		return religion switch
		{
			Religion.SilkReligion =>
				isSuccess
					? EffectType.Piece_Occupy_Silk_Sucess
					: EffectType.Piece_Occupy_Silk_Fail,

			Religion.RedMoonReligion =>
				isSuccess
					? EffectType.Piece_Occupy_RedMoon_Success
					: EffectType.Piece_Occupy_RedMoon_Fail,

			Religion.MayaReligion =>
				isSuccess
					? EffectType.Piece_Occupy_Maya_Success
					: EffectType.Piece_Occupy_Maya_Fail,

			Religion.MadScientistReligion =>
				isSuccess
					? EffectType.Piece_Occupy_Mad_Success
					: EffectType.Piece_Occupy_Mad_Fail,

			_ => EffectType.None
		};
	}

	public GameObject PlayOccupyEffect(
	Vector3 position,
	Quaternion rotation,
	Transform parent,
	bool isSuccess)
	{
		var religion = SceneStateManager.Instance.PlayerReligion;

		EffectType effectType = GetOccupyEffectType(religion, isSuccess);

		if (effectType == EffectType.None)
		{
			Debug.LogWarning($"[Effect] No Occupy effect for {religion}");
			return null;
		}

		return PlayEffect(effectType, position, rotation, false, parent);
	}

	
	private EffectType GetCharmEffectType(Religion religion, bool isSuccess)
	{
		return religion switch
		{
			Religion.SilkReligion =>
				isSuccess
					? EffectType.Piece_Charm_Silk_Continue
					: EffectType.Piece_Charm_Silk_Fail ,

			Religion.RedMoonReligion =>
				isSuccess
					? EffectType.Piece_Charm_RedMoon_Continue
					: EffectType.Piece_Charm_RedMoon_Fail,

			Religion.MayaReligion =>
				isSuccess
					? EffectType.Piece_Charm_Maya_Continue
					: EffectType.Piece_Charm_Maya_Fail,

			Religion.MadScientistReligion =>
			isSuccess
					? EffectType.Piece_Charm_Mad_Continue
					: EffectType.Piece_Charm_Mad_Fail,

			_ => EffectType.None
		};
	}

	public void PlayCharmEffect(
	Transform target,
	Vector3 position,
	Quaternion rotation,
	bool isSuccess)
	{
		var religion = SceneStateManager.Instance.PlayerReligion;
		var effectType = GetCharmEffectType(religion, isSuccess);

		if (effectType == EffectType.None) return;

		PlayEffect(
			effectType,
			position,
			rotation,
			//25,12,16 RI change loop to true
			isLoop: true,
			parent: target
		);

	}
}
