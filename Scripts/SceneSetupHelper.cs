using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XianXiaGame
{
    /// <summary>
    /// 场景设置助手 - 自动创建基本的游戏UI结构
    /// </summary>
    public class SceneSetupHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("自动设置选项")]
        [SerializeField] private bool m_CreateGameManager = true;
        [SerializeField] private bool m_CreateTestUI = true;
        [SerializeField] private bool m_CreateCompleteUI = false;

        [ContextMenu("自动设置测试场景")]
        public void SetupTestScene()
        {
            Debug.Log("开始自动设置测试场景...");

            // 创建GameManager
            if (m_CreateGameManager)
            {
                CreateGameManager();
            }

            // 创建测试UI
            if (m_CreateTestUI)
            {
                CreateTestUI();
            }

            // 创建完整UI
            if (m_CreateCompleteUI)
            {
                CreateCompleteUI();
            }

            Debug.Log("场景设置完成！可以运行游戏了。");
        }

        private void CreateGameManager()
        {
            // 查找是否已存在GameManager
            GameManager existingManager = FindObjectOfType<GameManager>();
            if (existingManager != null)
            {
                Debug.Log("GameManager已存在，跳过创建。");
                return;
            }

            // 创建GameManager GameObject
            GameObject managerGO = new GameObject("GameManager");
            managerGO.AddComponent<GameManager>();
            managerGO.AddComponent<GameStarter>();

            Debug.Log("创建了GameManager和GameStarter组件。");
        }

        private void CreateTestUI()
        {
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 确保有EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 创建测试UI面板
            GameObject testPanel = CreateUIPanel(canvas.transform, "TestPanel", new Vector2(400, 300));
            
            // 创建按钮
            GameObject exploreBtn = CreateButton(testPanel.transform, "ExploreButton", "探索", new Vector2(0, 80));
            GameObject battleBtn = CreateButton(testPanel.transform, "BattleButton", "测试战斗", new Vector2(0, 40));
            GameObject inventoryBtn = CreateButton(testPanel.transform, "InventoryButton", "添加物品", new Vector2(0, 0));
            
            // 创建日志文本
            GameObject logText = CreateText(testPanel.transform, "LogText", "游戏日志...", new Vector2(0, -80));
            logText.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 100);

            // 自动连接到GameStarter
            GameStarter starter = FindObjectOfType<GameStarter>();
            if (starter != null)
            {
                // 使用反射设置私有字段
                var fields = typeof(GameStarter).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.Name == "m_TestExploreButton")
                        field.SetValue(starter, exploreBtn.GetComponent<Button>());
                    else if (field.Name == "m_TestBattleButton")
                        field.SetValue(starter, battleBtn.GetComponent<Button>());
                    else if (field.Name == "m_TestInventoryButton")
                        field.SetValue(starter, inventoryBtn.GetComponent<Button>());
                    else if (field.Name == "m_TestLogText")
                        field.SetValue(starter, logText.GetComponent<TextMeshProUGUI>());
                }
                
                Debug.Log("已自动连接按钮到GameStarter组件。");
            }

            Debug.Log("创建了测试UI界面。");
        }

        private void CreateCompleteUI()
        {
            // 这里可以创建完整的游戏UI
            // 由于UI较复杂，建议手动创建或使用UI预制体
            Debug.Log("完整UI创建功能待实现，建议手动创建或使用预制体。");
        }

        private GameObject CreateUIPanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            return panel;
        }

        private GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform rectTransform = button.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160, 30);
            rectTransform.anchoredPosition = position;
            
            Image image = button.AddComponent<Image>();
            image.color = new Color(0.1f, 0.5f, 0.8f, 1f);
            
            Button buttonComponent = button.AddComponent<Button>();
            
            // 创建按钮文本
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(button.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return button;
        }

        private GameObject CreateText(Transform parent, string name, string text, Vector2 position)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            RectTransform rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            rectTransform.anchoredPosition = position;
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.wordWrapping = true;
            
            return textGO;
        }

        [ContextMenu("清理场景")]
        public void CleanupScene()
        {
            // 清理测试对象
            GameObject[] testObjects = { 
                GameObject.Find("GameManager"), 
                GameObject.Find("Canvas"),
                GameObject.Find("EventSystem")
            };

            foreach (GameObject obj in testObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }

            Debug.Log("场景已清理。");
        }
#endif
    }
}