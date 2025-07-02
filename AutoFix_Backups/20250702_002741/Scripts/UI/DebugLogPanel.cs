using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRBoxingGame.Core;
using TMPro;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// In-Game Debug Log Panel for VR Boxing Game
    /// Provides real-time log viewing, system status, and performance metrics
    /// </summary>
    public class DebugLogPanel : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas debugCanvas;
        public TextMeshProUGUI logTextDisplay;
        public TextMeshProUGUI systemStatusDisplay;
        public TextMeshProUGUI performanceDisplay;
        public Button clearLogsButton;
        public Button exportLogsButton;
        public Button toggleSystemStatusButton;
        public Button togglePerformanceButton;
        public Dropdown logLevelFilter;
        public Dropdown categoryFilter;
        public Slider logCountSlider;
        public Toggle autoScrollToggle;
        
        [Header("Display Settings")]
        public bool showOnStart = false;
        public int maxDisplayedLogs = 50;
        public float updateInterval = 0.5f;
        public Color errorColor = Color.red;
        public Color warningColor = Color.yellow;
        public Color infoColor = Color.white;
        public Color debugColor = Color.gray;
        
        [Header("Panel Layout")]
        public RectTransform logPanel;
        public RectTransform statusPanel;
        public RectTransform performancePanel;
        
        // Private fields
        private List<AdvancedLoggingSystem.LogEntry> displayedLogs;
        private AdvancedLoggingSystem.LogLevel selectedLogLevel = AdvancedLoggingSystem.LogLevel.Info;
        private AdvancedLoggingSystem.LogCategory selectedCategory = AdvancedLoggingSystem.LogCategory.System;
        private bool showSystemStatus = true;
        private bool showPerformance = true;
        private bool isVisible = false;
        private float lastUpdateTime;
        private ScrollRect logScrollRect;
        
        // Keyboard shortcuts
        private bool ctrlPressed = false;
        
        private void Start()
        {
            InitializeUI();
            
            if (showOnStart)
            {
                ShowPanel();
            }
            else
            {
                HidePanel();
            }
        }
        
        private void InitializeUI()
        {
            displayedLogs = new List<AdvancedLoggingSystem.LogEntry>();
            
            // Setup UI components
            SetupButtons();
            SetupDropdowns();
            SetupSliders();
            SetupScrollRect();
            
            // Initialize display
            UpdateLogDisplay();
            UpdateSystemStatus();
            UpdatePerformanceDisplay();
        }
        
        private void SetupButtons()
        {
            if (clearLogsButton != null)
                clearLogsButton.onClick.AddListener(ClearLogs);
            
            if (exportLogsButton != null)
                exportLogsButton.onClick.AddListener(ExportLogs);
            
            if (toggleSystemStatusButton != null)
                toggleSystemStatusButton.onClick.AddListener(ToggleSystemStatus);
            
            if (togglePerformanceButton != null)
                togglePerformanceButton.onClick.AddListener(TogglePerformance);
        }
        
        private void SetupDropdowns()
        {
            // Log level filter
            if (logLevelFilter != null)
            {
                logLevelFilter.ClearOptions();
                var levelOptions = new List<string>();
                foreach (AdvancedLoggingSystem.LogLevel level in System.Enum.GetValues(typeof(AdvancedLoggingSystem.LogLevel)))
                {
                    levelOptions.Add(level.ToString());
                }
                logLevelFilter.AddOptions(levelOptions);
                logLevelFilter.value = (int)selectedLogLevel;
                logLevelFilter.onValueChanged.AddListener(OnLogLevelChanged);
            }
            
            // Category filter
            if (categoryFilter != null)
            {
                categoryFilter.ClearOptions();
                var categoryOptions = new List<string> { "All" };
                foreach (AdvancedLoggingSystem.LogCategory category in System.Enum.GetValues(typeof(AdvancedLoggingSystem.LogCategory)))
                {
                    categoryOptions.Add(category.ToString());
                }
                categoryFilter.AddOptions(categoryOptions);
                categoryFilter.onValueChanged.AddListener(OnCategoryChanged);
            }
        }
        
        private void SetupSliders()
        {
            if (logCountSlider != null)
            {
                logCountSlider.minValue = 10;
                logCountSlider.maxValue = 200;
                logCountSlider.value = maxDisplayedLogs;
                logCountSlider.onValueChanged.AddListener(OnLogCountChanged);
            }
        }
        
        private void SetupScrollRect()
        {
            logScrollRect = logTextDisplay?.GetComponentInParent<ScrollRect>();
        }
        
        private void Update()
        {
            // Handle keyboard shortcuts
            HandleKeyboardInput();
            
            // Update display periodically
            if (isVisible && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateLogDisplay();
                UpdateSystemStatus();
                UpdatePerformanceDisplay();
                lastUpdateTime = Time.time;
            }
        }
        
        private void HandleKeyboardInput()
        {
            // Check for modifier keys
            ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            
            // Toggle panel visibility (Ctrl + ~)
            if (ctrlPressed && Input.GetKeyDown(KeyCode.BackQuote))
            {
                TogglePanel();
            }
            
            // Quick actions
            if (isVisible && ctrlPressed)
            {
                if (Input.GetKeyDown(KeyCode.C)) ClearLogs();
                if (Input.GetKeyDown(KeyCode.E)) ExportLogs();
                if (Input.GetKeyDown(KeyCode.S)) ToggleSystemStatus();
                if (Input.GetKeyDown(KeyCode.P)) TogglePerformance();
            }
        }
        
        private void UpdateLogDisplay()
        {
            if (logTextDisplay == null) return;
            
            // Get filtered logs
            var logs = GetFilteredLogs();
            
            // Build display text
            var displayText = new StringBuilder();
            
            foreach (var log in logs.TakeLast(maxDisplayedLogs))
            {
                string colorTag = GetColorTagForLogLevel(log.level);
                string icon = GetIconForLogLevel(log.level);
                
                displayText.AppendLine($"{colorTag}{log.timestamp:HH:mm:ss.ff} {icon} [{log.category}] {log.system}: {log.message}</color>");
            }
            
            logTextDisplay.text = displayText.ToString();
            
            // Auto-scroll to bottom
            if (autoScrollToggle != null && autoScrollToggle.isOn && logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private List<AdvancedLoggingSystem.LogEntry> GetFilteredLogs()
        {
            var allLogs = AdvancedLoggingSystem.GetRecentLogs(500);
            
            return allLogs.Where(log => 
                log.level >= selectedLogLevel && 
                (selectedCategory == AdvancedLoggingSystem.LogCategory.System || log.category == selectedCategory)
            ).ToList();
        }
        
        private string GetColorTagForLogLevel(AdvancedLoggingSystem.LogLevel level)
        {
            return level switch
            {
                AdvancedLoggingSystem.LogLevel.Error => $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>",
                AdvancedLoggingSystem.LogLevel.Critical => $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>",
                AdvancedLoggingSystem.LogLevel.Warning => $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>",
                AdvancedLoggingSystem.LogLevel.Debug => $"<color=#{ColorUtility.ToHtmlStringRGB(debugColor)}>",
                _ => $"<color=#{ColorUtility.ToHtmlStringRGB(infoColor)}>"
            };
        }
        
        private string GetIconForLogLevel(AdvancedLoggingSystem.LogLevel level)
        {
            return level switch
            {
                AdvancedLoggingSystem.LogLevel.Trace => "🔍",
                AdvancedLoggingSystem.LogLevel.Debug => "🐛",
                AdvancedLoggingSystem.LogLevel.Info => "ℹ️",
                AdvancedLoggingSystem.LogLevel.Warning => "⚠️",
                AdvancedLoggingSystem.LogLevel.Error => "❌",
                AdvancedLoggingSystem.LogLevel.Critical => "🚨",
                _ => "📝"
            };
        }
        
        private void UpdateSystemStatus()
        {
            if (!showSystemStatus || systemStatusDisplay == null) return;
            
            var statusText = new StringBuilder();
            statusText.AppendLine("<b>=== System Status ===</b>");
            
            // Core systems
            statusText.AppendLine($"Game Manager: {GetSystemStatus<GameManager>()}");
            statusText.AppendLine($"Audio Manager: {GetSystemStatus<AdvancedAudioManager>()}");
            statusText.AppendLine($"Hand Tracking: {GetSystemStatus<HandTrackingManager>()}");
            statusText.AppendLine($"Rhythm Targets: {GetSystemStatus<RhythmTargetSystem>()}");
            statusText.AppendLine($"Form Tracker: {GetSystemStatus<BoxingFormTracker>()}");
            
            // VR systems
            statusText.AppendLine($"VR Camera Helper: {GetSystemStatus<VRCameraHelper>()}");
            statusText.AppendLine($"Movement System: {GetSystemStatus<VR360MovementSystem>()}");
            statusText.AppendLine($"Haptic Feedback: {GetSystemStatus<HapticFeedbackManager>()}");
            
            // Performance systems
            statusText.AppendLine($"Performance Monitor: {GetSystemStatus<VRPerformanceMonitor>()}");
            statusText.AppendLine($"Render Graph: {GetSystemStatus<VRRenderGraphSystem>()}");
            
            // Session info
            statusText.AppendLine($"\n<b>Session:</b> {AdvancedLoggingSystem.GetSessionReport().Split('\n')[1]}");
            
            systemStatusDisplay.text = statusText.ToString();
        }
        
        private string GetSystemStatus<T>() where T : MonoBehaviour
        {
            var system = CachedReferenceManager.Get<T>();
            if (system == null) return "<color=red>❌ Not Found</color>";
            if (!system.enabled) return "<color=yellow>⚠️ Disabled</color>";
            return "<color=green>✅ Active</color>";
        }
        
        private void UpdatePerformanceDisplay()
        {
            if (!showPerformance || performanceDisplay == null) return;
            
            var perfText = new StringBuilder();
            perfText.AppendLine("<b>=== Performance ===</b>");
            
            // Frame rate
            float fps = 1f / Time.unscaledDeltaTime;
            string fpsColor = fps >= 80 ? "green" : fps >= 60 ? "yellow" : "red";
            perfText.AppendLine($"FPS: <color={fpsColor}>{fps:F1}</color>");
            
            // Frame time
            float frameTime = Time.unscaledDeltaTime * 1000f;
            string frameTimeColor = frameTime <= 11.1f ? "green" : frameTime <= 16.7f ? "yellow" : "red";
            perfText.AppendLine($"Frame Time: <color={frameTimeColor}>{frameTime:F1}ms</color>");
            
            // Memory usage
            long memoryUsage = System.GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / (1024f * 1024f);
            string memoryColor = memoryMB <= 500 ? "green" : memoryMB <= 1000 ? "yellow" : "red";
            perfText.AppendLine($"Memory: <color={memoryColor}>{memoryMB:F1} MB</color>");
            
            // System info
            perfText.AppendLine($"Time Scale: {Time.timeScale:F2}");
            perfText.AppendLine($"Frame Count: {Time.frameCount}");
            
            // Recent errors
            var recentErrors = AdvancedLoggingSystem.GetErrorLogs(3);
            if (recentErrors.Count > 0)
            {
                perfText.AppendLine("\n<b><color=red>Recent Errors:</color></b>");
                foreach (var error in recentErrors)
                {
                    perfText.AppendLine($"<color=red>• {error.timestamp:HH:mm:ss} - {error.system}: {error.message.Substring(0, Mathf.Min(50, error.message.Length))}...</color>");
                }
            }
            
            performanceDisplay.text = perfText.ToString();
        }
        
        // UI Event Handlers
        private void OnLogLevelChanged(int value)
        {
            selectedLogLevel = (AdvancedLoggingSystem.LogLevel)value;
            UpdateLogDisplay();
        }
        
        private void OnCategoryChanged(int value)
        {
            if (value == 0) // "All" option
            {
                selectedCategory = AdvancedLoggingSystem.LogCategory.System; // Default, but we'll ignore this in filtering
            }
            else
            {
                selectedCategory = (AdvancedLoggingSystem.LogCategory)(value - 1);
            }
            UpdateLogDisplay();
        }
        
        private void OnLogCountChanged(float value)
        {
            maxDisplayedLogs = (int)value;
            UpdateLogDisplay();
        }
        
        private void ClearLogs()
        {
            displayedLogs.Clear();
            if (logTextDisplay != null)
            {
                logTextDisplay.text = "";
            }
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.UI, "DebugPanel", "🧹 Logs cleared from display");
        }
        
        private void ExportLogs()
        {
            AdvancedLoggingSystem.ExportLogsToFile();
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.UI, "DebugPanel", "📁 Logs exported to file");
        }
        
        private void ToggleSystemStatus()
        {
            showSystemStatus = !showSystemStatus;
            if (statusPanel != null)
            {
                statusPanel.gameObject.SetActive(showSystemStatus);
            }
        }
        
        private void TogglePerformance()
        {
            showPerformance = !showPerformance;
            if (performancePanel != null)
            {
                performancePanel.gameObject.SetActive(showPerformance);
            }
        }
        
        public void ShowPanel()
        {
            isVisible = true;
            if (debugCanvas != null)
            {
                debugCanvas.gameObject.SetActive(true);
            }
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.UI, "DebugPanel", "🔍 Debug panel opened");
        }
        
        public void HidePanel()
        {
            isVisible = false;
            if (debugCanvas != null)
            {
                debugCanvas.gameObject.SetActive(false);
            }
        }
        
        public void TogglePanel()
        {
            if (isVisible)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }
        
        // Public API for external systems
        public static void ShowDebugPanel()
        {
            var panel = CachedReferenceManager.Get<DebugLogPanel>();
            if (panel != null)
            {
                panel.ShowPanel();
            }
        }
        
        public static void HideDebugPanel()
        {
            var panel = CachedReferenceManager.Get<DebugLogPanel>();
            if (panel != null)
            {
                panel.HidePanel();
            }
        }
        
        // VR-specific methods
        public void AttachToVRCamera()
        {
            if (VRCameraHelper.ActiveCamera != null && debugCanvas != null)
            {
                // Attach canvas to VR camera for world space display
                debugCanvas.renderMode = RenderMode.WorldSpace;
                debugCanvas.transform.SetParent(VRCameraHelper.ActiveCamera.transform);
                debugCanvas.transform.localPosition = new Vector3(0, 0, 2f); // 2 meters in front
                debugCanvas.transform.localRotation = Quaternion.identity;
                debugCanvas.transform.localScale = Vector3.one * 0.001f; // Scale down for VR
                
                AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.VR, "DebugPanel", "🥽 Debug panel attached to VR camera");
            }
        }
        
        private void OnDestroy()
        {
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.UI, "DebugPanel", "🔍 Debug panel destroyed");
        }
    }
} 