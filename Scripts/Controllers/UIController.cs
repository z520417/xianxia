using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XianXiaGame.Core;
using System.Collections.Generic;
using System.Collections;

namespace XianXiaGame.Controllers
{
    /// <summary>
    /// UI控制器
    /// 专门负责UI管理和更新，高性能，低耦合
    /// </summary>
    public class UIController : RegisteredComponent
    {
        #region UI组件引用

        [Header("玩家信息面板")]
        [SerializeField] private GameObject m_PlayerInfoPanel;
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
        [SerializeField] private GameObject m_StatisticsPanel;
        [SerializeField] private TextMeshProUGUI m_ExplorationCountText;
        [SerializeField] private TextMeshProUGUI m_BattleWinsText;
        [SerializeField] private TextMeshProUGUI m_TreasuresFoundText;

        [Header("消息面板")]
        [SerializeField] private GameObject m_MessagePanel;
        [SerializeField] private ScrollRect m_MessageScrollRect;
        [SerializeField] private TextMeshProUGUI m_MessageText;
        [SerializeField] private Transform m_MessageContainer;

        [Header("控制面板")]
        [SerializeField] private GameObject m_ControlPanel;
        [SerializeField] private Button m_ExploreButton;
        [SerializeField] private Button m_InventoryButton;
        [SerializeField] private Button m_SaveButton;
        [SerializeField] private Button m_LoadButton;
        [SerializeField] private Button m_SettingsButton;

        [Header("战斗面板")]
        [SerializeField] private GameObject m_BattlePanel;
        [SerializeField] private TextMeshProUGUI m_EnemyNameText;
        [SerializeField] private Slider m_EnemyHealthSlider;
        [SerializeField] private TextMeshProUGUI m_EnemyHealthText;
        [SerializeField] private Button m_AttackButton;
        [SerializeField] private Button m_DefendButton;
        [SerializeField] private Button m_EscapeButton;

        [Header("背包面板")]
        [SerializeField] private GameObject m_InventoryPanel;
        [SerializeField] private Transform m_InventoryContainer;
        [SerializeField] private GameObject m_ItemSlotPrefab;

        #endregion

        #region 配置设置

        [Header("UI配置")]
        [SerializeField] private bool m_UseOptimizedUpdates = true;
        [SerializeField] private bool m_EnableUIAnimations = true;
        [SerializeField] private bool m_EnableUIEffects = true;
        [SerializeField] private int m_MaxMessageLines = 50;

        [Header("动画配置")]
        [SerializeField] private float m_FadeSpeed = 2f;
        [SerializeField] private float m_SlideSpeed = 3f;
        [SerializeField] private float m_ButtonScaleEffect = 1.1f;

        [Header("调试设置")]
        [SerializeField] private bool m_EnableDebugMode = false;
        [SerializeField] private bool m_ShowUpdateFrequency = false;

        #endregion

        #region 服务引用

        private IGameManager m_GameManager;
        private IEventService m_EventService;
        private IUIUpdateService m_UIUpdateService;
        private IUIObjectPoolService m_ObjectPoolService;
        private IConfigService m_ConfigService;

        #endregion

        #region UI状态管理

        private Dictionary<UIUpdateType, float> m_LastUpdateTimes = new Dictionary<UIUpdateType, float>();
        private Queue<UIMessage> m_MessageQueue = new Queue<UIMessage>();
        private List<GameObject> m_ActiveItemSlots = new List<GameObject>();
        private Dictionary<string, Component> m_CachedComponents = new Dictionary<string, Component>();
        
        private GameState m_CurrentGameState = GameState.MainMenu;
        private bool m_IsInitialized = false;
        private UIAnimationManager m_AnimationManager;

        #endregion

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeUIController();
        }

        private void Start()
        {
            SetupUI();
            RegisterUIUpdateHandlers();
        }

