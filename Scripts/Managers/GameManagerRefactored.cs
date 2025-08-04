using System;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 重构后的游戏主管理器
    /// 作为协调器，通过服务来管理游戏的各个方面
    /// </summary>
    public class GameManagerRefactored : MonoBehaviour
    {
        #region 单例模式
        private static GameManagerRefactored s_Instance;
        public static GameManagerRefactored Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<GameManagerRefactored>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        s_Instance = go.AddComponent<GameManagerRefactored>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }
        #endregion

        #region 组件引用
        [Header("核心组件")]
        [SerializeField] private PlayerDataManager m_PlayerDataManager;
        [SerializeField] private InventorySystem m_InventorySystem;
        [SerializeField] private EquipmentManager m_EquipmentManager;
        [SerializeField] private BattleSystem m_BattleSystem;
        [SerializeField] private RandomItemGenerator m_ItemGenerator;
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
        #endregion

        #region 游戏状态
        [Header("游戏状态")]
        [SerializeField] private bool m_IsGameInitialized = false;
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
        #endregion

        #region 事件（向后兼容）
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnGameMessage;
        public event Action<int> OnPlayerLevelChanged;
        public event Action<int> OnGoldChanged;
        #endregion

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameManager();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (!m_IsGameInitialized)
            {
                StartNewGame();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region 初始化方法

        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        private void InitializeGameManager()
        {
            // 确保服务已初始化
            if (GameServiceBootstrapper.Instance == null || !GameServiceBootstrapper.Instance.IsInitialized)
            {
                var bootstrapper = FindObjectOfType<GameServiceBootstrapper>();
                if (bootstrapper == null)
                {
                    var go = new GameObject("GameServiceBootstrapper");
                    bootstrapper = go.AddComponent<GameServiceBootstrapper>();
                    DontDestroyOnLoad(go);
                }
                bootstrapper.InitializeServices();
            }

            // 获取服务引用
            InitializeServices();

            // 查找或创建核心组件
            FindOrCreateCoreComponents();

            // 订阅事件
            SubscribeToEvents();

            m_LoggingService?.Log("游戏管理器初始化完成", LogLevel.Info, "GameManager");
        }

        /// <summary>
        /// 初始化服务引用
        /// </summary>
        private void InitializeServices()
        {
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
            m_GameStateService = GameServiceBootstrapper.GetService<IGameStateService>();
            m_StatisticsService = GameServiceBootstrapper.GetService<IStatisticsService>();
            m_ExplorationService = GameServiceBootstrapper.GetService<IExplorationService>();
            m_SaveService = GameServiceBootstrapper.GetService<ISaveService>();
            m_UIUpdateService = GameServiceBootstrapper.GetService<IUIUpdateService>();

            m_LoggingService?.Log("游戏服务初始化完成", LogLevel.Debug, "GameManager");
        }

        /// <summary>
        /// 查找或创建核心组件
        /// </summary>
        private void FindOrCreateCoreComponents()
        {
            // PlayerDataManager
            if (m_PlayerDataManager == null)
            {
                m_PlayerDataManager = FindObjectOfType<PlayerDataManager>();
                if (m_PlayerDataManager == null)
                {
                    GameObject playerDataGO = new GameObject("PlayerDataManager");
                    playerDataGO.transform.SetParent(transform);
                    m_PlayerDataManager = playerDataGO.AddComponent<PlayerDataManager>();
                }
            }

            // InventorySystem
            if (m_InventorySystem == null)
            {
                m_InventorySystem = FindObjectOfType<InventorySystem>();
                if (m_InventorySystem == null)
                {
                    GameObject inventoryGO = new GameObject("InventorySystem");
                    inventoryGO.transform.SetParent(transform);
                    m_InventorySystem = inventoryGO.AddComponent<InventorySystem>();
                }
            }

            // EquipmentManager
            if (m_EquipmentManager == null)
            {
                m_EquipmentManager = FindObjectOfType<EquipmentManager>();
                if (m_EquipmentManager == null)
                {
                    GameObject equipmentGO = new GameObject("EquipmentManager");
                    equipmentGO.transform.SetParent(transform);
                    m_EquipmentManager = equipmentGO.AddComponent<EquipmentManager>();
                }
            }

            // BattleSystem
            if (m_BattleSystem == null)
            {
                m_BattleSystem = FindObjectOfType<BattleSystem>();
                if (m_BattleSystem == null)
                {
                    GameObject battleGO = new GameObject("BattleSystem");
                    battleGO.transform.SetParent(transform);
                    m_BattleSystem = battleGO.AddComponent<BattleSystem>();
                }
            }

            // RandomItemGenerator
            if (m_ItemGenerator == null)
            {
                m_ItemGenerator = RandomItemGenerator.Instance;
            }
        }

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

            // 装备管理器事件
            if (m_EquipmentManager != null)
            {
                m_EquipmentManager.OnStatsChanged += OnPlayerStatsChanged;
            }

            // 探索事件
            if (m_ExplorationService != null)
            {
                m_ExplorationService.OnExplorationCompleted += OnExplorationCompleted;
            }

            // 全局事件
            if (m_EventService != null)
            {
                m_EventService.AddListener(GameEventType.GameMessage, OnGameMessageEvent);
            }
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

            if (m_EquipmentManager != null)
            {
                m_EquipmentManager.OnStatsChanged -= OnPlayerStatsChanged;
            }

            if (m_ExplorationService != null)
            {
                m_ExplorationService.OnExplorationCompleted -= OnExplorationCompleted;
            }

            if (m_EventService != null)
            {
                m_EventService.RemoveListener(GameEventType.GameMessage, OnGameMessageEvent);
            }
        }

        #endregion

        #region 游戏控制方法

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            m_LoggingService?.Log("开始新游戏", LogLevel.Info, "GameManager");

            // 创建新角色
            m_PlayerDataManager?.CreateNewCharacter();

            // 给予初始物品
            GiveStartingItems();

            // 设置游戏状态
            m_GameStateService?.ChangeState(GameState.Exploring);

            m_IsGameInitialized = true;

            // 发送游戏消息
            SendGameMessage("欢迎来到仙侠世界！开始你的修仙之路吧！");
        }

        /// <summary>
        /// 探索
        /// </summary>
        public void Explore()
        {
            if (CurrentState != GameState.Exploring)
            {
                m_LoggingService?.Log("当前状态不允许探索", LogLevel.Warning, "GameManager");
                return;
            }

            m_ExplorationService?.StartExploration();
            
            // 处理探索结果
            var result = m_ExplorationService?.ProcessExploration();
            if (result != null)
            {
                ProcessExplorationResult(result);
            }
        }

        /// <summary>
        /// 改变游戏状态（向后兼容）
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            m_GameStateService?.ChangeState(newState);
        }

        /// <summary>
        /// 添加金钱（向后兼容）
        /// </summary>
        public void AddGold(int amount)
        {
            m_PlayerDataManager?.AddGold(amount);
        }

        /// <summary>
        /// 花费金钱（向后兼容）
        /// </summary>
        public bool SpendGold(int amount)
        {
            return m_PlayerDataManager?.SpendGold(amount) ?? false;
        }

        /// <summary>
        /// 发送游戏消息
        /// </summary>
        public void SendGameMessage(string message)
        {
            m_EventService?.TriggerEvent(GameEventType.GameMessage, 
                new GameMessageEventData(message, MessageType.Normal));
        }

        #endregion

        #region 存档系统

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame()
        {
            if (m_SaveService != null)
            {
                bool success = m_SaveService.SaveGame(0, "自动存档");
                if (success)
                {
                    SendGameMessage("游戏已保存！");
                }
            }
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame()
        {
            if (m_SaveService != null)
            {
                bool success = m_SaveService.LoadGame(0);
                if (success)
                {
                    SendGameMessage("游戏数据加载完成！");
                }
                else
                {
                    SendGameMessage("没有找到存档数据。");
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 给予初始物品
        /// </summary>
        private void GiveStartingItems()
        {
            if (m_InventorySystem == null || m_ItemGenerator == null) return;

            try
            {
                // 初始装备
                EquipmentData startingWeapon = EquipmentData.GenerateRandomEquipment(
                    EquipmentType.Weapon, ItemRarity.Common, 1);
                EquipmentData startingArmor = EquipmentData.GenerateRandomEquipment(
                    EquipmentType.Armor, ItemRarity.Common, 1);
                
                m_InventorySystem.AddItem(startingWeapon);
                m_InventorySystem.AddItem(startingArmor);

                // 初始消耗品
                for (int i = 0; i < 3; i++)
                {
                    ConsumableData startingPotion = ConsumableData.GenerateRandomConsumable(
                        ItemRarity.Common, 1);
                    m_InventorySystem.AddItem(startingPotion);
                }

                SendGameMessage("获得了一些初始装备和物品！");
                m_LoggingService?.Log("给予初始物品完成", LogLevel.Debug, "GameManager");
            }
            catch (Exception e)
            {
                m_LoggingService?.LogException(e, "GameManager");
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

            SendGameMessage(result.Message);
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
        }

        private void ProcessBattleResult(ExplorationResult result)
        {
            if (result.EncounteredEnemy != null && m_BattleSystem != null)
            {
                CharacterStats currentStats = m_EquipmentManager?.TotalStats ?? PlayerStats;
                var enemy = new BattleParticipant(
                    result.EncounteredEnemy.EnemyName,
                    result.EncounteredEnemy.CreateStatsForLevel(PlayerStats?.Level ?? 1),
                    false);

                // 这里应该启动战斗，但需要适配新的战斗系统
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

        #endregion

        #region 事件处理方法

        private void OnGameStateChangedInternal(GameState previous, GameState current)
        {
            OnGameStateChanged?.Invoke(current);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
        }

        private void OnPlayerLevelChangedInternal(int newLevel)
        {
            OnPlayerLevelChanged?.Invoke(newLevel);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerStats);
            SendGameMessage($"恭喜！等级提升到 {newLevel} 级！");
        }

        private void OnGoldChangedInternal(int newGold)
        {
            OnGoldChanged?.Invoke(newGold);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
        }

        private void OnBattleStarted(BattleParticipant player, BattleParticipant enemy)
        {
            m_GameStateService?.ChangeState(GameState.Battle);
            SendGameMessage($"与 {enemy.Name} 的战斗开始了！");
        }

        private void OnBattleEnded(BattleResult result, BattleParticipant winner)
        {
            switch (result)
            {
                case BattleResult.Victory:
                    m_StatisticsService?.RecordBattleWin();
                    SendGameMessage("战斗胜利！继续你的探索之路吧！");
                    break;
                case BattleResult.Defeat:
                    m_StatisticsService?.RecordBattleDefeat();
                    SendGameMessage("战斗失败了...休息一下再继续吧。");
                    break;
                case BattleResult.Escape:
                    SendGameMessage("成功逃脱了战斗！");
                    break;
            }

            m_GameStateService?.ChangeState(GameState.Exploring);
        }

        private void OnPlayerStatsChanged()
        {
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerStats);
        }

        private void OnExplorationCompleted(ExplorationResult result)
        {
            // 探索完成的额外处理
            m_UIUpdateService?.RequestUpdate(UIUpdateType.Statistics);
        }

        private void OnGameMessageEvent(GameEventData eventData)
        {
            if (eventData is GameMessageEventData messageData)
            {
                OnGameMessage?.Invoke(messageData.Message);
                m_UIUpdateService?.RequestUpdate(UIUpdateType.Messages, messageData);
            }
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("开始探索")]
        private void TestExplore()
        {
            Explore();
        }

        [ContextMenu("添加1000灵石")]
        private void TestAddGold()
        {
            AddGold(1000);
        }

        [ContextMenu("提升等级")]
        private void TestLevelUp()
        {
            m_PlayerDataManager?.GainExperience(PlayerStats?.ExperienceToNext ?? 100);
        }

        [ContextMenu("打印游戏信息")]
        private void PrintGameInfo()
        {
            Debug.Log($"=== 游戏信息 ===");
            Debug.Log($"游戏状态: {CurrentState}");
            Debug.Log($"玩家: {PlayerName}");
            Debug.Log($"等级: {PlayerStats?.Level}");
            Debug.Log($"灵石: {Gold}");
            Debug.Log($"探索次数: {ExplorationCount}");
            Debug.Log($"战斗胜利: {BattleWins}");
            Debug.Log($"发现宝藏: {TreasuresFound}");
        }

        [ContextMenu("重新初始化")]
        private void ReinitializeGameManager()
        {
            UnsubscribeFromEvents();
            InitializeGameManager();
        }
#endif

        #endregion
    }
}