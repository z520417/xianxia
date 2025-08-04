using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 属性修饰符类型
    /// </summary>
    public enum StatModifierType
    {
        Flat,           // 固定值加成
        Percentage,     // 百分比加成
        Multiplicative  // 乘法加成
    }

    /// <summary>
    /// 属性修饰符
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        [Header("属性修饰符")]
        public string StatName;                     // 属性名称
        public StatModifierType ModifierType;      // 修饰符类型
        public float MinValue;                      // 最小值
        public float MaxValue;                      // 最大值
        public AnimationCurve LevelScaling;        // 等级缩放曲线

        /// <summary>
        /// 计算指定等级的属性值
        /// </summary>
        public float CalculateValue(int level)
        {
            float baseValue = Random.Range(MinValue, MaxValue);
            
            if (LevelScaling != null && LevelScaling.keys.Length > 0)
            {
                float scaleFactor = LevelScaling.Evaluate(level);
                baseValue *= scaleFactor;
            }
            
            return baseValue;
        }
    }

    /// <summary>
    /// 物品模板基类
    /// </summary>
    public abstract class ItemTemplate : ScriptableObject
    {
        [Header("基本信息")]
        public string TemplateId;
        public string ItemName;
        [TextArea(3, 5)]
        public string Description;
        public Sprite Icon;
        public ItemType ItemType;
        
        [Header("稀有度配置")]
        public ItemRarity MinRarity = ItemRarity.Common;
        public ItemRarity MaxRarity = ItemRarity.Legendary;
        
        [Header("等级配置")]
        public int MinLevel = 1;
        public int MaxLevel = 50;
        
        [Header("价值配置")]
        public int BaseValue = 10;
        public AnimationCurve ValueScaling;
        
        [Header("堆叠配置")]
        public int MaxStackSize = 1;
        
        [Header("掉落配置")]
        [Range(0f, 1f)]
        public float DropChance = 0.1f;
        public List<string> DropSources = new List<string>(); // 掉落来源

        /// <summary>
        /// 创建物品实例
        /// </summary>
        public abstract ItemData CreateItem(ItemRarity rarity, int level);

        /// <summary>
        /// 计算物品价值
        /// </summary>
        protected virtual int CalculateValue(ItemRarity rarity, int level)
        {
            float baseValue = BaseValue;
            
            // 稀有度加成
            float rarityMultiplier = 1f + (int)rarity * 0.5f;
            
            // 等级缩放
            if (ValueScaling != null && ValueScaling.keys.Length > 0)
            {
                float levelMultiplier = ValueScaling.Evaluate(level);
                baseValue *= levelMultiplier;
            }
            else
            {
                baseValue *= (1f + level * 0.1f);
            }
            
            return Mathf.RoundToInt(baseValue * rarityMultiplier);
        }

        /// <summary>
        /// 获取本地化名称
        /// </summary>
        protected virtual string GetLocalizedName(ItemRarity rarity)
        {
            // 这里可以加入稀有度前缀/后缀
            string prefix = GetRarityPrefix(rarity);
            string suffix = GetRaritySuffix(rarity);
            
            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(suffix))
            {
                return $"{prefix}{ItemName}{suffix}";
            }
            else if (!string.IsNullOrEmpty(prefix))
            {
                return $"{prefix}{ItemName}";
            }
            else if (!string.IsNullOrEmpty(suffix))
            {
                return $"{ItemName}{suffix}";
            }
            
            return ItemName;
        }

        private string GetRarityPrefix(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return "灵光";
                case ItemRarity.Rare: return "玄品";
                case ItemRarity.Epic: return "地阶";
                case ItemRarity.Legendary: return "天阶";
                case ItemRarity.Mythic: return "仙品";
                default: return "";
            }
        }

        private string GetRaritySuffix(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return "之器";
                case ItemRarity.Rare: return "宝物";
                case ItemRarity.Epic: return "神兵";
                case ItemRarity.Legendary: return "仙器";
                case ItemRarity.Mythic: return "至宝";
                default: return "";
            }
        }
    }

    /// <summary>
    /// 装备模板
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment Template", menuName = "仙侠游戏/模板/装备模板")]
    public class EquipmentTemplate : ItemTemplate
    {
        [Header("装备特定配置")]
        public EquipmentType EquipmentType;
        
        [Header("属性修饰符")]
        public List<StatModifier> PrimaryStats = new List<StatModifier>();
        public List<StatModifier> SecondaryStats = new List<StatModifier>();
        
        [Header("特殊效果")]
        public List<string> PossibleEffects = new List<string>();

        public override ItemData CreateItem(ItemRarity rarity, int level)
        {
            var equipment = EquipmentData.GenerateRandomEquipment(EquipmentType, rarity, level);
            
            // 应用模板配置
            equipment.SetItemInfo(
                GetLocalizedName(rarity),
                Description,
                ItemType.Equipment,
                rarity,
                CalculateValue(rarity, level),
                MaxStackSize
            );

            // 应用属性修饰符
            ApplyStatModifiers(equipment, level);
            
            return equipment;
        }

        private void ApplyStatModifiers(EquipmentData equipment, int level)
        {
            var bonusStats = new CharacterStats();
            
            // 应用主要属性
            foreach (var modifier in PrimaryStats)
            {
                ApplyStatModifier(bonusStats, modifier, level, 1f);
            }
            
            // 应用次要属性（概率性）
            foreach (var modifier in SecondaryStats)
            {
                if (Random.Range(0f, 1f) < 0.5f) // 50%概率
                {
                    ApplyStatModifier(bonusStats, modifier, level, 0.5f);
                }
            }
            
            // 设置装备属性加成
            equipment.SetBonusStats(bonusStats);
        }

        private void ApplyStatModifier(CharacterStats stats, StatModifier modifier, int level, float effectiveness)
        {
            float value = modifier.CalculateValue(level) * effectiveness;
            
            switch (modifier.StatName.ToLower())
            {
                case "health":
                case "maxhealth":
                    stats.SetStats(
                        Mathf.RoundToInt(stats.MaxHealth + value),
                        stats.MaxMana,
                        stats.Attack,
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "mana":
                case "maxmana":
                    stats.SetStats(
                        stats.MaxHealth,
                        Mathf.RoundToInt(stats.MaxMana + value),
                        stats.Attack,
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "attack":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        Mathf.RoundToInt(stats.Attack + value),
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "defense":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        stats.Attack,
                        Mathf.RoundToInt(stats.Defense + value),
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "speed":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        stats.Attack,
                        stats.Defense,
                        Mathf.RoundToInt(stats.Speed + value),
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "luck":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        stats.Attack,
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage,
                        Mathf.RoundToInt(stats.Luck + value)
                    );
                    break;
                    
                case "criticalrate":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        stats.Attack,
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate + value * 0.01f, // 转换为百分比
                        stats.CriticalDamage,
                        stats.Luck
                    );
                    break;
                    
                case "criticaldamage":
                    stats.SetStats(
                        stats.MaxHealth,
                        stats.MaxMana,
                        stats.Attack,
                        stats.Defense,
                        stats.Speed,
                        stats.CriticalRate,
                        stats.CriticalDamage + value * 0.01f, // 转换为百分比
                        stats.Luck
                    );
                    break;
            }
        }
    }

    /// <summary>
    /// 消耗品模板
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable Template", menuName = "仙侠游戏/模板/消耗品模板")]
    public class ConsumableTemplate : ItemTemplate
    {
        [Header("消耗品特定配置")]
        public ConsumableType ConsumableType;
        
        [Header("效果配置")]
        public List<ConsumableEffect> Effects = new List<ConsumableEffect>();
        
        [Header("使用配置")]
        public float CooldownTime = 0f;
        public int MaxUses = 1; // -1 表示无限使用
        
        [Header("动画和音效")]
        public string UseAnimationClip;
        public string UseSoundEffect;

        public override ItemData CreateItem(ItemRarity rarity, int level)
        {
            var consumable = ConsumableData.GenerateRandomConsumable(rarity, level);
            
            // 应用模板配置
            consumable.SetItemInfo(
                GetLocalizedName(rarity),
                Description,
                ItemType.Consumable,
                rarity,
                CalculateValue(rarity, level),
                MaxStackSize
            );

            // 应用效果配置
            ApplyEffects(consumable, level);
            
            return consumable;
        }

        private void ApplyEffects(ConsumableData consumable, int level)
        {
            // 这里可以设置消耗品的具体效果
            // 由于原有的ConsumableData结构限制，这里只做示例
            foreach (var effect in Effects)
            {
                float effectValue = effect.CalculateValue(level);
                // 应用效果到消耗品
                // consumable.SetEffect(effect.EffectType, effectValue);
            }
        }
    }

    /// <summary>
    /// 消耗品类型
    /// </summary>
    public enum ConsumableType
    {
        HealthPotion,    // 生命药水
        ManaPotion,      // 法力药水
        BuffPotion,      // 增益药水
        DebuffPotion,    // 减益药水
        SpecialItem      // 特殊物品
    }

    /// <summary>
    /// 消耗品效果
    /// </summary>
    [System.Serializable]
    public class ConsumableEffect
    {
        [Header("效果配置")]
        public string EffectName;
        public EffectType EffectType;
        public float BaseValue;
        public float ValuePerLevel = 0f;
        public float Duration = 0f; // 0表示瞬间效果
        public AnimationCurve EffectCurve;

        public float CalculateValue(int level)
        {
            float value = BaseValue + ValuePerLevel * level;
            
            if (EffectCurve != null && EffectCurve.keys.Length > 0)
            {
                value *= EffectCurve.Evaluate(level);
            }
            
            return value;
        }
    }

    /// <summary>
    /// 效果类型
    /// </summary>
    public enum EffectType
    {
        InstantHeal,     // 瞬间治疗
        InstantMana,     // 瞬间法力恢复
        TemporaryBuff,   // 临时增益
        TemporaryDebuff, // 临时减益
        Poison,          // 中毒
        Regeneration     // 恢复
    }

    /// <summary>
    /// 材料模板
    /// </summary>
    [CreateAssetMenu(fileName = "New Material Template", menuName = "仙侠游戏/模板/材料模板")]
    public class MaterialTemplate : ItemTemplate
    {
        [Header("材料特定配置")]
        public MaterialType MaterialType;
        
        [Header("用途")]
        public List<string> UsedForCrafting = new List<string>();
        public List<string> UsedForUpgrading = new List<string>();
        
        [Header("获取方式")]
        public List<string> Sources = new List<string>();

        public override ItemData CreateItem(ItemRarity rarity, int level)
        {
            var material = ScriptableObject.CreateInstance<ItemData>();
            
            material.SetItemInfo(
                GetLocalizedName(rarity),
                Description,
                ItemType.Material,
                rarity,
                CalculateValue(rarity, level),
                MaxStackSize
            );
            
            return material;
        }
    }

    /// <summary>
    /// 材料类型
    /// </summary>
    public enum MaterialType
    {
        Ore,            // 矿石
        Herb,           // 草药
        Gem,            // 宝石
        Crystal,        // 水晶
        Essence,        // 精华
        Relic           // 遗物
    }

    /// <summary>
    /// 珍宝模板
    /// </summary>
    [CreateAssetMenu(fileName = "New Treasure Template", menuName = "仙侠游戏/模板/珍宝模板")]
    public class TreasureTemplate : ItemTemplate
    {
        [Header("珍宝特定配置")]
        public TreasureType TreasureType;
        
        [Header("收藏价值")]
        public int CollectionValue;
        public bool IsUnique = false;
        
        [Header("特殊属性")]
        public List<string> SpecialProperties = new List<string>();

        public override ItemData CreateItem(ItemRarity rarity, int level)
        {
            var treasure = ScriptableObject.CreateInstance<ItemData>();
            
            treasure.SetItemInfo(
                GetLocalizedName(rarity),
                Description,
                ItemType.Treasure,
                rarity,
                CalculateValue(rarity, level),
                IsUnique ? 1 : MaxStackSize
            );
            
            return treasure;
        }

        protected override int CalculateValue(ItemRarity rarity, int level)
        {
            // 珍宝的价值计算更复杂
            int baseValue = base.CalculateValue(rarity, level);
            baseValue += CollectionValue;
            
            if (IsUnique)
            {
                baseValue *= 3; // 独特物品价值更高
            }
            
            return baseValue;
        }
    }

    /// <summary>
    /// 珍宝类型
    /// </summary>
    public enum TreasureType
    {
        Coin,           // 古币
        Jewelry,        // 珠宝
        Artifact,       // 神器
        Scroll,         // 卷轴
        Talisman,       // 符咒
        Rune            // 符文
    }
}