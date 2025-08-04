using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace XianXiaGame
{
    /// <summary>
    /// 优化的物品槽位UI组件
    /// 使用对象池和缓存机制提高性能
    /// </summary>
    public class OptimizedItemSlotUI : MonoBehaviour, IPoolable
    {
        [Header("UI组件引用")]
        [SerializeField] private Image m_ItemIcon;
        [SerializeField] private TextMeshProUGUI m_ItemNameText;
        [SerializeField] private TextMeshProUGUI m_QuantityText;
        [SerializeField] private Image m_RarityBackground;
        [SerializeField] private Button m_ClickButton;
        [SerializeField] private GameObject m_EmptySlotIndicator;

        [Header("视觉效果")]
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private float m_EmptySlotAlpha = 0.3f;
        [SerializeField] private float m_FilledSlotAlpha = 1f;

        // 缓存的数据
        private ItemSlot m_CachedSlot;
        private int m_SlotIndex = -1;
        private bool m_IsEmpty = true;
        private string m_CachedItemName = "";
        private int m_CachedQuantity = 0;
        private ItemRarity m_CachedRarity = ItemRarity.Common;

        // 事件
        public event Action<int, ItemSlot> OnSlotClicked;

        // 优化标记
        private bool m_IsDirty = true;
        private bool m_IsInitialized = false;

        public int SlotIndex => m_SlotIndex;
        public ItemSlot CachedSlot => m_CachedSlot;
        public bool IsEmpty => m_IsEmpty;

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            if (m_IsInitialized) return;

            // 确保所有必要组件存在
            if (m_ClickButton == null)
                m_ClickButton = GetComponent<Button>();

            if (m_CanvasGroup == null)
                m_CanvasGroup = GetComponent<CanvasGroup>();

            // 绑定点击事件
            if (m_ClickButton != null)
            {
                m_ClickButton.onClick.RemoveAllListeners();
                m_ClickButton.onClick.AddListener(OnSlotClick);
            }

            m_IsInitialized = true;
        }

        /// <summary>
        /// 设置槽位数据
        /// </summary>
        public void SetSlotData(int slotIndex, ItemSlot itemSlot)
        {
            m_SlotIndex = slotIndex;
            m_CachedSlot = itemSlot;

            // 检查是否需要更新
            bool needsUpdate = CheckIfNeedsUpdate(itemSlot);
            
            if (needsUpdate)
            {
                CacheSlotData(itemSlot);
                m_IsDirty = true;
            }

            if (m_IsDirty)
            {
                UpdateVisuals();
                m_IsDirty = false;
            }
        }

        /// <summary>
        /// 强制刷新槽位
        /// </summary>
        public void ForceRefresh()
        {
            m_IsDirty = true;
            UpdateVisuals();
            m_IsDirty = false;
        }

        /// <summary>
        /// 设置交互状态
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (m_ClickButton != null)
            {
                m_ClickButton.interactable = interactable;
            }

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = interactable ? m_FilledSlotAlpha : m_EmptySlotAlpha;
            }
        }

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            if (m_RarityBackground != null)
            {
                Color backgroundColor = highlighted ? Color.yellow : GetRarityColor(m_CachedRarity);
                m_RarityBackground.color = backgroundColor;
            }
        }

        /// <summary>
        /// 播放获得物品动画
        /// </summary>
        public void PlayItemAddedAnimation()
        {
            if (m_CanvasGroup != null)
            {
                // 简单的缩放动画
                transform.localScale = Vector3.one * 1.2f;
                LeanTween.scale(gameObject, Vector3.one, 0.3f)
                    .setEase(LeanTweenType.easeOutBack);
            }
        }

        private bool CheckIfNeedsUpdate(ItemSlot itemSlot)
        {
            if (itemSlot == null || itemSlot.IsEmpty)
            {
                return !m_IsEmpty;
            }

            return m_IsEmpty ||
                   m_CachedItemName != itemSlot.ItemData.ItemName ||
                   m_CachedQuantity != itemSlot.Quantity ||
                   m_CachedRarity != itemSlot.ItemData.Rarity;
        }

        private void CacheSlotData(ItemSlot itemSlot)
        {
            if (itemSlot == null || itemSlot.IsEmpty)
            {
                m_IsEmpty = true;
                m_CachedItemName = "";
                m_CachedQuantity = 0;
                m_CachedRarity = ItemRarity.Common;
            }
            else
            {
                m_IsEmpty = false;
                m_CachedItemName = itemSlot.ItemData.ItemName;
                m_CachedQuantity = itemSlot.Quantity;
                m_CachedRarity = itemSlot.ItemData.Rarity;
            }
        }

        private void UpdateVisuals()
        {
            if (m_IsEmpty)
            {
                UpdateEmptySlot();
            }
            else
            {
                UpdateFilledSlot();
            }
        }

        private void UpdateEmptySlot()
        {
            // 隐藏物品相关UI
            if (m_ItemIcon != null)
                m_ItemIcon.gameObject.SetActive(false);

            if (m_ItemNameText != null)
                m_ItemNameText.gameObject.SetActive(false);

            if (m_QuantityText != null)
                m_QuantityText.gameObject.SetActive(false);

            // 显示空槽位指示器
            if (m_EmptySlotIndicator != null)
                m_EmptySlotIndicator.SetActive(true);

            // 设置背景色
            if (m_RarityBackground != null)
                m_RarityBackground.color = Color.gray;

            // 设置透明度
            SetInteractable(false);
        }

        private void UpdateFilledSlot()
        {
            // 显示物品相关UI
            if (m_ItemIcon != null)
            {
                m_ItemIcon.gameObject.SetActive(true);
                // 这里可以设置物品图标，如果有的话
                // m_ItemIcon.sprite = m_CachedSlot.ItemData.Icon;
            }

            if (m_ItemNameText != null)
            {
                m_ItemNameText.gameObject.SetActive(true);
                m_ItemNameText.text = m_CachedItemName;
                m_ItemNameText.color = GetRarityColor(m_CachedRarity);
            }

            if (m_QuantityText != null)
            {
                m_QuantityText.gameObject.SetActive(true);
                m_QuantityText.text = m_CachedQuantity > 1 ? $"x{m_CachedQuantity}" : "";
            }

            // 隐藏空槽位指示器
            if (m_EmptySlotIndicator != null)
                m_EmptySlotIndicator.SetActive(false);

            // 设置稀有度背景色
            if (m_RarityBackground != null)
                m_RarityBackground.color = GetRarityColor(m_CachedRarity);

            // 设置交互状态
            SetInteractable(true);
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return new Color(0.6f, 0.3f, 1f); // 紫色
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0f); // 橙色
                case ItemRarity.Mythic: return Color.red;
                default: return Color.white;
            }
        }

        private void OnSlotClick()
        {
            if (m_SlotIndex >= 0)
            {
                OnSlotClicked?.Invoke(m_SlotIndex, m_CachedSlot);
            }
        }

        #region IPoolable Implementation

        public void OnGetFromPool()
        {
            InitializeComponents();
            
            // 重置状态
            m_SlotIndex = -1;
            m_CachedSlot = null;
            m_IsEmpty = true;
            m_IsDirty = true;
            
            // 重置视觉状态
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            
            if (m_CanvasGroup != null)
                m_CanvasGroup.alpha = m_EmptySlotAlpha;
        }

        public void OnReturnToPool()
        {
            // 清理事件监听
            OnSlotClicked = null;
            
            // 停止所有动画
            LeanTween.cancel(gameObject);
            
            // 重置状态
            m_SlotIndex = -1;
            m_CachedSlot = null;
            m_IsEmpty = true;
            m_CachedItemName = "";
            m_CachedQuantity = 0;
            m_CachedRarity = ItemRarity.Common;
            
            gameObject.SetActive(false);
        }

        #endregion

        #region 调试方法

#if UNITY_EDITOR
        [ContextMenu("测试空槽位")]
        private void TestEmptySlot()
        {
            SetSlotData(0, null);
        }

        [ContextMenu("测试物品槽位")]
        private void TestItemSlot()
        {
            // 创建测试物品数据
            var testItem = ScriptableObject.CreateInstance<ItemData>();
            testItem.SetItemInfo("测试物品", "这是一个测试物品", ItemType.Material, ItemRarity.Rare, 100, 99);
            
            var testSlot = new ItemSlot(testItem, 5);
            SetSlotData(0, testSlot);
        }
#endif

        #endregion
    }
}