using GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 增强版 UnitListTable - 支持网络同步和 Prefab 管理
/// </summary>
public class UnitListTable : MonoBehaviour
{
    // 单例模式 - 方便全局访问
    public static UnitListTable Instance { get; private set; }

    public record PieceDetail(PieceType PieceType, Religion Religion);

    [System.Serializable]
    public class PieceDataEntry
    {
        [SerializeField] private PieceType pieceType;
        [SerializeField] private Religion religion;
        [SerializeField] private PieceDataSO pieceDataSO;

        public PieceType PieceType => pieceType;
        public Religion Religion => religion;
        public PieceDataSO PieceDataSO => pieceDataSO;
    }

    [Header("Unit Data Table")]
    [SerializeField] private List<PieceDataEntry> pieceDataEntries = new List<PieceDataEntry>();

    // 主查找字典 - 通过 PieceType 和 Religion 查找
    private Dictionary<PieceDetail, PieceDataSO> pieceDataDict;

    //25.11.18 RI 添加废墟预制体
    public List<GameObject> Ruins;

    //// 新增：通过资源路径查找（用于网络同步）
    //private Dictionary<string, PieceDataSO> pieceDataByPath;

    //// 新增：通过 CardType 查找（兼容现有代码）
    //private Dictionary<CardType, Dictionary<Religion, PieceDataSO>> pieceDataByCardType;

    //// 缓存所有的 prefab，避免重复加载
    //private Dictionary<string, GameObject> prefabCache;

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionary()
    {
        pieceDataDict = new Dictionary<PieceDetail, PieceDataSO>(pieceDataEntries.Count);
        //pieceDataByPath = new Dictionary<string, PieceDataSO>();
        //pieceDataByCardType = new Dictionary<CardType, Dictionary<Religion, PieceDataSO>>();
        //prefabCache = new Dictionary<string, GameObject>();

        int validCount = 0;
        int errorCount = 0;

        foreach (var entry in pieceDataEntries)
        {
            if (entry.PieceType != PieceType.None &&
                entry.Religion != Religion.None &&
                entry.PieceDataSO != null)
            {
                // 主字典：通过 PieceDetail 查找
                var key = new PieceDetail(entry.PieceType, entry.Religion);
                pieceDataDict[key] = entry.PieceDataSO;
                //// 新增：通过资源路径查找
                //if (!string.IsNullOrEmpty(entry.PieceDataSO.piecePrefabResourcePath))
                //{
                //    pieceDataByPath[entry.PieceDataSO.piecePrefabResourcePath] = entry.PieceDataSO;
                //}
                //else
                //{
                //    Debug.LogWarning($"[UnitListTable] PieceDataSO '{entry.PieceDataSO.pieceName}' 缺少 piecePrefabResourcePath！");
                //}

                //// 新增：通过 CardType 查找（如果 PieceType 可以转换为 CardType）
                //if (TryConvertToCardType(entry.PieceType, out CardType cardType))
                //{
                //    if (!pieceDataByCardType.ContainsKey(cardType))
                //    {
                //        pieceDataByCardType[cardType] = new Dictionary<Religion, PieceDataSO>();
                //    }
                //    pieceDataByCardType[cardType][entry.Religion] = entry.PieceDataSO;
                //}

                // 验证 prefab 是否正确设置
                if (entry.PieceDataSO.piecePrefab == null)
                {
                    Debug.LogError($"[UnitListTable] PieceDataSO '{entry.PieceDataSO.pieceName}' 缺少 piecePrefab 引用！");
                    errorCount++;
                }
                else
                {
                    validCount++;
                }
            }
            else
            {
                Debug.LogError($"[UnitListTable] Inspector's pieceDataEntry Setting Error! PieceType={entry.PieceType}, Religion={entry.Religion}");
                errorCount++;
            }
        }

        Debug.LogWarning($"[UnitListTable] 初始化完成 - 有效条目: {validCount}, 错误: {errorCount}");
    }

    //    // ==========================================
    //    // 原有功能：通过 PieceDetail 查找
    //    // ==========================================

    public PieceDataSO GetPieceDataSO(PieceDetail pD)
    {
        if (pieceDataDict.TryGetValue(pD, out PieceDataSO pdSO))
        {
            return pdSO;
        }
        else
        {
            Debug.LogError($"[UnitListTable] 找不到 PieceType={pD.PieceType}, Religion={pD.Religion} 的数据！");
            return null;
        }
    }

    //    // ==========================================
    //    // 新增功能：通过资源路径查找（用于网络同步）
    //    // ==========================================

    //    /// <summary>
    //    /// 通过资源路径获取 PieceDataSO（用于网络同步）
    //    /// </summary>
    //    public PieceDataSO GetPieceDataByPath(string resourcePath)
    //    {
    //        if (string.IsNullOrEmpty(resourcePath))
    //        {
    //            Debug.LogWarning("[UnitListTable] 资源路径为空！");
    //            return null;
    //        }

    //        if (pieceDataByPath.TryGetValue(resourcePath, out PieceDataSO data))
    //        {
    //            return data;
    //        }

