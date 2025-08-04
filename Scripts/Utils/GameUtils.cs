using System;
using System.Collections.Generic;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame.Utils
{
    /// <summary>
    /// 游戏通用工具类
    /// 提供便捷的开发支持方法
    /// </summary>
    public static class GameUtils
    {
        #region 服务快捷访问

        /// <summary>
        /// 快速获取配置服务
        /// </summary>
        public static IConfigService Config => GameServiceBootstrapper.GetService<IConfigService>();

        /// <summary>
        /// 快速获取事件服务
        /// </summary>
        public static IEventService Events => GameServiceBootstrapper.GetService<IEventService>();

        /// <summary>
        /// 快速获取日志服务
        /// </summary>
        public static ILoggingService Logger => GameServiceBootstrapper.GetService<ILoggingService>();

        /// <summary>
        /// 快速获取存档服务
        /// </summary>
        public static ISaveService SaveSystem => GameServiceBootstrapper.GetService<ISaveService>();

        /// <summary>
        /// 快速获取游戏状态服务
        /// </summary>
        public static IGameStateService GameState => GameServiceBootstrapper.GetService<IGameStateService>();

        /// <summary>
        /// 快速获取统计服务
        /// </summary>
        public static IStatisticsService Statistics => GameServiceBootstrapper.GetService<IStatisticsService>();

        /// <summary>
        /// 快速获取UI更新服务
        /// </summary>
        public static IUIUpdateService UIUpdater => GameServiceBootstrapper.GetService<IUIUpdateService>();

        #endregion

        #region 日志便捷方法

        /// <summary>
        /// 记录调试信息
        /// </summary>
        public static void LogDebug(string message, string category = "Game")
        {
            Logger?.Log(message, LogLevel.Debug, category);
        }

        /// <summary>
        /// 记录一般信息
        /// </summary>
        public static void LogInfo(string message, string category = "Game")
        {
            Logger?.Log(message, LogLevel.Info, category);
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        public static void LogWarning(string message, string category = "Game")
        {
            Logger?.Log(message, LogLevel.Warning, category);
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        public static void LogError(string message, string category = "Game")
        {
            Logger?.Log(message, LogLevel.Error, category);
        }

        /// <summary>
        /// 记录异常信息
        /// </summary>
        public static void LogException(Exception exception, string category = "Game")
        {
            Logger?.LogException(exception, category);
        }

        #endregion

        #region 事件便捷方法

        /// <summary>
        /// 触发游戏事件
        /// </summary>
        public static void TriggerEvent(GameEventType eventType, GameEventData eventData = null)
        {
            Events?.TriggerEvent(eventType, eventData);
        }

        /// <summary>
        /// 发送游戏消息
        /// </summary>
        public static void SendMessage(string message, MessageType messageType = MessageType.Normal)
        {
            TriggerEvent(GameEventType.GameMessage, new GameMessageEventData(message, messageType));
        }

        /// <summary>
        /// 发送成功消息
        /// </summary>
        public static void SendSuccessMessage(string message)
        {
            SendMessage(message, MessageType.Success);
        }

        /// <summary>
        /// 发送警告消息
        /// </summary>
        public static void SendWarningMessage(string message)
        {
            SendMessage(message, MessageType.Warning);
        }

        /// <summary>
        /// 发送错误消息
        /// </summary>
        public static void SendErrorMessage(string message)
        {
            SendMessage(message, MessageType.Error);
        }

        #endregion

        #region 数值计算工具

        /// <summary>
        /// 计算百分比值
        /// </summary>
        public static float CalculatePercentage(float current, float max)
        {
            if (max <= 0) return 0f;
            return Mathf.Clamp01(current / max);
        }

        /// <summary>
        /// 根据权重随机选择索引
        /// </summary>
        public static int GetWeightedRandomIndex(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return 0;

            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
                return 0;

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

        /// <summary>
        /// 计算运气影响的概率
        /// </summary>
        public static float CalculateLuckAffectedChance(float baseChance, int luckValue, float luckEffect = 0.01f)
        {
            float luckBonus = luckValue * luckEffect;
            return Mathf.Clamp01(baseChance + luckBonus);
        }

        /// <summary>
        /// 插值计算（用于等级缩放等）
        /// </summary>
        public static float Lerp(float from, float to, float ratio)
        {
            return from + (to - from) * Mathf.Clamp01(ratio);
        }

        /// <summary>
        /// 根据等级计算缩放值
        /// </summary>
        public static float CalculateLevelScale(int level, float baseValue, float perLevelMultiplier = 0.1f)
        {
            return baseValue * (1f + level * perLevelMultiplier);
        }

        #endregion

        #region 字符串和格式化工具

        /// <summary>
        /// 格式化大数字显示
        /// </summary>
        public static string FormatLargeNumber(long number)
        {
            if (number >= 1000000000)
                return $"{number / 1000000000.0:F1}B";
            if (number >= 1000000)
                return $"{number / 1000000.0:F1}M";
            if (number >= 1000)
                return $"{number / 1000.0:F1}K";
            return number.ToString();
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        public static string FormatTime(float timeInSeconds)
        {
            int hours = Mathf.FloorToInt(timeInSeconds / 3600);
            int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);

            if (hours > 0)
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            else
                return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// 获取稀有度颜色
        /// </summary>
        public static Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return new Color(0.5f, 0f, 1f); // 紫色
                case ItemRarity.Legendary: return new Color(1f, 0.5f, 0f); // 橙色
                case ItemRarity.Mythic: return Color.red;
                default: return Color.white;
            }
        }

        /// <summary>
        /// 获取稀有度中文名称
        /// </summary>
        public static string GetRarityChineseName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "普通";
                case ItemRarity.Uncommon: return "优秀";
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Epic: return "史诗";
                case ItemRarity.Legendary: return "传奇";
                case ItemRarity.Mythic: return "神话";
                default: return "未知";
            }
        }

        #endregion

        #region 游戏平衡工具

        /// <summary>
        /// 计算经验需求（指数增长）
        /// </summary>
        public static int CalculateExperienceRequired(int level, int baseExp = 100, float growthRate = 1.2f)
        {
            return Mathf.RoundToInt(baseExp * Mathf.Pow(growthRate, level - 1));
        }

        /// <summary>
        /// 计算伤害（包含暴击计算）
        /// </summary>
        public static int CalculateDamage(int baseAttack, int targetDefense, float criticalRate, float criticalDamage, out bool isCritical)
        {
            // 基础伤害计算
            int baseDamage = Mathf.Max(1, baseAttack - targetDefense / 2);
            
            // 暴击判定
            isCritical = UnityEngine.Random.Range(0f, 1f) < criticalRate;
            
            if (isCritical)
            {
                return Mathf.RoundToInt(baseDamage * criticalDamage);
            }
            
            return baseDamage;
        }

        /// <summary>
        /// 计算物品价值（考虑稀有度和等级）
        /// </summary>
        public static int CalculateItemValue(ItemRarity rarity, int level, int baseValue = 10)
        {
            float rarityMultiplier = 1f + (int)rarity * 0.5f;
            float levelMultiplier = 1f + level * 0.1f;
            return Mathf.RoundToInt(baseValue * rarityMultiplier * levelMultiplier);
        }

        #endregion

        #region 系统检查工具

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        public static bool IsServiceAvailable<T>() where T : class
        {
            return GameServiceBootstrapper.IsServiceRegistered<T>();
        }

        /// <summary>
        /// 检查核心服务状态
        /// </summary>
        public static bool AreEssentialServicesReady()
        {
            return IsServiceAvailable<IConfigService>() &&
                   IsServiceAvailable<IEventService>() &&
                   IsServiceAvailable<ILoggingService>();
        }

        /// <summary>
        /// 获取系统状态报告
        /// </summary>
        public static string GetSystemStatusReport()
        {
            var report = "=== 系统状态报告 ===\n";
            report += $"配置服务: {(IsServiceAvailable<IConfigService>() ? "✓" : "✗")}\n";
            report += $"事件服务: {(IsServiceAvailable<IEventService>() ? "✓" : "✗")}\n";
            report += $"日志服务: {(IsServiceAvailable<ILoggingService>() ? "✓" : "✗")}\n";
            report += $"存档服务: {(IsServiceAvailable<ISaveService>() ? "✓" : "✗")}\n";
            report += $"游戏状态服务: {(IsServiceAvailable<IGameStateService>() ? "✓" : "✗")}\n";
            report += $"统计服务: {(IsServiceAvailable<IStatisticsService>() ? "✓" : "✗")}\n";
            report += $"UI更新服务: {(IsServiceAvailable<IUIUpdateService>() ? "✓" : "✗")}\n";
            report += $"游戏数据服务: {(IsServiceAvailable<IGameDataService>() ? "✓" : "✗")}\n";
            return report;
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 打印系统状态到控制台
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PrintSystemStatus()
        {
            Debug.Log(GetSystemStatusReport());
        }

        /// <summary>
        /// 创建测试角色数据
        /// </summary>
#if UNITY_EDITOR
        public static CharacterStats CreateTestCharacterStats(int level = 10)
        {
            return new CharacterStats(level);
        }
#endif

        /// <summary>
        /// 创建测试物品
        /// </summary>
#if UNITY_EDITOR
        public static EquipmentData CreateTestEquipment(EquipmentType type = EquipmentType.Weapon, 
            ItemRarity rarity = ItemRarity.Rare, int level = 10)
        {
            return EquipmentData.GenerateRandomEquipment(type, rarity, level);
        }
#endif

        #endregion
    }

    /// <summary>
    /// 扩展方法类
    /// </summary>
    public static class GameExtensions
    {
        #region CharacterStats扩展

        /// <summary>
        /// 检查角色是否健康
        /// </summary>
        public static bool IsHealthy(this CharacterStats stats, float threshold = 0.5f)
        {
            return stats.HealthPercentage >= threshold;
        }

        /// <summary>
        /// 检查角色是否有足够法力
        /// </summary>
        public static bool HasEnoughMana(this CharacterStats stats, int requiredMana)
        {
            return stats.CurrentMana >= requiredMana;
        }

        /// <summary>
        /// 获取角色战力评分
        /// </summary>
        public static int GetPowerRating(this CharacterStats stats)
        {
            return stats.MaxHealth + stats.MaxMana + stats.Attack * 2 + stats.Defense + 
                   stats.Speed + Mathf.RoundToInt(stats.CriticalRate * 100) + stats.Luck;
        }

        #endregion

        #region ItemData扩展

        /// <summary>
        /// 检查物品是否为高品质
        /// </summary>
        public static bool IsHighQuality(this ItemData item)
        {
            return item.Rarity >= ItemRarity.Epic;
        }

        /// <summary>
        /// 获取物品品质等级（数值）
        /// </summary>
        public static int GetQualityLevel(this ItemData item)
        {
            return (int)item.Rarity;
        }

        #endregion

        #region Vector3扩展

        /// <summary>
        /// 添加随机偏移
        /// </summary>
        public static Vector3 AddRandomOffset(this Vector3 position, float range)
        {
            return position + new Vector3(
                UnityEngine.Random.Range(-range, range),
                0,
                UnityEngine.Random.Range(-range, range)
            );
        }

        #endregion

        #region GameObject扩展

        /// <summary>
        /// 安全销毁GameObject
        /// </summary>
        public static void SafeDestroy(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        #endregion
    }
}