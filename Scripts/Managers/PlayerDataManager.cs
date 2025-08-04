using System;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 玩家数据管理器
    /// 专门负责管理玩家的属性、金钱、经验等数据
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        #region 事件
        public event Action<int> OnLevelChanged;
        public event Action<int> OnExperienceChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<int, int> OnManaChanged;   // current, max
        public event Action<CharacterStats> OnStatsChanged;
        #endregion

        #region 玩家数据
        [Header("玩家数据")]
        [SerializeField] private string m_PlayerName = "无名道友";
        [SerializeField] private CharacterStats m_PlayerStats;
        [SerializeField] private int m_Gold = 1000;
        #endregion

        #region 服务引用
        private IConfigService m_ConfigService;
        private IEventService m_EventService;
        private ILoggingService m_LoggingService;
        #endregion

        #region 公共属性
        public string PlayerName 
        { 
            get => m_PlayerName; 
            set 
            { 
                if (m_PlayerName != value)
                {
                    m_PlayerName = value;
                    m_LoggingService?.Log($"玩家名称更改为: {value}", LogLevel.Info, "PlayerData");
                }
            } 
        }
        
        public CharacterStats PlayerStats => m_PlayerStats;
        public int Gold => m_Gold;
        
        public int Level => m_PlayerStats?.Level ?? 1;
        public int Experience => m_PlayerStats?.Experience ?? 0;
        public int CurrentHealth => m_PlayerStats?.CurrentHealth ?? 0;
        public int MaxHealth => m_PlayerStats?.MaxHealth ?? 0;
        public int CurrentMana => m_PlayerStats?.CurrentMana ?? 0;
        public int MaxMana => m_PlayerStats?.MaxMana ?? 0;
        #endregion

        private void Awake()
        {
            InitializeServices();
            InitializePlayerData();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region 初始化方法

        /// <summary>
        /// 初始化服务引用
        /// </summary>
        private void InitializeServices()
        {
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
        }

        /// <summary>
        /// 初始化玩家数据
        /// </summary>
        private void InitializePlayerData()
        {
            if (m_PlayerStats == null)
            {
                m_PlayerStats = new CharacterStats(1);
                m_LoggingService?.Log("创建新的玩家角色数据", LogLevel.Info, "PlayerData");
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 可以订阅其他系统的事件来响应玩家数据变化
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // 清理事件订阅
        }

        #endregion

        #region 玩家数据操作

        /// <summary>
        /// 创建新角色
        /// </summary>
        public void CreateNewCharacter(string playerName = "", int startingLevel = 1)
        {
            m_PlayerName = string.IsNullOrEmpty(playerName) ? "无名道友" : playerName;
            m_PlayerStats = new CharacterStats(startingLevel);
            m_Gold = GetStartingGold();

            // 触发所有变化事件
            TriggerAllChangeEvents();

            m_EventService?.TriggerEvent(GameEventType.PlayerStatsChanged, 
                new GameEventData());

            m_LoggingService?.Log($"创建新角色: {m_PlayerName}, 等级 {startingLevel}", LogLevel.Info, "PlayerData");
        }

        /// <summary>
        /// 设置玩家数据（用于加载存档）
        /// </summary>
        public void SetPlayerData(string playerName, CharacterStats stats, int gold)
        {
            var oldLevel = m_PlayerStats?.Level ?? 1;
            var oldGold = m_Gold;

            m_PlayerName = playerName;
            m_PlayerStats = stats?.Clone();
            m_Gold = gold;

            // 触发相应的变化事件
            if (oldLevel != Level)
                OnLevelChanged?.Invoke(Level);
            
            if (oldGold != m_Gold)
                OnGoldChanged?.Invoke(m_Gold);

            OnExperienceChanged?.Invoke(Experience);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
            OnStatsChanged?.Invoke(m_PlayerStats);

            m_EventService?.TriggerEvent(GameEventType.PlayerStatsChanged);

            m_LoggingService?.Log($"设置玩家数据: {m_PlayerName}, 等级 {Level}, 金钱 {m_Gold}", 
                LogLevel.Info, "PlayerData");
        }

        #endregion

        #region 经验和等级

        /// <summary>
        /// 获得经验值
        /// </summary>
        public bool GainExperience(int experience)
        {
            if (m_PlayerStats == null || experience <= 0) return false;

            int oldLevel = m_PlayerStats.Level;
            bool leveledUp = m_PlayerStats.GainExperience(experience);

            OnExperienceChanged?.Invoke(m_PlayerStats.Experience);

            if (leveledUp)
            {
                OnLevelChanged?.Invoke(m_PlayerStats.Level);
                OnHealthChanged?.Invoke(m_PlayerStats.CurrentHealth, m_PlayerStats.MaxHealth);
                OnManaChanged?.Invoke(m_PlayerStats.CurrentMana, m_PlayerStats.MaxMana);
                OnStatsChanged?.Invoke(m_PlayerStats);

                // 触发等级提升事件
                m_EventService?.TriggerEvent(GameEventType.PlayerLevelUp, 
                    new PlayerLevelUpEventData(m_PlayerStats.Level, oldLevel));

                m_LoggingService?.Log($"玩家等级提升: {oldLevel} -> {m_PlayerStats.Level}", 
                    LogLevel.Info, "PlayerData");
            }
            else
            {
                m_LoggingService?.Log($"玩家获得经验: {experience}", LogLevel.Debug, "PlayerData");
            }

            return leveledUp;
        }

        /// <summary>
        /// 设置经验值（用于存档加载）
        /// </summary>
        public void SetExperience(int experience)
        {
            if (m_PlayerStats != null)
            {
                m_PlayerStats.SetExperience(experience);
                OnExperienceChanged?.Invoke(experience);
            }
        }

        #endregion

        #region 生命值和法力值

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            if (m_PlayerStats == null || amount <= 0) return;

            int oldHealth = m_PlayerStats.CurrentHealth;
            m_PlayerStats.Heal(amount);

            if (oldHealth != m_PlayerStats.CurrentHealth)
            {
                OnHealthChanged?.Invoke(m_PlayerStats.CurrentHealth, m_PlayerStats.MaxHealth);
                
                m_EventService?.TriggerEvent(GameEventType.HealthRestored, 
                    new ValueChangedEventData(m_PlayerStats.CurrentHealth, oldHealth));

                m_LoggingService?.Log($"玩家恢复生命值: {amount}", LogLevel.Debug, "PlayerData");
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (m_PlayerStats == null || damage <= 0) return;

            int oldHealth = m_PlayerStats.CurrentHealth;
            m_PlayerStats.TakeDamage(damage);

            OnHealthChanged?.Invoke(m_PlayerStats.CurrentHealth, m_PlayerStats.MaxHealth);

            m_EventService?.TriggerEvent(GameEventType.PlayerHealthChanged, 
                new ValueChangedEventData(m_PlayerStats.CurrentHealth, oldHealth));

            m_LoggingService?.Log($"玩家受到伤害: {damage}", LogLevel.Debug, "PlayerData");

            // 检查是否死亡
            if (!m_PlayerStats.IsAlive)
            {
                m_LoggingService?.Log("玩家死亡", LogLevel.Warning, "PlayerData");
                // 这里可以触发死亡相关的事件或处理
            }
        }

        /// <summary>
        /// 恢复法力值
        /// </summary>
        public void RestoreMana(int amount)
        {
            if (m_PlayerStats == null || amount <= 0) return;

            int oldMana = m_PlayerStats.CurrentMana;
            m_PlayerStats.RestoreMana(amount);

            if (oldMana != m_PlayerStats.CurrentMana)
            {
                OnManaChanged?.Invoke(m_PlayerStats.CurrentMana, m_PlayerStats.MaxMana);
                
                m_LoggingService?.Log($"玩家恢复法力值: {amount}", LogLevel.Debug, "PlayerData");
            }
        }

        /// <summary>
        /// 消耗法力值
        /// </summary>
        public bool ConsumeMana(int amount)
        {
            if (m_PlayerStats == null || amount <= 0) return false;

            int oldMana = m_PlayerStats.CurrentMana;
            bool success = m_PlayerStats.ConsumeMana(amount);

            if (success && oldMana != m_PlayerStats.CurrentMana)
            {
                OnManaChanged?.Invoke(m_PlayerStats.CurrentMana, m_PlayerStats.MaxMana);
                
                m_LoggingService?.Log($"玩家消耗法力值: {amount}", LogLevel.Debug, "PlayerData");
            }

            return success;
        }

        /// <summary>
        /// 完全治愈
        /// </summary>
        public void FullHeal()
        {
            if (m_PlayerStats == null) return;

            m_PlayerStats.FullHeal();
            OnHealthChanged?.Invoke(m_PlayerStats.CurrentHealth, m_PlayerStats.MaxHealth);
            OnManaChanged?.Invoke(m_PlayerStats.CurrentMana, m_PlayerStats.MaxMana);

            m_LoggingService?.Log("玩家完全恢复", LogLevel.Info, "PlayerData");
        }

        #endregion

        #region 金钱管理

        /// <summary>
        /// 添加金钱
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            int oldGold = m_Gold;
            m_Gold += amount;

            OnGoldChanged?.Invoke(m_Gold);
            
            m_EventService?.TriggerEvent(GameEventType.PlayerGoldChanged, 
                new ValueChangedEventData(m_Gold, oldGold));

            m_LoggingService?.Log($"玩家获得金钱: {amount}, 总计: {m_Gold}", LogLevel.Debug, "PlayerData");
        }

        /// <summary>
        /// 花费金钱
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || m_Gold < amount) return false;

            int oldGold = m_Gold;
            m_Gold -= amount;

            OnGoldChanged?.Invoke(m_Gold);
            
            m_EventService?.TriggerEvent(GameEventType.PlayerGoldChanged, 
                new ValueChangedEventData(m_Gold, oldGold));

            m_LoggingService?.Log($"玩家消费金钱: {amount}, 剩余: {m_Gold}", LogLevel.Debug, "PlayerData");
            return true;
        }

        /// <summary>
        /// 检查是否有足够金钱
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return m_Gold >= amount;
        }

        /// <summary>
        /// 设置金钱（用于存档加载）
        /// </summary>
        public void SetGold(int gold)
        {
            int oldGold = m_Gold;
            m_Gold = Mathf.Max(0, gold);

            if (oldGold != m_Gold)
            {
                OnGoldChanged?.Invoke(m_Gold);
                m_LoggingService?.Log($"设置玩家金钱: {m_Gold}", LogLevel.Debug, "PlayerData");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取起始金钱数量
        /// </summary>
        private int GetStartingGold()
        {
            // 可以从配置中获取，或者使用默认值
            return 1000;
        }

        /// <summary>
        /// 触发所有变化事件
        /// </summary>
        private void TriggerAllChangeEvents()
        {
            OnLevelChanged?.Invoke(Level);
            OnExperienceChanged?.Invoke(Experience);
            OnGoldChanged?.Invoke(m_Gold);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
            OnStatsChanged?.Invoke(m_PlayerStats);
        }

        /// <summary>
        /// 获取玩家数据快照
        /// </summary>
        public PlayerDataSnapshot GetDataSnapshot()
        {
            return new PlayerDataSnapshot
            {
                PlayerName = m_PlayerName,
                Stats = m_PlayerStats?.Clone(),
                Gold = m_Gold
            };
        }

        /// <summary>
        /// 应用玩家数据快照
        /// </summary>
        public void ApplyDataSnapshot(PlayerDataSnapshot snapshot)
        {
            if (snapshot != null)
            {
                SetPlayerData(snapshot.PlayerName, snapshot.Stats, snapshot.Gold);
            }
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("添加1000金钱")]
        private void TestAddGold()
        {
            AddGold(1000);
        }

        [ContextMenu("获得100经验")]
        private void TestGainExperience()
        {
            GainExperience(100);
        }

        [ContextMenu("完全治愈")]
        private void TestFullHeal()
        {
            FullHeal();
        }

        [ContextMenu("打印玩家数据")]
        private void PrintPlayerData()
        {
            Debug.Log($"=== 玩家数据 ===");
            Debug.Log($"姓名: {m_PlayerName}");
            Debug.Log($"等级: {Level}");
            Debug.Log($"经验: {Experience}");
            Debug.Log($"生命值: {CurrentHealth}/{MaxHealth}");
            Debug.Log($"法力值: {CurrentMana}/{MaxMana}");
            Debug.Log($"金钱: {m_Gold}");
            
            if (m_PlayerStats != null)
            {
                Debug.Log($"攻击: {m_PlayerStats.Attack}");
                Debug.Log($"防御: {m_PlayerStats.Defense}");
                Debug.Log($"速度: {m_PlayerStats.Speed}");
                Debug.Log($"运气: {m_PlayerStats.Luck}");
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// 玩家数据快照
    /// </summary>
    [System.Serializable]
    public class PlayerDataSnapshot
    {
        public string PlayerName;
        public CharacterStats Stats;
        public int Gold;
    }
}