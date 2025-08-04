using UnityEngine;
using XianXiaGame.Core;
using System.Collections.Generic;
using System.Linq;

namespace XianXiaGame.Controllers
{
    /// <summary>
    /// 探索控制器
    /// 专门负责探索逻辑，与其他系统解耦
    /// </summary>
    public class ExplorationController : RegisteredComponent, IExplorationService
    {
        #region 配置

        [Header("探索配置")]
        [SerializeField] private bool m_UseConfiguredEvents = true;
        [SerializeField] private List<ExplorationEventData> m_CustomEvents = new List<ExplorationEventData>();

        [Header("调试设置")]
        [SerializeField] private bool m_EnableDebugMode = false;

        #endregion

        #region 服务引用

        private IConfigService m_ConfigService;
        private IEventService m_EventService;
        private ILoggingService m_LoggingService;
        private IGameDataService m_GameDataService;
        private IStatisticsService m_StatisticsService;

        #endregion

        #region 探索数据

        private List<ExplorationEventData> m_AvailableEvents = new List<ExplorationEventData>();
        private Dictionary<string, float> m_EventCooldowns = new Dictionary<string, float>();
        private ExplorationSession m_CurrentSession;

        #endregion

        #region 事件

        public event System.Action<ExplorationResult> OnExplorationCompleted;
        public event System.Action<ExplorationEventData> OnEventTriggered;
        public event System.Action<string> OnExplorationMessage;

        #endregion

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeServices();
        }

        private void Start()
        {
            LoadExplorationEvents();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
            m_GameDataService = GameServiceBootstrapper.GetService<IGameDataService>();
            m_StatisticsService = GameServiceBootstrapper.GetService<IStatisticsService>();
        }

        /// <summary>
        /// 加载探索事件
        /// </summary>
        private void LoadExplorationEvents()
        {
            m_AvailableEvents.Clear();

            if (m_UseConfiguredEvents)
            {
                // 从配置加载事件
                var configEvents = Resources.LoadAll<ExplorationEventData>("Data/ExplorationEvents");
                if (configEvents != null && configEvents.Length > 0)
                {
                    m_AvailableEvents.AddRange(configEvents);
                    GameLog.Debug($"从配置加载了 {configEvents.Length} 个探索事件", "ExplorationController");
                }
            }

            // 添加自定义事件
            if (m_CustomEvents.Count > 0)
            {
                m_AvailableEvents.AddRange(m_CustomEvents);
                GameLog.Debug($"添加了 {m_CustomEvents.Count} 个自定义探索事件", "ExplorationController");
            }

            // 如果没有事件，创建默认事件
            if (m_AvailableEvents.Count == 0)
            {
                CreateDefaultEvents();
            }

            GameLog.Info($"探索系统已加载 {m_AvailableEvents.Count} 个事件", "ExplorationController");
        }

        /// <summary>
        /// 创建默认探索事件
        /// </summary>
        private void CreateDefaultEvents()
        {
            var treasureEvent = CreateDefaultTreasureEvent();
            var battleEvent = CreateDefaultBattleEvent();
            var itemEvent = CreateDefaultItemEvent();
            var nothingEvent = CreateDefaultNothingEvent();

            m_AvailableEvents.AddRange(new[] { treasureEvent, battleEvent, itemEvent, nothingEvent });
        }

        #endregion

        #region 探索核心逻辑

        /// <summary>
        /// 开始探索
        /// </summary>
        public void StartExploration()
        {
            var playerData = GetPlayerData();
            if (playerData == null)
            {
                GameLog.Error("无法获取玩家数据，探索失败", "ExplorationController");
                return;
            }

            m_CurrentSession = new ExplorationSession
            {
                StartTime = Time.time,
                PlayerLevel = playerData.Level,
                PlayerLuck = playerData.Luck,
                SessionId = System.Guid.NewGuid().ToString()
            };

            if (m_EnableDebugMode)
            {
                GameLog.Debug($"开始探索会话: {m_CurrentSession.SessionId}", "ExplorationController");
            }

            // 触发探索开始事件
            m_EventService?.TriggerEvent(GameEventType.ExplorationStarted, 
                new ExplorationStartedEventData(m_CurrentSession));
        }

