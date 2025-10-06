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


/// <summary>
/// 玩家单位的数据，种类与坐标一一对应
/// </summary>
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


/// <summary>
/// 玩家数据
/// </summary>
[Serializable]
public struct PlayerData
{
    // 玩家的序号
    public int PlayerID;

    // 玩家拥有的单位
    public List<PlayerUnitData> PlayerUnits;

    public PlayerData(int playerId)
    {
        PlayerID = playerId;
        PlayerUnits = new List<PlayerUnitData>();
    }

    // 添加单位
    public void AddUnit(PlayerUnitType type, int2 pos)
    {
        PlayerUnits.Add(new PlayerUnitData(type, pos));
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
}

public class PlayerDataManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