        private void Update()
        {
            if (m_IsInitialized)
            {
                ProcessMessageQueue();
                UpdateDebugInfo();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupUIController();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化UI控制器
        /// </summary>
        private void InitializeUIController()
        {
            try
            {
                // 获取服务引用
                GetServiceReferences();

                // 创建动画管理器
                m_AnimationManager = new UIAnimationManager(this, m_FadeSpeed, m_SlideSpeed);

                // 缓存组件
                CacheUIComponents();

                m_IsInitialized = true;
                GameLog.Debug("UI控制器初始化完成", "UIController");
            }
            catch (System.Exception e)
            {
                GameLog.Error($"UI控制器初始化失败: {e.Message}", "UIController");
            }
        }

        /// <summary>
        /// 获取服务引用
        /// </summary>
        private void GetServiceReferences()
        {
            m_GameManager = OptimizedGameManager.Instance;
            m_EventService = GameServiceBootstrapper.GetService<IEventService>();
            m_UIUpdateService = GameServiceBootstrapper.GetService<IUIUpdateService>();
            m_ObjectPoolService = GameServiceBootstrapper.GetService<IUIObjectPoolService>();
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();
        }

        /// <summary>
        /// 缓存UI组件
        /// </summary>
        private void CacheUIComponents()
        {
            // 缓存常用组件以提高性能
            CacheComponent("PlayerName", m_PlayerNameText);
            CacheComponent("PlayerLevel", m_PlayerLevelText);
            CacheComponent("PlayerExp", m_PlayerExpText);
            CacheComponent("HealthSlider", m_HealthSlider);
            CacheComponent("ManaSlider", m_ManaSlider);
            CacheComponent("Gold", m_GoldText);
        }

        /// <summary>
        /// 缓存组件
        /// </summary>
        private void CacheComponent(string key, Component component)
        {
            if (component != null)
            {
                m_CachedComponents[key] = component;
            }
        }

        #endregion

        #region UI设置

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            SetupButtons();
            SetupPanels();
            SubscribeToEvents();
            InitialUIUpdate();
        }

        /// <summary>
        /// 设置按钮
        /// </summary>
        private void SetupButtons()
        {
            SetupButton(m_ExploreButton, OnExploreClicked, "开始探索");
            SetupButton(m_InventoryButton, OnInventoryClicked, "背包");
            SetupButton(m_SaveButton, OnSaveClicked, "保存游戏");
            SetupButton(m_LoadButton, OnLoadClicked, "加载游戏");
            SetupButton(m_SettingsButton, OnSettingsClicked, "设置");

            // 战斗按钮
            SetupButton(m_AttackButton, OnAttackClicked, "攻击");
            SetupButton(m_DefendButton, OnDefendClicked, "防御");
            SetupButton(m_EscapeButton, OnEscapeClicked, "逃跑");
        }

        /// <summary>
        /// 设置单个按钮
        /// </summary>
        private void SetupButton(Button button, System.Action action, string tooltip = "")
        {
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(action, button, tooltip));

            // 添加按钮效果
            if (m_EnableUIEffects)
            {
                AddButtonEffects(button);
            }
        }

        /// <summary>
        /// 添加按钮效果
        /// </summary>
        private void AddButtonEffects(Button button)
        {
            var buttonAnimator = button.gameObject.GetComponent<ButtonAnimator>();
            if (buttonAnimator == null)
            {
                buttonAnimator = button.gameObject.AddComponent<ButtonAnimator>();
            }
            buttonAnimator.Initialize(m_ButtonScaleEffect);
        }

        /// <summary>
        /// 设置面板
        /// </summary>
        private void SetupPanels()
        {
            // 设置面板初始状态
            SetPanelActive(m_PlayerInfoPanel, true);
            SetPanelActive(m_StatisticsPanel, true);
            SetPanelActive(m_MessagePanel, true);
            SetPanelActive(m_ControlPanel, true);
            SetPanelActive(m_BattlePanel, false);
            SetPanelActive(m_InventoryPanel, false);

            // 设置消息面板
            if (m_MessageText != null)
            {
                m_MessageText.text = "";
            }
        }

