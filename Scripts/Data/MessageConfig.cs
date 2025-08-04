using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 消息类别
    /// </summary>
    public enum MessageCategory
    {
        Exploration,    // 探索
        Battle,         // 战斗
        Treasure,       // 宝藏
        Item,           // 物品
        System,         // 系统
        Achievement,    // 成就
        Tutorial,       // 教程
        Error           // 错误
    }

    /// <summary>
    /// 消息重要性
    /// </summary>
    public enum MessageImportance
    {
        Low,            // 低重要性
        Normal,         // 普通
        High,           // 高重要性
        Critical        // 关键
    }

    /// <summary>
    /// 消息数据
    /// </summary>
    [System.Serializable]
    public class MessageData
    {
        [Header("消息基本信息")]
        public string MessageId;
        public MessageCategory Category;
        public MessageImportance Importance;
        
        [Header("消息内容")]
        [TextArea(2, 4)]
        public string Content;
        public List<string> Variations = new List<string>();
        
        [Header("本地化")]
        public string LocalizationKey;
        
        [Header("显示配置")]
        public bool ShowInUI = true;
        public float DisplayDuration = 3f;
        public Color TextColor = Color.white;
        
        [Header("音效配置")]
        public string SoundEffect;
        public bool PlaySound = false;

        /// <summary>
        /// 获取随机变体消息
        /// </summary>
        public string GetRandomMessage()
        {
            if (Variations.Count == 0)
                return Content;
            
            if (Variations.Count == 1)
                return Variations[0];
            
            return Variations[Random.Range(0, Variations.Count)];
        }

        /// <summary>
        /// 获取格式化消息
        /// </summary>
        public string GetFormattedMessage(params object[] args)
        {
            string message = GetRandomMessage();
            try
            {
                return args.Length > 0 ? string.Format(message, args) : message;
            }
            catch (System.FormatException)
            {
                GameLog.Warning($"消息格式化失败: {MessageId}, 内容: {message}", "MessageConfig");
                return message;
            }
        }
    }

    /// <summary>
    /// 消息配置
    /// 统一管理所有游戏消息，解决硬编码问题
    /// </summary>
    [CreateAssetMenu(fileName = "MessageConfig", menuName = "仙侠游戏/消息配置")]
    public class MessageConfig : ScriptableObject
    {
        [Header("探索消息")]
        public List<MessageData> ExplorationMessages = new List<MessageData>();
        
        [Header("战斗消息")]
        public List<MessageData> BattleMessages = new List<MessageData>();
        
        [Header("宝藏消息")]
        public List<MessageData> TreasureMessages = new List<MessageData>();
        
        [Header("物品消息")]
        public List<MessageData> ItemMessages = new List<MessageData>();
        
        [Header("系统消息")]
        public List<MessageData> SystemMessages = new List<MessageData>();
        
        [Header("成就消息")]
        public List<MessageData> AchievementMessages = new List<MessageData>();

        // 消息缓存字典
        private Dictionary<string, MessageData> m_MessageCache;
        private Dictionary<MessageCategory, List<MessageData>> m_CategoryCache;

        private void OnEnable()
        {
            BuildCache();
        }

        /// <summary>
        /// 构建消息缓存
        /// </summary>
        private void BuildCache()
        {
            m_MessageCache = new Dictionary<string, MessageData>();
            m_CategoryCache = new Dictionary<MessageCategory, List<MessageData>>();

            var allMessages = new List<MessageData>();
            allMessages.AddRange(ExplorationMessages);
            allMessages.AddRange(BattleMessages);
            allMessages.AddRange(TreasureMessages);
            allMessages.AddRange(ItemMessages);
            allMessages.AddRange(SystemMessages);
            allMessages.AddRange(AchievementMessages);

            foreach (var message in allMessages)
            {
                if (!string.IsNullOrEmpty(message.MessageId))
                {
                    m_MessageCache[message.MessageId] = message;
                }

                if (!m_CategoryCache.ContainsKey(message.Category))
                {
                    m_CategoryCache[message.Category] = new List<MessageData>();
                }
                m_CategoryCache[message.Category].Add(message);
            }
        }

        /// <summary>
        /// 根据ID获取消息
        /// </summary>
        public MessageData GetMessage(string messageId)
        {
            if (m_MessageCache == null) BuildCache();
            
            return m_MessageCache.TryGetValue(messageId, out MessageData message) ? message : null;
        }

        /// <summary>
        /// 根据类别获取随机消息
        /// </summary>
        public MessageData GetRandomMessage(MessageCategory category)
        {
            if (m_CategoryCache == null) BuildCache();
            
            if (m_CategoryCache.TryGetValue(category, out List<MessageData> messages) && messages.Count > 0)
            {
                return messages[Random.Range(0, messages.Count)];
            }
            
            return null;
        }

        /// <summary>
        /// 根据类别和重要性获取消息
        /// </summary>
        public List<MessageData> GetMessages(MessageCategory category, MessageImportance? importance = null)
        {
            if (m_CategoryCache == null) BuildCache();
            
            var result = new List<MessageData>();
            
            if (m_CategoryCache.TryGetValue(category, out List<MessageData> messages))
            {
                foreach (var message in messages)
                {
                    if (importance == null || message.Importance == importance.Value)
                    {
                        result.Add(message);
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 获取格式化的消息文本
        /// </summary>
        public string GetFormattedMessage(string messageId, params object[] args)
        {
            var message = GetMessage(messageId);
            return message?.GetFormattedMessage(args) ?? $"[消息未找到: {messageId}]";
        }

        /// <summary>
        /// 获取随机格式化消息
        /// </summary>
        public string GetRandomFormattedMessage(MessageCategory category, params object[] args)
        {
            var message = GetRandomMessage(category);
            return message?.GetFormattedMessage(args) ?? $"[无可用消息: {category}]";
        }

        /// <summary>
        /// 验证消息配置
        /// </summary>
        public List<string> ValidateMessages()
        {
            var errors = new List<string>();
            var usedIds = new HashSet<string>();

            var allMessages = new List<MessageData>();
            allMessages.AddRange(ExplorationMessages);
            allMessages.AddRange(BattleMessages);
            allMessages.AddRange(TreasureMessages);
            allMessages.AddRange(ItemMessages);
            allMessages.AddRange(SystemMessages);
            allMessages.AddRange(AchievementMessages);

            foreach (var message in allMessages)
            {
                // 检查ID重复
                if (!string.IsNullOrEmpty(message.MessageId))
                {
                    if (usedIds.Contains(message.MessageId))
                    {
                        errors.Add($"重复的消息ID: {message.MessageId}");
                    }
                    usedIds.Add(message.MessageId);
                }

                // 检查内容为空
                if (string.IsNullOrEmpty(message.Content) && message.Variations.Count == 0)
                {
                    errors.Add($"消息内容为空: {message.MessageId}");
                }

                // 检查显示时间
                if (message.DisplayDuration <= 0)
                {
                    errors.Add($"显示时间无效: {message.MessageId}");
                }
            }

            return errors;
        }

        /// <summary>
        /// 创建默认消息配置
        /// </summary>
        [ContextMenu("创建默认消息")]
        public void CreateDefaultMessages()
        {
            CreateExplorationMessages();
            CreateBattleMessages();
            CreateTreasureMessages();
            CreateItemMessages();
            CreateSystemMessages();
            CreateAchievementMessages();
            
            BuildCache();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void CreateExplorationMessages()
        {
            ExplorationMessages.Clear();
            
            var messages = new[]
            {
                new { id = "explore_start", content = "你开始了新的探索..." },
                new { id = "explore_ruins", content = "你在一片废墟中挖掘着..." },
                new { id = "explore_mountain", content = "你沿着山间小径前行..." },
                new { id = "explore_forest", content = "你在密林深处搜寻着..." },
                new { id = "explore_cave", content = "你来到了一处神秘的洞穴..." },
                new { id = "explore_altar", content = "你发现了一个古老的祭坛..." },
                new { id = "explore_lake", content = "你在湖边发现了可疑的痕迹..." }
            };

            foreach (var msg in messages)
            {
                ExplorationMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.Exploration,
                    Importance = MessageImportance.Normal,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 2f,
                    TextColor = Color.white
                });
            }
        }

        private void CreateBattleMessages()
        {
            BattleMessages.Clear();
            
            var messages = new[]
            {
                new { id = "battle_start", content = "战斗开始了！" },
                new { id = "battle_enemy_appear", content = "一个敌人突然出现了！" },
                new { id = "battle_attack", content = "你遭到了袭击！" },
                new { id = "battle_danger", content = "危险的气息逼近..." },
                new { id = "battle_discovered", content = "敌人发现了你！" },
                new { id = "battle_unavoidable", content = "战斗不可避免！" },
                new { id = "battle_victory", content = "战斗胜利！继续你的探索之路吧！" },
                new { id = "battle_defeat", content = "战斗失败了...休息一下再继续吧。" },
                new { id = "battle_escape", content = "成功逃脱了战斗！" }
            };

            foreach (var msg in messages)
            {
                BattleMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.Battle,
                    Importance = MessageImportance.High,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 3f,
                    TextColor = Color.red
                });
            }
        }

        private void CreateTreasureMessages()
        {
            TreasureMessages.Clear();
            
            var messages = new[]
            {
                new { id = "treasure_found", content = "你发现了一个古老的宝箱！" },
                new { id = "treasure_crevice", content = "在岩石缝隙中，你找到了宝藏！" },
                new { id = "treasure_shiny", content = "一个闪闪发光的物体吸引了你的注意！" },
                new { id = "treasure_buried", content = "你意外挖到了埋藏的宝物！" },
                new { id = "treasure_cave_light", content = "洞穴深处传来了宝光！" }
            };

            foreach (var msg in messages)
            {
                TreasureMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.Treasure,
                    Importance = MessageImportance.High,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 4f,
                    TextColor = Color.yellow,
                    SoundEffect = "treasure_found",
                    PlaySound = true
                });
            }
        }

        private void CreateItemMessages()
        {
            ItemMessages.Clear();
            
            var messages = new[]
            {
                new { id = "item_obtained", content = "获得物品：{0}" },
                new { id = "item_equipped", content = "装备了：{0}" },
                new { id = "item_unequipped", content = "卸下了：{0}" },
                new { id = "item_used", content = "使用了：{0}" },
                new { id = "item_dropped", content = "丢弃了：{0}" },
                new { id = "inventory_full", content = "背包已满！" }
            };

            foreach (var msg in messages)
            {
                ItemMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.Item,
                    Importance = MessageImportance.Normal,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 2f,
                    TextColor = Color.cyan
                });
            }
        }

        private void CreateSystemMessages()
        {
            SystemMessages.Clear();
            
            var messages = new[]
            {
                new { id = "game_saved", content = "游戏已保存！" },
                new { id = "game_loaded", content = "游戏数据加载完成！" },
                new { id = "level_up", content = "恭喜！等级提升到 {0} 级！" },
                new { id = "welcome", content = "欢迎来到仙侠世界！开始你的修仙之路吧！" },
                new { id = "error_save", content = "保存失败，请重试。" },
                new { id = "error_load", content = "没有找到存档数据。" }
            };

            foreach (var msg in messages)
            {
                SystemMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.System,
                    Importance = MessageImportance.Normal,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 3f,
                    TextColor = Color.green
                });
            }
        }

        private void CreateAchievementMessages()
        {
            AchievementMessages.Clear();
            
            var messages = new[]
            {
                new { id = "achievement_first_exploration", content = "成就达成：初次探索" },
                new { id = "achievement_first_battle", content = "成就达成：首战告捷" },
                new { id = "achievement_treasure_hunter", content = "成就达成：寻宝专家" },
                new { id = "achievement_level_10", content = "成就达成：修仙小成" },
                new { id = "achievement_rich", content = "成就达成：财源广进" }
            };

            foreach (var msg in messages)
            {
                AchievementMessages.Add(new MessageData
                {
                    MessageId = msg.id,
                    Category = MessageCategory.Achievement,
                    Importance = MessageImportance.Critical,
                    Content = msg.content,
                    ShowInUI = true,
                    DisplayDuration = 5f,
                    TextColor = Color.magenta,
                    SoundEffect = "achievement",
                    PlaySound = true
                });
            }
        }

