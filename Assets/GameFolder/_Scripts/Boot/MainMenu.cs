using UnityEngine;
using UnityEngine.UI;

namespace SKC.Boot
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField ] private  Button PlayButton;

        private void Start()
        {
            PlayButton.onClick.AddListener(() => LoadLevel());
        }

        private void LoadLevel()
        {
            StartCoroutine(LoadingController.Instance.GetScene("LevelScene", "MenuScene"));
        }
    }
}
