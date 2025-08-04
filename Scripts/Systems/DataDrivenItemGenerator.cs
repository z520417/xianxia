using System.Collections.Generic;
using UnityEngine;
using XianXiaGame.Core;

namespace XianXiaGame
{
    /// <summary>
    /// 数据驱动的物品生成器
    /// 使用ScriptableObject模板来生成物品，替代硬编码的生成逻辑
    /// </summary>
    public class DataDrivenItemGenerator : MonoBehaviour
    {
        private static DataDrivenItemGenerator s_Instance;
        public static DataDrivenItemGenerator Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<DataDrivenItemGenerator>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("DataDrivenItemGenerator");
                        s_Instance = go.AddComponent<DataDrivenItemGenerator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        [Header("物品模板配置")]
        [SerializeField] private ItemGeneratorConfig m_GeneratorConfig;
        
        [Header("缓存配置")]
        [SerializeField] private bool m_EnableCaching = true;
        [SerializeField] private int m_MaxCacheSize = 100;

        // 模板缓存
        private Dictionary<ItemType, List<ItemTemplate>> m_TemplateCache = new Dictionary<ItemType, List<ItemTemplate>>();
        private Dictionary<EquipmentType, List<EquipmentTemplate>> m_EquipmentTemplateCache = new Dictionary<EquipmentType, List<EquipmentTemplate>>();
        
        // 生成历史（用于调试和统计）
        private Queue<ItemGenerationRecord> m_GenerationHistory = new Queue<ItemGenerationRecord>();

        // 服务引用
        private ILoggingService m_LoggingService;
        private IConfigService m_ConfigService;

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGenerator();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        #region 初始化

        /// <summary>
        /// 初始化生成器
        /// </summary>
        private void InitializeGenerator()
        {
            // 获取服务引用
            m_LoggingService = GameServiceBootstrapper.GetService<ILoggingService>();
            m_ConfigService = GameServiceBootstrapper.GetService<IConfigService>();

            // 加载配置
            LoadGeneratorConfig();
            
            // 构建模板缓存
            BuildTemplateCache();

            m_LoggingService?.Log("数据驱动物品生成器初始化完成", LogLevel.Info, "ItemGenerator");
        }

        /// <summary>
        /// 加载生成器配置
        /// </summary>
        private void LoadGeneratorConfig()
        {
            if (m_GeneratorConfig == null)
            {
                m_GeneratorConfig = Resources.Load<ItemGeneratorConfig>("Config/ItemGeneratorConfig");
                
                if (m_GeneratorConfig == null)
                {
                    m_LoggingService?.Log("未找到ItemGeneratorConfig，使用默认配置", LogLevel.Warning, "ItemGenerator");
                    CreateDefaultConfig();
                }
            }
        }

