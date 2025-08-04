using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// UI更新类型
    /// </summary>
    public enum UIUpdateType
    {
        PlayerStats,        // 玩家属性
        PlayerInfo,         // 玩家基本信息
        Inventory,          // 背包
        Equipment,          // 装备
        Battle,             // 战斗
        Statistics,         // 统计信息
        Messages            // 消息
    }

    /// <summary>
    /// UI更新请求
    /// </summary>
    public class UIUpdateRequest
    {
        public UIUpdateType UpdateType { get; }
        public object Data { get; }
        public float RequestTime { get; }
        public bool IsCritical { get; }

        public UIUpdateRequest(UIUpdateType updateType, object data = null, bool isCritical = false)
        {
            UpdateType = updateType;
            Data = data;
            RequestTime = Time.time;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// UI更新管理器
    /// 用于批量处理UI更新请求，减少不必要的刷新，提高性能
    /// </summary>
    public class UIUpdateManager : MonoBehaviour
    {
        private static UIUpdateManager s_Instance;
        public static UIUpdateManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<UIUpdateManager>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("UIUpdateManager");
                        s_Instance = go.AddComponent<UIUpdateManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        [Header("更新配置")]
        [SerializeField] private float m_UpdateInterval = 0.1f;        // 默认更新间隔
        [SerializeField] private float m_CriticalUpdateInterval = 0.05f; // 关键更新间隔
        [SerializeField] private int m_MaxUpdatesPerFrame = 5;         // 每帧最大更新数量

        [Header("调试设置")]
        [SerializeField] private bool m_EnableDebugLog = false;
        [SerializeField] private bool m_ShowPerformanceStats = false;

        // 更新请求队列
        private Queue<UIUpdateRequest> m_UpdateQueue = new Queue<UIUpdateRequest>();
        
        // 更新处理器字典
        private Dictionary<UIUpdateType, Action<object>> m_UpdateHandlers = new Dictionary<UIUpdateType, Action<object>>();
        
        // 最后更新时间记录
        private Dictionary<UIUpdateType, float> m_LastUpdateTimes = new Dictionary<UIUpdateType, float>();
        
        // 待处理的更新类型（去重用）
        private HashSet<UIUpdateType> m_PendingUpdates = new HashSet<UIUpdateType>();

        // 性能统计
        private int m_UpdatesThisFrame = 0;
        private int m_TotalUpdatesProcessed = 0;
        private float m_LastFrameResetTime = 0f;

        public event Action<UIUpdateType, object> OnUIUpdateProcessed;

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

        private void Update()
        {
            ResetFrameCounter();
            ProcessUpdateQueue();
        }

        /// <summary>
        /// 注册UI更新处理器
        /// </summary>
        public void RegisterUpdateHandler(UIUpdateType updateType, Action<object> handler)
        {
            if (handler == null)
            {
                Debug.LogError($"[UIUpdateManager] 尝试注册空的更新处理器: {updateType}");
                return;
            }

            m_UpdateHandlers[updateType] = handler;
            
            if (m_EnableDebugLog)
            {
                Debug.Log($"[UIUpdateManager] 注册更新处理器: {updateType}");
            }
        }

        /// <summary>
        /// 取消注册UI更新处理器
        /// </summary>
        public void UnregisterUpdateHandler(UIUpdateType updateType)
        {
            if (m_UpdateHandlers.ContainsKey(updateType))
            {
                m_UpdateHandlers.Remove(updateType);
                
                if (m_EnableDebugLog)
                {
                    Debug.Log($"[UIUpdateManager] 取消注册更新处理器: {updateType}");
                }
            }
        }

        /// <summary>
        /// 请求UI更新
        /// </summary>
        public void RequestUpdate(UIUpdateType updateType, object data = null, bool isCritical = false)
        {
            // 检查是否需要立即更新
            if (isCritical || ShouldUpdateImmediately(updateType))
            {
                ProcessUpdate(updateType, data);
                return;
            }

            // 去重：如果已经有相同类型的更新在队列中，则跳过
            if (m_PendingUpdates.Contains(updateType))
            {
                if (m_EnableDebugLog)
                {
                    Debug.Log($"[UIUpdateManager] 跳过重复更新请求: {updateType}");
                }
                return;
            }

            // 检查更新频率限制
            if (!CanUpdate(updateType, isCritical))
            {
                if (m_EnableDebugLog)
                {
                    Debug.Log($"[UIUpdateManager] 更新频率限制，延迟更新: {updateType}");
                }
                return;
            }

            // 添加到更新队列
            m_UpdateQueue.Enqueue(new UIUpdateRequest(updateType, data, isCritical));
            m_PendingUpdates.Add(updateType);

            if (m_EnableDebugLog)
            {
                Debug.Log($"[UIUpdateManager] 添加更新请求: {updateType} (队列长度: {m_UpdateQueue.Count})");
            }
        }

        /// <summary>
        /// 立即执行指定类型的UI更新
        /// </summary>
        public void ForceUpdate(UIUpdateType updateType, object data = null)
        {
            ProcessUpdate(updateType, data);
        }

        /// <summary>
        /// 批量请求UI更新
        /// </summary>
        public void RequestBatchUpdate(params UIUpdateType[] updateTypes)
        {
            foreach (var updateType in updateTypes)
            {
                RequestUpdate(updateType);
            }
        }

        /// <summary>
        /// 清空更新队列
        /// </summary>
        public void ClearUpdateQueue()
        {
            m_UpdateQueue.Clear();
            m_PendingUpdates.Clear();
            
            if (m_EnableDebugLog)
            {
                Debug.Log("[UIUpdateManager] 清空更新队列");
            }
        }

        /// <summary>
        /// 设置更新间隔
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            m_UpdateInterval = Mathf.Max(0.01f, interval);
        }

        /// <summary>
        /// 获取更新统计信息
        /// </summary>
        public (int queueSize, int totalProcessed, int thisFrame) GetUpdateStatistics()
        {
            return (m_UpdateQueue.Count, m_TotalUpdatesProcessed, m_UpdatesThisFrame);
        }

        private void ProcessUpdateQueue()
        {
            m_UpdatesThisFrame = 0;

            while (m_UpdateQueue.Count > 0 && m_UpdatesThisFrame < m_MaxUpdatesPerFrame)
            {
                var request = m_UpdateQueue.Dequeue();
                
                // 从待处理集合中移除
                m_PendingUpdates.Remove(request.UpdateType);

                // 再次检查是否可以更新（可能时间已过期）
                if (CanUpdate(request.UpdateType, request.IsCritical))
                {
                    ProcessUpdate(request.UpdateType, request.Data);
                    m_UpdatesThisFrame++;
                }
                else if (m_EnableDebugLog)
                {
                    Debug.Log($"[UIUpdateManager] 跳过过期的更新请求: {request.UpdateType}");
                }
            }

            // 显示性能统计
            if (m_ShowPerformanceStats && m_UpdatesThisFrame > 0)
            {
                Debug.Log($"[UIUpdateManager] 本帧处理 {m_UpdatesThisFrame} 个UI更新");
            }
        }

        private void ProcessUpdate(UIUpdateType updateType, object data)
        {
            if (m_UpdateHandlers.ContainsKey(updateType))
            {
                try
                {
                    m_UpdateHandlers[updateType].Invoke(data);
                    m_LastUpdateTimes[updateType] = Time.time;
                    m_TotalUpdatesProcessed++;

                    OnUIUpdateProcessed?.Invoke(updateType, data);

                    if (m_EnableDebugLog)
                    {
                        Debug.Log($"[UIUpdateManager] 处理UI更新: {updateType}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UIUpdateManager] UI更新处理器执行错误 {updateType}: {e.Message}");
                }
            }
            else if (m_EnableDebugLog)
            {
                Debug.LogWarning($"[UIUpdateManager] 未找到更新处理器: {updateType}");
            }
        }

        private bool CanUpdate(UIUpdateType updateType, bool isCritical)
        {
            if (!m_LastUpdateTimes.ContainsKey(updateType))
            {
                return true;
            }

            float timeSinceLastUpdate = Time.time - m_LastUpdateTimes[updateType];
            float requiredInterval = isCritical ? m_CriticalUpdateInterval : m_UpdateInterval;

            return timeSinceLastUpdate >= requiredInterval;
        }

        private bool ShouldUpdateImmediately(UIUpdateType updateType)
        {
            // 某些类型的更新应该立即处理
            switch (updateType)
            {
                case UIUpdateType.Battle:
                case UIUpdateType.Messages:
                    return true;
                default:
                    return false;
            }
        }

        private void ResetFrameCounter()
        {
            if (Time.time - m_LastFrameResetTime >= 1f)
            {
                m_UpdatesThisFrame = 0;
                m_LastFrameResetTime = Time.time;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("打印更新统计")]
        private void PrintUpdateStatistics()
        {
            var stats = GetUpdateStatistics();
            Debug.Log($"=== UI更新管理器统计 ===");
            Debug.Log($"队列大小: {stats.queueSize}");
            Debug.Log($"总处理数: {stats.totalProcessed}");
            Debug.Log($"本帧处理: {stats.thisFrame}");
            Debug.Log($"注册的处理器数量: {m_UpdateHandlers.Count}");
        }

        [ContextMenu("清空更新队列")]
        private void ClearUpdateQueueInEditor()
        {
            ClearUpdateQueue();
        }

        [ContextMenu("强制全量更新")]
        private void ForceFullUpdate()
        {
            foreach (UIUpdateType updateType in Enum.GetValues(typeof(UIUpdateType)))
            {
                ForceUpdate(updateType);
            }
        }
#endif
    }
}