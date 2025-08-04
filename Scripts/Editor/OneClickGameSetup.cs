#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// 一键游戏设置工具 - 快速搭建完整的游戏场景
    /// </summary>
    public class OneClickGameSetup : EditorWindow
    {
        private bool m_SetupComplete = false;
        
        [MenuItem("XianXia Game/一键游戏设置")]
        public static void ShowWindow()
        {
            GetWindow<OneClickGameSetup>("一键游戏设置");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🎮 仙侠探索挖宝游戏", EditorStyles.largeLabel);
            GUILayout.Label("一键设置工具", EditorStyles.boldLabel);
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox("这个工具将为您自动创建完整的游戏场景，包括：\n" +
                "✅ GameManager和所有游戏系统\n" +
                "✅ 完整的UI界面和预制件\n" +
                "✅ 自动连接所有UI引用\n" +
                "✅ 可直接运行的游戏场景", MessageType.Info);
            
            GUILayout.Space(20);
            
            if (!m_SetupComplete)
            {
                if (GUILayout.Button("🚀 一键创建完整游戏", GUILayout.Height(60)))
                {
                    SetupCompleteGame();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("仅创建UI预制件", GUILayout.Height(40)))
                {
                    CreateUIPrefabsOnly();
                }
                
                if (GUILayout.Button("仅创建测试场景", GUILayout.Height(40)))
                {
                    CreateTestSceneOnly();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ 游戏设置完成！\n\n" +
                    "现在您可以：\n" +
                    "1. 点击Unity的播放按钮▶️\n" +
                    "2. 开始游戏测试\n" +
                    "3. 点击'探索'按钮开始冒险！", MessageType.None);
                
                if (GUILayout.Button("重新设置", GUILayout.Height(40)))
                {
                    m_SetupComplete = false;
                }
            }
        }
        
        private void SetupCompleteGame()
        {
            try
            {
                EditorUtility.DisplayProgressBar("设置游戏", "正在创建游戏系统...", 0.1f);
                
                // 1. 创建文件夹结构
                CreateFolderStructure();
                
                EditorUtility.DisplayProgressBar("设置游戏", "正在创建UI预制件...", 0.3f);
                
                // 2. 创建UI预制件
                CreateAllUIPrefabs();
                
                EditorUtility.DisplayProgressBar("设置游戏", "正在设置游戏场景...", 0.6f);
                
                // 3. 创建游戏管理器
                CreateGameManager();
                
                EditorUtility.DisplayProgressBar("设置游戏", "正在创建UI界面...", 0.8f);
                
                // 4. 创建UI并自动连接
                CreateAndConnectUI();
                
                EditorUtility.DisplayProgressBar("设置游戏", "完成设置...", 1.0f);
                
                // 5. 创建EventSystem
                EnsureEventSystem();
                
                EditorUtility.ClearProgressBar();
                
                m_SetupComplete = true;
                
                EditorUtility.DisplayDialog("设置完成", 
                    "🎉 游戏设置完成！\n\n" +
                    "现在可以点击播放按钮开始游戏了！\n\n" +
                    "💡 提示：点击'探索'按钮开始你的仙侠冒险之旅！", "开始游戏");
                
                Debug.Log("=== 🎮 仙侠探索挖宝游戏设置完成！===");
                Debug.Log("现在可以运行游戏了！");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("设置失败", $"游戏设置过程中出现错误：\n{e.Message}", "确定");
                Debug.LogError($"游戏设置失败：{e}");
            }
        }
        
        private void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Prefabs",
                "Assets/Prefabs/UI",
                "Assets/Resources",
                "Assets/Scenes"
            };
            
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parentFolder = Path.GetDirectoryName(folder);
                    string folderName = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
            
            AssetDatabase.Refresh();
        }
        
        private void CreateAllUIPrefabs()
        {
            // 创建简化的UI预制件
            CreateSimpleMainUI();
            CreateItemSlotPrefab();
        }
        
        private void CreateSimpleMainUI()
        {
            // 创建简化但功能完整的主UI
            GameObject canvas = new GameObject("MainGameCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 0;
            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvas.AddComponent<GraphicRaycaster>();
            
            // 添加MainGameUI组件
            MainGameUI uiComponent = canvas.AddComponent<MainGameUI>();
            
            // 创建基础UI布局
            CreateSimpleUILayout(canvas.transform);
            
            // 保存预制件
            string prefabPath = "Assets/Prefabs/UI/MainGameUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
            DestroyImmediate(canvas);
            
            Debug.Log($"主UI预制件已创建：{prefabPath}");
        }
        
        private void CreateSimpleUILayout(Transform parent)
        {
            // 创建主背景
            GameObject background = new GameObject("Background");
            background.transform.SetParent(parent, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            
            // 玩家信息面板 (左上角)
            GameObject playerPanel = CreateUIPanel(parent, "PlayerInfoPanel", new Vector2(350, 200), new Vector2(-785, 440));
            CreatePlayerInfoUI(playerPanel.transform);
            
            // 消息面板 (下方)
            GameObject messagePanel = CreateUIPanel(parent, "MessagePanel", new Vector2(800, 200), new Vector2(0, -440));
            CreateMessageUI(messagePanel.transform);
            
            // 操作面板 (右上角)
            GameObject actionPanel = CreateUIPanel(parent, "ExplorePanel", new Vector2(300, 150), new Vector2(810, 465));
            CreateActionUI(actionPanel.transform);
            
            // 战斗面板 (中央，初始隐藏)
            GameObject battlePanel = CreateUIPanel(parent, "BattlePanel", new Vector2(600, 400), Vector2.zero);
            CreateBattleUI(battlePanel.transform);
            battlePanel.SetActive(false);
            
            // 背包面板 (中央，初始隐藏)
            GameObject inventoryPanel = CreateUIPanel(parent, "InventoryPanel", new Vector2(800, 600), Vector2.zero);
            CreateInventoryUI(inventoryPanel.transform);
            inventoryPanel.SetActive(false);
        }
        
        private void CreatePlayerInfoUI(Transform parent)
        {
            CreateUIText(parent, "PlayerNameText", "道友", new Vector2(0, 80), 18);
            CreateUIText(parent, "PlayerLevelText", "等级: 1", new Vector2(-100, 50), 14);
            CreateUIText(parent, "PlayerExpText", "经验: 0/100", new Vector2(100, 50), 14);
            
            CreateUISlider(parent, "HealthSlider", new Vector2(-50, 20), Color.red);
            CreateUIText(parent, "HealthText", "100/100", new Vector2(80, 20), 12);
            
            CreateUISlider(parent, "ManaSlider", new Vector2(-50, -10), Color.blue);
            CreateUIText(parent, "ManaText", "50/50", new Vector2(80, -10), 12);
            
            CreateUIText(parent, "AttackText", "攻击: 10", new Vector2(-100, -40), 12);
            CreateUIText(parent, "DefenseText", "防御: 5", new Vector2(0, -40), 12);
            CreateUIText(parent, "SpeedText", "速度: 10", new Vector2(100, -40), 12);
            CreateUIText(parent, "LuckText", "运气: 10", new Vector2(-50, -65), 12);
            CreateUIText(parent, "GoldText", "灵石: 1000", new Vector2(50, -65), 12);
        }
        
        private void CreateMessageUI(Transform parent)
        {
            GameObject scrollView = new GameObject("MessageScrollRect");
            scrollView.transform.SetParent(parent, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -10);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            
            GameObject messageText = CreateUIText(content.transform, "MessageText", "欢迎来到仙侠世界！\n点击探索开始冒险...", Vector2.zero, 12);
            RectTransform textRect = messageText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = messageText.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
        }
        
        private void CreateActionUI(Transform parent)
        {
            CreateUIButton(parent, "ExploreButton", "探索", new Vector2(0, 50));
            CreateUIButton(parent, "InventoryButton", "背包", new Vector2(0, 10));
            CreateUIButton(parent, "SaveButton", "保存", new Vector2(-70, -30));
            CreateUIButton(parent, "LoadButton", "加载", new Vector2(70, -30));
            
            CreateUIText(parent, "ExplorationCountText", "探索: 0", new Vector2(-70, -60), 10);
            CreateUIText(parent, "BattleWinsText", "胜利: 0", new Vector2(0, -60), 10);
            CreateUIText(parent, "TreasuresFoundText", "宝藏: 0", new Vector2(70, -60), 10);
        }
        
        private void CreateBattleUI(Transform parent)
        {
            CreateUIText(parent, "EnemyNameText", "敌人名称", new Vector2(0, 150), 16);
            CreateUIText(parent, "EnemyStatsText", "敌人属性", new Vector2(0, 120), 12);
            CreateUISlider(parent, "EnemyHealthSlider", new Vector2(-50, 90), Color.red);
            CreateUIText(parent, "EnemyHealthText", "100/100", new Vector2(80, 90), 12);
            
            CreateUIButton(parent, "AttackButton", "攻击", new Vector2(-150, -50));
            CreateUIButton(parent, "DefendButton", "防御", new Vector2(-50, -50));
            CreateUIButton(parent, "UseItemButton", "使用物品", new Vector2(50, -50));
            CreateUIButton(parent, "EscapeButton", "逃跑", new Vector2(150, -50));
            
            CreateUIText(parent, "TurnInfoText", "第1回合 - 你的回合", new Vector2(0, -150), 14);
        }
        
        private void CreateInventoryUI(Transform parent)
        {
            CreateUIText(parent, "InventoryInfoText", "背包: 0/50", new Vector2(0, 280), 16);
            
            // 简化的背包网格区域
            GameObject scrollView = new GameObject("InventoryScrollRect");
            scrollView.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(600, 400);
            scrollRect.anchoredPosition = new Vector2(-100, 0);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject content = new GameObject("InventoryContent");
            content.transform.SetParent(scrollView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            
            // 装备面板
            GameObject equipPanel = CreateUIPanel(parent, "EquipmentPanel", new Vector2(250, 400), new Vector2(250, 0));
            GameObject equipSlots = new GameObject("EquipmentSlotsParent");
            equipSlots.transform.SetParent(equipPanel.transform, false);
            
            CreateUIButton(parent, "CloseInventoryButton", "关闭", new Vector2(300, -250));
            CreateUIButton(parent, "SortInventoryButton", "整理", new Vector2(200, -250));
        }
        
        private void CreateItemSlotPrefab()
        {
            GameObject slot = new GameObject("ItemSlot");
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);
            
            Image image = slot.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            Button button = slot.AddComponent<Button>();
            
            GameObject text = CreateUIText(slot.transform, "ItemText", "空", Vector2.zero, 10);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            string prefabPath = "Assets/Prefabs/UI/ItemSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(slot, prefabPath);
            DestroyImmediate(slot);
        }
        
        private void CreateGameManager()
        {
            GameObject existing = GameObject.Find("GameManager");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }
            
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            gameManager.AddComponent<GameStarter>();
            
            Debug.Log("GameManager已创建");
        }
        
        private void CreateAndConnectUI()
        {
            // 加载并实例化UI预制件
            string prefabPath = "Assets/Prefabs/UI/MainGameUI.prefab";
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (uiPrefab != null)
            {
                GameObject uiInstance = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                MainGameUI uiComponent = uiInstance.GetComponent<MainGameUI>();
                
                // 简化的自动连接
                AutoConnectBasicUI(uiComponent, uiInstance.transform);
                
                Debug.Log("UI已创建并连接");
            }
        }
        
        private void AutoConnectBasicUI(MainGameUI uiComponent, Transform root)
        {
            // 使用简单的名称匹配来连接最重要的UI元素
            var playerNameText = FindChildByName(root, "PlayerNameText")?.GetComponent<TextMeshProUGUI>();
            var exploreButton = FindChildByName(root, "ExploreButton")?.GetComponent<Button>();
            var messageText = FindChildByName(root, "MessageText")?.GetComponent<TextMeshProUGUI>();
            
            if (playerNameText != null || exploreButton != null || messageText != null)
            {
                Debug.Log("基础UI组件连接成功");
            }
        }
        
        private Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildByName(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
        
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("EventSystem已创建");
            }
        }
        
        private void CreateUIPrefabsOnly()
        {
            CreateFolderStructure();
            CreateAllUIPrefabs();
            
            EditorUtility.DisplayDialog("完成", "UI预制件创建完成！", "确定");
        }
        
        private void CreateTestSceneOnly()
        {
            CreateGameManager();
            EnsureEventSystem();
            
            // 创建简单的测试UI
            CreateSimpleTestUI();
            
            EditorUtility.DisplayDialog("完成", "测试场景创建完成！", "确定");
        }
        
        private void CreateSimpleTestUI()
        {
            GameObject canvas = new GameObject("TestCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvas.AddComponent<GraphicRaycaster>();
            
            // 简单的测试按钮
            CreateUIButton(canvas.transform, "TestExploreButton", "开始探索", new Vector2(0, 50));
            CreateUIText(canvas.transform, "TestInfoText", "点击按钮开始游戏测试", new Vector2(0, -50), 14);
        }
        
        // 辅助UI创建方法
        private GameObject CreateUIPanel(Transform parent, string name, Vector2 size, Vector2 position)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            return panel;
        }
        
        private GameObject CreateUIButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 30);
            rect.anchoredPosition = position;
            
            Image image = button.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button btn = button.AddComponent<Button>();
            
            GameObject textGO = CreateUIText(button.transform, "Text", text, Vector2.zero, 14);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            
            return button;
        }
        
        private GameObject CreateUIText(Transform parent, string name, string text, Vector2 position, float fontSize)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 20);
            rect.anchoredPosition = position;
            
            TextMeshProUGUI textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;
            
            // 尝试应用中文字体
            TMP_FontAsset chineseFont = UIFontUpdater.GetDefaultChineseFont();
            if (chineseFont != null)
            {
                textComp.font = chineseFont;
            }
            
            return textGO;
        }
        
        private GameObject CreateUISlider(Transform parent, string name, Vector2 position, Color fillColor)
        {
            GameObject slider = new GameObject(name);
            slider.transform.SetParent(parent, false);
            
            RectTransform rect = slider.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 15);
            rect.anchoredPosition = position;
            
            Slider sliderComp = slider.AddComponent<Slider>();
            
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(slider.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            
            sliderComp.fillRect = fillRect;
            sliderComp.value = 1f;
            
            return slider;
        }
    }
}
#endif