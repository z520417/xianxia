using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame.Core
{
    /// <summary>
    /// 高性能组件注册表
    /// 替代FindObjectOfType，提供快速的组件查找和管理
    /// </summary>
    public class ComponentRegistry : MonoBehaviour
    {
        private static ComponentRegistry s_Instance;
        public static ComponentRegistry Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("ComponentRegistry");
                    s_Instance = go.AddComponent<ComponentRegistry>();
                    DontDestroyOnLoad(go);
                }
                return s_Instance;
            }
        }

        // 组件缓存字典 - 按类型存储
        private readonly Dictionary<Type, Component> m_ComponentCache = new Dictionary<Type, Component>();
        
        // 多实例组件列表
        private readonly Dictionary<Type, List<Component>> m_MultiComponentCache = new Dictionary<Type, List<Component>>();
        
        // 组件生命周期追踪
        private readonly HashSet<Component> m_TrackedComponents = new HashSet<Component>();

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 初始化时自动注册所有现有组件
                RegisterExistingComponents();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        #region 注册方法

        /// <summary>
        /// 注册组件（自动调用）
        /// </summary>
        public static void RegisterComponent<T>(T component) where T : Component
        {
            Instance.RegisterComponentInternal(component);
        }

        /// <summary>
        /// 注册多个同类型组件
        /// </summary>
        public static void RegisterMultiComponent<T>(T component) where T : Component
        {
            Instance.RegisterMultiComponentInternal(component);
        }

        /// <summary>
        /// 取消注册组件
        /// </summary>
        public static void UnregisterComponent<T>(T component) where T : Component
        {
            Instance.UnregisterComponentInternal(component);
        }

        private void RegisterComponentInternal<T>(T component) where T : Component
        {
            if (component == null) return;

            Type type = typeof(T);
            m_ComponentCache[type] = component;
            m_TrackedComponents.Add(component);

            GameLog.Debug($"注册组件: {type.Name}", "ComponentRegistry");
        }

        private void RegisterMultiComponentInternal<T>(T component) where T : Component
        {
            if (component == null) return;

            Type type = typeof(T);
            if (!m_MultiComponentCache.ContainsKey(type))
            {
                m_MultiComponentCache[type] = new List<Component>();
            }
            
            m_MultiComponentCache[type].Add(component);
            m_TrackedComponents.Add(component);

            GameLog.Debug($"注册多实例组件: {type.Name}", "ComponentRegistry");
        }

        private void UnregisterComponentInternal<T>(T component) where T : Component
        {
            if (component == null) return;

            Type type = typeof(T);
            
            // 从单例缓存中移除
            if (m_ComponentCache.ContainsKey(type) && m_ComponentCache[type] == component)
            {
                m_ComponentCache.Remove(type);
            }
            
            // 从多实例缓存中移除
            if (m_MultiComponentCache.ContainsKey(type))
            {
                m_MultiComponentCache[type].Remove(component);
                if (m_MultiComponentCache[type].Count == 0)
                {
                    m_MultiComponentCache.Remove(type);
                }
            }
            
            m_TrackedComponents.Remove(component);
            GameLog.Debug($"取消注册组件: {type.Name}", "ComponentRegistry");
        }

        #endregion

        #region 查找方法

        /// <summary>
        /// 快速获取组件（替代FindObjectOfType）
        /// </summary>
        public static T GetComponent<T>() where T : Component
        {
            return Instance.GetComponentInternal<T>();
        }

        /// <summary>
        /// 安全获取组件（不为空才返回）
        /// </summary>
        public static T GetComponentSafe<T>() where T : Component
        {
            var component = Instance.GetComponentInternal<T>();
            return component != null && component.gameObject != null ? component : null;
        }

        /// <summary>
        /// 获取所有同类型组件
        /// </summary>
        public static List<T> GetComponents<T>() where T : Component
        {
            return Instance.GetComponentsInternal<T>();
        }

        /// <summary>
        /// 检查组件是否存在
        /// </summary>
        public static bool HasComponent<T>() where T : Component
        {
            return Instance.GetComponentInternal<T>() != null;
        }

        /// <summary>
        /// 获取或创建组件
        /// </summary>
        public static T GetOrCreateComponent<T>(string gameObjectName = null) where T : Component
        {
            var component = GetComponentSafe<T>();
            if (component != null) return component;

            // 创建新组件
            var go = new GameObject(gameObjectName ?? typeof(T).Name);
            component = go.AddComponent<T>();
            RegisterComponent(component);
            
            GameLog.Info($"创建新组件: {typeof(T).Name}", "ComponentRegistry");
            return component;
        }

        private T GetComponentInternal<T>() where T : Component
        {
            Type type = typeof(T);
            
            if (m_ComponentCache.TryGetValue(type, out Component component))
            {
                // 检查组件是否仍然有效
                if (component != null && component.gameObject != null)
                {
                    return component as T;
                }
                else
                {
                    // 清理无效组件
                    m_ComponentCache.Remove(type);
                    m_TrackedComponents.Remove(component);
                }
            }
            
            return null;
        }

        private List<T> GetComponentsInternal<T>() where T : Component
        {
            Type type = typeof(T);
            var result = new List<T>();
            
            if (m_MultiComponentCache.TryGetValue(type, out List<Component> components))
            {
                for (int i = components.Count - 1; i >= 0; i--)
                {
                    var component = components[i];
                    if (component != null && component.gameObject != null)
                    {
                        result.Add(component as T);
                    }
                    else
                    {
                        // 清理无效组件
                        components.RemoveAt(i);
                        m_TrackedComponents.Remove(component);
                    }
                }
            }
            
            return result;
        }

        #endregion

        #region 初始化和清理

        /// <summary>
        /// 自动注册现有组件
        /// </summary>
        private void RegisterExistingComponents()
        {
            try
            {
                // 注册常用的游戏组件
                RegisterIfExists<GameManager>();
                RegisterIfExists<GameManagerRefactored>();
                RegisterIfExists<PlayerDataManager>();
                RegisterIfExists<InventorySystem>();
                RegisterIfExists<EquipmentManager>();
                RegisterIfExists<BattleSystem>();
                RegisterIfExists<UIUpdateManager>();
                RegisterIfExists<EventSystem>();
                RegisterIfExists<SaveSystem>();
                RegisterIfExists<ConfigManager>();
                
                GameLog.Info("现有组件注册完成", "ComponentRegistry");
            }
            catch (Exception e)
            {
                GameLog.Error($"注册现有组件时出错: {e.Message}", "ComponentRegistry");
            }
        }

        private void RegisterIfExists<T>() where T : Component
        {
            var component = FindObjectOfType<T>();
            if (component != null)
            {
                RegisterComponentInternal(component);
            }
        }

        /// <summary>
        /// 清理无效组件
        /// </summary>
        public static void CleanupInvalidComponents()
        {
            Instance.CleanupInvalidComponentsInternal();
        }

        private void CleanupInvalidComponentsInternal()
        {
            var invalidComponents = new List<Component>();
            
            foreach (var component in m_TrackedComponents)
            {
                if (component == null || component.gameObject == null)
                {
                    invalidComponents.Add(component);
                }
            }
            
            foreach (var invalid in invalidComponents)
            {
                m_TrackedComponents.Remove(invalid);
                
                // 从缓存中移除
                var typesToRemove = new List<Type>();
                foreach (var kvp in m_ComponentCache)
                {
                    if (kvp.Value == invalid)
                    {
                        typesToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var type in typesToRemove)
                {
                    m_ComponentCache.Remove(type);
                }
            }
            
            GameLog.Debug($"清理了 {invalidComponents.Count} 个无效组件", "ComponentRegistry");
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static (int singletons, int multiComponents, int totalTracked) GetCacheStatistics()
        {
            var instance = Instance;
            return (
                instance.m_ComponentCache.Count,
                instance.m_MultiComponentCache.Count,
                instance.m_TrackedComponents.Count
            );
        }

        /// <summary>
        /// 打印缓存状态
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PrintCacheStatus()
        {
            var (singletons, multi, total) = GetCacheStatistics();
            Debug.Log($"ComponentRegistry状态: 单例组件={singletons}, 多实例组件类型={multi}, 总追踪组件={total}");
        }

        #endregion

        #region Unity生命周期

        private void Update()
        {
            // 定期清理无效组件（每秒一次）
            if (Time.time % 1f < Time.deltaTime)
            {
                CleanupInvalidComponentsInternal();
            }
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("清理无效组件")]
        private void EditorCleanupInvalidComponents()
        {
            CleanupInvalidComponents();
        }

        [ContextMenu("打印缓存状态")]
        private void EditorPrintCacheStatus()
        {
            PrintCacheStatus();
        }

        [ContextMenu("重新注册所有组件")]
        private void EditorReregisterAllComponents()
        {
            m_ComponentCache.Clear();
            m_MultiComponentCache.Clear();
            m_TrackedComponents.Clear();
            RegisterExistingComponents();
        }
#endif
    }

    /// <summary>
    /// 自动注册组件的基类
    /// 继承此类的组件会自动注册到ComponentRegistry
    /// </summary>
    public abstract class RegisteredComponent : MonoBehaviour
    {
        protected virtual void Awake()
        {
            RegisterSelf();
        }

        protected virtual void OnDestroy()
        {
            UnregisterSelf();
        }

        private void RegisterSelf()
        {
            ComponentRegistry.RegisterComponent(this);
        }

        private void UnregisterSelf()
        {
            ComponentRegistry.UnregisterComponent(this);
        }
    }
}