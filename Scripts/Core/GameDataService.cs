using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏数据服务实现
    /// 负责管理和提供所有游戏数据
    /// </summary>
    public class GameDataService : IGameDataService
    {
        #region 数据缓存

        private readonly Dictionary<string, ItemData> m_ItemCache = new Dictionary<string, ItemData>();
        private readonly Dictionary<string, EquipmentData> m_EquipmentCache = new Dictionary<string, EquipmentData>();
        private readonly Dictionary<string, EnemyData> m_EnemyCache = new Dictionary<string, EnemyData>();
        private readonly Dictionary<string, ConsumableData> m_ConsumableCache = new Dictionary<string, ConsumableData>();

        private readonly List<ItemData> m_AllItems = new List<ItemData>();
        private readonly List<EquipmentData> m_AllEquipments = new List<EquipmentData>();
        private readonly List<EnemyData> m_AllEnemies = new List<EnemyData>();
        private readonly List<ConsumableData> m_AllConsumables = new List<ConsumableData>();

        #endregion

        #region 配置引用

        private IConfigService m_ConfigService;
        private ILoggingService m_LoggingService;

        #endregion

        #region 初始化

        public GameDataService()
        {
            m_ConfigService = ServiceLocator.GetService<IConfigService>();
            m_LoggingService = ServiceLocator.GetService<ILoggingService>();

            InitializeData();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            try
            {
                LoadItemTemplates();
                LoadEnemyData();
                BuildDataCaches();

                m_LoggingService?.Log($"游戏数据服务初始化完成: 物品={m_AllItems.Count}, 装备={m_AllEquipments.Count}, 敌人={m_AllEnemies.Count}, 消耗品={m_AllConsumables.Count}", 
                    LogLevel.Info, "GameDataService");
            }
            catch (System.Exception e)
            {
                m_LoggingService?.Log($"游戏数据服务初始化失败: {e.Message}", LogLevel.Error, "GameDataService");
            }
        }

        /// <summary>
        /// 加载物品模板
        /// </summary>
        private void LoadItemTemplates()
        {
            // 从Resources加载ItemTemplates
            var templates = Resources.LoadAll<ItemTemplate>("Data/ItemTemplates");
            foreach (var template in templates)
            {
                if (template != null)
                {
                    var items = GenerateItemsFromTemplate(template);
                    m_AllItems.AddRange(items);
                }
            }

            // 从Resources加载EquipmentTemplates
            var equipmentTemplates = Resources.LoadAll<EquipmentTemplate>("Data/EquipmentTemplates");
            foreach (var template in equipmentTemplates)
            {
                if (template != null)
                {
                    var equipments = GenerateEquipmentsFromTemplate(template);
                    m_AllEquipments.AddRange(equipments);
                }
            }

            // 从Resources加载ConsumableTemplates
            var consumableTemplates = Resources.LoadAll<ConsumableTemplate>("Data/ConsumableTemplates");
            foreach (var template in consumableTemplates)
            {
                if (template != null)
                {
                    var consumables = GenerateConsumablesFromTemplate(template);
                    m_AllConsumables.AddRange(consumables);
                }
            }
        }

        /// <summary>
        /// 加载敌人数据
        /// </summary>
        private void LoadEnemyData()
        {
            // 创建默认敌人数据
            CreateDefaultEnemies();

            // 从Resources加载额外敌人数据
            var enemyAssets = Resources.LoadAll<EnemyData>("Data/Enemies");
            foreach (var enemy in enemyAssets)
            {
                if (enemy != null)
                {
                    m_AllEnemies.Add(enemy);
                }
            }
        }

        /// <summary>
        /// 构建数据缓存
        /// </summary>
        private void BuildDataCaches()
        {
            // 构建物品缓存
            foreach (var item in m_AllItems)
            {
                if (!string.IsNullOrEmpty(item.ItemId))
                {
                    m_ItemCache[item.ItemId] = item;
                }
            }

            // 构建装备缓存
            foreach (var equipment in m_AllEquipments)
            {
                if (!string.IsNullOrEmpty(equipment.ItemId))
                {
                    m_EquipmentCache[equipment.ItemId] = equipment;
                }
            }

            // 构建敌人缓存
            foreach (var enemy in m_AllEnemies)
            {
                if (!string.IsNullOrEmpty(enemy.EnemyId))
                {
                    m_EnemyCache[enemy.EnemyId] = enemy;
                }
            }

            // 构建消耗品缓存
            foreach (var consumable in m_AllConsumables)
            {
                if (!string.IsNullOrEmpty(consumable.ItemId))
                {
                    m_ConsumableCache[consumable.ItemId] = consumable;
                }
            }
        }

        #endregion

        #region 物品数据接口实现

        public List<ItemData> GetAllItems()
        {
            return new List<ItemData>(m_AllItems);
        }

        public ItemData GetItemById(string itemId)
        {
            return m_ItemCache.TryGetValue(itemId, out ItemData item) ? item : null;
        }

        public ItemData CreateItemFromTemplate(string templateId, ItemRarity rarity, int level)
        {
            // 查找模板
            var template = Resources.Load<ItemTemplate>($"Data/ItemTemplates/{templateId}");
            if (template == null)
            {
                m_LoggingService?.Log($"找不到物品模板: {templateId}", LogLevel.Warning, "GameDataService");
                return null;
            }

            // 从模板生成物品
            var items = GenerateItemsFromTemplate(template, rarity, level, 1);
            return items.Count > 0 ? items[0] : null;
        }

        #endregion

        #region 装备数据接口实现

        public List<EquipmentData> GetAllEquipments()
        {
            return new List<EquipmentData>(m_AllEquipments);
        }

        public EquipmentData GetEquipmentById(string equipmentId)
        {
            return m_EquipmentCache.TryGetValue(equipmentId, out EquipmentData equipment) ? equipment : null;
        }

        public EquipmentData CreateEquipmentFromTemplate(string templateId, EquipmentType type, ItemRarity rarity, int level)
        {
            var template = Resources.Load<EquipmentTemplate>($"Data/EquipmentTemplates/{templateId}");
            if (template == null)
            {
                m_LoggingService?.Log($"找不到装备模板: {templateId}", LogLevel.Warning, "GameDataService");
                return null;
            }

            var equipments = GenerateEquipmentsFromTemplate(template, type, rarity, level, 1);
            return equipments.Count > 0 ? equipments[0] : null;
        }

        #endregion

        #region 敌人数据接口实现

        public List<EnemyData> GetAllEnemies()
        {
            return new List<EnemyData>(m_AllEnemies);
        }

        public EnemyData GetEnemyById(string enemyId)
        {
            return m_EnemyCache.TryGetValue(enemyId, out EnemyData enemy) ? enemy : null;
        }

        public EnemyData GetRandomEnemyByLevel(int level)
        {
            // 过滤适合等级的敌人
            var suitableEnemies = m_AllEnemies
                .Where(e => Mathf.Abs(e.Level - level) <= GameConstants.Enemy.LEVEL_VARIANCE)
                .ToList();

            if (suitableEnemies.Count == 0)
            {
                // 如果没有合适的敌人，生成一个
                return EnemyData.CreateRandomEnemy(level);
            }

            // 随机选择一个
            var selectedEnemy = suitableEnemies[Random.Range(0, suitableEnemies.Count)];
            
            // 创建副本并调整等级
            var enemyCopy = new EnemyData(selectedEnemy.Name, level, selectedEnemy.EnemyType);
            return enemyCopy;
        }

        #endregion

        #region 消耗品数据接口实现

        public List<ConsumableData> GetAllConsumables()
        {
            return new List<ConsumableData>(m_AllConsumables);
        }

        public ConsumableData GetConsumableById(string consumableId)
        {
            return m_ConsumableCache.TryGetValue(consumableId, out ConsumableData consumable) ? consumable : null;
        }

        public ConsumableData CreateConsumableFromTemplate(string templateId, ItemRarity rarity, int level)
        {
            var template = Resources.Load<ConsumableTemplate>($"Data/ConsumableTemplates/{templateId}");
            if (template == null)
            {
                m_LoggingService?.Log($"找不到消耗品模板: {templateId}", LogLevel.Warning, "GameDataService");
                return null;
            }

            var consumables = GenerateConsumablesFromTemplate(template, rarity, level, 1);
            return consumables.Count > 0 ? consumables[0] : null;
        }

        #endregion

        #region 模板生成方法

        /// <summary>
        /// 从物品模板生成物品
        /// </summary>
        private List<ItemData> GenerateItemsFromTemplate(ItemTemplate template, ItemRarity? forceRarity = null, int? forceLevel = null, int count = 1)
        {
            var items = new List<ItemData>();

            for (int i = 0; i < count; i++)
            {
                var rarity = forceRarity ?? template.GetRandomRarity();
                var level = forceLevel ?? Random.Range(template.MinLevel, template.MaxLevel + 1);

                var item = new ItemData
                {
                    ItemId = System.Guid.NewGuid().ToString(),
                    Name = template.BaseName,
                    Description = template.Description,
                    Rarity = rarity,
                    Level = level,
                    Value = CalculateItemValue(template.BaseValue, rarity, level),
                    Icon = template.Icon,
                    ItemType = template.ItemType,
                    StackSize = 1
                };

                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// 从装备模板生成装备
        /// </summary>
        private List<EquipmentData> GenerateEquipmentsFromTemplate(EquipmentTemplate template, EquipmentType? forceType = null, ItemRarity? forceRarity = null, int? forceLevel = null, int count = 1)
        {
            var equipments = new List<EquipmentData>();

            for (int i = 0; i < count; i++)
            {
                var type = forceType ?? template.EquipmentType;
                var rarity = forceRarity ?? template.GetRandomRarity();
                var level = forceLevel ?? Random.Range(template.MinLevel, template.MaxLevel + 1);

                var equipment = EquipmentData.GenerateRandomEquipment(type, rarity, level);
                if (equipment != null)
                {
                    equipment.Name = template.BaseName;
                    equipment.Description = template.Description;
                    equipment.Icon = template.Icon;
                    equipments.Add(equipment);
                }
            }

            return equipments;
        }

        /// <summary>
        /// 从消耗品模板生成消耗品
        /// </summary>
        private List<ConsumableData> GenerateConsumablesFromTemplate(ConsumableTemplate template, ItemRarity? forceRarity = null, int? forceLevel = null, int count = 1)
        {
            var consumables = new List<ConsumableData>();

            for (int i = 0; i < count; i++)
            {
                var rarity = forceRarity ?? template.GetRandomRarity();
                var level = forceLevel ?? Random.Range(template.MinLevel, template.MaxLevel + 1);

                var consumable = new ConsumableData
                {
                    ItemId = System.Guid.NewGuid().ToString(),
                    Name = template.BaseName,
                    Description = template.Description,
                    Rarity = rarity,
                    Level = level,
                    Value = CalculateItemValue(template.BaseValue, rarity, level),
                    Icon = template.Icon,
                    ItemType = ItemType.Consumable,
                    StackSize = template.MaxStackSize
                };

                // 设置消耗品特有属性
                consumable.SetConsumableInfo(
                    template.EffectType,
                    CalculateEffectPower(template.BaseEffectPower, rarity, level),
                    template.EffectDuration,
                    template.Cooldown
                );

                consumables.Add(consumable);
            }

            return consumables;
        }

        #endregion

        #region 默认数据创建

        /// <summary>
        /// 创建默认敌人
        /// </summary>
        private void CreateDefaultEnemies()
        {
            // 为每个等级范围创建一些基础敌人
            for (int level = 1; level <= 50; level += 5)
            {
                // 为每种敌人类型创建敌人
                foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
                {
                    if (type == EnemyType.Random) continue;

                    var enemy = EnemyData.CreateRandomEnemy(level, type);
                    m_AllEnemies.Add(enemy);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算物品价值
        /// </summary>
        private int CalculateItemValue(int baseValue, ItemRarity rarity, int level)
        {
            float rarityMultiplier = GameConstants.GetRarityValueMultiplier(rarity);
            float levelMultiplier = 1f + level * GameConstants.Items.LEVEL_VALUE_MULTIPLIER;
            
            return Mathf.RoundToInt(baseValue * rarityMultiplier * levelMultiplier);
        }

        /// <summary>
        /// 计算效果强度
        /// </summary>
        private float CalculateEffectPower(float basePower, ItemRarity rarity, int level)
        {
            float rarityMultiplier = GameConstants.GetRarityValueMultiplier(rarity);
            float levelMultiplier = 1f + level * 0.05f; // 每级5%增长
            
            return basePower * rarityMultiplier * levelMultiplier;
        }

        #endregion

        #region 数据管理

        /// <summary>
        /// 添加物品到数据库
        /// </summary>
        public void AddItem(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemId)) return;

            if (!m_ItemCache.ContainsKey(item.ItemId))
            {
                m_AllItems.Add(item);
                m_ItemCache[item.ItemId] = item;

                if (item is EquipmentData equipment)
                {
                    m_AllEquipments.Add(equipment);
                    m_EquipmentCache[equipment.ItemId] = equipment;
                }
                else if (item is ConsumableData consumable)
                {
                    m_AllConsumables.Add(consumable);
                    m_ConsumableCache[consumable.ItemId] = consumable;
                }
            }
        }

        /// <summary>
        /// 添加敌人到数据库
        /// </summary>
        public void AddEnemy(EnemyData enemy)
        {
            if (enemy == null || string.IsNullOrEmpty(enemy.EnemyId)) return;

            if (!m_EnemyCache.ContainsKey(enemy.EnemyId))
            {
                m_AllEnemies.Add(enemy);
                m_EnemyCache[enemy.EnemyId] = enemy;
            }
        }

        /// <summary>
        /// 重新加载数据
        /// </summary>
        public void ReloadData()
        {
            // 清理现有数据
            m_AllItems.Clear();
            m_AllEquipments.Clear();
            m_AllEnemies.Clear();
            m_AllConsumables.Clear();
            
            m_ItemCache.Clear();
            m_EquipmentCache.Clear();
            m_EnemyCache.Clear();
            m_ConsumableCache.Clear();

            // 重新初始化
            InitializeData();
        }

        /// <summary>
        /// 获取数据统计
        /// </summary>
        public Dictionary<string, int> GetDataStatistics()
        {
            return new Dictionary<string, int>
            {
                ["TotalItems"] = m_AllItems.Count,
                ["TotalEquipments"] = m_AllEquipments.Count,
                ["TotalEnemies"] = m_AllEnemies.Count,
                ["TotalConsumables"] = m_AllConsumables.Count,
                ["CachedItems"] = m_ItemCache.Count,
                ["CachedEquipments"] = m_EquipmentCache.Count,
                ["CachedEnemies"] = m_EnemyCache.Count,
                ["CachedConsumables"] = m_ConsumableCache.Count
            };
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器调试方法
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintDataStatistics()
        {
            var stats = GetDataStatistics();
            var message = "=== 游戏数据统计 ===\n";
            
            foreach (var kvp in stats)
            {
                message += $"{kvp.Key}: {kvp.Value}\n";
            }
            
            Debug.Log(message);
        }
#endif
    }
}