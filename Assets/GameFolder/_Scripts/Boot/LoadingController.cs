using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SKC.Boot
{
    public class LoadingController : MonoBehaviour
    {
        private string currentLoaderSceneName = "LoaderScene";
        public static LoadingController Instance;

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

        public IEnumerator GetScene(string sceneToLoad, string sceneToUnload)
        {
            AsyncOperation loaderSceneAsyncLoad = SceneManager.LoadSceneAsync(currentLoaderSceneName, LoadSceneMode.Additive);
            
            yield return loaderSceneAsyncLoad; 

            StartCoroutine(LoadingScreen.Instance.LoadSceneWithTransition(sceneToLoad, sceneToUnload));
        }
    }
}
