#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// 字体测试助手 - 快速测试中文字体效果
    /// </summary>
    public class FontTestHelper : EditorWindow
    {
        private string m_TestText = "仙侠游戏 - 探索修真世界\n装备：青云剑 +10\n等级：筑基期 第3层\n经验：12345/50000";
        private TMP_FontAsset m_TestFont;
        private float m_FontSize = 16f;
        private Color m_TextColor = Color.white;
        private GameObject m_TestCanvas;
        private TextMeshProUGUI m_TestTextComponent;
        
        [MenuItem("XianXia Game/字体测试助手")]
        public static void ShowWindow()
        {
            GetWindow<FontTestHelper>("字体测试助手");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🧪 中文字体测试助手", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("用于测试不同中文字体在游戏中的显示效果。\n" +
                "可以实时预览字体、大小和颜色的效果。", MessageType.Info);
            
            GUILayout.Space(15);
            
            // 字体测试设置
            GUILayout.Label("测试设置", EditorStyles.boldLabel);
            
            m_TestFont = (TMP_FontAsset)EditorGUILayout.ObjectField("测试字体", m_TestFont, typeof(TMP_FontAsset), false);
            
            GUILayout.Space(5);
            m_FontSize = EditorGUILayout.Slider("字体大小", m_FontSize, 8f, 48f);
            
            GUILayout.Space(5);
            m_TextColor = EditorGUILayout.ColorField("文字颜色", m_TextColor);
            
            GUILayout.Space(10);
            
            // 测试文本
            GUILayout.Label("测试文本", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("编辑下面的文本来测试不同的中文内容", MessageType.None);
            m_TestText = EditorGUILayout.TextArea(m_TestText, GUILayout.Height(80));
            
            GUILayout.Space(15);
            
            // 控制按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 创建测试预览", GUILayout.Height(40)))
            {
                CreateTestPreview();
            }
            
            if (GUILayout.Button("🗑️ 清理测试对象", GUILayout.Height(40)))
            {
                CleanupTestObjects();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("📋 应用当前字体到所有UI"))
            {
                ApplyFontToAllUI();
            }
            
            GUILayout.Space(15);
            
            // 快速字体选择
            GUILayout.Label("快速字体测试", EditorStyles.boldLabel);
            
            if (GUILayout.Button("🎯 自动检测并测试项目字体"))
            {
                TestProjectFonts();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("测试小字体 (12px)"))
            {
                m_FontSize = 12f;
                UpdateTestPreview();
            }
            
            if (GUILayout.Button("测试标准 (16px)"))
            {
                m_FontSize = 16f;
                UpdateTestPreview();
            }
            
            if (GUILayout.Button("测试大字体 (24px)"))
            {
                m_FontSize = 24f;
                UpdateTestPreview();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // 显示测试状态
            if (m_TestTextComponent != null)
            {
                EditorGUILayout.HelpBox($"✅ 测试预览已创建\n" +
                    $"字体：{(m_TestFont ? m_TestFont.name : "默认")}\n" +
                    $"大小：{m_FontSize}px\n" +
                    $"对象：{m_TestTextComponent.gameObject.name}", MessageType.None);
            }
        }
        
        private void CreateTestPreview()
        {
            // 清理现有的测试对象
            CleanupTestObjects();
            
            // 创建测试Canvas
            m_TestCanvas = new GameObject("FontTestCanvas");
            Canvas canvas = m_TestCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 确保在最前面
            
            CanvasScaler scaler = m_TestCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            m_TestCanvas.AddComponent<GraphicRaycaster>();
            
            // 创建背景
            GameObject background = new GameObject("Background");
            background.transform.SetParent(m_TestCanvas.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            
            // 创建测试文本
            GameObject textGO = new GameObject("TestText");
            textGO.transform.SetParent(m_TestCanvas.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.1f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            m_TestTextComponent = textGO.AddComponent<TextMeshProUGUI>();
            m_TestTextComponent.text = m_TestText;
            m_TestTextComponent.fontSize = m_FontSize;
            m_TestTextComponent.color = m_TextColor;
            m_TestTextComponent.alignment = TextAlignmentOptions.Center;
            m_TestTextComponent.enableWordWrapping = true;
            
            if (m_TestFont != null)
            {
                m_TestTextComponent.font = m_TestFont;
            }
            
            // 创建关闭按钮
            CreateCloseButton();
            
            Debug.Log("✅ 字体测试预览已创建，按ESC或点击关闭按钮退出预览");
        }
        
        private void CreateCloseButton()
        {
            GameObject buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(m_TestCanvas.transform, false);
            
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.9f, 0.9f);
            buttonRect.anchorMax = new Vector2(0.95f, 0.95f);
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            UnityEngine.UI.Image buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = Color.red;
            
            UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(CleanupTestObjects);
            
            // 添加X文字
            GameObject textGO = new GameObject("ButtonText");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = "✕";
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
        }
        
        private void UpdateTestPreview()
        {
            if (m_TestTextComponent != null)
            {
                m_TestTextComponent.text = m_TestText;
                m_TestTextComponent.fontSize = m_FontSize;
                m_TestTextComponent.color = m_TextColor;
                
                if (m_TestFont != null)
                {
                    m_TestTextComponent.font = m_TestFont;
                }
            }
        }
        
        private void CleanupTestObjects()
        {
            if (m_TestCanvas != null)
            {
                DestroyImmediate(m_TestCanvas);
                m_TestCanvas = null;
                m_TestTextComponent = null;
                Debug.Log("🗑️ 测试对象已清理");
            }
        }
        
        private void ApplyFontToAllUI()
        {
            if (m_TestFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个测试字体！", "确定");
                return;
            }
            
            // 设置为默认字体
            UIFontUpdater.SetDefaultChineseFont(m_TestFont);
            
            // 应用到所有UI
            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
            int updatedCount = 0;
            
            foreach (var text in allTexts)
            {
                if (text.gameObject != m_TestTextComponent?.gameObject) // 跳过测试对象
                {
                    text.font = m_TestFont;
                    EditorUtility.SetDirty(text);
                    updatedCount++;
                }
            }
            
            EditorUtility.DisplayDialog("完成", 
                $"已将字体 {m_TestFont.name} 应用到 {updatedCount} 个UI文本组件！", "确定");
        }
        
        private void TestProjectFonts()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", 
                    "项目中没有找到TextMeshPro字体资源。\n\n" +
                    "请先：\n" +
                    "1. 导入中文字体文件到Assets/Fonts/\n" +
                    "2. 使用 XianXia Game > 中文字体设置 创建字体资源", "确定");
                return;
            }
            
            string message = $"找到 {guids.Length} 个字体资源：\n\n";
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font != null)
                {
                    message += $"• {font.name}\n";
                }
            }
            
            message += "\n选择一个字体进行测试：";
            
            EditorUtility.DisplayDialog("项目字体资源", message, "确定");
            
            // 自动选择第一个字体
            if (guids.Length > 0)
            {
                string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_TestFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(firstPath);
                
                if (m_TestFont != null)
                {
                    CreateTestPreview();
                }
            }
        }
        
        private void OnDisable()
        {
            CleanupTestObjects();
        }
        
        private void Update()
        {
            // 按ESC键关闭测试预览
            if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                CleanupTestObjects();
            }
        }
    }
}
#endif