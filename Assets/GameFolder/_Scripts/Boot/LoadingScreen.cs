using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SKC.Boot
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField ] private  Image loadingIMG;
        [SerializeField ] private  CanvasGroup canvasGroup;

        public static LoadingScreen Instance;
        private bool _fadeOperation = false;

        private void Awake()
        {
            // --- Singleton Pattern Enforcement ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SceneTransition(string sceneNameToRemove, string sceneNameToLoad)
        {
            StartCoroutine(LoadSceneWithTransition(sceneNameToLoad, sceneNameToRemove));
        }
        
        public IEnumerator LoadSceneWithTransition(string sceneToLoad, string sceneToUnload)
        {
            StartCoroutine(FadeOperation(true));

            while (!_fadeOperation)
            {
                yield return new  WaitForSeconds(.1f);
            }

            AsyncOperation loaderSceneAsyncLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            loaderSceneAsyncLoad.allowSceneActivation = false;

            while (loaderSceneAsyncLoad.progress < 0.9f)
            {
                loadingIMG.fillAmount = loaderSceneAsyncLoad.progress / 0.9f;
                yield return null;
            }
            
            loadingIMG.fillAmount = 1f;
            loaderSceneAsyncLoad.allowSceneActivation = true;
            yield return loaderSceneAsyncLoad;
            
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
            if (!string.IsNullOrEmpty(sceneToUnload) && SceneManager.GetSceneByName(sceneToUnload).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(sceneToUnload);
            }

            yield return new WaitForSeconds(0.50f);
            StartCoroutine(FadeOperation(false));
        }

        private IEnumerator FadeOperation(bool on)
        {
            if (on)
            {
                while (canvasGroup.alpha <= 0.9f)
                {
                    canvasGroup.alpha += 0.05f;
                    yield return new WaitForSeconds(0.02f);
                }
                canvasGroup.alpha = 1f;
                _fadeOperation = true;
            }
            else
            {
                while (canvasGroup.alpha >= 0.1f)
                {
                    canvasGroup.alpha -= 0.05f;
                    yield return new WaitForSeconds(0.02f);
                }
                canvasGroup.alpha = 0f;
                _fadeOperation = true;
                SceneManager.UnloadSceneAsync("LoaderScene");
            }
        }

    }
}
