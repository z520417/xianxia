using UnityEngine;

namespace XianXiaGame.Core
{
    /// <summary>
    /// 游戏常量配置
    /// 统一管理所有魔法数字和硬编码值
    /// </summary>
    public static class GameConstants
    {
        #region 玩家默认值

        public static class Player
        {
            public const string DEFAULT_NAME = "无名道友";
            public const int DEFAULT_STARTING_GOLD = 1000;
            public const int DEFAULT_STARTING_LEVEL = 1;
            public const int DEFAULT_STARTING_EXPERIENCE = 0;
        }

        #endregion

        #region 战斗系统

        public static class Battle
        {
            public const float DEFAULT_ACTION_DELAY = 1.5f;
            public const float DEFAULT_ESCAPE_CHANCE = 0.3f;
            public const float DEFAULT_DEFENSE_REDUCTION = 0.5f;
            public const int MAX_BATTLE_TURNS = 100;
            public const float CRITICAL_HIT_MULTIPLIER = 1.5f;
        }

        #endregion

        #region 物品系统

        public static class Items
        {
            public const int DEFAULT_MAX_STACK_SIZE = 99;
            public const int EQUIPMENT_MAX_STACK_SIZE = 1;
            public const int DEFAULT_ITEM_VALUE = 10;
            public const float RARITY_VALUE_MULTIPLIER = 0.5f;
            public const float LEVEL_VALUE_MULTIPLIER = 0.1f;
        }

        #endregion

        #region 背包系统

        public static class Inventory
        {
            public const int DEFAULT_INITIAL_SLOTS = 20;
            public const int DEFAULT_MAX_SLOTS = 100;
            public const int DEFAULT_SLOT_UPGRADE_COST = 1000;
            public const int MAX_UPGRADE_SLOTS = 80;
        }

        #endregion

        #region 探索系统

        public static class Exploration
        {
            public const float DEFAULT_TREASURE_CHANCE = 25f;
            public const float DEFAULT_BATTLE_CHANCE = 40f;
            public const float DEFAULT_ITEM_CHANCE = 25f;
            public const float DEFAULT_NOTHING_CHANCE = 10f;
            
            public const int MIN_GOLD_REWARD = 50;
            public const int MAX_GOLD_REWARD = 200;
            public const int NOTHING_FOUND_EXP = 5;
        }

        #endregion

        #region UI系统

        public static class UI
        {
            public const float DEFAULT_UPDATE_INTERVAL = 0.1f;
            public const float CRITICAL_UPDATE_INTERVAL = 0.05f;
            public const int MAX_UPDATES_PER_FRAME = 5;
            public const int MAX_MESSAGE_LINES = 20;
            public const float MESSAGE_FADE_TIME = 0.5f;
            public const float UI_FADE_SPEED = 2f;
            public const float BUTTON_SCALE_EFFECT = 1.1f;
        }

        #endregion

        #region 音频系统

        public static class Audio
        {
            public const float DEFAULT_MASTER_VOLUME = 1f;
            public const float DEFAULT_SFX_VOLUME = 0.8f;
            public const float DEFAULT_MUSIC_VOLUME = 0.6f;
            public const bool DEFAULT_SFX_ENABLED = true;
            public const bool DEFAULT_MUSIC_ENABLED = true;
        }

        #endregion

        #region 性能配置

        public static class Performance
        {
            public const int COMPONENT_CACHE_CLEANUP_INTERVAL = 60; // 秒
            public const int MAX_COMPONENT_CACHE_SIZE = 100;
            public const int MAX_UI_OBJECT_POOL_SIZE = 50;
            public const float GARBAGE_COLLECTION_INTERVAL = 30f; // 秒
        }

        #endregion

        #region 存档系统

        public static class Save
        {
            public const int MAX_SAVE_SLOTS = 10;
            public const string DEFAULT_SAVE_NAME = "自动存档";
            public const string SAVE_FILE_EXTENSION = ".sav";
            public const string BACKUP_FILE_EXTENSION = ".bak";
            public const bool DEFAULT_ENCRYPT_SAVES = true;
        }

        #endregion

        #region 角色属性

        public static class CharacterStats
        {
            public const int BASE_HEALTH = 100;
            public const int BASE_MANA = 50;
            public const int BASE_ATTACK = 15;
            public const int BASE_DEFENSE = 8;
            public const int BASE_SPEED = 12;
            public const int BASE_LUCK = 10;
            
