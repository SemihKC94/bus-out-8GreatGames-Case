using System;
using UnityEngine;

namespace SKC.Events
{
    public static class EventBroker
    {
        public static event Action LevelStart;
        public static void OnLevelStart()
        {
            LevelStart?.Invoke();
        }
        
        public static event Action<bool> LevelEnd;
        public static void OnLevelEnd(bool success)
        {
            LevelEnd?.Invoke(success);
        }

        public static event Action BusFinish;
        public static void OnBusFinish()
        {
            BusFinish?.Invoke();
        }
    }
}
