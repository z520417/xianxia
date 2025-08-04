using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XianXiaGame
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,    // 详细信息
        Debug = 1,      // 调试信息
        Info = 2,       // 一般信息
        Warning = 3,    // 警告
        Error = 4,      // 错误
        Critical = 5    // 严重错误
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        public string Message;
        public LogLevel Level;
        public string Category;
        public string StackTrace;
        public float Timestamp;
        public string FormattedTime;

        public LogEntry(string message, LogLevel level, string category, string stackTrace = "")
        {
            Message = message;
            Level = level;
            Category = category;
            StackTrace = stackTrace;
            Timestamp = Time.time;
            FormattedTime = DateTime.Now.ToString("HH:mm:ss.fff");
        }

        public override string ToString()
        {
            return $"[{FormattedTime}] [{Level}] [{Category}] {Message}";
        }

        public string ToDetailedString()
        {
            string result = ToString();
            if (!string.IsNullOrEmpty(StackTrace))
            {
                result += $"\nStackTrace:\n{StackTrace}";
            }
            return result;
        }
    }

    /// <summary>
    /// 游戏日志系统
    /// 提供统一的日志记录、文件输出和运行时查看功能
    /// </summary>
    public class LoggingSystem : MonoBehaviour
    {
        private static LoggingSystem s_Instance;
        public static LoggingSystem Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<LoggingSystem>();
                    if (s_Instance == null)
                    {
                        GameObject go = new GameObject("LoggingSystem");
                        s_Instance = go.AddComponent<LoggingSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_Instance;
            }
        }

        [Header("日志配置")]
        [SerializeField] private LogLevel m_MinLogLevel = LogLevel.Debug;
        [SerializeField] private bool m_WriteToFile = true;
        [SerializeField] private bool m_WriteToUnityConsole = true;
        [SerializeField] private int m_MaxLogEntries = 1000;

        [Header("文件输出配置")]
        [SerializeField] private bool m_EnableFileRotation = true;
        [SerializeField] private float m_MaxFileSize = 10f; // MB
        [SerializeField] private int m_MaxLogFiles = 5;

        [Header("性能配置")]
        [SerializeField] private int m_MaxLogsPerFrame = 10;
        [SerializeField] private bool m_AsyncFileWriting = true;

        // 日志存储
        private List<LogEntry> m_LogEntries = new List<LogEntry>();
        private Queue<LogEntry> m_PendingFileWrites = new Queue<LogEntry>();

        // 文件路径
        private string m_LogDirectory;
        private string m_CurrentLogFile;

        // 性能统计
        private int m_LogsThisFrame = 0;
        private float m_LastFrameTime = 0f;

        // 事件
        public event Action<LogEntry> OnLogEntryAdded;

        // 日志级别颜色映射
        private readonly Dictionary<LogLevel, Color> m_LogColors = new Dictionary<LogLevel, Color>
        {
            { LogLevel.Verbose, Color.gray },
            { LogLevel.Debug, Color.white },
            { LogLevel.Info, Color.cyan },
            { LogLevel.Warning, Color.yellow },
            { LogLevel.Error, Color.red },
            { LogLevel.Critical, new Color(1f, 0f, 0f, 1f) }
        };

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogging();
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            ResetFrameLogCounter();
            ProcessPendingFileWrites();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushPendingLogs();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushPendingLogs();
            }
        }

        private void OnDestroy()
        {
            FlushPendingLogs();
        }

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        private void InitializeLogging()
        {
            // 设置日志目录
            m_LogDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            
            if (!Directory.Exists(m_LogDirectory))
            {
                Directory.CreateDirectory(m_LogDirectory);
            }

            // 创建新的日志文件
            CreateNewLogFile();

            // 注册Unity日志回调
            if (m_WriteToUnityConsole)
            {
                Application.logMessageReceived += OnUnityLogMessageReceived;
            }

            Log("日志系统初始化完成", LogLevel.Info, "LoggingSystem");
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, string category = "Game")
        {
            Instance?.LogInternal(message, level, category);
        }

        /// <summary>
        /// 记录详细日志（包含堆栈跟踪）
        /// </summary>
        public static void LogDetailed(string message, LogLevel level = LogLevel.Info, string category = "Game")
        {
            string stackTrace = level >= LogLevel.Warning ? Environment.StackTrace : "";
            Instance?.LogInternal(message, level, category, stackTrace);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        public static void LogException(Exception exception, string category = "Game")
        {
            string message = $"异常: {exception.Message}";
            Instance?.LogInternal(message, LogLevel.Error, category, exception.StackTrace);
        }

        /// <summary>
        /// 记录格式化日志
        /// </summary>
        public static void LogFormat(LogLevel level, string category, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                Log(message, level, category);
            }
            catch (FormatException e)
            {
                Log($"日志格式化错误: {e.Message}", LogLevel.Error, "LoggingSystem");
            }
        }

        /// <summary>
        /// 设置最小日志级别
        /// </summary>
        public void SetMinLogLevel(LogLevel minLevel)
        {
            m_MinLogLevel = minLevel;
            Log($"最小日志级别设置为: {minLevel}", LogLevel.Info, "LoggingSystem");
        }

        /// <summary>
        /// 获取所有日志条目
        /// </summary>
        public List<LogEntry> GetLogEntries(LogLevel? minLevel = null, string category = null)
        {
            List<LogEntry> filteredLogs = new List<LogEntry>();

            foreach (var entry in m_LogEntries)
            {
                bool levelMatch = minLevel == null || entry.Level >= minLevel;
                bool categoryMatch = string.IsNullOrEmpty(category) || entry.Category == category;

                if (levelMatch && categoryMatch)
                {
                    filteredLogs.Add(entry);
                }
            }

            return filteredLogs;
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void ClearLogs()
        {
            m_LogEntries.Clear();
            Log("日志已清空", LogLevel.Info, "LoggingSystem");
        }

        /// <summary>
        /// 导出日志到文件
        /// </summary>
        public void ExportLogs(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"游戏日志导出 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine("=" + new string('=', 50));
                    writer.WriteLine();

                    foreach (var entry in m_LogEntries)
                    {
                        writer.WriteLine(entry.ToDetailedString());
                        writer.WriteLine();
                    }
                }

                Log($"日志已导出到: {filePath}", LogLevel.Info, "LoggingSystem");
            }
            catch (Exception e)
            {
                Log($"导出日志失败: {e.Message}", LogLevel.Error, "LoggingSystem");
            }
        }

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        public Dictionary<LogLevel, int> GetLogStatistics()
        {
            var stats = new Dictionary<LogLevel, int>();
            
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                stats[level] = 0;
            }

            foreach (var entry in m_LogEntries)
            {
                stats[entry.Level]++;
            }

            return stats;
        }

        private void LogInternal(string message, LogLevel level, string category, string stackTrace = "")
        {
            // 检查日志级别
            if (level < m_MinLogLevel)
                return;

            // 检查性能限制
            if (m_LogsThisFrame >= m_MaxLogsPerFrame)
                return;

            // 创建日志条目
            var logEntry = new LogEntry(message, level, category, stackTrace);

            // 添加到内存存储
            AddLogEntry(logEntry);

            // 输出到Unity控制台
            if (m_WriteToUnityConsole)
            {
                LogToUnityConsole(logEntry);
            }

            // 添加到文件写入队列
            if (m_WriteToFile)
            {
                if (m_AsyncFileWriting)
                {
                    m_PendingFileWrites.Enqueue(logEntry);
                }
                else
                {
                    WriteLogToFile(logEntry);
                }
            }

            // 触发事件
            OnLogEntryAdded?.Invoke(logEntry);

            m_LogsThisFrame++;
        }

        private void AddLogEntry(LogEntry logEntry)
        {
            m_LogEntries.Add(logEntry);

            // 限制内存中的日志数量
            while (m_LogEntries.Count > m_MaxLogEntries)
            {
                m_LogEntries.RemoveAt(0);
            }
        }

        private void LogToUnityConsole(LogEntry logEntry)
        {
            string coloredMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(m_LogColors[logEntry.Level])}>{logEntry}</color>";

            switch (logEntry.Level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(coloredMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(coloredMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(coloredMessage);
                    break;
            }
        }

        private void WriteLogToFile(LogEntry logEntry)
        {
            try
            {
                if (string.IsNullOrEmpty(m_CurrentLogFile))
                {
                    CreateNewLogFile();
                }

                // 检查文件大小，必要时轮转
                if (m_EnableFileRotation && ShouldRotateLogFile())
                {
                    RotateLogFile();
                }

                // 写入日志
                using (StreamWriter writer = new StreamWriter(m_CurrentLogFile, true))
                {
                    writer.WriteLine(logEntry.ToDetailedString());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"写入日志文件失败: {e.Message}");
            }
        }

        private void ProcessPendingFileWrites()
        {
            int writesThisFrame = 0;
            const int maxWritesPerFrame = 5;

            while (m_PendingFileWrites.Count > 0 && writesThisFrame < maxWritesPerFrame)
            {
                var logEntry = m_PendingFileWrites.Dequeue();
                WriteLogToFile(logEntry);
                writesThisFrame++;
            }
        }

        private void FlushPendingLogs()
        {
            while (m_PendingFileWrites.Count > 0)
            {
                var logEntry = m_PendingFileWrites.Dequeue();
                WriteLogToFile(logEntry);
            }
        }

        private void CreateNewLogFile()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            m_CurrentLogFile = Path.Combine(m_LogDirectory, $"game_log_{timestamp}.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(m_CurrentLogFile))
                {
                    writer.WriteLine($"仙侠游戏日志 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Unity版本: {Application.unityVersion}");
                    writer.WriteLine($"平台: {Application.platform}");
                    writer.WriteLine($"设备信息: {SystemInfo.deviceModel}");
                    writer.WriteLine("=" + new string('=', 50));
                    writer.WriteLine();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"创建日志文件失败: {e.Message}");
            }
        }

        private bool ShouldRotateLogFile()
        {
            if (!File.Exists(m_CurrentLogFile))
                return false;

            var fileInfo = new FileInfo(m_CurrentLogFile);
            return fileInfo.Length > m_MaxFileSize * 1024 * 1024; // 转换为字节
        }

        private void RotateLogFile()
        {
            // 删除旧文件
            CleanupOldLogFiles();
            
            // 创建新文件
            CreateNewLogFile();
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(m_LogDirectory, "game_log_*.txt");
                
                if (logFiles.Length >= m_MaxLogFiles)
                {
                    Array.Sort(logFiles, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));
                    
                    int filesToDelete = logFiles.Length - m_MaxLogFiles + 1;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"清理旧日志文件失败: {e.Message}");
            }
        }

        private void ResetFrameLogCounter()
        {
            if (Time.time - m_LastFrameTime >= 1f)
            {
                m_LogsThisFrame = 0;
                m_LastFrameTime = Time.time;
            }
        }

        private void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // 避免递归日志
            if (condition.Contains("LoggingSystem"))
                return;

            LogLevel level = LogLevel.Debug;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    level = LogLevel.Error;
                    break;
                case LogType.Assert:
                    level = LogLevel.Critical;
                    break;
                case LogType.Warning:
                    level = LogLevel.Warning;
                    break;
                case LogType.Log:
                    level = LogLevel.Info;
                    break;
            }

            LogInternal(condition, level, "Unity", stackTrace);
        }

