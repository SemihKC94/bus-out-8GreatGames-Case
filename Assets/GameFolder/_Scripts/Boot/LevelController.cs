
using System.Collections;
using SKC.Bus;
using SKC.Events;
using SKC.GameLogic;
using SKC.Grid;
using SKC.GUI;
using UnityEngine;
using SKC.Level;
using SKC.Passenger;
using SKC.Helpers;

namespace SKC.Boot
{
    public class LevelController : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField ] private LevelData[] levelData;
        
        [Space,Header("References")]
        [SerializeField ] private  BusSpawner busSpawner;
        [SerializeField ] private  PassengerSpawner passengerSpawner;
        [SerializeField ] private  LevelGridGenerator gridSpawner;
        
        [Space,Header("References")]
        [SerializeField ] private  Timer levelTimer;
        [SerializeField ] private  CamSetter camSetter;
        [SerializeField ] private  UIController  uiController;
        
        // Privates
        private int currentLevel = 0;
        private int _finishedBusCount = 0;
        private int _totalBusCount = 0;
        
        private IEnumerator Start()
        {
            if (!PlayerPrefs.HasKey("LEVEL"))
            {
                PlayerPrefs.SetInt("LEVEL", 0);
                currentLevel = PlayerPrefs.GetInt("LEVEL");
            }
            else
            {
                currentLevel = PlayerPrefs.GetInt("LEVEL");
            }
            
            if(currentLevel >= levelData.Length) currentLevel = currentLevel % levelData.Length;
            
            passengerSpawner.Initalize(levelData[currentLevel].busData);
            yield return new WaitForSeconds(.1f);
            gridSpawner.Initialize(levelData[currentLevel].gridData, levelData[currentLevel].destinationData);
            yield return new WaitForSeconds(.1f);
            busSpawner.Initalize(levelData[currentLevel].busData);
            yield return new WaitForSeconds(.1f);
            camSetter.Initialize(levelData[currentLevel].CamPositionY);

            uiController.Initialize();
            
            levelTimer._duration = levelData[currentLevel].duration;
            
            BusLevelData busses = JsonUtility.FromJson<BusLevelData>(levelData[currentLevel].busData.text);


            foreach (BusInfo busInfo in busses.buses)
            {
                _totalBusCount++;
            }
        }

        private void OnEnable()
        {
            EventBroker.BusFinish += BusFinished;
            levelTimer.ValueChanged += Countdown;
        }

        private void OnDisable()
        {
            EventBroker.BusFinish -= BusFinished;
            levelTimer.ValueChanged -= Countdown;
        }

        private void BusFinished()
        {
            _finishedBusCount++;
            if (_finishedBusCount == _totalBusCount)
            {
                levelTimer.IsStop = true;
                EventBroker.OnLevelEnd(true);
            }
        }

        private void Countdown(Timer.EventArgs e)
        {
            if(e.IsComplete &&  _finishedBusCount < _totalBusCount) EventBroker.OnLevelEnd(false); 
        }

        public void LoadMainMenu()
        {
            StartCoroutine(LoadingController.Instance.GetScene("MenuScene","LevelScene"));
        }

        private void Update()
        {
            levelTimer.Tick();
        }
    }
}
