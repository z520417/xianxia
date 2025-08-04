#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// 中文字体设置工具
    /// </summary>
    public class ChineseFontSetup : EditorWindow
    {
        private TMP_FontAsset m_SelectedChineseFont;
        private bool m_AutoSetupComplete = false;
        
        // 推荐的中文字体列表
        private readonly string[] m_RecommendedFonts = {
            "Assets/Fonts/SourceHanSansCN-Regular.ttf",
            "Assets/Fonts/NotoSansCJK-Regular.ttc", 
            "Assets/Fonts/SimHei.ttf",
            "Assets/Fonts/Microsoft-YaHei.ttf",
            "Assets/Fonts/DroidSansFallback.ttf"
        };
        
        [MenuItem("XianXia Game/中文字体设置")]
        public static void ShowWindow()
        {
            GetWindow<ChineseFontSetup>("中文字体设置");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🈶 仙侠游戏中文字体设置", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("为了获得最佳的中文显示效果，建议设置专用的中文字体。\n" +
                "工具会自动：\n" +
                "✅ 检测和导入中文字体\n" +
                "✅ 创建TextMeshPro字体资源\n" +
                "✅ 应用到所有UI文本组件", MessageType.Info);
            
            GUILayout.Space(15);
            
            // 字体选择区域
            GUILayout.Label("字体设置", EditorStyles.boldLabel);
            
            m_SelectedChineseFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "选择中文字体", m_SelectedChineseFont, typeof(TMP_FontAsset), false);
            
            GUILayout.Space(10);
            
            // 自动设置按钮
            if (!m_AutoSetupComplete)
            {
                if (GUILayout.Button("🚀 自动设置中文字体", GUILayout.Height(50)))
                {
                    AutoSetupChineseFont();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("📥 下载推荐中文字体", GUILayout.Height(40)))
                {
                    ShowFontDownloadGuide();
                }
                
                if (GUILayout.Button("🔍 扫描现有字体", GUILayout.Height(30)))
                {
                    ScanExistingFonts();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ 中文字体设置完成！\n\n" +
                    "所有UI文本现在都将使用中文字体显示。", MessageType.None);
                
                if (GUILayout.Button("重新设置", GUILayout.Height(30)))
                {
                    m_AutoSetupComplete = false;
                }
            }
            
            GUILayout.Space(15);
            
            // 手动操作区域
            GUILayout.Label("手动操作", EditorStyles.boldLabel);
            
            if (GUILayout.Button("应用字体到场景中所有文本"))
            {
                ApplyFontToAllTexts();
            }
            
            if (GUILayout.Button("应用字体到所有UI预制件"))
            {
                ApplyFontToAllPrefabs();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("创建字体变体（粗体/斜体）"))
            {
                CreateFontVariants();
            }
        }
        
        private void AutoSetupChineseFont()
        {
            try
            {
                EditorUtility.DisplayProgressBar("设置中文字体", "正在扫描字体资源...", 0.1f);
                
                // 1. 扫描并创建字体资源
                TMP_FontAsset chineseFont = FindOrCreateChineseFont();
                
                if (chineseFont == null)
                {
                    EditorUtility.ClearProgressBar();
                    ShowFontDownloadGuide();
                    return;
                }
                
                EditorUtility.DisplayProgressBar("设置中文字体", "正在应用到UI组件...", 0.5f);
                
                // 2. 应用到所有UI
                ApplyFontToAllComponents(chineseFont);
                
                EditorUtility.DisplayProgressBar("设置中文字体", "正在更新预制件...", 0.8f);
                
                // 3. 更新预制件
                UpdatePrefabsWithFont(chineseFont);
                
                EditorUtility.ClearProgressBar();
                
                m_SelectedChineseFont = chineseFont;
                m_AutoSetupComplete = true;
                
                EditorUtility.DisplayDialog("设置完成", 
                    "🎉 中文字体设置完成！\n\n" +
                    $"使用字体：{chineseFont.name}\n\n" +
                    "所有UI文本现在都会正确显示中文字符。", "确定");
                
                Debug.Log($"中文字体设置完成，使用字体：{chineseFont.name}");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("设置失败", $"中文字体设置过程中出现错误：\n{e.Message}", "确定");
                Debug.LogError($"中文字体设置失败：{e}");
            }
        }
        
        private TMP_FontAsset FindOrCreateChineseFont()
        {
            // 1. 先查找已存在的中文TextMeshPro字体
            TMP_FontAsset existingFont = FindExistingChineseFont();
            if (existingFont != null)
            {
                Debug.Log($"找到现有中文字体：{existingFont.name}");
                return existingFont;
            }
            
            // 2. 查找系统字体文件
            Font systemFont = FindSystemChineseFont();
            if (systemFont != null)
            {
                return CreateTMPFontFromSystemFont(systemFont);
            }
            
            // 3. 使用Unity默认字体
            return CreateFallbackFont();
        }
        
        private TMP_FontAsset FindExistingChineseFont()
        {
            // 搜索项目中的TextMeshPro字体资源
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font != null && IsChineseFont(font))
                {
                    return font;
                }
            }
            
            return null;
        }
        
        private bool IsChineseFont(TMP_FontAsset font)
        {
            // 检查字体名称是否包含中文相关关键词
            string fontName = font.name.ToLower();
            string[] chineseKeywords = { "chinese", "cjk", "han", "simhei", "yahei", "noto", "source" };
            
            foreach (string keyword in chineseKeywords)
            {
                if (fontName.Contains(keyword))
                {
                    return true;
                }
            }
            
            // 检查字体是否包含常用中文字符
            return font.HasCharacter('中') && font.HasCharacter('文');
        }
        
        private Font FindSystemChineseFont()
        {
            // 搜索项目中的字体文件
            string[] fontExtensions = { ".ttf", ".otf", ".ttc" };
            
            foreach (string extension in fontExtensions)
            {
                string[] guids = AssetDatabase.FindAssets($"t:Font");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.ToLower().EndsWith(extension))
                    {
                        Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                        if (font != null && IsSystemChineseFont(font))
                        {
                            return font;
                        }
                    }
                }
            }
            
            return null;
        }
        
        private bool IsSystemChineseFont(Font font)
        {
            string fontName = font.name.ToLower();
            string[] chineseFontNames = { 
                "simhei", "simsun", "yahei", "microsoft", "noto", "source", 
                "han", "cjk", "chinese", "pingfang", "hiragino" 
            };
            
            foreach (string name in chineseFontNames)
            {
                if (fontName.Contains(name))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private TMP_FontAsset CreateTMPFontFromSystemFont(Font systemFont)
        {
            // 确保Fonts目录存在
            string fontDir = "Assets/Fonts/";
            if (!AssetDatabase.IsValidFolder(fontDir))
            {
                AssetDatabase.CreateFolder("Assets", "Fonts");
            }
            
            // 创建TextMeshPro字体资源
            string fontAssetPath = fontDir + systemFont.name + "_TMP.asset";
            
            // 使用TextMeshPro字体创建窗口的默认设置
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(systemFont);
            
            if (fontAsset != null)
            {
                AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"从系统字体创建了TextMeshPro字体：{fontAssetPath}");
                return fontAsset;
            }
            
            return null;
        }
        
        private TMP_FontAsset CreateFallbackFont()
        {
            // 使用Arial Unicode作为fallback，它包含中文字符
            Font arialUnicode = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arialUnicode != null)
            {
                TMP_FontAsset fallbackFont = TMP_FontAsset.CreateFontAsset(arialUnicode);
                if (fallbackFont != null)
                {
                    string fallbackPath = "Assets/Fonts/ChineseFallback_TMP.asset";
                    AssetDatabase.CreateAsset(fallbackFont, fallbackPath);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log("创建了中文备用字体");
                    return fallbackFont;
                }
            }
            
            return null;
        }
        
        private void ApplyFontToAllComponents(TMP_FontAsset font)
        {
            // 应用到场景中的所有TextMeshPro组件
            TextMeshProUGUI[] uiTexts = FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in uiTexts)
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
            
            TextMeshPro[] worldTexts = FindObjectsOfType<TextMeshPro>();
            foreach (var text in worldTexts)
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }
            
            Debug.Log($"应用字体到 {uiTexts.Length + worldTexts.Length} 个文本组件");
        }
        
        private void UpdatePrefabsWithFont(TMP_FontAsset font)
        {
            // 更新UI预制件中的字体
            string[] prefabPaths = { "Assets/Prefabs/UI/" };
            
            foreach (string prefabPath in prefabPaths)
            {
                if (AssetDatabase.IsValidFolder(prefabPath))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
                    
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        
                        if (prefab != null)
                        {
                            UpdatePrefabFont(prefab, font);
                        }
                    }
                }
            }
        }
        
        private void UpdatePrefabFont(GameObject prefab, TMP_FontAsset font)
        {
            TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
            
            if (texts.Length > 0)
            {
                foreach (var text in texts)
                {
                    text.font = font;
                }
                
                EditorUtility.SetDirty(prefab);
                Debug.Log($"更新预制件 {prefab.name} 的字体设置");
            }
        }
        
        private void ScanExistingFonts()
        {
            List<TMP_FontAsset> chineseFonts = new List<TMP_FontAsset>();
            
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font != null && IsChineseFont(font))
                {
                    chineseFonts.Add(font);
                }
            }
            
            if (chineseFonts.Count > 0)
            {
                string message = $"找到 {chineseFonts.Count} 个中文字体：\n\n";
                foreach (var font in chineseFonts)
                {
                    message += $"• {font.name}\n";
                }
                message += "\n选择一个字体进行设置：";
                
                EditorUtility.DisplayDialog("发现中文字体", message, "确定");
                
                if (chineseFonts.Count > 0)
                {
                    m_SelectedChineseFont = chineseFonts[0];
                }
            }
            else
            {
                ShowFontDownloadGuide();
            }
        }
        
        private void ShowFontDownloadGuide()
        {
            string message = "📥 推荐下载以下免费中文字体：\n\n" +
                "1. Source Han Sans CN (思源黑体)\n" +
                "   - Google Fonts免费字体\n" +
                "   - 支持简繁体中文\n" +
                "   - 下载：fonts.google.com\n\n" +
                "2. Noto Sans CJK\n" +
                "   - Google开源字体\n" +
                "   - 优秀的中日韩支持\n\n" +
                "3. 微软雅黑 (系统自带)\n" +
                "   - Windows系统字体\n" +
                "   - 路径：C:/Windows/Fonts/\n\n" +
                "下载后放入 Assets/Fonts/ 文件夹";
            
            EditorUtility.DisplayDialog("中文字体下载指南", message, "我知道了");
        }
        
        private void ApplyFontToAllTexts()
        {
            if (m_SelectedChineseFont != null)
            {
                ApplyFontToAllComponents(m_SelectedChineseFont);
                EditorUtility.DisplayDialog("完成", "已应用字体到场景中所有文本组件！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个中文字体！", "确定");
            }
        }
        
        private void ApplyFontToAllPrefabs()
        {
            if (m_SelectedChineseFont != null)
            {
                UpdatePrefabsWithFont(m_SelectedChineseFont);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("完成", "已应用字体到所有UI预制件！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个中文字体！", "确定");
            }
        }
        
        private void CreateFontVariants()
        {
            if (m_SelectedChineseFont != null)
            {
                EditorUtility.DisplayDialog("功能提示", 
                    "字体变体创建功能:\n\n" +
                    "1. 打开 Window > TextMeshPro > Font Asset Creator\n" +
                    "2. 选择源字体文件\n" +
                    "3. 设置 Rendering Mode\n" +
                    "4. 调整 Padding 和 Atlas Resolution\n" +
                    "5. 点击 Generate Font Atlas", "了解");
            }
        }
    }
}
#endif