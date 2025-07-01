using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRBoxingGame.UI;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// Creates a complete VR main menu UI system at runtime
    /// </summary>
    public class VRMainMenuCreator : MonoBehaviour
    {
        [Header("Menu Creation")]
        public bool createOnStart = true;
        public bool createInWorldSpace = true;
        public Vector3 menuPosition = new Vector3(0, 2f, 3f);
        public Vector3 menuScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        [Header("Menu Styling")]
        public Color primaryColor = new Color(0.2f, 0.6f, 1f);
        public Color secondaryColor = new Color(0.1f, 0.1f, 0.2f);
        public Color textColor = Color.white;
        public Font menuFont;
        
        private GameObject mainMenuCanvas;
        private MainMenuSystem menuSystem;
        
        private void Start()
        {
            if (createOnStart)
            {
                CreateCompleteMainMenu();
            }
        }
        
        [ContextMenu("Create Complete Main Menu")]
        public void CreateCompleteMainMenu()
        {
            CreateMainMenuCanvas();
            CreateMainMenuPanels();
            CreateMainMenuButtons();
            CreateSettingsPanel();
            CreateBackgroundSelectionPanel();
            CreateSceneSelectionPanel();
            CreateSpotifyPanel();
            CreateCreditsPanel();
            SetupMainMenuSystem();
            
            Debug.Log("✅ Complete VR Main Menu created!");
        }
        
        private void CreateMainMenuCanvas()
        {
            // Create main canvas
            mainMenuCanvas = new GameObject("VR_MainMenu_Canvas");
            mainMenuCanvas.transform.SetParent(transform);
            
            Canvas canvas = mainMenuCanvas.AddComponent<Canvas>();
            CanvasScaler scaler = mainMenuCanvas.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = mainMenuCanvas.AddComponent<GraphicRaycaster>();
            
            if (createInWorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                mainMenuCanvas.transform.position = menuPosition;
                mainMenuCanvas.transform.localScale = menuScale;
                
                // Set canvas size for world space
                RectTransform canvasRect = mainMenuCanvas.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(1920, 1080);
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        
        private void CreateMainMenuPanels()
        {
            // Main Menu Panel
            CreatePanel("MainMenuPanel", Vector2.zero, new Vector2(1920, 1080));
            
            // Settings Panel (initially hidden)
            CreatePanel("SettingsPanel", Vector2.zero, new Vector2(1920, 1080), false);
            
            // Background Selection Panel (initially hidden)
            CreatePanel("BackgroundSelectionPanel", Vector2.zero, new Vector2(1920, 1080), false);
            
            // Scene Selection Panel (initially hidden)
            CreatePanel("SceneSelectionPanel", Vector2.zero, new Vector2(1920, 1080), false);
            
            // Spotify Panel (initially hidden)
            CreatePanel("SpotifyPanel", Vector2.zero, new Vector2(1920, 1080), false);
            
            // Credits Panel (initially hidden)
            CreatePanel("CreditsPanel", Vector2.zero, new Vector2(1920, 1080), false);
        }
        
        private GameObject CreatePanel(string name, Vector2 position, Vector2 size, bool active = true)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(mainMenuCanvas.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = position;
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, 0.8f);
            
            panel.SetActive(active);
            return panel;
        }
        
        private void CreateMainMenuButtons()
        {
            Transform mainPanel = mainMenuCanvas.transform.Find("MainMenuPanel");
            if (mainPanel == null) return;
            
            // Game Title
            CreateText("VR RHYTHM BOXING", mainPanel, new Vector2(0, 400), new Vector2(800, 100), 72);
            
            // Main Menu Buttons
            CreateButton("START GAME", mainPanel, new Vector2(0, 250), new Vector2(400, 80));
            CreateButton("SETTINGS", mainPanel, new Vector2(0, 150), new Vector2(400, 80));
            CreateButton("BACKGROUNDS", mainPanel, new Vector2(0, 50), new Vector2(400, 80));
            CreateButton("SCENES", mainPanel, new Vector2(0, -50), new Vector2(400, 80));
            CreateButton("SPOTIFY", mainPanel, new Vector2(0, -150), new Vector2(400, 80));
            CreateButton("CREDITS", mainPanel, new Vector2(0, -250), new Vector2(400, 80));
            CreateButton("QUIT", mainPanel, new Vector2(0, -350), new Vector2(400, 80));
        }
        
        private void CreateSettingsPanel()
        {
            Transform settingsPanel = mainMenuCanvas.transform.Find("SettingsPanel");
            if (settingsPanel == null) return;
            
            // Settings Title
            CreateText("SETTINGS", settingsPanel, new Vector2(0, 400), new Vector2(400, 80), 48);
            
            // Volume Slider
            CreateText("Volume", settingsPanel, new Vector2(-200, 250), new Vector2(200, 60), 36);
            CreateSlider("VolumeSlider", settingsPanel, new Vector2(100, 250), new Vector2(300, 60));
            
            // Difficulty Slider
            CreateText("Difficulty", settingsPanel, new Vector2(-200, 150), new Vector2(200, 60), 36);
            CreateSlider("DifficultySlider", settingsPanel, new Vector2(100, 150), new Vector2(300, 60));
            
            // Hand Tracking Toggle
            CreateText("Hand Tracking", settingsPanel, new Vector2(-200, 50), new Vector2(200, 60), 36);
            CreateToggle("HandTrackingToggle", settingsPanel, new Vector2(100, 50), new Vector2(60, 60));
            
            // Auto Optimization Toggle
            CreateText("Auto Optimization", settingsPanel, new Vector2(-200, -50), new Vector2(200, 60), 36);
            CreateToggle("AutoOptimizationToggle", settingsPanel, new Vector2(100, -50), new Vector2(60, 60));
            
            // Back Button
            CreateButton("BACK", settingsPanel, new Vector2(0, -300), new Vector2(200, 60));
        }
        
        private void CreateBackgroundSelectionPanel()
        {
            Transform bgPanel = mainMenuCanvas.transform.Find("BackgroundSelectionPanel");
            if (bgPanel == null) return;
            
            // Background Selection Title
            CreateText("SELECT BACKGROUND", bgPanel, new Vector2(0, 400), new Vector2(600, 80), 48);
            
            // Current Background Display
            CreateText("Current: Cyberpunk City", bgPanel, new Vector2(0, 300), new Vector2(400, 60), 32);
            
            // Background Selection Buttons
            string[] backgrounds = { "Cyberpunk", "Space", "Abstract", "Crystal", "Aurora", "Underwater" };
            for (int i = 0; i < backgrounds.Length; i++)
            {
                float xPos = (i % 3 - 1) * 200;
                float yPos = (i < 3) ? -50 : -150;
                CreateButton(backgrounds[i], bgPanel, new Vector2(xPos, yPos), new Vector2(180, 60));
            }
            
            // Back Button
            CreateButton("BACK", bgPanel, new Vector2(0, -300), new Vector2(200, 60));
        }
        
        private void CreateSceneSelectionPanel()
        {
            Transform scenePanel = mainMenuCanvas.transform.Find("SceneSelectionPanel");
            if (scenePanel == null) return;
            
            // Scene Selection Title
            CreateText("SELECT SCENE", scenePanel, new Vector2(0, 400), new Vector2(600, 80), 48);
            
            // Current Scene Display
            CreateText("Current: Default Arena", scenePanel, new Vector2(0, 320), new Vector2(500, 60), 32);
            
            // Scene Description
            CreateText("Classic VR boxing arena with customizable backgrounds", scenePanel, new Vector2(0, 260), new Vector2(800, 40), 24);
            
            // Scene Selection Buttons (2 rows of 4)
            string[] scenes = { "Default", "Rain Storm", "Neon City", "Space Station", "Crystal Cave", "Underwater", "Desert", "Forest" };
            for (int i = 0; i < scenes.Length; i++)
            {
                float xPos = (i % 4 - 1.5f) * 180;
                float yPos = (i < 4) ? 100 : 0;
                CreateButton(scenes[i], scenePanel, new Vector2(xPos, yPos), new Vector2(160, 60));
            }
            
            // Scene Preview Area
            CreateText("Scene Preview", scenePanel, new Vector2(0, -120), new Vector2(200, 40), 28);
            
            // Back Button
            CreateButton("BACK", scenePanel, new Vector2(0, -300), new Vector2(200, 60));
            
            // Target Mode Toggle Section
            CreateTargetModeToggle(scenePanel.gameObject);
        }
        
        private void CreateTargetModeToggle(GameObject parent)
        {
            // Target mode section container
            GameObject toggleSection = new GameObject("TargetModeSection");
            toggleSection.transform.SetParent(parent.transform, false);
            
            RectTransform toggleSectionRect = toggleSection.AddComponent<RectTransform>();
            toggleSectionRect.sizeDelta = new Vector2(760, 80);
            toggleSectionRect.anchoredPosition = new Vector2(0, 200);
            
            // Background for toggle section
            Image sectionBg = toggleSection.AddComponent<Image>();
            sectionBg.color = new Color(0.15f, 0.15f, 0.25f, 0.8f);
            
            // Toggle label
            GameObject toggleLabel = new GameObject("ToggleLabel");
            toggleLabel.transform.SetParent(toggleSection.transform, false);
            
            TextMeshProUGUI labelText = toggleLabel.AddComponent<TextMeshProUGUI>();
            labelText.text = "Target Mode:";
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MiddleLeft;
            
            RectTransform labelRect = toggleLabel.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 40);
            labelRect.anchoredPosition = new Vector2(-280, 0);
            
            // Toggle
            GameObject toggleObj = new GameObject("TraditionalTargetsToggle");
            toggleObj.transform.SetParent(toggleSection.transform, false);
            
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            menuSystem.traditionalTargetsToggle = toggle;
            
            // Toggle background
            GameObject toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            
            Image toggleBgImage = toggleBg.AddComponent<Image>();
            toggleBgImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            
            RectTransform toggleBgRect = toggleBg.GetComponent<RectTransform>();
            toggleBgRect.sizeDelta = new Vector2(40, 20);
            
            // Toggle checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = Color.green;
            checkmarkImage.sprite = CreateCheckmarkSprite();
            
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.sizeDelta = new Vector2(16, 16);
            
            // Configure toggle
            toggle.targetGraphic = toggleBgImage;
            toggle.graphic = checkmarkImage;
            
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(40, 20);
            toggleRect.anchoredPosition = new Vector2(-100, 0);
            
            // Mode text display
            GameObject modeTextObj = new GameObject("ModeText");
            modeTextObj.transform.SetParent(toggleSection.transform, false);
            
            TextMeshProUGUI modeText = modeTextObj.AddComponent<TextMeshProUGUI>();
            modeText.text = "Mode: Scene Immersion";
            modeText.fontSize = 16;
            modeText.color = Color.cyan;
            modeText.alignment = TextAlignmentOptions.MiddleLeft;
            
            RectTransform modeTextRect = modeTextObj.GetComponent<RectTransform>();
            modeTextRect.sizeDelta = new Vector2(300, 40);
            modeTextRect.anchoredPosition = new Vector2(100, 0);
            
            menuSystem.targetModeText = modeText;
            
            // Info text
            GameObject infoTextObj = new GameObject("InfoText");
            infoTextObj.transform.SetParent(toggleSection.transform, false);
            
            TextMeshProUGUI infoText = infoTextObj.AddComponent<TextMeshProUGUI>();
            infoText.text = "Toggle ON for traditional white/gray circles, OFF for immersive scene objects";
            infoText.fontSize = 12;
            infoText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            infoText.alignment = TextAlignmentOptions.MiddleCenter;
            
            RectTransform infoTextRect = infoTextObj.GetComponent<RectTransform>();
            infoTextRect.sizeDelta = new Vector2(700, 30);
            infoTextRect.anchoredPosition = new Vector2(0, -25);
        }
        
        private Sprite CreateCheckmarkSprite()
        {
            // Create a simple checkmark texture
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            
            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Draw simple checkmark pattern
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if ((x == 4 && y >= 6 && y <= 10) ||
                        (x == 5 && y >= 7 && y <= 11) ||
                        (x >= 6 && x <= 12 && y == 12 - x + 6))
                    {
                        pixels[y * 16 + x] = Color.white;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        private void CreateSceneButtonsGrid(GameObject parent)
        {
            // Scene buttons container
            GameObject buttonsContainer = new GameObject("SceneButtonsContainer");
            buttonsContainer.transform.SetParent(parent.transform, false);
            
            RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(760, 200);
            containerRect.anchoredPosition = new Vector2(0, 50);
            
            // Create 2x4 grid of scene buttons
            string[] sceneNames = {
                "Default Arena", "Rain Storm", "Neon City", "Space Station",
                "Crystal Cave", "Underwater World", "Desert Oasis", "Forest Glade"
            };
            
            for (int i = 0; i < 8; i++)
            {
                GameObject buttonObj = new GameObject($"SceneButton_{i}");
                buttonObj.transform.SetParent(buttonsContainer.transform, false);
                
                Button button = buttonObj.AddComponent<Button>();
                menuSystem.sceneButtons[i] = button;
                
                // Button image
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.3f, 0.5f, 1f);
                
                // Button text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                
                TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = sceneNames[i];
                buttonText.fontSize = 14;
                buttonText.color = Color.white;
                buttonText.alignment = TextAlignmentOptions.Center;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(170, 80);
                textRect.anchoredPosition = Vector2.zero;
                
                // Position button in grid
                int row = i / 4;
                int col = i % 4;
                
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(180, 90);
                buttonRect.anchoredPosition = new Vector2(-285 + col * 190, 50 - row * 100);
                
                // Button functionality
                int sceneIndex = i;
                button.onClick.AddListener(() => menuSystem.SelectScene(sceneIndex));
                
                // Configure button
                button.targetGraphic = buttonImage;
                
                // Hover effects
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(0.3f, 0.4f, 0.6f, 1f);
                colors.pressedColor = new Color(0.1f, 0.2f, 0.4f, 1f);
                button.colors = colors;
            }
        }
        
        private void CreateSpotifyPanel()
        {
            Transform spotifyPanel = mainMenuCanvas.transform.Find("SpotifyPanel");
            if (spotifyPanel == null) return;
            
            // Spotify Title
            CreateText("SPOTIFY INTEGRATION", spotifyPanel, new Vector2(0, 400), new Vector2(600, 80), 48);
            
            // Connection Status
            CreateText("Status: Not Connected", spotifyPanel, new Vector2(0, 300), new Vector2(400, 60), 32);
            
            // Connect/Disconnect Buttons
            CreateButton("CONNECT TO SPOTIFY", spotifyPanel, new Vector2(0, 200), new Vector2(400, 80));
            CreateButton("DISCONNECT", spotifyPanel, new Vector2(0, 100), new Vector2(400, 80));
            
            // Instructions
            CreateText("Connect to Spotify to access your playlists\\nand discover workout music!", 
                spotifyPanel, new Vector2(0, -50), new Vector2(500, 100), 24);
            
            // Back Button
            CreateButton("BACK", spotifyPanel, new Vector2(0, -300), new Vector2(200, 60));
        }
        
        private void CreateCreditsPanel()
        {
            Transform creditsPanel = mainMenuCanvas.transform.Find("CreditsPanel");
            if (creditsPanel == null) return;
            
            // Credits Title
            CreateText("CREDITS", creditsPanel, new Vector2(0, 400), new Vector2(400, 80), 48);
            
            // Credits Text
            string creditsText = "VR RHYTHM BOXING\\n\\n" +
                               "Developed with Unity 6\\n" +
                               "Built for Meta Quest 2/3\\n\\n" +
                               "Features:\\n" +
                               "• Advanced Hand Tracking\\n" +
                               "• Music-Reactive Environments\\n" +
                               "• Spotify Integration\\n" +
                               "• Performance Optimization\\n\\n" +
                               "Thank you for playing!";
            
            CreateText(creditsText, creditsPanel, new Vector2(0, 0), new Vector2(600, 600), 28);
            
            // Back Button
            CreateButton("BACK", creditsPanel, new Vector2(0, -350), new Vector2(200, 60));
        }
        
        private GameObject CreateButton(string text, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject button = new GameObject($"Button_{text.Replace(" ", "")}");
            button.transform.SetParent(parent, false);
            
            RectTransform rect = button.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = button.AddComponent<Image>();
            image.color = primaryColor;
            
            Button btn = button.AddComponent<Button>();
            
            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.color = textColor;
            textComp.fontSize = 24;
            textComp.alignment = TextAlignmentOptions.Center;
            if (menuFont != null) textComp.font = TMP_FontAsset.CreateFontAsset(menuFont);
            
            return button;
        }
        
        private GameObject CreateText(string text, Transform parent, Vector2 position, Vector2 size, float fontSize = 24)
        {
            GameObject textObj = new GameObject($"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(10, text.Length))}");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.color = textColor;
            textComp.fontSize = fontSize;
            textComp.alignment = TextAlignmentOptions.Center;
            if (menuFont != null) textComp.font = TMP_FontAsset.CreateFontAsset(menuFont);
            
            return textObj;
        }
        
        private GameObject CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject slider = new GameObject(name);
            slider.transform.SetParent(parent, false);
            
            RectTransform rect = slider.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Slider sliderComp = slider.AddComponent<Slider>();
            sliderComp.minValue = 0f;
            sliderComp.maxValue = 1f;
            sliderComp.value = 0.5f;
            
            // Background
            GameObject background = CreatePanel("Background", Vector2.zero, size);
            background.transform.SetParent(slider.transform, false);
            background.GetComponent<Image>().color = Color.gray;
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            RectTransform fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            // Fill
            GameObject fill = CreatePanel("Fill", Vector2.zero, size);
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = primaryColor;
            
            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(slider.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            // Handle
            GameObject handle = CreatePanel("Handle", Vector2.zero, new Vector2(20, size.y));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<Image>().color = Color.white;
            
            // Configure slider
            sliderComp.fillRect = fill.GetComponent<RectTransform>();
            sliderComp.handleRect = handle.GetComponent<RectTransform>();
            
            return slider;
        }
        
        private GameObject CreateToggle(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject toggle = new GameObject(name);
            toggle.transform.SetParent(parent, false);
            
            RectTransform rect = toggle.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Toggle toggleComp = toggle.AddComponent<Toggle>();
            
            // Background
            GameObject background = CreatePanel("Background", Vector2.zero, size);
            background.transform.SetParent(toggle.transform, false);
            background.GetComponent<Image>().color = Color.gray;
            
            // Checkmark
            GameObject checkmark = CreatePanel("Checkmark", Vector2.zero, size * 0.8f);
            checkmark.transform.SetParent(background.transform, false);
            checkmark.GetComponent<Image>().color = primaryColor;
            
            // Configure toggle
            toggleComp.targetGraphic = background.GetComponent<Image>();
            toggleComp.graphic = checkmark.GetComponent<Image>();
            
            return toggle;
        }
        
        private void SetupMainMenuSystem()
        {
            // Add MainMenuSystem component to the canvas
            menuSystem = mainMenuCanvas.AddComponent<MainMenuSystem>();
            
            // Assign all the created UI elements
            menuSystem.mainMenuPanel = mainMenuCanvas.transform.Find("MainMenuPanel")?.gameObject;
            menuSystem.settingsPanel = mainMenuCanvas.transform.Find("SettingsPanel")?.gameObject;
            menuSystem.backgroundSelectionPanel = mainMenuCanvas.transform.Find("BackgroundSelectionPanel")?.gameObject;
            menuSystem.sceneSelectionPanel = mainMenuCanvas.transform.Find("SceneSelectionPanel")?.gameObject;
            menuSystem.spotifyPanel = mainMenuCanvas.transform.Find("SpotifyPanel")?.gameObject;
            menuSystem.creditsPanel = mainMenuCanvas.transform.Find("CreditsPanel")?.gameObject;
            
            // Find and assign main menu buttons
            menuSystem.startGameButton = FindButtonByName("Button_STARTGAME");
            menuSystem.settingsButton = FindButtonByName("Button_SETTINGS");
            menuSystem.backgroundsButton = FindButtonByName("Button_BACKGROUNDS");
            menuSystem.scenesButton = FindButtonByName("Button_SCENES");
            menuSystem.spotifyButton = FindButtonByName("Button_SPOTIFY");
            menuSystem.creditsButton = FindButtonByName("Button_CREDITS");
            menuSystem.quitButton = FindButtonByName("Button_QUIT");
            
            // Find and assign settings controls
            menuSystem.volumeSlider = FindComponentByName<Slider>("VolumeSlider");
            menuSystem.difficultySlider = FindComponentByName<Slider>("DifficultySlider");
            menuSystem.handTrackingToggle = FindComponentByName<Toggle>("HandTrackingToggle");
            menuSystem.autoOptimizationToggle = FindComponentByName<Toggle>("AutoOptimizationToggle");
            
            // Find and assign background selection buttons
            menuSystem.backgroundButtons = new Button[6];
            string[] bgButtonNames = { "Button_Cyberpunk", "Button_Space", "Button_Abstract", "Button_Crystal", "Button_Aurora", "Button_Underwater" };
            for (int i = 0; i < bgButtonNames.Length && i < menuSystem.backgroundButtons.Length; i++)
            {
                menuSystem.backgroundButtons[i] = FindButtonInPanel("BackgroundSelectionPanel", bgButtonNames[i]);
            }
            
            // Find and assign scene selection buttons
            menuSystem.sceneButtons = new Button[8];
            string[] sceneButtonNames = { "Button_Default", "Button_RainStorm", "Button_NeonCity", "Button_SpaceStation", "Button_CrystalCave", "Button_Underwater", "Button_Desert", "Button_Forest" };
            for (int i = 0; i < sceneButtonNames.Length && i < menuSystem.sceneButtons.Length; i++)
            {
                menuSystem.sceneButtons[i] = FindButtonInPanel("SceneSelectionPanel", sceneButtonNames[i]);
            }
            
            // Find and assign text displays
            menuSystem.currentBackgroundText = FindTextInPanel("BackgroundSelectionPanel", "Text_Current");
            menuSystem.currentSceneText = FindTextInPanel("SceneSelectionPanel", "Text_Current");
            menuSystem.sceneDescriptionText = FindTextInPanel("SceneSelectionPanel", "Text_Classic");
            
            Debug.Log("✅ MainMenuSystem configured with all UI elements including scene selection");
        }
        
        private Button FindButtonByName(string name)
        {
            Transform found = mainMenuCanvas.transform.Find($"MainMenuPanel/{name}");
            return found?.GetComponent<Button>();
        }
        
        private Button FindButtonInPanel(string panelName, string buttonName)
        {
            Transform found = mainMenuCanvas.transform.Find($"{panelName}/{buttonName}");
            return found?.GetComponent<Button>();
        }
        
        private TextMeshProUGUI FindTextInPanel(string panelName, string textName)
        {
            Transform found = mainMenuCanvas.transform.Find($"{panelName}/{textName}");
            return found?.GetComponent<TextMeshProUGUI>();
        }
        
        private T FindComponentByName<T>(string name) where T : Component
        {
            Transform found = mainMenuCanvas.transform.Find($"SettingsPanel/{name}");
            return found?.GetComponent<T>();
        }
    }
} 