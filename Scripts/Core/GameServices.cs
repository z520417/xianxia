using System;
using System.Collections.Generic;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏状态服务实现
    /// </summary>
    public class GameStateService : IGameStateService
    {
        private GameState m_CurrentState = GameState.MainMenu;
        private GameState m_PreviousState = GameState.MainMenu;

        public GameState CurrentState => m_CurrentState;
        public GameState PreviousState => m_PreviousState;

        public event Action<GameState, GameState> OnStateChanged;
        public event Action<GameState> OnStateEnter;
        public event Action<GameState> OnStateExit;

        public void ChangeState(GameState newState)
        {
            if (m_CurrentState == newState) return;

            if (!CanChangeState(newState))
            {
                GameLog.Warning($"无法从状态 {m_CurrentState} 切换到 {newState}", "GameState");
                return;
            }

            GameState oldState = m_CurrentState;
            
            // 退出当前状态
            OnStateExit?.Invoke(oldState);
            
            // 更新状态
            m_PreviousState = oldState;
            m_CurrentState = newState;
            
            // 进入新状态
            OnStateEnter?.Invoke(newState);
            OnStateChanged?.Invoke(oldState, newState);

            GameLog.Info($"游戏状态从 {oldState} 切换到 {newState}", "GameState");
        }

        public bool CanChangeState(GameState newState)
        {
            // 基本状态切换规则
            switch (m_CurrentState)
            {
                case GameState.MainMenu:
                    return newState == GameState.Exploring;
                    
                case GameState.Exploring:
                    return newState == GameState.Battle || 
                           newState == GameState.Inventory || 
                           newState == GameState.Paused ||
                           newState == GameState.MainMenu;
                    
                case GameState.Battle:
                    return newState == GameState.Exploring ||
                           newState == GameState.Paused;
                    
                case GameState.Inventory:
                    return newState == GameState.Exploring ||
                           newState == GameState.Paused;
                    
                case GameState.Paused:
                    return newState == m_PreviousState ||
                           newState == GameState.MainMenu;
                    
                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// 统计服务实现
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private int m_ExplorationCount = 0;
        private int m_BattleWins = 0;
        private int m_BattleDefeats = 0;
        private int m_TreasuresFound = 0;
        private int m_ItemsCollected = 0;
        private float m_TotalPlayTime = 0f;

        public int ExplorationCount => m_ExplorationCount;
        public int BattleWins => m_BattleWins;
        public int BattleDefeats => m_BattleDefeats;
        public int TreasuresFound => m_TreasuresFound;
        public int ItemsCollected => m_ItemsCollected;
        public float TotalPlayTime => m_TotalPlayTime;

        public event Action<string, object> OnStatisticChanged;

        public void RecordExploration()
        {
            m_ExplorationCount++;
            OnStatisticChanged?.Invoke(nameof(ExplorationCount), m_ExplorationCount);
            GameLog.Debug($"记录探索次数: {m_ExplorationCount}", "Statistics");
        }

        public void RecordBattleWin()
        {
            m_BattleWins++;
            OnStatisticChanged?.Invoke(nameof(BattleWins), m_BattleWins);
            GameLog.Debug($"记录战斗胜利: {m_BattleWins}", "Statistics");
        }

        public void RecordBattleDefeat()
        {
            m_BattleDefeats++;
            OnStatisticChanged?.Invoke(nameof(BattleDefeats), m_BattleDefeats);
            GameLog.Debug($"记录战斗失败: {m_BattleDefeats}", "Statistics");
        }

        public void RecordTreasureFound()
        {
            m_TreasuresFound++;
            OnStatisticChanged?.Invoke(nameof(TreasuresFound), m_TreasuresFound);
            GameLog.Debug($"记录发现宝藏: {m_TreasuresFound}", "Statistics");
        }

        public void RecordItemCollected()
        {
            m_ItemsCollected++;
            OnStatisticChanged?.Invoke(nameof(ItemsCollected), m_ItemsCollected);
            GameLog.Debug($"记录收集物品: {m_ItemsCollected}", "Statistics");
        }

        public void UpdatePlayTime(float deltaTime)
        {
            m_TotalPlayTime += deltaTime;
            // 不为每次更新触发事件，会太频繁
        }

        public Dictionary<string, object> GetAllStatistics()
        {
            return new Dictionary<string, object>
            {
                { nameof(ExplorationCount), m_ExplorationCount },
                { nameof(BattleWins), m_BattleWins },
                { nameof(BattleDefeats), m_BattleDefeats },
                { nameof(TreasuresFound), m_TreasuresFound },
                { nameof(ItemsCollected), m_ItemsCollected },
                { nameof(TotalPlayTime), m_TotalPlayTime }
            };
        }

        public void ResetStatistics()
        {
            m_ExplorationCount = 0;
            m_BattleWins = 0;
            m_BattleDefeats = 0;
            m_TreasuresFound = 0;
            m_ItemsCollected = 0;
            m_TotalPlayTime = 0f;

            GameLog.Info("统计数据已重置", "Statistics");
        }
    }

    /// <summary>
    /// 探索服务实现
    /// </summary>
    public class ExplorationService : IExplorationService
    {
        private readonly IConfigService m_ConfigService;
        private readonly IEventService m_EventService;
        private readonly IStatisticsService m_StatisticsService;

        public event Action OnExplorationStarted;
        public event Action<ExplorationResult> OnExplorationCompleted;

        public ExplorationService()
        {
            m_ConfigService = ServiceLocator.GetService<IConfigService>();
            m_EventService = ServiceLocator.GetService<IEventService>();
            m_StatisticsService = ServiceLocator.GetService<IStatisticsService>();
        }

        public void StartExploration()
        {
            GameLog.Info("开始探索", "Exploration");
            
            m_StatisticsService?.RecordExploration();
            OnExplorationStarted?.Invoke();
            
            // 触发探索事件
            m_EventService?.TriggerEvent(GameEventType.ExplorationStarted);
        }

        public ExplorationResult ProcessExploration()
        {
            var config = m_ConfigService?.Config?.Exploration;
            if (config == null)
            {
                GameLog.Warning("探索配置不可用，使用默认设置", "Exploration");
                return ProcessExplorationWithDefaults();
            }

            // 构建事件权重数组
            float[] eventChances = {
                config.TreasureChance,
                config.BattleChance,
                config.ItemChance,
                config.NothingChance
            };

            // 根据权重选择探索结果
            int eventType = GetWeightedRandomIndex(eventChances);
            ExplorationResult result = null;

            switch (eventType)
            {
                case 0:
                    result = CreateTreasureResult();
                    break;
                case 1:
                    result = CreateBattleResult();
                    break;
                case 2:
                    result = CreateItemResult();
                    break;
                case 3:
                    result = CreateNothingResult();
                    break;
            }

            OnExplorationCompleted?.Invoke(result);
            return result;
        }

        public void HandleTreasureFound()
        {
            var result = CreateTreasureResult();
            m_StatisticsService?.RecordTreasureFound();
            m_EventService?.TriggerEvent(GameEventType.TreasureFound);
            OnExplorationCompleted?.Invoke(result);
        }

        public void HandleBattleEncounter()
        {
            var result = CreateBattleResult();
            m_EventService?.TriggerEvent(GameEventType.BattleEncountered);
            OnExplorationCompleted?.Invoke(result);
        }

        public void HandleItemFound()
        {
            var result = CreateItemResult();
            m_StatisticsService?.RecordItemCollected();
            m_EventService?.TriggerEvent(GameEventType.ItemFound);
            OnExplorationCompleted?.Invoke(result);
        }

        public void HandleNothingFound()
        {
            var result = CreateNothingResult();
            m_EventService?.TriggerEvent(GameEventType.NothingFound);
            OnExplorationCompleted?.Invoke(result);
        }

        private ExplorationResult ProcessExplorationWithDefaults()
        {
            float[] defaultChances = { 30f, 40f, 20f, 10f };
            int eventType = GetWeightedRandomIndex(defaultChances);

            switch (eventType)
            {
                case 0: return CreateTreasureResult();
                case 1: return CreateBattleResult();
                case 2: return CreateItemResult();
                default: return CreateNothingResult();
            }
        }

        private ExplorationResult CreateTreasureResult()
        {
            var config = m_ConfigService?.Config?.Exploration;
            int minGold = config?.MinGoldReward ?? 50;
            int maxGold = config?.MaxGoldReward ?? 200;

            return new ExplorationResult
            {
                Type = ExplorationResultType.TreasureFound,
                GoldReward = UnityEngine.Random.Range(minGold, maxGold + 1),
                Message = "你发现了一个古老的宝箱！"
            };
        }

        private ExplorationResult CreateBattleResult()
        {
            var gameDataService = ServiceLocator.GetService<IGameDataService>();
            var playerLevel = GameManager.Instance?.PlayerStats?.Level ?? 1;
            var enemy = gameDataService?.GetRandomEnemyByLevel(playerLevel);

            return new ExplorationResult
            {
                Type = ExplorationResultType.BattleEncounter,
                EncounteredEnemy = enemy,
                Message = enemy?.GetRandomBattleMessage() ?? "一个敌人突然出现了！"
            };
        }

        private ExplorationResult CreateItemResult()
        {
            return new ExplorationResult
            {
                Type = ExplorationResultType.ItemFound,
                Message = "你发现了一些有用的物品！"
            };
        }

        private ExplorationResult CreateNothingResult()
        {
            var config = m_ConfigService?.Config?.Exploration;
            int expReward = config?.NothingFoundExp ?? 5;

            return new ExplorationResult
            {
                Type = ExplorationResultType.NothingFound,
                ExperienceReward = expReward,
                Message = "这里似乎什么都没有..."
            };
        }

        private int GetWeightedRandomIndex(float[] weights)
        {
            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }
    }

    /// <summary>
    /// 音效服务实现
    /// </summary>
    public class AudioService : IAudioService
    {
        private AudioSource m_MusicSource;
        private AudioSource m_SFXSource;

        private bool m_IsSFXEnabled = true;
        private bool m_IsMusicEnabled = true;
        private float m_MasterVolume = 1f;
        private float m_SFXVolume = 0.8f;
        private float m_MusicVolume = 0.6f;

        public bool IsSFXEnabled => m_IsSFXEnabled;
        public bool IsMusicEnabled => m_IsMusicEnabled;
        public float MasterVolume => m_MasterVolume;
        public float SFXVolume => m_SFXVolume;
        public float MusicVolume => m_MusicVolume;

        public AudioService()
        {
            InitializeAudioSources();
            LoadAudioSettings();
        }

        public void PlaySFX(string soundName, float volume = 1f)
        {
            if (!m_IsSFXEnabled || m_SFXSource == null) return;

            // 这里应该从音效资源中加载AudioClip
            // var clip = Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
            // if (clip != null)
            // {
            //     m_SFXSource.PlayOneShot(clip, volume * m_SFXVolume * m_MasterVolume);
            // }

            GameLog.Debug($"播放音效: {soundName}", "Audio");
        }

        public void PlayMusic(string musicName, bool loop = true, float volume = 1f)
        {
            if (!m_IsMusicEnabled || m_MusicSource == null) return;

            // 这里应该从音乐资源中加载AudioClip
            // var clip = Resources.Load<AudioClip>($"Audio/Music/{musicName}");
            // if (clip != null)
            // {
            //     m_MusicSource.clip = clip;
            //     m_MusicSource.loop = loop;
            //     m_MusicSource.volume = volume * m_MusicVolume * m_MasterVolume;
            //     m_MusicSource.Play();
            // }

            GameLog.Debug($"播放音乐: {musicName}", "Audio");
        }

        public void StopMusic()
        {
            if (m_MusicSource != null)
            {
                m_MusicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (m_MusicSource != null)
            {
                m_MusicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (m_MusicSource != null)
            {
                m_MusicSource.UnPause();
            }
        }

        public void SetMasterVolume(float volume)
        {
            m_MasterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveAudioSettings();
        }

        public void SetSFXVolume(float volume)
        {
            m_SFXVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveAudioSettings();
        }

        public void SetMusicVolume(float volume)
        {
            m_MusicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveAudioSettings();
        }

        public void SetSFXEnabled(bool enabled)
        {
            m_IsSFXEnabled = enabled;
            SaveAudioSettings();
        }

        public void SetMusicEnabled(bool enabled)
        {
            m_IsMusicEnabled = enabled;
            if (!enabled)
            {
                StopMusic();
            }
            SaveAudioSettings();
        }

        private void InitializeAudioSources()
        {
            var audioObject = new GameObject("AudioManager");
            UnityEngine.Object.DontDestroyOnLoad(audioObject);

            m_MusicSource = audioObject.AddComponent<AudioSource>();
            m_MusicSource.loop = true;

            m_SFXSource = audioObject.AddComponent<AudioSource>();
            m_SFXSource.loop = false;
        }

        private void UpdateVolumes()
        {
            if (m_MusicSource != null)
            {
                m_MusicSource.volume = m_MusicVolume * m_MasterVolume;
            }

            if (m_SFXSource != null)
            {
                m_SFXSource.volume = m_SFXVolume * m_MasterVolume;
            }
        }

        private void LoadAudioSettings()
        {
            m_MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            m_SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            m_MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
            m_IsSFXEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
            m_IsMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

            UpdateVolumes();
        }

        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", m_MasterVolume);
            PlayerPrefs.SetFloat("SFXVolume", m_SFXVolume);
            PlayerPrefs.SetFloat("MusicVolume", m_MusicVolume);
            PlayerPrefs.SetInt("SFXEnabled", m_IsSFXEnabled ? 1 : 0);
            PlayerPrefs.SetInt("MusicEnabled", m_IsMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 游戏数据服务实现
    /// </summary>
    public class GameDataService : IGameDataService
    {
        private List<ItemData> m_AllItems;
        private List<EquipmentData> m_AllEquipments;
        private List<EnemyData> m_AllEnemies;
        private List<ConsumableData> m_AllConsumables;

        public GameDataService()
        {
            LoadGameData();
        }

        public List<ItemData> GetAllItems()
        {
            return m_AllItems ?? new List<ItemData>();
        }

        public ItemData GetItemById(string itemId)
        {
            return m_AllItems?.Find(item => item.name == itemId);
        }

        public ItemData CreateItemFromTemplate(string templateId, ItemRarity rarity, int level)
        {
            var template = GetItemById(templateId);
            if (template == null) return null;

            // 这里应该基于模板创建新的物品实例
            // 暂时返回模板本身
            return template;
        }

        public List<EquipmentData> GetAllEquipments()
        {
            return m_AllEquipments ?? new List<EquipmentData>();
        }

        public EquipmentData GetEquipmentById(string equipmentId)
        {
            return m_AllEquipments?.Find(eq => eq.name == equipmentId);
        }

        public EquipmentData CreateEquipmentFromTemplate(string templateId, EquipmentType type, ItemRarity rarity, int level)
        {
            // 使用现有的EquipmentData.GenerateRandomEquipment方法
            return EquipmentData.GenerateRandomEquipment(type, rarity, level);
        }

        public List<EnemyData> GetAllEnemies()
        {
            return m_AllEnemies ?? new List<EnemyData>();
        }

        public EnemyData GetEnemyById(string enemyId)
        {
            return m_AllEnemies?.Find(enemy => enemy.EnemyId == enemyId);
        }

        public EnemyData GetRandomEnemyByLevel(int level)
        {
            var suitableEnemies = m_AllEnemies?.FindAll(enemy => 
                level >= enemy.MinLevel && level <= enemy.MaxLevel);

            if (suitableEnemies == null || suitableEnemies.Count == 0)
                return null;

            return suitableEnemies[UnityEngine.Random.Range(0, suitableEnemies.Count)];
        }

        public List<ConsumableData> GetAllConsumables()
        {
            return m_AllConsumables ?? new List<ConsumableData>();
        }

        public ConsumableData GetConsumableById(string consumableId)
        {
            return m_AllConsumables?.Find(consumable => consumable.name == consumableId);
        }

        public ConsumableData CreateConsumableFromTemplate(string templateId, ItemRarity rarity, int level)
        {
            // 使用现有的ConsumableData.GenerateRandomConsumable方法
            return ConsumableData.GenerateRandomConsumable(rarity, level);
        }

        private void LoadGameData()
        {
            // 从Resources文件夹加载所有游戏数据
            m_AllItems = new List<ItemData>(Resources.LoadAll<ItemData>("Data/Items"));
            m_AllEquipments = new List<EquipmentData>(Resources.LoadAll<EquipmentData>("Data/Equipments"));
            m_AllEnemies = new List<EnemyData>(Resources.LoadAll<EnemyData>("Data/Enemies"));
            m_AllConsumables = new List<ConsumableData>(Resources.LoadAll<ConsumableData>("Data/Consumables"));

            GameLog.Info($"加载游戏数据完成 - 物品: {m_AllItems.Count}, 装备: {m_AllEquipments.Count}, " +
                        $"敌人: {m_AllEnemies.Count}, 消耗品: {m_AllConsumables.Count}", "GameData");
        }
    }
}