using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 装备数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "仙侠游戏/装备数据", order = 2)]
    public class EquipmentData : ItemData
    {
        #region 装备属性
        [Header("装备属性")]
        [SerializeField] protected EquipmentType m_EquipmentType = EquipmentType.Weapon;
        [SerializeField] protected int m_RequiredLevel = 1;
        [SerializeField] protected CharacterStats m_BonusStats = new CharacterStats();
        #endregion

        #region 装备名称数据
        private static readonly string[][] s_EquipmentNames = 
        {
            // 武器
            new string[] { "木剑", "铁剑", "钢剑", "寒光剑", "龙泉剑", "轩辕剑" },
            // 头盔
            new string[] { "布帽", "皮盔", "铁盔", "玄铁盔", "龙鳞盔", "凤羽冠" },
            // 护甲
            new string[] { "布衣", "皮甲", "锁甲", "玄铁甲", "龙鳞甲", "仙羽衣" },
            // 靴子
            new string[] { "草鞋", "皮靴", "铁靴", "疾风靴", "踏云靴", "凌波履" },
            // 戒指
            new string[] { "铜戒", "银戒", "金戒", "玉戒", "灵戒", "仙戒" },
            // 项链
            new string[] { "绳索", "银链", "金链", "玉佩", "灵符", "仙符" }
        };

        private static readonly string[][] s_EquipmentPrefixes = 
        {
            new string[] { "破旧的", "普通的", "精良的", "卓越的", "完美的", "传奇的" },
            new string[] { "凡品", "灵品", "宝品", "王品", "帝品", "仙品" },
            new string[] { "下品", "中品", "上品", "极品", "绝品", "神品" }
        };
        #endregion

        #region 公共属性
        public EquipmentType EquipmentType => m_EquipmentType;
        public int RequiredLevel => m_RequiredLevel;
        public CharacterStats BonusStats => m_BonusStats;
        #endregion

        #region 装备生成方法
        /// <summary>
        /// 生成随机装备
        /// </summary>
        public static EquipmentData GenerateRandomEquipment(EquipmentType _type, ItemRarity _rarity, int _level)
        {
            EquipmentData equipment = CreateInstance<EquipmentData>();
            
            // 设置基础属性
            equipment.m_EquipmentType = _type;
            equipment.m_ItemType = ItemType.Equipment;
            equipment.m_Rarity = _rarity;
            equipment.m_RequiredLevel = Mathf.Max(1, _level - Random.Range(0, 3));
            
            // 生成名称
            equipment.m_ItemName = GenerateEquipmentName(_type, _rarity);
            
            // 生成属性
            equipment.m_BonusStats = GenerateRandomStats(_rarity, _level);
            
            // 设置价值
            equipment.m_Value = CalculateEquipmentValue(_rarity, _level);
            
            // 装备不能堆叠
            equipment.m_MaxStackSize = 1;
            
            // 生成描述
            equipment.m_Description = GenerateEquipmentDescription(equipment);
            
            return equipment;
        }

        /// <summary>
        /// 生成装备名称
        /// </summary>
        private static string GenerateEquipmentName(EquipmentType _type, ItemRarity _rarity)
        {
            string[] typeNames = s_EquipmentNames[(int)_type];
            string baseName = typeNames[Mathf.Min((int)_rarity, typeNames.Length - 1)];
            
            if (_rarity >= ItemRarity.Rare && Random.Range(0f, 1f) < 0.5f)
            {
                string[] prefixes = s_EquipmentPrefixes[Random.Range(0, s_EquipmentPrefixes.Length)];
                string prefix = prefixes[Mathf.Min((int)_rarity, prefixes.Length - 1)];
                return baseName;
            }
            
            return baseName;
        }

        /// <summary>
        /// 生成随机属性
        /// </summary>
        private static CharacterStats GenerateRandomStats(ItemRarity _rarity, int _level)
        {
            CharacterStats stats = new CharacterStats();
            
            // 基础数值乘数（根据稀有度）
            float baseMultiplier = 1f + (int)_rarity * 0.5f;
            
            // 等级影响
            float levelMultiplier = 1f + _level * 0.1f;
            
            float totalMultiplier = baseMultiplier * levelMultiplier;
            
            // 随机分配属性点
            int totalPoints = Mathf.RoundToInt(Random.Range(8f, 15f) * totalMultiplier);
            
            // 分配属性
            int healthBonus = Random.Range(0, totalPoints / 4) * 5;
            totalPoints -= healthBonus / 5;
            
            int manaBonus = Random.Range(0, totalPoints / 3) * 3;
            totalPoints -= manaBonus / 3;
            
            int attackBonus = Random.Range(0, totalPoints / 2) * 2;
            totalPoints -= attackBonus / 2;
            
            int defenseBonus = Random.Range(0, totalPoints) * 1;
            totalPoints -= defenseBonus;
            
            int speedBonus = totalPoints;
            
            // 初始化特殊属性
            float criticalRateBonus = 0f;
            float criticalDamageBonus = 0f;
            int luckBonus = 0;
            
            // 稀有装备有概率获得特殊属性
            if (_rarity >= ItemRarity.Rare)
            {
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    criticalRateBonus = Random.Range(0.01f, 0.05f) * (int)_rarity;
                }
                
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    criticalDamageBonus = Random.Range(0.1f, 0.3f) * (int)_rarity;
                }
                
                if (Random.Range(0f, 1f) < 0.2f)
                {
                    luckBonus = Random.Range(1, 5) * (int)_rarity;
                }
            }
            
            // 使用SetStats方法设置属性
            stats.SetStats(healthBonus, manaBonus, attackBonus, defenseBonus, speedBonus, 
                           criticalRateBonus, criticalDamageBonus, luckBonus);
            
            return stats;
        }

        /// <summary>
        /// 计算装备价值
        /// </summary>
        private static int CalculateEquipmentValue(ItemRarity _rarity, int _level)
        {
            int baseValue = 50;
            int rarityMultiplier = 1 + (int)_rarity * 2;
            int levelMultiplier = 1 + _level;
            
            return baseValue * rarityMultiplier * levelMultiplier;
        }

        /// <summary>
        /// 生成装备描述
        /// </summary>
        private static string GenerateEquipmentDescription(EquipmentData _equipment)
        {
            string description = GetEquipmentFlavorText(_equipment.EquipmentType, _equipment.Rarity);
            
            // 添加一些仙侠风格的描述
            string[] additionalTexts = 
            {
                "蕴含着神秘的力量。",
                "散发着淡淡的灵气。",
                "似有若无的光芒流转。",
                "触之如有电流通过。",
                "仿佛有生命一般。"
            };
            
            if (_equipment.Rarity >= ItemRarity.Rare)
            {
                description += "\n" + additionalTexts[Random.Range(0, additionalTexts.Length)];
            }
            
            return description;
        }

        /// <summary>
        /// 获取装备风味文本
        /// </summary>
        private static string GetEquipmentFlavorText(EquipmentType _type, ItemRarity _rarity)
        {
            switch (_type)
            {
                case EquipmentType.Weapon:
                    return _rarity >= ItemRarity.Epic ? "传说中的神兵利器，削铁如泥。" : "一件还算锋利的武器。";
                case EquipmentType.Helmet:
                    return _rarity >= ItemRarity.Epic ? "古老的护头法宝，坚不可摧。" : "能够保护头部的护具。";
                case EquipmentType.Armor:
                    return _rarity >= ItemRarity.Epic ? "仙家炼制的护身宝甲，水火不侵。" : "普通的护身甲胄。";
                case EquipmentType.Boots:
                    return _rarity >= ItemRarity.Epic ? "神行千里的仙家宝履。" : "适合长途跋涉的靴子。";
                case EquipmentType.Ring:
                    return _rarity >= ItemRarity.Epic ? "蕴含强大法力的灵戒。" : "一枚普通的戒指。";
                case EquipmentType.Necklace:
                    return _rarity >= ItemRarity.Epic ? "护身避邪的仙家符佩。" : "装饰用的项链。";
                default:
                    return "一件神秘的装备。";
            }
        }
        #endregion

        #region 重写方法
        /// <summary>
        /// 获取装备详细信息
        /// </summary>
        public override string GetDetailedInfo()
        {
            string baseInfo = base.GetDetailedInfo();
            
            string equipmentInfo = $"\n装备类型：{GetEquipmentTypeText()}\n" +
                                 $"需求等级：{m_RequiredLevel}\n\n" +
                                 "装备属性：\n";
            
            if (m_BonusStats.MaxHealth > 0)
                equipmentInfo += $"生命值 +{m_BonusStats.MaxHealth}\n";
            if (m_BonusStats.MaxMana > 0)
                equipmentInfo += $"法力值 +{m_BonusStats.MaxMana}\n";
            if (m_BonusStats.Attack > 0)
                equipmentInfo += $"攻击力 +{m_BonusStats.Attack}\n";
            if (m_BonusStats.Defense > 0)
                equipmentInfo += $"防御力 +{m_BonusStats.Defense}\n";
            if (m_BonusStats.Speed > 0)
                equipmentInfo += $"速度 +{m_BonusStats.Speed}\n";
            if (m_BonusStats.CriticalRate > 0)
                equipmentInfo += $"暴击率 +{(m_BonusStats.CriticalRate * 100):F1}%\n";
            if (m_BonusStats.CriticalDamage > 0)
                equipmentInfo += $"暴击伤害 +{(m_BonusStats.CriticalDamage * 100):F1}%\n";
            if (m_BonusStats.Luck > 0)
                equipmentInfo += $"运气 +{m_BonusStats.Luck}\n";
            
            return baseInfo + equipmentInfo;
        }

        /// <summary>
        /// 获取装备类型文本
        /// </summary>
        private string GetEquipmentTypeText()
        {
            switch (m_EquipmentType)
            {
                case EquipmentType.Weapon: return "武器";
                case EquipmentType.Helmet: return "头盔";
                case EquipmentType.Armor: return "护甲";
                case EquipmentType.Boots: return "靴子";
                case EquipmentType.Ring: return "戒指";
                case EquipmentType.Necklace: return "项链";
                default: return "未知";
            }
        }
        #endregion
    }
}