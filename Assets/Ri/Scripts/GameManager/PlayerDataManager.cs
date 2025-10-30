using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using GameData;
using UnityEngine.UIElements;
using TMPro;


// 玩家单位的数据
[Serializable]
public struct PlayerUnitData
{
    // 单位的唯一ID
    public int UnitID;

    // 单位的种类
    public CardType UnitType;

    // 单位的2维坐标
    public int2 Position;

    // 玩家拥有的单位GameObject
    public GameObject PlayerUnitObject;

    // 单位的数据
    public PieceDataSO PlayerUnitDataSO;

    // 该单位是否已经上场
    public bool bUnitIsUsed;

    public PlayerUnitData(int unitId, CardType type, int2 pos, GameObject unitObject = null, PieceDataSO unitData = null,bool isUsed=false)
    {
        UnitID = unitId;
        UnitType = type;
        Position = pos;
        PlayerUnitObject = unitObject;
        PlayerUnitDataSO = unitData;
        bUnitIsUsed = isUsed;
    }

}


// 玩家数据
[Serializable]
public struct PlayerData
{
    // 玩家的序号
    public int PlayerID;

    // 玩家拥有的单位
    public List<PlayerUnitData> PlayerUnits;

    // 玩家资源 
    public int Resources;

    // 玩家宗教
    public Religion PlayerReligion;

    public PlayerData(int playerId)
    {
        PlayerID = playerId;
        PlayerUnits = new List<PlayerUnitData>();
        Resources = 0;
        PlayerReligion = Religion.None;
    }

    // 添加单位，此为单位加入手牌(单位与位置)
    public void AddUnit(int unitId, CardType type, int2 pos, GameObject unitObject,PieceDataSO unitData)
    {
        PlayerUnits.Add(new PlayerUnitData(unitId, type, pos, unitObject, unitData));
        Debug.Log($"玩家 {PlayerID} 在 ({pos.x},{pos.y}) 添加了 {type}，ID: {unitId}");
    }

 
    // 更新单位的GameObject引用
    public bool UpdateUnitGameObject(int unitId, GameObject unitObject)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].UnitID == unitId)
            {
                PlayerUnitData updatedUnit = PlayerUnits[i];
                updatedUnit.PlayerUnitObject = unitObject;
                PlayerUnits[i] = updatedUnit;
                return true;
            }
        }
        return false;
    }

    // 移动单位
    public bool MoveUnit(int2 startPos, int2 endPos)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].Position.Equals(startPos))
            {
                PlayerUnitData movedUnit = PlayerUnits[i];
                movedUnit.Position = endPos;
                PlayerUnits[i] = movedUnit;
                return true;
            }
        }
        return false;
    }

    // 根据坐标删除单位
    public bool RemoveUnitAt(int2 position)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].Position.x == position.x && PlayerUnits[i].Position.y == position.y)
            {
                PlayerUnits.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    // 根据ID删除单位
    public bool RemoveUnitById(int unitId)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].UnitID == unitId)
            {
                PlayerUnits.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    // 根据坐标查找单位
    public PlayerUnitData? FindUnitAt(int2 position)
    {
        foreach (var unit in PlayerUnits)
        {
            if (unit.Position.Equals(position))
                return unit;
        }
        return null;
    }

    // 根据ID查找单位
    public PlayerUnitData? FindUnitById(int unitId)
    {
        foreach (var unit in PlayerUnits)
        {
            if (unit.UnitID == unitId)
                return unit;
        }
        return null;
    }

    // 获取所有单位的位置
    public List<int2> GetAllUnitPositions()
    {
        List<int2> positions = new List<int2>();
        foreach (var unit in PlayerUnits)
        {
            positions.Add(unit.Position);
        }
        return positions;
    }

    // 获取单位数量
    public int GetUnitCount()
    {
        return PlayerUnits.Count;
    }

}

public class PlayerDataManager : MonoBehaviour
{
    // 单例
    public static PlayerDataManager Instance { get; private set; }

    // 所有玩家数据
    private Dictionary<int, PlayerData> allPlayersData = new Dictionary<int, PlayerData>();

    // 单位ID计数器（用于生成唯一ID）
    private int unitIdCounter = 0;

    // 通过UnitID快速查找的字典 <UnitID, PlayerID>
    private Dictionary<int, int> unitIdToPlayerIdMap = new Dictionary<int, int>();

    // 当前选择中的单位id
    public int nowChooseUnitID;

    // 事件: 玩家数据变化
    public event Action<int, PlayerData> OnPlayerDataChanged;

