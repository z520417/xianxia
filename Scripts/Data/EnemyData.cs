using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 敌人数据类
    /// 定义敌人的基本属性和行为
    /// </summary>
    [System.Serializable]
    public class EnemyData
    {
        [Header("基本信息")]
        public string EnemyId;
        public string Name;
        [TextArea(2, 4)]
        public string Description;
        public Sprite Icon;

        [Header("等级和属性")]
        public int Level;
        public CharacterStats BaseStats;
        public EnemyType EnemyType;
        public EnemyRarity Rarity;

        [Header("战斗配置")]
        public float AggroRange = 5f;
        public float AttackSpeed = 1f;
        public float MoveSpeed = 3f;
        public bool CanUseSkills = false;
        public string[] AvailableSkills;

        [Header("掉落配置")]
        public int BaseExperienceReward = 25;
        public int MinGoldReward = 10;
        public int MaxGoldReward = 50;
        [Range(0f, 1f)]
        public float ItemDropChance = 0.3f;
        public ItemRarity[] PossibleDropRarities = { ItemRarity.Common, ItemRarity.Uncommon };

        [Header("AI配置")]
        public EnemyAIType AIType = EnemyAIType.Basic;
        public float ThinkInterval = 1f;
        public bool CanFlee = false;
        [Range(0f, 1f)]
        public float FleeHealthThreshold = 0.2f;

        /// <summary>
        /// 构造函数
        /// </summary>
        public EnemyData()
        {
            EnemyId = System.Guid.NewGuid().ToString();
            Name = "未命名敌人";
            Level = 1;
            BaseStats = new CharacterStats(1);
            EnemyType = EnemyType.Beast;
            Rarity = EnemyRarity.Common;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public EnemyData(string name, int level, EnemyType type = EnemyType.Beast)
        {
            EnemyId = System.Guid.NewGuid().ToString();
            Name = name;
            Level = level;
            BaseStats = new CharacterStats(level);
            EnemyType = type;
            Rarity = CalculateRarityByLevel(level);
        }

        /// <summary>
        /// 获取调整后的属性（考虑等级和稀有度）
        /// </summary>
        public CharacterStats GetAdjustedStats()
        {
            var adjustedStats = new CharacterStats(BaseStats);

            // 应用稀有度修正
            float rarityMultiplier = GetRarityMultiplier();
            adjustedStats.MultiplyStats(rarityMultiplier);

            // 应用敌人类型修正
            ApplyEnemyTypeModifications(adjustedStats);

            return adjustedStats;
        }

        /// <summary>
        /// 获取经验奖励
        /// </summary>
        public int GetExperienceReward()
        {
            float baseExp = BaseExperienceReward;
            float levelMultiplier = 1f + (Level - 1) * 0.1f;
            float rarityMultiplier = GetRarityMultiplier();
            
            return Mathf.RoundToInt(baseExp * levelMultiplier * rarityMultiplier);
        }

        /// <summary>
        /// 获取金币奖励
        /// </summary>
        public int GetGoldReward()
        {
            float baseGold = Random.Range(MinGoldReward, MaxGoldReward + 1);
            float levelMultiplier = 1f + (Level - 1) * 0.05f;
            float rarityMultiplier = GetRarityMultiplier();
            
            return Mathf.RoundToInt(baseGold * levelMultiplier * rarityMultiplier);
        }

        /// <summary>
        /// 检查是否掉落物品
        /// </summary>
        public bool ShouldDropItem()
        {
            float adjustedChance = ItemDropChance * GetRarityMultiplier();
            return Random.Range(0f, 1f) <= adjustedChance;
        }

        /// <summary>
        /// 生成掉落物品
        /// </summary>
        public ItemData GenerateDropItem()
        {
            if (!ShouldDropItem()) return null;

            // 选择随机稀有度
            var rarity = PossibleDropRarities[Random.Range(0, PossibleDropRarities.Length)];
            
            // 使用物品生成器创建物品
            var generator = ComponentRegistry.GetComponent<DataDrivenItemGenerator>();
            if (generator != null)
            {
                var items = generator.GenerateItems(1, Level, 0.1f);
                return items.Count > 0 ? items[0] : null;
            }
            else
            {
                // 回退到旧生成器
                return RandomItemGenerator.Instance?.GenerateRandomItem(Level, 10);
            }
        }

        /// <summary>
        /// 创建随机敌人
        /// </summary>
        public static EnemyData CreateRandomEnemy(int level, EnemyType type = EnemyType.Random)
        {
            if (type == EnemyType.Random)
            {
                var values = System.Enum.GetValues(typeof(EnemyType));
                type = (EnemyType)values.GetValue(Random.Range(1, values.Length)); // 跳过Random
            }

            string[] names = GetNamesForType(type);
            string name = names[Random.Range(0, names.Length)];

            var enemy = new EnemyData(name, level, type);
            enemy.RandomizeAttributes();
            
            return enemy;
        }

        /// <summary>
        /// 随机化属性
        /// </summary>
        private void RandomizeAttributes()
        {
            // 随机调整基础属性（±20%）
            float variance = 0.2f;
            BaseStats.ModifyStats(
                Random.Range(1f - variance, 1f + variance),
                Random.Range(1f - variance, 1f + variance),
                Random.Range(1f - variance, 1f + variance),
                Random.Range(1f - variance, 1f + variance),
                Random.Range(1f - variance, 1f + variance)
            );

            // 随机化AI配置
            ThinkInterval = Random.Range(0.5f, 2f);
            AggroRange = Random.Range(3f, 8f);
            
            // 根据类型调整逃跑概率
            CanFlee = EnemyType switch
            {
                EnemyType.Beast => Random.Range(0f, 1f) < 0.7f,
                EnemyType.Undead => false,
                EnemyType.Demon => Random.Range(0f, 1f) < 0.3f,
                EnemyType.Elemental => Random.Range(0f, 1f) < 0.5f,
                EnemyType.Humanoid => Random.Range(0f, 1f) < 0.8f,
                _ => Random.Range(0f, 1f) < 0.5f
            };
        }

        /// <summary>
        /// 获取稀有度倍数
        /// </summary>
        private float GetRarityMultiplier()
        {
            return Rarity switch
            {
                EnemyRarity.Common => 1f,
                EnemyRarity.Uncommon => 1.2f,
                EnemyRarity.Rare => 1.5f,
                EnemyRarity.Elite => 2f,
                EnemyRarity.Boss => 3f,
                EnemyRarity.Legendary => 5f,
                _ => 1f
            };
        }

        /// <summary>
        /// 应用敌人类型修正
        /// </summary>
        private void ApplyEnemyTypeModifications(CharacterStats stats)
        {
            switch (EnemyType)
            {
                case EnemyType.Beast:
                    stats.ModifyStats(health: 1.1f, attack: 1.1f, speed: 1.2f);
                    break;
                case EnemyType.Undead:
                    stats.ModifyStats(health: 1.3f, defense: 1.2f, speed: 0.8f);
                    break;
                case EnemyType.Demon:
                    stats.ModifyStats(attack: 1.3f, mana: 1.2f, defense: 0.9f);
                    break;
                case EnemyType.Elemental:
                    stats.ModifyStats(mana: 1.5f, attack: 1.1f, health: 0.9f);
                    break;
                case EnemyType.Humanoid:
                    // 平衡型，无修正
                    break;
                case EnemyType.Dragon:
                    stats.ModifyStats(health: 1.5f, attack: 1.4f, defense: 1.3f, speed: 0.8f);
                    break;
            }
        }

        /// <summary>
        /// 根据等级计算稀有度
        /// </summary>
        private EnemyRarity CalculateRarityByLevel(int level)
        {
            float rarityRoll = Random.Range(0f, 100f);
            
            if (level >= 50 && rarityRoll < 5f) return EnemyRarity.Legendary;
            if (level >= 40 && rarityRoll < 10f) return EnemyRarity.Boss;
            if (level >= 30 && rarityRoll < 20f) return EnemyRarity.Elite;
            if (level >= 15 && rarityRoll < 35f) return EnemyRarity.Rare;
            if (level >= 5 && rarityRoll < 55f) return EnemyRarity.Uncommon;
            
            return EnemyRarity.Common;
        }

        /// <summary>
        /// 获取敌人类型对应的名称
        /// </summary>
        private static string[] GetNamesForType(EnemyType type)
        {
            return type switch
            {
                EnemyType.Beast => new[] { "野狼", "巨熊", "毒蛇", "雷鹰", "火狐", "冰虎" },
                EnemyType.Undead => new[] { "骷髅兵", "僵尸", "幽魂", "死灵法师", "巫妖", "骨龙" },
                EnemyType.Demon => new[] { "小恶魔", "炎魔", "暗影恶魔", "魔王", "堕落天使", "地狱犬" },
                EnemyType.Elemental => new[] { "火元素", "水元素", "土元素", "风元素", "雷元素", "冰元素" },
                EnemyType.Humanoid => new[] { "盗贼", "刺客", "武士", "法师", "弓箭手", "骑士" },
                EnemyType.Dragon => new[] { "幼龙", "成年龙", "古龙", "远古龙", "神龙", "魔龙" },
                _ => new[] { "未知生物", "神秘敌人", "混沌生物" }
            };
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            var stats = GetAdjustedStats();
            return $"{Name} (等级 {Level})\n" +
                   $"类型: {GetEnemyTypeText()}\n" +
                   $"稀有度: {GetRarityText()}\n" +
                   $"生命: {stats.MaxHealth}\n" +
                   $"攻击: {stats.Attack}\n" +
                   $"防御: {stats.Defense}\n" +
                   $"速度: {stats.Speed}\n" +
                   $"经验奖励: {GetExperienceReward()}\n" +
                   $"金币奖励: {MinGoldReward}-{MaxGoldReward}";
        }

        private string GetEnemyTypeText()
        {
            return EnemyType switch
            {
                EnemyType.Beast => "野兽",
                EnemyType.Undead => "不死族",
                EnemyType.Demon => "恶魔",
                EnemyType.Elemental => "元素",
                EnemyType.Humanoid => "人形",
                EnemyType.Dragon => "龙族",
                _ => "未知"
            };
        }

        private string GetRarityText()
        {
            return Rarity switch
            {
                EnemyRarity.Common => "普通",
                EnemyRarity.Uncommon => "不普通",
                EnemyRarity.Rare => "稀有",
                EnemyRarity.Elite => "精英",
                EnemyRarity.Boss => "首领",
                EnemyRarity.Legendary => "传说",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 敌人类型
    /// </summary>
    public enum EnemyType
    {
        Random,     // 随机选择
        Beast,      // 野兽
        Undead,     // 不死族
        Demon,      // 恶魔
        Elemental,  // 元素
        Humanoid,   // 人形
        Dragon      // 龙族
    }

    /// <summary>
    /// 敌人稀有度
    /// </summary>
    public enum EnemyRarity
    {
        Common,     // 普通
        Uncommon,   // 不普通
        Rare,       // 稀有
        Elite,      // 精英
        Boss,       // 首领
        Legendary   // 传说
    }

    /// <summary>
    /// 敌人AI类型
    /// </summary>
    public enum EnemyAIType
    {
        Basic,      // 基础AI
        Aggressive, // 攻击型
        Defensive,  // 防御型
        Smart,      // 智能型
        Berserker,  // 狂战士
        Coward      // 胆小型
    }
}