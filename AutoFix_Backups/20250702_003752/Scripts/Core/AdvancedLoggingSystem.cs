using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Advanced Logging System for VR Boxing Game
    /// Provides persistent logging, filtering, performance tracking, and debugging capabilities
    /// </summary>
    public class AdvancedLoggingSystem : MonoBehaviour
    {
        [Header("Logging Configuration")]
        public LogLevel minimumLogLevel = LogLevel.Info;
        public bool enableFileLogging = true;
        public bool enableConsoleLogging = true;
        public bool enablePerformanceLogging = true;
        public bool enableSessionTracking = true;
        
        [Header("File Settings")]
        public int maxLogFileSize = 10; // MB
        public int maxLogFiles = 5;
        
        [Header("Performance Monitoring")]
        public float performanceLogInterval = 5f; // seconds
        
        // Singleton
        public static AdvancedLoggingSystem Instance { get; private set; }
        
        // Log levels
        public enum LogLevel
        {
            Trace = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5
        }
        
        // Log categories for filtering
        public enum LogCategory
        {
            System,
            Audio,
            Boxing,
            VR,
            Performance,
            UI,
            Network,
            Input,
            Environment,
            HandTracking
        }
        
        // Log entry structure
        [Serializable]
        public class LogEntry
        {
            public DateTime timestamp;
            public LogLevel level;
            public LogCategory category;
            public string system;
            public string message;
            public string stackTrace;
            public float frameTime;
            public long memoryUsage;
            public string sessionId;
            public Vector3 playerPosition;
            public int frameCount;
        }
        
        // Session tracking
        [Serializable]
        public class GameSession
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public string unityVersion;
            public string deviceInfo;
            public List<string> criticalErrors;
            public float averageFrameRate;
            public float maxMemoryUsage;
            public int totalLogs;
        }
        
        // Private fields
        private string logDirectory;
        private string currentLogFile;
        private StringBuilder logBuffer;
        private Queue<LogEntry> recentLogs;
        private GameSession currentSession;
        private float lastPerformanceLog;
        private bool isInitialized = false;
        
        // Threading
        private readonly object logLock = new object();
        private Task currentWriteTask;
        
        // Performance tracking
        private Queue<float> frameTimeHistory;
        private float averageFrameTime;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLoggingSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeLoggingSystem()
        {
            // Setup directories
            logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Initialize collections
            logBuffer = new StringBuilder();
            recentLogs = new Queue<LogEntry>();
            frameTimeHistory = new Queue<float>();
            
            // Create session
            StartNewSession();
            
            // Setup log file
            CreateNewLogFile();
            
            // Hook Unity logging
            Application.logMessageReceived += OnUnityLogReceived;
            
            isInitialized = true;
            
            LogInfo(LogCategory.System, "AdvancedLoggingSystem", "🔍 Advanced Logging System initialized successfully!");
            LogSystemInfo();
        }
        
        private void StartNewSession()
        {
            currentSession = new GameSession
            {
                sessionId = Guid.NewGuid().ToString("N")[..8],
                startTime = DateTime.Now,
                unityVersion = Application.unityVersion,
                deviceInfo = $"{SystemInfo.deviceModel} | {SystemInfo.operatingSystem}",
                criticalErrors = new List<string>(),
                totalLogs = 0
            };
            
            LogInfo(LogCategory.System, "Session", $"🎮 New session started: {currentSession.sessionId}");
        }
        
        private void CreateNewLogFile()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            currentLogFile = Path.Combine(logDirectory, $"VRBoxing_{timestamp}_{currentSession.sessionId}.log");
            
            // Clean old log files
            CleanOldLogFiles();
        }
        
        private void CleanOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "*.log");
                Array.Sort(logFiles);
                
                if (logFiles.Length > maxLogFiles)
                {
                    for (int i = 0; i < logFiles.Length - maxLogFiles; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
                
                // Check file sizes and rotate if needed
                if (File.Exists(currentLogFile))
                {
                    var fileInfo = new FileInfo(currentLogFile);
                    if (fileInfo.Length > maxLogFileSize * 1024 * 1024) // MB to bytes
                    {
                        CreateNewLogFile();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error cleaning log files: {ex.Message}");
            }
        }
        
        private void LogSystemInfo()
        {
            LogInfo(LogCategory.System, "SystemInfo", $"📱 Device: {SystemInfo.deviceModel}");
            LogInfo(LogCategory.System, "SystemInfo", $"🖥️ OS: {SystemInfo.operatingSystem}");
            LogInfo(LogCategory.System, "SystemInfo", $"🎮 Unity: {Application.unityVersion}");
            LogInfo(LogCategory.System, "SystemInfo", $"💾 Memory: {SystemInfo.systemMemorySize} MB");
            LogInfo(LogCategory.System, "SystemInfo", $"🎯 Target FPS: {Application.targetFrameRate}");
            LogInfo(LogCategory.System, "SystemInfo", $"🔧 Graphics: {SystemInfo.graphicsDeviceName}");
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Track performance
            TrackPerformance();
            
            // Log performance periodically
            if (enablePerformanceLogging && Time.time - lastPerformanceLog >= performanceLogInterval)
            {
                LogPerformanceMetrics();
                lastPerformanceLog = Time.time;
            }
        }
        
        private void TrackPerformance()
        {
            // Track frame time
            float frameTime = Time.unscaledDeltaTime;
            frameTimeHistory.Enqueue(frameTime);
            
            if (frameTimeHistory.Count > 60) // Keep last 60 frames
            {
                frameTimeHistory.Dequeue();
            }
            
            // Calculate average
            float total = 0f;
            foreach (float ft in frameTimeHistory)
            {
                total += ft;
            }
            averageFrameTime = total / frameTimeHistory.Count;
        }
        
        private void LogPerformanceMetrics()
        {
            float fps = 1f / averageFrameTime;
            long memoryUsage = GC.GetTotalMemory(false);
            
            LogTrace(LogCategory.Performance, "Metrics", 
                $"📊 FPS: {fps:F1} | Frame: {averageFrameTime * 1000:F1}ms | Memory: {memoryUsage / 1024 / 1024:F1}MB");
            
            // Update session stats
            currentSession.averageFrameRate = fps;
            currentSession.maxMemoryUsage = Math.Max(currentSession.maxMemoryUsage, memoryUsage);
        }
        
        // Public logging methods
        public static void LogTrace(LogCategory category, string system, string message)
        {
            Instance?.Log(LogLevel.Trace, category, system, message);
        }
        
        public static void LogDebug(LogCategory category, string system, string message)
        {
            Instance?.Log(LogLevel.Debug, category, system, message);
        }
        
        public static void LogInfo(LogCategory category, string system, string message)
        {
            Instance?.Log(LogLevel.Info, category, system, message);
        }
        
        public static void LogWarning(LogCategory category, string system, string message)
        {
            Instance?.Log(LogLevel.Warning, category, system, message);
        }
        
        public static void LogError(LogCategory category, string system, string message, Exception exception = null)
        {
            string fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
            Instance?.Log(LogLevel.Error, category, system, fullMessage);
        }
        
        public static void LogCritical(LogCategory category, string system, string message, Exception exception = null)
        {
            string fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
            Instance?.Log(LogLevel.Critical, category, system, fullMessage);
            
            // Track critical errors
            if (Instance != null)
            {
                Instance.currentSession.criticalErrors.Add($"{DateTime.Now:HH:mm:ss} - {system}: {message}");
            }
        }
        
        private void Log(LogLevel level, LogCategory category, string system, string message)
        {
            if (!isInitialized || level < minimumLogLevel) return;
            
            lock (logLock)
            {
                var entry = new LogEntry
                {
                    timestamp = DateTime.Now,
                    level = level,
                    category = category,
                    system = system,
                    message = message,
                    stackTrace = level >= LogLevel.Error ? Environment.StackTrace : null,
                    frameTime = averageFrameTime,
                    memoryUsage = GC.GetTotalMemory(false),
                    sessionId = currentSession.sessionId,
                    playerPosition = VRCameraHelper.PlayerPosition,
                    frameCount = Time.frameCount
                };
                
                // Add to recent logs
                recentLogs.Enqueue(entry);
                if (recentLogs.Count > 1000) // Keep last 1000 logs in memory
                {
                    recentLogs.Dequeue();
                }
                
                // Console logging
                if (enableConsoleLogging)
                {
                    LogToConsole(entry);
                }
                
                // File logging
                if (enableFileLogging)
                {
                    LogToFile(entry);
                }
                
                currentSession.totalLogs++;
            }
        }
        
        private void LogToConsole(LogEntry entry)
        {
            string formattedMessage = FormatLogMessage(entry, false);
            
            switch (entry.level)
            {
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                default:
                    Debug.Log(formattedMessage);
                    break;
            }
        }
        
        private void LogToFile(LogEntry entry)
        {
            try
            {
                string formattedMessage = FormatLogMessage(entry, true);
                
                // Async file writing to avoid blocking main thread
                if (currentWriteTask == null || currentWriteTask.IsCompleted)
                {
                    currentWriteTask = Task.Run(() => WriteToFile(formattedMessage));
                }
                else
                {
                    // Queue the message if previous write is still running
                    logBuffer.AppendLine(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing to log file: {ex.Message}");
            }
        }
        
        private async Task WriteToFile(string message)
        {
            try
            {
                await File.AppendAllTextAsync(currentLogFile, message + Environment.NewLine);
                
                // Write any queued messages
                if (logBuffer.Length > 0)
                {
                    await File.AppendAllTextAsync(currentLogFile, logBuffer.ToString());
                    logBuffer.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write log to file: {ex.Message}");
            }
        }
        
        private string FormatLogMessage(LogEntry entry, bool includeDetails)
        {
            string levelIcon = entry.level switch
            {
                LogLevel.Trace => "🔍",
                LogLevel.Debug => "🐛",
                LogLevel.Info => "ℹ️",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🚨",
                _ => "📝"
            };
            
            string baseMessage = $"{entry.timestamp:HH:mm:ss.fff} {levelIcon} [{entry.level}] [{entry.category}] {entry.system}: {entry.message}";
            
            if (includeDetails)
            {
                baseMessage += $" | Frame: {entry.frameCount} | FPS: {1f / entry.frameTime:F1} | Mem: {entry.memoryUsage / 1024 / 1024:F1}MB | Pos: {entry.playerPosition}";
                
                if (entry.stackTrace != null)
                {
                    baseMessage += $"\nStack Trace:\n{entry.stackTrace}";
                }
            }
            
            return baseMessage;
        }
        
        private void OnUnityLogReceived(string logString, string stackTrace, LogType type)
        {
            // Convert Unity logs to our system
            LogLevel level = type switch
            {
                LogType.Error => LogLevel.Error,
                LogType.Exception => LogLevel.Critical,
                LogType.Warning => LogLevel.Warning,
                LogType.Assert => LogLevel.Error,
                _ => LogLevel.Debug
            };
            
            // Only process if it's not from our own system (prevent recursion)
            if (!logString.Contains("[AdvancedLoggingSystem]"))
            {
                Log(level, LogCategory.System, "Unity", logString);
            }
        }
        
        // Public utility methods
        public static List<LogEntry> GetRecentLogs(int count = 100)
        {
            var logs = new List<LogEntry>();
            if (Instance != null)
            {
                lock (Instance.logLock)
                {
                    logs.AddRange(Instance.recentLogs);
                }
            }
            return logs.TakeLast(count).ToList();
        }
        
        public static List<LogEntry> GetLogsByCategory(LogCategory category, int count = 50)
        {
            return GetRecentLogs(1000).Where(log => log.category == category).TakeLast(count).ToList();
        }
        
        public static List<LogEntry> GetErrorLogs(int count = 20)
        {
            return GetRecentLogs(1000).Where(log => log.level >= LogLevel.Error).TakeLast(count).ToList();
        }
        
        public static string GetSessionReport()
        {
            if (Instance?.currentSession == null) return "No active session";
            
            var session = Instance.currentSession;
            var report = new StringBuilder();
            
            report.AppendLine("=== VR Boxing Game Session Report ===");
            report.AppendLine($"Session ID: {session.sessionId}");
            report.AppendLine($"Duration: {DateTime.Now - session.startTime:hh\\:mm\\:ss}");
            report.AppendLine($"Device: {session.deviceInfo}");
            report.AppendLine($"Unity: {session.unityVersion}");
            report.AppendLine($"Average FPS: {session.averageFrameRate:F1}");
            report.AppendLine($"Max Memory: {session.maxMemoryUsage / 1024 / 1024:F1} MB");
            report.AppendLine($"Total Logs: {session.totalLogs}");
            report.AppendLine($"Critical Errors: {session.criticalErrors.Count}");
            
            if (session.criticalErrors.Count > 0)
            {
                report.AppendLine("\n=== Critical Errors ===");
                foreach (var error in session.criticalErrors)
                {
                    report.AppendLine($"- {error}");
                }
            }
            
            return report.ToString();
        }
        
        public static void ExportLogsToFile(string filename = null)
        {
            if (Instance == null) return;
            
            filename ??= $"VRBoxing_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            string exportPath = Path.Combine(Instance.logDirectory, filename);
            
            try
            {
                var allLogs = GetRecentLogs(1000);
                using var writer = new StreamWriter(exportPath);
                
                writer.WriteLine(GetSessionReport());
                writer.WriteLine("\n=== Detailed Logs ===");
                
                foreach (var log in allLogs)
                {
                    writer.WriteLine(Instance.FormatLogMessage(log, true));
                }
                
                LogInfo(LogCategory.System, "LogExport", $"📄 Logs exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                LogError(LogCategory.System, "LogExport", $"Failed to export logs: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Finalize session
                if (currentSession != null)
                {
                    currentSession.endTime = DateTime.Now;
                    LogInfo(LogCategory.System, "Session", $"🏁 Session ended: {currentSession.sessionId}");
                }
                
                // Wait for any pending writes
                currentWriteTask?.Wait(1000);
                
                // Unhook Unity logging
                Application.logMessageReceived -= OnUnityLogReceived;
                
                LogInfo(LogCategory.System, "AdvancedLoggingSystem", "🔍 Advanced Logging System shutdown complete");
            }
        }
    }
} 