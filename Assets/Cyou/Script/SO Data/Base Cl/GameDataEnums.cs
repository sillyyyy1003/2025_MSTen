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
        MirrorLakeReligion,     // 鏡湖教
        F, G, H                 // 将来の拡張用
    }


	/// <summary>
	/// マップの列挙
	/// </summary>
	public enum MAP_TYPE
	{
		MAP_A = 0,
		MAP_B = 1,
		MAP_C = 2,
		MAP_D = 3,
		MAP_E = 4,
		MAP_F = 5,
		MAP_G = 6,
		MAP_H = 7,
		MAP_I = 8,
		MAP_J = 9
    }

    // 駒の操作タイプ（APコスト取得用）
    public enum OperationType
	{
		Move,       // 移动
		Attack,     // 士兵攻击
		Cure,       // 农民治疗
		Work,       // 农民进入建筑
		Switch,     // 教皇交换位置
		Charm,      // 魅惑
		Occupy      // 占地
	}

    /// <summary>
    /// 駒の操作タイプ（APコスト取得用）
    /// </summary>
    //public enum OperationType
    //{
    //    Move,       // 移動（全駒共通）
    //    Attack,     // 攻撃（軍隊のみ）
    //    Occupy,     // 領地占領（宣教師のみ）
    //    Convert,    // 敵魅惑（宣教師のみ）
    //    Sacrifice   // 回復スキル（農民のみ）
    //}

    public enum Terrain
    {
        Normal, Gold
    }
}