    //        Debug.LogWarning($"[UnitListTable] 找不到资源路径 '{resourcePath}' 对应的 PieceData！");
    //        return null;
    //    }

    //    // ==========================================
    //    // 新增功能：通过 CardType 查找
    //    // ==========================================

    //    /// <summary>
    //    /// 通过 CardType 和 Religion 获取 PieceDataSO
    //    /// </summary>
    //    public PieceDataSO GetPieceDataByCardType(CardType cardType, Religion religion = Religion.None)
    //    {
    //        if (!pieceDataByCardType.ContainsKey(cardType))
    //        {
    //            Debug.LogWarning($"[UnitListTable] 找不到 CardType={cardType} 的数据！");
    //            return null;
    //        }

    //        // 如果指定了 Religion，优先使用
    //        if (religion != Religion.None && pieceDataByCardType[cardType].ContainsKey(religion))
    //        {
    //            return pieceDataByCardType[cardType][religion];
    //        }

    //        // 否则返回第一个可用的
    //        foreach (var data in pieceDataByCardType[cardType].Values)
    //        {
    //            return data;
    //        }

    //        Debug.LogWarning($"[UnitListTable] CardType={cardType} 没有可用的数据！");
    //        return null;
    //    }

    //    // ==========================================
    //    // 新增功能：Prefab 管理
    //    // ==========================================

    //    /// <summary>
    //    /// 获取单位的 Prefab（带缓存）
    //    /// </summary>
    //    public GameObject GetPiecePrefab(PieceDetail pD)
    //    {
    //        PieceDataSO data = GetPieceDataSO(pD);
    //        if (data == null) return null;

    //        return data.piecePrefab;
    //    }

    //    /// <summary>
    //    /// 通过资源路径获取 Prefab
    //    /// </summary>
    //    public GameObject GetPrefabByPath(string resourcePath)
    //    {
    //        // 检查缓存
    //        if (prefabCache.TryGetValue(resourcePath, out GameObject cachedPrefab))
    //        {
    //            return cachedPrefab;
    //        }

    //        // 先尝试从 PieceDataSO 获取
    //        PieceDataSO data = GetPieceDataByPath(resourcePath);
    //        if (data != null && data.piecePrefab != null)
    //        {
    //            prefabCache[resourcePath] = data.piecePrefab;
    //            return data.piecePrefab;
    //        }

    //        // 如果失败，尝试从 Resources 加载
    //        GameObject prefab = Resources.Load<GameObject>(resourcePath);
    //        if (prefab != null)
    //        {
    //            prefabCache[resourcePath] = prefab;
    //            Debug.Log($"[UnitListTable] 从 Resources 加载 Prefab: {resourcePath}");
    //            return prefab;
    //        }

    //        Debug.LogError($"[UnitListTable] 无法加载 Prefab: {resourcePath}");
    //        return null;
    //    }

    //    /// <summary>
    //    /// 实例化单位 Prefab
    //    /// </summary>
    //    public GameObject InstantiatePiece(PieceDetail pD, Vector3 position, Quaternion rotation)
    //    {
    //        GameObject prefab = GetPiecePrefab(pD);
    //        if (prefab == null)
    //        {
    //            Debug.LogError($"[UnitListTable] 无法实例化单位 - Prefab 为 null");
    //            return null;
    //        }

    //        return Instantiate(prefab, position, rotation);
    //    }

    //    // ==========================================
    //    // 新增功能：网络同步支持
    //    // ==========================================

    //    /// <summary>
    //    /// 从网络消息创建单位（用于 HandleNetworkAddUnit）
    //    /// </summary>
    //    public PieceDataSO GetPieceDataForNetworkSync(string resourcePath, CardType cardType, Religion religion)
    //    {
    //        // 方法1: 通过资源路径查找（最推荐）
    //        PieceDataSO data = GetPieceDataByPath(resourcePath);
    //        if (data != null)
    //        {
    //            Debug.Log($"[UnitListTable] 通过路径找到: {data.pieceName}");
    //            return data;
    //        }

    //        // 方法2: 通过 CardType 和 Religion 查找
    //        data = GetPieceDataByCardType(cardType, religion);
    //        if (data != null)
    //        {
    //            Debug.Log($"[UnitListTable] 通过 CardType 找到: {data.pieceName}");
    //            return data;
    //        }

    //        // 方法3: 尝试从 Resources 加载
    //        data = Resources.Load<PieceDataSO>(resourcePath);
    //        if (data != null)
    //        {
    //            Debug.Log($"[UnitListTable] 从 Resources 加载: {data.pieceName}");
    //            return data;
    //        }

    //        Debug.LogError($"[UnitListTable] 网络同步失败 - 找不到单位数据: Path={resourcePath}, CardType={cardType}, Religion={religion}");
    //        return null;
    //    }

    //    // ==========================================
    //    // 辅助功能
    //    // ==========================================