#if UNITY_EDITOR
        [ContextMenu("验证消息配置")]
        public void ValidateInEditor()
        {
            var errors = ValidateMessages();
            if (errors.Count == 0)
            {
                UnityEngine.Debug.Log("✓ 消息配置验证通过");
            }
            else
            {
                UnityEngine.Debug.LogError($"✗ 消息配置验证失败，发现 {errors.Count} 个错误：");
                foreach (var error in errors)
                {
                    UnityEngine.Debug.LogError($"- {error}");
                }
            }
        }
#endif
    }

    /// <summary>
    /// 消息管理器
    /// 提供统一的消息访问接口
    /// </summary>
    public static class MessageManager
    {
        private static MessageConfig s_Config;

        /// <summary>
        /// 初始化消息管理器
        /// </summary>
        public static void Initialize(MessageConfig config = null)
        {
            if (config != null)
            {
                s_Config = config;
            }
            else
            {
                s_Config = Resources.Load<MessageConfig>("Config/MessageConfig");
            }

            if (s_Config == null)
            {
                GameLog.Error("MessageConfig未找到，请创建消息配置文件", "MessageManager");
            }
        }

        /// <summary>
        /// 获取消息
        /// </summary>
        public static string GetMessage(string messageId, params object[] args)
        {
            EnsureInitialized();
            return s_Config?.GetFormattedMessage(messageId, args) ?? $"[消息未找到: {messageId}]";
        }

        /// <summary>
        /// 获取随机消息
        /// </summary>
        public static string GetRandomMessage(MessageCategory category, params object[] args)
        {
            EnsureInitialized();
            return s_Config?.GetRandomFormattedMessage(category, args) ?? $"[无可用消息: {category}]";
        }

        /// <summary>
        /// 获取消息数据
        /// </summary>
        public static MessageData GetMessageData(string messageId)
        {
            EnsureInitialized();
            return s_Config?.GetMessage(messageId);
        }

        private static void EnsureInitialized()
        {
            if (s_Config == null)
            {
                Initialize();
            }
        }
    }
}