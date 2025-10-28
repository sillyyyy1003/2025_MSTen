using GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnitListTable : MonoBehaviour
{

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

    private Dictionary<PieceDetail, PieceDataSO> pieceDataDict;
    

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        pieceDataDict=new Dictionary<PieceDetail, PieceDataSO> (pieceDataEntries.Count);//C#����Count()�ł͂Ȃ�Count�ł���

        foreach(var entry in pieceDataEntries)
        {
            if (entry.PieceType != PieceType.None && entry.Religion != Religion.None&&entry.PieceDataSO!=null)
            {
                var key = new PieceDetail(entry.PieceType, entry.Religion);
                pieceDataDict[key] = entry.PieceDataSO;
            }
            else
            {
                Debug.LogError($"Inspector's pieceDataEntry Setting Error!");
            }
        }
    }

    public PieceDataSO GetPieceDataSO(PieceDetail pD)
    {
        if(pieceDataDict.TryGetValue(pD,out PieceDataSO pdSO))
        {
            return pdSO;
        }
        else
        {
            Debug.LogError($"{pD.PieceType},{pD.Religion}�̋��SO�f�[�^��������܂���B");
            return null;
        }
    }

}
