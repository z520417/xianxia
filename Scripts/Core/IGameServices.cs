using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame.Core
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigService
    {
        GameConfig Config { get; }
        void ReloadConfig();
        bool ValidateConfig();
    }

    /// <summary>
    /// 事件服务接口
    /// </summary>
    public interface IEventService
    {
        void AddListener(GameEventType eventType, Action<GameEventData> listener);
        void RemoveListener(GameEventType eventType, Action<GameEventData> listener);
        void TriggerEvent(GameEventType eventType, GameEventData eventData = null);
        List<(GameEventType, GameEventData, float)> GetEventHistory();
        void ClearEventHistory();
    }

    /// <summary>
    /// 存档服务接口
    /// </summary>
    public interface ISaveService
    {
        bool SaveGame(int slotIndex, string saveName = "");
        bool LoadGame(int slotIndex);
        List<GameSaveData> GetAllSaveInfo();
        bool DeleteSave(int slotIndex);
        GameSaveData CreateSaveData();
        
        event Action<GameSaveData> OnGameSaved;
        event Action<GameSaveData> OnGameLoaded;
        event Action<string> OnSaveError;
    }

    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILoggingService
    {
        void Log(string message, LogLevel level = LogLevel.Info, string category = "Game");
        void LogDetailed(string message, LogLevel level = LogLevel.Info, string category = "Game");
        void LogException(Exception exception, string category = "Game");
        void LogFormat(LogLevel level, string category, string format, params object[] args);
        
        List<LogEntry> GetLogEntries(LogLevel? minLevel = null, string category = null);
        void ClearLogs();
        void SetMinLogLevel(LogLevel minLevel);
        
        event Action<LogEntry> OnLogEntryAdded;
    }

    /// <summary>
    /// UI更新服务接口
    /// </summary>
    public interface IUIUpdateService
    {
        void RegisterUpdateHandler(UIUpdateType updateType, Action<object> handler);
        void UnregisterUpdateHandler(UIUpdateType updateType);
        void RequestUpdate(UIUpdateType updateType, object data = null, bool isCritical = false);
        void ForceUpdate(UIUpdateType updateType, object data = null);
        void RequestBatchUpdate(params UIUpdateType[] updateTypes);
        void ClearUpdateQueue();
        void SetUpdateInterval(float interval);
        
        event Action<UIUpdateType, object> OnUIUpdateProcessed;
    }

    /// <summary>
    /// UI对象池服务接口
    /// </summary>
    public interface IUIObjectPoolService
    {
        void WarmupPool(GameObject prefab, int count = -1);
        GameObject GetFromPool(GameObject prefab, Transform parent = null);
        void ReturnToPool(GameObject obj);
        void ClearPool(GameObject prefab);
        void ClearPool(string poolKey);
        void ClearAllPools();
        Dictionary<string, int> GetPoolStatistics();
        int GetActiveObjectCount();
    }

    /// <summary>
    /// 游戏数据服务接口（新增）
    /// </summary>
    public interface IGameDataService
    {
        // 物品数据
        List<ItemData> GetAllItems();
        ItemData GetItemById(string itemId);
        ItemData CreateItemFromTemplate(string templateId, ItemRarity rarity, int level);
        
        // 装备数据
        List<EquipmentData> GetAllEquipments();
        EquipmentData GetEquipmentById(string equipmentId);
        EquipmentData CreateEquipmentFromTemplate(string templateId, EquipmentType type, ItemRarity rarity, int level);
        
        // 敌人数据
        List<EnemyData> GetAllEnemies();
        EnemyData GetEnemyById(string enemyId);
        EnemyData GetRandomEnemyByLevel(int level);
        
        // 消耗品数据
        List<ConsumableData> GetAllConsumables();
        ConsumableData GetConsumableById(string consumableId);
        ConsumableData CreateConsumableFromTemplate(string templateId, ItemRarity rarity, int level);
    }

    /// <summary>
    /// 探索服务接口（新增）
    /// </summary>
    public interface IExplorationService
    {
        void StartExploration();
        ExplorationResult ProcessExploration();
        void HandleTreasureFound();
        void HandleBattleEncounter();
        void HandleItemFound();
        void HandleNothingFound();
        
        event Action OnExplorationStarted;
        event Action<ExplorationResult> OnExplorationCompleted;
    }

    /// <summary>
    /// 探索结果
    /// </summary>
    public enum ExplorationResultType
    {
        TreasureFound,
        BattleEncounter,
        ItemFound,
        NothingFound
    }

    /// <summary>
    /// 探索结果数据
    /// </summary>
    public class ExplorationResult
    {
        public ExplorationResultType Type { get; set; }
        public List<ItemData> ItemsFound { get; set; } = new List<ItemData>();
        public int GoldReward { get; set; }
        public int ExperienceReward { get; set; }
        public string Message { get; set; }
        public EnemyData EncounteredEnemy { get; set; }
    }

    /// <summary>
    /// 统计服务接口（新增）
    /// </summary>
    public interface IStatisticsService
    {
        // 玩家统计
        int ExplorationCount { get; }
        int BattleWins { get; }
        int BattleDefeats { get; }
        int TreasuresFound { get; }
        int ItemsCollected { get; }
        float TotalPlayTime { get; }
        
        // 统计方法
        void RecordExploration();
        void RecordBattleWin();
        void RecordBattleDefeat();
        void RecordTreasureFound();
        void RecordItemCollected();
        void UpdatePlayTime(float deltaTime);
        
        // 获取统计信息
        Dictionary<string, object> GetAllStatistics();
        void ResetStatistics();
        
        event Action<string, object> OnStatisticChanged;
    }

    /// <summary>
    /// 音效服务接口（新增）
    /// </summary>
    public interface IAudioService
    {
        // 音效控制
        void PlaySFX(string soundName, float volume = 1f);
        void PlayMusic(string musicName, bool loop = true, float volume = 1f);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
        
        // 音量控制
        void SetMasterVolume(float volume);
        void SetSFXVolume(float volume);
        void SetMusicVolume(float volume);
        
        // 配置
        void SetSFXEnabled(bool enabled);
        void SetMusicEnabled(bool enabled);
        
        bool IsSFXEnabled { get; }
        bool IsMusicEnabled { get; }
        float MasterVolume { get; }
        float SFXVolume { get; }
        float MusicVolume { get; }
    }

    /// <summary>
    /// 游戏状态服务接口（新增）
    /// </summary>
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        GameState PreviousState { get; }
        
        void ChangeState(GameState newState);
        bool CanChangeState(GameState newState);
        
        event Action<GameState, GameState> OnStateChanged; // previous, current
        event Action<GameState> OnStateEnter;
        event Action<GameState> OnStateExit;
    }
}

