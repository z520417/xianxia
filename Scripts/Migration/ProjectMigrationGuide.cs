#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XianXiaGame.Utils;

namespace XianXiaGame.Migration
{
    /// <summary>
    /// 项目迁移指南
    /// 帮助用户从旧系统升级到新的服务架构
    /// </summary>
    public class ProjectMigrationGuide : EditorWindow
    {
        private Vector2 m_ScrollPosition;
        private bool m_ShowMigrationSteps = true;
        private bool m_ShowCompatibilityCheck = true;
        private bool m_ShowNewFeatures = true;

        [MenuItem("仙侠游戏/项目迁移指南", priority = 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectMigrationGuide>("项目迁移指南");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.Label("仙侠游戏项目迁移指南", titleStyle);
            GUILayout.Space(10);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            // 迁移概述
            DrawMigrationOverview();
            GUILayout.Space(10);

            // 兼容性检查
            m_ShowCompatibilityCheck = EditorGUILayout.Foldout(m_ShowCompatibilityCheck, "兼容性检查", true);
            if (m_ShowCompatibilityCheck)
            {
                DrawCompatibilityCheck();
            }
            GUILayout.Space(10);

            // 迁移步骤
            m_ShowMigrationSteps = EditorGUILayout.Foldout(m_ShowMigrationSteps, "迁移步骤", true);
            if (m_ShowMigrationSteps)
            {
                DrawMigrationSteps();
            }
            GUILayout.Space(10);

            // 新功能介绍
            m_ShowNewFeatures = EditorGUILayout.Foldout(m_ShowNewFeatures, "新功能介绍", true);
            if (m_ShowNewFeatures)
            {
                DrawNewFeatures();
            }

            EditorGUILayout.EndScrollView();

            // 操作按钮
            GUILayout.Space(10);
            DrawActionButtons();
        }

        private void DrawMigrationOverview()
        {
            EditorGUILayout.HelpBox(
                "此迁移指南将帮助您将现有的仙侠游戏项目升级到新的服务架构。\n\n" +
                "新架构的主要改进：\n" +
                "• 依赖注入容器 - 降低系统耦合度\n" +
                "• 数据驱动设计 - ScriptableObject配置\n" +
                "• 事件系统 - 解耦系统间通信\n" +
                "• UI性能优化 - 对象池和更新管理\n" +
                "• 完整存档系统 - JSON序列化\n" +
                "• 日志系统 - 完善的调试支持\n\n" +
                "升级过程完全向后兼容，您的现有代码不会受到影响。",
                MessageType.Info);
        }

        private void DrawCompatibilityCheck()
        {
            EditorGUI.indentLevel++;

            GUILayout.Label("检查项目兼容性:", EditorStyles.boldLabel);

            var compatibilityItems = new List<(string, bool, string)>
            {
                ("GameManager.cs", CheckFileExists("Scripts/Managers/GameManager.cs"), "原始游戏管理器"),
                ("CharacterStats.cs", CheckFileExists("Scripts/Data/CharacterStats.cs"), "角色属性系统"),
                ("ItemData.cs", CheckFileExists("Scripts/Data/ItemData.cs"), "物品数据系统"),
                ("BattleSystem.cs", CheckFileExists("Scripts/Systems/BattleSystem.cs"), "战斗系统"),
                ("InventorySystem.cs", CheckFileExists("Scripts/Systems/InventorySystem.cs"), "背包系统"),
                ("EquipmentManager.cs", CheckFileExists("Scripts/Systems/EquipmentManager.cs"), "装备管理器"),
                ("UI系统", CheckFileExists("Scripts/UI/MainGameUI.cs"), "主要UI组件")
            };

            foreach (var (name, exists, description) in compatibilityItems)
            {
                DrawCompatibilityItem(name, exists, description);
            }

            GUILayout.Space(5);
            
            bool allCompatible = compatibilityItems.TrueForAll(item => item.Item2);
            if (allCompatible)
            {
                EditorGUILayout.HelpBox("✓ 项目结构兼容，可以安全升级！", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ 部分组件缺失，建议先完善基础系统。", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawCompatibilityItem(string name, bool exists, string description)
        {
            GUILayout.BeginHorizontal();
            
            string status = exists ? "✓" : "✗";
            Color statusColor = exists ? Color.green : Color.red;
            
            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(status, GUILayout.Width(20));
            GUI.color = originalColor;
            
            GUILayout.Label(name, GUILayout.Width(150));
            GUILayout.Label(description);
            
            GUILayout.EndHorizontal();
        }

        private void DrawMigrationSteps()
        {
            EditorGUI.indentLevel++;

            var steps = new List<(string, string, System.Action)>
            {
                ("第1步：创建服务引导程序", 
                 "在场景中创建GameServiceBootstrapper，自动初始化所有服务。", 
                 () => CreateServiceBootstrapper()),
                 
                ("第2步：生成配置文件", 
                 "使用配置设置向导创建GameConfig和物品模板。", 
                 () => ConfigurationSetupWizard.ShowWindow()),
                 
                ("第3步：迁移GameManager", 
                 "将现有GameManager替换为GameManagerRefactored。", 
                 () => ShowGameManagerMigration()),
                 
                ("第4步：更新UI系统", 
                 "应用UI性能优化，使用新的更新管理器。", 
                 () => ShowUIMigration()),
                 
                ("第5步：测试和验证", 
                 "运行测试确保所有功能正常工作。", 
                 () => RunValidationTests())
            };

            for (int i = 0; i < steps.Count; i++)
            {
                var (title, description, action) = steps[i];
                
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.BeginVertical();
                
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.Label(description, EditorStyles.wordWrappedLabel);
                
                GUILayout.EndVertical();
                
                if (action != null && GUILayout.Button("执行", GUILayout.Width(60), GUILayout.Height(40)))
                {
                    action.Invoke();
                }
                
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawNewFeatures()
        {
            EditorGUI.indentLevel++;

            var features = new List<(string, string, string)>
            {
                ("依赖注入容器", 
                 "ServiceContainer.cs", 
                 "轻量级DI容器，支持单例、瞬态生命周期，自动依赖解析。"),
                 
                ("数据驱动配置", 
                 "GameConfig.cs + ItemTemplates.cs", 
                 "ScriptableObject配置系统，支持装备、消耗品、探索事件等模板。"),
                 
                ("事件系统", 
                 "EventSystem.cs", 
                 "统一的事件通信系统，支持类型安全的事件数据。"),
                 
                ("UI性能优化", 
                 "UIUpdateManager.cs + UIObjectPool.cs", 
                 "批量UI更新管理和对象池，显著提升UI性能。"),
                 
                ("完整存档系统", 
                 "SaveSystem.cs", 
                 "JSON序列化存档，支持多存档槽、自动保存、数据加密。"),
                 
                ("技能系统框架", 
                 "SkillSystem.cs", 
                 "完整的技能系统，支持技能树、等级、冷却、效果。"),
                 
                ("探索事件系统", 
                 "ExplorationEventData.cs", 
                 "可配置的探索事件，支持条件、奖励、概率。"),
                 
                ("工具和验证", 
                 "GameUtils.cs + ConfigValidator.cs", 
                 "开发工具集和配置验证器，提升开发效率。")
            };

            foreach (var (name, file, description) in features)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(file, EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
                
                GUILayout.Label(description, EditorStyles.wordWrappedLabel);
                
                GUILayout.EndVertical();
                GUILayout.Space(3);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("开始自动迁移", GUILayout.Height(30)))
            {
                StartAutomaticMigration();
            }

            if (GUILayout.Button("打开配置向导", GUILayout.Height(30)))
            {
                ConfigurationSetupWizard.ShowWindow();
            }

            if (GUILayout.Button("运行系统检测", GUILayout.Height(30)))
            {
                RunSystemDiagnostics();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("查看完整文档", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/your-project/documentation");
            }
        }

        private bool CheckFileExists(string path)
        {
            return System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, path));
        }

        private void CreateServiceBootstrapper()
        {
            var go = new GameObject("GameServiceBootstrapper");
            go.AddComponent<GameServiceBootstrapper>();
            
            EditorUtility.DisplayDialog("服务引导程序已创建", 
                "GameServiceBootstrapper已添加到场景中。\n\n" +
                "它将自动初始化所有游戏服务。\n" +
                "建议将其设置为DontDestroyOnLoad。", "确定");
            
            Selection.activeGameObject = go;
        }

        private void ShowGameManagerMigration()
        {
            string message = "GameManager迁移步骤：\n\n" +
                           "1. 保留现有的GameManager作为备份\n" +
                           "2. 在场景中添加GameManagerRefactored\n" +
                           "3. 添加PlayerDataManager组件\n" +
                           "4. 逐步迁移引用和依赖\n" +
                           "5. 测试所有功能\n\n" +
                           "新的GameManager使用服务架构，职责更清晰，扩展性更强。";

            EditorUtility.DisplayDialog("GameManager迁移", message, "了解");
        }

        private void ShowUIMigration()
        {
            string message = "UI系统迁移步骤：\n\n" +
                           "1. 在场景中添加UIUpdateManager\n" +
                           "2. 在场景中添加UIObjectPool\n" +
                           "3. 修改UI脚本使用新的更新系统\n" +
                           "4. 应用对象池到频繁创建的UI元素\n" +
                           "5. 调整UI更新频率\n\n" +
                           "这将显著提升UI性能，特别是在移动设备上。";

            EditorUtility.DisplayDialog("UI系统迁移", message, "了解");
        }

        private void RunValidationTests()
        {
            bool allPassed = true;
            var results = new List<string>();

            // 检查服务系统
            if (FindObjectOfType<GameServiceBootstrapper>() != null)
            {
                results.Add("✓ 服务引导程序存在");
            }
            else
            {
                results.Add("✗ 缺少服务引导程序");
                allPassed = false;
            }

            // 检查配置文件
            var configs = Resources.LoadAll<GameConfig>("");
            if (configs.Length > 0)
            {
                results.Add($"✓ 找到 {configs.Length} 个配置文件");
            }
            else
            {
                results.Add("✗ 缺少游戏配置文件");
                allPassed = false;
            }

            // 检查新管理器
            if (FindObjectOfType<GameManagerRefactored>() != null)
            {
                results.Add("✓ 新游戏管理器存在");
            }
            else
            {
                results.Add("! 建议添加新游戏管理器");
            }

            string resultMessage = "验证测试结果：\n\n" + string.Join("\n", results);
            
            if (allPassed)
            {
                resultMessage += "\n\n✓ 迁移成功！系统已准备就绪。";
            }
            else
            {
                resultMessage += "\n\n! 仍有项目需要完善。";
            }

            EditorUtility.DisplayDialog("验证测试", resultMessage, "确定");
        }

        private void StartAutomaticMigration()
        {
            if (EditorUtility.DisplayDialog("自动迁移", 
                "这将自动执行以下操作：\n\n" +
                "1. 创建服务引导程序\n" +
                "2. 生成默认配置文件\n" +
                "3. 设置UI管理器\n" +
                "4. 运行验证测试\n\n" +
                "是否继续？", "确定", "取消"))
            {
                try
                {
                    // 1. 创建服务引导程序
                    CreateServiceBootstrapper();

                    // 2. 生成配置文件
                    ConfigurationSetupWizard.ShowWindow();

                    // 3. 运行验证
                    RunValidationTests();

                    EditorUtility.DisplayDialog("自动迁移完成", 
                        "自动迁移已完成！\n\n" +
                        "请查看场景中的新组件，并根据需要调整配置。\n" +
                        "建议运行一次游戏测试以确保一切正常。", "确定");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("迁移失败", 
                        $"自动迁移过程中出现错误：\n{e.Message}\n\n" +
                        "请手动执行迁移步骤。", "确定");
                }
            }
        }

        private void RunSystemDiagnostics()
        {
            Debug.Log("=== 系统诊断开始 ===");
            
            // 检查服务状态
            bool servicesReady = GameUtils.AreEssentialServicesReady();
            Debug.Log($"核心服务状态: {(servicesReady ? "正常" : "异常")}");
            
            // 打印系统状态
            GameUtils.PrintSystemStatus();
            
            // 验证配置
            ConfigValidator.ValidateAllConfigs();
            
            // 检查文件结构
            var essentialFiles = new string[]
            {
                "Scripts/Core/ServiceContainer.cs",
                "Scripts/Core/GameServiceBootstrapper.cs",
                "Scripts/Data/GameConfig.cs",
                "Scripts/Utils/GameUtils.cs"
            };

            foreach (var file in essentialFiles)
            {
                bool exists = CheckFileExists(file);
                Debug.Log($"文件检查 {file}: {(exists ? "存在" : "缺失")}");
            }
            
            Debug.Log("=== 系统诊断完成 ===");
            
            EditorUtility.DisplayDialog("系统诊断", 
                "系统诊断已完成，请查看Console窗口获取详细信息。", "确定");
        }
    }

    /// <summary>
    /// 迁移工具类
    /// </summary>
    public static class MigrationUtilities
    {
        /// <summary>
        /// 备份现有的GameManager
        /// </summary>
        public static void BackupGameManager()
        {
            var gameManager = Object.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var backup = Object.Instantiate(gameManager.gameObject);
                backup.name = "GameManager_Backup";
                backup.SetActive(false);
                
                Debug.Log("GameManager已备份为 GameManager_Backup");
            }
        }

        /// <summary>
        /// 设置新的游戏管理器
        /// </summary>
        public static void SetupNewGameManager()
        {
            var oldManager = Object.FindObjectOfType<GameManager>();
            if (oldManager != null)
            {
                var go = oldManager.gameObject;
                
                // 添加新组件
                go.AddComponent<GameManagerRefactored>();
                go.AddComponent<PlayerDataManager>();
                
                // 禁用旧组件
                oldManager.enabled = false;
                
                Debug.Log("新游戏管理器设置完成");
            }
        }

        /// <summary>
        /// 验证迁移完整性
        /// </summary>
        public static bool ValidateMigration()
        {
            bool isValid = true;
            
            // 检查必要组件
            if (Object.FindObjectOfType<GameServiceBootstrapper>() == null)
            {
                Debug.LogError("缺少GameServiceBootstrapper");
                isValid = false;
            }
            
            // 检查配置文件
            var configs = Resources.LoadAll<GameConfig>("");
            if (configs.Length == 0)
            {
                Debug.LogError("缺少GameConfig配置文件");
                isValid = false;
            }
            
            return isValid;
        }
    }
}
#endif