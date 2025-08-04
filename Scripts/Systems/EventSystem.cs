using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏事件类型
    /// </summary>
    public enum GameEventType
    {
        // 玩家事件
        PlayerLevelUp,
        PlayerStatsChanged,
        PlayerHealthChanged,
        PlayerManaChanged,
        PlayerGoldChanged,
        PlayerExperienceGained,
        
        // 战斗事件
        BattleStarted,
        BattleEnded,
        BattleTurnStarted,
        BattleActionExecuted,
        DamageDealt,
        HealthRestored,
        
        // 背包事件
        ItemAdded,
        ItemRemoved,
        ItemUsed,
        InventoryChanged,
        InventoryUpgraded,
        
        // 装备事件
        EquipmentEquipped,
        EquipmentUnequipped,
        EquipmentChanged,
        
        // 探索事件
        ExplorationStarted,
        TreasureFound,
        BattleEncountered,
        ItemFound,
        NothingFound,
        
        // 游戏状态事件
        GameStateChanged,
        GameSaved,
        GameLoaded,
        GameMessage
    }

    /// <summary>
    /// 游戏事件数据基类
    /// </summary>
    public abstract class GameEventData
    {
        public float Timestamp { get; private set; }
        
        protected GameEventData()
        {
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 玩家等级提升事件数据
    /// </summary>
    public class PlayerLevelUpEventData : GameEventData
    {
        public int NewLevel { get; }
        public int OldLevel { get; }
        
        public PlayerLevelUpEventData(int newLevel, int oldLevel)
        {
            NewLevel = newLevel;
            OldLevel = oldLevel;
        }
    }

    /// <summary>
    /// 物品相关事件数据
    /// </summary>
    public class ItemEventData : GameEventData
    {
        public ItemData Item { get; }
        public int Quantity { get; }
        
        public ItemEventData(ItemData item, int quantity = 1)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// 战斗事件数据
    /// </summary>
    public class BattleEventData : GameEventData
    {
        public BattleParticipant Player { get; }
        public BattleParticipant Enemy { get; }
        public BattleResult Result { get; }
        
        public BattleEventData(BattleParticipant player, BattleParticipant enemy, BattleResult result = BattleResult.Victory)
        {
            Player = player;
            Enemy = enemy;
            Result = result;
        }
    }

    /// <summary>
    /// 游戏消息事件数据
    /// </summary>
    public class GameMessageEventData : GameEventData
    {
        public string Message { get; }
        public MessageType Type { get; }
        
        public GameMessageEventData(string message, MessageType type = MessageType.Normal)
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MessageType
    {
        Normal,
        Important,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// 数值变化事件数据
    /// </summary>
    public class ValueChangedEventData : GameEventData
    {
        public int NewValue { get; }
        public int OldValue { get; }
        public int Change => NewValue - OldValue;
        
        public ValueChangedEventData(int newValue, int oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }
    }

    /// <summary>
    /// 游戏事件系统 - 用于解耦系统间的通信
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        private static EventSystem s_Instance;
        public static EventSystem Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<EventSystem>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("EventSystem");
                        s_Instance = go.AddComponent<EventSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        // 事件监听器字典
        private Dictionary<GameEventType, List<Action<GameEventData>>> m_EventListeners = new Dictionary<GameEventType, List<Action<GameEventData>>>();
        
        // 事件历史记录（用于调试）
        private Queue<(GameEventType, GameEventData, float)> m_EventHistory = new Queue<(GameEventType, GameEventData, float)>();
        private const int MAX_HISTORY_SIZE = 100;

        [Header("调试设置")]
        [SerializeField] private bool m_EnableEventLogging = false;
        [SerializeField] private bool m_EnableEventHistory = true;

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 注册事件监听器
        /// </summary>
        public void AddListener(GameEventType eventType, Action<GameEventData> listener)
        {
            if (!m_EventListeners.ContainsKey(eventType))
            {
                m_EventListeners[eventType] = new List<Action<GameEventData>>();
            }
            
            m_EventListeners[eventType].Add(listener);
            
            if (m_EnableEventLogging)
            {
                Debug.Log($"[EventSystem] 注册监听器: {eventType}");
            }
        }

        /// <summary>
        /// 移除事件监听器
        /// </summary>
        public void RemoveListener(GameEventType eventType, Action<GameEventData> listener)
        {
            if (m_EventListeners.ContainsKey(eventType))
            {
                m_EventListeners[eventType].Remove(listener);
                
                if (m_EnableEventLogging)
                {
                    Debug.Log($"[EventSystem] 移除监听器: {eventType}");
                }
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void TriggerEvent(GameEventType eventType, GameEventData eventData = null)
        {
            if (m_EnableEventLogging)
            {
                Debug.Log($"[EventSystem] 触发事件: {eventType} at {Time.time}");
            }

            // 记录事件历史
            if (m_EnableEventHistory)
            {
                RecordEvent(eventType, eventData);
            }

            // 通知所有监听器
            if (m_EventListeners.ContainsKey(eventType))
            {
                var listeners = m_EventListeners[eventType];
                for (int i = listeners.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        listeners[i]?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventSystem] 事件监听器执行错误 {eventType}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 记录事件历史
        /// </summary>
        private void RecordEvent(GameEventType eventType, GameEventData eventData)
        {
            m_EventHistory.Enqueue((eventType, eventData, Time.time));
            
            // 限制历史记录大小
            while (m_EventHistory.Count > MAX_HISTORY_SIZE)
            {
                m_EventHistory.Dequeue();
            }
        }

        /// <summary>
        /// 获取事件历史记录
        /// </summary>
        public List<(GameEventType, GameEventData, float)> GetEventHistory()
        {
            return new List<(GameEventType, GameEventData, float)>(m_EventHistory);
        }

        /// <summary>
        /// 清除事件历史记录
        /// </summary>
        public void ClearEventHistory()
        {
            m_EventHistory.Clear();
        }

        /// <summary>
        /// 清除所有监听器
        /// </summary>
        public void ClearAllListeners()
        {
            m_EventListeners.Clear();
            Debug.Log("[EventSystem] 清除所有事件监听器");
        }

        /// <summary>
        /// 获取指定事件类型的监听器数量
        /// </summary>
        public int GetListenerCount(GameEventType eventType)
        {
            return m_EventListeners.ContainsKey(eventType) ? m_EventListeners[eventType].Count : 0;
        }

#if UNITY_EDITOR
        [ContextMenu("打印事件统计")]
        private void PrintEventStatistics()
        {
            Debug.Log("=== 事件系统统计 ===");
            foreach (var kvp in m_EventListeners)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.Count} 个监听器");
            }
            Debug.Log($"事件历史记录: {m_EventHistory.Count} 条");
        }

        [ContextMenu("清除事件历史")]
        private void ClearEventHistoryInEditor()
        {
            ClearEventHistory();
        }

        [ContextMenu("测试事件")]
        private void TestEvent()
        {
            TriggerEvent(GameEventType.GameMessage, new GameMessageEventData("测试事件消息", MessageType.Normal));
        }
#endif
    }
}