using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XianXiaGame.Core;
using System.Collections.Generic;

namespace XianXiaGame
{
    /// <summary>
    /// 优化的游戏启动器
    /// 使用新的架构，低耦合，高性能
    /// </summary>
    public class OptimizedGameStarter : MonoBehaviour
    {
        #region 配置设置

        [Header("启动配置")]
        [SerializeField] private bool m_AutoStartGame = true;
        [SerializeField] private bool m_EnableDebugMode = true;
        [SerializeField] private bool m_ShowPerformanceInfo = false;
        [SerializeField] private StartupMode m_StartupMode = StartupMode.NewGame;

        [Header("UI测试组件")]
        [SerializeField] private Button m_TestExploreButton;
        [SerializeField] private Button m_TestBattleButton;
        [SerializeField] private Button m_TestInventoryButton;
        [SerializeField] private Button m_SaveGameButton;
        [SerializeField] private Button m_LoadGameButton;
        [SerializeField] private TextMeshProUGUI m_TestLogText;
        [SerializeField] private TextMeshProUGUI m_PerformanceText;

        [Header("高级测试")]
        [SerializeField] private Button m_StressTestButton;
        [SerializeField] private Button m_MemoryTestButton;
        [SerializeField] private Button m_ComponentTestButton;

        #endregion

        #region 服务引用

        private IGameManager m_GameManager;
        private IEventService m_EventService;
        private ILoggingService m_LoggingService;
        private IUIUpdateService m_UIUpdateService;
        private IStatisticsService m_StatisticsService;
        private ISaveService m_SaveService;

        #endregion

        #region 状态管理

        private List<string> m_LogMessages = new List<string>();
        private bool m_IsInitialized = false;
        private float m_StartupTime = 0f;
        private PerformanceMonitor m_PerformanceMonitor;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            m_PerformanceMonitor = new PerformanceMonitor();
            m_StartupTime = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            InitializeAsync();
        }

        private void Update()
        {
            if (m_IsInitialized && m_ShowPerformanceInfo)
            {
                UpdatePerformanceDisplay();
            }
        }

        private void OnDestroy()
        {
            CleanupStarter();
        }

        #endregion

        #region 初始化系统

        /// <summary>
        /// 异步初始化启动器
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                AddLog("开始初始化游戏启动器...");

                // 等待服务系统准备就绪
                await WaitForServicesReady();

                // 获取服务引用
                GetServiceReferences();

                // 验证服务
                ValidateServices();

                // 设置UI
                SetupUI();

                // 订阅事件
                SubscribeToEvents();

                m_IsInitialized = true;
                float initTime = Time.realtimeSinceStartup - m_StartupTime;
                AddLog($"游戏启动器初始化完成！耗时: {initTime:F3}秒");

