using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame.Core
{
    /// <summary>
    /// 单例类型
    /// </summary>
    public enum SingletonType
    {
        MonoBehaviour,  // MonoBehaviour单例
        Service,        // 服务单例
        ScriptableObject // ScriptableObject单例
    }

    /// <summary>
    /// 单例配置
    /// </summary>
    [System.Serializable]
    public class SingletonConfig
    {
        public bool DontDestroyOnLoad = true;
        public bool AutoCreate = true;
        public bool ThreadSafe = false;
        public string CustomName = "";
    }

    /// <summary>
    /// 优化的单例管理器
    /// 提供线程安全、高性能的单例管理
    /// </summary>
    public static class SingletonManager
    {
        private static readonly Dictionary<Type, object> s_Instances = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, SingletonConfig> s_Configs = new Dictionary<Type, SingletonConfig>();
        private static readonly object s_Lock = new object();

        /// <summary>
        /// 注册单例配置
        /// </summary>
        public static void RegisterConfig<T>(SingletonConfig config) where T : class
        {
            lock (s_Lock)
            {
                s_Configs[typeof(T)] = config;
            }
        }

        /// <summary>
        /// 获取或创建MonoBehaviour单例
        /// </summary>
        public static T GetOrCreateMonoBehaviour<T>() where T : MonoBehaviour
        {
            Type type = typeof(T);
            
            if (s_Instances.TryGetValue(type, out object instance))
            {
                var mono = instance as T;
                if (mono != null && mono.gameObject != null)
                {
                    return mono;
                }
                else
                {
                    // 清理无效实例
                    s_Instances.Remove(type);
                }
            }

            // 创建新实例
            return CreateMonoBehaviourInstance<T>();
        }

        /// <summary>
        /// 获取或创建服务单例
        /// </summary>
        public static T GetOrCreateService<T>() where T : class, new()
        {
            Type type = typeof(T);
            var config = GetConfig<T>();

            if (config.ThreadSafe)
            {
                lock (s_Lock)
                {
                    return GetOrCreateServiceInternal<T>(type);
                }
            }
            else
            {
                return GetOrCreateServiceInternal<T>(type);
            }
        }

        /// <summary>
        /// 设置单例实例
        /// </summary>
        public static void SetInstance<T>(T instance) where T : class
        {
            if (instance == null) return;

            Type type = typeof(T);
            var config = GetConfig<T>();

            if (config.ThreadSafe)
            {
                lock (s_Lock)
                {
                    s_Instances[type] = instance;
                }
            }
            else
            {
                s_Instances[type] = instance;
            }

            GameLog.Debug($"设置单例实例: {type.Name}", "SingletonManager");
        }

        /// <summary>
        /// 检查单例是否存在
        /// </summary>
        public static bool HasInstance<T>() where T : class
        {
            Type type = typeof(T);
            return s_Instances.ContainsKey(type) && s_Instances[type] != null;
        }

        /// <summary>
        /// 移除单例实例
        /// </summary>
        public static void RemoveInstance<T>() where T : class
        {
            Type type = typeof(T);
            
            lock (s_Lock)
            {
                if (s_Instances.TryGetValue(type, out object instance))
                {
                    s_Instances.Remove(type);
                    
                    // 如果是MonoBehaviour，销毁GameObject
                    if (instance is MonoBehaviour mono && mono != null)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(mono.gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(mono.gameObject);
                    }
                    
                    GameLog.Debug($"移除单例实例: {type.Name}", "SingletonManager");
                }
            }
        }

        /// <summary>
        /// 清理所有单例
        /// </summary>
        public static void ClearAllInstances()
        {
            lock (s_Lock)
            {
                foreach (var kvp in s_Instances)
                {
                    if (kvp.Value is MonoBehaviour mono && mono != null)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(mono.gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(mono.gameObject);
                    }
                }
                
                s_Instances.Clear();
                GameLog.Info("所有单例实例已清理", "SingletonManager");
            }
        }

        #region 私有方法

        private static T GetOrCreateServiceInternal<T>(Type type) where T : class, new()
        {
            if (s_Instances.TryGetValue(type, out object instance))
            {
                return instance as T;
            }

            var config = GetConfig<T>();
            if (config.AutoCreate)
            {
                var newInstance = new T();
                s_Instances[type] = newInstance;
                return newInstance;
            }

            return null;
        }

        private static T CreateMonoBehaviourInstance<T>() where T : MonoBehaviour
        {
            var config = GetConfig<T>();
            string objectName = string.IsNullOrEmpty(config.CustomName) ? typeof(T).Name : config.CustomName;
            
            var go = new GameObject(objectName);
            var instance = go.AddComponent<T>();
            
            if (config.DontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            
            s_Instances[typeof(T)] = instance;
            
            // 自动注册到ComponentRegistry
            ComponentRegistry.RegisterComponent(instance);
            
            GameLog.Debug($"创建MonoBehaviour单例: {typeof(T).Name}", "SingletonManager");
            return instance;
        }

        private static SingletonConfig GetConfig<T>()
        {
            Type type = typeof(T);
            if (s_Configs.TryGetValue(type, out SingletonConfig config))
            {
                return config;
            }
            
            // 返回默认配置
            return new SingletonConfig();
        }

        #endregion

        #region 统计和调试

        /// <summary>
        /// 获取单例统计信息
        /// </summary>
        public static Dictionary<string, object> GetStatistics()
        {
            lock (s_Lock)
            {
                var stats = new Dictionary<string, object>
                {
                    ["TotalInstances"] = s_Instances.Count,
                    ["ConfiguredTypes"] = s_Configs.Count
                };

                var instanceTypes = new List<string>();
                foreach (var kvp in s_Instances)
                {
                    instanceTypes.Add(kvp.Key.Name);
                }
                stats["InstanceTypes"] = instanceTypes;

                return stats;
            }
        }

        /// <summary>
        /// 打印统计信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PrintStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"SingletonManager统计: 实例数={stats["TotalInstances"]}, 配置类型数={stats["ConfiguredTypes"]}");
            
            var types = stats["InstanceTypes"] as List<string>;
            if (types != null && types.Count > 0)
            {
                Debug.Log($"已注册类型: {string.Join(", ", types)}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 优化的MonoBehaviour单例基类
    /// </summary>
    public abstract class OptimizedSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T s_Instance;
        private static readonly object s_Lock = new object();

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Lock)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = SingletonManager.GetOrCreateMonoBehaviour<T>();
                        }
                    }
                }
                return s_Instance;
            }
        }

        protected virtual void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this as T;
                SingletonManager.SetInstance(s_Instance);
                OnSingletonAwake();
            }
            else if (s_Instance != this)
            {
                GameLog.Warning($"发现重复的单例实例: {typeof(T).Name}, 销毁多余实例", "OptimizedSingleton");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
                SingletonManager.RemoveInstance<T>();
                OnSingletonDestroy();
            }
        }

        /// <summary>
        /// 单例初始化时调用
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 单例销毁时调用
        /// </summary>
        protected virtual void OnSingletonDestroy() { }

        /// <summary>
        /// 检查实例是否有效
        /// </summary>
        public static bool IsValid => s_Instance != null && s_Instance.gameObject != null;
    }

    /// <summary>
    /// 服务单例基类
    /// </summary>
    public abstract class ServiceSingleton<T> where T : class, new()
    {
        private static T s_Instance;
        private static readonly object s_Lock = new object();

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Lock)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = SingletonManager.GetOrCreateService<T>();
                        }
                    }
                }
                return s_Instance;
            }
        }

        protected ServiceSingleton()
        {
            if (s_Instance == null)
            {
                s_Instance = this as T;
                SingletonManager.SetInstance(s_Instance);
                OnInstanceCreated();
            }
        }

        /// <summary>
        /// 实例创建时调用
        /// </summary>
        protected virtual void OnInstanceCreated() { }

        /// <summary>
        /// 销毁单例
        /// </summary>
        public static void DestroyInstance()
        {
            lock (s_Lock)
            {
                if (s_Instance != null)
                {
                    SingletonManager.RemoveInstance<T>();
                    s_Instance = null;
                }
            }
        }
    }
}