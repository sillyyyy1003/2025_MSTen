using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using GameData;
using UnityEngine.UIElements;
using TMPro;
using System.Linq;
using System.Runtime.InteropServices;



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
    //public GameObject PlayerUnitObject;

    // 单位的数据
    public syncPieceData PlayerUnitDataSO;  
    
    // 建筑的数据（如果这是一个建筑单位）
    public syncBuildingData? BuildingData;  // nullable，只有建筑单位才有值


    // 该单位是否已经上场
    public bool bUnitIsActivated;

    // 是否能够行动
    public bool bCanDoAction;

    // 魅惑相关属性
    public bool bIsCharmed;              // 是否被魅惑
    public int charmedRemainingTurns;    // 魅惑剩余回合数（每个回合结束时-1）
    public int originalOwnerID;          // 原始所有者ID（用于归还控制权）


    public PlayerUnitData(int unitId, CardType type, int2 pos, syncPieceData unitData, bool isActivated = true, bool canDo = true, bool isCharmed = false, int charmedTurns = 0, int originalOwner = -1, syncBuildingData? buildingData = null)
    {
        UnitID = unitId;
        UnitType = type;
        Position = pos;
        PlayerUnitDataSO = unitData;
        bUnitIsActivated = isActivated;
        bCanDoAction = canDo;
        bIsCharmed = isCharmed;
        charmedRemainingTurns = charmedTurns;
        originalOwnerID = originalOwner;
        BuildingData = buildingData;
    }
    public void SetUnitDataSO(syncPieceData unitData)
    {
        Debug.Log("Set unitID is "+ PlayerUnitDataSO.pieceID+" HP is "+ PlayerUnitDataSO.currentHPLevel);
        PlayerUnitDataSO = unitData;
        Debug.Log("Set unitID is " + PlayerUnitDataSO.pieceID + " HP is " + PlayerUnitDataSO.currentHPLevel);
    }
    public void SetBuildingUnitDataSO(syncBuildingData unitData)
    {
        BuildingData = unitData;
    }
    public void SetCanDoAction(bool canDo)
    {
        bCanDoAction = canDo;
    }

    // 判断是否为建筑单位
    public bool IsBuilding()
    {
        return BuildingData.HasValue;
    }

    // 获取建筑数据
    public syncBuildingData? GetBuildingData()
    {
        return BuildingData;
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

    // 玩家占领的格子id
    public List<int> PlayerOwnedCells;

    

    public PlayerData(int playerId)
    {
        PlayerID = playerId;
        PlayerUnits = new List<PlayerUnitData>();
        Resources = 100;
        PlayerReligion = SceneStateManager.Instance.PlayerReligion;
        PlayerOwnedCells = new List<int>();
    }

    //
    public bool UpdateUnitSyncDataByPos(int2 position, syncPieceData newData)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].Position.Equals(position))
            {
                PlayerUnitData updatedUnit = PlayerUnits[i];
                updatedUnit.PlayerUnitDataSO = newData;
                PlayerUnits[i] = updatedUnit;
                return true;
            }
        }
        return false;
    }


    public void AddOwnedCell(int id)
    {
        PlayerOwnedCells.Add(id);
    }

    // 更新单位行动力
    public bool UpdateUnitCanDoActionByPos(int2 position, bool canDoAction)
    {
        for (int i = 0; i < PlayerUnits.Count; i++)
        {
            if (PlayerUnits[i].Position.Equals(position))
            {
                PlayerUnitData updatedUnit = PlayerUnits[i];
                updatedUnit.bCanDoAction = canDoAction;
                PlayerUnits[i] = updatedUnit;
                return true;
            }
        }
        return false;
    }


    // 添加单位，此为单位加入手牌(单位与位置)
    public void AddUnit(int unitId, CardType type, int2 pos, GameObject unitObject, syncPieceData unitData)
    {
        PlayerUnits.Add(new PlayerUnitData(unitId, type, pos, unitData));
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

    // 根据格子查找单位
    public PlayerUnitData? FindUnitAt(int cellID)
    {
        foreach (var unit in PlayerUnits)
        {
            if (unit.Position.Equals(GameManage.Instance.FindCell(cellID).Cells2DPos))
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
    public void SetReligion()
    {
        PlayerReligion = SceneStateManager.Instance.PlayerReligion;
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

    // 当前选择中的单位类型
    public CardType nowChooseUnitType;



    // 建筑
    [SerializeField] private BuildingRegistry buildingRegistry;

    // 事件: 玩家数据变化
    public event Action<int, PlayerData> OnPlayerDataChanged;

    // 事件: 单位添加
    public event Action<int, PlayerUnitData> OnUnitAdded;

    // 事件: 单位移除
    public event Action<int, int2,bool> OnUnitRemoved;

    // 事件: 单位移动
    public event Action<int, int2, int2> OnUnitMoved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
			//2025.11.17 Guoning
			//DontDestroyOnLoad(gameObject);
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
    public int GenerateUnitID()
    {
        int baseId = GameManage.Instance.LocalPlayerID * 10000 + unitIdCounter;
        Debug.Log("new generate unit ID is " + baseId);
        unitIdCounter++;
        return baseId;
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
            //allPlayersData[playerId].SetReligion();
            Debug.Log($"PlayerDataManager: 创建玩家 {playerId} 宗教{allPlayersData[playerId].PlayerReligion}");
        }
        else
        {
            Debug.LogWarning($"PlayerDataManager: 玩家 {playerId} 已存在");
        }
        PieceManager.Instance.SetPieceRligion(allPlayersData[playerId].PlayerReligion);
    
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

    /// <summary>
    /// 根据位置更新单位的同步数据
    /// 这个方法对于网络同步非常重要
    /// </summary>
    public bool UpdateUnitSyncDataByPos(int playerId, int2 pos, syncPieceData newData)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            bool success = data.UpdateUnitSyncDataByPos(pos, newData);

            if (success)
            {
                allPlayersData[playerId] = data;

                // 触发数据变更事件
                OnPlayerDataChanged?.Invoke(playerId, data);

                Debug.Log($"[PlayerDataManager] 玩家 {playerId} 位置 ({pos.x},{pos.y}) 的{newData.pieceID}同步数据已更新");
                return true;
            }
            else
            {
                Debug.LogWarning($"[PlayerDataManager] 找不到玩家 {playerId} 在位置 ({pos.x},{pos.y}) 的单位");
            }
        }
        else
        {
            Debug.LogError($"[PlayerDataManager] 找不到玩家 {playerId}");
        }

        return false;
    }


    // 更新单位行动力
    public bool UpdateUnitCanDoActionByPos(int playerId, int2 pos, bool canDoAction)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];
            bool success = data.UpdateUnitCanDoActionByPos(pos, canDoAction);

            if (success)
            {
                allPlayersData[playerId] = data;
                OnPlayerDataChanged?.Invoke(playerId, data);
                Debug.Log($"[PlayerDataManager] 单位 at ({pos.x},{pos.y}) 的 bCanDoAction 更新为: {canDoAction}");
                return true;
            }
        }
        return false;
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
    public void AddUnitToDeck(int playerID, int unitID)
    {

    }


    /// <summary>
    /// 得到已激活或未激活的单位数量
    /// </summary>
    /// <param name="activated">true:已激活 false:未激活</param>
    /// <returns></returns>
    public int GetActivateUnitCount(bool activated)
    {
        int allCount = allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits.Count;
        int count = 0;

        foreach (var a in allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits)
        {
            if (a.bUnitIsActivated)
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
            if (a.UnitType == type && a.bUnitIsActivated)
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
            if (a.UnitType == type && !a.bUnitIsActivated)
                count++;
        }

        return count;
    }

    // 拿到玩家尚未行动的单位数量
    public int GetUnitCanUse()
    {
        int count = 0;
        foreach (var a in allPlayersData[GameManage.Instance.LocalPlayerID].PlayerUnits)
        {
            if (a.bCanDoAction)
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

    // 得到当前选择的单位类型
    public CardType GetUnitTypeIDBy2DPos(int2 pos)
    {
        PlayerUnitData? foundUnit = allPlayersData[GameManage.Instance.LocalPlayerID].FindUnitAt(pos);
        if (foundUnit != null)
        {
            return foundUnit.Value.UnitType;
        }
        else
            return CardType.None;

    }

    // 获取一个单位的位置
    public Vector3 GetUnitPos(int unitID)
    {
        int2 pos = default;
        foreach (var a in allPlayersData)
        {
            if (a.Value.FindUnitById(unitID) != null)
            {
                pos = a.Value.FindUnitById(unitID).Value.Position;
            }
        }


        Debug.Log("unit ID is " + unitID +
            " unit 2Dpos is " + pos
            + " unit 3D pos is " + GameManage.Instance.GetCellObject(pos).transform.position);

        return GameManage.Instance.GetCellObject(pos).transform.position;
    }


    // 添加单位(种类与位置) - 返回生成的UnitID
    public int AddUnit(int playerId, CardType type, int2 pos, syncPieceData unitData, GameObject unitObject = null, bool bUnitIsActivated = true)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            //int newUnitId = GenerateUnitID();

            PlayerData data = allPlayersData[playerId];
            data.AddUnit(unitData.pieceID, type, pos, unitObject, unitData);
            allPlayersData[playerId] = data;



            // 添加ID映射
            unitIdToPlayerIdMap[unitData.pieceID] = playerId;

            // 触发事件
            OnUnitAdded?.Invoke(playerId, new PlayerUnitData(unitData.pieceID, type, pos, unitData));
            OnPlayerDataChanged?.Invoke(playerId, data);

            return unitData.pieceID;
        }
        return -1; // 失败返回-1
    }

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
                OnUnitRemoved?.Invoke(playerId, pos,false);
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
                    OnUnitRemoved?.Invoke(playerId, unitData.Value.Position,false);
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
        Debug.Log($"Player:ID为 {playerId}");
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

    // 通过UnitID获取单位所在的格子id
    public int GetCellIdByUnitId(int unitId)
    {
        // 先找到单位所属的玩家
        if (!unitIdToPlayerIdMap.ContainsKey(unitId))
        {
            Debug.LogWarning($"PlayerDataManager: 找不到ID为 {unitId} 的单位");
            return -1;
        }

        int playerId = unitIdToPlayerIdMap[unitId];

        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];

            // 获取单位位置（用于事件）
            PlayerUnitData? unitData = data.FindUnitById(unitId);

            
            return GameManage.Instance.GetCell2D(unitData.Value.Position).id;
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
                Debug.Log($"  - ID:{unit.UnitID} {unit.UnitType} at ({unit.Position.x},{unit.Position.y})");
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

    // 获取某个格子id返回所属玩家ID
    public int GetUnitOwner(int cellID)
    {
        foreach (var kvp in allPlayersData)
        {
            if (kvp.Value.FindUnitAt(cellID) != null)
                return kvp.Key;
        }
        return -1; // 没有单位
    }

    // 设置玩家资源
    public void SetPlayerResourses(int newResources)
    {
        int playerId = GameManage.Instance.LocalPlayerID;

        if (!allPlayersData.TryGetValue(playerId, out PlayerData data))
        {
            Debug.LogWarning($"UpdatePlayerResources: Player {playerId} not found!");
            return;
        }

        data.Resources = newResources;
        allPlayersData[playerId] = data;

        Debug.Log($"Player {playerId} Resources updated to {newResources}");

    }

    public int GetCreateUnitResoursesCost(CardType type)
    {
        switch (type)
        {
            case CardType.Missionary:
                return 3;
            case CardType.Farmer:
                return 2;
            case CardType.Solider:
                return 5;
            case CardType.Building:
                return 3;
            default:
                return 99;

        }

    }

    // *************************
    //     魅惑单位管理
    // *************************

    /// <summary>
    /// 设置单位为被魅惑状态
    /// </summary>
    public bool SetUnitCharmed(int playerId, int2 pos, int originalOwnerId, int turns = 3)
    {
        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];

            for (int i = 0; i < data.PlayerUnits.Count; i++)
            {
                if (data.PlayerUnits[i].Position.Equals(pos))
                {
                    PlayerUnitData updatedUnit = data.PlayerUnits[i];
                    updatedUnit.bIsCharmed = true;
                    updatedUnit.charmedRemainingTurns = turns;
                    updatedUnit.originalOwnerID = originalOwnerId;
                    data.PlayerUnits[i] = updatedUnit;

                    allPlayersData[playerId] = data;
                    OnPlayerDataChanged?.Invoke(playerId, data);

                    Debug.Log($"单位在 ({pos.x},{pos.y}) 被魅惑，原始所有者: {originalOwnerId}, 剩余回合: {turns}");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 转移单位所有权（用于魅惑功能）
    /// 直接在数据层面转移单位，不删除重建GameObject
    /// </summary>
    /// <param name="fromPlayerId">原所有者ID</param>
    /// <param name="toPlayerId">新所有者ID</param>
    /// <param name="pos">单位位置</param>
    /// <param name="updatedSyncData">更新后的同步数据（playerID已更新）</param>
    /// <param name="charmedTurns">魅惑持续回合数（0表示解除魅惑）</param>
    /// <returns>转移是否成功</returns>
    public bool TransferUnitOwnership(int fromPlayerId, int toPlayerId, int2 pos, syncPieceData updatedSyncData, int charmedTurns = 0)
    {
        Debug.Log($"[TransferUnitOwnership] 开始转移单位 - 从玩家{fromPlayerId}到玩家{toPlayerId} at ({pos.x},{pos.y})");

        // 1. 从原所有者获取单位数据
        if (!allPlayersData.ContainsKey(fromPlayerId))
        {
            Debug.LogError($"[TransferUnitOwnership] 找不到原所有者: {fromPlayerId}");
            return false;
        }

        PlayerData fromPlayerData = allPlayersData[fromPlayerId];
        PlayerUnitData? unitToTransfer = null;
        int unitIndex = -1;

        for (int i = 0; i < fromPlayerData.PlayerUnits.Count; i++)
        {
            if (fromPlayerData.PlayerUnits[i].Position.Equals(pos))
            {
                unitToTransfer = fromPlayerData.PlayerUnits[i];
                unitIndex = i;
                break;
            }
        }

        if (!unitToTransfer.HasValue)
        {
            Debug.LogError($"[TransferUnitOwnership] 在原所有者处未找到单位 at ({pos.x},{pos.y})");
            return false;
        }

        // 2. 从原所有者移除单位
        fromPlayerData.PlayerUnits.RemoveAt(unitIndex);
        allPlayersData[fromPlayerId] = fromPlayerData;

        // 移除unitId映射
        if (unitIdToPlayerIdMap.ContainsKey(unitToTransfer.Value.UnitID))
        {
            unitIdToPlayerIdMap.Remove(unitToTransfer.Value.UnitID);
        }

        // 触发移除事件
        OnUnitRemoved?.Invoke(fromPlayerId, pos,true);
        OnPlayerDataChanged?.Invoke(fromPlayerId, fromPlayerData);

        Debug.Log($"[TransferUnitOwnership] 已从玩家{fromPlayerId}移除单位");

        // 3. 准备转移后的单位数据
        PlayerUnitData transferredUnit = unitToTransfer.Value;

        // 更新同步数据
        transferredUnit.PlayerUnitDataSO = updatedSyncData;

        // 设置魅惑状态
        if (charmedTurns > 0)
        {
            // 被魅惑状态
            transferredUnit.bIsCharmed = true;
            transferredUnit.charmedRemainingTurns = charmedTurns;
            transferredUnit.originalOwnerID = fromPlayerId;
            Debug.Log($"[TransferUnitOwnership] 设置魅惑状态 - 剩余回合:{charmedTurns}, 原所有者:{fromPlayerId}");
        }
        else
        {
            // 解除魅惑（归还控制权）
            transferredUnit.bIsCharmed = false;
            transferredUnit.charmedRemainingTurns = 0;
            transferredUnit.originalOwnerID = -1;
            Debug.Log($"[TransferUnitOwnership] 解除魅惑状态");
        }

        // 4. 添加到新所有者
        if (!allPlayersData.ContainsKey(toPlayerId))
        {
            Debug.LogError($"[TransferUnitOwnership] 找不到新所有者: {toPlayerId}");
            return false;
        }

        PlayerData toPlayerData = allPlayersData[toPlayerId];
        toPlayerData.PlayerUnits.Add(transferredUnit);
        allPlayersData[toPlayerId] = toPlayerData;

        // 更新unitId映射
        unitIdToPlayerIdMap[transferredUnit.UnitID] = toPlayerId;

        // 触发添加事件
        OnUnitAdded?.Invoke(toPlayerId, transferredUnit);
        OnPlayerDataChanged?.Invoke(toPlayerId, toPlayerData);

        Debug.Log($"[TransferUnitOwnership] 单位已添加到玩家{toPlayerId}");
        Debug.Log($"[TransferUnitOwnership] 转移完成 - 单位ID:{transferredUnit.UnitID}, 类型:{transferredUnit.UnitType}");

        return true;
    }

    /// <summary>
    /// 批量转移单位所有权（用于一次性转移多个单位）
    /// </summary>
    public bool TransferMultipleUnits(int fromPlayerId, int toPlayerId, List<int2> positions, List<syncPieceData> updatedSyncDataList, int charmedTurns = 0)
    {
        if (positions.Count != updatedSyncDataList.Count)
        {
            Debug.LogError("[TransferMultipleUnits] 位置列表和同步数据列表长度不匹配");
            return false;
        }

        bool allSuccess = true;
        for (int i = 0; i < positions.Count; i++)
        {
            bool success = TransferUnitOwnership(fromPlayerId, toPlayerId, positions[i], updatedSyncDataList[i], charmedTurns);
            if (!success)
            {
                Debug.LogWarning($"[TransferMultipleUnits] 转移单位失败 at ({positions[i].x},{positions[i].y})");
                allSuccess = false;
            }
        }

        return allSuccess;
    }



    /// <summary>
    /// 减少被魅惑单位的剩余回合数，并检查是否需要归还控制权
    /// 在回合结束时调用
    /// </summary>
    public List<CharmExpireInfo> UpdateCharmedUnits(int playerId)
    {
        List<CharmExpireInfo> expiredUnits = new List<CharmExpireInfo>();

        if (allPlayersData.ContainsKey(playerId))
        {
            PlayerData data = allPlayersData[playerId];

            for (int i = data.PlayerUnits.Count - 1; i >= 0; i--)
            {
                if (data.PlayerUnits[i].bIsCharmed)
                {
                    PlayerUnitData unit = data.PlayerUnits[i];
                    unit.charmedRemainingTurns--;

                    if (unit.charmedRemainingTurns <= 0)
                    {
                        // 魅惑效果已过期，需要归还控制权
                        expiredUnits.Add(new CharmExpireInfo
                        {
                            UnitID = unit.UnitID,
                            Position = unit.Position,
                            OriginalOwnerID = unit.originalOwnerID,
                            UnitData = unit
                        });

                        // 从当前玩家移除
                        data.PlayerUnits.RemoveAt(i);
                    }
                    else
                    {
                        // 更新剩余回合数
                        data.PlayerUnits[i] = unit;
                    }
                }
            }

            allPlayersData[playerId] = data;
            OnPlayerDataChanged?.Invoke(playerId, data);
        }

        return expiredUnits;
    }

    /// <summary>
    /// 归还被魅惑单位的控制权给原始所有者
    /// </summary>
    public bool ReturnCharmedUnit(int originalOwnerId, PlayerUnitData unitData)
    {
        if (!allPlayersData.ContainsKey(originalOwnerId))
        {
            Debug.LogError($"找不到原始所有者: {originalOwnerId}");
            return false;
        }

        PlayerData data = allPlayersData[originalOwnerId];

        // 重置魅惑状态
        PlayerUnitData returnedUnit = unitData;
        returnedUnit.bIsCharmed = false;
        returnedUnit.charmedRemainingTurns = 0;
        returnedUnit.originalOwnerID = -1;

        // 更新同步数据中的playerID
        syncPieceData updatedSyncData = returnedUnit.PlayerUnitDataSO;
        updatedSyncData.currentPID = originalOwnerId;
        returnedUnit.PlayerUnitDataSO = updatedSyncData;

        // 添加回原始所有者
        data.PlayerUnits.Add(returnedUnit);
        allPlayersData[originalOwnerId] = data;

        // 更新ID映射
        unitIdToPlayerIdMap[returnedUnit.UnitID] = originalOwnerId;

        OnUnitAdded?.Invoke(originalOwnerId, returnedUnit);
        OnPlayerDataChanged?.Invoke(originalOwnerId, data);

        Debug.Log($"单位 {returnedUnit.UnitID} 在 ({returnedUnit.Position.x},{returnedUnit.Position.y}) 归还给原始所有者 {originalOwnerId}");
        return true;
    }

    /// <summary>
    /// 检查单位是否被魅惑
    /// </summary>
    public bool IsUnitCharmed(int playerId, int2 pos)
    {
        PlayerUnitData? unit = FindUnit(playerId, pos);
        return unit.HasValue && unit.Value.bIsCharmed;
    }

    /// <summary>
    /// 获取被魅惑单位的原始所有者
    /// </summary>
    public int GetCharmedUnitOriginalOwner(int playerId, int2 pos)
    {
        PlayerUnitData? unit = FindUnit(playerId, pos);
        if (unit.HasValue && unit.Value.bIsCharmed)
        {
            return unit.Value.originalOwnerID;
        }
        return -1;
    }
}

/// <summary>
/// 魅惑过期信息结构体
/// </summary>
public struct CharmExpireInfo
{
    public int UnitID;
    public int2 Position;
    public int OriginalOwnerID;
    public PlayerUnitData UnitData;
}

