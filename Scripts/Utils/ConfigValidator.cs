using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame.Utils
{
    /// <summary>
    /// 配置验证器
    /// 用于检查游戏配置的完整性和合理性
    /// </summary>
    public static class ConfigValidator
    {
        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();

            public void AddWarning(string message)
            {
                Warnings.Add(message);
                GameUtils.LogWarning($"配置警告: {message}", "ConfigValidator");
            }

            public void AddError(string message)
            {
                Errors.Add(message);
                IsValid = false;
                GameUtils.LogError($"配置错误: {message}", "ConfigValidator");
            }

            public string GetSummary()
            {
                var summary = new StringBuilder();
                summary.AppendLine("=== 配置验证结果 ===");
                summary.AppendLine($"状态: {(IsValid ? "✓ 通过" : "✗ 失败")}");
                summary.AppendLine($"警告数量: {Warnings.Count}");
                summary.AppendLine($"错误数量: {Errors.Count}");

                if (Warnings.Count > 0)
                {
                    summary.AppendLine("\n警告:");
                    foreach (var warning in Warnings)
                    {
                        summary.AppendLine($"- {warning}");
                    }
                }

                if (Errors.Count > 0)
                {
                    summary.AppendLine("\n错误:");
                    foreach (var error in Errors)
                    {
                        summary.AppendLine($"- {error}");
                    }
                }

                return summary.ToString();
            }
        }

        /// <summary>
        /// 验证完整的游戏配置
        /// </summary>
        public static ValidationResult ValidateGameConfig(GameConfig config)
        {
            var result = new ValidationResult();

            if (config == null)
            {
                result.AddError("GameConfig为空");
                return result;
            }

            // 验证各个子配置
            ValidateCharacterStats(config.CharacterStats, result);
            ValidateBattleConfig(config.Battle, result);
            ValidateExplorationConfig(config.Exploration, result);
            ValidateInventoryConfig(config.Inventory, result);
            ValidateUIConfig(config.UI, result);
            ValidateAudioConfig(config.Audio, result);

            GameUtils.LogInfo($"配置验证完成 - {(result.IsValid ? "通过" : "失败")}", "ConfigValidator");
            return result;
        }

        /// <summary>
        /// 验证角色属性配置
        /// </summary>
        private static void ValidateCharacterStats(CharacterStatsConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("CharacterStatsConfig为空");
                return;
            }

            // 检查基础属性
            if (config.BaseHealth <= 0)
                result.AddError("基础生命值必须大于0");
            if (config.BaseAttack <= 0)
                result.AddError("基础攻击力必须大于0");
            if (config.BaseDefense < 0)
                result.AddWarning("基础防御力为负数可能导致问题");

            // 检查成长属性
            if (config.HealthPerLevel <= 0)
                result.AddWarning("每级生命值增长为0，角色不会变强");
            if (config.AttackPerLevel <= 0)
                result.AddWarning("每级攻击力增长为0，角色不会变强");

            // 检查经验配置
            if (config.BaseExperience <= 0)
                result.AddError("基础经验值必须大于0");
            if (config.ExperienceGrowthRate <= 1.0f)
                result.AddError("经验增长率必须大于1.0");

            // 检查暴击配置
            if (config.BaseCriticalRate < 0 || config.BaseCriticalRate > 1)
                result.AddError("基础暴击率必须在0-1之间");
            if (config.BaseCriticalDamage < 1)
                result.AddError("基础暴击伤害必须大于等于1");
        }

        /// <summary>
        /// 验证战斗配置
        /// </summary>
        private static void ValidateBattleConfig(BattleConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("BattleConfig为空");
                return;
            }

            // 检查敌人配置
            if (config.EnemyLevelVariance < 0)
                result.AddError("敌人等级差异不能为负数");
            if (config.EnemyLevelVariance > 10)
                result.AddWarning("敌人等级差异过大可能导致难度不平衡");

            if (config.EnemyDifficultyMin <= 0 || config.EnemyDifficultyMax <= 0)
                result.AddError("敌人难度系数必须大于0");
            if (config.EnemyDifficultyMin > config.EnemyDifficultyMax)
                result.AddError("敌人最小难度不能大于最大难度");

            // 检查奖励配置
            if (config.BaseExperienceReward <= 0)
                result.AddError("基础经验奖励必须大于0");
            if (config.ItemRewardChance < 0 || config.ItemRewardChance > 1)
                result.AddError("物品奖励概率必须在0-1之间");

            // 检查失败惩罚
            if (config.GoldLossPercentage < 0 || config.GoldLossPercentage > 1)
                result.AddError("金钱损失百分比必须在0-1之间");
            if (config.MaxGoldLoss < 0)
                result.AddError("最大金钱损失不能为负数");
        }

        /// <summary>
        /// 验证探索配置
        /// </summary>
        private static void ValidateExplorationConfig(ExplorationConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("ExplorationConfig为空");
                return;
            }

            // 检查事件概率总和
            float totalChance = config.TreasureChance + config.BattleChance + 
                               config.ItemChance + config.NothingChance;
            
            if (Mathf.Abs(totalChance - 100f) > 1f)
                result.AddWarning($"探索事件概率总和为{totalChance:F1}%，建议为100%");

            // 检查各项概率
            if (config.TreasureChance < 0 || config.BattleChance < 0 || 
                config.ItemChance < 0 || config.NothingChance < 0)
                result.AddError("探索事件概率不能为负数");

            // 检查奖励配置
            if (config.MinGoldReward < 0 || config.MaxGoldReward < 0)
                result.AddError("金钱奖励不能为负数");
            if (config.MinGoldReward > config.MaxGoldReward)
                result.AddError("最小金钱奖励不能大于最大金钱奖励");

            if (config.NothingFoundExp < 0)
                result.AddError("空手而归经验奖励不能为负数");
        }

        /// <summary>
        /// 验证背包配置
        /// </summary>
        private static void ValidateInventoryConfig(InventoryConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("InventoryConfig为空");
                return;
            }

            if (config.InitialSlots <= 0)
                result.AddError("初始背包槽位必须大于0");
            if (config.MaxSlots <= config.InitialSlots)
                result.AddError("最大背包槽位必须大于初始槽位");
            if (config.MaxUpgradeSlots < 0)
                result.AddError("最大升级槽位不能为负数");

            if (config.SlotUpgradeCost <= 0)
                result.AddError("槽位升级费用必须大于0");
        }

        /// <summary>
        /// 验证UI配置
        /// </summary>
        private static void ValidateUIConfig(UIConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("UIConfig为空");
                return;
            }

            if (config.MaxMessageLines <= 0)
                result.AddError("最大消息行数必须大于0");
            if (config.MessageFadeTime < 0)
                result.AddError("消息淡出时间不能为负数");

            if (config.UIFadeSpeed <= 0)
                result.AddError("UI淡入淡出速度必须大于0");
            if (config.ButtonScaleEffect <= 0)
                result.AddError("按钮缩放效果必须大于0");

            if (config.UIUpdateInterval <= 0)
                result.AddError("UI更新间隔必须大于0");
            if (config.BattleUIUpdateInterval <= 0)
                result.AddError("战斗UI更新间隔必须大于0");

            // 性能警告
            if (config.UIUpdateInterval < 0.05f)
                result.AddWarning("UI更新间隔过小可能影响性能");
            if (config.BattleUIUpdateInterval < 0.02f)
                result.AddWarning("战斗UI更新间隔过小可能影响性能");
        }

        /// <summary>
        /// 验证音频配置
        /// </summary>
        private static void ValidateAudioConfig(AudioConfig config, ValidationResult result)
        {
            if (config == null)
            {
                result.AddError("AudioConfig为空");
                return;
            }

            if (config.MasterVolume < 0 || config.MasterVolume > 1)
                result.AddError("主音量必须在0-1之间");
            if (config.SFXVolume < 0 || config.SFXVolume > 1)
                result.AddError("音效音量必须在0-1之间");
            if (config.MusicVolume < 0 || config.MusicVolume > 1)
                result.AddError("音乐音量必须在0-1之间");
        }

        /// <summary>
        /// 验证物品模板
        /// </summary>
        public static ValidationResult ValidateItemTemplate(ItemTemplate template)
        {
            var result = new ValidationResult();

            if (template == null)
            {
                result.AddError("ItemTemplate为空");
                return result;
            }

            // 检查基本信息
            if (string.IsNullOrEmpty(template.TemplateId))
                result.AddError("模板ID不能为空");
            if (string.IsNullOrEmpty(template.ItemName))
                result.AddError("物品名称不能为空");

            // 检查等级范围
            if (template.MinLevel <= 0)
                result.AddError("最小等级必须大于0");
            if (template.MaxLevel <= 0)
                result.AddError("最大等级必须大于0");
            if (template.MinLevel > template.MaxLevel)
                result.AddError("最小等级不能大于最大等级");

            // 检查稀有度范围
            if (template.MinRarity > template.MaxRarity)
                result.AddError("最小稀有度不能大于最大稀有度");

            // 检查价值配置
            if (template.BaseValue < 0)
                result.AddError("基础价值不能为负数");

            // 检查堆叠配置
            if (template.MaxStackSize <= 0)
                result.AddError("最大堆叠数量必须大于0");

            // 检查掉落概率
            if (template.DropChance < 0 || template.DropChance > 1)
                result.AddError("掉落概率必须在0-1之间");

            return result;
        }

        /// <summary>
        /// 验证装备模板
        /// </summary>
        public static ValidationResult ValidateEquipmentTemplate(EquipmentTemplate template)
        {
            var result = ValidateItemTemplate(template);

            if (template == null)
                return result;

            // 验证属性修饰符
            foreach (var modifier in template.PrimaryStats)
            {
                ValidateStatModifier(modifier, result, "主要属性");
            }

            foreach (var modifier in template.SecondaryStats)
            {
                ValidateStatModifier(modifier, result, "次要属性");
            }

            return result;
        }

        /// <summary>
        /// 验证属性修饰符
        /// </summary>
        private static void ValidateStatModifier(StatModifier modifier, ValidationResult result, string prefix)
        {
            if (modifier == null)
            {
                result.AddError($"{prefix}修饰符为空");
                return;
            }

            if (string.IsNullOrEmpty(modifier.StatName))
                result.AddError($"{prefix}修饰符的属性名称为空");

            if (modifier.MinValue > modifier.MaxValue)
                result.AddError($"{prefix}修饰符的最小值不能大于最大值");
        }

        /// <summary>
        /// 获取配置健康度评分
        /// </summary>
        public static float GetConfigHealthScore(GameConfig config)
        {
            var result = ValidateGameConfig(config);
            
            if (!result.IsValid)
                return 0f;

            // 基础分数
            float score = 100f;

            // 每个警告扣5分
            score -= result.Warnings.Count * 5f;

            // 每个错误扣20分
            score -= result.Errors.Count * 20f;

            return Mathf.Clamp(score, 0f, 100f);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor方法：验证项目中的所有配置
        /// </summary>
        [UnityEditor.MenuItem("仙侠游戏/验证所有配置")]
        public static void ValidateAllConfigs()
        {
            var configs = Resources.LoadAll<GameConfig>("");
            int validCount = 0;
            int totalCount = configs.Length;

            foreach (var config in configs)
            {
                var result = ValidateGameConfig(config);
                Debug.Log($"配置 {config.name}: {(result.IsValid ? "✓" : "✗")}");
                
                if (result.IsValid)
                    validCount++;
                
                if (!result.IsValid || result.Warnings.Count > 0)
                {
                    Debug.Log(result.GetSummary());
                }
            }

            string summary = $"配置验证完成: {validCount}/{totalCount} 通过";
            if (validCount == totalCount)
                Debug.Log($"<color=green>{summary}</color>");
            else
                Debug.LogWarning($"<color=yellow>{summary}</color>");
        }

        /// <summary>
        /// Editor方法：验证项目中的所有物品模板
        /// </summary>
        [UnityEditor.MenuItem("仙侠游戏/验证所有物品模板")]
        public static void ValidateAllItemTemplates()
        {
            var templates = Resources.LoadAll<ItemTemplate>("");
            int validCount = 0;
            int totalCount = templates.Length;

            foreach (var template in templates)
            {
                var result = ValidateItemTemplate(template);
                Debug.Log($"模板 {template.name}: {(result.IsValid ? "✓" : "✗")}");
                
                if (result.IsValid)
                    validCount++;
                
                if (!result.IsValid || result.Warnings.Count > 0)
                {
                    Debug.Log(result.GetSummary());
                }
            }

            string summary = $"模板验证完成: {validCount}/{totalCount} 通过";
            if (validCount == totalCount)
                Debug.Log($"<color=green>{summary}</color>");
            else
                Debug.LogWarning($"<color=yellow>{summary}</color>");
        }
#endif
    }
}