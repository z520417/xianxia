using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 消耗品数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "仙侠游戏/消耗品数据", order = 3)]
    public class ConsumableData : ItemData
    {
        #region 消耗品属性
        [Header("消耗品效果")]
        [SerializeField] protected int m_HealthRestore = 0;      // 恢复生命值
        [SerializeField] protected int m_ManaRestore = 0;        // 恢复法力值
        [SerializeField] protected int m_ExperienceGain = 0;     // 获得经验值
        [SerializeField] protected float m_Duration = 0f;        // 持续时间（秒）
        [SerializeField] protected CharacterStats m_TempBonus = new CharacterStats(); // 临时属性加成
        #endregion

        #region 消耗品名称数据
        private static readonly string[][] s_ConsumableNames = 
        {
            // 恢复类
            new string[] { "馒头", "小还丹", "中还丹", "大还丹", "九转金丹", "太乙金丹" },
            // 法力类
            new string[] { "清水", "回气散", "回元丹", "紫气丹", "太虚丹", "无极丹" },
            // 经验类
            new string[] { "悟道茶", "启灵果", "悟性丹", "化神果", "升灵丹", "破境丹" },
            // 增益类
            new string[] { "兴奋剂", "力量散", "神力丹", "霸体丹", "金刚丹", "无敌丹" }
        };

        private static readonly string[] s_ConsumableDescriptions = 
        {
            "普通的食物，能够充饥。",
            "炼药师制作的基础丹药。",
            "蕴含丰富灵气的珍贵丹药。",
            "传说中的仙丹，效果卓著。",
            "仙家炼制的无上宝丹。",
            "传说中的神丹，可遇不可求。"
        };
        #endregion

        #region 公共属性
        public int HealthRestore => m_HealthRestore;
        public int ManaRestore => m_ManaRestore;
        public int ExperienceGain => m_ExperienceGain;
        public float Duration => m_Duration;
        public CharacterStats TempBonus => m_TempBonus;
        #endregion

        #region 消耗品生成方法
        /// <summary>
        /// 生成随机消耗品
        /// </summary>
        public static ConsumableData GenerateRandomConsumable(ItemRarity _rarity, int _level)
        {
            ConsumableData consumable = CreateInstance<ConsumableData>();
            
            // 设置基础属性
            consumable.m_ItemType = ItemType.Consumable;
            consumable.m_Rarity = _rarity;
            
            // 随机选择消耗品类型
            ConsumableType type = (ConsumableType)Random.Range(0, 4);
            
            // 生成名称和效果
            GenerateConsumableProperties(consumable, type, _rarity, _level);
            
            // 设置价值
            consumable.m_Value = CalculateConsumableValue(_rarity, _level);
            
            // 设置堆叠数量
            consumable.m_MaxStackSize = GetStackSize(_rarity);
            
            return consumable;
        }

        /// <summary>
        /// 消耗品类型枚举
        /// </summary>
        private enum ConsumableType
        {
            Health = 0,     // 恢复生命值
            Mana = 1,       // 恢复法力值
            Experience = 2, // 增加经验值
            Buff = 3        // 临时增益
        }

        /// <summary>
        /// 生成消耗品属性
        /// </summary>
        private static void GenerateConsumableProperties(ConsumableData _consumable, 
            ConsumableType _type, ItemRarity _rarity, int _level)
        {
            // 基础效果乘数
            float effectMultiplier = 1f + (int)_rarity * 0.8f;
            float levelMultiplier = 1f + _level * 0.1f;
            float totalMultiplier = effectMultiplier * levelMultiplier;

            // 设置名称
            string[] typeNames = s_ConsumableNames[(int)_type];
            _consumable.m_ItemName = typeNames[Mathf.Min((int)_rarity, typeNames.Length - 1)];

            // 设置描述
            _consumable.m_Description = s_ConsumableDescriptions[Mathf.Min((int)_rarity, s_ConsumableDescriptions.Length - 1)];

            switch (_type)
            {
                case ConsumableType.Health:
                    _consumable.m_HealthRestore = Mathf.RoundToInt(Random.Range(20f, 50f) * totalMultiplier);
                    break;

                case ConsumableType.Mana:
                    _consumable.m_ManaRestore = Mathf.RoundToInt(Random.Range(15f, 35f) * totalMultiplier);
                    break;

                case ConsumableType.Experience:
                    _consumable.m_ExperienceGain = Mathf.RoundToInt(Random.Range(10f, 30f) * totalMultiplier);
                    break;

                case ConsumableType.Buff:
                    _consumable.m_Duration = Random.Range(30f, 120f) * (1f + (int)_rarity * 0.5f);
                    GenerateBuffEffect(_consumable, _rarity, totalMultiplier);
                    break;
            }
        }

        /// <summary>
        /// 生成增益效果
        /// </summary>
        private static void GenerateBuffEffect(ConsumableData _consumable, ItemRarity _rarity, float _multiplier)
        {
            // 根据稀有度确定增益属性数量
            int buffCount = 1 + (int)_rarity / 2;
            
            for (int i = 0; i < buffCount; i++)
            {
                int statType = Random.Range(0, 5);
                float buffValue = Random.Range(3f, 8f) * _multiplier;

                switch (statType)
                {
                    case 0: // 攻击力
                        _consumable.m_TempBonus.AddSingleStat(0, buffValue);
                        break;
                    case 1: // 防御力
                        _consumable.m_TempBonus.AddSingleStat(1, buffValue);
                        break;
                    case 2: // 速度
                        _consumable.m_TempBonus.AddSingleStat(2, buffValue * 0.5f);
                        break;
                    case 3: // 暴击率
                        _consumable.m_TempBonus.AddSingleStat(3, buffValue * 0.01f);
                        break;
                    case 4: // 运气
                        _consumable.m_TempBonus.AddSingleStat(4, buffValue * 0.3f);
                        break;
                }
            }
        }

        /// <summary>
        /// 计算消耗品价值
        /// </summary>
        private static int CalculateConsumableValue(ItemRarity _rarity, int _level)
        {
            int baseValue = 20;
            int rarityMultiplier = 1 + (int)_rarity;
            int levelMultiplier = 1 + _level / 2;
            
            return baseValue * rarityMultiplier * levelMultiplier;
        }

        /// <summary>
        /// 获取堆叠数量
        /// </summary>
        private static int GetStackSize(ItemRarity _rarity)
        {
            switch (_rarity)
            {
                case ItemRarity.Common: return 99;
                case ItemRarity.Uncommon: return 50;
                case ItemRarity.Rare: return 20;
                case ItemRarity.Epic: return 10;
                case ItemRarity.Legendary: return 5;
                case ItemRarity.Mythic: return 1;
                default: return 99;
            }
        }
        #endregion

        #region 重写方法
        /// <summary>
        /// 使用消耗品
        /// </summary>
        public override bool UseItem(CharacterStats _characterStats)
        {
            if (_characterStats == null) return false;

            bool hasEffect = false;

            // 恢复生命值
            if (m_HealthRestore > 0)
            {
                int oldHealth = _characterStats.CurrentHealth;
                _characterStats.Heal(m_HealthRestore);
                int healedAmount = _characterStats.CurrentHealth - oldHealth;
                
                if (healedAmount > 0)
                {
                    Debug.Log($"恢复了 {healedAmount} 点生命值");
                    hasEffect = true;
                }
            }

            // 恢复法力值
            if (m_ManaRestore > 0)
            {
                int oldMana = _characterStats.CurrentMana;
                _characterStats.RestoreMana(m_ManaRestore);
                int restoredAmount = _characterStats.CurrentMana - oldMana;
                
                if (restoredAmount > 0)
                {
                    Debug.Log($"恢复了 {restoredAmount} 点法力值");
                    hasEffect = true;
                }
            }

            // 获得经验值
            if (m_ExperienceGain > 0)
            {
                bool leveledUp = _characterStats.GainExperience(m_ExperienceGain);
                Debug.Log($"获得了 {m_ExperienceGain} 点经验值" + (leveledUp ? "，等级提升！" : ""));
                hasEffect = true;
            }

            // 临时增益效果（这里简化处理，实际应该有增益系统）
            if (m_Duration > 0 && HasTempBonus())
            {
                Debug.Log($"获得临时增益效果，持续 {m_Duration} 秒");
                // 这里应该调用增益系统来应用临时效果
                hasEffect = true;
            }

            return hasEffect;
        }

        /// <summary>
        /// 检查是否有临时增益效果
        /// </summary>
        private bool HasTempBonus()
        {
            return m_TempBonus.Attack > 0 || m_TempBonus.Defense > 0 || 
                   m_TempBonus.Speed > 0 || m_TempBonus.CriticalRate > 0 || 
                   m_TempBonus.Luck > 0;
        }

        /// <summary>
        /// 获取消耗品详细信息
        /// </summary>
        public override string GetDetailedInfo()
        {
            string baseInfo = base.GetDetailedInfo();
            
            string effectInfo = "\n消耗品效果：\n";
            
            if (m_HealthRestore > 0)
                effectInfo += $"恢复生命值：{m_HealthRestore}\n";
            if (m_ManaRestore > 0)
                effectInfo += $"恢复法力值：{m_ManaRestore}\n";
            if (m_ExperienceGain > 0)
                effectInfo += $"获得经验值：{m_ExperienceGain}\n";
            
            if (m_Duration > 0 && HasTempBonus())
            {
                effectInfo += $"临时增益效果（持续{m_Duration}秒）：\n";
                
                if (m_TempBonus.Attack > 0)
                    effectInfo += $"  攻击力 +{m_TempBonus.Attack}\n";
                if (m_TempBonus.Defense > 0)
                    effectInfo += $"  防御力 +{m_TempBonus.Defense}\n";
                if (m_TempBonus.Speed > 0)
                    effectInfo += $"  速度 +{m_TempBonus.Speed}\n";
                if (m_TempBonus.CriticalRate > 0)
                    effectInfo += $"  暴击率 +{(m_TempBonus.CriticalRate * 100):F1}%\n";
                if (m_TempBonus.Luck > 0)
                    effectInfo += $"  运气 +{m_TempBonus.Luck}\n";
            }
            
            return baseInfo + effectInfo;
        }
        #endregion
    }
}