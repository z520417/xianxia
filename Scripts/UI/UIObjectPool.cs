using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// UI对象池管理器
    /// 用于高效管理UI元素的创建和销毁，减少GC压力
    /// </summary>
    public class UIObjectPool : MonoBehaviour
    {
        private static UIObjectPool s_Instance;
        public static UIObjectPool Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<UIObjectPool>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("UIObjectPool");
                        s_Instance = go.AddComponent<UIObjectPool>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        // 对象池字典，按预制体名称分类
        private Dictionary<string, Queue<GameObject>> m_PoolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        // 已激活对象的跟踪
        private Dictionary<GameObject, string> m_ActiveObjects = new Dictionary<GameObject, string>();
        
        // 对象池配置
        [Header("对象池配置")]
        [SerializeField] private int m_DefaultPoolSize = 10;
        [SerializeField] private int m_MaxPoolSize = 50;
        [SerializeField] private bool m_AutoExpand = true;
        
        [Header("调试信息")]
        [SerializeField] private bool m_ShowDebugInfo = false;

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
        /// 预热对象池，预创建指定数量的对象
        /// </summary>
        public void WarmupPool(GameObject prefab, int count = -1)
        {
            if (prefab == null) return;

            string poolKey = GetPoolKey(prefab);
            if (count < 0) count = m_DefaultPoolSize;

            if (!m_PoolDictionary.ContainsKey(poolKey))
            {
                m_PoolDictionary[poolKey] = new Queue<GameObject>();
            }

            var pool = m_PoolDictionary[poolKey];
            
            for (int i = 0; i < count; i++)
            {
                if (pool.Count >= m_MaxPoolSize) break;
                
                GameObject obj = CreatePooledObject(prefab, poolKey);
                pool.Enqueue(obj);
            }

            if (m_ShowDebugInfo)
            {
                Debug.Log($"[UIObjectPool] 预热对象池 {poolKey}: {pool.Count} 个对象");
            }
        }

        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        public GameObject GetFromPool(GameObject prefab, Transform parent = null)
        {
            if (prefab == null) return null;

            string poolKey = GetPoolKey(prefab);
            
            if (!m_PoolDictionary.ContainsKey(poolKey))
            {
                m_PoolDictionary[poolKey] = new Queue<GameObject>();
            }

            var pool = m_PoolDictionary[poolKey];
            GameObject obj;

            if (pool.Count > 0)
            {
                // 从池中获取对象
                obj = pool.Dequeue();
            }
            else if (m_AutoExpand)
            {
                // 自动扩展池大小
                obj = CreatePooledObject(prefab, poolKey);
                
                if (m_ShowDebugInfo)
                {
                    Debug.Log($"[UIObjectPool] 自动扩展对象池 {poolKey}");
                }
            }
            else
            {
                Debug.LogWarning($"[UIObjectPool] 对象池 {poolKey} 已耗尽且不允许自动扩展");
                return null;
            }

            // 激活对象并设置父级
            obj.SetActive(true);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            // 记录激活对象
            m_ActiveObjects[obj] = poolKey;

            return obj;
        }

        /// <summary>
        /// 将对象返回到对象池
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            if (!m_ActiveObjects.ContainsKey(obj))
            {
                Debug.LogWarning($"[UIObjectPool] 尝试回收不属于对象池的对象: {obj.name}");
                return;
            }

            string poolKey = m_ActiveObjects[obj];
            m_ActiveObjects.Remove(obj);

            // 重置对象状态
            obj.SetActive(false);
            obj.transform.SetParent(transform, false);

            // 回收组件状态
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();

            // 添加到对象池
            if (!m_PoolDictionary.ContainsKey(poolKey))
            {
                m_PoolDictionary[poolKey] = new Queue<GameObject>();
            }

            var pool = m_PoolDictionary[poolKey];
            
            if (pool.Count < m_MaxPoolSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                // 池已满，直接销毁对象
                Destroy(obj);
                
                if (m_ShowDebugInfo)
                {
                    Debug.Log($"[UIObjectPool] 对象池 {poolKey} 已满，销毁多余对象");
                }
            }
        }

        /// <summary>
        /// 清空指定预制体的对象池
        /// </summary>
        public void ClearPool(GameObject prefab)
        {
            if (prefab == null) return;

            string poolKey = GetPoolKey(prefab);
            ClearPool(poolKey);
        }

        /// <summary>
        /// 清空指定键的对象池
        /// </summary>
        public void ClearPool(string poolKey)
        {
            if (!m_PoolDictionary.ContainsKey(poolKey)) return;

            var pool = m_PoolDictionary[poolKey];
            
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            m_PoolDictionary.Remove(poolKey);

            if (m_ShowDebugInfo)
            {
                Debug.Log($"[UIObjectPool] 清空对象池: {poolKey}");
            }
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            // 回收所有激活的对象
            var activeObjects = new List<GameObject>(m_ActiveObjects.Keys);
            foreach (var obj in activeObjects)
            {
                ReturnToPool(obj);
            }

            // 销毁所有池中的对象
            foreach (var kvp in m_PoolDictionary)
            {
                while (kvp.Value.Count > 0)
                {
                    GameObject obj = kvp.Value.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }

            m_PoolDictionary.Clear();
            m_ActiveObjects.Clear();

            if (m_ShowDebugInfo)
            {
                Debug.Log("[UIObjectPool] 清空所有对象池");
            }
        }

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        public Dictionary<string, int> GetPoolStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var kvp in m_PoolDictionary)
            {
                stats[kvp.Key] = kvp.Value.Count;
            }

            return stats;
        }

        /// <summary>
        /// 获取活跃对象数量
        /// </summary>
        public int GetActiveObjectCount()
        {
            return m_ActiveObjects.Count;
        }

        private string GetPoolKey(GameObject prefab)
        {
            return prefab.name;
        }

        private GameObject CreatePooledObject(GameObject prefab, string poolKey)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = $"{poolKey}_pooled";
            obj.transform.SetParent(transform, false);
            obj.SetActive(false);

            // 添加对象池组件标识
            if (obj.GetComponent<PooledObject>() == null)
            {
                obj.AddComponent<PooledObject>();
            }

            return obj;
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }

#if UNITY_EDITOR
        [ContextMenu("打印对象池统计")]
        private void PrintPoolStatistics()
        {
            Debug.Log("=== UI对象池统计 ===");
            
            var stats = GetPoolStatistics();
            foreach (var kvp in stats)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} 个对象");
            }
            
            Debug.Log($"活跃对象总数: {GetActiveObjectCount()}");
        }

        [ContextMenu("清空所有对象池")]
        private void ClearAllPoolsInEditor()
        {
            ClearAllPools();
        }
#endif
    }

    /// <summary>
    /// 可池化对象接口
    /// </summary>
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// 池化对象组件标识
    /// </summary>
    public class PooledObject : MonoBehaviour, IPoolable
    {
        public virtual void OnGetFromPool()
        {
            // 重置对象状态
        }

        public virtual void OnReturnToPool()
        {
            // 清理对象状态
        }

        /// <summary>
        /// 返回到对象池
        /// </summary>
        public void ReturnToPool()
        {
            UIObjectPool.Instance?.ReturnToPool(gameObject);
        }
    }
}