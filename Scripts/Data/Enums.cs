using System;

namespace XianXiaGame
{
    /// <summary>
    /// 物品稀有度等级
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,     // 普通 - 白色
        Uncommon = 1,   // 不凡 - 绿色
        Rare = 2,       // 稀有 - 蓝色
        Epic = 3,       // 史诗 - 紫色
        Legendary = 4,  // 传说 - 橙色
        Mythic = 5      // 神话 - 红色
    }

    /// <summary>
    /// 装备类型
    /// </summary>
    public enum EquipmentType
    {
        Weapon = 0,     // 武器
        Helmet = 1,     // 头盔
        Armor = 2,      // 护甲
        Boots = 3,      // 靴子
        Ring = 4,       // 戒指
        Necklace = 5    // 项链
    }

    /// <summary>
    /// 物品类型
    /// </summary>
    public enum ItemType
    {
        Equipment = 0,  // 装备
        Consumable = 1, // 消耗品
        Material = 2,   // 材料
        Treasure = 3    // 珍宝
    }

    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum BattleResult
    {
        Victory = 0,    // 胜利
        Defeat = 1,     // 失败
        Escape = 2      // 逃跑
    }
}