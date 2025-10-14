using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 单位种类
/// </summary>
public enum PlayerUnitType
{
    Soldier,    //士兵
    Farmer,     //农民
    // 后续添加
}


// 玩家单位的数据，种类与坐标一一对应
[Serializable]
public struct PlayerUnitData
{
    // 单位的种类
    public PlayerUnitType UnitType;
    // 单位的2维坐标
    public int2 Position;

    public PlayerUnitData(PlayerUnitType type, int2 pos)
    {
        UnitType = type;
        Position = pos;
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

    public PlayerData(int playerId)
    {
        PlayerID = playerId;
        PlayerUnits = new List<PlayerUnitData>();
        Resources = 0;
    }

    // 添加单位(单位与位置)
    public void AddUnit(PlayerUnitType type, int2 pos)
    {
        PlayerUnits.Add(new PlayerUnitData(type, pos));
        Debug.Log($"玩家 {PlayerID} 在 ({pos.x},{pos.y}) 添加了 {type}");
    }

    // 添加单位(完整数据)
    public void AddUnit(PlayerUnitData unitData)
    {
        PlayerUnits.Add(unitData);
        Debug.Log($"玩家 {PlayerID} 添加单位: {unitData.UnitType} at ({unitData.Position.x},{unitData.Position.y})");
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

                //Debug.Log($"玩家 {PlayerID} 移动单位: ({startPos.x},{startPos.y}) -> ({endPos.x},{endPos.y})");
                return true;
            }
        }
        return false; // 没找到要移动的单位
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



    // 查找单位
    public PlayerUnitData? FindUnitAt(int2 position)
    {
        foreach (var unit in PlayerUnits)
        {
            if (unit.Position.Equals(position))
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
        Debug.Log("PlayerDataManager: 清空所有玩家数据");
    }

    // 获取玩家数量
    public int GetPlayerCount()
    {
        return allPlayersData.Count;
    }

    // *************************
    //        单位管理
    // *************************

    // 添加单位(种类与位置)
    public bool AddUnit(int playerId, PlayerUnitType type, int2 pos)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            data.AddUnit(type, pos);
            allPlayersData[playerId] = data;

            // 触发事件
            OnUnitAdded?.Invoke(playerId, new PlayerUnitData(type, pos));
            OnPlayerDataChanged?.Invoke(playerId, data);

            return true;
        }
        return false;
    }

    // 添加单位(完整数据)
    public bool AddUnit(int playerId, PlayerUnitData unitData)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            data.AddUnit(unitData);
            allPlayersData[playerId] = data;

            OnUnitAdded?.Invoke(playerId, unitData);
            OnPlayerDataChanged?.Invoke(playerId, data);

            return true;
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

    // 删除单位
    public bool RemoveUnit(int playerId, int2 pos)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            bool success = data.RemoveUnitAt(pos);

            if (success)
            {
                allPlayersData[playerId] = data;

                // 触发事件
                OnUnitRemoved?.Invoke(playerId, pos);
                OnPlayerDataChanged?.Invoke(playerId, data);
            }

            return success;
        }
        return false;
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
                Debug.Log($"  - {unit.UnitType} at ({unit.Position.x},{unit.Position.y}) ");
            }
        }
    }

    // 查找单位
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