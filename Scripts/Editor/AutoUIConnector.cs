#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Reflection;

namespace XianXiaGame.Editor
{
    /// <summary>
    /// 自动UI连接器 - 自动连接UI组件引用
    /// </summary>
    public class AutoUIConnector : EditorWindow
    {
        private MainGameUI m_TargetUI;
        
        [MenuItem("XianXia Game/Auto UI Connector")]
        public static void ShowWindow()
        {
            GetWindow<AutoUIConnector>("自动UI连接器");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("自动UI连接器", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("选择要连接的MainGameUI组件：", EditorStyles.label);
            m_TargetUI = (MainGameUI)EditorGUILayout.ObjectField("MainGameUI", m_TargetUI, typeof(MainGameUI), true);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("自动连接所有UI引用", GUILayout.Height(40)))
            {
                if (m_TargetUI != null)
                {
                    AutoConnectUIReferences();
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请先选择一个MainGameUI组件！", "确定");
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("扫描场景中的MainGameUI", GUILayout.Height(30)))
            {
                ScanForMainGameUI();
            }
        }
        
        private void ScanForMainGameUI()
        {
            MainGameUI foundUI = FindObjectOfType<MainGameUI>();
            if (foundUI != null)
            {
                m_TargetUI = foundUI;
                Debug.Log($"找到MainGameUI组件：{foundUI.name}");
            }
            else
            {
                EditorUtility.DisplayDialog("未找到", "场景中没有找到MainGameUI组件！", "确定");
            }
        }
        
        private void AutoConnectUIReferences()
        {
            if (m_TargetUI == null) return;
            
            Transform root = m_TargetUI.transform;
            
            // 使用反射获取MainGameUI的所有字段
            FieldInfo[] fields = typeof(MainGameUI).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            
            int connectedCount = 0;
            
            foreach (FieldInfo field in fields)
            {
                // 只处理UI相关的字段
                if (IsUIField(field))
                {
                    GameObject found = FindUIElementByName(root, field.Name);
                    if (found != null)
                    {
                        object component = GetRequiredComponent(found, field.FieldType);
                        if (component != null)
                        {
                            field.SetValue(m_TargetUI, component);
                            connectedCount++;
                            Debug.Log($"已连接：{field.Name} -> {found.name}");
                        }
                    }
                }
            }
            
            // 标记为已修改
            EditorUtility.SetDirty(m_TargetUI);
            
            EditorUtility.DisplayDialog("连接完成", 
                $"成功连接了 {connectedCount} 个UI组件引用！\n\n请检查Console查看详细信息。", "确定");
        }
        
        private bool IsUIField(FieldInfo field)
        {
            // 检查字段类型是否为UI组件
            return typeof(Component).IsAssignableFrom(field.FieldType) && 
                   (field.FieldType == typeof(TextMeshProUGUI) ||
                    field.FieldType == typeof(Button) ||
                    field.FieldType == typeof(Slider) ||
                    field.FieldType == typeof(ScrollRect) ||
                    field.FieldType == typeof(GameObject) ||
                    field.FieldType == typeof(Transform));
        }
        
        private GameObject FindUIElementByName(Transform root, string fieldName)
        {
            // 根据字段名生成可能的UI元素名称
            string[] possibleNames = GeneratePossibleNames(fieldName);
            
            foreach (string name in possibleNames)
            {
                Transform found = FindChildRecursive(root, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }
            
            return null;
        }
        
        private string[] GeneratePossibleNames(string fieldName)
        {
            // 移除前缀 m_ 并生成可能的名称
            string baseName = fieldName.StartsWith("m_") ? fieldName.Substring(2) : fieldName;
            
            return new string[]
            {
                baseName,
                baseName.Replace("Text", ""),
                baseName.Replace("Button", ""),
                baseName.Replace("Slider", ""),
                baseName.Replace("Panel", ""),
                baseName + "Text",
                baseName + "Button",
                baseName + "Slider",
                baseName + "Panel"
            };
        }
        
        private Transform FindChildRecursive(Transform parent, string name)
        {
            // 递归查找子对象
            if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }
        
        private object GetRequiredComponent(GameObject obj, System.Type requiredType)
        {
            if (requiredType == typeof(GameObject))
            {
                return obj;
            }
            else if (requiredType == typeof(Transform))
            {
                return obj.transform;
            }
            else
            {
                return obj.GetComponent(requiredType);
            }
        }
    }
}
#endif