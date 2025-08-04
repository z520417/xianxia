using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 探索事件类型
    /// </summary>
    public enum ExplorationEventType
    {
        TreasureHunt,    // 寻宝
        EnemyEncounter,  // 敌人遭遇
        ItemDiscovery,   // 物品发现
        NothingFound,    // 一无所获
        SpecialEvent,    // 特殊事件
        Merchant,        // 商人遭遇
        Shrine,          // 神龛
        Trap,            // 陷阱
        SecretArea       // 秘密区域
    }

    /// <summary>
    /// 探索事件数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Exploration Event", menuName = "仙侠游戏/探索事件")]
    public class ExplorationEventData : ScriptableObject
    {
        [Header("基本信息")]
        public string EventId;
        public string EventName;
        [TextArea(3, 5)]
        public string EventDescription;
        public ExplorationEventType EventType;
        
        [Header("触发条件")]
        public int MinPlayerLevel = 1;
        public int MaxPlayerLevel = 100;
        [Range(0f, 100f)]
        public float BaseChance = 10f;
        public List<string> RequiredConditions = new List<string>();
        
        [Header("消息配置")]
        public List<string> StartMessages = new List<string>();
        public List<string> SuccessMessages = new List<string>();
        public List<string> FailureMessages = new List<string>();
        
        [Header("奖励配置")]
        public ExplorationReward Rewards;
        
        [Header("特殊配置")]
        public bool IsRepeatable = true;
        public float CooldownTime = 0f;
        public List<string> Tags = new List<string>();

        /// <summary>
        /// 检查事件是否可以触发
        /// </summary>
        public bool CanTrigger(int playerLevel, List<string> playerConditions = null)
        {
            // 等级检查
            if (playerLevel < MinPlayerLevel || playerLevel > MaxPlayerLevel)
                return false;

            // 条件检查
            if (RequiredConditions.Count > 0 && playerConditions != null)
            {
                foreach (var condition in RequiredConditions)
                {
                    if (!playerConditions.Contains(condition))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取随机开始消息
        /// </summary>
        public string GetRandomStartMessage()
        {
            if (StartMessages.Count == 0)
                return $"触发了事件：{EventName}";
            
            return StartMessages[Random.Range(0, StartMessages.Count)];
        }

        /// <summary>
        /// 获取随机成功消息
        /// </summary>
        public string GetRandomSuccessMessage()
        {
            if (SuccessMessages.Count == 0)
                return "事件成功完成！";
            
            return SuccessMessages[Random.Range(0, SuccessMessages.Count)];
        }

        /// <summary>
        /// 获取随机失败消息
        /// </summary>
        public string GetRandomFailureMessage()
        {
            if (FailureMessages.Count == 0)
                return "事件未能完成...";
            
            return FailureMessages[Random.Range(0, FailureMessages.Count)];
        }

        /// <summary>
        /// 计算实际触发概率
        /// </summary>
        public float CalculateTriggerChance(int playerLevel, float luckModifier = 0f)
        {
            float chance = BaseChance;
            
            // 运气修正
            chance += luckModifier;
            
            // 等级修正（可选）
            if (EventType == ExplorationEventType.SpecialEvent)
            {
                chance += playerLevel * 0.1f;
            }
            
            return Mathf.Clamp(chance, 0f, 100f);
        }
    }

    /// <summary>
    /// 探索奖励配置
    /// </summary>
    [System.Serializable]
    public class ExplorationReward
    {
        [Header("经验奖励")]
        public int BaseExperience = 0;
        public float ExperiencePerLevel = 0f;
        
        [Header("金钱奖励")]
        public int MinGold = 0;
        public int MaxGold = 0;
        public float GoldPerLevel = 0f;
        
        [Header("物品奖励")]
        public List<ItemReward> ItemRewards = new List<ItemReward>();
        
        [Header("特殊奖励")]
        public List<string> SpecialRewards = new List<string>();

        /// <summary>
        /// 计算经验奖励
        /// </summary>
        public int CalculateExperience(int playerLevel)
        {
            return Mathf.RoundToInt(BaseExperience + ExperiencePerLevel * playerLevel);
        }

        /// <summary>
        /// 计算金钱奖励
        /// </summary>
        public int CalculateGold(int playerLevel)
        {
            int baseGold = Random.Range(MinGold, MaxGold + 1);
            float levelBonus = GoldPerLevel * playerLevel;
            return Mathf.RoundToInt(baseGold + levelBonus);
        }

        /// <summary>
        /// 获取物品奖励
        /// </summary>
        public List<ItemData> GetItemRewards(int playerLevel, float luckModifier = 0f)
        {
            var rewards = new List<ItemData>();
            
            foreach (var itemReward in ItemRewards)
            {
                if (itemReward.ShouldDrop(luckModifier))
                {
                    var item = itemReward.GenerateItem(playerLevel);
                    if (item != null)
                    {
                        rewards.Add(item);
                    }
                }
            }
            
            return rewards;
        }
    }

    /// <summary>
    /// 物品奖励配置
    /// </summary>
    [System.Serializable]
    public class ItemReward
    {
        [Header("物品配置")]
        public ItemTemplate ItemTemplate;
        public string ItemTemplateId; // 如果没有直接引用，可以通过ID查找
        
        [Header("掉落配置")]
        [Range(0f, 1f)]
        public float DropChance = 0.5f;
        public int MinQuantity = 1;
        public int MaxQuantity = 1;
        
        [Header("稀有度配置")]
        public ItemRarity MinRarity = ItemRarity.Common;
        public ItemRarity MaxRarity = ItemRarity.Rare;
        public AnimationCurve RarityDistribution; // 稀有度分布曲线

        /// <summary>
        /// 判断是否应该掉落
        /// </summary>
        public bool ShouldDrop(float luckModifier = 0f)
        {
            float actualChance = DropChance + luckModifier * 0.01f;
            return Random.Range(0f, 1f) < actualChance;
        }

        /// <summary>
        /// 生成物品
        /// </summary>
        public ItemData GenerateItem(int playerLevel)
        {
            if (ItemTemplate == null)
            {
                Debug.LogWarning($"ItemTemplate为空，无法生成物品");
                return null;
            }

            ItemRarity rarity = DetermineRarity();
            int quantity = Random.Range(MinQuantity, MaxQuantity + 1);
            
            var item = ItemTemplate.CreateItem(rarity, playerLevel);
            // 这里可能需要设置数量，取决于ItemData的实现
            
            return item;
        }

        /// <summary>
        /// 确定稀有度
        /// </summary>
        private ItemRarity DetermineRarity()
        {
            if (RarityDistribution != null && RarityDistribution.keys.Length > 0)
            {
                float randomValue = Random.Range(0f, 1f);
                float curveValue = RarityDistribution.Evaluate(randomValue);
                
                // 将曲线值映射到稀有度范围
                int rarityRange = (int)MaxRarity - (int)MinRarity;
                int rarityIndex = Mathf.RoundToInt(curveValue * rarityRange) + (int)MinRarity;
                
                return (ItemRarity)Mathf.Clamp(rarityIndex, (int)MinRarity, (int)MaxRarity);
            }
            else
            {
                // 均匀分布
                return (ItemRarity)Random.Range((int)MinRarity, (int)MaxRarity + 1);
            }
        }
    }

    /// <summary>
    /// 探索区域配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Exploration Area", menuName = "仙侠游戏/探索区域")]
    public class ExplorationAreaData : ScriptableObject
    {
        [Header("区域信息")]
        public string AreaId;
        public string AreaName;
        [TextArea(3, 5)]
        public string AreaDescription;
        public Sprite AreaImage;
        
        [Header("区域配置")]
        public int MinPlayerLevel = 1;
        public int MaxPlayerLevel = 100;
        public float DifficultyMultiplier = 1f;
        
        [Header("可触发事件")]
        public List<ExplorationEventData> AvailableEvents = new List<ExplorationEventData>();
        public List<EventWeight> EventWeights = new List<EventWeight>();
        
        [Header("环境效果")]
        public List<EnvironmentEffect> EnvironmentEffects = new List<EnvironmentEffect>();
        
        [Header("特殊属性")]
        public List<string> AreaTags = new List<string>();
        public Color AreaThemeColor = Color.white;

        /// <summary>
        /// 获取可用的探索事件
        /// </summary>
        public List<ExplorationEventData> GetAvailableEvents(int playerLevel)
        {
            var availableEvents = new List<ExplorationEventData>();
            
            foreach (var eventData in AvailableEvents)
            {
                if (eventData.CanTrigger(playerLevel))
                {
                    availableEvents.Add(eventData);
                }
            }
            
            return availableEvents;
        }

        /// <summary>
        /// 根据权重选择事件
        /// </summary>
        public ExplorationEventData SelectRandomEvent(int playerLevel, float luckModifier = 0f)
        {
            var availableEvents = GetAvailableEvents(playerLevel);
            if (availableEvents.Count == 0)
                return null;

            // 计算总权重
            float totalWeight = 0f;
            foreach (var eventData in availableEvents)
            {
                float weight = GetEventWeight(eventData.EventType);
                totalWeight += weight * eventData.CalculateTriggerChance(playerLevel, luckModifier);
            }

            if (totalWeight <= 0f)
                return null;

            // 随机选择
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var eventData in availableEvents)
            {
                float weight = GetEventWeight(eventData.EventType);
                currentWeight += weight * eventData.CalculateTriggerChance(playerLevel, luckModifier);
                
                if (randomValue <= currentWeight)
                {
                    return eventData;
                }
            }

            // 如果没有选中，返回最后一个
            return availableEvents[availableEvents.Count - 1];
        }

        /// <summary>
        /// 获取事件权重
        /// </summary>
        private float GetEventWeight(ExplorationEventType eventType)
        {
            foreach (var eventWeight in EventWeights)
            {
                if (eventWeight.EventType == eventType)
                {
                    return eventWeight.Weight;
                }
            }
            
            return 1f; // 默认权重
        }
    }

    /// <summary>
    /// 事件权重配置
    /// </summary>
    [System.Serializable]
    public class EventWeight
    {
        public ExplorationEventType EventType;
        [Range(0f, 100f)]
        public float Weight = 1f;
    }

    /// <summary>
    /// 环境效果
    /// </summary>
    [System.Serializable]
    public class EnvironmentEffect
    {
        [Header("效果信息")]
        public string EffectName;
        public string EffectDescription;
        
        [Header("属性影响")]
        public StatModifier StatModifier;
        
        [Header("持续时间")]
        public float Duration = -1f; // -1表示永久
        
        [Header("视觉效果")]
        public ParticleSystem EffectParticles;
        public Color EffectColor = Color.white;
    }

    /// <summary>
    /// 探索事件管理器
    /// </summary>
    [CreateAssetMenu(fileName = "Exploration Event Manager", menuName = "仙侠游戏/探索事件管理器")]
    public class ExplorationEventManager : ScriptableObject
    {
        [Header("全局配置")]
        public List<ExplorationAreaData> AllAreas = new List<ExplorationAreaData>();
        public List<ExplorationEventData> GlobalEvents = new List<ExplorationEventData>();
        
        [Header("默认权重")]
        public List<EventWeight> DefaultEventWeights = new List<EventWeight>();

        /// <summary>
        /// 根据玩家等级获取合适的区域
        /// </summary>
        public List<ExplorationAreaData> GetSuitableAreas(int playerLevel)
        {
            var suitableAreas = new List<ExplorationAreaData>();
            
            foreach (var area in AllAreas)
            {
                if (playerLevel >= area.MinPlayerLevel && playerLevel <= area.MaxPlayerLevel)
                {
                    suitableAreas.Add(area);
                }
            }
            
            return suitableAreas;
        }

        /// <summary>
        /// 获取随机探索事件
        /// </summary>
        public ExplorationEventData GetRandomEvent(int playerLevel, string areaId = "", float luckModifier = 0f)
        {
            ExplorationAreaData targetArea = null;
            
            if (!string.IsNullOrEmpty(areaId))
            {
                targetArea = AllAreas.Find(area => area.AreaId == areaId);
            }
            
            if (targetArea == null)
            {
                var suitableAreas = GetSuitableAreas(playerLevel);
                if (suitableAreas.Count > 0)
                {
                    targetArea = suitableAreas[Random.Range(0, suitableAreas.Count)];
                }
            }

            if (targetArea != null)
            {
                return targetArea.SelectRandomEvent(playerLevel, luckModifier);
            }

            // 如果没有合适的区域，从全局事件中选择
            var availableGlobalEvents = new List<ExplorationEventData>();
            foreach (var eventData in GlobalEvents)
            {
                if (eventData.CanTrigger(playerLevel))
                {
                    availableGlobalEvents.Add(eventData);
                }
            }

            if (availableGlobalEvents.Count > 0)
            {
                return availableGlobalEvents[Random.Range(0, availableGlobalEvents.Count)];
            }

            return null;
        }

        /// <summary>
        /// 根据ID获取事件
        /// </summary>
        public ExplorationEventData GetEventById(string eventId)
        {
            foreach (var area in AllAreas)
            {
                var eventData = area.AvailableEvents.Find(e => e.EventId == eventId);
                if (eventData != null)
                    return eventData;
            }

            return GlobalEvents.Find(e => e.EventId == eventId);
        }

        /// <summary>
        /// 根据ID获取区域
        /// </summary>
        public ExplorationAreaData GetAreaById(string areaId)
        {
            return AllAreas.Find(area => area.AreaId == areaId);
        }
    }
}