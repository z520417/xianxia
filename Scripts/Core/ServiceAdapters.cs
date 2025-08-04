using System;
using System.Collections.Generic;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 配置服务适配器
    /// </summary>
    public class ConfigServiceAdapter : IConfigService
    {
        private readonly ConfigManager m_ConfigManager;

        public GameConfig Config => m_ConfigManager?.Config;

        public ConfigServiceAdapter(ConfigManager configManager)
        {
            m_ConfigManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        public void ReloadConfig()
        {
            m_ConfigManager.ReloadConfig();
        }

        public bool ValidateConfig()
        {
            return m_ConfigManager.ValidateConfig();
        }
    }

    /// <summary>
    /// 日志服务适配器
    /// </summary>
    public class LoggingServiceAdapter : ILoggingService
    {
        private readonly LoggingSystem m_LoggingSystem;

        public event Action<LogEntry> OnLogEntryAdded
        {
            add { m_LoggingSystem.OnLogEntryAdded += value; }
            remove { m_LoggingSystem.OnLogEntryAdded -= value; }
        }

        public LoggingServiceAdapter(LoggingSystem loggingSystem)
        {
            m_LoggingSystem = loggingSystem ?? throw new ArgumentNullException(nameof(loggingSystem));
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string category = "Game")
        {
            LoggingSystem.Log(message, level, category);
        }

        public void LogDetailed(string message, LogLevel level = LogLevel.Info, string category = "Game")
        {
            LoggingSystem.LogDetailed(message, level, category);
        }

        public void LogException(Exception exception, string category = "Game")
        {
            LoggingSystem.LogException(exception, category);
        }

        public void LogFormat(LogLevel level, string category, string format, params object[] args)
        {
            LoggingSystem.LogFormat(level, category, format, args);
        }

        public List<LogEntry> GetLogEntries(LogLevel? minLevel = null, string category = null)
        {
            return m_LoggingSystem.GetLogEntries(minLevel, category);
        }

        public void ClearLogs()
        {
            m_LoggingSystem.ClearLogs();
        }

        public void SetMinLogLevel(LogLevel minLevel)
        {
            m_LoggingSystem.SetMinLogLevel(minLevel);
        }
    }

    /// <summary>
    /// 事件服务适配器
    /// </summary>
    public class EventServiceAdapter : IEventService
    {
        private readonly EventSystem m_EventSystem;

        public EventServiceAdapter(EventSystem eventSystem)
        {
            m_EventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }

        public void AddListener(GameEventType eventType, Action<GameEventData> listener)
        {
            m_EventSystem.AddListener(eventType, listener);
        }

        public void RemoveListener(GameEventType eventType, Action<GameEventData> listener)
        {
            m_EventSystem.RemoveListener(eventType, listener);
        }

        public void TriggerEvent(GameEventType eventType, GameEventData eventData = null)
        {
            m_EventSystem.TriggerEvent(eventType, eventData);
        }

        public List<(GameEventType, GameEventData, float)> GetEventHistory()
        {
            return m_EventSystem.GetEventHistory();
        }

        public void ClearEventHistory()
        {
            m_EventSystem.ClearEventHistory();
        }
    }

    /// <summary>
    /// 存档服务适配器
    /// </summary>
    public class SaveServiceAdapter : ISaveService
    {
        private readonly SaveSystem m_SaveSystem;

        public event Action<GameSaveData> OnGameSaved
        {
            add { m_SaveSystem.OnGameSaved += value; }
            remove { m_SaveSystem.OnGameSaved -= value; }
        }

        public event Action<GameSaveData> OnGameLoaded
        {
            add { m_SaveSystem.OnGameLoaded += value; }
            remove { m_SaveSystem.OnGameLoaded -= value; }
        }

        public event Action<string> OnSaveError
        {
            add { m_SaveSystem.OnSaveError += value; }
            remove { m_SaveSystem.OnSaveError -= value; }
        }

        public SaveServiceAdapter(SaveSystem saveSystem)
        {
            m_SaveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
        }

        public bool SaveGame(int slotIndex, string saveName = "")
        {
            return m_SaveSystem.SaveGame(slotIndex, saveName);
        }

        public bool LoadGame(int slotIndex)
        {
            return m_SaveSystem.LoadGame(slotIndex);
        }

        public List<GameSaveData> GetAllSaveInfo()
        {
            return m_SaveSystem.GetAllSaveInfo();
        }

        public bool DeleteSave(int slotIndex)
        {
            return m_SaveSystem.DeleteSave(slotIndex);
        }

        public GameSaveData CreateSaveData()
        {
            return m_SaveSystem.CreateSaveData();
        }
    }

    /// <summary>
    /// UI更新服务适配器
    /// </summary>
    public class UIUpdateServiceAdapter : IUIUpdateService
    {
        private readonly UIUpdateManager m_UIUpdateManager;

        public event Action<UIUpdateType, object> OnUIUpdateProcessed
        {
            add { m_UIUpdateManager.OnUIUpdateProcessed += value; }
            remove { m_UIUpdateManager.OnUIUpdateProcessed -= value; }
        }

        public UIUpdateServiceAdapter(UIUpdateManager uiUpdateManager)
        {
            m_UIUpdateManager = uiUpdateManager ?? throw new ArgumentNullException(nameof(uiUpdateManager));
        }

        public void RegisterUpdateHandler(UIUpdateType updateType, Action<object> handler)
        {
            m_UIUpdateManager.RegisterUpdateHandler(updateType, handler);
        }

        public void UnregisterUpdateHandler(UIUpdateType updateType)
        {
            m_UIUpdateManager.UnregisterUpdateHandler(updateType);
        }

        public void RequestUpdate(UIUpdateType updateType, object data = null, bool isCritical = false)
        {
            m_UIUpdateManager.RequestUpdate(updateType, data, isCritical);
        }

        public void ForceUpdate(UIUpdateType updateType, object data = null)
        {
            m_UIUpdateManager.ForceUpdate(updateType, data);
        }

        public void RequestBatchUpdate(params UIUpdateType[] updateTypes)
        {
            m_UIUpdateManager.RequestBatchUpdate(updateTypes);
        }

        public void ClearUpdateQueue()
        {
            m_UIUpdateManager.ClearUpdateQueue();
        }

        public void SetUpdateInterval(float interval)
        {
            m_UIUpdateManager.SetUpdateInterval(interval);
        }
    }

    /// <summary>
    /// UI对象池服务适配器
    /// </summary>
    public class UIObjectPoolServiceAdapter : IUIObjectPoolService
    {
        private readonly UIObjectPool m_UIObjectPool;

        public UIObjectPoolServiceAdapter(UIObjectPool uiObjectPool)
        {
            m_UIObjectPool = uiObjectPool ?? throw new ArgumentNullException(nameof(uiObjectPool));
        }

        public void WarmupPool(GameObject prefab, int count = -1)
        {
            m_UIObjectPool.WarmupPool(prefab, count);
        }

        public GameObject GetFromPool(GameObject prefab, Transform parent = null)
        {
            return m_UIObjectPool.GetFromPool(prefab, parent);
        }

        public void ReturnToPool(GameObject obj)
        {
            m_UIObjectPool.ReturnToPool(obj);
        }

        public void ClearPool(GameObject prefab)
        {
            m_UIObjectPool.ClearPool(prefab);
        }

        public void ClearPool(string poolKey)
        {
            m_UIObjectPool.ClearPool(poolKey);
        }

        public void ClearAllPools()
        {
            m_UIObjectPool.ClearAllPools();
        }

        public Dictionary<string, int> GetPoolStatistics()
        {
            return m_UIObjectPool.GetPoolStatistics();
        }

        public int GetActiveObjectCount()
        {
            return m_UIObjectPool.GetActiveObjectCount();
        }
    }
}