            public const int HEALTH_PER_LEVEL = 20;
            public const int MANA_PER_LEVEL = 10;
            public const int ATTACK_PER_LEVEL = 3;
            public const int DEFENSE_PER_LEVEL = 2;
            public const int SPEED_PER_LEVEL = 1;
            
            public const int BASE_EXPERIENCE = 100;
            public const int EXPERIENCE_PER_LEVEL = 25;
            public const float EXPERIENCE_GROWTH_RATE = 1.15f;
            
            public const float BASE_CRITICAL_RATE = 0.05f;
            public const float BASE_CRITICAL_DAMAGE = 1.5f;
        }

        #endregion

        #region 敌人配置

        public static class Enemy
        {
            public const int LEVEL_VARIANCE = 2;
            public const float DIFFICULTY_MIN = 0.8f;
            public const float DIFFICULTY_MAX = 1.2f;
            public const int BASE_EXPERIENCE_REWARD = 25;
            public const int BASE_GOLD_REWARD = 15;
            public const float ITEM_REWARD_CHANCE = 0.3f;
        }

        #endregion

        #region 文件路径

        public static class Paths
        {
            public const string CONFIG_FOLDER = "Config/";
            public const string DATA_FOLDER = "Data/";
            public const string SAVE_FOLDER = "Saves/";
            public const string TEMPLATE_FOLDER = "Templates/";
            public const string AUDIO_FOLDER = "Audio/";
            public const string UI_FOLDER = "UI/";
            
            public const string GAME_CONFIG_FILE = "GameConfig";
            public const string MESSAGE_CONFIG_FILE = "MessageConfig";
            public const string ITEM_GENERATOR_CONFIG_FILE = "ItemGeneratorConfig";
        }

        #endregion

        #region 调试配置

        public static class Debug
        {
            public const bool ENABLE_PERFORMANCE_MONITORING = true;
            public const bool ENABLE_COMPONENT_TRACKING = true;
            public const bool ENABLE_UI_DEBUG = false;
            public const bool ENABLE_SAVE_DEBUG = true;
            public const bool ENABLE_BATTLE_DEBUG = false;
            
            public const string LOG_DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
            public const int MAX_LOG_ENTRIES = 1000;
            public const float LOG_CLEANUP_INTERVAL = 300f; // 秒
        }

        #endregion

        #region 游戏平衡

        public static class Balance
        {
            // 稀有度权重
            public static readonly float[] RarityWeights = { 50f, 25f, 15f, 7f, 2.5f, 0.5f };
            
            // 物品类型权重
            public static readonly float[] ItemTypeWeights = { 40f, 35f, 20f, 5f };
            
            // 探索事件权重
            public static readonly float[] ExplorationEventWeights = { 25f, 40f, 25f, 10f };
            
            // 经验值计算
            public const float LUCK_EFFECT_MULTIPLIER = 0.01f;
            public const float LEVEL_SCALE_MULTIPLIER = 0.1f;
            
            // 金钱损失
            public const float GOLD_LOSS_PERCENTAGE = 0.1f;
            public const int MAX_GOLD_LOSS = 100;
        }

        #endregion

        #region 本地化

        public static class Localization
        {
            public const string DEFAULT_LANGUAGE = "zh-CN";
            public const string FALLBACK_LANGUAGE = "en-US";
            public const string LOCALIZATION_FOLDER = "Localization/";
        }

        #endregion

        #region 网络配置

        public static class Network
        {
            public const float CONNECTION_TIMEOUT = 10f;
            public const int MAX_RETRY_ATTEMPTS = 3;
            public const float RETRY_DELAY = 2f;
            public const int NETWORK_BUFFER_SIZE = 1024;
        }

        #endregion

        #region 版本信息

        public static class Version
        {
            public const string GAME_VERSION = "1.0.0";
            public const string BUILD_VERSION = "1.0.0.1";
            public const string SAVE_VERSION = "1.0";
            public const string CONFIG_VERSION = "1.0";
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 根据稀有度获取价值倍数
        /// </summary>
        public static float GetRarityValueMultiplier(ItemRarity rarity)
        {
            return 1f + (int)rarity * Items.RARITY_VALUE_MULTIPLIER;
        }

