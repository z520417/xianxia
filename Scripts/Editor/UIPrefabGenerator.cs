#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// UI预制件自动生成器
    /// </summary>
    public class UIPrefabGenerator : EditorWindow
    {
        private bool m_CreateMainGameUI = true;
        private bool m_CreateInventoryUI = true;
        private bool m_CreateBattleUI = true;
        private bool m_CreateItemSlot = true;
        private bool m_CreateEquipmentSlot = true;
        
        private string m_PrefabPath = "Assets/Prefabs/UI/";
        
        [MenuItem("XianXia Game/UI Prefab Generator")]
        public static void ShowWindow()
        {
            GetWindow<UIPrefabGenerator>("UI预制件生成器");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("仙侠游戏 UI预制件生成器", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("选择要生成的UI预制件：", EditorStyles.label);
            m_CreateMainGameUI = EditorGUILayout.Toggle("主游戏UI", m_CreateMainGameUI);
            m_CreateInventoryUI = EditorGUILayout.Toggle("背包UI", m_CreateInventoryUI);
            m_CreateBattleUI = EditorGUILayout.Toggle("战斗UI", m_CreateBattleUI);
            m_CreateItemSlot = EditorGUILayout.Toggle("物品槽位", m_CreateItemSlot);
            m_CreateEquipmentSlot = EditorGUILayout.Toggle("装备槽位", m_CreateEquipmentSlot);
            
            GUILayout.Space(10);
            GUILayout.Label("预制件保存路径：", EditorStyles.label);
            m_PrefabPath = EditorGUILayout.TextField(m_PrefabPath);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("生成所有选中的UI预制件", GUILayout.Height(40)))
            {
                GenerateUIPrefabs();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("生成完整场景", GUILayout.Height(30)))
            {
                GenerateCompleteScene();
            }
        }
        
        private void GenerateUIPrefabs()
        {
            // 确保目录存在
            if (!Directory.Exists(m_PrefabPath))
            {
                Directory.CreateDirectory(m_PrefabPath);
            }
            
            if (m_CreateMainGameUI)
                CreateMainGameUIPrefab();
            
            if (m_CreateInventoryUI)
                CreateInventoryUIPrefab();
            
            if (m_CreateBattleUI)
                CreateBattleUIPrefab();
            
            if (m_CreateItemSlot)
                CreateItemSlotPrefab();
            
            if (m_CreateEquipmentSlot)
                CreateEquipmentSlotPrefab();
            
            AssetDatabase.Refresh();
            Debug.Log("UI预制件生成完成！");
        }
        
        private void CreateMainGameUIPrefab()
        {
            // 创建Canvas
            GameObject canvas = new GameObject("MainGameCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvas.AddComponent<GraphicRaycaster>();
            
            // 添加MainGameUI组件
            canvas.AddComponent<MainGameUI>();
            
            // 创建主要UI面板
            CreatePlayerInfoPanel(canvas.transform);
            CreateMessagePanel(canvas.transform);
            CreateActionPanel(canvas.transform);
            CreateBattlePanelInMain(canvas.transform);
            CreateInventoryPanelInMain(canvas.transform);
            
            // 保存为预制件
            string prefabPath = m_PrefabPath + "MainGameUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
            DestroyImmediate(canvas);
            
            Debug.Log($"主游戏UI预制件已创建：{prefabPath}");
        }
        
        private void CreatePlayerInfoPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "PlayerInfoPanel", new Vector2(300, 200), new Vector2(-760, 340));
            
            // 玩家名称
            CreateText(panel.transform, "PlayerNameText", "道友", new Vector2(0, 80), 16);
            
            // 等级和经验
            CreateText(panel.transform, "PlayerLevelText", "等级: 1", new Vector2(-100, 50), 14);
            CreateText(panel.transform, "PlayerExpText", "经验: 0/100", new Vector2(100, 50), 14);
            
            // 生命值条
            GameObject healthBar = CreateSlider(panel.transform, "HealthSlider", new Vector2(0, 20));
            CreateText(panel.transform, "HealthText", "100/100", new Vector2(120, 20), 12);
            
            // 法力值条
            GameObject manaBar = CreateSlider(panel.transform, "ManaSlider", new Vector2(0, -10));
            CreateText(panel.transform, "ManaText", "50/50", new Vector2(120, -10), 12);
            
            // 属性文本
            CreateText(panel.transform, "AttackText", "攻击: 10", new Vector2(-100, -40), 12);
            CreateText(panel.transform, "DefenseText", "防御: 5", new Vector2(0, -40), 12);
            CreateText(panel.transform, "SpeedText", "速度: 10", new Vector2(100, -40), 12);
            CreateText(panel.transform, "LuckText", "运气: 10", new Vector2(-50, -60), 12);
            CreateText(panel.transform, "GoldText", "灵石: 1000", new Vector2(50, -60), 12);
        }
        
        private void CreateMessagePanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "MessagePanel", new Vector2(600, 250), new Vector2(0, -300));
            
            // 创建ScrollView
            GameObject scrollView = new GameObject("MessageScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -10);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            
            // 创建Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 200);
            
            // 消息文本
            GameObject messageText = CreateText(content.transform, "MessageText", "游戏消息将在这里显示...", Vector2.zero, 12);
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
        
        private void CreateActionPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "ExplorePanel", new Vector2(200, 300), new Vector2(760, 0));
            
            CreateButton(panel.transform, "ExploreButton", "探索", new Vector2(0, 100));
            CreateButton(panel.transform, "InventoryButton", "背包", new Vector2(0, 50));
            CreateButton(panel.transform, "SaveButton", "保存", new Vector2(0, 0));
            CreateButton(panel.transform, "LoadButton", "加载", new Vector2(0, -50));
            
            // 统计信息
            CreateText(panel.transform, "ExplorationCountText", "探索: 0", new Vector2(0, -100), 12);
            CreateText(panel.transform, "BattleWinsText", "胜利: 0", new Vector2(0, -120), 12);
            CreateText(panel.transform, "TreasuresFoundText", "宝藏: 0", new Vector2(0, -140), 12);
        }
        
        private void CreateBattlePanelInMain(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "BattlePanel", new Vector2(800, 400), Vector2.zero);
            panel.SetActive(false); // 初始隐藏
            
            // 敌人信息
            GameObject enemyInfo = CreatePanel(panel.transform, "EnemyInfo", new Vector2(300, 150), new Vector2(0, 100));
            CreateText(enemyInfo.transform, "EnemyNameText", "敌人名称", new Vector2(0, 50), 16);
            CreateText(enemyInfo.transform, "EnemyStatsText", "敌人属性", new Vector2(0, 20), 12);
            CreateSlider(enemyInfo.transform, "EnemyHealthSlider", new Vector2(0, -20));
            CreateText(enemyInfo.transform, "EnemyHealthText", "100/100", new Vector2(0, -50), 12);
            
            // 战斗操作
            GameObject battleActions = CreatePanel(panel.transform, "BattleActions", new Vector2(400, 100), new Vector2(0, -50));
            CreateButton(battleActions.transform, "AttackButton", "攻击", new Vector2(-150, 0));
            CreateButton(battleActions.transform, "DefendButton", "防御", new Vector2(-50, 0));
            CreateButton(battleActions.transform, "UseItemButton", "使用物品", new Vector2(50, 0));
            CreateButton(battleActions.transform, "EscapeButton", "逃跑", new Vector2(150, 0));
            
            // 回合信息
            CreateText(panel.transform, "TurnInfoText", "第1回合 - 你的回合", new Vector2(0, -150), 14);
        }
        
        private void CreateInventoryPanelInMain(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "InventoryPanel", new Vector2(800, 600), Vector2.zero);
            panel.SetActive(false); // 初始隐藏
            
            // 背包标题
            CreateText(panel.transform, "InventoryTitle", "背包", new Vector2(0, 260), 18);
            
            // 背包网格
            GameObject scrollView = new GameObject("InventoryScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(500, 400);
            scrollRect.anchoredPosition = new Vector2(-100, 0);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject content = new GameObject("InventoryContent");
            content.transform.SetParent(scrollView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80, 80);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;
            
            // 装备面板
            GameObject equipPanel = CreatePanel(panel.transform, "EquipmentPanel", new Vector2(250, 400), new Vector2(200, 0));
            CreateText(equipPanel.transform, "EquipmentTitle", "装备", new Vector2(0, 180), 16);
            
            GameObject equipSlots = new GameObject("EquipmentSlotsParent");
            equipSlots.transform.SetParent(equipPanel.transform, false);
            
            // 按钮
            CreateButton(panel.transform, "CloseInventoryButton", "关闭", new Vector2(300, -250));
            CreateButton(panel.transform, "SortInventoryButton", "整理", new Vector2(200, -250));
            CreateText(panel.transform, "InventoryInfoText", "背包: 0/50", new Vector2(-300, -250), 14);
        }
        
        private void CreateInventoryUIPrefab()
        {
            // 创建独立的背包UI预制件...
            Debug.Log("背包UI预制件创建功能待完善");
        }
        
        private void CreateBattleUIPrefab()
        {
            // 创建独立的战斗UI预制件...
            Debug.Log("战斗UI预制件创建功能待完善");
        }
        
        private void CreateItemSlotPrefab()
        {
            GameObject slot = new GameObject("ItemSlot");
            
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);
            
            Image image = slot.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            Button button = slot.AddComponent<Button>();
            
            // 创建文本
            GameObject text = CreateText(slot.transform, "ItemText", "空", Vector2.zero, 10);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComp = text.GetComponent<TextMeshProUGUI>();
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.fontSize = 8;
            textComp.enableWordWrapping = true;
            
            // 保存预制件
            string prefabPath = m_PrefabPath + "ItemSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(slot, prefabPath);
            DestroyImmediate(slot);
            
            Debug.Log($"物品槽位预制件已创建：{prefabPath}");
        }
        
        private void CreateEquipmentSlotPrefab()
        {
            GameObject slot = new GameObject("EquipmentSlot");
            
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 30);
            
            Image image = slot.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            Button button = slot.AddComponent<Button>();
            
            // 创建文本
            GameObject text = CreateText(slot.transform, "EquipmentText", "武器: 未装备", Vector2.zero, 12);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            TextMeshProUGUI textComp = text.GetComponent<TextMeshProUGUI>();
            textComp.alignment = TextAlignmentOptions.MidlineLeft;
            
            // 保存预制件
            string prefabPath = m_PrefabPath + "EquipmentSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(slot, prefabPath);
            DestroyImmediate(slot);
            
            Debug.Log($"装备槽位预制件已创建：{prefabPath}");
        }
        
        private void GenerateCompleteScene()
        {
            // 先生成所有预制件
            GenerateUIPrefabs();
            
            // 创建完整场景
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            gameManager.AddComponent<GameStarter>();
            
            // 加载并实例化主UI预制件
            string mainUIPrefabPath = m_PrefabPath + "MainGameUI.prefab";
            if (File.Exists(mainUIPrefabPath))
            {
                GameObject mainUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainUIPrefabPath);
                if (mainUIPrefab != null)
                {
                    GameObject mainUI = PrefabUtility.InstantiatePrefab(mainUIPrefab) as GameObject;
                    
                    // 自动连接引用
                    MainGameUI uiComponent = mainUI.GetComponent<MainGameUI>();
                    GameStarter starter = gameManager.GetComponent<GameStarter>();
                    
                    // 这里可以通过反射或其他方式自动连接UI引用
                    AutoConnectUIReferences(uiComponent, mainUI.transform);
                }
            }
            
            // 确保有EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("完整场景生成完成！");
        }
        
        private void AutoConnectUIReferences(MainGameUI uiComponent, Transform uiRoot)
        {
            // 这里可以实现自动连接UI引用的逻辑
            // 通过名称查找对应的UI组件并设置到MainGameUI的字段中
            Debug.Log("UI引用自动连接功能待实现");
        }
        
        // 辅助方法
        private GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position)
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
        
        private GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 30);
            rect.anchoredPosition = position;
            
            Image image = button.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button btn = button.AddComponent<Button>();
            
            // 按钮文本
            GameObject textGO = CreateText(button.transform, "Text", text, Vector2.zero, 14);
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
        
        private GameObject CreateText(Transform parent, string name, string text, Vector2 position, float fontSize)
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
            
            return textGO;
        }
        
        private GameObject CreateSlider(Transform parent, string name, Vector2 position)
        {
            GameObject slider = new GameObject(name);
            slider.transform.SetParent(parent, false);
            
            RectTransform rect = slider.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 20);
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
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0f, 0.8f, 0f, 1f);
            
            sliderComp.fillRect = fillRect;
            sliderComp.value = 1f;
            
            return slider;
        }
    }
}
#endif