namespace XianXiaGame
{
    /// <summary>
    /// 敌人数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy", menuName = "仙侠游戏/敌人数据")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本信息")]
        public string EnemyId;
        public string EnemyName;
        public string Description;
        public Sprite Icon;
        
        [Header("属性配置")]
        public int BaseLevel = 1;
        public int MinLevel = 1;
        public int MaxLevel = 50;
        
        [Header("属性倍数")]
        public float HealthMultiplier = 1f;
        public float AttackMultiplier = 1f;
        public float DefenseMultiplier = 1f;
        public float SpeedMultiplier = 1f;
        
        [Header("AI行为")]
        public float AttackChance = 0.8f;
        public float DefendChance = 0.2f;
        public float SpecialSkillChance = 0f;
        
        [Header("奖励配置")]
        public int BaseExperienceReward = 25;
        public int BaseGoldReward = 10;
        public List<ItemDropData> PossibleDrops = new List<ItemDropData>();
        
        [Header("外观")]
        public Color NameColor = Color.white;
        public string[] BattleMessages = new string[0];

        /// <summary>
        /// 创建指定等级的敌人角色属性
        /// </summary>
        public CharacterStats CreateStatsForLevel(int level)
        {
            int actualLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
            var stats = new CharacterStats(actualLevel);
            
            // 应用属性倍数
            stats.ModifyStats(HealthMultiplier, AttackMultiplier, DefenseMultiplier, SpeedMultiplier);
            
            return stats;
        }

        /// <summary>
        /// 获取随机战斗消息
        /// </summary>
        public string GetRandomBattleMessage()
        {
            if (BattleMessages.Length == 0)
                return $"{EnemyName}出现了！";
            
            return BattleMessages[UnityEngine.Random.Range(0, BattleMessages.Length)];
        }
    }

    /// <summary>
    /// 物品掉落数据
    /// </summary>
    [System.Serializable]
    public class ItemDropData
    {
        public string ItemId;
        public float DropChance = 0.1f;
        public int MinQuantity = 1;
        public int MaxQuantity = 1;
        public ItemRarity ForcedRarity = ItemRarity.Common;
        public bool UseRandomRarity = true;
    }
}