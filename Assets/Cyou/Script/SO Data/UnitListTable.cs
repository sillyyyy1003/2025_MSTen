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

    [Header("駒データ定義")]
    [SerializeField] private List<PieceDataEntry> pieceDataEntries = new List<PieceDataEntry>();

    private Dictionary<PieceDetail, PieceDataSO> pieceDataDict;
    

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        pieceDataDict=new Dictionary<PieceDetail, PieceDataSO> (pieceDataEntries.Count);//C#だとCount()ではなくCountでいい

        foreach(var entry in pieceDataEntries)
        {
            if (entry.PieceType != PieceType.None && entry.Religion != Religion.None&&entry.PieceDataSO!=null)
            {
                var key = new PieceDetail(entry.PieceType, entry.Religion);
                pieceDataDict[key] = entry.PieceDataSO;
            }
            else
            {
                Debug.LogError($"Inspector内でpieceDataEntryの定義が正しいか確認してください。");
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
            Debug.LogError($"{pD.PieceType},{pD.Religion}の駒のSOデータが見つかりません。");
            return null;
        }
    }

}
