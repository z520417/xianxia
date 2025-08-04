using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 物品数据基类
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "仙侠游戏/物品数据", order = 1)]
    public class ItemData : ScriptableObject
    {
        #region 基础信息
        [Header("基础信息")]
        [SerializeField] protected string m_ItemName = "神秘物品";
        [SerializeField] protected string m_Description = "一件神秘的物品";
        [SerializeField] protected ItemType m_ItemType = ItemType.Material;
        [SerializeField] protected ItemRarity m_Rarity = ItemRarity.Common;
        [SerializeField] protected Sprite m_Icon;
        [SerializeField] protected int m_Value = 10;
        [SerializeField] protected int m_MaxStackSize = 99;
        #endregion

        #region 仙侠风格名称前缀
        private static readonly string[] s_RarityPrefixes = 
        {
            "",           // Common
            "灵光",        // Uncommon
            "玄品",        // Rare
            "地阶",        // Epic
            "天阶",        // Legendary
            "仙品"         // Mythic
        };

        private static readonly string[] s_RaritySuffixes = 
        {
            "",           // Common
            "之器",        // Uncommon
            "宝物",        // Rare
            "神兵",        // Epic
            "仙器",        // Legendary
            "至宝"         // Mythic
        };
        #endregion

        #region 公共属性
        public string ItemName => m_ItemName;
        public string Description => m_Description;
        public ItemType ItemType => m_ItemType;
        public ItemRarity Rarity => m_Rarity;
        public Sprite Icon => m_Icon;
        public int Value => m_Value;
        public int MaxStackSize => m_MaxStackSize;

        public string FullName => GetFullName();
        public Color RarityColor => GetRarityColor();
        #endregion

        #region 名称和颜色方法
        /// <summary>
        /// 获取完整名称（包含稀有度前缀）
        /// </summary>
        private string GetFullName()
        {
            string prefix = s_RarityPrefixes[(int)m_Rarity];
            string suffix = s_RaritySuffixes[(int)m_Rarity];
            
            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(suffix))
            {
                return $"{prefix}{m_ItemName}{suffix}";
            }
            else if (!string.IsNullOrEmpty(prefix))
            {
                return $"{prefix}{m_ItemName}";
            }
            else if (!string.IsNullOrEmpty(suffix))
            {
                return $"{m_ItemName}{suffix}";
            }
            
            return m_ItemName;
        }

        /// <summary>
        /// 获取稀有度对应的颜色
        /// </summary>
        public Color GetRarityColor()
        {
            switch (m_Rarity)
            {
                case ItemRarity.Common:
                    return Color.white;
                case ItemRarity.Uncommon:
                    return Color.green;
                case ItemRarity.Rare:
                    return Color.blue;
                case ItemRarity.Epic:
                    return new Color(0.6f, 0.3f, 1f); // 紫色
                case ItemRarity.Legendary:
                    return new Color(1f, 0.6f, 0f);   // 橙色
                case ItemRarity.Mythic:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
        #endregion

        #region 设置方法
        /// <summary>
        /// 设置物品基础信息（用于动态创建物品）
        /// </summary>
        public void SetItemInfo(string _name, string _description, ItemType _type, ItemRarity _rarity, int _value, int _maxStackSize)
        {
            m_ItemName = _name;
            m_Description = _description;
            m_ItemType = _type;
            m_Rarity = _rarity;
            m_Value = _value;
            m_MaxStackSize = _maxStackSize;
        }
        #endregion

        #region 虚拟方法
        /// <summary>
        /// 使用物品（虚拟方法，子类重写）
        /// </summary>
        public virtual bool UseItem(CharacterStats _characterStats)
        {
            Debug.Log($"使用了物品：{FullName}");
            return false;
        }

        /// <summary>
        /// 获取物品详细信息
        /// </summary>
        public virtual string GetDetailedInfo()
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(RarityColor)}>{FullName}</color>\n" +
                   $"类型：{GetItemTypeText()}\n" +
                   $"稀有度：{GetRarityText()}\n" +
                   $"价值：{m_Value} 灵石\n" +
                   $"最大堆叠：{m_MaxStackSize}\n\n" +
                   $"{m_Description}";
        }

        /// <summary>
        /// 获取物品类型文本
        /// </summary>
        private string GetItemTypeText()
        {
            switch (m_ItemType)
            {
                case ItemType.Equipment: return "装备";
                case ItemType.Consumable: return "消耗品";
                case ItemType.Material: return "材料";
                case ItemType.Treasure: return "珍宝";
                default: return "未知";
            }
        }

        /// <summary>
        /// 获取稀有度文本
        /// </summary>
        private string GetRarityText()
        {
            switch (m_Rarity)
            {
                case ItemRarity.Common: return "普通";
                case ItemRarity.Uncommon: return "不凡";
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Epic: return "史诗";
                case ItemRarity.Legendary: return "传说";
                case ItemRarity.Mythic: return "神话";
                default: return "未知";
            }
        }
        #endregion
    }
}