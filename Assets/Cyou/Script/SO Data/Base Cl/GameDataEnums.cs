// ===== 列挙型定義 =====
// (so_data_definitions.cs より分離)

namespace GameData
{
    public enum ResourceGenerationType
    {
        None,
        Food,
        Wood,
        Stone,
        Iron,
        Gold
    }

    public enum DamageType
    {
        Physical,
        Magic,
        True
    }

    public enum SkillType
    {
        Attack,
        Buff,
        Debuff,
        Heal,
        Summon
    }


    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    public enum PieceType
    {
        None, Military, Farmer, Missionary, Pope,
        //25.11.11 RI add building enum
        Building
    }

    public enum Religion
    {
        None,
        SilkReligion,           // 絲織教
        RedMoonReligion,        // 紅月教
        MayaReligion,           // 瑪雅外星人文明教
        MadScientistReligion,   // 瘋狂科學家教
        E, F, G, H              // 将来の拡張用
    }

    public enum Terrain
    {
        Normal, Gold
    }
}