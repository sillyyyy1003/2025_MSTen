// ===== ScriptableObject データ定義 =====
using UnityEngine;

namespace GameData
{
    /// <summary>
    /// スキルデータ定義
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "GameData/Skills/SkillData")]
    public class SkillDataSO : ScriptableObject
    {
        public string skillName;
        public string description;
        public float aPCost;
        public float coolDown;
        public float effectValue;
        public SkillType skillType;
        public Sprite skillIcon;
    }
}