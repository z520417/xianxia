using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏服务引导程序
    /// 负责初始化和配置所有游戏服务
    /// </summary>
    public class GameServiceBootstrapper : MonoBehaviour
    {
        [Header("服务配置")]
        [SerializeField] private bool m_AutoInitialize = true;
        [SerializeField] private bool m_DontDestroyOnLoad = true;
        
        [Header("调试设置")]
        [SerializeField] private bool m_LogServiceRegistration = true;

        private IServiceContainer m_ServiceContainer;
        private bool m_IsInitialized = false;

        public static GameServiceBootstrapper Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                if (m_DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                if (m_AutoInitialize)
                {
                    InitializeServices();
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化所有游戏服务
        /// </summary>
        public void InitializeServices()
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("游戏服务已经初始化过了");
                return;
            }

            LogMessage("开始初始化游戏服务...");

            // 创建服务容器
            m_ServiceContainer = new ServiceContainer();
            ServiceLocator.SetContainer(m_ServiceContainer);

            // 注册核心服务
            RegisterCoreServices();

            // 注册游戏系统服务
            RegisterGameSystemServices();

            // 注册UI服务
            RegisterUIServices();

            // 注册数据服务
            RegisterDataServices();

            // 初始化服务
            InitializeRegisteredServices();

            m_IsInitialized = true;
            LogMessage("游戏服务初始化完成");
        }

        /// <summary>
        /// 注册核心服务
        /// </summary>
        private void RegisterCoreServices()
        {
            LogMessage("注册核心服务...");

            // 配置服务
            m_ServiceContainer.RegisterSingleton<IConfigService>(container =>
            {
                var configManager = FindOrCreateComponent<ConfigManager>();
                return new ConfigServiceAdapter(configManager);
            });

            // 日志服务
            m_ServiceContainer.RegisterSingleton<ILoggingService>(container =>
            {
                var loggingSystem = FindOrCreateComponent<LoggingSystem>();
                return new LoggingServiceAdapter(loggingSystem);
            });

            // 事件服务
            m_ServiceContainer.RegisterSingleton<IEventService>(container =>
            {
                var eventSystem = FindOrCreateComponent<EventSystem>();
                return new EventServiceAdapter(eventSystem);
            });

            // 存档服务
            m_ServiceContainer.RegisterSingleton<ISaveService>(container =>
            {
                var saveSystem = FindOrCreateComponent<SaveSystem>();
                return new SaveServiceAdapter(saveSystem);
            });
        }

        /// <summary>
        /// 注册游戏系统服务
        /// </summary>
        private void RegisterGameSystemServices()
        {
            LogMessage("注册游戏系统服务...");

            // 游戏状态服务
            m_ServiceContainer.RegisterSingleton<IGameStateService, GameStateService>();

            // 统计服务
            m_ServiceContainer.RegisterSingleton<IStatisticsService, StatisticsService>();

            // 探索服务
            m_ServiceContainer.RegisterSingleton<IExplorationService, ExplorationService>();

            // 音效服务
            m_ServiceContainer.RegisterSingleton<IAudioService, AudioService>();
        }

        /// <summary>
        /// 注册UI服务
        /// </summary>
        private void RegisterUIServices()
        {
            LogMessage("注册UI服务...");

            // UI更新服务
            m_ServiceContainer.RegisterSingleton<IUIUpdateService>(container =>
            {
                var uiUpdateManager = FindOrCreateComponent<UIUpdateManager>();
                return new UIUpdateServiceAdapter(uiUpdateManager);
            });

            // UI对象池服务
            m_ServiceContainer.RegisterSingleton<IUIObjectPoolService>(container =>
            {
                var uiObjectPool = FindOrCreateComponent<UIObjectPool>();
                return new UIObjectPoolServiceAdapter(uiObjectPool);
            });
        }

        /// <summary>
        /// 注册数据服务
        /// </summary>
        private void RegisterDataServices()
        {
            LogMessage("注册数据服务...");

            // 游戏数据服务
            m_ServiceContainer.RegisterSingleton<IGameDataService, GameDataService>();
        }

        /// <summary>
        /// 初始化已注册的服务
        /// </summary>
        private void InitializeRegisteredServices()
        {
            LogMessage("初始化已注册的服务...");

            // 获取所有核心服务以确保它们被初始化
            var configService = m_ServiceContainer.GetRequiredService<IConfigService>();
            var loggingService = m_ServiceContainer.GetRequiredService<ILoggingService>();
            var eventService = m_ServiceContainer.GetRequiredService<IEventService>();
            var saveService = m_ServiceContainer.GetRequiredService<ISaveService>();

            // 初始化其他服务
            var gameStateService = m_ServiceContainer.GetRequiredService<IGameStateService>();
            var statisticsService = m_ServiceContainer.GetRequiredService<IStatisticsService>();
            var uiUpdateService = m_ServiceContainer.GetRequiredService<IUIUpdateService>();
            var uiObjectPoolService = m_ServiceContainer.GetRequiredService<IUIObjectPoolService>();
            var gameDataService = m_ServiceContainer.GetRequiredService<IGameDataService>();

            LogMessage("所有服务初始化完成");
        }

        /// <summary>
        /// 查找或创建组件
        /// </summary>
        private T FindOrCreateComponent<T>() where T : MonoBehaviour
        {
            var component = FindObjectOfType<T>();
            
            if (component == null)
            {
                var go = new GameObject(typeof(T).Name);
                component = go.AddComponent<T>();
                
                if (m_DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(go);
                }
                
                LogMessage($"创建新组件: {typeof(T).Name}");
            }
            else
            {
                LogMessage($"找到现有组件: {typeof(T).Name}");
            }

            return component;
        }

        /// <summary>
        /// 获取服务容器
        /// </summary>
        public IServiceContainer GetServiceContainer()
        {
            return m_ServiceContainer;
        }

        /// <summary>
        /// 检查服务是否已初始化
        /// </summary>
        public bool IsInitialized => m_IsInitialized;

        private void LogMessage(string message)
        {
            if (m_LogServiceRegistration)
            {
                Debug.Log($"[GameServiceBootstrapper] {message}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                LogMessage("销毁游戏服务引导程序");
                
                m_ServiceContainer?.Dispose();
                ServiceLocator.Reset();
                
                Instance = null;
                m_IsInitialized = false;
            }
        }

        #region 静态便捷方法

        /// <summary>
        /// 获取服务（静态方法）
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return ServiceLocator.GetService<T>();
        }

        /// <summary>
        /// 获取必需服务（静态方法）
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            return ServiceLocator.GetRequiredService<T>();
        }

        /// <summary>
        /// 检查服务是否已注册（静态方法）
        /// </summary>
        public static bool IsServiceRegistered<T>()
        {
            return ServiceLocator.Current.IsRegistered<T>();
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("重新初始化服务")]
        private void ReinitializeServices()
        {
            if (m_IsInitialized)
            {
                m_ServiceContainer?.Dispose();
                ServiceLocator.Reset();
                m_IsInitialized = false;
            }
            
            InitializeServices();
        }

        [ContextMenu("打印已注册服务")]
        private void PrintRegisteredServices()
        {
            if (!m_IsInitialized)
            {
                Debug.Log("服务尚未初始化");
                return;
            }

            Debug.Log("=== 已注册的服务 ===");
            Debug.Log($"IConfigService: {m_ServiceContainer.IsRegistered<IConfigService>()}");
            Debug.Log($"ILoggingService: {m_ServiceContainer.IsRegistered<ILoggingService>()}");
            Debug.Log($"IEventService: {m_ServiceContainer.IsRegistered<IEventService>()}");
            Debug.Log($"ISaveService: {m_ServiceContainer.IsRegistered<ISaveService>()}");
            Debug.Log($"IGameStateService: {m_ServiceContainer.IsRegistered<IGameStateService>()}");
            Debug.Log($"IStatisticsService: {m_ServiceContainer.IsRegistered<IStatisticsService>()}");
            Debug.Log($"IUIUpdateService: {m_ServiceContainer.IsRegistered<IUIUpdateService>()}");
            Debug.Log($"IUIObjectPoolService: {m_ServiceContainer.IsRegistered<IUIObjectPoolService>()}");
            Debug.Log($"IGameDataService: {m_ServiceContainer.IsRegistered<IGameDataService>()}");
        }

        [ContextMenu("测试服务获取")]
        private void TestServiceRetrieval()
        {
            try
            {
                var configService = GetService<IConfigService>();
                var loggingService = GetService<ILoggingService>();
                var eventService = GetService<IEventService>();
                
                Debug.Log("服务获取测试成功");
                loggingService?.Log("服务获取测试", LogLevel.Info, "ServiceTest");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"服务获取测试失败: {e.Message}");
            }
        }
#endif

        #endregion
    }
}