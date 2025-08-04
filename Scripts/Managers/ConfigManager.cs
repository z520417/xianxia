using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 配置管理器 - 单例模式
    /// 负责加载和管理游戏配置数据
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        private static ConfigManager s_Instance;
        public static ConfigManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<ConfigManager>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("ConfigManager");
                        s_Instance = go.AddComponent<ConfigManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        [Header("游戏配置")]
        [SerializeField] private GameConfig m_GameConfig;

        private const string DEFAULT_CONFIG_PATH = "Config/GameConfig";

        public GameConfig Config => m_GameConfig;

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadConfig();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfig()
        {
            if (m_GameConfig == null)
            {
                m_GameConfig = Resources.Load<GameConfig>(DEFAULT_CONFIG_PATH);
                
                if (m_GameConfig == null)
                {
                    Debug.LogError($"无法加载游戏配置文件: {DEFAULT_CONFIG_PATH}");
                    CreateDefaultConfig();
                }
                else
                {
                    Debug.Log("游戏配置加载成功");
                }
            }
        }

        /// <summary>
        /// 创建默认配置（当配置文件不存在时）
        /// </summary>
        private void CreateDefaultConfig()
        {
#if UNITY_EDITOR
            m_GameConfig = ScriptableObject.CreateInstance<GameConfig>();
            
            // 在编辑器中创建默认配置文件
            string assetPath = "Assets/Resources/Config/GameConfig.asset";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(assetPath));
            UnityEditor.AssetDatabase.CreateAsset(m_GameConfig, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log("已创建默认配置文件: " + assetPath);
#else
            Debug.LogError("运行时无法创建配置文件，请在编辑器中创建GameConfig资源");
#endif
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            m_GameConfig = null;
            LoadConfig();
        }

        /// <summary>
        /// 验证配置文件的完整性
        /// </summary>
        public bool ValidateConfig()
        {
            if (m_GameConfig == null)
            {
                Debug.LogError("配置文件为空");
                return false;
            }

            // 验证各个配置模块
            if (m_GameConfig.CharacterStats == null)
            {
                Debug.LogError("角色属性配置缺失");
                return false;
            }

            if (m_GameConfig.Exploration == null)
            {
                Debug.LogError("探索配置缺失");
                return false;
            }

            if (m_GameConfig.Battle == null)
            {
                Debug.LogError("战斗配置缺失");
                return false;
            }

            if (m_GameConfig.Inventory == null)
            {
                Debug.LogError("背包配置缺失");
                return false;
            }

            if (m_GameConfig.UI == null)
            {
                Debug.LogError("UI配置缺失");
                return false;
            }

            if (m_GameConfig.Audio == null)
            {
                Debug.LogError("音效配置缺失");
                return false;
            }

            Debug.Log("配置文件验证通过");
            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("验证配置")]
        private void ValidateConfigInEditor()
        {
            ValidateConfig();
        }

        [ContextMenu("重新加载配置")]
        private void ReloadConfigInEditor()
        {
            ReloadConfig();
        }
#endif
    }
}