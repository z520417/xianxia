using System;
using System.Collections;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        MainMenu,       // 主菜单
        Exploring,      // 探索中
        Battle,         // 战斗中
        Inventory,      // 背包界面
        Paused          // 暂停
    }

    /// <summary>
    /// 游戏主管理器
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region 单例模式
        private static GameManager s_Instance;
        public static GameManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<GameManager>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        s_Instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }
        #endregion

        #region 事件
        public event Action<GameState> OnGameStateChanged;
        public event Action<string> OnGameMessage;
        public event Action<int> OnPlayerLevelChanged;
        public event Action<int> OnGoldChanged;
        #endregion

        #region 游戏状态
        [Header("游戏状态")]
        [SerializeField] private GameState m_CurrentState = GameState.MainMenu;
        [SerializeField] private bool m_IsGameInitialized = false;
        #endregion

        #region 玩家数据
        [Header("玩家数据")]
        [SerializeField] private string m_PlayerName = "无名道友";
        [SerializeField] private CharacterStats m_PlayerStats;
        [SerializeField] private int m_Gold = 1000;                    // 灵石数量
        [SerializeField] private int m_ExplorationCount = 0;           // 探索次数
        [SerializeField] private int m_BattleWins = 0;                 // 战斗胜利次数
        [SerializeField] private int m_TreasuresFound = 0;             // 发现宝藏次数
        #endregion

        #region 游戏系统引用
        [Header("游戏系统")]
        [SerializeField] private InventorySystem m_InventorySystem;
        [SerializeField] private EquipmentManager m_EquipmentManager;
        [SerializeField] private BattleSystem m_BattleSystem;
        [SerializeField] private RandomItemGenerator m_ItemGenerator;
        #endregion

        #region 探索配置
        [Header("探索配置")]
        [SerializeField] private float[] m_ExploreEventChances = 
        {
            30f,    // 发现宝藏
            40f,    // 遭遇战斗
            20f,    // 发现物品
            10f     // 什么都没有
        };

        [SerializeField] private string[] m_ExploreMessages = 
        {
            "你在一片废墟中挖掘着...",
            "你沿着山间小径前行...",
            "你在密林深处搜寻着...",
            "你来到了一处神秘的洞穴...",
            "你发现了一个古老的祭坛...",
            "你在湖边发现了可疑的痕迹..."
        };

        [SerializeField] private string[] m_TreasureMessages = 
        {
            "你发现了一个古老的宝箱！",
            "在岩石缝隙中，你找到了宝藏！",
            "一个闪闪发光的物体吸引了你的注意！",
            "你意外挖到了埋藏的宝物！",
            "洞穴深处传来了宝光！"
        };

        [SerializeField] private string[] m_BattleMessages = 
        {
            "一个敌人突然出现了！",
            "你遭到了袭击！",
            "危险的气息逼近...",
            "敌人发现了你！",
            "战斗不可避免！"
        };
        #endregion

        #region 公共属性
        public GameState CurrentState => m_CurrentState;
        public string PlayerName => m_PlayerName;
        public CharacterStats PlayerStats => m_PlayerStats;
        public int Gold => m_Gold;
        public int ExplorationCount => m_ExplorationCount;
        public int BattleWins => m_BattleWins;
        public int TreasuresFound => m_TreasuresFound;
        
        public InventorySystem InventorySystem => m_InventorySystem;
        public EquipmentManager EquipmentManager => m_EquipmentManager;
        public BattleSystem BattleSystem => m_BattleSystem;
        #endregion

        #region Unity生命周期
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
                return;
            }

            InitializeGame();
        }

        private void Start()
        {
            if (!m_IsGameInitialized)
            {
                StartNewGame();
            }
        }
        #endregion

        #region 游戏初始化
        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitializeGame()
        {
            // 查找或创建游戏系统
            FindOrCreateGameSystems();

            // 订阅事件
            SubscribeToEvents();

            Debug.Log("游戏系统初始化完成");
        }

        /// <summary>
        /// 查找或创建游戏系统
        /// </summary>
        private void FindOrCreateGameSystems()
        {
            // 背包系统
            if (m_InventorySystem == null)
            {
                m_InventorySystem = FindObjectOfType<InventorySystem>();
                if (m_InventorySystem == null)
                {
                    GameObject inventoryGO = new GameObject("InventorySystem");
                    inventoryGO.transform.SetParent(transform);
                    m_InventorySystem = inventoryGO.AddComponent<InventorySystem>();
                }
            }

            // 装备管理器
            if (m_EquipmentManager == null)
            {
                m_EquipmentManager = FindObjectOfType<EquipmentManager>();
                if (m_EquipmentManager == null)
                {
                    GameObject equipmentGO = new GameObject("EquipmentManager");
                    equipmentGO.transform.SetParent(transform);
                    m_EquipmentManager = equipmentGO.AddComponent<EquipmentManager>();
                }
            }

            // 战斗系统
            if (m_BattleSystem == null)
            {
                m_BattleSystem = FindObjectOfType<BattleSystem>();
                if (m_BattleSystem == null)
                {
                    GameObject battleGO = new GameObject("BattleSystem");
                    battleGO.transform.SetParent(transform);
                    m_BattleSystem = battleGO.AddComponent<BattleSystem>();
                }
            }

            // 随机物品生成器
            if (m_ItemGenerator == null)
            {
                m_ItemGenerator = RandomItemGenerator.Instance;
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            // 战斗系统事件
            if (m_BattleSystem != null)
            {
                m_BattleSystem.OnBattleStarted += OnBattleStarted;
                m_BattleSystem.OnBattleEnded += OnBattleEnded;
            }

            // 装备管理器事件
            if (m_EquipmentManager != null)
            {
                m_EquipmentManager.OnStatsChanged += OnPlayerStatsChanged;
            }
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            // 初始化玩家数据
            m_PlayerStats = new CharacterStats(1);
            m_Gold = 1000;
            m_ExplorationCount = 0;
            m_BattleWins = 0;
            m_TreasuresFound = 0;

            // 给玩家一些初始物品
            GiveStartingItems();

            // 设置游戏状态
            ChangeGameState(GameState.Exploring);

            m_IsGameInitialized = true;

            SendGameMessage("欢迎来到仙侠世界！开始你的修仙之路吧！");
            Debug.Log("新游戏开始");
        }

        /// <summary>
        /// 给予初始物品
        /// </summary>
        private void GiveStartingItems()
        {
            if (m_InventorySystem == null || m_ItemGenerator == null) return;

            // 初始装备
            EquipmentData startingWeapon = EquipmentData.GenerateRandomEquipment(EquipmentType.Weapon, ItemRarity.Common, 1);
            EquipmentData startingArmor = EquipmentData.GenerateRandomEquipment(EquipmentType.Armor, ItemRarity.Common, 1);
            
            m_InventorySystem.AddItem(startingWeapon);
            m_InventorySystem.AddItem(startingArmor);

            // 初始消耗品
            for (int i = 0; i < 3; i++)
            {
                ConsumableData startingPotion = ConsumableData.GenerateRandomConsumable(ItemRarity.Common, 1);
                m_InventorySystem.AddItem(startingPotion);
            }

            SendGameMessage("获得了一些初始装备和物品！");
        }
        #endregion

        #region 游戏状态管理
        /// <summary>
        /// 改变游戏状态
        /// </summary>
        public void ChangeGameState(GameState _newState)
        {
            if (m_CurrentState == _newState) return;

            GameState previousState = m_CurrentState;
            m_CurrentState = _newState;

            OnGameStateChanged?.Invoke(m_CurrentState);

            Debug.Log($"游戏状态从 {previousState} 改变为 {m_CurrentState}");
        }
        #endregion

        #region 探索系统
        /// <summary>
        /// 探索
        /// </summary>
        public void Explore()
        {
            if (m_CurrentState != GameState.Exploring) return;

            m_ExplorationCount++;

            // 随机选择探索消息
            string exploreMessage = m_ExploreMessages[UnityEngine.Random.Range(0, m_ExploreMessages.Length)];
            SendGameMessage(exploreMessage);

            // 延迟执行探索结果
            StartCoroutine(ProcessExploreResult());
        }

        /// <summary>
        /// 处理探索结果
        /// </summary>
        private IEnumerator ProcessExploreResult()
        {
            yield return new WaitForSeconds(1f);

            // 根据权重随机选择事件
            int eventType = GetWeightedRandomIndex(m_ExploreEventChances);

            switch (eventType)
            {
                case 0: // 发现宝藏
                    HandleTreasureFound();
                    break;

                case 1: // 遭遇战斗
                    HandleBattleEncounter();
                    break;

                case 2: // 发现物品
                    HandleItemFound();
                    break;

                case 3: // 什么都没有
                    HandleNothingFound();
                    break;
            }
        }

        /// <summary>
        /// 处理发现宝藏
        /// </summary>
        private void HandleTreasureFound()
        {
            m_TreasuresFound++;

            string treasureMessage = m_TreasureMessages[UnityEngine.Random.Range(0, m_TreasureMessages.Length)];
            SendGameMessage(treasureMessage);

            if (m_ItemGenerator != null && m_InventorySystem != null)
            {
                // 挖宝获得多个物品
                var treasures = m_ItemGenerator.DigTreasure(m_PlayerStats.Level, m_PlayerStats.Luck);
                
                foreach (var treasure in treasures)
                {
                    m_InventorySystem.AddItem(treasure);
                }

                // 获得一些灵石
                int goldReward = UnityEngine.Random.Range(50, 200) * m_PlayerStats.Level;
                AddGold(goldReward);

                SendGameMessage($"获得了 {treasures.Count} 个宝物和 {goldReward} 灵石！");
            }
        }

        /// <summary>
        /// 处理战斗遭遇
        /// </summary>
        private void HandleBattleEncounter()
        {
            string battleMessage = m_BattleMessages[UnityEngine.Random.Range(0, m_BattleMessages.Length)];
            SendGameMessage(battleMessage);

            if (m_BattleSystem != null)
            {
                // 敌人等级在玩家等级±2范围内
                int enemyLevel = m_PlayerStats.Level + UnityEngine.Random.Range(-2, 3);
                enemyLevel = Mathf.Max(1, enemyLevel);

                // 获取当前总属性（包括装备加成）
                CharacterStats currentStats = m_EquipmentManager != null ? m_EquipmentManager.TotalStats : m_PlayerStats;
                
                m_BattleSystem.StartBattle(currentStats, enemyLevel);
            }
        }

        /// <summary>
        /// 处理发现物品
        /// </summary>
        private void HandleItemFound()
        {
            SendGameMessage("你发现了一些有用的物品！");

            if (m_ItemGenerator != null && m_InventorySystem != null)
            {
                ItemData foundItem = m_ItemGenerator.GenerateRandomItem(m_PlayerStats.Level, m_PlayerStats.Luck);
                m_InventorySystem.AddItem(foundItem);

                SendGameMessage($"获得了 {foundItem.FullName}！");
            }
        }

        /// <summary>
        /// 处理什么都没发现
        /// </summary>
        private void HandleNothingFound()
        {
            string[] nothingMessages = 
            {
                "这里似乎什么都没有...",
                "一无所获，继续探索吧。",
                "空手而归，不过经验还是有所增长。",
                "虽然没有收获，但你对这片区域更加熟悉了。"
            };

            string message = nothingMessages[UnityEngine.Random.Range(0, nothingMessages.Length)];
            SendGameMessage(message);

            // 给予少量经验
            m_PlayerStats.GainExperience(5);
        }

        /// <summary>
        /// 根据权重获取随机索引
        /// </summary>
        private int GetWeightedRandomIndex(float[] _weights)
        {
            float totalWeight = 0f;
            foreach (float weight in _weights)
            {
                totalWeight += weight;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < _weights.Length; i++)
            {
                currentWeight += _weights[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }

            return _weights.Length - 1;
        }
        #endregion

        #region 金钱管理
        /// <summary>
        /// 添加金钱
        /// </summary>
        public void AddGold(int _amount)
        {
            m_Gold += _amount;
            OnGoldChanged?.Invoke(m_Gold);
        }

        /// <summary>
        /// 扣除金钱
        /// </summary>
        public bool SpendGold(int _amount)
        {
            if (m_Gold >= _amount)
            {
                m_Gold -= _amount;
                OnGoldChanged?.Invoke(m_Gold);
                return true;
            }
            return false;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 战斗开始事件处理
        /// </summary>
        private void OnBattleStarted(BattleParticipant _player, BattleParticipant _enemy)
        {
            ChangeGameState(GameState.Battle);
            SendGameMessage($"与 {_enemy.Name} 的战斗开始了！");
        }

        /// <summary>
        /// 战斗结束事件处理
        /// </summary>
        private void OnBattleEnded(BattleResult _result, BattleParticipant _winner)
        {
            switch (_result)
            {
                case BattleResult.Victory:
                    m_BattleWins++;
                    SendGameMessage("战斗胜利！继续你的探索之路吧！");
                    break;

                case BattleResult.Defeat:
                    SendGameMessage("战斗失败了...休息一下再继续吧。");
                    // 失败惩罚：失去一些金钱
                    int lostGold = Mathf.Min(m_Gold / 10, 100);
                    if (lostGold > 0)
                    {
                        m_Gold -= lostGold;
                        OnGoldChanged?.Invoke(m_Gold);
                        SendGameMessage($"失去了 {lostGold} 灵石...");
                    }
                    break;

                case BattleResult.Escape:
                    SendGameMessage("成功逃脱了战斗！");
                    break;
            }

            ChangeGameState(GameState.Exploring);
        }

        /// <summary>
        /// 玩家属性改变事件处理
        /// </summary>
        private void OnPlayerStatsChanged()
        {
            if (m_EquipmentManager != null)
            {
                int newLevel = m_EquipmentManager.TotalStats.Level;
                if (newLevel != m_PlayerStats.Level)
                {
                    OnPlayerLevelChanged?.Invoke(newLevel);
                }
                
                // 更新玩家基础属性
                m_PlayerStats = m_EquipmentManager.TotalStats.Clone();
            }
        }
        #endregion

        #region 消息系统
        /// <summary>
        /// 发送游戏消息
        /// </summary>
        public void SendGameMessage(string _message)
        {
            OnGameMessage?.Invoke(_message);
            Debug.Log($"[游戏消息] {_message}");
        }
        #endregion

        #region 保存和加载
        /// <summary>
        /// 保存游戏数据
        /// </summary>
        public void SaveGame()
        {
            // 这里应该实现游戏数据的序列化保存
            // 简化版本，仅记录到PlayerPrefs
            PlayerPrefs.SetString("PlayerName", m_PlayerName);
            PlayerPrefs.SetInt("PlayerLevel", m_PlayerStats.Level);
            PlayerPrefs.SetInt("PlayerExp", m_PlayerStats.Experience);
            PlayerPrefs.SetInt("Gold", m_Gold);
            PlayerPrefs.SetInt("ExplorationCount", m_ExplorationCount);
            PlayerPrefs.SetInt("BattleWins", m_BattleWins);
            PlayerPrefs.SetInt("TreasuresFound", m_TreasuresFound);
            
            PlayerPrefs.Save();
            SendGameMessage("游戏已保存！");
        }

        /// <summary>
        /// 加载游戏数据
        /// </summary>
        public void LoadGame()
        {
            if (PlayerPrefs.HasKey("PlayerLevel"))
            {
                m_PlayerName = PlayerPrefs.GetString("PlayerName", "无名道友");
                int level = PlayerPrefs.GetInt("PlayerLevel", 1);
                int exp = PlayerPrefs.GetInt("PlayerExp", 0);
                
                m_PlayerStats = new CharacterStats(level);
                m_PlayerStats.m_Experience = exp;
                
                m_Gold = PlayerPrefs.GetInt("Gold", 1000);
                m_ExplorationCount = PlayerPrefs.GetInt("ExplorationCount", 0);
                m_BattleWins = PlayerPrefs.GetInt("BattleWins", 0);
                m_TreasuresFound = PlayerPrefs.GetInt("TreasuresFound", 0);
                
                m_IsGameInitialized = true;
                ChangeGameState(GameState.Exploring);
                
                SendGameMessage("游戏数据加载完成！");
            }
            else
            {
                SendGameMessage("没有找到存档数据。");
            }
        }
        #endregion

        #region 调试方法
#if UNITY_EDITOR
        [ContextMenu("开始探索")]
        private void TestExplore()
        {
            Explore();
        }

        [ContextMenu("添加1000灵石")]
        private void TestAddGold()
        {
            AddGold(1000);
        }

        [ContextMenu("提升等级")]
        private void TestLevelUp()
        {
            m_PlayerStats.GainExperience(m_PlayerStats.ExperienceToNext);
        }

        [ContextMenu("打印玩家信息")]
        private void PrintPlayerInfo()
        {
            Debug.Log($"玩家：{m_PlayerName}");
            Debug.Log($"等级：{m_PlayerStats.Level}");
            Debug.Log($"经验：{m_PlayerStats.Experience}/{m_PlayerStats.ExperienceToNext}");
            Debug.Log($"生命值：{m_PlayerStats.CurrentHealth}/{m_PlayerStats.MaxHealth}");
            Debug.Log($"攻击力：{m_PlayerStats.Attack}");
            Debug.Log($"防御力：{m_PlayerStats.Defense}");
            Debug.Log($"灵石：{m_Gold}");
            Debug.Log($"探索次数：{m_ExplorationCount}");
            Debug.Log($"战斗胜利：{m_BattleWins}");
            Debug.Log($"发现宝藏：{m_TreasuresFound}");
        }
#endif
        #endregion
    }
}