        /// <summary>
        /// 构建模板缓存
        /// </summary>
        private void BuildTemplateCache()
        {
            if (m_GeneratorConfig == null) return;

            // 清空缓存
            m_TemplateCache.Clear();
            m_EquipmentTemplateCache.Clear();

            // 缓存物品模板
            foreach (var template in m_GeneratorConfig.AllItemTemplates)
            {
                if (!m_TemplateCache.ContainsKey(template.ItemType))
                {
                    m_TemplateCache[template.ItemType] = new List<ItemTemplate>();
                }
                m_TemplateCache[template.ItemType].Add(template);
            }

            // 缓存装备模板
            foreach (var template in m_GeneratorConfig.EquipmentTemplates)
            {
                if (!m_EquipmentTemplateCache.ContainsKey(template.EquipmentType))
                {
                    m_EquipmentTemplateCache[template.EquipmentType] = new List<EquipmentTemplate>();
                }
                m_EquipmentTemplateCache[template.EquipmentType].Add(template);
            }

            m_LoggingService?.Log($"模板缓存构建完成 - 物品模板: {m_GeneratorConfig.AllItemTemplates.Count}, " +
                                $"装备模板: {m_GeneratorConfig.EquipmentTemplates.Count}", LogLevel.Debug, "ItemGenerator");
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private void CreateDefaultConfig()
        {
#if UNITY_EDITOR
            m_GeneratorConfig = ScriptableObject.CreateInstance<ItemGeneratorConfig>();
            
            string assetPath = "Assets/Resources/Config/ItemGeneratorConfig.asset";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(assetPath));
            UnityEditor.AssetDatabase.CreateAsset(m_GeneratorConfig, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            
            m_LoggingService?.Log("已创建默认ItemGeneratorConfig", LogLevel.Info, "ItemGenerator");
#endif
        }

        #endregion

        #region 主要生成方法

        /// <summary>
        /// 生成随机物品
        /// </summary>
        public ItemData GenerateRandomItem(int playerLevel, float luckModifier = 0f, ItemType? forceType = null)
        {
            if (m_GeneratorConfig == null)
            {
                m_LoggingService?.Log("配置未加载，无法生成物品", LogLevel.Error, "ItemGenerator");
                return null;
            }

            try
            {
                // 确定物品类型
                ItemType itemType = forceType ?? SelectRandomItemType(luckModifier);
                
                // 确定稀有度
                ItemRarity rarity = SelectRandomRarity(playerLevel, luckModifier);
                
                // 生成物品
                ItemData item = GenerateItemOfType(itemType, rarity, playerLevel);
                
                // 记录生成历史
                RecordGeneration(item, playerLevel, luckModifier);
                
                return item;
            }
            catch (System.Exception e)
            {
                m_LoggingService?.LogException(e, "ItemGenerator");
                return null;
            }
        }

        /// <summary>
        /// 生成指定类型的装备
        /// </summary>
        public EquipmentData GenerateEquipment(EquipmentType equipmentType, ItemRarity rarity, int level)
        {
            var templates = GetEquipmentTemplates(equipmentType);
            if (templates.Count == 0)
            {
                m_LoggingService?.Log($"未找到装备类型 {equipmentType} 的模板", LogLevel.Warning, "ItemGenerator");
                return EquipmentData.GenerateRandomEquipment(equipmentType, rarity, level); // 回退到原始方法
            }

            var template = SelectRandomTemplate(templates, level);
            if (template != null)
            {
                var equipment = template.CreateItem(rarity, level) as EquipmentData;
                RecordGeneration(equipment, level, 0f);
                return equipment;
            }

            return EquipmentData.GenerateRandomEquipment(equipmentType, rarity, level);
        }

        /// <summary>
        /// 生成消耗品
        /// </summary>
        public ConsumableData GenerateConsumable(ItemRarity rarity, int level)
        {
            var templates = GetConsumableTemplates();
            if (templates.Count == 0)
            {
                m_LoggingService?.Log("未找到消耗品模板", LogLevel.Warning, "ItemGenerator");
                return ConsumableData.GenerateRandomConsumable(rarity, level); // 回退到原始方法
            }

            var template = SelectRandomTemplate(templates, level);
            if (template != null)
            {
                var consumable = template.CreateItem(rarity, level) as ConsumableData;
                RecordGeneration(consumable, level, 0f);
                return consumable;
            }

            return ConsumableData.GenerateRandomConsumable(rarity, level);
        }

        /// <summary>
        /// 批量生成物品
        /// </summary>
        public List<ItemData> GenerateItems(int count, int playerLevel, float luckModifier = 0f)
        {
            var items = new List<ItemData>();
            
            for (int i = 0; i < count; i++)
            {
                var item = GenerateRandomItem(playerLevel, luckModifier);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            
            return items;
        }

        /// <summary>
        /// 挖宝生成物品（高品质概率）
        /// </summary>
        public List<ItemData> DigTreasure(int playerLevel, float luckModifier = 0f)
        {
            var treasureItems = new List<ItemData>();
            
            // 挖宝获得1-3个物品
            int itemCount = Random.Range(1, 4);
            
            // 增加运气影响
            float enhancedLuck = luckModifier * 1.5f; // 挖宝时运气效果更强
            
            for (int i = 0; i < itemCount; i++)
            {
                // 挖宝时有更高概率获得装备和珍宝
                ItemType itemType = SelectTreasureItemType();
                ItemRarity rarity = SelectRandomRarity(playerLevel, enhancedLuck);
                
                var item = GenerateItemOfType(itemType, rarity, playerLevel);
                if (item != null)
                {
                    treasureItems.Add(item);
                }
            }
            
            m_LoggingService?.Log($"挖宝获得 {treasureItems.Count} 个物品", LogLevel.Debug, "ItemGenerator");
            return treasureItems;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 选择随机物品类型
        /// </summary>
        private ItemType SelectRandomItemType(float luckModifier)
        {
            if (m_GeneratorConfig?.ItemTypeWeights == null || m_GeneratorConfig.ItemTypeWeights.Count == 0)
            {
                // 默认权重
                var weights = new float[] { 40f, 35f, 20f, 5f }; // Equipment, Consumable, Material, Treasure
                return (ItemType)GetWeightedRandomIndex(weights);
            }

            var typeWeights = new List<float>();
            foreach (var weight in m_GeneratorConfig.ItemTypeWeights)
            {
                float adjustedWeight = weight.Weight;
                
                // 运气可能影响某些类型的权重
                if (weight.ItemType == ItemType.Treasure)
                {
                    adjustedWeight += luckModifier * 0.1f;
                }
                
                typeWeights.Add(adjustedWeight);
            }

            int selectedIndex = GetWeightedRandomIndex(typeWeights.ToArray());
            return m_GeneratorConfig.ItemTypeWeights[selectedIndex].ItemType;
        }

        /// <summary>
        /// 选择挖宝时的物品类型
        /// </summary>
        private ItemType SelectTreasureItemType()
        {
            var treasureWeights = new float[] { 50f, 20f, 15f, 15f }; // Equipment, Consumable, Material, Treasure
            return (ItemType)GetWeightedRandomIndex(treasureWeights);
        }

        /// <summary>
        /// 选择随机稀有度
        /// </summary>
        private ItemRarity SelectRandomRarity(int playerLevel, float luckModifier)
        {
            if (m_GeneratorConfig?.RarityWeights == null || m_GeneratorConfig.RarityWeights.Count == 0)
            {
                // 默认稀有度权重
                var weights = new float[] { 50f, 25f, 15f, 7f, 2.5f, 0.5f };
                return (ItemRarity)GetWeightedRandomIndex(weights);
            }

            var rarityWeights = new List<float>();
            foreach (var weight in m_GeneratorConfig.RarityWeights)
            {
                float adjustedWeight = weight.Weight;
                
                // 运气影响稀有度（运气越高，稀有物品概率越大）
                if ((int)weight.Rarity > 0)
                {
                    float luckFactor = 1f + (luckModifier * m_GeneratorConfig.LuckEffect * 0.01f);
                    adjustedWeight *= luckFactor;
                }
                
                rarityWeights.Add(adjustedWeight);
            }

            int selectedIndex = GetWeightedRandomIndex(rarityWeights.ToArray());
            return m_GeneratorConfig.RarityWeights[selectedIndex].Rarity;
        }

        /// <summary>
        /// 生成指定类型的物品
        /// </summary>
        private ItemData GenerateItemOfType(ItemType itemType, ItemRarity rarity, int level)
        {
            switch (itemType)
            {
                case ItemType.Equipment:
                    var equipmentType = (EquipmentType)Random.Range(0, System.Enum.GetValues(typeof(EquipmentType)).Length);
                    return GenerateEquipment(equipmentType, rarity, level);
                    
                case ItemType.Consumable:
                    return GenerateConsumable(rarity, level);
                    
                case ItemType.Material:
                    return GenerateMaterial(rarity, level);
                    
                case ItemType.Treasure:
                    return GenerateTreasure(rarity, level);
                    
                default:
                    return GenerateConsumable(rarity, level);
            }
        }

        /// <summary>
        /// 生成材料
        /// </summary>
        private ItemData GenerateMaterial(ItemRarity rarity, int level)
        {
            var templates = GetMaterialTemplates();
            if (templates.Count == 0)
            {
                // 如果没有模板，使用原始方法
                var material = ScriptableObject.CreateInstance<ItemData>();
                string[] materialNames = { "铁矿石", "灵石碎片", "玄铁", "星辰石", "仙晶", "混沌石" };
                string name = materialNames[Mathf.Min((int)rarity, materialNames.Length - 1)];
                material.SetItemInfo(name, "修炼用材料", ItemType.Material, rarity, 10 * (int)rarity * level, 999);
                return material;
            }

            var template = SelectRandomTemplate(templates, level);
            if (template != null)
            {
                return template.CreateItem(rarity, level);
            }

            return null;
        }

        /// <summary>
        /// 生成珍宝
        /// </summary>
        private ItemData GenerateTreasure(ItemRarity rarity, int level)
        {
            var templates = GetTreasureTemplates();
            if (templates.Count == 0)
            {
                // 如果没有模板，使用原始方法
                var treasure = ScriptableObject.CreateInstance<ItemData>();
                string[] treasureNames = { "古铜币", "银锭", "金块", "夜明珠", "龙珠", "凤凰蛋" };
                string name = treasureNames[Mathf.Min((int)rarity, treasureNames.Length - 1)];
                treasure.SetItemInfo(name, "珍贵的收藏品", ItemType.Treasure, rarity, 100 * (int)rarity * level, 1);
                return treasure;
            }

            var template = SelectRandomTemplate(templates, level);
            if (template != null)
            {
                return template.CreateItem(rarity, level);
            }

            return null;
        }

        /// <summary>
        /// 获取装备模板
        /// </summary>
        private List<EquipmentTemplate> GetEquipmentTemplates(EquipmentType equipmentType)
        {
            if (m_EquipmentTemplateCache.ContainsKey(equipmentType))
            {
                return m_EquipmentTemplateCache[equipmentType];
            }
            return new List<EquipmentTemplate>();
        }

        /// <summary>
        /// 获取消耗品模板
        /// </summary>
        private List<ConsumableTemplate> GetConsumableTemplates()
        {
            var templates = new List<ConsumableTemplate>();
            if (m_TemplateCache.ContainsKey(ItemType.Consumable))
            {
                foreach (var template in m_TemplateCache[ItemType.Consumable])
                {
                    if (template is ConsumableTemplate consumableTemplate)
                    {
                        templates.Add(consumableTemplate);
                    }
                }
            }
            return templates;
        }

        /// <summary>
        /// 获取材料模板
        /// </summary>
        private List<MaterialTemplate> GetMaterialTemplates()
        {
            var templates = new List<MaterialTemplate>();
            if (m_TemplateCache.ContainsKey(ItemType.Material))
            {
                foreach (var template in m_TemplateCache[ItemType.Material])
                {
                    if (template is MaterialTemplate materialTemplate)
                    {
                        templates.Add(materialTemplate);
                    }
                }
            }
            return templates;
        }

        /// <summary>
        /// 获取珍宝模板
        /// </summary>
        private List<TreasureTemplate> GetTreasureTemplates()
        {
            var templates = new List<TreasureTemplate>();
            if (m_TemplateCache.ContainsKey(ItemType.Treasure))
            {
                foreach (var template in m_TemplateCache[ItemType.Treasure])
                {
                    if (template is TreasureTemplate treasureTemplate)
                    {
                        templates.Add(treasureTemplate);
                    }
                }
            }
            return templates;
        }

        /// <summary>
        /// 从模板列表中选择随机模板
        /// </summary>
        private T SelectRandomTemplate<T>(List<T> templates, int level) where T : ItemTemplate
        {
            if (templates.Count == 0) return null;

            // 过滤符合等级要求的模板
            var suitableTemplates = new List<T>();
            foreach (var template in templates)
            {
                if (level >= template.MinLevel && level <= template.MaxLevel)
                {
                    suitableTemplates.Add(template);
                }
            }

            if (suitableTemplates.Count == 0)
            {
                // 如果没有合适等级的模板，返回任意一个
                return templates[Random.Range(0, templates.Count)];
            }

            return suitableTemplates[Random.Range(0, suitableTemplates.Count)];
        }

        /// <summary>
        /// 根据权重获取随机索引
        /// </summary>
        private int GetWeightedRandomIndex(float[] weights)
        {
            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }

            if (totalWeight <= 0f) return 0;

            float randomValue = Random.Range(0f, totalWeight);
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
        /// 记录生成历史
        /// </summary>
        private void RecordGeneration(ItemData item, int level, float luck)
        {
            if (item == null || !m_EnableCaching) return;

            var record = new ItemGenerationRecord
            {
                Item = item,
                GenerationTime = System.DateTime.Now,
                PlayerLevel = level,
                LuckModifier = luck
            };

            m_GenerationHistory.Enqueue(record);

            // 限制历史记录大小
            while (m_GenerationHistory.Count > m_MaxCacheSize)
            {
                m_GenerationHistory.Dequeue();
            }
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取生成统计信息
        /// </summary>
        public Dictionary<ItemRarity, int> GetGenerationStatistics()
        {
            var stats = new Dictionary<ItemRarity, int>();
            
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                stats[rarity] = 0;
            }

            foreach (var record in m_GenerationHistory)
            {
                if (record.Item != null)
                {
                    stats[record.Item.Rarity]++;
                }
            }

            return stats;
        }

        /// <summary>
        /// 清除生成历史
        /// </summary>
        public void ClearGenerationHistory()
        {
            m_GenerationHistory.Clear();
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("测试生成物品")]
        private void TestGenerateItem()
        {
            var item = GenerateRandomItem(10, 50f);
            if (item != null)
            {
                Debug.Log($"生成物品：{item.FullName} ({item.Rarity})");
            }
        }

        [ContextMenu("测试挖宝")]
        private void TestDigTreasure()
        {
            var treasures = DigTreasure(15, 100f);
            Debug.Log($"挖宝获得 {treasures.Count} 个物品：");
            foreach (var item in treasures)
            {
                Debug.Log($"- {item.FullName} ({item.Rarity})");
            }
        }

        [ContextMenu("打印生成统计")]
        private void PrintGenerationStats()
        {
            var stats = GetGenerationStatistics();
            Debug.Log("=== 物品生成统计 ===");
            foreach (var kvp in stats)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} 个");
            }
            Debug.Log($"总计: {m_GenerationHistory.Count} 个物品");
        }

        [ContextMenu("重新构建缓存")]
        private void RebuildCache()
        {
            BuildTemplateCache();
        }
#endif

        #endregion
    }