        /// <summary>
        /// 根据等级获取经验需求
        /// </summary>
        public static int GetExperienceRequired(int level)
        {
            return Mathf.RoundToInt(CharacterStats.BASE_EXPERIENCE * 
                                  Mathf.Pow(CharacterStats.EXPERIENCE_GROWTH_RATE, level - 1));
        }

        /// <summary>
        /// 获取运气影响的概率
        /// </summary>
        public static float GetLuckAffectedChance(float baseChance, int luckValue)
        {
            return Mathf.Clamp01(baseChance + luckValue * Balance.LUCK_EFFECT_MULTIPLIER);
        }

        /// <summary>
        /// 计算等级缩放值
        /// </summary>
        public static float CalculateLevelScale(int level, float baseValue)
        {
            return baseValue * (1f + level * Balance.LEVEL_SCALE_MULTIPLIER);
        }

        /// <summary>
        /// 验证版本兼容性
        /// </summary>
        public static bool IsVersionCompatible(string version)
        {
            if (string.IsNullOrEmpty(version)) return false;
            
            var parts = version.Split('.');
            var currentParts = Version.SAVE_VERSION.Split('.');
            
            if (parts.Length < 2 || currentParts.Length < 2) return false;
            
            // 主版本号必须相同
            return parts[0] == currentParts[0];
        }

        #endregion
    }

    /// <summary>
    /// 运行时配置管理器
    /// 允许在运行时修改某些常量
    /// </summary>
    public class RuntimeConfig : ScriptableObject
    {
        [Header("性能配置")]
        [Range(0.01f, 1f)]
        public float UIUpdateInterval = GameConstants.UI.DEFAULT_UPDATE_INTERVAL;
        
        [Range(1, 20)]
        public int MaxUpdatesPerFrame = GameConstants.UI.MAX_UPDATES_PER_FRAME;
        
        [Header("调试配置")]
        public bool EnablePerformanceMonitoring = GameConstants.Debug.ENABLE_PERFORMANCE_MONITORING;
        public bool EnableComponentTracking = GameConstants.Debug.ENABLE_COMPONENT_TRACKING;
        public bool EnableUIDebug = GameConstants.Debug.ENABLE_UI_DEBUG;
        
        [Header("平衡配置")]
        [Range(0f, 1f)]
        public float LuckEffectMultiplier = GameConstants.Balance.LUCK_EFFECT_MULTIPLIER;
        
        [Range(0f, 0.5f)]
        public float LevelScaleMultiplier = GameConstants.Balance.LEVEL_SCALE_MULTIPLIER;

        private static RuntimeConfig s_Instance;
        public static RuntimeConfig Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<RuntimeConfig>("Config/RuntimeConfig");
                    if (s_Instance == null)
                    {
                        s_Instance = CreateInstance<RuntimeConfig>();
                        GameLog.Warning("RuntimeConfig未找到，使用默认配置", "RuntimeConfig");
                    }
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// 应用运行时配置
        /// </summary>
        public void ApplyRuntimeConfig()
        {
            // 这里可以将运行时配置应用到相应的系统
            var uiUpdateManager = ComponentRegistry.GetComponent<UIUpdateManager>();
            if (uiUpdateManager != null)
            {
                uiUpdateManager.SetUpdateInterval(UIUpdateInterval);
            }

            GameLog.Info("运行时配置已应用", "RuntimeConfig");
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        [ContextMenu("重置为默认值")]
        public void ResetToDefaults()
        {
            UIUpdateInterval = GameConstants.UI.DEFAULT_UPDATE_INTERVAL;
            MaxUpdatesPerFrame = GameConstants.UI.MAX_UPDATES_PER_FRAME;
            EnablePerformanceMonitoring = GameConstants.Debug.ENABLE_PERFORMANCE_MONITORING;
            EnableComponentTracking = GameConstants.Debug.ENABLE_COMPONENT_TRACKING;
            EnableUIDebug = GameConstants.Debug.ENABLE_UI_DEBUG;
            LuckEffectMultiplier = GameConstants.Balance.LUCK_EFFECT_MULTIPLIER;
            LevelScaleMultiplier = GameConstants.Balance.LEVEL_SCALE_MULTIPLIER;

            GameLog.Info("运行时配置已重置为默认值", "RuntimeConfig");
        }
    }
}