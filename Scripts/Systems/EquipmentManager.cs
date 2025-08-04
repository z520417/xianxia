using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 装备管理器
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        #region 事件
        public event Action<EquipmentType, EquipmentData> OnEquipmentChanged;
        public event Action<EquipmentData> OnEquipmentEquipped;
        public event Action<EquipmentData> OnEquipmentUnequipped;
        public event Action OnStatsChanged;
        #endregion

        #region 装备槽位
        [Header("装备槽位")]
        [SerializeField] private Dictionary<EquipmentType, EquipmentData> m_EquippedItems;
        #endregion

        #region 关联系统
        [Header("关联系统")]
        [SerializeField] private CharacterStats m_BaseStats;
        [SerializeField] private CharacterStats m_TotalStats;
        [SerializeField] private InventorySystem m_InventorySystem;
        #endregion

        #region 公共属性
        public CharacterStats BaseStats => m_BaseStats;
        public CharacterStats TotalStats => m_TotalStats;
        public Dictionary<EquipmentType, EquipmentData> EquippedItems => m_EquippedItems;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializeEquipment();
        }

        private void Start()
        {
            FindInventorySystem();
            CalculateTotalStats();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化装备系统
        /// </summary>
        private void InitializeEquipment()
        {
            if (m_EquippedItems == null)
            {
                m_EquippedItems = new Dictionary<EquipmentType, EquipmentData>();
            }

            // 初始化所有装备槽位
            foreach (EquipmentType equipType in Enum.GetValues(typeof(EquipmentType)))
            {
                if (!m_EquippedItems.ContainsKey(equipType))
                {
                    m_EquippedItems[equipType] = null;
                }
            }

            // 初始化基础属性
            if (m_BaseStats == null)
            {
                m_BaseStats = new CharacterStats(1);
            }

            // 初始化总属性
            if (m_TotalStats == null)
            {
                m_TotalStats = new CharacterStats();
            }
        }

        /// <summary>
        /// 查找背包系统
        /// </summary>
        private void FindInventorySystem()
        {
            if (m_InventorySystem == null)
            {
                m_InventorySystem = FindObjectOfType<InventorySystem>();
            }
        }
        #endregion

        #region 装备穿戴方法
        /// <summary>
        /// 穿戴装备
        /// </summary>
        public bool EquipItem(EquipmentData _equipment)
        {
            if (_equipment == null)
            {
                Debug.LogWarning("尝试装备空的装备");
                return false;
            }

            // 检查等级需求
            if (m_BaseStats.Level < _equipment.RequiredLevel)
            {
                Debug.LogWarning($"等级不足，无法装备 {_equipment.FullName}。需要等级：{_equipment.RequiredLevel}，当前等级：{m_BaseStats.Level}");
                return false;
            }

            EquipmentType equipType = _equipment.EquipmentType;

            // 如果已经装备了同类型装备，先卸下
            if (m_EquippedItems[equipType] != null)
            {
                UnequipItem(equipType);
            }

            // 装备新装备
            m_EquippedItems[equipType] = _equipment;

            // 重新计算属性
            CalculateTotalStats();

            // 触发事件
            OnEquipmentEquipped?.Invoke(_equipment);
            OnEquipmentChanged?.Invoke(equipType, _equipment);
            OnStatsChanged?.Invoke();

            Debug.Log($"装备了 {_equipment.FullName}");
            return true;
        }

        /// <summary>
        /// 从背包装备物品
        /// </summary>
        public bool EquipItemFromInventory(int _slotIndex)
        {
            if (m_InventorySystem == null)
            {
                Debug.LogError("未找到背包系统");
                return false;
            }

            ItemSlot slot = m_InventorySystem.GetSlot(_slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                Debug.LogWarning("槽位为空或无效");
                return false;
            }

            if (slot.ItemData is EquipmentData equipment)
            {
                // 尝试装备
                if (EquipItem(equipment))
                {
                    // 从背包移除装备
                    m_InventorySystem.RemoveItemFromSlot(_slotIndex, 1);
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("该物品不是装备");
            }

            return false;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public bool UnequipItem(EquipmentType _equipmentType)
        {
            if (!m_EquippedItems.ContainsKey(_equipmentType) || m_EquippedItems[_equipmentType] == null)
            {
                Debug.LogWarning($"没有装备 {_equipmentType} 类型的物品");
                return false;
            }

            EquipmentData unequippedItem = m_EquippedItems[_equipmentType];

            // 尝试放回背包
            if (m_InventorySystem != null)
            {
                bool addedToInventory = m_InventorySystem.AddItem(unequippedItem, 1);
                if (!addedToInventory)
                {
                    Debug.LogWarning("背包已满，无法卸下装备");
                    return false;
                }
            }

            // 卸下装备
            m_EquippedItems[_equipmentType] = null;

            // 重新计算属性
            CalculateTotalStats();

            // 触发事件
            OnEquipmentUnequipped?.Invoke(unequippedItem);
            OnEquipmentChanged?.Invoke(_equipmentType, null);
            OnStatsChanged?.Invoke();

            Debug.Log($"卸下了 {unequippedItem.FullName}");
            return true;
        }

        /// <summary>
        /// 卸下所有装备
        /// </summary>
        public void UnequipAllItems()
        {
            List<EquipmentType> equippedTypes = new List<EquipmentType>();
            
            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    equippedTypes.Add(kvp.Key);
                }
            }

            foreach (EquipmentType equipType in equippedTypes)
            {
                UnequipItem(equipType);
            }

            Debug.Log("卸下了所有装备");
        }
        #endregion

        #region 属性计算方法
        /// <summary>
        /// 计算总属性
        /// </summary>
        private void CalculateTotalStats()
        {
            // 复制基础属性
            m_TotalStats = m_BaseStats.Clone();

            // 添加所有装备的属性加成
            foreach (var kvp in m_EquippedItems)
            {
                EquipmentData equipment = kvp.Value;
                if (equipment != null)
                {
                    m_TotalStats.AddStats(equipment.BonusStats);
                }
            }

            Debug.Log($"属性重新计算完成 - 攻击力: {m_TotalStats.Attack}, 防御力: {m_TotalStats.Defense}");
        }

        /// <summary>
        /// 更新基础属性
        /// </summary>
        public void UpdateBaseStats(CharacterStats _newBaseStats)
        {
            m_BaseStats = _newBaseStats.Clone();
            CalculateTotalStats();
            OnStatsChanged?.Invoke();
        }
        #endregion

        #region 查询方法
        /// <summary>
        /// 获取指定类型的装备
        /// </summary>
        public EquipmentData GetEquippedItem(EquipmentType _equipmentType)
        {
            return m_EquippedItems.ContainsKey(_equipmentType) ? m_EquippedItems[_equipmentType] : null;
        }

        /// <summary>
        /// 检查是否装备了指定类型的装备
        /// </summary>
        public bool IsEquipped(EquipmentType _equipmentType)
        {
            return GetEquippedItem(_equipmentType) != null;
        }

        /// <summary>
        /// 获取所有已装备的物品
        /// </summary>
        public List<EquipmentData> GetAllEquippedItems()
        {
            List<EquipmentData> equippedItems = new List<EquipmentData>();
            
            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    equippedItems.Add(kvp.Value);
                }
            }

            return equippedItems;
        }

        /// <summary>
        /// 获取装备总价值
        /// </summary>
        public int GetTotalEquipmentValue()
        {
            int totalValue = 0;
            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    totalValue += kvp.Value.Value;
                }
            }
            return totalValue;
        }

        /// <summary>
        /// 获取装备属性加成总和
        /// </summary>
        public CharacterStats GetTotalEquipmentBonus()
        {
            CharacterStats totalBonus = new CharacterStats();

            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    totalBonus.AddStats(kvp.Value.BonusStats);
                }
            }

            return totalBonus;
        }
        #endregion

        #region 保存和加载方法
        /// <summary>
        /// 保存装备数据
        /// </summary>
        public string SaveEquipmentData()
        {
            // 这里应该实现装备数据的序列化
            // 简化版本，仅作示例
            List<string> equippedItemNames = new List<string>();
            
            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    equippedItemNames.Add($"{kvp.Key}:{kvp.Value.ItemName}");
                }
            }

            return string.Join(";", equippedItemNames);
        }

        /// <summary>
        /// 加载装备数据
        /// </summary>
        public void LoadEquipmentData(string _data)
        {
            // 这里应该实现装备数据的反序列化
            // 简化版本，仅作示例
            if (string.IsNullOrEmpty(_data)) return;

            string[] equipmentData = _data.Split(';');
            
            foreach (string data in equipmentData)
            {
                string[] parts = data.Split(':');
                if (parts.Length == 2)
                {
                    if (Enum.TryParse<EquipmentType>(parts[0], out EquipmentType equipType))
                    {
                        // 这里应该根据名称查找并装备对应的装备
                        Debug.Log($"加载装备：{equipType} - {parts[1]}");
                    }
                }
            }
        }
        #endregion

        #region 调试方法
#if UNITY_EDITOR
        [ContextMenu("打印装备信息")]
        private void PrintEquipmentInfo()
        {
            Debug.Log("当前装备：");
            
            foreach (var kvp in m_EquippedItems)
            {
                if (kvp.Value != null)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value.FullName}");
                }
                else
                {
                    Debug.Log($"{kvp.Key}: 未装备");
                }
            }

            Debug.Log($"总攻击力: {m_TotalStats.Attack}, 总防御力: {m_TotalStats.Defense}");
        }

        [ContextMenu("测试装备一套装备")]
        private void TestEquipRandomGear()
        {
            RandomItemGenerator itemGenerator = RandomItemGenerator.Instance;
            if (itemGenerator == null)
            {
                Debug.LogError("未找到随机物品生成器");
                return;
            }

            foreach (EquipmentType equipType in Enum.GetValues(typeof(EquipmentType)))
            {
                EquipmentData randomEquipment = EquipmentData.GenerateRandomEquipment(
                    equipType, ItemRarity.Rare, m_BaseStats.Level);
                EquipItem(randomEquipment);
            }

            Debug.Log("装备了一套随机装备");
        }
#endif
        #endregion
    }
}