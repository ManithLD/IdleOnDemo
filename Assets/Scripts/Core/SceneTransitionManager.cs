using System.Collections;
using IdleOnDemo.Gameplay.Environment;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleOnDemo.Core
{
    /// <summary>
    /// Coordinates persistent player and camera scene transitions with a full-screen fade.
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private const float FadeDuration = 0.5f;
        private const int FadeCanvasSortOrder = 100;

        private static SceneTransitionManager instance;

        private CanvasGroup fadeCanvasGroup;
        private Coroutine activeTransition;

        /// <summary>
        /// Gets the active scene transition manager instance.
        /// </summary>
        public static SceneTransitionManager Instance => instance;

        /// <summary>
        /// Enforces singleton lifetime and creates the persistent fade canvas.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }

        /// <summary>
        /// Starts an asynchronous scene transition and moves the persistent player to a target spawn point.
        /// </summary>
        /// <param name="sceneName">The scene name to load.</param>
        /// <param name="spawnPointID">The spawn point identifier to locate in the destination scene.</param>
        /// <param name="playerTransform">The player transform that should persist and be teleported.</param>
        public void TransitionToScene(string sceneName, string spawnPointID, Transform playerTransform)
        {
            if (activeTransition != null)
            {
                return;
            }

            activeTransition = StartCoroutine(TransitionRoutine(sceneName, spawnPointID, playerTransform));
        }

        /// <summary>
        /// Executes the transition fade, scene load, persistence setup, spawn lookup, and player teleport.
        /// </summary>
        /// <param name="sceneName">The scene name to load.</param>
        /// <param name="spawnPointID">The spawn point identifier to locate in the destination scene.</param>
        /// <param name="playerTransform">The player transform that should persist and be teleported.</param>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private IEnumerator TransitionRoutine(string sceneName, string spawnPointID, Transform playerTransform)
        {
            yield return FadeTo(1f);

            Camera preservedCamera = Camera.main;
            PreservePlayerAndCamera(playerTransform);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                Debug.LogError($"Could not start scene load for '{sceneName}'.");
                yield return FadeTo(0f);
                activeTransition = null;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            RemoveLoadedSceneDuplicates(playerTransform, preservedCamera);
            TeleportPlayerToSpawn(spawnPointID, playerTransform);

            yield return FadeTo(0f);
            activeTransition = null;
        }

        /// <summary>
        /// Creates the always-on-top black fade canvas used during scene transitions.
        /// </summary>
        private void CreateFadeCanvas()
        {
            if (fadeCanvasGroup != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("Scene Transition Fade Canvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = FadeCanvasSortOrder;

            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;

            GameObject imageObject = new GameObject("Fade Image");
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = Color.black;

            RectTransform rectTransform = fadeImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Fades the transition canvas group to the requested alpha over the configured duration.
        /// </summary>
        /// <param name="targetAlpha">The final alpha value for the fade canvas.</param>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = fadeCanvasGroup.alpha;
            float elapsedTime = 0f;

            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;

            while (elapsedTime < FadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / FadeDuration);
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
        }

        /// <summary>
        /// Keeps the player and main camera alive through a single-mode scene load.
        /// </summary>
        /// <param name="playerTransform">The player transform to preserve.</param>
        private static void PreservePlayerAndCamera(Transform playerTransform)
        {
            if (playerTransform != null)
            {
                DontDestroyOnLoad(playerTransform.gameObject);
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                DontDestroyOnLoad(mainCamera.gameObject);
            }
        }

        /// <summary>
        /// Removes newly loaded duplicate player and camera objects so the preserved runtime objects remain authoritative.
        /// </summary>
        /// <param name="playerTransform">The persistent player transform that should survive the transition.</param>
        /// <param name="preservedCamera">The persistent camera that should survive the transition.</param>
        private static void RemoveLoadedSceneDuplicates(Transform playerTransform, Camera preservedCamera)
        {
            if (playerTransform != null)
            {
                PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
                foreach (PlayerController player in players)
                {
                    if (player.transform == playerTransform)
                    {
                        continue;
                    }

                    Destroy(player.gameObject);
                }
            }

            if (preservedCamera == null)
            {
                return;
            }

            preservedCamera.enabled = true;
            preservedCamera.tag = "MainCamera";

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            foreach (Camera camera in cameras)
            {
                if (camera == preservedCamera)
                {
                    continue;
                }

                camera.enabled = false;
                AudioListener audioListener = camera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }

                Destroy(camera.gameObject);
            }
        }

        /// <summary>
        /// Moves the persistent player to the destination spawn point when one exists.
        /// </summary>
        /// <param name="spawnPointID">The spawn point identifier to match.</param>
        /// <param name="playerTransform">The player transform to teleport.</param>
        private static void TeleportPlayerToSpawn(string spawnPointID, Transform playerTransform)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("Scene transition completed without a player transform to reposition.");
                return;
            }

            SpawnPoint targetSpawn = FindSpawnPoint(spawnPointID);
            if (targetSpawn == null)
            {
                Debug.LogWarning($"No SpawnPoint found with ID '{spawnPointID}' in scene '{SceneManager.GetActiveScene().name}'.");
                return;
            }

            playerTransform.position = targetSpawn.transform.position;
        }

        /// <summary>
        /// Finds a destination spawn point in the active scene by identifier.
        /// </summary>
        /// <param name="spawnPointID">The spawn point identifier to match.</param>
        /// <returns>The matching spawn point, or <c>null</c> if no matching point exists.</returns>
        private static SpawnPoint FindSpawnPoint(string spawnPointID)
        {
            SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint.SpawnPointID == spawnPointID)
                {
                    return spawnPoint;
                }
            }

            return null;
        }
    }
}
