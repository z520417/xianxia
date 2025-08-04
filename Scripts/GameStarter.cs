using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏启动器 - 用于快速测试和启动游戏
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        [Header("测试模式")]
        [SerializeField] private bool m_AutoStartGame = true;
        [SerializeField] private bool m_EnableDebugMode = true;
        
        [Header("UI测试组件")]
        [SerializeField] private Button m_TestExploreButton;
        [SerializeField] private Button m_TestBattleButton;
        [SerializeField] private Button m_TestInventoryButton;
        [SerializeField] private TextMeshProUGUI m_TestLogText;
        
        private GameManager m_GameManager;
        private string m_LogContent = "";
        
        private void Start()
        {
            // 查找或创建GameManager
            m_GameManager = GameManager.Instance;
            
            if (m_GameManager == null)
            {
                Debug.LogError("无法找到GameManager！请确保场景中有GameManager组件。");
                return;
            }
            
            // 设置测试按钮
            SetupTestButtons();
            
            // 订阅游戏消息
            if (m_GameManager != null)
            {
                m_GameManager.OnGameMessage += OnGameMessage;
            }
            
            // 自动启动游戏
            if (m_AutoStartGame)
            {
                StartGame();
            }
            
            AddLog("游戏启动器初始化完成！");
        }
        
        private void OnDestroy()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnGameMessage -= OnGameMessage;
            }
        }
        
        /// <summary>
        /// 启动游戏
        /// </summary>
        public void StartGame()
        {
            if (m_GameManager == null)
            {
                AddLog("错误：GameManager未找到！");
                return;
            }
            
            // 如果游戏已经初始化，重新开始
            m_GameManager.StartNewGame();
            AddLog("=== 仙侠探索挖宝游戏开始 ===");
            AddLog("点击'探索'开始你的冒险之旅！");
        }
        
        /// <summary>
        /// 设置测试按钮
        /// </summary>
        private void SetupTestButtons()
        {
            if (m_TestExploreButton != null)
            {
                m_TestExploreButton.onClick.AddListener(TestExplore);
                m_TestExploreButton.GetComponentInChildren<TextMeshProUGUI>().text = "探索";
            }
            
            if (m_TestBattleButton != null)
            {
                m_TestBattleButton.onClick.AddListener(TestBattle);
                m_TestBattleButton.GetComponentInChildren<TextMeshProUGUI>().text = "测试战斗";
            }
            
            if (m_TestInventoryButton != null)
            {
                m_TestInventoryButton.onClick.AddListener(TestAddItems);
                m_TestInventoryButton.GetComponentInChildren<TextMeshProUGUI>().text = "添加物品";
            }
        }
        
        /// <summary>
        /// 测试探索
        /// </summary>
        public void TestExplore()
        {
            if (m_GameManager != null)
            {
                m_GameManager.Explore();
            }
        }
        
        /// <summary>
        /// 测试战斗
        /// </summary>
        public void TestBattle()
        {
            if (m_GameManager?.BattleSystem != null && !m_GameManager.BattleSystem.IsBattleActive)
            {
                var playerStats = m_GameManager.EquipmentManager?.TotalStats ?? m_GameManager.PlayerStats;
                m_GameManager.BattleSystem.StartBattle(playerStats, m_GameManager.PlayerStats.Level);
                AddLog("启动了测试战斗！");
            }
            else
            {
                AddLog("已经在战斗中或战斗系统未就绪！");
            }
        }
        
        /// <summary>
        /// 测试添加物品
        /// </summary>
        public void TestAddItems()
        {
            if (RandomItemGenerator.Instance != null && m_GameManager?.InventorySystem != null)
            {
                // 添加一些测试物品
                var items = RandomItemGenerator.Instance.GenerateRandomItems(3, m_GameManager.PlayerStats.Level, m_GameManager.PlayerStats.Luck);
                
                foreach (var item in items)
                {
                    m_GameManager.InventorySystem.AddItem(item);
                }
                
                AddLog($"添加了 {items.Count} 个随机物品到背包！");
            }
        }
        
        /// <summary>
        /// 游戏消息处理
        /// </summary>
        private void OnGameMessage(string _message)
        {
            AddLog(_message);
        }
        
        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string _message)
        {
            if (m_EnableDebugMode)
            {
                Debug.Log($"[游戏] {_message}");
            }
            
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            m_LogContent += $"[{timestamp}] {_message}\n";
            
            if (m_TestLogText != null)
            {
                m_TestLogText.text = m_LogContent;
                
                // 限制日志长度
                if (m_LogContent.Length > 2000)
                {
                    int halfLength = m_LogContent.Length / 2;
                    int newlineIndex = m_LogContent.IndexOf('\n', halfLength);
                    if (newlineIndex > 0)
                    {
                        m_LogContent = "...\n" + m_LogContent.Substring(newlineIndex + 1);
                    }
                }
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("重启游戏")]
        private void RestartGame()
        {
            StartGame();
        }
        
        [ContextMenu("清空日志")]
        private void ClearLog()
        {
            m_LogContent = "";
            if (m_TestLogText != null)
            {
                m_TestLogText.text = "";
            }
        }
        
        [ContextMenu("添加1000灵石")]
        private void AddGold()
        {
            if (m_GameManager != null)
            {
                m_GameManager.AddGold(1000);
                AddLog("添加了1000灵石！");
            }
        }
        
        [ContextMenu("提升等级")]
        private void LevelUp()
        {
            if (m_GameManager?.PlayerStats != null)
            {
                m_GameManager.PlayerStats.GainExperience(m_GameManager.PlayerStats.ExperienceToNext);
                AddLog("等级提升！");
            }
        }
#endif
    }
}