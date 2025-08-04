#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// UI字体更新器 - 自动为新创建的UI应用中文字体
    /// </summary>
    [InitializeOnLoad]
    public class UIFontUpdater
    {
        private static TMP_FontAsset s_DefaultChineseFont;
        
        static UIFontUpdater()
        {
            // 在编辑器加载时查找默认中文字体
            FindDefaultChineseFont();
            
            // 监听层级视图的变化
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        
        private static void FindDefaultChineseFont()
        {
            // 查找项目中的中文字体
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font != null && IsChineseFont(font))
                {
                    s_DefaultChineseFont = font;
                    Debug.Log($"自动检测到中文字体：{font.name}");
                    break;
                }
            }
        }
        
        private static bool IsChineseFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            
            string fontName = font.name.ToLower();
            string[] chineseKeywords = { 
                "chinese", "cjk", "han", "simhei", "yahei", "noto", 
                "source", "tmp", "思源", "雅黑", "黑体" 
            };
            
            foreach (string keyword in chineseKeywords)
            {
                if (fontName.Contains(keyword))
                {
                    return true;
                }
            }
            
            // 检查是否包含中文字符（如果字体已加载）
            try
            {
                return font.HasCharacter('中') || font.HasCharacter('文');
            }
            catch
            {
                return false;
            }
        }
        
        private static void OnHierarchyChanged()
        {
            if (s_DefaultChineseFont == null) return;
            
            // 查找新创建的TextMeshPro组件
            TextMeshProUGUI[] uiTexts = Object.FindObjectsOfType<TextMeshProUGUI>();
            
            foreach (var text in uiTexts)
            {
                // 如果使用的是默认字体或没有字体，则应用中文字体
                if (text.font == null || text.font.name.Contains("LiberationSans"))
                {
                    text.font = s_DefaultChineseFont;
                    EditorUtility.SetDirty(text);
                }
            }
        }
        
        /// <summary>
        /// 手动设置默认中文字体
        /// </summary>
        public static void SetDefaultChineseFont(TMP_FontAsset font)
        {
            s_DefaultChineseFont = font;
            
            if (font != null)
            {
                Debug.Log($"设置默认中文字体：{font.name}");
                
                // 立即应用到所有现有的文本组件
                ApplyToAllExistingTexts();
            }
        }
        
        private static void ApplyToAllExistingTexts()
        {
            if (s_DefaultChineseFont == null) return;
            
            TextMeshProUGUI[] uiTexts = Object.FindObjectsOfType<TextMeshProUGUI>();
            TextMeshPro[] worldTexts = Object.FindObjectsOfType<TextMeshPro>();
            
            int updatedCount = 0;
            
            foreach (var text in uiTexts)
            {
                text.font = s_DefaultChineseFont;
                EditorUtility.SetDirty(text);
                updatedCount++;
            }
            
            foreach (var text in worldTexts)
            {
                text.font = s_DefaultChineseFont;
                EditorUtility.SetDirty(text);
                updatedCount++;
            }
            
            if (updatedCount > 0)
            {
                Debug.Log($"已更新 {updatedCount} 个文本组件的字体");
            }
        }
        
        /// <summary>
        /// 获取当前默认中文字体
        /// </summary>
        public static TMP_FontAsset GetDefaultChineseFont()
        {
            return s_DefaultChineseFont;
        }
    }
}
#endif