        /// <summary>
        /// 设置面板激活状态
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                if (m_EnableUIAnimations)
                {
                    m_AnimationManager?.AnimatePanel(panel, active);
                }
                else
                {
                    panel.SetActive(active);
                }
            }
        }

        #endregion

        #region UI更新系统

        /// <summary>
        /// 注册UI更新处理器
        /// </summary>
        private void RegisterUIUpdateHandlers()
        {
            if (m_UIUpdateService == null) return;

            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.PlayerInfo, UpdatePlayerInfo);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.PlayerStats, UpdatePlayerStats);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.Statistics, UpdateStatistics);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.Messages, UpdateMessages);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.Inventory, UpdateInventory);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.Equipment, UpdateEquipment);
            m_UIUpdateService.RegisterUpdateHandler(UIUpdateType.Battle, UpdateBattle);

            GameLog.Debug("UI更新处理器注册完成", "UIController");
        }

        /// <summary>
        /// 初始UI更新
        /// </summary>
        private void InitialUIUpdate()
        {
            UpdatePlayerInfo(null);
            UpdatePlayerStats(null);
            UpdateStatistics(null);
            ShowMessage("界面已准备就绪", MessageType.Success);
        }

        /// <summary>
        /// 更新玩家信息
        /// </summary>
        private void UpdatePlayerInfo(object data)
        {
            if (m_GameManager == null) return;

            try
            {
                SetTextSafe("PlayerName", m_PlayerNameText, m_GameManager.PlayerName);
                
                var stats = m_GameManager.PlayerStats;
                if (stats != null)
                {
                    SetTextSafe("PlayerLevel", m_PlayerLevelText, $"等级: {stats.Level}");
                    SetTextSafe("PlayerExp", m_PlayerExpText, 
                        $"经验: {stats.CurrentExperience}/{stats.ExperienceToNext}");
                }

                SetTextSafe("Gold", m_GoldText, $"灵石: {m_GameManager.Gold}");

                m_LastUpdateTimes[UIUpdateType.PlayerInfo] = Time.time;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"更新玩家信息失败: {e.Message}", "UIController");
            }
        }

        /// <summary>
        /// 更新玩家属性
        /// </summary>
        private void UpdatePlayerStats(object data)
        {
            if (m_GameManager?.PlayerStats == null) return;

            try
            {
                var stats = m_GameManager.PlayerStats;

                UpdateSliderSafe(m_HealthSlider, stats.CurrentHealth, stats.MaxHealth);
                SetTextSafe(m_HealthText, $"{stats.CurrentHealth}/{stats.MaxHealth}");

                UpdateSliderSafe(m_ManaSlider, stats.CurrentMana, stats.MaxMana);
                SetTextSafe(m_ManaText, $"{stats.CurrentMana}/{stats.MaxMana}");

                SetTextSafe(m_AttackText, $"攻击: {stats.Attack}");
                SetTextSafe(m_DefenseText, $"防御: {stats.Defense}");
                SetTextSafe(m_SpeedText, $"速度: {stats.Speed}");
                SetTextSafe(m_LuckText, $"运气: {stats.Luck}");

                m_LastUpdateTimes[UIUpdateType.PlayerStats] = Time.time;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"更新玩家属性失败: {e.Message}", "UIController");
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(object data)
        {
            if (m_GameManager == null) return;

            try
            {
                SetTextSafe(m_ExplorationCountText, $"探索次数: {m_GameManager.ExplorationCount}");
                SetTextSafe(m_BattleWinsText, $"战斗胜利: {m_GameManager.BattleWins}");
                SetTextSafe(m_TreasuresFoundText, $"发现宝藏: {m_GameManager.TreasuresFound}");

                m_LastUpdateTimes[UIUpdateType.Statistics] = Time.time;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"更新统计信息失败: {e.Message}", "UIController");
            }
        }

        /// <summary>
        /// 更新消息
        /// </summary>
        private void UpdateMessages(object data)
        {
            if (data is UIMessage message)
            {
                EnqueueMessage(message);
            }
            else if (data is string text)
            {
                EnqueueMessage(new UIMessage(text, MessageType.Normal));
            }
            else if (data != null)
            {
                var messageData = data.GetType().GetProperty("Message")?.GetValue(data) as string;
                if (!string.IsNullOrEmpty(messageData))
                {
                    EnqueueMessage(new UIMessage(messageData, MessageType.Normal));
                }
            }
        }

        /// <summary>
        /// 更新背包
        /// </summary>
        private void UpdateInventory(object data)
        {
            if (m_GameManager?.InventorySystem == null) return;

            try
            {
                RefreshInventorySlots();
                m_LastUpdateTimes[UIUpdateType.Inventory] = Time.time;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"更新背包失败: {e.Message}", "UIController");
            }
        }

        /// <summary>
        /// 更新装备
        /// </summary>
        private void UpdateEquipment(object data)
        {
            // TODO: 实现装备面板更新
            m_LastUpdateTimes[UIUpdateType.Equipment] = Time.time;
        }

        /// <summary>
        /// 更新战斗
        /// </summary>
        private void UpdateBattle(object data)
        {
            if (m_GameManager?.BattleSystem == null) return;

            try
            {
                var battleSystem = m_GameManager.BattleSystem;
                bool inBattle = battleSystem.IsBattleActive;

                SetPanelActive(m_BattlePanel, inBattle);
                SetPanelActive(m_ControlPanel, !inBattle);

                if (inBattle)
                {
                    UpdateBattleInfo();
                }

                m_LastUpdateTimes[UIUpdateType.Battle] = Time.time;
            }
            catch (System.Exception e)
            {
                GameLog.Error($"更新战斗界面失败: {e.Message}", "UIController");
            }
        }

        #endregion

        #region 消息系统

        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, MessageType type = MessageType.Normal)
        {
            EnqueueMessage(new UIMessage(message, type));
        }

        /// <summary>
        /// 加入消息队列
        /// </summary>
        private void EnqueueMessage(UIMessage message)
        {
            m_MessageQueue.Enqueue(message);
        }

        /// <summary>
        /// 处理消息队列
        /// </summary>
        private void ProcessMessageQueue()
        {
            if (m_MessageQueue.Count == 0) return;

            // 每帧最多处理3条消息
            int processed = 0;
            while (m_MessageQueue.Count > 0 && processed < 3)
            {
                var message = m_MessageQueue.Dequeue();
                DisplayMessage(message);
                processed++;
            }
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void DisplayMessage(UIMessage message)
        {
            if (m_MessageText == null) return;

            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string colorTag = GetMessageColorTag(message.Type);
            string formattedMessage = $"[{timestamp}] {colorTag}{message.Text}</color>";

            // 添加到现有文本
            if (!string.IsNullOrEmpty(m_MessageText.text))
            {
                m_MessageText.text += "\n" + formattedMessage;
            }
            else
            {
                m_MessageText.text = formattedMessage;
            }

            // 限制消息行数
            LimitMessageLines();

            // 滚动到底部
            if (m_MessageScrollRect != null)
            {
                StartCoroutine(ScrollToBottom());
            }
        }

        /// <summary>
        /// 限制消息行数
        /// </summary>
        private void LimitMessageLines()
        {
            if (m_MessageText == null) return;

            string[] lines = m_MessageText.text.Split('\n');
            if (lines.Length > m_MaxMessageLines)
            {
                int excess = lines.Length - m_MaxMessageLines;
                var remainingLines = new string[m_MaxMessageLines];
                System.Array.Copy(lines, excess, remainingLines, 0, m_MaxMessageLines);
                m_MessageText.text = string.Join("\n", remainingLines);
            }
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (m_MessageScrollRect != null)
            {
                m_MessageScrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        /// <summary>
        /// 获取消息颜色标签
        /// </summary>
        private string GetMessageColorTag(MessageType type)
        {
            return type switch
            {
                MessageType.Error => "<color=red>",
                MessageType.Warning => "<color=yellow>",
                MessageType.Success => "<color=green>",
                MessageType.Important => "<color=orange>",
                MessageType.Info => "<color=cyan>",
                _ => "<color=white>"
            };
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnGameStateChanged += OnGameStateChanged;
                m_GameManager.OnGameMessage += OnGameMessage;
                m_GameManager.OnPlayerLevelChanged += OnPlayerLevelChanged;
                m_GameManager.OnGoldChanged += OnGoldChanged;
            }

            if (m_EventService != null)
            {
                m_EventService.Subscribe(GameEventType.GameMessage, OnGameMessageEvent);
                m_EventService.Subscribe(GameEventType.PlayerLevelUp, OnPlayerLevelUpEvent);
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnGameStateChanged -= OnGameStateChanged;
                m_GameManager.OnGameMessage -= OnGameMessage;
                m_GameManager.OnPlayerLevelChanged -= OnPlayerLevelChanged;
                m_GameManager.OnGoldChanged -= OnGoldChanged;
            }

            if (m_EventService != null)
            {
                m_EventService.Unsubscribe(GameEventType.GameMessage, OnGameMessageEvent);
                m_EventService.Unsubscribe(GameEventType.PlayerLevelUp, OnPlayerLevelUpEvent);
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            m_CurrentGameState = newState;
            UpdateUIForGameState(newState);
        }

        private void OnGameMessage(string message)
        {
            ShowMessage(message);
        }

        private void OnPlayerLevelChanged(int newLevel)
        {
            ShowMessage($"🎉 等级提升到 {newLevel} 级！", MessageType.Important);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerStats);
        }

        private void OnGoldChanged(int newGold)
        {
            m_UIUpdateService?.RequestUpdate(UIUpdateType.PlayerInfo);
        }

        private void OnGameMessageEvent(GameEventData eventData)
        {
            if (eventData is GameMessageEventData messageData)
            {
                ShowMessage(messageData.Message, messageData.MessageType);
            }
        }

        private void OnPlayerLevelUpEvent(GameEventData eventData)
        {
            if (eventData is PlayerLevelUpEventData levelData)
            {
                ShowMessage($"🌟 恭喜升级到 {levelData.NewLevel} 级！", MessageType.Important);
            }
        }

        #endregion

        #region 按钮点击处理

        private void OnButtonClicked(System.Action action, Button button, string tooltip)
        {
            try
            {
                // 按钮反馈
                if (m_EnableUIEffects)
                {
                    StartCoroutine(ButtonClickEffect(button));
                }

                // 执行操作
                action?.Invoke();

                if (m_EnableDebugMode && !string.IsNullOrEmpty(tooltip))
                {
                    ShowMessage($"执行操作: {tooltip}", MessageType.Info);
                }
            }
            catch (System.Exception e)
            {
                GameLog.Error($"按钮点击处理失败: {e.Message}", "UIController");
                ShowMessage("操作失败，请重试", MessageType.Error);
            }
        }

        private void OnExploreClicked()
        {
            m_GameManager?.Explore();
        }

        private void OnInventoryClicked()
        {
            ToggleInventoryPanel();
        }

        private void OnSaveClicked()
        {
            bool success = m_GameManager?.SaveGame() ?? false;
            ShowMessage(success ? "游戏保存成功！" : "游戏保存失败！", 
                       success ? MessageType.Success : MessageType.Error);
        }

        private void OnLoadClicked()
        {
            bool success = m_GameManager?.LoadGame() ?? false;
            ShowMessage(success ? "游戏加载成功！" : "游戏加载失败！", 
                       success ? MessageType.Success : MessageType.Error);
        }

        private void OnSettingsClicked()
        {
            ShowMessage("设置面板开发中...", MessageType.Info);
        }

        private void OnAttackClicked()
        {
            var battleSystem = m_GameManager?.BattleSystem;
            if (battleSystem != null && battleSystem.IsBattleActive)
            {
                battleSystem.ExecutePlayerAction(BattleAction.Attack);
            }
        }

        private void OnDefendClicked()
        {
            var battleSystem = m_GameManager?.BattleSystem;
            if (battleSystem != null && battleSystem.IsBattleActive)
            {
                battleSystem.ExecutePlayerAction(BattleAction.Defend);
            }
        }

        private void OnEscapeClicked()
        {
            var battleSystem = m_GameManager?.BattleSystem;
            if (battleSystem != null && battleSystem.IsBattleActive)
            {
                battleSystem.ExecutePlayerAction(BattleAction.Escape);
            }
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 安全设置文本
        /// </summary>
        private void SetTextSafe(TextMeshProUGUI textComponent, string text)
        {
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }

        /// <summary>
        /// 安全设置文本（使用缓存）
        /// </summary>
        private void SetTextSafe(string cacheKey, TextMeshProUGUI fallback, string text)
        {
            var component = GetCachedComponent<TextMeshProUGUI>(cacheKey) ?? fallback;
            SetTextSafe(component, text);
        }

        /// <summary>
        /// 安全更新滑动条
        /// </summary>
        private void UpdateSliderSafe(Slider slider, float current, float max)
        {
            if (slider != null && max > 0)
            {
                slider.maxValue = max;
                slider.value = current;
            }
        }

        /// <summary>
        /// 获取缓存的组件
        /// </summary>
        private T GetCachedComponent<T>(string key) where T : Component
        {
            return m_CachedComponents.TryGetValue(key, out Component component) ? 
                   component as T : null;
        }

        /// <summary>
        /// 更新游戏状态UI
        /// </summary>
        private void UpdateUIForGameState(GameState state)
        {
            switch (state)
            {
                case GameState.Exploring:
                    SetPanelActive(m_BattlePanel, false);
                    SetPanelActive(m_ControlPanel, true);
                    SetPanelActive(m_InventoryPanel, false);
                    break;

                case GameState.Battle:
                    SetPanelActive(m_BattlePanel, true);
                    SetPanelActive(m_ControlPanel, false);
                    SetPanelActive(m_InventoryPanel, false);
                    break;

                case GameState.Inventory:
                    SetPanelActive(m_InventoryPanel, true);
                    break;
            }
        }

        /// <summary>
        /// 切换背包面板
        /// </summary>
        private void ToggleInventoryPanel()
        {
            bool isActive = m_InventoryPanel != null && m_InventoryPanel.activeSelf;
            SetPanelActive(m_InventoryPanel, !isActive);
            
            if (!isActive)
            {
                UpdateInventory(null);
            }
        }

        /// <summary>
        /// 刷新背包槽位
        /// </summary>
        private void RefreshInventorySlots()
        {
            if (m_InventoryContainer == null || m_ItemSlotPrefab == null) return;

            // 清理现有槽位
            foreach (var slot in m_ActiveItemSlots)
            {
                if (slot != null)
                {
                    if (m_ObjectPoolService != null)
                    {
                        m_ObjectPoolService.ReturnToPool("ItemSlot", slot);
                    }
                    else
                    {
                        Destroy(slot);
                    }
                }
            }
            m_ActiveItemSlots.Clear();

            // 创建新槽位
            var inventory = m_GameManager?.InventorySystem;
            if (inventory != null)
            {
                for (int i = 0; i < inventory.GetMaxSlots(); i++)
                {
                    var item = inventory.GetItemAt(i);
                    var slot = CreateItemSlot(item, i);
                    if (slot != null)
                    {
                        m_ActiveItemSlots.Add(slot);
                    }
                }
            }
        }

        /// <summary>
        /// 创建物品槽位
        /// </summary>
        private GameObject CreateItemSlot(ItemData item, int index)
        {
            GameObject slot;
            
            if (m_ObjectPoolService != null)
            {
                slot = m_ObjectPoolService.GetFromPool("ItemSlot", m_ItemSlotPrefab);
            }
            else
            {
                slot = Instantiate(m_ItemSlotPrefab);
            }

            if (slot != null)
            {
                slot.transform.SetParent(m_InventoryContainer, false);
                
                var slotComponent = slot.GetComponent<ItemSlotUI>();
                if (slotComponent != null)
                {
                    slotComponent.SetItem(item);
                    slotComponent.SetIndex(index);
                }
            }

            return slot;
        }

        /// <summary>
        /// 更新战斗信息
        /// </summary>
        private void UpdateBattleInfo()
        {
            var battleSystem = m_GameManager?.BattleSystem;
            if (battleSystem == null || !battleSystem.IsBattleActive) return;

            // TODO: 获取敌人信息并更新UI
            // var enemy = battleSystem.GetCurrentEnemy();
            // SetTextSafe(m_EnemyNameText, enemy?.Name ?? "未知敌人");
            // UpdateSliderSafe(m_EnemyHealthSlider, enemy?.CurrentHealth ?? 0, enemy?.MaxHealth ?? 1);
        }

        /// <summary>
        /// 按钮点击效果
        /// </summary>
        private IEnumerator ButtonClickEffect(Button button)
        {
            if (button == null) yield break;

            var transform = button.transform;
            var originalScale = transform.localScale;
            var targetScale = originalScale * m_ButtonScaleEffect;

            // 放大
            float elapsed = 0f;
            float duration = 0.1f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // 恢复
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        /// 更新调试信息
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (!m_ShowUpdateFrequency || Time.frameCount % 60 != 0) return;

            float currentTime = Time.time;
            var frequencies = new List<string>();

            foreach (var kvp in m_LastUpdateTimes)
            {
                float timeSince = currentTime - kvp.Value;
                frequencies.Add($"{kvp.Key}: {timeSince:F1}s");
            }

            if (frequencies.Count > 0)
            {
                ShowMessage($"UI更新频率: {string.Join(", ", frequencies)}", MessageType.Info);
            }
        }

        /// <summary>
        /// 清理UI控制器
        /// </summary>
        private void CleanupUIController()
        {
            UnsubscribeFromEvents();
            m_AnimationManager?.Dispose();
            
            // 清理对象池中的物品
            foreach (var slot in m_ActiveItemSlots)
            {
                if (slot != null && m_ObjectPoolService != null)
                {
                    m_ObjectPoolService.ReturnToPool("ItemSlot", slot);
                }
            }
            m_ActiveItemSlots.Clear();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("强制更新所有UI")]
        private void ForceUpdateAllUI()
        {
            UpdatePlayerInfo(null);
            UpdatePlayerStats(null);
            UpdateStatistics(null);
            UpdateInventory(null);
            UpdateBattle(null);
            Debug.Log("所有UI已强制更新");
        }

        [ContextMenu("清空消息")]
        private void ClearMessages()
        {
            if (m_MessageText != null)
            {
                m_MessageText.text = "";
            }
            m_MessageQueue.Clear();
            Debug.Log("消息已清空");
        }

        [ContextMenu("测试消息")]
        private void TestMessages()
        {
            ShowMessage("这是一条测试消息", MessageType.Normal);
            ShowMessage("这是成功消息", MessageType.Success);
            ShowMessage("这是警告消息", MessageType.Warning);
            ShowMessage("这是错误消息", MessageType.Error);
            ShowMessage("这是重要消息", MessageType.Important);
        }
#endif
    }

    /// <summary>
    /// UI消息
    /// </summary>
    [System.Serializable]
    public class UIMessage
    {
        public string Text;
        public MessageType Type;
        public float Timestamp;

        public UIMessage(string text, MessageType type = MessageType.Normal)
        {
            Text = text;
            Type = type;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// 按钮动画组件
    /// </summary>
    public class ButtonAnimator : MonoBehaviour
    {
        private Button m_Button;
        private Vector3 m_OriginalScale;
        private float m_ScaleEffect = 1.1f;

        public void Initialize(float scaleEffect)
        {
            m_Button = GetComponent<Button>();
            m_OriginalScale = transform.localScale;
            m_ScaleEffect = scaleEffect;

            if (m_Button != null)
            {
                m_Button.onClick.AddListener(AnimateClick);
            }
        }

        private void AnimateClick()
        {
            StartCoroutine(ClickAnimation());
        }

        private System.Collections.IEnumerator ClickAnimation()
        {
            // 简单的缩放动画
            transform.localScale = m_OriginalScale * m_ScaleEffect;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = m_OriginalScale;
        }

        private void OnDestroy()
        {
            if (m_Button != null)
            {
                m_Button.onClick.RemoveListener(AnimateClick);
            }
        }
    }

    /// <summary>
    /// UI动画管理器
    /// </summary>
    public class UIAnimationManager : System.IDisposable
    {
        private MonoBehaviour m_Owner;
        private float m_FadeSpeed;
        private float m_SlideSpeed;

        public UIAnimationManager(MonoBehaviour owner, float fadeSpeed, float slideSpeed)
        {
            m_Owner = owner;
            m_FadeSpeed = fadeSpeed;
            m_SlideSpeed = slideSpeed;
        }

        public void AnimatePanel(GameObject panel, bool show)
        {
            if (panel == null || m_Owner == null) return;

            if (show && !panel.activeSelf)
            {
                panel.SetActive(true);
                m_Owner.StartCoroutine(FadeIn(panel));
            }
            else if (!show && panel.activeSelf)
            {
                m_Owner.StartCoroutine(FadeOut(panel));
            }
        }

        private System.Collections.IEnumerator FadeIn(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.unscaledDeltaTime * m_FadeSpeed;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut(GameObject panel)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                panel.SetActive(false);
                yield break;
            }

            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.unscaledDeltaTime * m_FadeSpeed;
                yield return null;
            }
            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }

        public void Dispose()
        {
            // 清理资源
        }
    }

    /// <summary>
    /// 物品槽位UI组件
    /// </summary>
    public class ItemSlotUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image m_ItemIcon;
        [SerializeField] private TextMeshProUGUI m_ItemCountText;
        [SerializeField] private UnityEngine.UI.Button m_SlotButton;

        private ItemData m_Item;
        private int m_SlotIndex;

        public void SetItem(ItemData item)
        {
            m_Item = item;
            UpdateDisplay();
        }

        public void SetIndex(int index)
        {
            m_SlotIndex = index;
        }

        private void UpdateDisplay()
        {
            if (m_Item == null)
            {
                // 空槽位
                SetIcon(null);
                SetCount("");
            }
            else
            {
                // 有物品
                SetIcon(m_Item.Icon);
                SetCount(m_Item.StackSize > 1 ? m_Item.StackSize.ToString() : "");
            }
        }

        private void SetIcon(Sprite icon)
        {
            if (m_ItemIcon != null)
            {
                m_ItemIcon.sprite = icon;
                m_ItemIcon.enabled = icon != null;
            }
        }

        private void SetCount(string count)
        {
            if (m_ItemCountText != null)
            {
                m_ItemCountText.text = count;
                m_ItemCountText.gameObject.SetActive(!string.IsNullOrEmpty(count));
            }
        }

        private void Start()
        {
            if (m_SlotButton != null)
            {
                m_SlotButton.onClick.AddListener(OnSlotClicked);
            }
        }

        private void OnSlotClicked()
        {
            if (m_Item != null)
            {
                // 处理物品点击
                var gameManager = OptimizedGameManager.Instance;
                if (gameManager != null)
                {
                    // 使用物品或显示详情
                    GameLog.Info($"点击物品: {m_Item.Name}", "ItemSlotUI");
                }
            }
        }

        private void OnDestroy()
        {
            if (m_SlotButton != null)
            {
                m_SlotButton.onClick.RemoveListener(OnSlotClicked);
            }
        }
    }
}