        /// <summary>
        /// 处理探索
        /// </summary>
        public ExplorationResult ProcessExploration()
        {
            if (m_CurrentSession == null)
            {
                GameLog.Warning("没有活跃的探索会话", "ExplorationController");
                return null;
            }

            try
            {
                // 选择事件
                var selectedEvent = SelectExplorationEvent();
                if (selectedEvent == null)
                {
                    return CreateNothingFoundResult();
                }

                // 触发事件
                OnEventTriggered?.Invoke(selectedEvent);

                // 处理事件
                var result = ProcessEvent(selectedEvent);

                // 记录统计
                RecordExplorationStatistics(result);

                // 完成探索会话
                CompleteExplorationSession(result);

                return result;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"探索处理失败: {e.Message}", "ExplorationController");
                return CreateErrorResult();
            }
        }

        /// <summary>
        /// 选择探索事件
        /// </summary>
        private ExplorationEventData SelectExplorationEvent()
        {
            var availableEvents = GetAvailableEvents();
            if (availableEvents.Count == 0)
            {
                return null;
            }

            // 计算权重
            var weightedEvents = new List<(ExplorationEventData evt, float weight)>();
            float totalWeight = 0f;

            foreach (var evt in availableEvents)
            {
                float weight = CalculateEventWeight(evt);
                if (weight > 0f)
                {
                    weightedEvents.Add((evt, weight));
                    totalWeight += weight;
                }
            }

            if (weightedEvents.Count == 0)
            {
                return null;
            }

            // 随机选择
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var (evt, weight) in weightedEvents)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                {
                    return evt;
                }
            }

            return weightedEvents.Last().evt;
        }

        /// <summary>
        /// 计算事件权重
        /// </summary>
        private float CalculateEventWeight(ExplorationEventData eventData)
        {
            if (eventData == null) return 0f;

            var playerData = GetPlayerData();
            if (playerData == null) return 0f;

            // 检查等级限制
            if (playerData.Level < eventData.MinPlayerLevel || 
                playerData.Level > eventData.MaxPlayerLevel)
            {
                return 0f;
            }

            // 检查冷却时间
            if (IsEventOnCooldown(eventData.EventId))
            {
                return 0f;
            }

            // 基础权重
            float weight = eventData.BaseChance;

            // 运气影响
            if (playerData.Luck > 0)
            {
                weight = GameConstants.GetLuckAffectedChance(weight / 100f, playerData.Luck) * 100f;
            }

            return weight;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        private ExplorationResult ProcessEvent(ExplorationEventData eventData)
        {
            var result = new ExplorationResult
            {
                EventId = eventData.EventId,
                EventName = eventData.EventName,
                Type = GetResultType(eventData.EventType),
                Message = GetRandomMessage(eventData.StartMessages),
                Timestamp = System.DateTime.Now
            };

            // 处理奖励
            ProcessEventRewards(eventData, result);

            // 设置冷却时间
            if (eventData.CooldownTime > 0f)
            {
                SetEventCooldown(eventData.EventId, eventData.CooldownTime);
            }

            if (m_EnableDebugMode)
            {
                GameLog.Debug($"处理事件: {eventData.EventName}, 类型: {result.Type}", "ExplorationController");
            }

            return result;
        }

        /// <summary>
        /// 处理事件奖励
        /// </summary>
        private void ProcessEventRewards(ExplorationEventData eventData, ExplorationResult result)
        {
            if (eventData.Rewards == null) return;

            var rewards = eventData.Rewards;
            var playerData = GetPlayerData();

            // 经验奖励
            if (rewards.BaseExperience > 0)
            {
                result.ExperienceReward = CalculateExperienceReward(rewards.BaseExperience, playerData.Level);
            }

            // 金币奖励
            if (rewards.MinGold > 0 || rewards.MaxGold > 0)
            {
                int baseGold = Random.Range(rewards.MinGold, rewards.MaxGold + 1);
                result.GoldReward = CalculateGoldReward(baseGold, rewards.GoldPerLevel, playerData.Level);
            }

            // 物品奖励
            if (rewards.ItemRewards != null && rewards.ItemRewards.Count > 0)
            {
                result.ItemsFound = GenerateItemRewards(rewards.ItemRewards);
            }

            // 特殊效果
            ProcessSpecialEffects(eventData, result);
        }

        #endregion

        #region 奖励计算

        /// <summary>
        /// 计算经验奖励
        /// </summary>
        private int CalculateExperienceReward(int baseExperience, int playerLevel)
        {
            float scaledExp = GameConstants.CalculateLevelScale(playerLevel, baseExperience);
            return Mathf.RoundToInt(scaledExp * Random.Range(0.8f, 1.2f));
        }

        /// <summary>
        /// 计算金币奖励
        /// </summary>
        private int CalculateGoldReward(int baseGold, float goldPerLevel, int playerLevel)
        {
            float levelBonus = goldPerLevel * playerLevel;
            return Mathf.RoundToInt((baseGold + levelBonus) * Random.Range(0.9f, 1.1f));
        }

        /// <summary>
        /// 生成物品奖励
        /// </summary>
        private List<ItemData> GenerateItemRewards(List<ItemReward> itemRewards)
        {
            var result = new List<ItemData>();
            var generator = ComponentRegistry.GetComponent<DataDrivenItemGenerator>();

            foreach (var reward in itemRewards)
            {
                if (Random.Range(0f, 100f) <= reward.DropChance)
                {
                    if (generator != null)
                    {
                        var items = generator.GenerateItems(reward.Quantity, 
                            m_CurrentSession.PlayerLevel, reward.QualityBonus);
                        result.AddRange(items);
                    }
                    else
                    {
                        // 回退生成方法
                        var fallbackItems = GenerateFallbackItems(reward);
                        result.AddRange(fallbackItems);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 生成回退物品
        /// </summary>
        private List<ItemData> GenerateFallbackItems(ItemReward reward)
        {
            var items = new List<ItemData>();
            var itemGenerator = RandomItemGenerator.Instance;

            if (itemGenerator != null)
            {
                for (int i = 0; i < reward.Quantity; i++)
                {
                    var item = itemGenerator.GenerateRandomItem(m_CurrentSession.PlayerLevel, 
                        m_CurrentSession.PlayerLuck);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        private CharacterStats GetPlayerData()
        {
            var gameManager = OptimizedGameManager.Instance;
            return gameManager?.PlayerStats;
        }

        /// <summary>
        /// 获取可用事件
        /// </summary>
        private List<ExplorationEventData> GetAvailableEvents()
        {
            var playerData = GetPlayerData();
            if (playerData == null) return new List<ExplorationEventData>();

            return m_AvailableEvents
                .Where(evt => evt != null && 
                             playerData.Level >= evt.MinPlayerLevel && 
                             playerData.Level <= evt.MaxPlayerLevel &&
                             !IsEventOnCooldown(evt.EventId))
                .ToList();
        }

        /// <summary>
        /// 检查事件是否在冷却中
        /// </summary>
        private bool IsEventOnCooldown(string eventId)
        {
            return m_EventCooldowns.ContainsKey(eventId) && 
                   m_EventCooldowns[eventId] > Time.time;
        }

        /// <summary>
        /// 设置事件冷却
        /// </summary>
        private void SetEventCooldown(string eventId, float cooldownTime)
        {
            m_EventCooldowns[eventId] = Time.time + cooldownTime;
        }

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        private void UpdateCooldowns()
        {
            if (Time.frameCount % 60 == 0) // 每秒检查一次
            {
                var expiredEvents = m_EventCooldowns
                    .Where(kvp => kvp.Value <= Time.time)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var eventId in expiredEvents)
                {
                    m_EventCooldowns.Remove(eventId);
                }
            }
        }

        /// <summary>
        /// 获取随机消息
        /// </summary>
        private string GetRandomMessage(List<string> messages)
        {
            if (messages == null || messages.Count == 0)
                return "你继续探索着...";

            return messages[Random.Range(0, messages.Count)];
        }

        /// <summary>
        /// 获取结果类型
        /// </summary>
        private ExplorationResultType GetResultType(ExplorationEventType eventType)
        {
            return eventType switch
            {
                ExplorationEventType.TreasureHunt => ExplorationResultType.TreasureFound,
                ExplorationEventType.EnemyEncounter => ExplorationResultType.BattleEncounter,
                ExplorationEventType.ItemDiscovery => ExplorationResultType.ItemFound,
                ExplorationEventType.Merchant => ExplorationResultType.MerchantEncounter,
                ExplorationEventType.Shrine => ExplorationResultType.SpecialEvent,
                _ => ExplorationResultType.NothingFound
            };
        }

        /// <summary>
        /// 处理特殊效果
        /// </summary>
        private void ProcessSpecialEffects(ExplorationEventData eventData, ExplorationResult result)
        {
            // 根据事件类型处理特殊效果
            switch (eventData.EventType)
            {
                case ExplorationEventType.EnemyEncounter:
                    // 生成敌人
                    result.EncounteredEnemy = GenerateEnemy();
                    break;

                case ExplorationEventType.Shrine:
                    // 神龛效果（属性提升等）
                    ProcessShrineEffect(result);
                    break;

                case ExplorationEventType.Trap:
                    // 陷阱效果（扣血等）
                    ProcessTrapEffect(result);
                    break;
            }
        }

        /// <summary>
        /// 生成敌人
        /// </summary>
        private CharacterStats GenerateEnemy()
        {
            var playerData = GetPlayerData();
            if (playerData == null) return null;

            var config = m_ConfigService?.Config?.Battle;
            int enemyLevel = playerData.Level + Random.Range(
                -GameConstants.Enemy.LEVEL_VARIANCE, 
                GameConstants.Enemy.LEVEL_VARIANCE + 1);
            enemyLevel = Mathf.Max(1, enemyLevel);

            return new CharacterStats(enemyLevel);
        }

        /// <summary>
        /// 处理神龛效果
        /// </summary>
        private void ProcessShrineEffect(ExplorationResult result)
        {
            // 临时属性提升或永久小幅提升
            result.Message += "\n神圣的力量提升了你的能力！";
        }

        /// <summary>
        /// 处理陷阱效果
        /// </summary>
        private void ProcessTrapEffect(ExplorationResult result)
        {
            // 减少生命值或其他负面效果
            var playerData = GetPlayerData();
            if (playerData != null)
            {
                int damage = Mathf.RoundToInt(playerData.MaxHealth * 0.1f);
                result.HealthLoss = damage;
                result.Message += $"\n你受到了 {damage} 点伤害！";
            }
        }

        #endregion

        #region 结果创建

        /// <summary>
        /// 创建"一无所获"结果
        /// </summary>
        private ExplorationResult CreateNothingFoundResult()
        {
            return new ExplorationResult
            {
                Type = ExplorationResultType.NothingFound,
                Message = MessageManager.GetRandomMessage(MessageCategory.Exploration),
                ExperienceReward = GameConstants.Exploration.NOTHING_FOUND_EXP,
                Timestamp = System.DateTime.Now
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        private ExplorationResult CreateErrorResult()
        {
            return new ExplorationResult
            {
                Type = ExplorationResultType.NothingFound,
                Message = "探索过程中发生了意外...",
                Timestamp = System.DateTime.Now
            };
        }

        #endregion

        #region 统计和完成

        /// <summary>
        /// 记录探索统计
        /// </summary>
        private void RecordExplorationStatistics(ExplorationResult result)
        {
            m_StatisticsService?.RecordExploration();

            switch (result.Type)
            {
                case ExplorationResultType.TreasureFound:
                    m_StatisticsService?.RecordTreasureFound();
                    break;
                case ExplorationResultType.BattleEncounter:
                    // 战斗统计由战斗系统处理
                    break;
                case ExplorationResultType.ItemFound:
                    m_StatisticsService?.RecordItemCollected();
                    break;
            }
        }

        /// <summary>
        /// 完成探索会话
        /// </summary>
        private void CompleteExplorationSession(ExplorationResult result)
        {
            if (m_CurrentSession != null)
            {
                m_CurrentSession.EndTime = Time.time;
                m_CurrentSession.Duration = m_CurrentSession.EndTime - m_CurrentSession.StartTime;
                m_CurrentSession.Result = result;

                // 触发完成事件
                OnExplorationCompleted?.Invoke(result);
                m_EventService?.TriggerEvent(GameEventType.ExplorationCompleted, 
                    new ExplorationCompletedEventData(m_CurrentSession, result));

                if (m_EnableDebugMode)
                {
                    GameLog.Debug($"探索会话完成: {m_CurrentSession.SessionId}, 耗时: {m_CurrentSession.Duration:F2}秒", 
                        "ExplorationController");
                }

                m_CurrentSession = null;
            }
        }

        #endregion

        #region 默认事件创建

        private ExplorationEventData CreateDefaultTreasureEvent()
        {
            var evt = ScriptableObject.CreateInstance<ExplorationEventData>();
            evt.EventId = "default_treasure";
            evt.EventName = "宝藏发现";
            evt.EventType = ExplorationEventType.TreasureHunt;
            evt.BaseChance = GameConstants.Exploration.DEFAULT_TREASURE_CHANCE;
            evt.StartMessages = new List<string> { "你发现了一个闪闪发光的宝箱！" };

            evt.Rewards = new ExplorationReward
            {
                BaseExperience = 20,
                MinGold = GameConstants.Exploration.MIN_GOLD_REWARD,
                MaxGold = GameConstants.Exploration.MAX_GOLD_REWARD,
                GoldPerLevel = 5f
            };

            return evt;
        }

        private ExplorationEventData CreateDefaultBattleEvent()
        {
            var evt = ScriptableObject.CreateInstance<ExplorationEventData>();
            evt.EventId = "default_battle";
            evt.EventName = "敌人遭遇";
            evt.EventType = ExplorationEventType.EnemyEncounter;
            evt.BaseChance = GameConstants.Exploration.DEFAULT_BATTLE_CHANCE;
            evt.StartMessages = new List<string> { "一个敌人突然出现了！" };

            return evt;
        }

        private ExplorationEventData CreateDefaultItemEvent()
        {
            var evt = ScriptableObject.CreateInstance<ExplorationEventData>();
            evt.EventId = "default_item";
            evt.EventName = "物品发现";
            evt.EventType = ExplorationEventType.ItemDiscovery;
            evt.BaseChance = GameConstants.Exploration.DEFAULT_ITEM_CHANCE;
            evt.StartMessages = new List<string> { "你发现了一些有用的物品！" };

            evt.Rewards = new ExplorationReward
            {
                BaseExperience = 10,
                ItemRewards = new List<ItemReward>
                {
                    new ItemReward { Quantity = 1, DropChance = 100f }
                }
            };

            return evt;
        }

        private ExplorationEventData CreateDefaultNothingEvent()
        {
            var evt = ScriptableObject.CreateInstance<ExplorationEventData>();
            evt.EventId = "default_nothing";
            evt.EventName = "一无所获";
            evt.EventType = ExplorationEventType.NothingFound;
            evt.BaseChance = GameConstants.Exploration.DEFAULT_NOTHING_CHANCE;
            evt.StartMessages = new List<string> { "这次探索没有什么收获..." };

            evt.Rewards = new ExplorationReward
            {
                BaseExperience = GameConstants.Exploration.NOTHING_FOUND_EXP
            };

            return evt;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("测试探索")]
        private void TestExploration()
        {
            StartExploration();
            var result = ProcessExploration();
            if (result != null)
            {
                Debug.Log($"探索结果: {result.Type}, 消息: {result.Message}");
            }
        }

        [ContextMenu("清理冷却")]
        private void ClearCooldowns()
        {
            m_EventCooldowns.Clear();
            Debug.Log("所有事件冷却已清理");
        }

        [ContextMenu("重新加载事件")]
        private void ReloadEvents()
        {
            LoadExplorationEvents();
        }
#endif
    }

    /// <summary>
    /// 探索会话数据
    /// </summary>
    [System.Serializable]
    public class ExplorationSession
    {
        public string SessionId;
        public float StartTime;
        public float EndTime;
        public float Duration;
        public int PlayerLevel;
        public int PlayerLuck;
        public ExplorationResult Result;
    }

    /// <summary>
    /// 探索结果类型
    /// </summary>
    public enum ExplorationResultType
    {
        TreasureFound,      // 发现宝藏
        BattleEncounter,    // 遭遇战斗
        ItemFound,          // 发现物品
        NothingFound,       // 一无所获
        MerchantEncounter,  // 遇到商人
        SpecialEvent        // 特殊事件
    }

    /// <summary>
    /// 探索结果
    /// </summary>
    [System.Serializable]
    public class ExplorationResult
    {
        public string EventId;
        public string EventName;
        public ExplorationResultType Type;
        public string Message;
        public int ExperienceReward;
        public int GoldReward;
        public int HealthLoss;
        public List<ItemData> ItemsFound = new List<ItemData>();
        public CharacterStats EncounteredEnemy;
        public System.DateTime Timestamp;
    }

    /// <summary>
    /// 物品奖励配置
    /// </summary>
    [System.Serializable]
    public class ItemReward
    {
        public int Quantity = 1;
        [Range(0f, 100f)]
        public float DropChance = 50f;
        [Range(0f, 1f)]
        public float QualityBonus = 0f;
        public ItemType SpecificType = ItemType.Equipment;
        public ItemRarity MinRarity = ItemRarity.Common;
        public ItemRarity MaxRarity = ItemRarity.Legendary;
    }

    /// <summary>
    /// 探索奖励配置
    /// </summary>
    [System.Serializable]
    public class ExplorationReward
    {
        [Header("基础奖励")]
        public int BaseExperience;
        public int MinGold;
        public int MaxGold;
        public float GoldPerLevel;

        [Header("物品奖励")]
        public List<ItemReward> ItemRewards = new List<ItemReward>();

        [Header("特殊奖励")]
        public bool GrantsSkillPoint;
        public bool GrantsAttributePoint;
        public string SpecialEffect;
    }
}