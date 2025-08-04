using System;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 优化的游戏管理器
    /// 使用新的架构，高性能，低耦合，模块化设计
    /// </summary>
    public class OptimizedGameManager : OptimizedSingleton<OptimizedGameManager>, IGameManager
    {
        #region 配置和状态

        [Header("启动配置")]
        [SerializeField] private bool m_AutoInitialize = true;
        [SerializeField] private bool m_UseMessageConfig = true;
        [SerializeField] private MessageConfig m_MessageConfig;

        [Header("调试设置")]
        [SerializeField] private bool m_EnableDebugMode = false;
        [SerializeField] private bool m_ShowPerformanceStats = false;

        private bool m_IsInitialized = false;
        private float m_InitializationTime = 0f;

        #endregion

        #region 服务引用

        private IConfigService m_ConfigService;
        private IEventService m_EventService;
        private ILoggingService m_LoggingService;
        private IGameStateService m_GameStateService;
        private IStatisticsService m_StatisticsService;
        private IExplorationService m_ExplorationService;
        private ISaveService m_SaveService;
        private IUIUpdateService m_UIUpdateService;
        private IGameDataService m_GameDataService;
        private IAudioService m_AudioService;

        #endregion

        #region 核心组件

        private PlayerDataManager m_PlayerDataManager;
        private InventorySystem m_InventorySystem;
        private EquipmentManager m_EquipmentManager;
        private BattleSystem m_BattleSystem;

        #endregion

        #region 公共属性（向后兼容）

        public GameState CurrentState => m_GameStateService?.CurrentState ?? GameState.MainMenu;
        public string PlayerName => m_PlayerDataManager?.PlayerName ?? "无名道友";
        public CharacterStats PlayerStats => m_PlayerDataManager?.PlayerStats;
        public int Gold => m_PlayerDataManager?.Gold ?? 0;
        public int ExplorationCount => m_StatisticsService?.ExplorationCount ?? 0;
        public int BattleWins => m_StatisticsService?.BattleWins ?? 0;
        public int TreasuresFound => m_StatisticsService?.TreasuresFound ?? 0;

        public InventorySystem InventorySystem => m_InventorySystem;
        public EquipmentManager EquipmentManager => m_EquipmentManager;
        public BattleSystem BattleSystem => m_BattleSystem;
        public PlayerDataManager PlayerDataManager => m_PlayerDataManager;

        public bool IsInitialized => m_IsInitialized;
        public float InitializationTime => m_InitializationTime;

        #endregion

        #region 事件（向后兼容）

        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnGameMessage;
        public event Action<int> OnPlayerLevelChanged;
        public event Action<int> OnGoldChanged;
        public event Action OnGameInitialized;

        #endregion

        #region Unity生命周期

        protected override void OnSingletonAwake()
        {
            if (m_AutoInitialize)
            {
                InitializeAsync();
            }
        }

        private void Start()
        {
            if (!m_IsInitialized && m_AutoInitialize)
            {
                CompleteInitialization();
            }
        }

        protected override void OnSingletonDestroy()
        {
            CleanupManager();
        }

        #endregion

        #region 初始化系统

        /// <summary>
        /// 异步初始化游戏管理器
        /// </summary>
        public async void InitializeAsync()
        {
            if (m_IsInitialized) return;

            float startTime = Time.realtimeSinceStartup;
            GameLog.Info("开始初始化优化的游戏管理器", "OptimizedGameManager");

            try
            {
                // 1. 初始化服务系统
                await InitializeServicesAsync();

                // 2. 初始化消息系统
                InitializeMessageSystem();

                // 3. 初始化核心组件
                InitializeCoreComponents();

                // 4. 订阅事件
                SubscribeToEvents();

                // 5. 设置初始状态
                SetupInitialState();

                m_InitializationTime = Time.realtimeSinceStartup - startTime;
                m_IsInitialized = true;

                GameLog.Info($"游戏管理器初始化完成，耗时: {m_InitializationTime:F3}秒", "OptimizedGameManager");
                
                OnGameInitialized?.Invoke();
                SendMessage("system_initialized", "游戏系统初始化完成！");
            }
            catch (Exception e)
            {
                GameLog.Error($"游戏管理器初始化失败: {e.Message}", "OptimizedGameManager");
                throw;
            }
        }

        /// <summary>
        /// 初始化服务系统
        /// </summary>
        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            // 确保服务引导程序存在
            if (!GameServiceBootstrapper.Instance.IsInitialized)
            {
                GameServiceBootstrapper.Instance.InitializeServices();
                
                // 等待一帧确保服务完全初始化
                await System.Threading.Tasks.Task.Yield();
            }

            // 获取服务引用
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
            m_GameStateService = GameServiceBootstrapper.GetService<IGameStateService>();
            m_StatisticsService = GameServiceBootstrapper.GetService<IStatisticsService>();
            m_ExplorationService = GameServiceBootstrapper.GetService<IExplorationService>();
            m_SaveService = GameServiceBootstrapper.GetService<ISaveService>();
            m_UIUpdateService = GameServiceBootstrapper.GetService<IUIUpdateService>();
            m_GameDataService = GameServiceBootstrapper.GetService<IGameDataService>();
            m_AudioService = GameServiceBootstrapper.GetService<IAudioService>();

            ValidateServices();
        }

        /// <summary>
        /// 初始化消息系统
        /// </summary>
        private void InitializeMessageSystem()
        {
            if (m_UseMessageConfig)
            {
                MessageManager.Initialize(m_MessageConfig);
                GameLog.Debug("消息系统初始化完成", "OptimizedGameManager");
            }
        }

        /// <summary>
        /// 初始化核心组件
        /// </summary>
        private void InitializeCoreComponents()
        {
            // 使用ComponentRegistry获取组件，避免FindObjectOfType
            m_PlayerDataManager = ComponentRegistry.GetOrCreateComponent<PlayerDataManager>("PlayerDataManager");
            m_InventorySystem = ComponentRegistry.GetOrCreateComponent<InventorySystem>("InventorySystem");
            m_EquipmentManager = ComponentRegistry.GetOrCreateComponent<EquipmentManager>("EquipmentManager");
            m_BattleSystem = ComponentRegistry.GetOrCreateComponent<BattleSystem>("BattleSystem");

            // 设置组件层级
            if (m_PlayerDataManager.transform.parent == null)
                m_PlayerDataManager.transform.SetParent(transform);
            if (m_InventorySystem.transform.parent == null)
                m_InventorySystem.transform.SetParent(transform);
            if (m_EquipmentManager.transform.parent == null)
                m_EquipmentManager.transform.SetParent(transform);
            if (m_BattleSystem.transform.parent == null)
                m_BattleSystem.transform.SetParent(transform);

            GameLog.Debug("核心组件初始化完成", "OptimizedGameManager");
        }

        /// <summary>
        /// 验证服务可用性
        /// </summary>
        private void ValidateServices()
        {
            var missingServices = new System.Collections.Generic.List<string>();

            if (m_ConfigService == null) missingServices.Add("ConfigService");
            if (m_EventService == null) missingServices.Add("EventService");
            if (m_LoggingService == null) missingServices.Add("LoggingService");
            if (m_GameStateService == null) missingServices.Add("GameStateService");

            if (missingServices.Count > 0)
            {
                string missing = string.Join(", ", missingServices);
                throw new System.InvalidOperationException($"关键服务缺失: {missing}");
            }
        }

        /// <summary>
        /// 完成初始化
        /// </summary>
        private void CompleteInitialization()
        {
            if (!m_IsInitialized)
            {
                InitializeAsync();
                return;
            }

            // 开始新游戏或加载存档
            if (HasSaveData())
            {
                LoadLastGame();
            }
            else
            {
                StartNewGame();
            }
        }

        #endregion

        #region 游戏控制

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            if (!m_IsInitialized)
            {
                GameLog.Warning("游戏管理器未初始化，无法开始新游戏", "OptimizedGameManager");
                return;
            }

            GameLog.Info("开始新游戏", "OptimizedGameManager");

            // 创建新角色
            m_PlayerDataManager?.CreateNewCharacter();

            // 给予初始物品
            GiveStartingItems();

            // 设置游戏状态
            m_GameStateService?.ChangeState(GameState.Exploring);

            // 播放欢迎消息
            SendMessage("welcome");

            // 播放背景音乐
            m_AudioService?.PlayMusic("main_theme");

            GameLog.Info("新游戏开始完成", "OptimizedGameManager");
        }

        /// <summary>
        /// 探索
        /// </summary>
        public void Explore()
        {
            if (!CanExplore())
            {
                SendMessage("error_cannot_explore", "当前无法进行探索");
                return;
            }

            // 发送探索开始消息
            SendMessage(MessageCategory.Exploration);

            // 执行探索
            m_ExplorationService?.StartExploration();
            var result = m_ExplorationService?.ProcessExploration();
            
            if (result != null)
            {
                ProcessExplorationResult(result);
            }

            // 更新统计
            m_StatisticsService?.RecordExploration();
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public bool SaveGame(int slotIndex = 0, string saveName = "自动存档")
        {
            if (!m_IsInitialized)
            {
                SendMessage("error_save");
                return false;
            }

            bool success = m_SaveService?.SaveGame(slotIndex, saveName) ?? false;
            
            if (success)
            {
                SendMessage("game_saved");
                GameLog.Info($"游戏保存成功: 槽位{slotIndex}", "OptimizedGameManager");
            }
            else
            {
                SendMessage("error_save");
                GameLog.Error($"游戏保存失败: 槽位{slotIndex}", "OptimizedGameManager");
            }

            return success;
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public bool LoadGame(int slotIndex = 0)
        {
            bool success = m_SaveService?.LoadGame(slotIndex) ?? false;
            
            if (success)
            {
                SendMessage("game_loaded");
                GameLog.Info($"游戏加载成功: 槽位{slotIndex}", "OptimizedGameManager");
            }
            else
            {
                SendMessage("error_load");
                GameLog.Error($"游戏加载失败: 槽位{slotIndex}", "OptimizedGameManager");
            }

            return success;
        }

        /// <summary>
        /// 改变游戏状态
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            m_GameStateService?.ChangeState(newState);
        }

        #endregion

        #region 消息系统

        /// <summary>
        /// 发送消息（使用消息ID）
        /// </summary>
        public void SendMessage(string messageId, params object[] args)
        {
            string message = MessageManager.GetMessage(messageId, args);
            SendMessageInternal(message, MessageType.Normal);
        }

        /// <summary>
        /// 发送分类消息
        /// </summary>
        public void SendMessage(MessageCategory category, params object[] args)
        {
            string message = MessageManager.GetRandomMessage(category, args);
            SendMessageInternal(message, GetMessageTypeFromCategory(category));
        }

        /// <summary>
        /// 发送自定义消息
        /// </summary>
        public void SendMessage(string message, MessageType type = MessageType.Normal)
        {
            SendMessageInternal(message, type);
        }

        private void SendMessageInternal(string message, MessageType type)
        {
            // 触发事件
            OnGameMessage?.Invoke(message);

            // 通过事件系统发送
            m_EventService?.TriggerEvent(GameEventType.GameMessage, 
                new GameMessageEventData(message, type));

            // 更新UI
            m_UIUpdateService?.RequestUpdate(UIUpdateType.Messages, new { Message = message, Type = type });

            if (m_EnableDebugMode)
            {
                GameLog.Info($"游戏消息: {message}", "OptimizedGameManager");
            }
        }

        private MessageType GetMessageTypeFromCategory(MessageCategory category)
        {
            return category switch
            {
                MessageCategory.Battle => MessageType.Warning,
                MessageCategory.Treasure => MessageType.Success,
                MessageCategory.Achievement => MessageType.Important,
                MessageCategory.Error => MessageType.Error,
                _ => MessageType.Normal
            };
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 游戏状态事件
            if (m_GameStateService != null)
            {
                m_GameStateService.OnStateChanged += OnGameStateChangedInternal;
            }

            // 玩家数据事件
            if (m_PlayerDataManager != null)
            {
                m_PlayerDataManager.OnLevelChanged += OnPlayerLevelChangedInternal;
                m_PlayerDataManager.OnGoldChanged += OnGoldChangedInternal;
            }

            // 战斗系统事件
            if (m_BattleSystem != null)
            {
                m_BattleSystem.OnBattleStarted += OnBattleStarted;
                m_BattleSystem.OnBattleEnded += OnBattleEnded;
            }

            // 探索事件
            if (m_ExplorationService != null)
            {
                m_ExplorationService.OnExplorationCompleted += OnExplorationCompleted;
            }

            GameLog.Debug("事件订阅完成", "OptimizedGameManager");
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (m_GameStateService != null)
            {
                m_GameStateService.OnStateChanged -= OnGameStateChangedInternal;
            }

            if (m_PlayerDataManager != null)
            {
                m_PlayerDataManager.OnLevelChanged -= OnPlayerLevelChangedInternal;
                m_PlayerDataManager.OnGoldChanged -= OnGoldChangedInternal;
            }

            if (m_BattleSystem != null)
            {
                m_BattleSystem.OnBattleStarted -= OnBattleStarted;
                m_BattleSystem.OnBattleEnded -= OnBattleEnded;
            }

            if (m_ExplorationService != null)
            {
                m_ExplorationService.OnExplorationCompleted -= OnExplorationCompleted;
            }
        }

        private void OnGameStateChangedInternal(GameState previous, GameState current)
        {
            OnGameStateChanged?.Invoke(current);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
            
            GameLog.Debug($"游戏状态变更: {previous} -> {current}", "OptimizedGameManager");
        }

        private void OnPlayerLevelChangedInternal(int newLevel)
        {
            OnPlayerLevelChanged?.Invoke(newLevel);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerStats);
            SendMessage("level_up", newLevel);
            
            // 播放升级音效
            m_AudioService?.PlaySFX("level_up");
        }

        private void OnGoldChangedInternal(int newGold)
        {
            OnGoldChanged?.Invoke(newGold);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
        }

        private void OnBattleStarted(BattleParticipant player, BattleParticipant enemy)
        {
            m_GameStateService?.ChangeState(GameState.Battle);
            SendMessage("battle_start");
        }

        private void OnBattleEnded(BattleResult result, BattleParticipant winner)
        {
            switch (result)
            {
                case BattleResult.Victory:
                    m_StatisticsService?.RecordBattleWin();
                    SendMessage("battle_victory");
                    break;
                case BattleResult.Defeat:
                    m_StatisticsService?.RecordBattleDefeat();
                    SendMessage("battle_defeat");
                    break;
                case BattleResult.Escape:
                    SendMessage("battle_escape");
                    break;
            }

            m_GameStateService?.ChangeState(GameState.Exploring);
        }

        private void OnExplorationCompleted(ExplorationResult result)
        {
            m_UIUpdateService?.RequestUpdate(UIUpdateType.Statistics);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            m_GameStateService?.ChangeState(GameState.MainMenu);
        }

        /// <summary>
        /// 检查是否可以探索
        /// </summary>
        private bool CanExplore()
        {
            return m_IsInitialized && 
                   CurrentState == GameState.Exploring && 
                   m_PlayerDataManager?.PlayerStats?.IsAlive == true;
        }

        /// <summary>
        /// 检查是否有存档数据
        /// </summary>
        private bool HasSaveData()
        {
            var saveInfo = m_SaveService?.GetAllSaveInfo();
            return saveInfo != null && saveInfo.Count > 0;
        }

        /// <summary>
        /// 加载最近的游戏
        /// </summary>
        private void LoadLastGame()
        {
            LoadGame(0); // 加载第一个存档槽
        }

        /// <summary>
        /// 给予初始物品
        /// </summary>
        private void GiveStartingItems()
        {
            if (m_InventorySystem == null) return;

            try
            {
                // 使用数据驱动的物品生成
                var generator = ComponentRegistry.GetComponent<DataDrivenItemGenerator>();
                if (generator != null)
                {
                    var startingItems = generator.GenerateItems(3, 1, 0f);
                    foreach (var item in startingItems)
                    {
                        m_InventorySystem.AddItem(item);
                    }
                }
                else
                {
                    // 回退到原始方法
                    var weapon = EquipmentData.GenerateRandomEquipment(EquipmentType.Weapon, ItemRarity.Common, 1);
                    var armor = EquipmentData.GenerateRandomEquipment(EquipmentType.Armor, ItemRarity.Common, 1);
                    
                    m_InventorySystem.AddItem(weapon);
                    m_InventorySystem.AddItem(armor);
                }

                SendMessage("item_obtained", "初始装备");
                GameLog.Debug("初始物品发放完成", "OptimizedGameManager");
            }
            catch (Exception e)
            {
                GameLog.Error($"发放初始物品失败: {e.Message}", "OptimizedGameManager");
            }
        }

        /// <summary>
        /// 处理探索结果
        /// </summary>
        private void ProcessExplorationResult(ExplorationResult result)
        {
            switch (result.Type)
            {
                case ExplorationResultType.TreasureFound:
                    ProcessTreasureResult(result);
                    break;
                case ExplorationResultType.BattleEncounter:
                    ProcessBattleResult(result);
                    break;
                case ExplorationResultType.ItemFound:
                    ProcessItemResult(result);
                    break;
                case ExplorationResultType.NothingFound:
                    ProcessNothingResult(result);
                    break;
            }

            // 发送结果消息
            if (!string.IsNullOrEmpty(result.Message))
            {
                SendMessage(result.Message);
            }
        }

        private void ProcessTreasureResult(ExplorationResult result)
        {
            if (result.GoldReward > 0)
            {
                m_PlayerDataManager?.AddGold(result.GoldReward);
            }

            foreach (var item in result.ItemsFound)
            {
                m_InventorySystem?.AddItem(item);
            }

            m_StatisticsService?.RecordTreasureFound();
            m_AudioService?.PlaySFX("treasure_found");
        }

        private void ProcessBattleResult(ExplorationResult result)
        {
            if (result.EncounteredEnemy != null && m_BattleSystem != null)
            {
                // 这里需要适配战斗系统启动
                m_GameStateService?.ChangeState(GameState.Battle);
            }
        }

        private void ProcessItemResult(ExplorationResult result)
        {
            foreach (var item in result.ItemsFound)
            {
                m_InventorySystem?.AddItem(item);
                m_StatisticsService?.RecordItemCollected();
            }
        }

        private void ProcessNothingResult(ExplorationResult result)
        {
            if (result.ExperienceReward > 0)
            {
                m_PlayerDataManager?.GainExperience(result.ExperienceReward);
            }
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        private void CleanupManager()
        {
            UnsubscribeFromEvents();
            GameLog.Info("优化的游戏管理器已清理", "OptimizedGameManager");
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 获取性能统计
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            var (singletons, multi, total) = ComponentRegistry.GetCacheStatistics();
            var uiStats = m_UIUpdateService as UIUpdateManager;
            var (queueSize, totalProcessed, thisFrame) = uiStats?.GetUpdateStatistics() ?? (0, 0, 0);

            return new PerformanceStats
            {
                InitializationTime = m_InitializationTime,
                ComponentCacheSize = total,
                UIUpdateQueueSize = queueSize,
                TotalUIUpdatesProcessed = totalProcessed,
                IsInitialized = m_IsInitialized
            };
        }

        /// <summary>
        /// 性能统计数据
        /// </summary>
        public struct PerformanceStats
        {
            public float InitializationTime;
            public int ComponentCacheSize;
            public int UIUpdateQueueSize;
            public int TotalUIUpdatesProcessed;
            public bool IsInitialized;
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("强制初始化")]
        private void ForceInitialize()
        {
            m_IsInitialized = false;
            InitializeAsync();
        }

        [ContextMenu("开始探索")]
        private void TestExplore()
        {
            Explore();
        }

        [ContextMenu("保存游戏")]
        private void TestSave()
        {
            SaveGame();
        }

        [ContextMenu("打印性能统计")]
        private void PrintPerformanceStats()
        {
            var stats = GetPerformanceStats();
            Debug.Log($"=== 性能统计 ===\n" +
                     $"初始化时间: {stats.InitializationTime:F3}秒\n" +
                     $"组件缓存大小: {stats.ComponentCacheSize}\n" +
                     $"UI更新队列: {stats.UIUpdateQueueSize}\n" +
                     $"UI更新总数: {stats.TotalUIUpdatesProcessed}\n" +
                     $"初始化状态: {stats.IsInitialized}");
        }

        [ContextMenu("清理组件缓存")]
        private void CleanupComponentCache()
        {
            ComponentRegistry.CleanupInvalidComponents();
        }
#endif

        #endregion
    }

    /// <summary>
    /// 游戏管理器接口
    /// </summary>
    public interface IGameManager
    {
        bool IsInitialized { get; }
        GameState CurrentState { get; }
        CharacterStats PlayerStats { get; }
        
        void StartNewGame();
        void Explore();
        bool SaveGame(int slotIndex = 0, string saveName = "自动存档");
        bool LoadGame(int slotIndex = 0);
        void ChangeGameState(GameState newState);
    }
}