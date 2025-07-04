using SKC.Boot;
using SKC.Events;
using SKC.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SKC.GUI
{
    public class UIController : MonoBehaviour
    {
        [Header("UI Components")]
        [Space,Header("Panels")]
        [SerializeField] private CanvasGroup gamePlay = null;
        [SerializeField] private CanvasGroup winScreen = null;
        [SerializeField] private CanvasGroup loseScreen = null;
        
        [Space,Header("Texts")]
        [SerializeField] private TextMeshProUGUI levelText = null;
        [SerializeField] private TextMeshProUGUI counterText = null;
        
        [Space,Header("Buttons")]
        [SerializeField] private Button nextLevelButton = null;
        [SerializeField] private Button lossLevelButton = null;
        
        [Space,Header("References")]
        [SerializeField] private Timer timer;
        [SerializeField] private LevelController  levelController = null;

        private void Awake()
        {
            gamePlay.alpha = 0.0f;
            gamePlay.interactable = false;
            gamePlay.blocksRaycasts = false;
            
            winScreen.alpha = 0.0f;
            winScreen.interactable = false;
            winScreen.blocksRaycasts = false;
            
            loseScreen.alpha = 0.0f;
            loseScreen.interactable = false;
            loseScreen.blocksRaycasts = false;
        }

        private void Start()
        {
            levelText.SetText("LEVEL " + (PlayerPrefs.GetInt("LEVEL") + 1).ToString());
            
            nextLevelButton.onClick.AddListener(() => LoadMainMenu());
            lossLevelButton.onClick.AddListener(() => LoadMainMenu());
        }

        private void OnEnable()
        {
            EventBroker.LevelEnd += CheckLevelEnd;
        }

        private void OnDisable()
        {
            EventBroker.LevelEnd -= CheckLevelEnd;
        }

        public void Initialize()
        {
            timer.ValueChanged += Countdown;
            
            gamePlay.alpha = 1.0f;
        }

        private void OnDestroy()
        {
            timer.ValueChanged -= Countdown;
        }

        private void LoadMainMenu()
        {
            levelController.LoadMainMenu();
        }
        
        private void Countdown(Timer.EventArgs e)
        {
            if (!e.IsComplete)
            {
                int minutes = Mathf.FloorToInt((e.MaxValue - e.Value) / 60F);
                int seconds = Mathf.FloorToInt((e.MaxValue - e.Value)  - minutes * 60);

                string niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);
                counterText.text = niceTime;
            }
        }

        private void CheckLevelEnd(bool success)
        {
            if (success)
            {
                int tempLevel = PlayerPrefs.GetInt("LEVEL");
                PlayerPrefs.SetInt("LEVEL", tempLevel + 1);
                
                winScreen.alpha = 1.0f;
                winScreen.interactable = true;
                winScreen.blocksRaycasts = true;
                return;
            }
            
            loseScreen.alpha = 1.0f;
            loseScreen.interactable = true;
            loseScreen.blocksRaycasts = true;
        }
    }
}