    //    /// <summary>
    //    /// 将 PieceType 转换为 CardType（根据你的项目调整）
    //    /// </summary>
    //    private bool TryConvertToCardType(PieceType pieceType, out CardType cardType)
    //    {
    //        // 根据你的枚举定义进行映射
    //        // 这里提供一个示例实现
    //        switch (pieceType)
    //        {
    //            case PieceType.Farmer:
    //                cardType = CardType.Farmer;
    //                return true;
    //            case PieceType.Military:
    //                cardType = CardType.Solider;
    //                return true;
    //            case PieceType.Missionary:
    //                cardType = CardType.Missionary;
    //                return true;
    //            // 添加其他类型的映射
    //            default:
    //                cardType = default;
    //                return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 检查所有 PieceData 的配置是否正确
    //    /// </summary>
    //    public void ValidateAllPieceData()
    //    {
    //        Debug.Log("========== UnitListTable 验证 ==========");

    //        int totalCount = 0;
    //        int validPrefabCount = 0;
    //        int validPathCount = 0;
    //        int errorCount = 0;

    //        foreach (var entry in pieceDataEntries)
    //        {
    //            totalCount++;

    //            if (entry.PieceDataSO == null)
    //            {
    //                Debug.LogError($"[验证] Entry [{totalCount}] PieceDataSO 为 null!");
    //                errorCount++;
    //                continue;
    //            }

    //            string info = $"[验证] {entry.PieceType} ({entry.Religion}) - {entry.PieceDataSO.pieceName}";

    //            // 检查 Prefab
    //            if (entry.PieceDataSO.piecePrefab != null)
    //            {
    //                validPrefabCount++;
    //                info += " ✓ Prefab";
    //            }
    //            else
    //            {
    //                info += " ✗ Prefab 缺失";
    //                errorCount++;
    //            }

    //            // 检查资源路径
    //            if (!string.IsNullOrEmpty(entry.PieceDataSO.piecePrefabResourcePath))
    //            {
    //                validPathCount++;
    //                info += $" ✓ Path: {entry.PieceDataSO.piecePrefabResourcePath}";
    //            }
    //            else
    //            {
    //                info += " ✗ 资源路径缺失";
    //                errorCount++;
    //            }

    //            Debug.Log(info);
    //        }

    //        Debug.Log($"========== 验证完成 ==========");
    //        Debug.Log($"总计: {totalCount} | 有效Prefab: {validPrefabCount} | 有效路径: {validPathCount} | 错误: {errorCount}");
    //    }

    //    /// <summary>
    //    /// 获取所有已注册的 PieceDataSO
    //    /// </summary>
    //    public List<PieceDataSO> GetAllPieceData()
    //    {
    //        List<PieceDataSO> allData = new List<PieceDataSO>();
    //        foreach (var entry in pieceDataEntries)
    //        {
    //            if (entry.PieceDataSO != null)
    //            {
    //                allData.Add(entry.PieceDataSO);
    //            }
    //        }
    //        return allData;
    //    }

    //    /// <summary>
    //    /// 获取指定 Religion 的所有单位数据
    //    /// </summary>
    //    public List<PieceDataSO> GetPieceDataByReligion(Religion religion)
    //    {
    //        List<PieceDataSO> result = new List<PieceDataSO>();

    //        foreach (var entry in pieceDataEntries)
    //        {
    //            if (entry.Religion == religion && entry.PieceDataSO != null)
    //            {
    //                result.Add(entry.PieceDataSO);
    //            }
    //        }

    //        return result;
    //    }

    //#if UNITY_EDITOR
    //    /// <summary>
    //    /// 编辑器功能：自动设置所有 PieceDataSO 的资源路径
    //    /// </summary>
    //    [ContextMenu("自动设置所有资源路径")]
    //    private void AutoSetResourcePaths()
    //    {
    //        foreach (var entry in pieceDataEntries)
    //        {
    //            if (entry.PieceDataSO != null && entry.PieceDataSO.piecePrefab != null)
    //            {
    //                // 如果资源路径为空，尝试自动生成
    //                if (string.IsNullOrEmpty(entry.PieceDataSO.piecePrefabResourcePath))
    //                {
    //                    string prefabName = entry.PieceDataSO.piecePrefab.name;
    //                    string suggestedPath = $"Prefabs/Units/{prefabName}";

    //                    Debug.Log($"[自动设置] {entry.PieceDataSO.pieceName} → {suggestedPath}");

    //                    // 注意：这只是建议，实际需要手动在 Inspector 中设置
    //                    // 因为 ScriptableObject 的字段在运行时不能直接修改并保存
    //                }
    //            }
    //        }

    //        Debug.Log("[自动设置] 完成！请在 Inspector 中检查并手动设置资源路径。");
    //    }

    //    /// <summary>
    //    /// 编辑器功能：验证配置
    //    /// </summary>
    //    [ContextMenu("验证所有配置")]
    //    private void EditorValidate()
    //    {
    //        if (pieceDataDict == null || pieceDataDict.Count == 0)
    //        {
    //            InitializeDictionary();
    //        }
    //        ValidateAllPieceData();
    //    }
    //#endif
}