    /// <summary>
    /// 物品生成记录
    /// </summary>
    [System.Serializable]
    public class ItemGenerationRecord
    {
        public ItemData Item;
        public System.DateTime GenerationTime;
        public int PlayerLevel;
        public float LuckModifier;
    }

    /// <summary>
    /// 物品生成器配置
    /// </summary>
    [CreateAssetMenu(fileName = "ItemGeneratorConfig", menuName = "仙侠游戏/物品生成器配置")]
    public class ItemGeneratorConfig : ScriptableObject
    {
        [Header("模板配置")]
        public List<ItemTemplate> AllItemTemplates = new List<ItemTemplate>();
        public List<EquipmentTemplate> EquipmentTemplates = new List<EquipmentTemplate>();
        
        [Header("权重配置")]
        public List<ItemTypeWeight> ItemTypeWeights = new List<ItemTypeWeight>();
        public List<RarityWeight> RarityWeights = new List<RarityWeight>();
        
        [Header("运气影响")]
        [Range(0f, 1f)]
        public float LuckEffect = 0.1f;

        [System.Serializable]
        public class ItemTypeWeight
        {
            public ItemType ItemType;
            public float Weight = 1f;
        }

        [System.Serializable]
        public class RarityWeight
        {
            public ItemRarity Rarity;
            public float Weight = 1f;
        }
    }
}