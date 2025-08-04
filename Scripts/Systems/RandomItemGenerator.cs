using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 随机物品生成器
    /// </summary>
    public class RandomItemGenerator : MonoBehaviour
    {
        #region 单例模式
        private static RandomItemGenerator s_Instance;
        public static RandomItemGenerator Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<RandomItemGenerator>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("RandomItemGenerator");
                        s_Instance = go.AddComponent<RandomItemGenerator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }
        #endregion

        #region 稀有度权重配置
        [Header("稀有度权重配置")]
        [SerializeField] private float[] m_RarityWeights = 
        {
            50f,  // Common (普通)
            25f,  // Uncommon (不凡)
            15f,  // Rare (稀有)
            7f,   // Epic (史诗)
            2.5f, // Legendary (传说)
            0.5f  // Mythic (神话)
        };

        [Header("装备类型权重")]
        [SerializeField] private float[] m_EquipmentTypeWeights = 
        {
            20f,  // Weapon (武器)
            15f,  // Helmet (头盔)
            20f,  // Armor (护甲)
            15f,  // Boots (靴子)
            15f,  // Ring (戒指)
            15f   // Necklace (项链)
        };

        [Header("物品类型权重")]
        [SerializeField] private float[] m_ItemTypeWeights = 
        {
            40f,  // Equipment (装备)
            35f,  // Consumable (消耗品)
            20f,  // Material (材料)
            5f    // Treasure (珍宝)
        };
        #endregion

        #region 运气影响配置
        [Header("运气影响配置")]
        [SerializeField] private float m_LuckEffect = 0.1f;        // 运气对稀有度的影响程度
        [SerializeField] private int m_MaxLuckBonus = 50;          // 最大运气加成
        #endregion

        #region 宝藏配置
        [Header("宝藏配置")]
        [SerializeField] private int m_TreasureBaseValue = 100;    // 宝藏基础价值
        [SerializeField] private string[] m_TreasureNames = 
        {
            "古铜币", "银锭", "金块", "夜明珠", "龙珠", "凤凰蛋"
        };
        [SerializeField] private string[] m_TreasureDescriptions = 
        {
            "古代遗留的铜钱，有一定收藏价值。",
            "纯度很高的银锭，价值不菲。",
            "闪闪发光的金块，让人目眩神迷。",
            "传说中的夜明珠，散发着神秘光芒。",
            "传说中的龙珠，蕴含着强大的力量。",
            "凤凰涅槃时留下的蛋，无价之宝。"
        };
        #endregion

        #region 材料配置
        [Header("材料配置")]
        [SerializeField] private string[] m_MaterialNames = 
        {
            "铁矿石", "灵石碎片", "玄铁", "星辰石", "仙晶", "混沌石"
        };
        [SerializeField] private string[] m_MaterialDescriptions = 
        {
            "普通的铁矿石，可用于锻造。",
            "含有微弱灵气的石头碎片。",
            "稀有的玄铁矿石，异常坚硬。",
            "来自星空的神秘石头。",
            "纯净的仙界水晶，价值连城。",
            "传说中的混沌初开时的原石。"
        };
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
            }
        }
        #endregion

        #region 随机生成方法
        /// <summary>
        /// 生成随机物品
        /// </summary>
        public ItemData GenerateRandomItem(int _playerLevel, int _luckValue = 0)
        {
            // 根据权重选择物品类型
            ItemType itemType = GetRandomItemType();
            
            // 根据运气和等级确定稀有度
            ItemRarity rarity = GetRandomRarity(_luckValue);
            
            // 生成对应类型的物品
            switch (itemType)
            {
                case ItemType.Equipment:
                    return GenerateRandomEquipment(_playerLevel, rarity);
                case ItemType.Consumable:
                    return GenerateRandomConsumable(_playerLevel, rarity);
                case ItemType.Material:
                    return GenerateRandomMaterial(_playerLevel, rarity);
                case ItemType.Treasure:
                    return GenerateRandomTreasure(_playerLevel, rarity);
                default:
                    return GenerateRandomConsumable(_playerLevel, rarity);
            }
        }

        /// <summary>
        /// 生成多个随机物品
        /// </summary>
        public List<ItemData> GenerateRandomItems(int _count, int _playerLevel, int _luckValue = 0)
        {
            List<ItemData> items = new List<ItemData>();
            
            for (int i = 0; i < _count; i++)
            {
                ItemData item = GenerateRandomItem(_playerLevel, _luckValue);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            
            return items;
        }

        /// <summary>
        /// 挖宝生成物品（更高的稀有物品概率）
        /// </summary>
        public List<ItemData> DigTreasure(int _playerLevel, int _luckValue = 0)
        {
            List<ItemData> treasureItems = new List<ItemData>();
            
            // 挖宝有更高的概率获得好物品
            int treasureCount = Random.Range(1, 4); // 1-3个物品
            
            // 增加运气影响
            float luckBonus = Mathf.Min(_luckValue * m_LuckEffect, m_MaxLuckBonus);
            int effectiveLuck = _luckValue + Mathf.RoundToInt(luckBonus);
            
            for (int i = 0; i < treasureCount; i++)
            {
                // 挖宝时装备和珍宝概率更高
                ItemType itemType = GetTreasureItemType();
                ItemRarity rarity = GetRandomRarity(effectiveLuck);
                
                ItemData item = null;
                switch (itemType)
                {
                    case ItemType.Equipment:
                        item = GenerateRandomEquipment(_playerLevel, rarity);
                        break;
                    case ItemType.Treasure:
                        item = GenerateRandomTreasure(_playerLevel, rarity);
                        break;
                    case ItemType.Consumable:
                        item = GenerateRandomConsumable(_playerLevel, rarity);
                        break;
                    default:
                        item = GenerateRandomMaterial(_playerLevel, rarity);
                        break;
                }
                
                if (item != null)
                {
                    treasureItems.Add(item);
                }
            }
            
            return treasureItems;
        }
        #endregion

        #region 私有生成方法
        /// <summary>
        /// 生成随机装备
        /// </summary>
        private ItemData GenerateRandomEquipment(int _playerLevel, ItemRarity _rarity)
        {
            EquipmentType equipmentType = GetRandomEquipmentType();
            return EquipmentData.GenerateRandomEquipment(equipmentType, _rarity, _playerLevel);
        }

        /// <summary>
        /// 生成随机消耗品
        /// </summary>
        private ItemData GenerateRandomConsumable(int _playerLevel, ItemRarity _rarity)
        {
            return ConsumableData.GenerateRandomConsumable(_rarity, _playerLevel);
        }

        /// <summary>
        /// 生成随机材料
        /// </summary>
        private ItemData GenerateRandomMaterial(int _playerLevel, ItemRarity _rarity)
        {
            ItemData material = ScriptableObject.CreateInstance<ItemData>();
            
            // 设置基础属性
            material.m_ItemType = ItemType.Material;
            material.m_Rarity = _rarity;
            
            // 根据稀有度选择名称
            int nameIndex = Mathf.Min((int)_rarity, m_MaterialNames.Length - 1);
            material.m_ItemName = m_MaterialNames[nameIndex];
            material.m_Description = m_MaterialDescriptions[nameIndex];
            
            // 设置价值
            material.m_Value = CalculateMaterialValue(_rarity, _playerLevel);
            
            // 设置堆叠数量
            material.m_MaxStackSize = GetMaterialStackSize(_rarity);
            
            return material;
        }

        /// <summary>
        /// 生成随机珍宝
        /// </summary>
        private ItemData GenerateRandomTreasure(int _playerLevel, ItemRarity _rarity)
        {
            ItemData treasure = ScriptableObject.CreateInstance<ItemData>();
            
            // 设置基础属性
            treasure.m_ItemType = ItemType.Treasure;
            treasure.m_Rarity = _rarity;
            
            // 根据稀有度选择名称
            int nameIndex = Mathf.Min((int)_rarity, m_TreasureNames.Length - 1);
            treasure.m_ItemName = m_TreasureNames[nameIndex];
            treasure.m_Description = m_TreasureDescriptions[nameIndex];
            
            // 设置价值（珍宝价值较高）
            treasure.m_Value = CalculateTreasureValue(_rarity, _playerLevel);
            
            // 珍宝通常不堆叠或少量堆叠
            treasure.m_MaxStackSize = _rarity >= ItemRarity.Rare ? 1 : Random.Range(1, 5);
            
            return treasure;
        }
        #endregion

        #region 权重选择方法
        /// <summary>
        /// 根据权重获取随机物品类型
        /// </summary>
        private ItemType GetRandomItemType()
        {
            return (ItemType)GetWeightedRandomIndex(m_ItemTypeWeights);
        }

        /// <summary>
        /// 获取挖宝时的物品类型（调整权重）
        /// </summary>
        private ItemType GetTreasureItemType()
        {
            float[] treasureWeights = { 50f, 20f, 15f, 15f }; // 装备、消耗品、材料、珍宝
            return (ItemType)GetWeightedRandomIndex(treasureWeights);
        }

        /// <summary>
        /// 根据权重获取随机装备类型
        /// </summary>
        private EquipmentType GetRandomEquipmentType()
        {
            return (EquipmentType)GetWeightedRandomIndex(m_EquipmentTypeWeights);
        }

        /// <summary>
        /// 根据运气获取随机稀有度
        /// </summary>
        private ItemRarity GetRandomRarity(int _luckValue)
        {
            // 复制权重数组以便修改
            float[] adjustedWeights = new float[m_RarityWeights.Length];
            System.Array.Copy(m_RarityWeights, adjustedWeights, m_RarityWeights.Length);
            
            // 根据运气调整权重（运气越高，稀有物品概率越大）
            float luckFactor = 1f + (_luckValue * m_LuckEffect * 0.01f);
            
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                if (i > 0) // 对稀有度大于普通的物品增加权重
                {
                    adjustedWeights[i] *= luckFactor;
                }
            }
            
            return (ItemRarity)GetWeightedRandomIndex(adjustedWeights);
        }

        /// <summary>
        /// 根据权重数组获取随机索引
        /// </summary>
        private int GetWeightedRandomIndex(float[] _weights)
        {
            float totalWeight = 0f;
            foreach (float weight in _weights)
            {
                totalWeight += weight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            for (int i = 0; i < _weights.Length; i++)
            {
                currentWeight += _weights[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }
            
            return _weights.Length - 1; // fallback
        }
        #endregion

        #region 价值计算方法
        /// <summary>
        /// 计算材料价值
        /// </summary>
        private int CalculateMaterialValue(ItemRarity _rarity, int _level)
        {
            int baseValue = 10;
            int rarityMultiplier = 1 + (int)_rarity;
            int levelMultiplier = 1 + _level / 3;
            
            return baseValue * rarityMultiplier * levelMultiplier;
        }

        /// <summary>
        /// 计算珍宝价值
        /// </summary>
        private int CalculateTreasureValue(ItemRarity _rarity, int _level)
        {
            int baseValue = m_TreasureBaseValue;
            int rarityMultiplier = 2 + (int)_rarity * 3;
            int levelMultiplier = 1 + _level / 2;
            
            return baseValue * rarityMultiplier * levelMultiplier;
        }

        /// <summary>
        /// 获取材料堆叠数量
        /// </summary>
        private int GetMaterialStackSize(ItemRarity _rarity)
        {
            switch (_rarity)
            {
                case ItemRarity.Common: return 999;
                case ItemRarity.Uncommon: return 500;
                case ItemRarity.Rare: return 200;
                case ItemRarity.Epic: return 100;
                case ItemRarity.Legendary: return 50;
                case ItemRarity.Mythic: return 10;
                default: return 99;
            }
        }
        #endregion

        #region 调试方法
#if UNITY_EDITOR
        [ContextMenu("测试生成物品")]
        private void TestGenerateItem()
        {
            ItemData testItem = GenerateRandomItem(10, 50);
            if (testItem != null)
            {
                Debug.Log($"生成物品：{testItem.GetDetailedInfo()}");
            }
        }

        [ContextMenu("测试挖宝")]
        private void TestDigTreasure()
        {
            List<ItemData> treasures = DigTreasure(15, 100);
            Debug.Log($"挖宝获得 {treasures.Count} 个物品：");
            foreach (ItemData item in treasures)
            {
                Debug.Log($"- {item.FullName} ({item.Rarity})");
            }
        }
#endif
        #endregion
    }
}