                // 根据启动模式执行相应操作
                await HandleStartupMode();
            }
            catch (System.Exception e)
            {
                AddLog($"初始化失败: {e.Message}", LogLevel.Error);
                GameLog.Error($"游戏启动器初始化失败: {e.Message}", "OptimizedGameStarter");
            }
        }

        /// <summary>
        /// 等待服务系统准备就绪
        /// </summary>
        private async System.Threading.Tasks.Task WaitForServicesReady()
        {
            int attempts = 0;
            const int maxAttempts = 50; // 5秒超时

            while (attempts < maxAttempts)
            {
                if (GameServiceBootstrapper.Instance != null && GameServiceBootstrapper.Instance.IsInitialized)
                {
                    break;
                }

                await System.Threading.Tasks.Task.Delay(100);
                attempts++;
            }

            if (attempts >= maxAttempts)
            {
                throw new System.TimeoutException("等待服务系统初始化超时");
            }
        }

        /// <summary>
        /// 获取服务引用
        /// </summary>
        private void GetServiceReferences()
        {
            m_GameManager = OptimizedGameManager.Instance;
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
            m_UIUpdateService = GameServiceBootstrapper.GetService<IUIUpdateService>();
            m_StatisticsService = GameServiceBootstrapper.GetService<IStatisticsService>();
            m_SaveService = GameServiceBootstrapper.GetService<ISaveService>();
        }

        /// <summary>
        /// 验证服务可用性
        /// </summary>
        private void ValidateServices()
        {
            var missingServices = new List<string>();

            if (m_GameManager == null) missingServices.Add("GameManager");
            if (m_EventService == null) missingServices.Add("EventService");
            if (m_LoggingService == null) missingServices.Add("LoggingService");

            if (missingServices.Count > 0)
            {
                string missing = string.Join(", ", missingServices);
                throw new System.InvalidOperationException($"关键服务缺失: {missing}");
            }
        }

        /// <summary>
        /// 处理启动模式
        /// </summary>
        private async System.Threading.Tasks.Task HandleStartupMode()
        {
            switch (m_StartupMode)
            {
                case StartupMode.NewGame:
                    if (m_AutoStartGame)
                    {
                        await System.Threading.Tasks.Task.Delay(500); // 短暂延迟确保系统稳定
                        StartNewGame();
                    }
                    break;

                case StartupMode.LoadGame:
                    if (m_SaveService != null && HasSaveData())
                    {
                        LoadLastGame();
                    }
                    else
                    {
                        AddLog("没有找到存档数据，开始新游戏");
                        StartNewGame();
                    }
                    break;

                case StartupMode.MainMenu:
                    // 仅初始化，不自动开始游戏
                    AddLog("游戏已准备就绪，请手动开始");
                    break;
            }
        }

        #endregion

        #region UI设置

        /// <summary>
        /// 设置UI组件
        /// </summary>
        private void SetupUI()
        {
            SetupButton(m_TestExploreButton, "探索", TestExplore);
            SetupButton(m_TestBattleButton, "测试战斗", TestBattle);
            SetupButton(m_TestInventoryButton, "添加物品", TestAddItems);
            SetupButton(m_SaveGameButton, "保存游戏", SaveGame);
            SetupButton(m_LoadGameButton, "加载游戏", LoadGame);
            SetupButton(m_StressTestButton, "压力测试", StressTest);
            SetupButton(m_MemoryTestButton, "内存测试", MemoryTest);
            SetupButton(m_ComponentTestButton, "组件测试", ComponentTest);

            // 设置日志文本初始状态
            if (m_TestLogText != null)
            {
                m_TestLogText.text = "";
            }

            if (m_PerformanceText != null)
            {
                m_PerformanceText.gameObject.SetActive(m_ShowPerformanceInfo);
            }
        }

        /// <summary>
        /// 设置按钮
        /// </summary>
        private void SetupButton(Button button, string text, System.Action action)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => action?.Invoke());

                var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
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
                AddLog("启动器未初始化完成，请稍等...", LogLevel.Warning);
                return;
            }

            if (m_GameManager == null)
            {
                AddLog("游戏管理器不可用！", LogLevel.Error);
                return;
            }

            try
            {
                m_GameManager.StartNewGame();
                AddLog("=== 仙侠探索挖宝游戏开始 ===", LogLevel.Important);
                AddLog("欢迎来到修仙世界！点击'探索'开始你的冒险之旅！");

                // 显示角色信息
                ShowPlayerInfo();
            }
            catch (System.Exception e)
            {
                AddLog($"开始新游戏失败: {e.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame()
        {
            if (!CanPerformAction()) return;

            bool success = m_SaveService?.SaveGame(0, "手动存档") ?? false;
            if (success)
            {
                AddLog("游戏保存成功！", LogLevel.Success);
            }
            else
            {
                AddLog("游戏保存失败！", LogLevel.Error);
            }
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame()
        {
            if (!CanPerformAction()) return;

            bool success = m_SaveService?.LoadGame(0) ?? false;
            if (success)
            {
                AddLog("游戏加载成功！", LogLevel.Success);
                ShowPlayerInfo();
            }
            else
            {
                AddLog("游戏加载失败！", LogLevel.Error);
            }
        }

        #endregion

        #region 测试功能

        /// <summary>
        /// 测试探索
        /// </summary>
        public void TestExplore()
        {
            if (!CanPerformAction()) return;

            try
            {
                m_GameManager.Explore();
                AddLog("开始探索...");
            }
            catch (System.Exception e)
            {
                AddLog($"探索失败: {e.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 测试战斗
        /// </summary>
        public void TestBattle()
        {
            if (!CanPerformAction()) return;

            try
            {
                var battleSystem = m_GameManager.BattleSystem;
                if (battleSystem != null && !battleSystem.IsBattleActive)
                {
                    var playerStats = m_GameManager.EquipmentManager?.TotalStats ?? m_GameManager.PlayerStats;
                    battleSystem.StartBattle(playerStats, m_GameManager.PlayerStats.Level);
                    AddLog("启动测试战斗！", LogLevel.Important);
                }
                else
                {
                    AddLog("已经在战斗中或战斗系统未就绪！", LogLevel.Warning);
                }
            }
            catch (System.Exception e)
            {
                AddLog($"战斗测试失败: {e.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 测试添加物品
        /// </summary>
        public void TestAddItems()
        {
            if (!CanPerformAction()) return;

            try
            {
                var generator = ComponentRegistry.GetComponent<DataDrivenItemGenerator>();
                if (generator != null && m_GameManager?.InventorySystem != null)
                {
                    var items = generator.GenerateItems(3, m_GameManager.PlayerStats.Level, 0.1f);
                    int addedCount = 0;

                    foreach (var item in items)
                    {
                        if (m_GameManager.InventorySystem.AddItem(item))
                        {
                            addedCount++;
                        }
                    }

                    AddLog($"成功添加了 {addedCount}/{items.Count} 个随机物品到背包！", LogLevel.Success);
                }
                else
                {
                    // 回退到原始方法
                    var items = RandomItemGenerator.Instance?.GenerateRandomItems(3, 
                        m_GameManager.PlayerStats.Level, m_GameManager.PlayerStats.Luck);
                    
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            m_GameManager.InventorySystem.AddItem(item);
                        }
                        AddLog($"添加了 {items.Count} 个随机物品（使用旧生成器）", LogLevel.Success);
                    }
                }
            }
            catch (System.Exception e)
            {
                AddLog($"添加物品失败: {e.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 压力测试
        /// </summary>
        public void StressTest()
        {
            if (!CanPerformAction()) return;

            StartCoroutine(PerformStressTest());
        }

        /// <summary>
        /// 内存测试
        /// </summary>
        public void MemoryTest()
        {
            if (!CanPerformAction()) return;

            m_PerformanceMonitor.RunMemoryTest();
            AddLog($"内存测试完成 - 使用内存: {m_PerformanceMonitor.GetMemoryUsage():F2}MB", LogLevel.Info);
        }

        /// <summary>
        /// 组件测试
        /// </summary>
        public void ComponentTest()
        {
            var (singletons, multi, total) = ComponentRegistry.GetCacheStatistics();
            AddLog($"组件缓存统计: 单例={singletons}, 多实例类型={multi}, 总追踪={total}", LogLevel.Info);
            
            ComponentRegistry.CleanupInvalidComponents();
            AddLog("组件缓存清理完成", LogLevel.Success);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnGameMessage += OnGameMessage;
                m_GameManager.OnGameStateChanged += OnGameStateChanged;
                m_GameManager.OnPlayerLevelChanged += OnPlayerLevelChanged;
            }

            // 通过事件服务订阅
            if (m_EventService != null)
            {
                m_EventService.Subscribe(GameEventType.GameMessage, OnGameMessageEvent);
                m_EventService.Subscribe(GameEventType.PlayerLevelUp, OnPlayerLevelUpEvent);
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnGameMessage -= OnGameMessage;
                m_GameManager.OnGameStateChanged -= OnGameStateChanged;
                m_GameManager.OnPlayerLevelChanged -= OnPlayerLevelChanged;
            }

            if (m_EventService != null)
            {
                m_EventService.Unsubscribe(GameEventType.GameMessage, OnGameMessageEvent);
                m_EventService.Unsubscribe(GameEventType.PlayerLevelUp, OnPlayerLevelUpEvent);
            }
        }

        private void OnGameMessage(string message)
        {
            AddLog($"[游戏] {message}");
        }

        private void OnGameStateChanged(GameState newState)
        {
            AddLog($"游戏状态变更为: {GetGameStateText(newState)}", LogLevel.Info);
        }

        private void OnPlayerLevelChanged(int newLevel)
        {
            AddLog($"🎉 等级提升到 {newLevel} 级！", LogLevel.Important);
        }

        private void OnGameMessageEvent(GameEventData eventData)
        {
            if (eventData is GameMessageEventData messageData)
            {
                AddLog($"[事件] {messageData.Message}");
            }
        }

        private void OnPlayerLevelUpEvent(GameEventData eventData)
        {
            if (eventData is PlayerLevelUpEventData levelData)
            {
                AddLog($"🌟 角色升级到 {levelData.NewLevel} 级！获得新的力量！", LogLevel.Important);
            }
        }

        #endregion

        #region 日志系统

        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string message, LogLevel level = LogLevel.Normal)
        {
            if (m_EnableDebugMode)
            {
                Debug.Log($"[游戏启动器] {message}");
            }

            // 记录到日志服务
            m_LoggingService?.Log(message, level.ToString(), "GameStarter");

            // 添加到本地日志
            string timestamp = System.DateTime.Now.ToString(GameConstants.Debug.LOG_DATE_FORMAT);
            string logEntry = $"[{timestamp}] {GetLogLevelIcon(level)} {message}";
            
            m_LogMessages.Add(logEntry);

            // 限制日志数量
            while (m_LogMessages.Count > GameConstants.Debug.MAX_LOG_ENTRIES)
            {
                m_LogMessages.RemoveAt(0);
            }

            // 更新UI
            UpdateLogDisplay();
        }

        /// <summary>
        /// 更新日志显示
        /// </summary>
        private void UpdateLogDisplay()
        {
            if (m_TestLogText != null)
            {
                // 只显示最近的消息
                int displayCount = Mathf.Min(20, m_LogMessages.Count);
                int startIndex = Mathf.Max(0, m_LogMessages.Count - displayCount);
                
                var displayMessages = m_LogMessages.GetRange(startIndex, displayCount);
                m_TestLogText.text = string.Join("\n", displayMessages);
            }
        }

        /// <summary>
        /// 获取日志级别图标
        /// </summary>
        private string GetLogLevelIcon(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => "❌",
                LogLevel.Warning => "⚠️",
                LogLevel.Success => "✅",
                LogLevel.Important => "🔥",
                LogLevel.Info => "ℹ️",
                _ => "📝"
            };
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 更新性能显示
        /// </summary>
        private void UpdatePerformanceDisplay()
        {
            if (m_PerformanceText != null && Time.frameCount % 30 == 0) // 每30帧更新一次
            {
                var stats = (m_GameManager as OptimizedGameManager)?.GetPerformanceStats();
                string perfText = "=== 性能监控 ===\n";
                
                if (stats.HasValue)
                {
                    perfText += $"初始化时间: {stats.Value.InitializationTime:F3}s\n";
                    perfText += $"组件缓存: {stats.Value.ComponentCacheSize}\n";
                    perfText += $"UI队列: {stats.Value.UIUpdateQueueSize}\n";
                }
                
                perfText += $"内存使用: {m_PerformanceMonitor.GetMemoryUsage():F1}MB\n";
                perfText += $"FPS: {m_PerformanceMonitor.GetFPS():F1}\n";
                perfText += $"帧时间: {m_PerformanceMonitor.GetFrameTime():F2}ms";
                
                m_PerformanceText.text = perfText;
            }
        }

        /// <summary>
        /// 执行压力测试
        /// </summary>
        private System.Collections.IEnumerator PerformStressTest()
        {
            AddLog("开始压力测试...", LogLevel.Important);
            
            float startTime = Time.realtimeSinceStartup;
            int operations = 0;

            // 执行1000次操作
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 0)
                {
                    AddLog($"压力测试进度: {i}/1000");
                    yield return null; // 让出一帧
                }

                // 模拟各种操作
                ComponentRegistry.GetComponent<InventorySystem>();
                m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerStats);
                operations++;
            }

            float duration = Time.realtimeSinceStartup - startTime;
            AddLog($"压力测试完成！执行 {operations} 次操作，耗时: {duration:F3}秒", LogLevel.Success);
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 检查是否可以执行操作
        /// </summary>
        private bool CanPerformAction()
        {
            if (!m_IsInitialized)
            {
                AddLog("启动器未初始化完成", LogLevel.Warning);
                return false;
            }

            if (m_GameManager == null)
            {
                AddLog("游戏管理器不可用", LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否有存档
        /// </summary>
        private bool HasSaveData()
        {
            var saveInfo = m_SaveService?.GetAllSaveInfo();
            return saveInfo != null && saveInfo.Count > 0;
        }

        /// <summary>
        /// 加载最近游戏
        /// </summary>
        private void LoadLastGame()
        {
            LoadGame();
        }

        /// <summary>
        /// 显示玩家信息
        /// </summary>
        private void ShowPlayerInfo()
        {
            if (m_GameManager?.PlayerStats != null)
            {
                var stats = m_GameManager.PlayerStats;
                AddLog($"角色信息 - 姓名: {m_GameManager.PlayerName}, 等级: {stats.Level}, " +
                      $"生命: {stats.CurrentHealth}/{stats.MaxHealth}, " +
                      $"灵石: {m_GameManager.Gold}", LogLevel.Info);
            }
        }

        /// <summary>
        /// 获取游戏状态文本
        /// </summary>
        private string GetGameStateText(GameState state)
        {
            return state switch
            {
                GameState.MainMenu => "主菜单",
                GameState.Exploring => "探索中",
                GameState.Battle => "战斗中",
                GameState.Inventory => "背包界面",
                GameState.Paused => "暂停",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 清理启动器
        /// </summary>
        private void CleanupStarter()
        {
            UnsubscribeFromEvents();
            m_PerformanceMonitor?.Dispose();
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("清空日志")]
        private void ClearLog()
        {
            m_LogMessages.Clear();
            UpdateLogDisplay();
        }

        [ContextMenu("添加1000灵石")]
        private void AddGold()
        {
            if (m_GameManager?.PlayerDataManager != null)
            {
                m_GameManager.PlayerDataManager.AddGold(1000);
                AddLog("添加了1000灵石！", LogLevel.Success);
            }
        }

        [ContextMenu("提升等级")]
        private void LevelUp()
        {
            if (m_GameManager?.PlayerDataManager != null)
            {
                var stats = m_GameManager.PlayerStats;
                m_GameManager.PlayerDataManager.GainExperience(stats.ExperienceToNext);
                AddLog("等级提升！", LogLevel.Success);
            }
        }

        [ContextMenu("重新初始化")]
        private void Reinitialize()
        {
            m_IsInitialized = false;
            InitializeAsync();
        }
#endif

        #endregion
    }

    /// <summary>
    /// 启动模式
    /// </summary>
    public enum StartupMode
    {
        NewGame,    // 新游戏
        LoadGame,   // 加载游戏
        MainMenu    // 主菜单
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Normal,     // 普通
        Info,       // 信息
        Success,    // 成功
        Warning,    // 警告
        Error,      // 错误
        Important   // 重要
    }

    /// <summary>
    /// 性能监控器
    /// </summary>
    public class PerformanceMonitor : System.IDisposable
    {
        private float[] m_FrameTimes = new float[60];
        private int m_FrameIndex = 0;
        private float m_LastFrameTime = 0f;

        public PerformanceMonitor()
        {
            m_LastFrameTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 获取内存使用量（MB）
        /// </summary>
        public float GetMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);
        }

        /// <summary>
        /// 获取FPS
        /// </summary>
        public float GetFPS()
        {
            float avgFrameTime = GetAverageFrameTime();
            return avgFrameTime > 0f ? 1f / avgFrameTime : 0f;
        }

        /// <summary>
        /// 获取帧时间（毫秒）
        /// </summary>
        public float GetFrameTime()
        {
            return GetAverageFrameTime() * 1000f;
        }

        /// <summary>
        /// 运行内存测试
        /// </summary>
        public void RunMemoryTest()
        {
            // 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            // 清理Unity对象
            Resources.UnloadUnusedAssets();
            
            GameLog.Info($"内存测试完成 - 当前内存使用: {GetMemoryUsage():F2}MB", "PerformanceMonitor");
        }

        private float GetAverageFrameTime()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - m_LastFrameTime;
            m_LastFrameTime = currentTime;

            m_FrameTimes[m_FrameIndex] = deltaTime;
            m_FrameIndex = (m_FrameIndex + 1) % m_FrameTimes.Length;

            float sum = 0f;
            for (int i = 0; i < m_FrameTimes.Length; i++)
            {
                sum += m_FrameTimes[i];
            }

            return sum / m_FrameTimes.Length;
        }

        public void Dispose()
        {
            // 清理资源
        }
    }
}