    // 事件: 单位添加
    public event Action<int, PlayerUnitData> OnUnitAdded;

    // 事件: 单位移除
    public event Action<int, int2> OnUnitRemoved;

    // 事件: 单位移动
    public event Action<int, int2, int2> OnUnitMoved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // *************************
    //        ID管理
    // *************************

    // 生成新的单位ID
    private int GenerateUnitID()
    {
        return unitIdCounter++;
    }

    // 重置ID计数器（用于新游戏开始时）
    public void ResetUnitIDCounter()
    {
        unitIdCounter = 0;
        unitIdToPlayerIdMap.Clear();
        Debug.Log("PlayerDataManager: ID计数器已重置");
    }

    // *************************
    //        玩家管理
    // *************************

    // 创建玩家
    public void CreatePlayer(int playerId)
    {
        if (!allPlayersData.ContainsKey(playerId))
        {
            allPlayersData[playerId] = new PlayerData(playerId);
            Debug.Log($"PlayerDataManager: 创建玩家 {playerId}");
        }
        else
        {
            Debug.LogWarning($"PlayerDataManager: 玩家 {playerId} 已存在");
        }
    }

    // 获取玩家数据
    public PlayerData GetPlayerData(int playerId)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            return allPlayersData[playerId];
        }

        Debug.LogWarning($"PlayerDataManager: 找不到玩家 {playerId}");
        return default;
    }

    // 更新玩家数据
    public void UpdatePlayerData(int playerId, PlayerData data)
    {
        allPlayersData[playerId] = data;
        OnPlayerDataChanged?.Invoke(playerId, data);

        Debug.Log($"PlayerDataManager: 更新玩家 {playerId} 数据");
    }

    // 移除玩家
    public void RemovePlayer(int playerId)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            // 清理该玩家所有单位的ID映射
            PlayerData data = allPlayersData[playerId];
            foreach (var unit in data.PlayerUnits)
            {
                unitIdToPlayerIdMap.Remove(unit.UnitID);
            }

            allPlayersData.Remove(playerId);
            Debug.Log($"PlayerDataManager: 移除玩家 {playerId}");
        }
    }

    // 获取所有玩家数据
    public Dictionary<int, PlayerData> GetAllPlayersData()
    {
        return new Dictionary<int, PlayerData>(allPlayersData);
    }

    // 清空所有玩家
    public void ClearAllPlayers()
    {
        allPlayersData.Clear();
        unitIdToPlayerIdMap.Clear();
        Debug.Log("PlayerDataManager: 清空所有玩家数据");
    }

    // 获取玩家数量
    public int GetPlayerCount()
    {
        return allPlayersData.Count;
    }

    // 返回当前玩家资源
    public int GetPlayerResource()
    {
        return allPlayersData[GameManage.Instance.LocalPlayerID].Resources;
    }
    // *************************
    //        单位管理
    // *************************

    // 将一个单位上场
    public void AddUnitToDeck(int playerID,int unitID)
    {

    }


    /// <summary>
    /// 得到已激活或未激活的单位数量
    /// </summary>
    /// <param name="activated">true:已激活 false:未激活</param>
    /// <returns></returns>
    public int GetActivateUnitCount(bool activated)
    {
        int allCount=allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits.Count;
        int count=0;

        foreach(var a in allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits)
        {
            if (a.bUnitIsUsed)
                count++;
        }
        if (activated)
            return count;
        else
            return allCount - count;
    }

    /// <summary>
    /// 拿到某一类型单位已上场的单位keyList
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<int> GetActivateUnitKey(CardType type)
    {
        List<int> list_unitID = new List<int>();
        foreach (var a in allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits)
        {
            if (a.UnitType==type&&a.bUnitIsUsed)
                list_unitID.Add(a.UnitID);
        }

        return list_unitID;
    }

    // 拿到特定类型单位未上场的数量
    public int GetUnActivateUnitCount(CardType type)
    {
        int count = 0;
        foreach (var a in allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits)
        {
            if (a.UnitType == type && !a.bUnitIsUsed)
                count++;
        }

        return count;
    }

    // 通过位置获取一个单位的id
    public int GetUnitIDBy2DPos(int2 pos)
    {
        PlayerUnitData? foundUnit = allPlayersData[GameManage.Instance.LocalPlayerID].FindUnitAt(pos);
        if (foundUnit != null)
        {
            return foundUnit.Value.UnitID;
        }
        else
            return 0;

    }
    // 添加单位(种类与位置) - 返回生成的UnitID
    public int AddUnit(int playerId, CardType type, int2 pos, GameObject unitObject=null,PieceDataSO unitData = null, bool isUsed = false)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            int newUnitId = GenerateUnitID();

            PlayerData data = allPlayersData[playerId];
            data.AddUnit(newUnitId, type, pos, unitObject, unitData);
            allPlayersData[playerId] = data;

            

            // 添加ID映射
            unitIdToPlayerIdMap[newUnitId] = playerId;

            // 触发事件
            OnUnitAdded?.Invoke(playerId, new PlayerUnitData(newUnitId, type, pos, unitObject, unitData));
            OnPlayerDataChanged?.Invoke(playerId, data);

            return newUnitId;
        }
        return -1; // 失败返回-1
    }

    // 添加单位(完整数据) - 如果UnitData中的ID为0，则生成新ID
    //public int AddUnit(int playerId, PlayerUnitData unitData)
    //{
    //    if (allPlayersData.ContainsKey(playerId))
    //    {
    //        // 如果没有ID，生成新的
    //        if (unitData.UnitID == 0)
    //        {
    //            unitData.UnitID = GenerateUnitID();
    //        }

    //        PlayerData data = allPlayersData[playerId];
    //        data.AddUnit(unitData);
    //        allPlayersData[playerId] = data;

    //        // 添加ID映射
    //        unitIdToPlayerIdMap[unitData.UnitID] = playerId;

    //        OnUnitAdded?.Invoke(playerId, unitData);
    //        OnPlayerDataChanged?.Invoke(playerId, data);

    //        return unitData.UnitID;
    //    }
    //    return -1;
    //}

    // 更新单位的GameObject引用
    public bool UpdateUnitGameObject(int playerId, int unitId, GameObject unitObject)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            bool success = data.UpdateUnitGameObject(unitId, unitObject);

            if (success)
            {
                allPlayersData[playerId] = data;
                OnPlayerDataChanged?.Invoke(playerId, data);
            }

            return success;
        }
        return false;
    }

    // 移动单位
    public bool MoveUnit(int playerId, int2 fromPos, int2 toPos)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            bool success = data.MoveUnit(fromPos, toPos);

            if (success)
            {
                allPlayersData[playerId] = data;

                // 触发事件
                OnUnitMoved?.Invoke(playerId, fromPos, toPos);
                OnPlayerDataChanged?.Invoke(playerId, data);
            }

            return success;
        }
        return false;
    }

    // 删除单位（按位置）
    public bool RemoveUnit(int playerId, int2 pos)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];

            // 先找到单位ID，以便清理映射
            PlayerUnitData? unitData = data.FindUnitAt(pos);

            bool success = data.RemoveUnitAt(pos);

            if (success)
            {
                // 清理ID映射
                if (unitData.HasValue)
                {
                    unitIdToPlayerIdMap.Remove(unitData.Value.UnitID);
                }

                allPlayersData[playerId] = data;

                // 触发事件
                OnUnitRemoved?.Invoke(playerId, pos);
                OnPlayerDataChanged?.Invoke(playerId, data);
            }

            return success;
        }
        return false;
    }

    // 删除单位（按ID）
    public bool RemoveUnitById(int unitId)
    {
        // 先找到单位所属的玩家
        if (!unitIdToPlayerIdMap.ContainsKey(unitId))
        {
            Debug.LogWarning($"PlayerDataManager: 找不到ID为 {unitId} 的单位");
            return false;
        }

        int playerId = unitIdToPlayerIdMap[unitId];

        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];

            // 获取单位位置（用于事件）
            PlayerUnitData? unitData = data.FindUnitById(unitId);

            bool success = data.RemoveUnitById(unitId);

            if (success)
            {
                // 清理ID映射
                unitIdToPlayerIdMap.Remove(unitId);
                allPlayersData[playerId] = data;

                // 触发事件
                if (unitData.HasValue)
                {
                    OnUnitRemoved?.Invoke(playerId, unitData.Value.Position);
                }
                OnPlayerDataChanged?.Invoke(playerId, data);
            }

            return success;
        }
        return false;
    }

    // *************************
    //        通过ID查询
    // *************************

    // 通过UnitID获取单位的GameObject
    public GameObject GetUnitGameObjectById(int unitId)
    {
        if (!unitIdToPlayerIdMap.ContainsKey(unitId))
        {
            Debug.LogWarning($"PlayerDataManager: 找不到ID为 {unitId} 的单位");
            return null;
        }

        int playerId = unitIdToPlayerIdMap[unitId];
        PlayerData data = GetPlayerData(playerId);

        PlayerUnitData? unitData = data.FindUnitById(unitId);

        if (unitData.HasValue)
        {
            return unitData.Value.PlayerUnitObject;
        }

        return null;
    }

    // 通过UnitID获取完整的单位数据
    public PlayerUnitData? GetUnitDataById(int unitId)
    {
        if (!unitIdToPlayerIdMap.ContainsKey(unitId))
        {
            Debug.LogWarning($"PlayerDataManager: 找不到ID为 {unitId} 的单位");
            return null;
        }

        int playerId = unitIdToPlayerIdMap[unitId];
        PlayerData data = GetPlayerData(playerId);

        return data.FindUnitById(unitId);
    }

    // 通过UnitID获取单位所属的玩家ID
    public int GetPlayerIdByUnitId(int unitId)
    {
        if (unitIdToPlayerIdMap.ContainsKey(unitId))
        {
            return unitIdToPlayerIdMap[unitId];
        }
        return -1;
    }

    // 检查UnitID是否存在
    public bool UnitIdExists(int unitId)
    {
        return unitIdToPlayerIdMap.ContainsKey(unitId);
    }

    // *************************
    //        数据统计
    // *************************

    // 获取某玩家的所有单位位置
    public List<int2> GetPlayerUnitPositions(int playerId)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            return allPlayersData[playerId].GetAllUnitPositions();
        }
        return new List<int2>();
    }

    // 获取某玩家的单位数量
    public int GetPlayerUnitCount(int playerId)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            return allPlayersData[playerId].GetUnitCount();
        }
        return 0;
    }

    // 获取所有玩家的单位总数
    public int GetTotalUnitCount()
    {
        int total = 0;
        foreach (var data in allPlayersData.Values)
        {
            total += data.GetUnitCount();
        }
        return total;
    }

    // 检查玩家是否还有单位(用于判断游戏结束)
    public bool PlayerHasUnits(int playerId)
    {
        return GetPlayerUnitCount(playerId) > 0;
    }

    // 获取存活的玩家列表
    public List<int> GetAlivePlayers()
    {
        List<int> alivePlayers = new List<int>();
        foreach (var kvp in allPlayersData)
        {
            if (kvp.Value.GetUnitCount() > 0)
            {
                alivePlayers.Add(kvp.Key);
            }
        }
        return alivePlayers;
    }

    // 获取某玩家的所有单位ID
    public List<int> GetPlayerUnitIds(int playerId)
    {
        List<int> unitIds = new List<int>();
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            foreach (var unit in data.PlayerUnits)
            {
                unitIds.Add(unit.UnitID);
            }
        }
        return unitIds;
    }

    // *************************
    //        调试功能
    // *************************

    // 打印所有玩家数据(调试用)
    public void DebugPrintAllData()
    {
        Debug.Log("=== PlayerDataManager 数据 ===");
        foreach (var kvp in allPlayersData)
        {
            Debug.Log($"玩家 {kvp.Key}: {kvp.Value.GetUnitCount()} 个单位");
            foreach (var unit in kvp.Value.PlayerUnits)
            {
                Debug.Log($"  - ID:{unit.UnitID} {unit.UnitType} at ({unit.Position.x},{unit.Position.y}) GameObject:{(unit.PlayerUnitObject != null ? unit.PlayerUnitObject.name : "null")}");
            }
        }

        Debug.Log($"=== 单位ID映射表 ({unitIdToPlayerIdMap.Count} 条) ===");
        foreach (var kvp in unitIdToPlayerIdMap)
        {
            Debug.Log($"  UnitID:{kvp.Key} -> PlayerID:{kvp.Value}");
        }
    }

    // 查找单位（按位置）
    public PlayerUnitData? FindUnit(int playerId, int2 pos)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            return allPlayersData[playerId].FindUnitAt(pos);
        }
        return null;
    }

    // 检查某个位置是否有任何玩家的单位
    public bool IsPositionOccupied(int2 pos)
    {
        foreach (var kvp in allPlayersData)
        {
            if (kvp.Value.FindUnitAt(pos) != null)
                return true;
        }
        return false;
    }

    // 获取某个位置的单位所属玩家ID
    public int GetUnitOwner(int2 pos)
    {
        foreach (var kvp in allPlayersData)
        {
            if (kvp.Value.FindUnitAt(pos) != null)
                return kvp.Key;
        }
        return -1; // 没有单位
    }
}