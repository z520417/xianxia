using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 游戏存档数据结构
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        [Header("基本信息")]
        public string SaveName = "存档";
        public string CreationTime;
        public string LastPlayTime;
        public float PlayTime = 0f;
        public string GameVersion = "1.0.0";

        [Header("玩家数据")]
        public PlayerSaveData PlayerData = new PlayerSaveData();
        
        [Header("背包数据")]
        public InventorySaveData InventoryData = new InventorySaveData();
        
        [Header("装备数据")]
        public EquipmentSaveData EquipmentData = new EquipmentSaveData();
        
        [Header("游戏进度")]
        public GameProgressData ProgressData = new GameProgressData();

        public GameSaveData()
        {
            CreationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LastPlayTime = CreationTime;
        }
    }

    /// <summary>
    /// 玩家存档数据
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerName = "无名道友";
        public int Level = 1;
        public int Experience = 0;
        public int CurrentHealth = 100;
        public int CurrentMana = 50;
        public int Gold = 1000;
        public int Cultivation = 0;
        
        // 统计数据
        public int ExplorationCount = 0;
        public int BattleWins = 0;
        public int TreasuresFound = 0;
    }

    /// <summary>
    /// 背包存档数据
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public int MaxSlots = 50;
        public List<ItemSlotSaveData> Items = new List<ItemSlotSaveData>();
    }

    /// <summary>
    /// 物品槽位存档数据
    /// </summary>
    [Serializable]
    public class ItemSlotSaveData
    {
        public string ItemName;
        public ItemType ItemType;
        public ItemRarity Rarity;
        public int Quantity;
        public string ItemDataJson; // 序列化的物品数据
    }

    /// <summary>
    /// 装备存档数据
    /// </summary>
    [Serializable]
    public class EquipmentSaveData
    {
        public List<EquipmentSlotSaveData> EquippedItems = new List<EquipmentSlotSaveData>();
    }

    /// <summary>
    /// 装备槽位存档数据
    /// </summary>
    [Serializable]
    public class EquipmentSlotSaveData
    {
        public EquipmentType SlotType;
        public string ItemName;
        public ItemRarity Rarity;
        public string ItemDataJson; // 序列化的装备数据
    }

    /// <summary>
    /// 游戏进度存档数据
    /// </summary>
    [Serializable]
    public class GameProgressData
    {
        public GameState CurrentGameState = GameState.MainMenu;
        public bool IsGameInitialized = false;
        
        // 可扩展的进度数据
        public Dictionary<string, object> CustomProgressData = new Dictionary<string, object>();
    }

    /// <summary>
    /// 存档系统管理器
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private static SaveSystem s_Instance;
        public static SaveSystem Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<SaveSystem>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("SaveSystem");
                        s_Instance = go.AddComponent<SaveSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        [Header("存档配置")]
        [SerializeField] private int m_MaxSaveSlots = 10;
        [SerializeField] private bool m_UseEncryption = false;
        [SerializeField] private bool m_AutoSave = true;
        [SerializeField] private float m_AutoSaveInterval = 300f; // 5分钟

        private string m_SaveDirectory;
        private float m_LastAutoSaveTime;
        private GameSaveData m_CurrentSaveData;

        // 事件
        public event Action<GameSaveData> OnGameSaved;
        public event Action<GameSaveData> OnGameLoaded;
        public event Action<string> OnSaveError;

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // 自动存档
            if (m_AutoSave && Time.time - m_LastAutoSaveTime > m_AutoSaveInterval)
            {
                AutoSave();
            }
        }

        /// <summary>
        /// 初始化存档系统
        /// </summary>
        private void InitializeSaveSystem()
        {
            // 设置存档目录
            m_SaveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            
            // 确保存档目录存在
            if (!Directory.Exists(m_SaveDirectory))
            {
                Directory.CreateDirectory(m_SaveDirectory);
            }

            Debug.Log($"存档系统初始化完成，存档目录: {m_SaveDirectory}");
        }

        /// <summary>
        /// 创建当前游戏状态的存档数据
        /// </summary>
        public GameSaveData CreateSaveData()
        {
            GameSaveData saveData = new GameSaveData();
            
            try
            {
                var gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    throw new InvalidOperationException("GameManager未找到");
                }

                // 保存玩家数据
                SavePlayerData(saveData.PlayerData, gameManager);
                
                // 保存背包数据
                SaveInventoryData(saveData.InventoryData, gameManager.InventorySystem);
                
                // 保存装备数据
                SaveEquipmentData(saveData.EquipmentData, gameManager.EquipmentManager);
                
                // 保存游戏进度
                SaveProgressData(saveData.ProgressData, gameManager);

                saveData.LastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                m_CurrentSaveData = saveData;
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"创建存档数据失败: {e.Message}");
                OnSaveError?.Invoke($"创建存档数据失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存游戏到指定槽位
        /// </summary>
        public bool SaveGame(int slotIndex, string saveName = "")
        {
            if (slotIndex < 0 || slotIndex >= m_MaxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位: {slotIndex}");
                return false;
            }

            try
            {
                GameSaveData saveData = CreateSaveData();
                if (saveData == null) return false;

                if (!string.IsNullOrEmpty(saveName))
                {
                    saveData.SaveName = saveName;
                }

                string filePath = GetSaveFilePath(slotIndex);
                string jsonData = JsonUtility.ToJson(saveData, true);

                if (m_UseEncryption)
                {
                    jsonData = EncryptSaveData(jsonData);
                }

                File.WriteAllText(filePath, jsonData);
                
                OnGameSaved?.Invoke(saveData);
                
                // 触发事件
                EventSystem.Instance?.TriggerEvent(GameEventType.GameSaved, 
                    new GameMessageEventData($"游戏已保存到槽位 {slotIndex + 1}", MessageType.Success));
                
                Debug.Log($"游戏保存成功: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存游戏失败: {e.Message}");
                OnSaveError?.Invoke($"保存游戏失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从指定槽位加载游戏
        /// </summary>
        public bool LoadGame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_MaxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位: {slotIndex}");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(slotIndex);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"存档文件不存在: {filePath}");
                    return false;
                }

                string jsonData = File.ReadAllText(filePath);

                if (m_UseEncryption)
                {
                    jsonData = DecryptSaveData(jsonData);
                }

                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                
                if (saveData == null)
                {
                    throw new InvalidOperationException("存档数据解析失败");
                }

                // 应用存档数据到游戏
                ApplySaveData(saveData);
                
                m_CurrentSaveData = saveData;
                OnGameLoaded?.Invoke(saveData);
                
                // 触发事件
                EventSystem.Instance?.TriggerEvent(GameEventType.GameLoaded,
                    new GameMessageEventData($"从槽位 {slotIndex + 1} 加载游戏成功", MessageType.Success));
                
                Debug.Log($"游戏加载成功: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载游戏失败: {e.Message}");
                OnSaveError?.Invoke($"加载游戏失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有存档信息
        /// </summary>
        public List<GameSaveData> GetAllSaveInfo()
        {
            List<GameSaveData> saveInfoList = new List<GameSaveData>();

            for (int i = 0; i < m_MaxSaveSlots; i++)
            {
                string filePath = GetSaveFilePath(i);
                
                if (File.Exists(filePath))
                {
                    try
                    {
                        string jsonData = File.ReadAllText(filePath);
                        
                        if (m_UseEncryption)
                        {
                            jsonData = DecryptSaveData(jsonData);
                        }

                        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                        saveInfoList.Add(saveData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"读取存档信息失败 {i}: {e.Message}");
                        saveInfoList.Add(null);
                    }
                }
                else
                {
                    saveInfoList.Add(null);
                }
            }

            return saveInfoList;
        }

        /// <summary>
        /// 删除指定槽位的存档
        /// </summary>
        public bool DeleteSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_MaxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位: {slotIndex}");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(slotIndex);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"删除存档成功: 槽位 {slotIndex}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"存档文件不存在: 槽位 {slotIndex}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"删除存档失败: {e.Message}");
                OnSaveError?.Invoke($"删除存档失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 自动存档
        /// </summary>
        private void AutoSave()
        {
            const int AUTO_SAVE_SLOT = 0; // 槽位0用于自动存档
            
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MainMenu)
            {
                SaveGame(AUTO_SAVE_SLOT, "自动存档");
                m_LastAutoSaveTime = Time.time;
            }
        }

        #region 私有辅助方法

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(m_SaveDirectory, $"save_{slotIndex:D2}.json");
        }

        private void SavePlayerData(PlayerSaveData playerData, GameManager gameManager)
        {
            playerData.PlayerName = gameManager.PlayerName;
            playerData.Level = gameManager.PlayerStats.Level;
            playerData.Experience = gameManager.PlayerStats.Experience;
            playerData.CurrentHealth = gameManager.PlayerStats.CurrentHealth;
            playerData.CurrentMana = gameManager.PlayerStats.CurrentMana;
            playerData.Gold = gameManager.Gold;
            playerData.Cultivation = gameManager.PlayerStats.Cultivation;
            playerData.ExplorationCount = gameManager.ExplorationCount;
            playerData.BattleWins = gameManager.BattleWins;
            playerData.TreasuresFound = gameManager.TreasuresFound;
        }

        private void SaveInventoryData(InventorySaveData inventoryData, InventorySystem inventory)
        {
            if (inventory == null) return;

            inventoryData.MaxSlots = inventory.MaxSlots;
            inventoryData.Items.Clear();

            foreach (var slot in inventory.ItemSlots)
            {
                if (!slot.IsEmpty)
                {
                    var itemSaveData = new ItemSlotSaveData
                    {
                        ItemName = slot.ItemData.ItemName,
                        ItemType = slot.ItemData.ItemType,
                        Rarity = slot.ItemData.Rarity,
                        Quantity = slot.Quantity,
                        ItemDataJson = JsonUtility.ToJson(slot.ItemData)
                    };
                    inventoryData.Items.Add(itemSaveData);
                }
            }
        }

        private void SaveEquipmentData(EquipmentSaveData equipmentData, EquipmentManager equipmentManager)
        {
            if (equipmentManager == null) return;

            equipmentData.EquippedItems.Clear();

            foreach (var kvp in equipmentManager.EquippedItems)
            {
                if (kvp.Value != null)
                {
                    var equipSaveData = new EquipmentSlotSaveData
                    {
                        SlotType = kvp.Key,
                        ItemName = kvp.Value.ItemName,
                        Rarity = kvp.Value.Rarity,
                        ItemDataJson = JsonUtility.ToJson(kvp.Value)
                    };
                    equipmentData.EquippedItems.Add(equipSaveData);
                }
            }
        }

        private void SaveProgressData(GameProgressData progressData, GameManager gameManager)
        {
            progressData.CurrentGameState = gameManager.CurrentState;
            progressData.IsGameInitialized = true;
        }

        private void ApplySaveData(GameSaveData saveData)
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            // TODO: 实现从存档数据恢复游戏状态
            // 这里需要根据具体的游戏管理器API来实现
            Debug.Log("应用存档数据到游戏 - 待实现");
        }

        private string EncryptSaveData(string data)
        {
            // 简单的Base64编码作为示例，实际项目中应使用更安全的加密方法
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            return System.Convert.ToBase64String(bytes);
        }

        private string DecryptSaveData(string encryptedData)
        {
            try
            {
                byte[] bytes = System.Convert.FromBase64String(encryptedData);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                throw new InvalidOperationException("存档数据解密失败");
            }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("测试保存")]
        private void TestSave()
        {
            SaveGame(9, "测试存档");
        }

        [ContextMenu("测试加载")]
        private void TestLoad()
        {
            LoadGame(9);
        }

        [ContextMenu("清除所有存档")]
        private void ClearAllSaves()
        {
            for (int i = 0; i < m_MaxSaveSlots; i++)
            {
                DeleteSave(i);
            }
            Debug.Log("所有存档已清除");
        }
#endif
    }
}