#if UNITY_EDITOR
        [ContextMenu("测试所有日志级别")]
        private void TestAllLogLevels()
        {
            Log("这是详细信息", LogLevel.Verbose, "Test");
            Log("这是调试信息", LogLevel.Debug, "Test");
            Log("这是一般信息", LogLevel.Info, "Test");
            Log("这是警告信息", LogLevel.Warning, "Test");
            Log("这是错误信息", LogLevel.Error, "Test");
            Log("这是严重错误", LogLevel.Critical, "Test");
        }

        [ContextMenu("打印日志统计")]
        private void PrintLogStatistics()
        {
            var stats = GetLogStatistics();
            Debug.Log("=== 日志统计 ===");
            foreach (var kvp in stats)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} 条");
            }
            Debug.Log($"总计: {m_LogEntries.Count} 条日志");
        }

        [ContextMenu("导出日志")]
        private void ExportLogsInEditor()
        {
            string filePath = Path.Combine(Application.dataPath, "exported_logs.txt");
            ExportLogs(filePath);
        }
#endif
    }

    /// <summary>
    /// 静态日志便捷方法
    /// </summary>
    public static class GameLog
    {
        public static void Verbose(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Verbose, category);
        public static void Debug(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Debug, category);
        public static void Info(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Info, category);
        public static void Warning(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Warning, category);
        public static void Error(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Error, category);
        public static void Critical(string message, string category = "Game") => LoggingSystem.Log(message, LogLevel.Critical, category);
        
        public static void Exception(Exception exception, string category = "Game") => LoggingSystem.LogException(exception, category);
        
        public static void Format(LogLevel level, string category, string format, params object[] args) => 
            LoggingSystem.LogFormat(level, category, format, args);
    }
}