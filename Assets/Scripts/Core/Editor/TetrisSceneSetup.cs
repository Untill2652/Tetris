#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TetrisCourse.EditorTools
{
    public static class TetrisSceneSetup
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tetris/Setup Scene")]
        public static void SetupGameScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            GameObject app = FindOrCreateRoot("App");
            GameObject gameRoot = FindOrCreateRoot("GameRoot");
            GameObject uiRoot = FindOrCreateRoot("UIRoot");

            if (app.GetComponent<AppBootstrapper>() == null)
            {
                app.AddComponent<AppBootstrapper>();
            }

            if (app.GetComponent<InputRouter>() == null)
            {
                app.AddComponent<InputRouter>();
            }

            if (gameRoot.GetComponent<GameSessionController>() == null)
            {
                gameRoot.AddComponent<GameSessionController>();
            }

            if (uiRoot.GetComponent<UIManager>() == null)
            {
                uiRoot.AddComponent<UIManager>();
            }

            ConfigureMainCamera();
            ConfigureCanvas(uiRoot.transform);
            ConfigureEventSystem(uiRoot.transform);
            AddGameSceneToBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Tetris scene setup complete: scene is ready for Play on Display 1.");
        }

        private static GameObject FindOrCreateRoot(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                existing.transform.SetParent(null);
                existing.SetActive(true);
                return existing;
            }

            return new GameObject(objectName);
        }

        private static void ConfigureMainCamera()
        {
            Camera camera = Camera.main;
            GameObject cameraObject = camera != null ? camera.gameObject : GameObject.Find("Main Camera");

            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
            }

            camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            cameraObject.SetActive(true);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            camera.targetDisplay = 0;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);

            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }
        }

        private static void ConfigureCanvas(Transform uiRoot)
        {
            Canvas canvas = uiRoot.GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(uiRoot, false);
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
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void ConfigureEventSystem(Transform uiRoot)
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(uiRoot, false);
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

        private static void AddGameSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = scenes.Count - 1; i >= 0; i--)
            {
                if (scenes[i].path == GameScenePath)
                {
                    scenes.RemoveAt(i);
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
