#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using XianXiaGame.Utils;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// 配置设置向导
    /// 帮助开发者快速创建和设置游戏配置
    /// </summary>
    public class ConfigurationSetupWizard : EditorWindow
    {
        private bool m_CreateMainConfig = true;
        private bool m_CreateItemTemplates = true;
        private bool m_CreateExplorationEvents = true;
        private bool m_CreateEnemyData = true;
        private bool m_CreateSkillData = true;

        private string m_ConfigPath = "Assets/Resources/Config/";
        private string m_TemplatesPath = "Assets/Resources/Data/Templates/";
        private string m_EventsPath = "Assets/Resources/Data/Events/";
        private string m_EnemiesPath = "Assets/Resources/Data/Enemies/";
        private string m_SkillsPath = "Assets/Resources/Data/Skills/";

        [MenuItem("仙侠游戏/配置设置向导", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigurationSetupWizard>("配置设置向导");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("仙侠游戏配置设置向导", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "此向导将帮助您创建游戏所需的基础配置文件和数据模板。\n" +
                "建议首次使用时创建所有配置以确保系统正常运行。", 
                MessageType.Info);

            GUILayout.Space(10);

            // 配置选项
            GUILayout.Label("要创建的配置:", EditorStyles.boldLabel);
            m_CreateMainConfig = EditorGUILayout.Toggle("主游戏配置 (GameConfig)", m_CreateMainConfig);
            m_CreateItemTemplates = EditorGUILayout.Toggle("物品模板示例", m_CreateItemTemplates);
            m_CreateExplorationEvents = EditorGUILayout.Toggle("探索事件示例", m_CreateExplorationEvents);
            m_CreateEnemyData = EditorGUILayout.Toggle("敌人数据示例", m_CreateEnemyData);
            m_CreateSkillData = EditorGUILayout.Toggle("技能数据示例", m_CreateSkillData);

            GUILayout.Space(10);

            // 路径配置
            GUILayout.Label("输出路径:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            m_ConfigPath = EditorGUILayout.TextField("配置路径:", m_ConfigPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择配置文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    m_ConfigPath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_TemplatesPath = EditorGUILayout.TextField("模板路径:", m_TemplatesPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择模板文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    m_TemplatesPath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // 操作按钮
            if (GUILayout.Button("创建配置文件", GUILayout.Height(30)))
            {
                CreateConfigurations();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("验证现有配置", GUILayout.Height(25)))
            {
                ValidateExistingConfigs();
            }

            if (GUILayout.Button("生成配置报告", GUILayout.Height(25)))
            {
                GenerateConfigReport();
            }

            GUILayout.Space(20);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "提示:\n" +
                "• 首次设置建议创建所有配置\n" +
                "• 配置文件创建后可在Inspector中调整参数\n" +
                "• 使用验证功能检查配置的正确性\n" +
                "• 生成的示例可以作为学习参考", 
                MessageType.Info);
        }

        private void CreateConfigurations()
        {
            try
            {
                int createdCount = 0;

                if (m_CreateMainConfig)
                {
                    CreateMainGameConfig();
                    createdCount++;
                }

                if (m_CreateItemTemplates)
                {
                    CreateItemTemplates();
                    createdCount++;
                }

                if (m_CreateExplorationEvents)
                {
                    CreateExplorationEvents();
                    createdCount++;
                }

                if (m_CreateEnemyData)
                {
                    CreateEnemyData();
                    createdCount++;
                }

                if (m_CreateSkillData)
                {
                    CreateSkillData();
                    createdCount++;
                }

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("配置创建完成", 
                    $"成功创建了 {createdCount} 类配置文件!\n\n" +
                    "请在Project窗口中查看生成的配置文件，\n" +
                    "并根据需要调整参数。", "确定");

                Debug.Log($"<color=green>配置创建完成! 共创建 {createdCount} 类配置文件</color>");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", $"配置创建过程中出现错误:\n{e.Message}", "确定");
                Debug.LogError($"配置创建失败: {e.Message}");
            }
        }

        private void CreateMainGameConfig()
        {
            EnsureDirectoryExists(m_ConfigPath);

            var config = ScriptableObject.CreateInstance<GameConfig>();
            
            // 设置默认值
            config.CharacterStats = new CharacterStatsConfig
            {
                BaseHealth = 100,
                BaseAttack = 15,
                BaseDefense = 8,
                BaseSpeed = 12,
                BaseLuck = 10,
                HealthPerLevel = 20,
                AttackPerLevel = 3,
                DefensePerLevel = 2,
                SpeedPerLevel = 1,
                BaseExperience = 100,
                ExperiencePerLevel = 25,
                ExperienceGrowthRate = 1.15f,
                BaseCriticalRate = 0.05f,
                BaseCriticalDamage = 1.5f
            };

            config.Battle = new BattleConfig
            {
                EnemyLevelVariance = 2,
                EnemyDifficultyMin = 0.8f,
                EnemyDifficultyMax = 1.2f,
                BaseExperienceReward = 25,
                ItemRewardChance = 0.3f,
                GoldLossPercentage = 0.1f,
                MaxGoldLoss = 100
            };

            config.Exploration = new ExplorationConfig
            {
                TreasureChance = 25f,
                BattleChance = 40f,
                ItemChance = 25f,
                NothingChance = 10f,
                MinGoldReward = 50,
                MaxGoldReward = 200,
                NothingFoundExp = 5
            };

            config.Inventory = new InventoryConfig
            {
                InitialSlots = 20,
                MaxSlots = 100,
                MaxUpgradeSlots = 80,
                SlotUpgradeCost = 1000
            };

            config.UI = new UIConfig
            {
                MaxMessageLines = 20,
                MessageFadeTime = 0.5f,
                UIFadeSpeed = 2f,
                ButtonScaleEffect = 1.1f,
                UIUpdateInterval = 0.1f,
                BattleUIUpdateInterval = 0.05f
            };

            config.Audio = new AudioConfig
            {
                EnableSFX = true,
                EnableMusic = true,
                MasterVolume = 1f,
                SFXVolume = 0.8f,
                MusicVolume = 0.6f
            };

            string assetPath = m_ConfigPath + "DefaultGameConfig.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            Debug.Log($"创建主配置文件: {assetPath}");
        }

        private void CreateItemTemplates()
        {
            EnsureDirectoryExists(m_TemplatesPath);

            // 创建武器模板
            CreateWeaponTemplate();
            CreateArmorTemplate();
            CreateConsumableTemplate();
            CreateMaterialTemplate();
            CreateTreasureTemplate();
        }

        private void CreateWeaponTemplate()
        {
            var template = ScriptableObject.CreateInstance<EquipmentTemplate>();
            template.TemplateId = "weapon_sword_basic";
            template.ItemName = "灵剑";
            template.Description = "散发着淡淡灵气的长剑，是修仙者的必备武器。";
            template.ItemType = ItemType.Equipment;
            template.EquipmentType = EquipmentType.Weapon;
            template.MinRarity = ItemRarity.Common;
            template.MaxRarity = ItemRarity.Legendary;
            template.MinLevel = 1;
            template.MaxLevel = 50;
            template.BaseValue = 100;
            template.MaxStackSize = 1;
            template.DropChance = 0.15f;

            // 添加主要属性（攻击力）
            var attackModifier = new StatModifier
            {
                StatName = "Attack",
                ModifierType = StatModifierType.Flat,
                MinValue = 5f,
                MaxValue = 15f
            };
            template.PrimaryStats.Add(attackModifier);

            // 添加次要属性（暴击率）
            var critModifier = new StatModifier
            {
                StatName = "CriticalRate",
                ModifierType = StatModifierType.Flat,
                MinValue = 1f,
                MaxValue = 5f
            };
            template.SecondaryStats.Add(critModifier);

            string assetPath = m_TemplatesPath + "WeaponTemplate_Sword.asset";
            AssetDatabase.CreateAsset(template, assetPath);
            Debug.Log($"创建武器模板: {assetPath}");
        }

        private void CreateArmorTemplate()
        {
            var template = ScriptableObject.CreateInstance<EquipmentTemplate>();
            template.TemplateId = "armor_robe_basic";
            template.ItemName = "法袍";
            template.Description = "用特殊材料编织的长袍，能够抵御一定的伤害。";
            template.ItemType = ItemType.Equipment;
            template.EquipmentType = EquipmentType.Armor;
            template.MinRarity = ItemRarity.Common;
            template.MaxRarity = ItemRarity.Legendary;
            template.MinLevel = 1;
            template.MaxLevel = 50;
            template.BaseValue = 80;

            // 主要属性（防御力和生命值）
            var defenseModifier = new StatModifier
            {
                StatName = "Defense",
                ModifierType = StatModifierType.Flat,
                MinValue = 3f,
                MaxValue = 10f
            };
            template.PrimaryStats.Add(defenseModifier);

            var healthModifier = new StatModifier
            {
                StatName = "Health",
                ModifierType = StatModifierType.Flat,
                MinValue = 10f,
                MaxValue = 30f
            };
            template.PrimaryStats.Add(healthModifier);

            string assetPath = m_TemplatesPath + "ArmorTemplate_Robe.asset";
            AssetDatabase.CreateAsset(template, assetPath);
            Debug.Log($"创建护甲模板: {assetPath}");
        }

        private void CreateConsumableTemplate()
        {
            var template = ScriptableObject.CreateInstance<ConsumableTemplate>();
            template.TemplateId = "potion_health_basic";
            template.ItemName = "回血丹";
            template.Description = "能够快速恢复生命力的丹药。";
            template.ItemType = ItemType.Consumable;
            template.ConsumableType = ConsumableType.HealthPotion;
            template.MinRarity = ItemRarity.Common;
            template.MaxRarity = ItemRarity.Rare;
            template.BaseValue = 50;
            template.MaxStackSize = 99;
            template.DropChance = 0.2f;

            // 添加治疗效果
            var healEffect = new ConsumableEffect
            {
                EffectName = "瞬间治疗",
                EffectType = EffectType.InstantHeal,
                BaseValue = 50f,
                ValuePerLevel = 5f,
                Duration = 0f
            };
            template.Effects.Add(healEffect);

            string assetPath = m_TemplatesPath + "ConsumableTemplate_HealthPotion.asset";
            AssetDatabase.CreateAsset(template, assetPath);
            Debug.Log($"创建消耗品模板: {assetPath}");
        }

        private void CreateMaterialTemplate()
        {
            var template = ScriptableObject.CreateInstance<MaterialTemplate>();
            template.TemplateId = "material_iron_ore";
            template.ItemName = "铁矿石";
            template.Description = "常见的金属矿石，可用于锻造装备。";
            template.ItemType = ItemType.Material;
            template.MaterialType = MaterialType.Ore;
            template.BaseValue = 10;
            template.MaxStackSize = 999;
            template.DropChance = 0.3f;

            template.UsedForCrafting.Add("weapon_crafting");
            template.UsedForCrafting.Add("armor_crafting");
            template.Sources.Add("mining");
            template.Sources.Add("exploration");

            string assetPath = m_TemplatesPath + "MaterialTemplate_IronOre.asset";
            AssetDatabase.CreateAsset(template, assetPath);
            Debug.Log($"创建材料模板: {assetPath}");
        }

        private void CreateTreasureTemplate()
        {
            var template = ScriptableObject.CreateInstance<TreasureTemplate>();
            template.TemplateId = "treasure_ancient_coin";
            template.ItemName = "古代金币";
            template.Description = "来自远古时代的金币，具有很高的收藏价值。";
            template.ItemType = ItemType.Treasure;
            template.TreasureType = TreasureType.Coin;
            template.BaseValue = 500;
            template.CollectionValue = 200;
            template.MaxStackSize = 99;
            template.DropChance = 0.05f;

            template.SpecialProperties.Add("古代文明遗物");
            template.SpecialProperties.Add("具有神秘力量");

            string assetPath = m_TemplatesPath + "TreasureTemplate_AncientCoin.asset";
            AssetDatabase.CreateAsset(template, assetPath);
            Debug.Log($"创建珍宝模板: {assetPath}");
        }

        private void CreateExplorationEvents()
        {
            EnsureDirectoryExists(m_EventsPath);

            // 创建几个示例探索事件
            CreateTreasureEvent();
            CreateBattleEvent();
            CreateMerchantEvent();
            CreateShrineEvent();
        }

        private void CreateTreasureEvent()
        {
            var eventData = ScriptableObject.CreateInstance<ExplorationEventData>();
            eventData.EventId = "treasure_ancient_cache";
            eventData.EventName = "古代宝藏";
            eventData.EventDescription = "发现了一个古老的宝藏缓存";
            eventData.EventType = ExplorationEventType.TreasureHunt;
            eventData.MinPlayerLevel = 1;
            eventData.MaxPlayerLevel = 100;
            eventData.BaseChance = 15f;

            eventData.StartMessages.Add("你发现了一个散发着古老气息的宝箱...");
            eventData.StartMessages.Add("神秘的光芒从地下透出...");
            eventData.SuccessMessages.Add("你成功打开了宝箱，获得了珍贵的宝物！");
            eventData.FailureMessages.Add("宝箱被强力的法术保护着，无法打开...");

            eventData.Rewards = new ExplorationReward
            {
                BaseExperience = 20,
                MinGold = 100,
                MaxGold = 300,
                GoldPerLevel = 5f
            };

            string assetPath = m_EventsPath + "Event_AncientTreasure.asset";
            AssetDatabase.CreateAsset(eventData, assetPath);
            Debug.Log($"创建探索事件: {assetPath}");
        }

        private void CreateBattleEvent()
        {
            var eventData = ScriptableObject.CreateInstance<ExplorationEventData>();
            eventData.EventId = "battle_wild_beast";
            eventData.EventName = "野兽袭击";
            eventData.EventDescription = "遭遇了野生的灵兽";
            eventData.EventType = ExplorationEventType.EnemyEncounter;
            eventData.BaseChance = 30f;

            eventData.StartMessages.Add("一只凶猛的灵兽从灌木丛中跳出！");
            eventData.StartMessages.Add("你听到了威胁性的咆哮声...");
            eventData.SuccessMessages.Add("你成功击败了野兽！");
            eventData.FailureMessages.Add("你被野兽击败了...");

            string assetPath = m_EventsPath + "Event_WildBeast.asset";
            AssetDatabase.CreateAsset(eventData, assetPath);
            Debug.Log($"创建战斗事件: {assetPath}");
        }

        private void CreateMerchantEvent()
        {
            var eventData = ScriptableObject.CreateInstance<ExplorationEventData>();
            eventData.EventId = "merchant_traveling";
            eventData.EventName = "旅行商人";
            eventData.EventDescription = "遇到了一位神秘的旅行商人";
            eventData.EventType = ExplorationEventType.Merchant;
            eventData.BaseChance = 10f;

            eventData.StartMessages.Add("一位神秘的商人向你走来...");
            eventData.StartMessages.Add("'年轻的修仙者，要看看我的货物吗？'");

            string assetPath = m_EventsPath + "Event_TravelingMerchant.asset";
            AssetDatabase.CreateAsset(eventData, assetPath);
            Debug.Log($"创建商人事件: {assetPath}");
        }

        private void CreateShrineEvent()
        {
            var eventData = ScriptableObject.CreateInstance<ExplorationEventData>();
            eventData.EventId = "shrine_blessing";
            eventData.EventName = "神秘神龛";
            eventData.EventDescription = "发现了一个古老的神龛";
            eventData.EventType = ExplorationEventType.Shrine;
            eventData.BaseChance = 5f;

            eventData.StartMessages.Add("你发现了一个散发着神圣光芒的神龛...");
            eventData.SuccessMessages.Add("神龛赐予了你祝福！");

            eventData.Rewards = new ExplorationReward
            {
                BaseExperience = 50,
                ExperiencePerLevel = 2f
            };

            string assetPath = m_EventsPath + "Event_BlessedShrine.asset";
            AssetDatabase.CreateAsset(eventData, assetPath);
            Debug.Log($"创建神龛事件: {assetPath}");
        }

        private void CreateEnemyData()
        {
            EnsureDirectoryExists(m_EnemiesPath);

            CreateBasicEnemy();
            CreateEliteEnemy();
            CreateBossEnemy();
        }

        private void CreateBasicEnemy()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.EnemyId = "wolf_forest";
            enemy.EnemyName = "森林狼";
            enemy.Description = "栖息在森林中的灵兽，具有一定的灵智。";
            enemy.BaseLevel = 5;
            enemy.MinLevel = 1;
            enemy.MaxLevel = 15;
            enemy.HealthMultiplier = 1.0f;
            enemy.AttackMultiplier = 0.9f;
            enemy.DefenseMultiplier = 0.8f;
            enemy.SpeedMultiplier = 1.2f;
            enemy.AttackChance = 0.8f;
            enemy.DefendChance = 0.2f;
            enemy.BaseExperienceReward = 25;
            enemy.BaseGoldReward = 15;

            enemy.BattleMessages = new string[]
            {
                "森林狼露出了尖锐的獠牙！",
                "狼眼中闪烁着危险的光芒！",
                "森林狼发出了威胁性的低吼！"
            };

            string assetPath = m_EnemiesPath + "Enemy_ForestWolf.asset";
            AssetDatabase.CreateAsset(enemy, assetPath);
            Debug.Log($"创建敌人数据: {assetPath}");
        }

        private void CreateEliteEnemy()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.EnemyId = "bear_spirit";
            enemy.EnemyName = "灵熊";
            enemy.Description = "巨大的灵兽，拥有强大的力量和厚实的皮毛。";
            enemy.BaseLevel = 15;
            enemy.MinLevel = 10;
            enemy.MaxLevel = 25;
            enemy.HealthMultiplier = 1.5f;
            enemy.AttackMultiplier = 1.3f;
            enemy.DefenseMultiplier = 1.4f;
            enemy.SpeedMultiplier = 0.8f;
            enemy.BaseExperienceReward = 75;
            enemy.BaseGoldReward = 50;
            enemy.NameColor = Color.yellow;

            string assetPath = m_EnemiesPath + "Enemy_SpiritBear.asset";
            AssetDatabase.CreateAsset(enemy, assetPath);
            Debug.Log($"创建精英敌人: {assetPath}");
        }

        private void CreateBossEnemy()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.EnemyId = "dragon_ancient";
            enemy.EnemyName = "远古龙族";
            enemy.Description = "传说中的远古龙族，拥有毁天灭地的力量。";
            enemy.BaseLevel = 50;
            enemy.MinLevel = 40;
            enemy.MaxLevel = 60;
            enemy.HealthMultiplier = 3.0f;
            enemy.AttackMultiplier = 2.5f;
            enemy.DefenseMultiplier = 2.0f;
            enemy.SpeedMultiplier = 1.0f;
            enemy.SpecialSkillChance = 0.3f;
            enemy.BaseExperienceReward = 500;
            enemy.BaseGoldReward = 1000;
            enemy.NameColor = Color.red;

            string assetPath = m_EnemiesPath + "Enemy_AncientDragon.asset";
            AssetDatabase.CreateAsset(enemy, assetPath);
            Debug.Log($"创建Boss敌人: {assetPath}");
        }

        private void CreateSkillData()
        {
            EnsureDirectoryExists(m_SkillsPath);

            CreateBasicSkill();
            CreateAdvancedSkill();
            CreateUltimateSkill();
        }

        private void CreateBasicSkill()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.SkillId = "attack_basic_slash";
            skill.SkillName = "基础剑术";
            skill.Description = "最基本的剑法攻击，造成物理伤害。";
            skill.SkillType = SkillType.Active;
            skill.TargetType = SkillTargetType.Enemy;
            skill.MaxLevel = 5;
            skill.UnlockLevel = 1;
            skill.ManaCost = 10;
            skill.CooldownTime = 2f;
            skill.CastTime = 0.5f;
            skill.Range = 1f;

            var damageEffect = new SkillEffect
            {
                EffectType = SkillEffectType.Damage,
                EffectName = "剑击伤害",
                BaseValue = 120f,
                ValuePerLevel = 10f,
                Chance = 1f
            };
            skill.Effects.Add(damageEffect);

            string assetPath = m_SkillsPath + "Skill_BasicSlash.asset";
            AssetDatabase.CreateAsset(skill, assetPath);
            Debug.Log($"创建基础技能: {assetPath}");
        }

        private void CreateAdvancedSkill()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.SkillId = "heal_spirit_recovery";
            skill.SkillName = "灵气回复";
            skill.Description = "调动体内灵气，快速恢复生命力。";
            skill.SkillType = SkillType.Active;
            skill.TargetType = SkillTargetType.Self;
            skill.MaxLevel = 10;
            skill.UnlockLevel = 5;
            skill.ManaCost = 25;
            skill.CooldownTime = 10f;

            var healEffect = new SkillEffect
            {
                EffectType = SkillEffectType.Heal,
                EffectName = "生命恢复",
                BaseValue = 80f,
                ValuePerLevel = 15f,
                Chance = 1f
            };
            skill.Effects.Add(healEffect);

            string assetPath = m_SkillsPath + "Skill_SpiritRecovery.asset";
            AssetDatabase.CreateAsset(skill, assetPath);
            Debug.Log($"创建治疗技能: {assetPath}");
        }

        private void CreateUltimateSkill()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.SkillId = "ultimate_sword_storm";
            skill.SkillName = "剑气风暴";
            skill.Description = "释放强大的剑气，对敌人造成巨额伤害。";
            skill.SkillType = SkillType.Active;
            skill.TargetType = SkillTargetType.Enemy;
            skill.MaxLevel = 3;
            skill.UnlockLevel = 20;
            skill.ManaCost = 100;
            skill.CooldownTime = 60f;
            skill.CastTime = 3f;

            var ultimateEffect = new SkillEffect
            {
                EffectType = SkillEffectType.Damage,
                EffectName = "剑气风暴",
                BaseValue = 500f,
                ValuePerLevel = 100f,
                Chance = 1f
            };
            skill.Effects.Add(ultimateEffect);

            string assetPath = m_SkillsPath + "Skill_SwordStorm.asset";
            AssetDatabase.CreateAsset(skill, assetPath);
            Debug.Log($"创建终极技能: {assetPath}");
        }

        private void ValidateExistingConfigs()
        {
            ConfigValidator.ValidateAllConfigs();
            ConfigValidator.ValidateAllItemTemplates();
        }

        private void GenerateConfigReport()
        {
            var configs = Resources.LoadAll<GameConfig>("");
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 游戏配置报告 ===");
            report.AppendLine($"生成时间: {System.DateTime.Now}");
            report.AppendLine($"配置文件数量: {configs.Length}");
            
            foreach (var config in configs)
            {
                float healthScore = ConfigValidator.GetConfigHealthScore(config);
                report.AppendLine($"\n配置: {config.name}");
                report.AppendLine($"健康度: {healthScore:F1}/100");
                
                var validation = ConfigValidator.ValidateGameConfig(config);
                if (validation.Warnings.Count > 0)
                {
                    report.AppendLine($"警告数: {validation.Warnings.Count}");
                }
                if (validation.Errors.Count > 0)
                {
                    report.AppendLine($"错误数: {validation.Errors.Count}");
                }
            }

            string reportPath = "Assets/ConfigurationReport.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();
            
            EditorUtility.RevealInFinder(reportPath);
            Debug.Log($"配置报告已生成: {reportPath}");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif