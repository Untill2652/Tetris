using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TetrisCourse
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string MainMenuBackgroundResource = "MainMenuBackground";
        private const string GameBackgroundResource = "GameBackground";
        private const string BoardBackgroundResource = "BoardBackground";
        private const string UiFontResource = "Fonts/TetrisUi";

        private readonly string[] tutorialTitles =
        {
            "Цель игры",
            "Управление на ПК",
            "Управление на телефоне",
            "Очки и уровни"
        };

        private readonly string[] tutorialTexts =
        {
            "Собирайте полностью заполненные горизонтальные линии. Заполненная линия исчезает, а блоки выше опускаются вниз.",
            "A или стрелка влево - влево. D или стрелка вправо - вправо. W, стрелка вверх или X - поворот. Space - сброс. C - удержать. Esc - пауза.",
            "На телефоне используйте нижние крупные кнопки: Влево, Поворот, Вправо, Удержать и Сброс. Пауза вынесена отдельно справа.",
            "За 1, 2, 3 и 4 линии начисляется 100, 300, 500 и 800 очков, умноженных на уровень. Уровень растёт каждые 5 линий."
        };

        private AppStateMachine stateMachine;
        private InputRouter inputRouter;
        private GameSessionController sessionController;
        private StorageService storageService;
        private SettingsService settingsService;
        private AudioAccessibilityService audioAccessibilityService;

        private Canvas canvas;
        private RectTransform safeAreaRoot;
        private Font font;

        private GameObject mainMenuPanel;
        private GameObject tutorialPanel;
        private GameObject gamePanel;
        private GameObject pausePanel;
        private GameObject settingsPanel;
        private GameObject recordsPanel;
        private GameObject gameOverPanel;
        private GameObject mobileControls;
        private GameObject keyboardHint;
        private GameObject desktopPauseButton;
        private GameObject mobilePauseButton;
        private GameObject tutorialNextObject;
        private GameObject tutorialStartObject;

        private RectTransform mainMenuCard;
        private RectTransform tutorialCard;
        private RectTransform settingsCard;
        private RectTransform recordsCard;
        private RectTransform recordsTableCard;
        private RectTransform gameOverCard;
        private RectTransform boardRect;
        private GridLayoutGroup boardGrid;
        private RectTransform hudRect;
        private RectTransform nextPanelRect;
        private RectTransform holdPanelRect;
        private RectTransform mobileControlsRect;
        private RectTransform keyboardHintRect;
        private RectTransform recordsRowsRoot;

        private Image[,] boardCells;
        private Image[,] nextCells;
        private Image[,] holdCells;
        private GridLayoutGroup nextPreviewGrid;
        private GridLayoutGroup holdPreviewGrid;
        private LayoutElement nextPreviewGridLayout;
        private LayoutElement holdPreviewGridLayout;
        private Sprite runtimeMainMenuBackgroundSprite;
        private Sprite runtimeGameBackgroundSprite;
        private Sprite runtimeBoardBackgroundSprite;
        private Sprite roundedButtonSprite;

        // Декоративные фоновые изображения — приглушаются в контрастном режиме.
        private readonly List<Image> backgroundImages = new List<Image>();
        private readonly List<Color> backgroundOriginalColors = new List<Color>();

        private Text scoreText;
        private Text linesText;
        private Text levelText;
        private Text timeText;
        private Text recordsEmptyText;
        private Text soundButtonText;
        private Text musicButtonText;
        private Text contrastButtonText;
        private Text gameOverSummaryText;
        private Text gameOverTopText;
        private Text tutorialTitleText;
        private Text tutorialBodyText;

        private int tutorialStep;
        private bool contrastMode;
        private bool isNarrowLayout;
        private Rect lastSafeArea;

        public void Initialize(
            AppStateMachine states,
            InputRouter input,
            GameSessionController session,
            StorageService storage,
            SettingsService settings,
            AudioAccessibilityService accessibility)
        {
            stateMachine = states;
            inputRouter = input;
            sessionController = session;
            storageService = storage;
            settingsService = settings;
            audioAccessibilityService = accessibility;

            BuildInterface();
            stateMachine.StateChanged += ShowState;
            ShowState(stateMachine.CurrentState);
        }

        public void ForceShowMainMenu()
        {
            ShowState(AppState.MainMenu);
        }

        public void ApplySettings(UserSettings settings)
        {
            contrastMode = settings.contrastMode;
            ApplyPalette();
            UpdateSettingsLabels();
            RenderGame(sessionController != null ? sessionController.Board : null, sessionController != null ? sessionController.PieceService : null);
        }

        public void RenderGame(BoardModel board, PieceService pieces)
        {
            if (boardCells == null || board == null)
            {
                return;
            }

            Color empty = BoardCellEmptyColor();
            for (int x = 0; x < BoardModel.Width; x++)
            {
                for (int y = 0; y < BoardModel.Height; y++)
                {
                    CellData cell = board.GetCell(x, y);
                    boardCells[x, y].color = cell.Filled ? TetrominoData.GetColor(cell.Type, contrastMode) : empty;
                }
            }

            if (pieces?.ActivePiece != null)
            {
                Vector2Int[] activeCells = pieces.ActivePiece.GetCells();
                for (int i = 0; i < activeCells.Length; i++)
                {
                    Vector2Int cell = activeCells[i];
                    if (board.IsInside(cell))
                    {
                        boardCells[cell.x, cell.y].color = TetrominoData.GetColor(pieces.ActivePiece.Type, contrastMode);
                    }
                }
            }

            if (pieces != null)
            {
                RenderPreview(nextCells, pieces.NextPiece);
                RenderPreview(holdCells, pieces.HoldPiece);
            }
        }

        public void UpdateHud(ScoreService score)
        {
            if (score == null || scoreText == null)
            {
                return;
            }

            scoreText.text = "Очки\n" + score.Score;
            linesText.text = "Линии\n" + score.Lines;
            levelText.text = "Уровень\n" + score.Level;
            timeText.text = "Время\n" + FormatTime(Mathf.RoundToInt(score.DurationSec));
        }

        public void RefreshRecords(HighScoreTable table)
        {
            if (recordsRowsRoot == null)
            {
                return;
            }

            ClearChildren(recordsRowsRoot);

            bool hasRecords = table != null && table.entries != null && table.entries.Count > 0;
            recordsRowsRoot.gameObject.SetActive(hasRecords);
            recordsEmptyText.gameObject.SetActive(!hasRecords);

            if (!hasRecords)
            {
                return;
            }

            int count = Mathf.Min(5, table.entries.Count);
            for (int i = 0; i < count; i++)
            {
                HighScoreEntry entry = table.entries[i];
                RectTransform row = CreateTableRow(recordsRowsRoot, i % 2 == 0);
                AddTableCell(row, (i + 1).ToString(), 72f, 22, TextAnchor.MiddleCenter);
                AddTableCell(row, entry.score.ToString(), 150f, 22, TextAnchor.MiddleCenter);
                AddTableCell(row, entry.lines.ToString(), 120f, 22, TextAnchor.MiddleCenter);
                AddTableCell(row, entry.level.ToString(), 120f, 22, TextAnchor.MiddleCenter);
                AddTableCell(row, FormatTime(entry.durationSec), 150f, 22, TextAnchor.MiddleCenter);
            }
        }

        public void ShowGameOver(ScoreService score, bool isTopScore)
        {
            if (gameOverSummaryText == null)
            {
                return;
            }

            gameOverSummaryText.text =
                "Очки: " + score.Score + "\n" +
                "Линии: " + score.Lines + "\n" +
                "Уровень: " + score.Level + "\n" +
                "Время: " + FormatTime(Mathf.RoundToInt(score.DurationSec));

            gameOverTopText.text = isTopScore ? "Результат попал в пятёрку рекордов" : "Результат не попал в пятёрку рекордов";
        }

        private void Update()
        {
            ApplySafeArea();

            bool nextLayout = Screen.width < Screen.height || Screen.width < 900;
            if (nextLayout != isNarrowLayout)
            {
                isNarrowLayout = nextLayout;
                UpdateResponsiveLayout();
            }
        }

        private void BuildInterface()
        {
            font = LoadUiFont();
            EnsureCanvas();
            EnsureEventSystem();

            mainMenuPanel = CreatePanel("MainMenuPanel", BackgroundColor());
            tutorialPanel = CreatePanel("TutorialPanel", BackgroundColor());
            gamePanel = CreatePanel("GamePanel", BackgroundColor());
            pausePanel = CreatePanel("PausePanel", new Color(0f, 0f, 0f, 0.78f));
            settingsPanel = CreatePanel("SettingsPanel", BackgroundColor());
            recordsPanel = CreatePanel("RecordsPanel", BackgroundColor());
            gameOverPanel = CreatePanel("GameOverPanel", GameOverOverlayColor());

            BuildMainMenu();
            BuildTutorial();
            BuildGamePanel();
            BuildPausePanel();
            BuildSettingsPanel();
            BuildRecordsPanel();
            BuildGameOverPanel();

            isNarrowLayout = Screen.width < Screen.height || Screen.width < 900;
            UpdateResponsiveLayout();
        }

        private void EnsureCanvas()
        {
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.GetComponent<Canvas>();
            }

            canvas.gameObject.SetActive(true);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = 0;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            safeAreaRoot = canvas.transform.Find("SafeArea") as RectTransform;
            if (safeAreaRoot == null)
            {
                safeAreaRoot = CreateRect(canvas.transform, "SafeArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            ApplySafeArea(true);
        }

        private Font LoadUiFont()
        {
            Font resourceFont = Resources.Load<Font>(UiFontResource);
            if (resourceFont != null)
            {
                return resourceFont;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(transform, false);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            eventSystem.gameObject.SetActive(true);

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.AssignDefaultActions();
        }

        private void ApplySafeArea(bool force = false)
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (!force && safeArea == lastSafeArea)
            {
                return;
            }

            lastSafeArea = safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private void BuildMainMenu()
        {
            BuildMainMenuBackground();

            mainMenuCard = CreateRect(mainMenuPanel.transform, "MenuCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 900f));
            VerticalLayoutGroup layout = AddVerticalLayout(mainMenuCard, 16f, new RectOffset(52, 52, 58, 58), TextAnchor.UpperCenter);
            layout.childForceExpandWidth = true;

            CreateText(mainMenuCard, "ТЕТРИС", 66, TextAnchor.MiddleCenter, 78f);
            AddSpacer(mainMenuCard, 62f);

            CreateMenuButton(mainMenuCard, "Новая игра", sessionController.StartNewGame);
            CreateMenuButton(mainMenuCard, "Рекорды", () => stateMachine.ChangeState(AppState.Records));
            CreateMenuButton(mainMenuCard, "Настройки", () => stateMachine.ChangeState(AppState.Settings));
            CreateMenuButton(mainMenuCard, "Обучение", () =>
            {
                tutorialStep = 0;
                UpdateTutorialStep();
                stateMachine.ChangeState(AppState.Tutorial);
            });
            CreateMenuButton(mainMenuCard, "Выход", Application.Quit);
        }

        private void BuildMainMenuBackground()
        {
            Sprite backgroundSprite = LoadSpriteResource(MainMenuBackgroundResource, ref runtimeMainMenuBackgroundSprite);
            if (backgroundSprite != null)
            {
                CreateFullscreenBackground(mainMenuPanel.transform, "MenuBackgroundImage", backgroundSprite, 0);
            }

            RectTransform overlay = CreateRect(mainMenuPanel.transform, "MenuBackgroundOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.SetSiblingIndex(backgroundSprite != null ? 1 : 0);

            Image overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0.015f, 0.018f, 0.026f, backgroundSprite != null ? 0.28f : 0.18f);
            overlayImage.raycastTarget = false;
        }

        private Sprite LoadSpriteResource(string resourceName, ref Sprite runtimeSprite)
        {
            Sprite sprite = Resources.Load<Sprite>(resourceName);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourceName);
            if (texture == null)
            {
                return null;
            }

            runtimeSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            return runtimeSprite;
        }

        private RectTransform CreateFullscreenBackground(Transform parent, string name, Sprite sprite, int siblingIndex)
        {
            RectTransform backgroundRect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            backgroundRect.SetSiblingIndex(siblingIndex);

            Image image = backgroundRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RegisterBackground(image);

            AspectRatioFitter fitter = backgroundRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

            return backgroundRect;
        }

        private void BuildTutorial()
        {
            BuildSharedBlueBackground(tutorialPanel.transform, "TutorialBackground");

            tutorialCard = CreateRect(tutorialPanel.transform, "TutorialCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 720f));
            AddVerticalLayout(tutorialCard, 22f, new RectOffset(46, 46, 48, 48), TextAnchor.MiddleCenter);

            tutorialTitleText = CreateText(tutorialCard, "", 42, TextAnchor.MiddleCenter, 74f);
            tutorialBodyText = CreateText(tutorialCard, "", 28, TextAnchor.MiddleCenter, 330f);

            RectTransform buttons = CreateRect(tutorialCard, "TutorialButtons", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(720f, 82f));
            AddHorizontalLayout(buttons, 16f, new RectOffset(0, 0, 0, 0));
            AddLayoutElement(buttons.gameObject, 720f, 82f);

            CreateMenuButton(buttons, "Назад", PreviousTutorialStep);
            tutorialNextObject = CreateMenuButton(buttons, "Далее", NextTutorialStep).gameObject;
            tutorialStartObject = CreateMenuButton(buttons, "Начать игру", () =>
            {
                settingsService.MarkTutorialShown();
                audioAccessibilityService.Apply(settingsService.Settings, this);
                sessionController.StartNewGame();
            }).gameObject;

            UpdateTutorialStep();
        }

        private void BuildGamePanel()
        {
            BuildGameBackground();

            hudRect = CreateCard(gamePanel.transform, "Hud", new Vector2(0.5f, 1f), new Vector2(920f, 96f));
            StyleBlueCard(hudRect, HudCardColor());
            hudRect.anchoredPosition = new Vector2(0f, -70f);
            AddHorizontalLayout(hudRect, 14f, new RectOffset(16, 16, 12, 12));
            scoreText = CreateHudText(hudRect, "Очки\n0");
            linesText = CreateHudText(hudRect, "Линии\n0");
            levelText = CreateHudText(hudRect, "Уровень\n1");
            timeText = CreateHudText(hudRect, "Время\n00:00");

            boardRect = CreateRect(gamePanel.transform, "Board", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(462f, 902f));
            Image boardBackground = boardRect.gameObject.AddComponent<Image>();
            Sprite boardSprite = LoadSpriteResource(BoardBackgroundResource, ref runtimeBoardBackgroundSprite);
            if (boardSprite != null)
            {
                boardBackground.sprite = boardSprite;
                boardBackground.type = Image.Type.Simple;
                boardBackground.preserveAspect = false;
                boardBackground.color = Color.white;
            }
            else
            {
                boardBackground.color = new Color(0.025f, 0.034f, 0.043f, 1f);
            }

            RegisterBackground(boardBackground);

            boardGrid = boardRect.gameObject.AddComponent<GridLayoutGroup>();
            boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boardGrid.constraintCount = BoardModel.Width;
            boardGrid.cellSize = new Vector2(42f, 42f);
            boardGrid.spacing = new Vector2(2f, 2f);
            boardGrid.padding = new RectOffset(12, 12, 12, 12);
            boardGrid.childAlignment = TextAnchor.MiddleCenter;

            boardCells = new Image[BoardModel.Width, BoardModel.Height];
            for (int y = BoardModel.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < BoardModel.Width; x++)
                {
                    boardCells[x, y] = CreateCell(boardRect, "Cell_" + x + "_" + y);
                }
            }

            holdPanelRect = BuildPreviewPanel("Удержание", new Vector2(0.5f, 0.5f), new Vector2(-390f, 330f), out holdCells, out holdPreviewGrid, out holdPreviewGridLayout);
            nextPanelRect = BuildPreviewPanel("Следующая\nфигура", new Vector2(0.5f, 0.5f), new Vector2(390f, 330f), out nextCells, out nextPreviewGrid, out nextPreviewGridLayout);

            desktopPauseButton = CreateFloatingButton(gamePanel.transform, "Пауза", inputRouter.SendPause, new Vector2(1f, 1f), new Vector2(-100f, -70f), new Vector2(154f, 64f)).gameObject;
            mobilePauseButton = CreateFloatingButton(gamePanel.transform, "Пауза", inputRouter.SendPause, new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(154f, 72f)).gameObject;

            BuildKeyboardHints();

            mobileControlsRect = CreateRect(gamePanel.transform, "MobileControls", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 400f));
            mobileControlsRect.offsetMin = new Vector2(24f, 28f);
            mobileControlsRect.offsetMax = new Vector2(-24f, 428f);
            mobileControls = mobileControlsRect.gameObject;
            BuildMobileControls();
        }

        private void BuildGameBackground()
        {
            Sprite backgroundSprite = LoadSpriteResource(GameBackgroundResource, ref runtimeGameBackgroundSprite);
            if (backgroundSprite != null)
            {
                CreateFullscreenBackground(gamePanel.transform, "GameBackgroundImage", backgroundSprite, 0);
            }

            RectTransform overlay = CreateRect(gamePanel.transform, "GameBackgroundOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.SetSiblingIndex(backgroundSprite != null ? 1 : 0);

            Image overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0.005f, 0.010f, 0.020f, backgroundSprite != null ? 0.16f : 0.08f);
            overlayImage.raycastTarget = false;
        }

        private void BuildSharedBlueBackground(Transform parent, string prefix)
        {
            Sprite backgroundSprite = LoadSpriteResource(GameBackgroundResource, ref runtimeGameBackgroundSprite);
            if (backgroundSprite != null)
            {
                CreateFullscreenBackground(parent, prefix + "Image", backgroundSprite, 0);
            }

            RectTransform overlay = CreateRect(parent, prefix + "Overlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.SetSiblingIndex(backgroundSprite != null ? 1 : 0);

            Image overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0.030f, 0.090f, backgroundSprite != null ? 0.30f : 0.10f);
            overlayImage.raycastTarget = false;
        }

        private void BuildMobileControls()
        {
            // Верхний ряд растянут на всю ширину зоны управления (зона привязана к краям
            // экрана с отступами), поэтому кнопки не выходят за пределы на любом экране.
            RectTransform topRow = CreateRect(mobileControls.transform, "MobileTopRow", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -85f), new Vector2(0f, 150f));
            AddHorizontalLayout(topRow, 18f, new RectOffset(0, 0, 0, 0));
            CreateControlButton(topRow, "Влево", inputRouter.SendMoveLeft);
            CreateControlButton(topRow, "Поворот", inputRouter.SendRotate);
            CreateControlButton(topRow, "Вправо", inputRouter.SendMoveRight);

            // Нижний ряд чуть уже верхнего (по 100 px отступа с каждой стороны) — сохраняет прежнюю
            // визуальную иерархию (две широкие кнопки под тремя).
            RectTransform bottomRow = CreateRect(mobileControls.transform, "MobileBottomRow", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 85f), new Vector2(-200f, 150f));
            AddHorizontalLayout(bottomRow, 18f, new RectOffset(0, 0, 0, 0));
            CreateControlButton(bottomRow, "Удержать", inputRouter.SendHold);
            CreateControlButton(bottomRow, "Сброс", inputRouter.SendHardDrop);
        }

        private void BuildKeyboardHints()
        {
            keyboardHintRect = CreateRect(gamePanel.transform, "KeyboardHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(455f, -250f), new Vector2(430f, 250f));
            keyboardHint = keyboardHintRect.gameObject;

            AddVerticalLayout(keyboardHintRect, 8f, new RectOffset(4, 4, 0, 0), TextAnchor.UpperCenter);

            AddHintRow(keyboardHintRect, "A / D  или  ← / →", "движение");
            AddHintRow(keyboardHintRect, "W / ↑ / X", "поворот");
            AddHintRow(keyboardHintRect, "S / ↓", "ускорить");
            AddHintRow(keyboardHintRect, "Space", "сброс");
            AddHintRow(keyboardHintRect, "C / Esc", "hold / pause");
        }

        private void AddHintRow(Transform parent, string keys, string action)
        {
            RectTransform row = CreateRect(parent, "HintRow", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(420f, 42f));
            AddLayoutElement(row.gameObject, 420f, 42f);
            AddHorizontalLayout(row, 10f, new RectOffset(0, 0, 0, 0));

            Text keyText = CreateKeyText(row, keys, 210f);
            keyText.fontStyle = FontStyle.Bold;

            Text actionText = CreateText(row, action, 18, TextAnchor.MiddleLeft, 38f);
            LayoutElement actionLayout = actionText.GetComponent<LayoutElement>();
            actionLayout.preferredWidth = 190f;
            actionLayout.flexibleWidth = 1f;
        }

        private Text CreateKeyText(Transform parent, string value, float width)
        {
            RectTransform keyBox = CreateRect(parent, "KeyBox", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(width, 38f));
            AddLayoutElement(keyBox.gameObject, width, 38f);

            Image image = keyBox.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.015f, 0.16f, 0.34f, 0.74f);

            Text text = CreateText(keyBox, value, 19, TextAnchor.MiddleCenter, 34f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            return text;
        }

        private void BuildPausePanel()
        {
            RectTransform card = CreateRect(pausePanel.transform, "PauseCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 430f));
            AddVerticalLayout(card, 18f, new RectOffset(42, 42, 44, 44), TextAnchor.MiddleCenter);
            CreateText(card, "Пауза", 46, TextAnchor.MiddleCenter, 72f);
            CreateMenuButton(card, "Продолжить", sessionController.ResumeGame);
            CreateMenuButton(card, "Начать заново", inputRouter.SendRestart);
            CreateMenuButton(card, "Главное меню", sessionController.ReturnToMainMenu);
        }

        private void BuildSettingsPanel()
        {
            BuildSharedBlueBackground(settingsPanel.transform, "SettingsBackground");

            settingsCard = CreateRect(settingsPanel.transform, "SettingsCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 540f));
            AddVerticalLayout(settingsCard, 20f, new RectOffset(42, 42, 44, 44), TextAnchor.MiddleCenter);
            CreateText(settingsCard, "Настройки", 46, TextAnchor.MiddleCenter, 72f);
            soundButtonText = CreateMenuButton(settingsCard, "", () =>
            {
                settingsService.ToggleSound();
                audioAccessibilityService.Apply(settingsService.Settings, this);
            }).GetComponentInChildren<Text>();
            musicButtonText = CreateMenuButton(settingsCard, "", () =>
            {
                settingsService.ToggleMusic();
                audioAccessibilityService.Apply(settingsService.Settings, this);
            }).GetComponentInChildren<Text>();
            contrastButtonText = CreateMenuButton(settingsCard, "", () =>
            {
                settingsService.ToggleContrast();
                audioAccessibilityService.Apply(settingsService.Settings, this);
            }).GetComponentInChildren<Text>();
            CreateMenuButton(settingsCard, "Назад", () => stateMachine.ChangeState(AppState.MainMenu));
            UpdateSettingsLabels();
        }

        private void BuildRecordsPanel()
        {
            BuildSharedBlueBackground(recordsPanel.transform, "RecordsBackground");

            recordsCard = CreateRect(recordsPanel.transform, "RecordsCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 800f));
            AddVerticalLayout(recordsCard, 18f, new RectOffset(42, 42, 44, 44), TextAnchor.MiddleCenter);
            CreateText(recordsCard, "Рекорды", 48, TextAnchor.MiddleCenter, 78f);

            recordsTableCard = CreateCard(recordsCard, "RecordsTable", Vector2.zero, new Vector2(790f, 470f));
            StyleBlueCard(recordsTableCard, PanelInnerCardColor());
            AddVerticalLayout(recordsTableCard, 12f, new RectOffset(24, 24, 22, 22), TextAnchor.UpperCenter);
            AddLayoutElement(recordsTableCard.gameObject, 790f, 470f);

            RectTransform header = CreateRecordsHeaderRow(recordsTableCard);
            AddHeaderCell(header, "Место", 90f);
            AddHeaderCell(header, "Очки", 160f);
            AddHeaderCell(header, "Линии", 130f);
            AddHeaderCell(header, "Уровень", 130f);
            AddHeaderCell(header, "Время", 160f);
            CreateSeparator(recordsTableCard);

            recordsRowsRoot = CreateRect(recordsTableCard, "RecordsRows", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(740f, 300f));
            AddVerticalLayout(recordsRowsRoot, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.UpperCenter);
            AddLayoutElement(recordsRowsRoot.gameObject, 740f, 300f);

            recordsEmptyText = CreateText(recordsTableCard, "Пока нет сохранённых результатов", 24, TextAnchor.MiddleCenter, 120f);
            CreateMenuButton(recordsCard, "Назад", () => stateMachine.ChangeState(AppState.MainMenu));
        }

        private void BuildGameOverPanel()
        {
            gameOverCard = CreateCard(gameOverPanel.transform, "GameOverCard", new Vector2(0.5f, 0.5f), new Vector2(680f, 650f));
            StyleGameOverCard(gameOverCard);
            AddVerticalLayout(gameOverCard, 16f, new RectOffset(44, 44, 46, 46), TextAnchor.MiddleCenter);
            CreateText(gameOverCard, "Игра окончена", 44, TextAnchor.MiddleCenter, 70f);
            gameOverSummaryText = CreateText(gameOverCard, "", 26, TextAnchor.MiddleCenter, 150f);
            gameOverTopText = CreateText(gameOverCard, "", 23, TextAnchor.MiddleCenter, 70f);
            CreateMenuButton(gameOverCard, "Новая игра", inputRouter.SendRestart);
            CreateMenuButton(gameOverCard, "Главное меню", sessionController.ReturnToMainMenu);
        }

        private void StyleGameOverCard(RectTransform card)
        {
            StyleBlueCard(card, GameOverCardColor());
        }

        private void StyleBlueCard(RectTransform card, Color color)
        {
            Image background = card.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = GetRoundedButtonSprite();
                background.type = Image.Type.Sliced;
                background.color = color;
            }

            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.04f, 0.10f, 0.70f);
            shadow.effectDistance = new Vector2(0f, -10f);
        }

        private RectTransform BuildPreviewPanel(
            string title,
            Vector2 anchor,
            Vector2 position,
            out Image[,] cells,
            out GridLayoutGroup previewGrid,
            out LayoutElement previewGridLayout)
        {
            RectTransform panel = CreateCard(gamePanel.transform, title, anchor, new Vector2(250f, 300f));
            StyleBlueCard(panel, PreviewPanelColor());
            panel.anchoredPosition = position;
            AddVerticalLayout(panel, 8f, new RectOffset(22, 22, 16, 18), TextAnchor.UpperCenter);
            CreateText(panel, title, 21, TextAnchor.MiddleCenter, 44f);

            RectTransform gridRect = CreateRect(panel, "PreviewGrid", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(168f, 168f));
            previewGridLayout = AddLayoutElement(gridRect.gameObject, 168f, 168f);
            previewGrid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            previewGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            previewGrid.constraintCount = 4;
            previewGrid.cellSize = new Vector2(39f, 39f);
            previewGrid.spacing = new Vector2(4f, 4f);
            previewGrid.childAlignment = TextAnchor.MiddleCenter;

            cells = new Image[4, 4];
            for (int y = 3; y >= 0; y--)
            {
                for (int x = 0; x < 4; x++)
                {
                    cells[x, y] = CreateCell(gridRect, "PreviewCell_" + x + "_" + y);
                }
            }

            return panel;
        }

        private void ShowState(AppState state)
        {
            mainMenuPanel.SetActive(state == AppState.MainMenu);
            tutorialPanel.SetActive(state == AppState.Tutorial);
            gamePanel.SetActive(state == AppState.Playing || state == AppState.Paused || state == AppState.GameOver);
            pausePanel.SetActive(state == AppState.Paused);
            settingsPanel.SetActive(state == AppState.Settings);
            recordsPanel.SetActive(state == AppState.Records);
            gameOverPanel.SetActive(state == AppState.GameOver);

            if (state == AppState.Tutorial)
            {
                UpdateTutorialStep();
            }

            if (state == AppState.Records)
            {
                RefreshRecords(storageService.LoadScores());
            }
        }

        private void PreviousTutorialStep()
        {
            if (tutorialStep <= 0)
            {
                stateMachine.ChangeState(AppState.MainMenu);
                return;
            }

            tutorialStep--;
            UpdateTutorialStep();
        }

        private void NextTutorialStep()
        {
            tutorialStep = Mathf.Min(tutorialStep + 1, tutorialTitles.Length - 1);
            UpdateTutorialStep();
        }

        private void UpdateTutorialStep()
        {
            if (tutorialTitleText == null)
            {
                return;
            }

            tutorialTitleText.text = tutorialTitles[tutorialStep];
            tutorialBodyText.text = tutorialTexts[tutorialStep];
            bool lastStep = tutorialStep == tutorialTitles.Length - 1;
            tutorialNextObject.SetActive(!lastStep);
            tutorialStartObject.SetActive(lastStep);
        }

        private void RenderPreview(Image[,] cells, TetrominoType type)
        {
            Color empty = PreviewCellEmptyColor();
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    cells[x, y].color = empty;
                }
            }

            if (type == TetrominoType.None)
            {
                return;
            }

            Vector2Int[] shape = TetrominoData.GetShape(type, 0);
            int minX = 99;
            int maxX = -99;
            int minY = 99;
            int maxY = -99;

            for (int i = 0; i < shape.Length; i++)
            {
                minX = Mathf.Min(minX, shape[i].x);
                maxX = Mathf.Max(maxX, shape[i].x);
                minY = Mathf.Min(minY, shape[i].y);
                maxY = Mathf.Max(maxY, shape[i].y);
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            int offsetX = (4 - width) / 2 - minX;
            int offsetY = (4 - height) / 2 - minY;

            for (int i = 0; i < shape.Length; i++)
            {
                int x = shape[i].x + offsetX;
                int y = shape[i].y + offsetY;
                if (x >= 0 && x < 4 && y >= 0 && y < 4)
                {
                    cells[x, y].color = TetrominoData.GetColor(type, contrastMode);
                }
            }
        }

        private void UpdateResponsiveLayout()
        {
            if (mobileControls != null)
            {
                mobileControls.SetActive(isNarrowLayout);
            }

            if (keyboardHint != null)
            {
                keyboardHint.SetActive(!isNarrowLayout);
            }

            if (desktopPauseButton != null)
            {
                desktopPauseButton.SetActive(!isNarrowLayout);
            }

            if (mobilePauseButton != null)
            {
                mobilePauseButton.SetActive(isNarrowLayout);
            }

            if (boardRect == null)
            {
                return;
            }

            if (isNarrowLayout)
            {
                mainMenuCard.sizeDelta = new Vector2(900f, 940f);
                tutorialCard.sizeDelta = new Vector2(860f, 760f);
                recordsCard.sizeDelta = new Vector2(900f, 820f);
                gameOverCard.sizeDelta = new Vector2(720f, 680f);

                hudRect.sizeDelta = new Vector2(920f, 102f);
                hudRect.anchoredPosition = new Vector2(0f, -76f);

                boardRect.anchoredPosition = new Vector2(0f, 90f);
                boardRect.sizeDelta = new Vector2(462f, 902f);
                boardGrid.cellSize = new Vector2(42f, 42f);
                boardGrid.spacing = new Vector2(2f, 2f);
                boardGrid.padding = new RectOffset(12, 12, 12, 12);

                holdPanelRect.anchoredPosition = new Vector2(-390f, 380f);
                nextPanelRect.anchoredPosition = new Vector2(390f, 380f);
                holdPanelRect.sizeDelta = new Vector2(250f, 300f);
                nextPanelRect.sizeDelta = new Vector2(250f, 300f);
                SetPreviewGridSize(holdPreviewGrid, holdPreviewGridLayout, 168f, 39f, 4f);
                SetPreviewGridSize(nextPreviewGrid, nextPreviewGridLayout, 168f, 39f, 4f);

                mobileControlsRect.offsetMin = new Vector2(24f, 28f);
                mobileControlsRect.offsetMax = new Vector2(-24f, 428f);
            }
            else
            {
                mainMenuCard.sizeDelta = new Vector2(760f, 860f);
                tutorialCard.sizeDelta = new Vector2(820f, 640f);
                recordsCard.sizeDelta = new Vector2(860f, 700f);
                gameOverCard.sizeDelta = new Vector2(640f, 650f);

                hudRect.sizeDelta = new Vector2(840f, 86f);
                hudRect.anchoredPosition = new Vector2(0f, -58f);

                boardRect.anchoredPosition = new Vector2(0f, -10f);
                boardRect.sizeDelta = new Vector2(340f, 660f);
                boardGrid.cellSize = new Vector2(30f, 30f);
                boardGrid.spacing = new Vector2(2f, 2f);
                boardGrid.padding = new RectOffset(8, 8, 8, 8);

                holdPanelRect.anchoredPosition = new Vector2(-380f, 100f);
                nextPanelRect.anchoredPosition = new Vector2(380f, 100f);
                holdPanelRect.sizeDelta = new Vector2(240f, 290f);
                nextPanelRect.sizeDelta = new Vector2(240f, 290f);
                SetPreviewGridSize(holdPreviewGrid, holdPreviewGridLayout, 160f, 37f, 4f);
                SetPreviewGridSize(nextPreviewGrid, nextPreviewGridLayout, 160f, 37f, 4f);

                keyboardHintRect.sizeDelta = new Vector2(430f, 250f);
                keyboardHintRect.anchoredPosition = new Vector2(455f, -250f);
            }
        }

        private void UpdateSettingsLabels()
        {
            if (settingsService == null || soundButtonText == null)
            {
                return;
            }

            soundButtonText.text = settingsService.Settings.soundOn ? "Звук: включён" : "Звук: выключен";
            musicButtonText.text = settingsService.Settings.musicOn ? "Музыка: включена" : "Музыка: выключена";
            contrastButtonText.text = settingsService.Settings.contrastMode ? "Контраст: включён" : "Контраст: выключен";
        }

        private void ApplyPalette()
        {
            Color panelColor = BackgroundColor();
            SetPanelColor(mainMenuPanel, panelColor);
            SetPanelColor(tutorialPanel, panelColor);
            SetPanelColor(settingsPanel, panelColor);
            SetPanelColor(recordsPanel, panelColor);
            SetPanelColor(gamePanel, panelColor);
            SetPanelColor(gameOverPanel, GameOverOverlayColor());

            Image gameOverCardImage = gameOverCard != null ? gameOverCard.GetComponent<Image>() : null;
            if (gameOverCardImage != null)
            {
                gameOverCardImage.color = GameOverCardColor();
            }

            SetCardColor(settingsCard, PanelCardColor());
            SetCardColor(recordsTableCard, PanelInnerCardColor());
            SetCardColor(hudRect, HudCardColor());
            SetCardColor(holdPanelRect, PreviewPanelColor());
            SetCardColor(nextPanelRect, PreviewPanelColor());

            ApplyBackgroundContrast();
        }

        private void RegisterBackground(Image image)
        {
            backgroundImages.Add(image);
            backgroundOriginalColors.Add(image.color);
        }

        private void ApplyBackgroundContrast()
        {
            // В контрастном режиме приглушаем (затемняем) декоративные фоны, оставляя их видимыми,
            // чтобы яркие фигуры выделялись, но фон не становился полностью чёрным.
            const float dim = 0.4f;
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                if (backgroundImages[i] == null)
                {
                    continue;
                }

                Color original = backgroundOriginalColors[i];
                backgroundImages[i].color = contrastMode
                    ? new Color(original.r * dim, original.g * dim, original.b * dim, original.a)
                    : original;
            }
        }

        private Color BackgroundColor()
        {
            return contrastMode ? Color.black : new Color(0.045f, 0.060f, 0.078f, 1f);
        }

        private Color CardColor()
        {
            return contrastMode ? new Color(0.04f, 0.04f, 0.04f, 0.96f) : new Color(0.075f, 0.105f, 0.135f, 0.94f);
        }

        private Color PanelCardColor()
        {
            return contrastMode ? new Color(0.02f, 0.09f, 0.14f, 0.94f) : new Color(0.012f, 0.18f, 0.34f, 0.86f);
        }

        private Color PanelInnerCardColor()
        {
            return contrastMode ? new Color(0.01f, 0.06f, 0.10f, 0.88f) : new Color(0.005f, 0.075f, 0.16f, 0.54f);
        }

        private Color HudCardColor()
        {
            return contrastMode ? new Color(0.01f, 0.07f, 0.12f, 0.86f) : new Color(0.005f, 0.14f, 0.30f, 0.56f);
        }

        private Color PreviewPanelColor()
        {
            return contrastMode ? new Color(0.01f, 0.07f, 0.12f, 0.88f) : new Color(0.006f, 0.13f, 0.27f, 0.58f);
        }

        private Color GameOverCardColor()
        {
            return contrastMode ? new Color(0.02f, 0.12f, 0.18f, 0.94f) : new Color(0.015f, 0.24f, 0.42f, 0.92f);
        }

        private Color GameOverOverlayColor()
        {
            return contrastMode ? new Color(0f, 0f, 0f, 0.62f) : new Color(0f, 0.045f, 0.11f, 0.58f);
        }

        private Color ButtonColor()
        {
            return contrastMode ? new Color(0.12f, 0.62f, 0.78f, 1f) : new Color(0.08f, 0.72f, 0.92f, 0.94f);
        }

        private Color BoardCellEmptyColor()
        {
            // Контраст: непрозрачная тёмно-серая клетка — поле читается как чёткая решётка.
            return contrastMode ? new Color(0.12f, 0.12f, 0.14f, 1f) : new Color(0.02f, 0.13f, 0.24f, 0.30f);
        }

        private Color PreviewCellEmptyColor()
        {
            return contrastMode ? new Color(0.12f, 0.12f, 0.14f, 1f) : new Color(0.02f, 0.11f, 0.20f, 0.54f);
        }

        private void SetPanelColor(GameObject panel, Color color)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void SetCardColor(RectTransform card, Color color)
        {
            Image image = card != null ? card.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = color;
            }
        }

        private GameObject CreatePanel(string name, Color color)
        {
            RectTransform rect = CreateRect(safeAreaRoot, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private RectTransform CreateCard(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            RectTransform card = CreateRect(parent, name, anchor, anchor, Vector2.zero, size);
            Image background = card.gameObject.AddComponent<Image>();
            background.color = CardColor();
            return card;
        }

        private Text CreateText(Transform parent, string value, int size, TextAnchor alignment, float preferredHeight)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.93f, 0.97f, 1f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.06f, 0.12f, 0.75f);
            shadow.effectDistance = new Vector2(0f, -2f);

            LayoutElement layout = textObject.GetComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            return text;
        }

        private Text CreateHudText(Transform parent, string value)
        {
            Text text = CreateText(parent, value, 24, TextAnchor.MiddleCenter, 72f);
            LayoutElement layout = text.GetComponent<LayoutElement>();
            layout.preferredWidth = 200f;
            layout.flexibleWidth = 1f;
            return text;
        }

        private Button CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(parent, label, action, 26);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.preferredHeight = 70f;
            layout.minHeight = 62f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 1f;
            return button;
        }

        private Button CreateControlButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(parent, label, action, 32);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.preferredHeight = 150f;
            layout.minHeight = 140f;
            layout.flexibleWidth = 1f;
            return button;
        }

        private Button CreateFloatingButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Button button = CreateButton(parent, label, action, 22);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return button;
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, int fontSize)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = ButtonColor();

            Shadow buttonShadow = buttonObject.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0f, 0.06f, 0.15f, 0.55f);
            buttonShadow.effectDistance = new Vector2(0f, -7f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor();
            colors.highlightedColor = new Color(0.22f, 0.86f, 1f, 1f);
            colors.pressedColor = new Color(0.04f, 0.48f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.12f, 0.18f, 0.22f, 0.7f);
            button.colors = colors;

            RectTransform glowRect = CreateRect(buttonObject.transform, "ButtonTopGlow", new Vector2(0f, 0.58f), Vector2.one, Vector2.zero, Vector2.zero);
            glowRect.offsetMin = new Vector2(9f, 0f);
            glowRect.offsetMax = new Vector2(-9f, -8f);
            Image glow = glowRect.gameObject.AddComponent<Image>();
            glow.sprite = GetRoundedButtonSprite();
            glow.type = Image.Type.Sliced;
            glow.color = new Color(1f, 1f, 1f, 0.16f);
            glow.raycastTarget = false;

            Text text = CreateText(buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter, 64f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);

            return button;
        }

        private Sprite GetRoundedButtonSprite()
        {
            if (roundedButtonSprite != null)
            {
                return roundedButtonSprite;
            }

            const int width = 128;
            const int height = 64;
            const float radius = 31f;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "RuntimeRoundedButton";
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color fill = Color.white;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, width - radius - 1f);
                    float nearestY = Mathf.Clamp(y, radius, height - radius - 1f);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, alpha > 0f ? new Color(fill.r, fill.g, fill.b, alpha) : clear);
                }
            }

            texture.Apply();
            roundedButtonSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(32f, 32f, 32f, 32f));

            return roundedButtonSprite;
        }

        private RectTransform CreateTableRow(Transform parent, bool strong)
        {
            RectTransform row = CreateRect(parent, "RecordRow", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(730f, 50f));
            Image image = row.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = strong ? new Color(0.012f, 0.105f, 0.22f, 0.74f) : new Color(0.010f, 0.070f, 0.16f, 0.62f);
            AddHorizontalLayout(row, 6f, new RectOffset(8, 8, 0, 0));
            AddLayoutElement(row.gameObject, 730f, 50f);
            return row;
        }

        private RectTransform CreateRecordsHeaderRow(Transform parent)
        {
            RectTransform row = CreateRect(parent, "RecordsHeader", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(730f, 38f));
            AddHorizontalLayout(row, 6f, new RectOffset(8, 8, 0, 0));
            AddLayoutElement(row.gameObject, 730f, 38f);
            return row;
        }

        private void AddHeaderCell(Transform parent, string value, float width)
        {
            Text text = CreateText(parent, value, 18, TextAnchor.MiddleCenter, 34f);
            text.color = new Color(0.72f, 0.90f, 1f, 0.92f);

            LayoutElement layout = text.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.flexibleWidth = 0f;
        }

        private void CreateSeparator(Transform parent)
        {
            RectTransform line = CreateRect(parent, "RecordsHeaderSeparator", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(730f, 2f));
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(0.62f, 0.88f, 1f, 0.30f);
            AddLayoutElement(line.gameObject, 730f, 2f);
        }

        private void AddTableCell(Transform parent, string value, float width, int size, TextAnchor alignment)
        {
            Text text = CreateText(parent, value, size, alignment, 44f);
            LayoutElement layout = text.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.flexibleWidth = 0f;
        }

        private Image CreateCell(Transform parent, string name)
        {
            GameObject cellObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(parent, false);
            Image image = cellObject.GetComponent<Image>();
            image.color = PreviewCellEmptyColor();
            return image;
        }

        private RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private VerticalLayoutGroup AddVerticalLayout(RectTransform target, float spacing, RectOffset padding, TextAnchor alignment)
        {
            VerticalLayoutGroup layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private HorizontalLayoutGroup AddHorizontalLayout(RectTransform target, float spacing, RectOffset padding)
        {
            HorizontalLayoutGroup layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        private LayoutElement AddLayoutElement(GameObject target, float width, float height)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return layout;
        }

        private void SetPreviewGridSize(GridLayoutGroup grid, LayoutElement layout, float gridSize, float cellSize, float spacing)
        {
            if (grid == null || layout == null)
            {
                return;
            }

            layout.preferredWidth = gridSize;
            layout.preferredHeight = gridSize;
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(spacing, spacing);
        }

        private void AddSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private string FormatTime(int seconds)
        {
            int minutes = seconds / 60;
            int rest = seconds % 60;
            return minutes.ToString("00") + ":" + rest.ToString("00");
        }
    }
}
