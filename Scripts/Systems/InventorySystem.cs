using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 物品槽位
    /// </summary>
    [Serializable]
    public class ItemSlot
    {
        [SerializeField] private ItemData m_ItemData;
        [SerializeField] private int m_Quantity;

        public ItemData ItemData => m_ItemData;
        public int Quantity => m_Quantity;
        public bool IsEmpty => m_ItemData == null || m_Quantity <= 0;
        public bool IsFull => m_ItemData != null && m_Quantity >= m_ItemData.MaxStackSize;

        public ItemSlot()
        {
            m_ItemData = null;
            m_Quantity = 0;
        }

        public ItemSlot(ItemData _itemData, int _quantity)
        {
            m_ItemData = _itemData;
            m_Quantity = _quantity;
        }

        /// <summary>
        /// 设置物品
        /// </summary>
        public void SetItem(ItemData _itemData, int _quantity)
        {
            m_ItemData = _itemData;
            m_Quantity = _quantity;
        }

        /// <summary>
        /// 添加物品数量
        /// </summary>
        public int AddQuantity(int _amount)
        {
            if (m_ItemData == null) return _amount;

            int maxAdd = m_ItemData.MaxStackSize - m_Quantity;
            int actualAdd = Mathf.Min(_amount, maxAdd);
            
            m_Quantity += actualAdd;
            return _amount - actualAdd;
        }

        /// <summary>
        /// 移除物品数量
        /// </summary>
        public int RemoveQuantity(int _amount)
        {
            int actualRemove = Mathf.Min(_amount, m_Quantity);
            m_Quantity -= actualRemove;
            
            if (m_Quantity <= 0)
            {
                Clear();
            }
            
            return actualRemove;
        }

        /// <summary>
        /// 清空槽位
        /// </summary>
        public void Clear()
        {
            m_ItemData = null;
            m_Quantity = 0;
        }

        /// <summary>
        /// 克隆槽位
        /// </summary>
        public ItemSlot Clone()
        {
            return new ItemSlot(m_ItemData, m_Quantity);
        }
    }

    /// <summary>
    /// 背包系统
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        #region 事件
        public event Action<int> OnInventoryChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        public event Action<ItemData> OnItemUsed;
        #endregion

        #region 背包配置
        [Header("背包配置")]
        [SerializeField] private int m_MaxSlots = 50;              // 最大槽位数量（可被配置覆盖）
        [SerializeField] private List<ItemSlot> m_ItemSlots;       // 物品槽位列表
        #endregion

        #region 公共属性
        public int MaxSlots => m_MaxSlots;
        public int UsedSlots => GetUsedSlotCount();
        public int FreeSlots => m_MaxSlots - UsedSlots;
        public List<ItemSlot> ItemSlots => m_ItemSlots;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            UpdateConfigValues();
            InitializeInventory();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 从配置管理器更新背包参数
        /// </summary>
        private void UpdateConfigValues()
        {
            var config = ConfigManager.Instance?.Config?.Inventory;
            if (config != null)
            {
                m_MaxSlots = config.DefaultMaxSlots;
            }
        }

        /// <summary>
        /// 初始化背包
        /// </summary>
        private void InitializeInventory()
        {
            if (m_ItemSlots == null)
            {
                m_ItemSlots = new List<ItemSlot>();
            }

            // 确保槽位数量正确
            while (m_ItemSlots.Count < m_MaxSlots)
            {
                m_ItemSlots.Add(new ItemSlot());
            }

            // 移除多余的槽位
            while (m_ItemSlots.Count > m_MaxSlots)
            {
                m_ItemSlots.RemoveAt(m_ItemSlots.Count - 1);
            }
        }

        /// <summary>
        /// 升级背包容量
        /// </summary>
        public bool UpgradeInventorySlots(int _additionalSlots)
        {
            var config = ConfigManager.Instance?.Config?.Inventory;
            int maxUpgradeSlots = config?.MaxUpgradeSlots ?? 100;
            
            if (m_MaxSlots + _additionalSlots <= maxUpgradeSlots)
            {
                m_MaxSlots += _additionalSlots;
                
                // 添加新的空槽位
                for (int i = 0; i < _additionalSlots; i++)
                {
                    m_ItemSlots.Add(new ItemSlot());
                }
                
                OnInventoryChanged?.Invoke(-1);
                Debug.Log($"背包容量提升到 {m_MaxSlots} 个槽位");
                return true;
            }
            
            Debug.LogWarning($"背包容量已达到上限 {maxUpgradeSlots}");
            return false;
        }
        #endregion

        #region 物品添加方法
        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public bool AddItem(ItemData _itemData, int _quantity = 1)
        {
            if (_itemData == null || _quantity <= 0) return false;

            int remainingQuantity = _quantity;

            // 先尝试添加到现有的同类物品槽位
            remainingQuantity = TryAddToExistingSlots(_itemData, remainingQuantity);

            // 如果还有剩余，尝试添加到空槽位
            if (remainingQuantity > 0)
            {
                remainingQuantity = TryAddToEmptySlots(_itemData, remainingQuantity);
            }

            // 如果成功添加了部分或全部物品
            int addedQuantity = _quantity - remainingQuantity;
            if (addedQuantity > 0)
            {
                OnItemAdded?.Invoke(_itemData, addedQuantity);
                OnInventoryChanged?.Invoke(-1);
                
                Debug.Log($"添加物品：{_itemData.FullName} x{addedQuantity}");
                
                if (remainingQuantity > 0)
                {
                    Debug.LogWarning($"背包空间不足，剩余 {remainingQuantity} 个 {_itemData.FullName} 未能添加");
                }
                
                return remainingQuantity == 0;
            }

            Debug.LogWarning("背包已满，无法添加物品");
            return false;
        }

        /// <summary>
        /// 批量添加物品
        /// </summary>
        public void AddItems(List<ItemData> _items)
        {
            foreach (ItemData item in _items)
            {
                AddItem(item, 1);
            }
        }

        /// <summary>
        /// 尝试添加到现有槽位
        /// </summary>
        private int TryAddToExistingSlots(ItemData _itemData, int _quantity)
        {
            int remainingQuantity = _quantity;

            for (int i = 0; i < m_ItemSlots.Count && remainingQuantity > 0; i++)
            {
                ItemSlot slot = m_ItemSlots[i];
                
                if (!slot.IsEmpty && slot.ItemData == _itemData && !slot.IsFull)
                {
                    int leftover = slot.AddQuantity(remainingQuantity);
                    remainingQuantity = leftover;
                }
            }

            return remainingQuantity;
        }

        /// <summary>
        /// 尝试添加到空槽位
        /// </summary>
        private int TryAddToEmptySlots(ItemData _itemData, int _quantity)
        {
            int remainingQuantity = _quantity;

            for (int i = 0; i < m_ItemSlots.Count && remainingQuantity > 0; i++)
            {
                ItemSlot slot = m_ItemSlots[i];
                
                if (slot.IsEmpty)
                {
                    int addAmount = Mathf.Min(remainingQuantity, _itemData.MaxStackSize);
                    slot.SetItem(_itemData, addAmount);
                    remainingQuantity -= addAmount;
                }
            }

            return remainingQuantity;
        }
        #endregion

        #region 物品移除方法
        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveItem(ItemData _itemData, int _quantity = 1)
        {
            if (_itemData == null || _quantity <= 0) return false;

            if (!HasItem(_itemData, _quantity))
            {
                Debug.LogWarning($"物品不足：{_itemData.FullName}，需要 {_quantity}，拥有 {GetItemCount(_itemData)}");
                return false;
            }

            int remainingToRemove = _quantity;

            // 从后往前移除，避免索引问题
            for (int i = m_ItemSlots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                ItemSlot slot = m_ItemSlots[i];
                
                if (!slot.IsEmpty && slot.ItemData == _itemData)
                {
                    int removedAmount = slot.RemoveQuantity(remainingToRemove);
                    remainingToRemove -= removedAmount;
                }
            }

            int actualRemoved = _quantity - remainingToRemove;
            if (actualRemoved > 0)
            {
                OnItemRemoved?.Invoke(_itemData, actualRemoved);
                OnInventoryChanged?.Invoke(-1);
                
                Debug.Log($"移除物品：{_itemData.FullName} x{actualRemoved}");
            }

            return remainingToRemove == 0;
        }

        /// <summary>
        /// 移除指定槽位的物品
        /// </summary>
        public bool RemoveItemFromSlot(int _slotIndex, int _quantity = 1)
        {
            if (!IsValidSlotIndex(_slotIndex)) return false;

            ItemSlot slot = m_ItemSlots[_slotIndex];
            if (slot.IsEmpty) return false;

            ItemData itemData = slot.ItemData;
            int removedAmount = slot.RemoveQuantity(_quantity);

            if (removedAmount > 0)
            {
                OnItemRemoved?.Invoke(itemData, removedAmount);
                OnInventoryChanged?.Invoke(_slotIndex);
                
                Debug.Log($"从槽位 {_slotIndex} 移除物品：{itemData.FullName} x{removedAmount}");
                return true;
            }

            return false;
        }
        #endregion

        #region 物品使用方法
        /// <summary>
        /// 使用物品
        /// </summary>
        public bool UseItem(int _slotIndex, CharacterStats _characterStats)
        {
            if (!IsValidSlotIndex(_slotIndex)) return false;

            ItemSlot slot = m_ItemSlots[_slotIndex];
            if (slot.IsEmpty) return false;

            ItemData itemData = slot.ItemData;

            // 尝试使用物品
            bool usedSuccessfully = itemData.UseItem(_characterStats);

            if (usedSuccessfully)
            {
                // 移除一个物品
                slot.RemoveQuantity(1);
                
                OnItemUsed?.Invoke(itemData);
                OnInventoryChanged?.Invoke(_slotIndex);
                
                Debug.Log($"使用物品：{itemData.FullName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 使用物品（按物品数据）
        /// </summary>
        public bool UseItem(ItemData _itemData, CharacterStats _characterStats)
        {
            if (_itemData == null) return false;

            // 查找第一个包含该物品的槽位
            for (int i = 0; i < m_ItemSlots.Count; i++)
            {
                ItemSlot slot = m_ItemSlots[i];
                if (!slot.IsEmpty && slot.ItemData == _itemData)
                {
                    return UseItem(i, _characterStats);
                }
            }

            Debug.LogWarning($"未找到物品：{_itemData.FullName}");
            return false;
        }
        #endregion

        #region 查询方法
        /// <summary>
        /// 检查是否拥有指定数量的物品
        /// </summary>
        public bool HasItem(ItemData _itemData, int _quantity = 1)
        {
            return GetItemCount(_itemData) >= _quantity;
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemCount(ItemData _itemData)
        {
            if (_itemData == null) return 0;

            int totalCount = 0;
            foreach (ItemSlot slot in m_ItemSlots)
            {
                if (!slot.IsEmpty && slot.ItemData == _itemData)
                {
                    totalCount += slot.Quantity;
                }
            }

            return totalCount;
        }

        /// <summary>
        /// 获取已使用的槽位数量
        /// </summary>
        private int GetUsedSlotCount()
        {
            int usedCount = 0;
            foreach (ItemSlot slot in m_ItemSlots)
            {
                if (!slot.IsEmpty)
                {
                    usedCount++;
                }
            }
            return usedCount;
        }

        /// <summary>
        /// 检查槽位索引是否有效
        /// </summary>
        private bool IsValidSlotIndex(int _index)
        {
            return _index >= 0 && _index < m_ItemSlots.Count;
        }

        /// <summary>
        /// 获取指定槽位的物品
        /// </summary>
        public ItemSlot GetSlot(int _index)
        {
            if (IsValidSlotIndex(_index))
            {
                return m_ItemSlots[_index];
            }
            return null;
        }

        /// <summary>
        /// 获取所有非空槽位
        /// </summary>
        public List<ItemSlot> GetNonEmptySlots()
        {
            List<ItemSlot> nonEmptySlots = new List<ItemSlot>();
            foreach (ItemSlot slot in m_ItemSlots)
            {
                if (!slot.IsEmpty)
                {
                    nonEmptySlots.Add(slot);
                }
            }
            return nonEmptySlots;
        }

        /// <summary>
        /// 按稀有度排序物品
        /// </summary>
        public List<ItemSlot> GetItemsSortedByRarity()
        {
            List<ItemSlot> sortedItems = GetNonEmptySlots();
            sortedItems.Sort((a, b) => b.ItemData.Rarity.CompareTo(a.ItemData.Rarity));
            return sortedItems;
        }
        #endregion

        #region 整理方法
        /// <summary>
        /// 整理背包
        /// </summary>
        public void SortInventory()
        {
            // 获取所有物品并按类型和稀有度排序
            List<ItemSlot> allItems = GetNonEmptySlots();
            allItems.Sort((a, b) =>
            {
                // 先按类型排序
                int typeComparison = a.ItemData.ItemType.CompareTo(b.ItemData.ItemType);
                if (typeComparison != 0) return typeComparison;
                
                // 再按稀有度排序（高稀有度在前）
                int rarityComparison = b.ItemData.Rarity.CompareTo(a.ItemData.Rarity);
                if (rarityComparison != 0) return rarityComparison;
                
                // 最后按名称排序
                return string.Compare(a.ItemData.ItemName, b.ItemData.ItemName);
            });

            // 清空所有槽位
            foreach (ItemSlot slot in m_ItemSlots)
            {
                slot.Clear();
            }

            // 重新分配物品
            int slotIndex = 0;
            foreach (ItemSlot originalSlot in allItems)
            {
                if (slotIndex < m_ItemSlots.Count)
                {
                    m_ItemSlots[slotIndex].SetItem(originalSlot.ItemData, originalSlot.Quantity);
                    slotIndex++;
                }
            }

            OnInventoryChanged?.Invoke(-1);
            Debug.Log("背包整理完成");
        }
        #endregion

        #region 调试方法
#if UNITY_EDITOR
        [ContextMenu("打印背包内容")]
        private void PrintInventoryContents()
        {
            Debug.Log($"背包内容 ({UsedSlots}/{MaxSlots})：");
            
            for (int i = 0; i < m_ItemSlots.Count; i++)
            {
                ItemSlot slot = m_ItemSlots[i];
                if (!slot.IsEmpty)
                {
                    Debug.Log($"槽位 {i}: {slot.ItemData.FullName} x{slot.Quantity}");
                }
            }
        }

        [ContextMenu("清空背包")]
        private void ClearInventory()
        {
            foreach (ItemSlot slot in m_ItemSlots)
            {
                slot.Clear();
            }
            OnInventoryChanged?.Invoke(-1);
            Debug.Log("背包已清空");
        }
#endif
        #endregion
    }
}