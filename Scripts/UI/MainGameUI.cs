using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XianXiaGame
{
    /// <summary>
    /// 主游戏UI控制器
    /// </summary>
    public class MainGameUI : MonoBehaviour
    {
        #region UI组件引用
        [Header("玩家信息面板")]
        [SerializeField] private TextMeshProUGUI m_PlayerNameText;
        [SerializeField] private TextMeshProUGUI m_PlayerLevelText;
        [SerializeField] private TextMeshProUGUI m_PlayerExpText;
        [SerializeField] private Slider m_HealthSlider;
        [SerializeField] private TextMeshProUGUI m_HealthText;
        [SerializeField] private Slider m_ManaSlider;
        [SerializeField] private TextMeshProUGUI m_ManaText;
        [SerializeField] private TextMeshProUGUI m_AttackText;
        [SerializeField] private TextMeshProUGUI m_DefenseText;
        [SerializeField] private TextMeshProUGUI m_SpeedText;
        [SerializeField] private TextMeshProUGUI m_LuckText;
        [SerializeField] private TextMeshProUGUI m_GoldText;

        [Header("统计信息面板")]
        [SerializeField] private TextMeshProUGUI m_ExplorationCountText;
        [SerializeField] private TextMeshProUGUI m_BattleWinsText;
        [SerializeField] private TextMeshProUGUI m_TreasuresFoundText;

        [Header("消息面板")]
        [SerializeField] private ScrollRect m_MessageScrollRect;
        [SerializeField] private TextMeshProUGUI m_MessageText;
        [SerializeField] private int m_MaxMessageLines = 20;

        [Header("探索面板")]
        [SerializeField] private GameObject m_ExplorePanel;
        [SerializeField] private Button m_ExploreButton;
        [SerializeField] private Button m_InventoryButton;
        [SerializeField] private Button m_SaveButton;
        [SerializeField] private Button m_LoadButton;

        [Header("战斗面板")]
        [SerializeField] private GameObject m_BattlePanel;
        [SerializeField] private TextMeshProUGUI m_EnemyNameText;
        [SerializeField] private TextMeshProUGUI m_EnemyStatsText;
        [SerializeField] private Slider m_EnemyHealthSlider;
        [SerializeField] private TextMeshProUGUI m_EnemyHealthText;
        [SerializeField] private Button m_AttackButton;
        [SerializeField] private Button m_DefendButton;
        [SerializeField] private Button m_UseItemButton;
        [SerializeField] private Button m_EscapeButton;
        [SerializeField] private TextMeshProUGUI m_TurnInfoText;

        [Header("背包面板")]
        [SerializeField] private GameObject m_InventoryPanel;
        [SerializeField] private ScrollRect m_InventoryScrollRect;
        [SerializeField] private Transform m_InventoryContent;
        [SerializeField] private GameObject m_ItemSlotPrefab;
        [SerializeField] private Button m_CloseInventoryButton;
        [SerializeField] private Button m_SortInventoryButton;
        [SerializeField] private TextMeshProUGUI m_InventoryInfoText;

        [Header("装备面板")]
        [SerializeField] private GameObject m_EquipmentPanel;
        [SerializeField] private Transform m_EquipmentSlotsParent;
        [SerializeField] private GameObject m_EquipmentSlotPrefab;
        [SerializeField] private Button m_ToggleEquipmentButton;
        #endregion

        #region 私有变量
        private GameManager m_GameManager;
        private List<string> m_MessageHistory = new List<string>();
        private List<GameObject> m_InventorySlots = new List<GameObject>();
        private Dictionary<EquipmentType, GameObject> m_EquipmentSlots = new Dictionary<EquipmentType, GameObject>();
        private bool m_IsInventoryOpen = false;
        private bool m_IsEquipmentPanelVisible = true;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            m_GameManager = GameManager.Instance;
            
            if (m_GameManager == null)
            {
                Debug.LogError("未找到GameManager！");
                return;
            }

            // 初始化面板显示状态
            ShowExplorePanel();
            
            // 初始化背包UI
            InitializeInventoryUI();
            
            // 初始化装备UI
            InitializeEquipmentUI();
            
            // 更新所有UI
            UpdateAllUI();

            AddMessage("欢迎来到仙侠探索挖宝游戏！");
        }

        /// <summary>
        /// 设置事件监听
        /// </summary>
        private void SetupEventListeners()
        {
            if (m_GameManager == null) return;

            // 游戏管理器事件
            m_GameManager.OnGameStateChanged += OnGameStateChanged;
            m_GameManager.OnGameMessage += AddMessage;
            m_GameManager.OnPlayerLevelChanged += OnPlayerLevelChanged;
            m_GameManager.OnGoldChanged += OnGoldChanged;

            // 战斗系统事件
            if (m_GameManager.BattleSystem != null)
            {
                m_GameManager.BattleSystem.OnBattleStarted += OnBattleStarted;
                m_GameManager.BattleSystem.OnBattleEnded += OnBattleEnded;
                m_GameManager.BattleSystem.OnActionExecuted += OnBattleActionExecuted;
                m_GameManager.BattleSystem.OnTurnStarted += OnBattleTurnStarted;
                m_GameManager.BattleSystem.OnDamageDealt += OnDamageDealt;
            }

            // 背包系统事件
            if (m_GameManager.InventorySystem != null)
            {
                m_GameManager.InventorySystem.OnInventoryChanged += OnInventoryChanged;
                m_GameManager.InventorySystem.OnItemAdded += OnItemAdded;
            }

            // 装备系统事件
            if (m_GameManager.EquipmentManager != null)
            {
                m_GameManager.EquipmentManager.OnEquipmentChanged += OnEquipmentChanged;
                m_GameManager.EquipmentManager.OnStatsChanged += UpdatePlayerStats;
            }

            // 按钮事件
            SetupButtonListeners();
        }

        /// <summary>
        /// 移除事件监听
        /// </summary>
        private void RemoveEventListeners()
        {
            if (m_GameManager == null) return;

            m_GameManager.OnGameStateChanged -= OnGameStateChanged;
            m_GameManager.OnGameMessage -= AddMessage;
            m_GameManager.OnPlayerLevelChanged -= OnPlayerLevelChanged;
            m_GameManager.OnGoldChanged -= OnGoldChanged;

            if (m_GameManager.BattleSystem != null)
            {
                m_GameManager.BattleSystem.OnBattleStarted -= OnBattleStarted;
                m_GameManager.BattleSystem.OnBattleEnded -= OnBattleEnded;
                m_GameManager.BattleSystem.OnActionExecuted -= OnBattleActionExecuted;
                m_GameManager.BattleSystem.OnTurnStarted -= OnBattleTurnStarted;
                m_GameManager.BattleSystem.OnDamageDealt -= OnDamageDealt;
            }

            if (m_GameManager.InventorySystem != null)
            {
                m_GameManager.InventorySystem.OnInventoryChanged -= OnInventoryChanged;
                m_GameManager.InventorySystem.OnItemAdded -= OnItemAdded;
            }

            if (m_GameManager.EquipmentManager != null)
            {
                m_GameManager.EquipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
                m_GameManager.EquipmentManager.OnStatsChanged -= UpdatePlayerStats;
            }
        }

        /// <summary>
        /// 设置按钮监听
        /// </summary>
        private void SetupButtonListeners()
        {
            // 探索面板按钮
            if (m_ExploreButton != null)
                m_ExploreButton.onClick.AddListener(() => m_GameManager.Explore());
            
            if (m_InventoryButton != null)
                m_InventoryButton.onClick.AddListener(ToggleInventory);
            
            if (m_SaveButton != null)
                m_SaveButton.onClick.AddListener(() => m_GameManager.SaveGame());
            
            if (m_LoadButton != null)
                m_LoadButton.onClick.AddListener(() => m_GameManager.LoadGame());

            // 战斗面板按钮
            if (m_AttackButton != null)
                m_AttackButton.onClick.AddListener(() => m_GameManager.BattleSystem.PlayerAttack());
            
            if (m_DefendButton != null)
                m_DefendButton.onClick.AddListener(() => m_GameManager.BattleSystem.PlayerDefend());
            
            if (m_EscapeButton != null)
                m_EscapeButton.onClick.AddListener(() => m_GameManager.BattleSystem.PlayerEscape());

            // 背包面板按钮
            if (m_CloseInventoryButton != null)
                m_CloseInventoryButton.onClick.AddListener(CloseInventory);
            
            if (m_SortInventoryButton != null)
                m_SortInventoryButton.onClick.AddListener(() => m_GameManager.InventorySystem.SortInventory());

            // 装备面板按钮
            if (m_ToggleEquipmentButton != null)
                m_ToggleEquipmentButton.onClick.AddListener(ToggleEquipmentPanel);
        }
        #endregion

        #region UI更新方法
        /// <summary>
        /// 更新所有UI
        /// </summary>
        private void UpdateAllUI()
        {
            UpdatePlayerInfo();
            UpdatePlayerStats();
            UpdateStatistics();
            UpdateInventoryInfo();
        }

        /// <summary>
        /// 更新玩家基本信息
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (m_GameManager == null) return;

            if (m_PlayerNameText != null)
                m_PlayerNameText.text = m_GameManager.PlayerName;

            if (m_PlayerLevelText != null)
                m_PlayerLevelText.text = $"等级: {m_GameManager.PlayerStats.Level}";

            if (m_PlayerExpText != null)
                m_PlayerExpText.text = $"经验: {m_GameManager.PlayerStats.Experience}/{m_GameManager.PlayerStats.ExperienceToNext}";

            if (m_GoldText != null)
                m_GoldText.text = $"灵石: {m_GameManager.Gold}";
        }

        /// <summary>
        /// 更新玩家属性
        /// </summary>
        private void UpdatePlayerStats()
        {
            CharacterStats stats = m_GameManager?.EquipmentManager?.TotalStats ?? m_GameManager?.PlayerStats;
            if (stats == null) return;

            // 生命值
            if (m_HealthSlider != null)
            {
                m_HealthSlider.maxValue = stats.MaxHealth;
                m_HealthSlider.value = stats.CurrentHealth;
            }
            if (m_HealthText != null)
                m_HealthText.text = $"{stats.CurrentHealth}/{stats.MaxHealth}";

            // 法力值
            if (m_ManaSlider != null)
            {
                m_ManaSlider.maxValue = stats.MaxMana;
                m_ManaSlider.value = stats.CurrentMana;
            }
            if (m_ManaText != null)
                m_ManaText.text = $"{stats.CurrentMana}/{stats.MaxMana}";

            // 其他属性
            if (m_AttackText != null)
                m_AttackText.text = $"攻击: {stats.Attack}";
            
            if (m_DefenseText != null)
                m_DefenseText.text = $"防御: {stats.Defense}";
            
            if (m_SpeedText != null)
                m_SpeedText.text = $"速度: {stats.Speed}";
            
            if (m_LuckText != null)
                m_LuckText.text = $"运气: {stats.Luck}";
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            if (m_GameManager == null) return;

            if (m_ExplorationCountText != null)
                m_ExplorationCountText.text = $"探索次数: {m_GameManager.ExplorationCount}";

            if (m_BattleWinsText != null)
                m_BattleWinsText.text = $"战斗胜利: {m_GameManager.BattleWins}";

            if (m_TreasuresFoundText != null)
                m_TreasuresFoundText.text = $"发现宝藏: {m_GameManager.TreasuresFound}";
        }

        /// <summary>
        /// 更新背包信息
        /// </summary>
        private void UpdateInventoryInfo()
        {
            if (m_GameManager?.InventorySystem == null) return;

            if (m_InventoryInfoText != null)
            {
                var inventory = m_GameManager.InventorySystem;
                m_InventoryInfoText.text = $"背包: {inventory.UsedSlots}/{inventory.MaxSlots}";
            }
        }
        #endregion

        #region 面板管理
        /// <summary>
        /// 显示探索面板
        /// </summary>
        private void ShowExplorePanel()
        {
            if (m_ExplorePanel != null) m_ExplorePanel.SetActive(true);
            if (m_BattlePanel != null) m_BattlePanel.SetActive(false);
        }

        /// <summary>
        /// 显示战斗面板
        /// </summary>
        private void ShowBattlePanel()
        {
            if (m_ExplorePanel != null) m_ExplorePanel.SetActive(false);
            if (m_BattlePanel != null) m_BattlePanel.SetActive(true);
        }

        /// <summary>
        /// 切换背包面板
        /// </summary>
        private void ToggleInventory()
        {
            m_IsInventoryOpen = !m_IsInventoryOpen;
            
            if (m_InventoryPanel != null)
                m_InventoryPanel.SetActive(m_IsInventoryOpen);

            if (m_IsInventoryOpen)
            {
                RefreshInventoryUI();
            }
        }

        /// <summary>
        /// 关闭背包
        /// </summary>
        private void CloseInventory()
        {
            m_IsInventoryOpen = false;
            if (m_InventoryPanel != null)
                m_InventoryPanel.SetActive(false);
        }

        /// <summary>
        /// 切换装备面板
        /// </summary>
        private void ToggleEquipmentPanel()
        {
            m_IsEquipmentPanelVisible = !m_IsEquipmentPanelVisible;
            
            if (m_EquipmentPanel != null)
                m_EquipmentPanel.SetActive(m_IsEquipmentPanelVisible);
        }
        #endregion

        #region 消息系统
        /// <summary>
        /// 添加消息
        /// </summary>
        private void AddMessage(string _message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {_message}";
            
            m_MessageHistory.Add(formattedMessage);

            // 限制消息历史长度
            if (m_MessageHistory.Count > m_MaxMessageLines)
            {
                m_MessageHistory.RemoveAt(0);
            }

            // 更新显示
            if (m_MessageText != null)
            {
                m_MessageText.text = string.Join("\n", m_MessageHistory);
            }

            // 滚动到底部
            if (m_MessageScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                m_MessageScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        #endregion

        #region 背包UI
        /// <summary>
        /// 初始化背包UI
        /// </summary>
        private void InitializeInventoryUI()
        {
            if (m_InventoryPanel != null)
                m_InventoryPanel.SetActive(false);
        }

        /// <summary>
        /// 刷新背包UI
        /// </summary>
        private void RefreshInventoryUI()
        {
            if (m_GameManager?.InventorySystem == null || m_InventoryContent == null) return;

            // 清除现有的槽位UI
            foreach (GameObject slot in m_InventorySlots)
            {
                if (slot != null) Destroy(slot);
            }
            m_InventorySlots.Clear();

            // 创建新的槽位UI
            var inventory = m_GameManager.InventorySystem;
            for (int i = 0; i < inventory.ItemSlots.Count; i++)
            {
                CreateInventorySlotUI(i, inventory.ItemSlots[i]);
            }

            UpdateInventoryInfo();
        }

        /// <summary>
        /// 创建背包槽位UI
        /// </summary>
        private void CreateInventorySlotUI(int _index, ItemSlot _slot)
        {
            if (m_ItemSlotPrefab == null || m_InventoryContent == null) return;

            GameObject slotGO = Instantiate(m_ItemSlotPrefab, m_InventoryContent);
            m_InventorySlots.Add(slotGO);

            // 设置槽位显示
            TextMeshProUGUI slotText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
            Button slotButton = slotGO.GetComponent<Button>();

            if (!_slot.IsEmpty)
            {
                string displayText = $"{_slot.ItemData.FullName}";
                if (_slot.Quantity > 1)
                {
                    displayText += $" x{_slot.Quantity}";
                }
                
                if (slotText != null)
                {
                    slotText.text = displayText;
                    slotText.color = _slot.ItemData.RarityColor;
                }

                if (slotButton != null)
                {
                    slotButton.onClick.AddListener(() => OnInventorySlotClicked(_index));
                }
            }
            else
            {
                if (slotText != null)
                    slotText.text = "空";
            }
        }

        /// <summary>
        /// 背包槽位点击事件
        /// </summary>
        private void OnInventorySlotClicked(int _slotIndex)
        {
            if (m_GameManager?.InventorySystem == null) return;

            ItemSlot slot = m_GameManager.InventorySystem.GetSlot(_slotIndex);
            if (slot == null || slot.IsEmpty) return;

            // 根据物品类型执行不同操作
            if (slot.ItemData is EquipmentData)
            {
                // 装备物品
                m_GameManager.EquipmentManager?.EquipItemFromInventory(_slotIndex);
            }
            else if (slot.ItemData is ConsumableData)
            {
                // 使用消耗品
                CharacterStats currentStats = m_GameManager.EquipmentManager?.TotalStats ?? m_GameManager.PlayerStats;
                m_GameManager.InventorySystem.UseItem(_slotIndex, currentStats);
            }

            // 显示物品详细信息
            AddMessage($"查看物品：{slot.ItemData.GetDetailedInfo()}");
        }
        #endregion

        #region 装备UI
        /// <summary>
        /// 初始化装备UI
        /// </summary>
        private void InitializeEquipmentUI()
        {
            if (m_EquipmentSlotsParent == null || m_EquipmentSlotPrefab == null) return;

            // 为每种装备类型创建槽位
            foreach (EquipmentType equipType in System.Enum.GetValues(typeof(EquipmentType)))
            {
                GameObject slotGO = Instantiate(m_EquipmentSlotPrefab, m_EquipmentSlotsParent);
                m_EquipmentSlots[equipType] = slotGO;

                // 设置槽位标签
                TextMeshProUGUI slotText = slotGO.GetComponentInChildren<TextMeshProUGUI>();
                if (slotText != null)
                {
                    slotText.text = GetEquipmentTypeText(equipType) + ": 未装备";
                }

                // 设置点击事件
                Button slotButton = slotGO.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.AddListener(() => OnEquipmentSlotClicked(equipType));
                }
            }

            RefreshEquipmentUI();
        }

        /// <summary>
        /// 刷新装备UI
        /// </summary>
        private void RefreshEquipmentUI()
        {
            if (m_GameManager?.EquipmentManager == null) return;

            foreach (var kvp in m_EquipmentSlots)
            {
                EquipmentType equipType = kvp.Key;
                GameObject slotGO = kvp.Value;
                
                if (slotGO == null) continue;

                EquipmentData equippedItem = m_GameManager.EquipmentManager.GetEquippedItem(equipType);
                TextMeshProUGUI slotText = slotGO.GetComponentInChildren<TextMeshProUGUI>();

                if (slotText != null)
                {
                    if (equippedItem != null)
                    {
                        slotText.text = $"{GetEquipmentTypeText(equipType)}: {equippedItem.FullName}";
                        slotText.color = equippedItem.RarityColor;
                    }
                    else
                    {
                        slotText.text = $"{GetEquipmentTypeText(equipType)}: 未装备";
                        slotText.color = Color.white;
                    }
                }
            }
        }

        /// <summary>
        /// 装备槽位点击事件
        /// </summary>
        private void OnEquipmentSlotClicked(EquipmentType _equipType)
        {
            if (m_GameManager?.EquipmentManager == null) return;

            EquipmentData equippedItem = m_GameManager.EquipmentManager.GetEquippedItem(_equipType);
            if (equippedItem != null)
            {
                // 卸下装备
                m_GameManager.EquipmentManager.UnequipItem(_equipType);
                AddMessage($"卸下了 {equippedItem.FullName}");
            }
            else
            {
                AddMessage($"{GetEquipmentTypeText(_equipType)}槽位为空");
            }
        }

        /// <summary>
        /// 获取装备类型文本
        /// </summary>
        private string GetEquipmentTypeText(EquipmentType _equipType)
        {
            switch (_equipType)
            {
                case EquipmentType.Weapon: return "武器";
                case EquipmentType.Helmet: return "头盔";
                case EquipmentType.Armor: return "护甲";
                case EquipmentType.Boots: return "靴子";
                case EquipmentType.Ring: return "戒指";
                case EquipmentType.Necklace: return "项链";
                default: return "未知";
            }
        }
        #endregion

        #region 战斗UI
        /// <summary>
        /// 更新战斗UI
        /// </summary>
        private void UpdateBattleUI()
        {
            if (m_GameManager?.BattleSystem == null) return;

            var enemy = m_GameManager.BattleSystem.Enemy;
            if (enemy != null)
            {
                if (m_EnemyNameText != null)
                    m_EnemyNameText.text = enemy.Name;

                if (m_EnemyStatsText != null)
                    m_EnemyStatsText.text = $"等级: {enemy.Stats.Level}\n攻击: {enemy.Stats.Attack}\n防御: {enemy.Stats.Defense}";

                if (m_EnemyHealthSlider != null)
                {
                    m_EnemyHealthSlider.maxValue = enemy.Stats.MaxHealth;
                    m_EnemyHealthSlider.value = enemy.Stats.CurrentHealth;
                }

                if (m_EnemyHealthText != null)
                    m_EnemyHealthText.text = $"{enemy.Stats.CurrentHealth}/{enemy.Stats.MaxHealth}";
            }

            var currentActor = m_GameManager.BattleSystem.CurrentTurnActor;
            if (m_TurnInfoText != null && currentActor != null)
            {
                m_TurnInfoText.text = $"第 {m_GameManager.BattleSystem.TurnCount} 回合 - {currentActor.Name} 的回合";
            }

            // 设置按钮交互状态
            bool canPlayerAct = currentActor != null && currentActor.IsPlayer;
            SetBattleButtonsInteractable(canPlayerAct);
        }

        /// <summary>
        /// 设置战斗按钮交互状态
        /// </summary>
        private void SetBattleButtonsInteractable(bool _interactable)
        {
            if (m_AttackButton != null) m_AttackButton.interactable = _interactable;
            if (m_DefendButton != null) m_DefendButton.interactable = _interactable;
            if (m_UseItemButton != null) m_UseItemButton.interactable = _interactable;
            if (m_EscapeButton != null) m_EscapeButton.interactable = _interactable;
        }
        #endregion

        #region 事件处理方法
        /// <summary>
        /// 游戏状态改变事件
        /// </summary>
        private void OnGameStateChanged(GameState _newState)
        {
            switch (_newState)
            {
                case GameState.Exploring:
                    ShowExplorePanel();
                    break;

                case GameState.Battle:
                    ShowBattlePanel();
                    UpdateBattleUI();
                    break;
            }
        }

        /// <summary>
        /// 战斗开始事件
        /// </summary>
        private void OnBattleStarted(BattleParticipant _player, BattleParticipant _enemy)
        {
            UpdateBattleUI();
        }

        /// <summary>
        /// 战斗结束事件
        /// </summary>
        private void OnBattleEnded(BattleResult _result, BattleParticipant _winner)
        {
            UpdateAllUI();
        }

        /// <summary>
        /// 战斗动作执行事件
        /// </summary>
        private void OnBattleActionExecuted(BattleAction _action, string _description)
        {
            AddMessage(_description);
            UpdateBattleUI();
        }

        /// <summary>
        /// 战斗回合开始事件
        /// </summary>
        private void OnBattleTurnStarted(BattleParticipant _currentActor)
        {
            UpdateBattleUI();
        }

        /// <summary>
        /// 伤害造成事件
        /// </summary>
        private void OnDamageDealt(BattleParticipant _target, int _damage, bool _isCritical)
        {
            UpdateBattleUI();
            UpdatePlayerStats(); // 更新玩家生命值显示
        }

        /// <summary>
        /// 玩家等级改变事件
        /// </summary>
        private void OnPlayerLevelChanged(int _newLevel)
        {
            UpdatePlayerInfo();
            UpdatePlayerStats();
            AddMessage($"恭喜！等级提升到 {_newLevel} 级！");
        }

        /// <summary>
        /// 金钱改变事件
        /// </summary>
        private void OnGoldChanged(int _newGold)
        {
            if (m_GoldText != null)
                m_GoldText.text = $"灵石: {_newGold}";
        }

        /// <summary>
        /// 背包改变事件
        /// </summary>
        private void OnInventoryChanged(int _slotIndex)
        {
            UpdateInventoryInfo();
            
            if (m_IsInventoryOpen)
            {
                RefreshInventoryUI();
            }
        }

        /// <summary>
        /// 物品添加事件
        /// </summary>
        private void OnItemAdded(ItemData _item, int _quantity)
        {
            string quantityText = _quantity > 1 ? $" x{_quantity}" : "";
            AddMessage($"获得物品: {_item.FullName}{quantityText}");
        }

        /// <summary>
        /// 装备改变事件
        /// </summary>
        private void OnEquipmentChanged(EquipmentType _equipType, EquipmentData _equipment)
        {
            RefreshEquipmentUI();
            UpdatePlayerStats();
        }
        #endregion
    }
}