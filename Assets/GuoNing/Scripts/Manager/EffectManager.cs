using GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
	None,

	Piece_Charm_RedMoon,
	Piece_Charm_Silk,

	Piece_Occupy_RedMoon,
	Piece_Occupy_Silk,

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

	public GameObject PlayerEffect(OperationType type, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		Religion religion = SceneStateManager.Instance.PlayerReligion;

		EffectType effect = GetEffectByOperation(type, religion);

		if (effect == EffectType.None)
		{
			Debug.LogWarning($"No effect found for operation {type}, religion {religion}");
			return null;
		}

		return PlayEffect(effect, position, rotation, parent);
	}


	// 播放特效
	public GameObject PlayEffect(EffectType type,Vector3 position,Quaternion rotation,Transform parent = null)
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
		instance.SetUp(type);

        // 25.12.4 RI 添加攻击特效回收
        if (type == EffectType.Piece_Hit)
			StartCoroutine(RecycleAttackEffect(type, obj));

		return obj;

	}
    // 25.12.4 RI 添加攻击特效回收
    private IEnumerator RecycleAttackEffect(EffectType type, GameObject prefab)
	{
        yield return new WaitForSeconds(1.0f);
        Debug.Log("回收特效");
        var ps = prefab.GetComponentsInChildren<ParticleSystem>();
        foreach (var p in ps)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        prefab.SetActive(false);
        prefab.transform.SetParent(transform);
        pool[type].Enqueue(prefab);
    }
    // 回收特效
    public void Recycle(EffectType type,GameObject prefab)
    {
		var ps = prefab.GetComponentsInChildren<ParticleSystem>();
		foreach (var p in ps)
		{
			p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		prefab.SetActive(false);
		prefab.transform.SetParent(transform);
		pool[type].Enqueue(prefab);
	}

	private EffectType GetEffectByOperation(OperationType op, Religion rel)
	{
		switch (op)
		{
			case OperationType.Occupy:
				return rel switch
				{
					Religion.SilkReligion => EffectType.Piece_Occupy_Silk,
					Religion.RedMoonReligion => EffectType.Piece_Occupy_RedMoon,
					_ => EffectType.None
				};

			case OperationType.Charm:
				return rel switch
				{
					Religion.SilkReligion => EffectType.Piece_Charm_Silk,
					Religion.RedMoonReligion => EffectType.Piece_Charm_RedMoon,
					_ => EffectType.None
				};

			case OperationType.Attack:
				return EffectType.Piece_Hit;

			case OperationType.Cure:
				return EffectType.Piece_Heal;

			// 如果以后你有更多技能升级特效……
			//case OperationType.UpgradeHP:
			//    return EffectType.LevelUp_HP;

			default:
				return EffectType.None;
		}
	}
}
