using UnityEngine;

namespace TetrisCourse
{
    public sealed class AppBootstrapper : MonoBehaviour
    {
        private static AppBootstrapper instance;

        private AppStateMachine stateMachine;
        private StorageService storageService;
        private SettingsService settingsService;
        private AudioAccessibilityService audioAccessibilityService;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapperInScene()
        {
            if (FindFirstObjectByType<AppBootstrapper>() != null)
            {
                return;
            }

            GameObject appObject = GameObject.Find("App");
            if (appObject == null)
            {
                appObject = new GameObject("App");
            }

            appObject.AddComponent<AppBootstrapper>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = "App";
        }

        private void Start()
        {
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            GameObject gameRoot = FindOrCreateRoot("GameRoot");
            GameObject uiRoot = FindOrCreateRoot("UIRoot");
            gameObject.SetActive(true);
            gameRoot.SetActive(true);
            uiRoot.SetActive(true);
            ConfigureMainCamera();

            InputRouter inputRouter = GetOrAdd<InputRouter>(gameObject);
            GameSessionController sessionController = GetOrAdd<GameSessionController>(gameRoot);
            UIManager uiManager = GetOrAdd<UIManager>(uiRoot);

            BoardModel board = new BoardModel();
            PieceService pieceService = new PieceService();
            RuleService ruleService = new RuleService();
            ScoreService scoreService = new ScoreService();

            storageService = new StorageService();
            settingsService = new SettingsService(storageService);
            audioAccessibilityService = new AudioAccessibilityService(CreateMusicSource());
            stateMachine = new AppStateMachine();

            uiManager.Initialize(stateMachine, inputRouter, sessionController, storageService, settingsService, audioAccessibilityService);
            sessionController.Initialize(board, pieceService, ruleService, scoreService, storageService, stateMachine, uiManager);
            inputRouter.CommandSent += sessionController.HandleCommand;

            audioAccessibilityService.Apply(settingsService.Settings, uiManager);
            uiManager.RefreshRecords(storageService.LoadScores());

            // Состояние по умолчанию уже MainMenu, поэтому событие StateChanged может не вызваться.
            // Явно показываем меню, чтобы после Play пользователь сразу видел игру.
            uiManager.ForceShowMainMenu();
        }

        private AudioSource CreateMusicSource()
        {
            // Фоновая музыка проигрывается зациклено через отдельный AudioSource.
            AudioSource musicSource = GetOrAdd<AudioSource>(gameObject);
            musicSource.clip = Resources.Load<AudioClip>("BackgroundMusic");
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.5f;

            if (musicSource.clip != null)
            {
                musicSource.Play();
            }

            return musicSource;
        }

        private void ConfigureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject == null)
                {
                    cameraObject = new GameObject("Main Camera");
                }

                camera = GetOrAdd<Camera>(cameraObject);
                cameraObject.tag = "MainCamera";

                if (cameraObject.GetComponent<AudioListener>() == null)
                {
                    cameraObject.AddComponent<AudioListener>();
                }
            }

            camera.gameObject.SetActive(true);
            camera.targetDisplay = 0;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
        }

        private GameObject FindOrCreateRoot(string objectName)
        {
            GameObject root = GameObject.Find(objectName);
            if (root != null)
            {
                return root;
            }

            return new GameObject(objectName);
        }

        private T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
