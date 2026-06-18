using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLastAethon.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadGame()
        {
            StartCoroutine(LoadSceneWithFade("Game"));
        }

        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
            fadeCanvasGroup.blocksRaycasts = to > 0.5f;
        }
    }
}
