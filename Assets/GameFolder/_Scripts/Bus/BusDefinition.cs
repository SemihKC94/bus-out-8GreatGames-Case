using SKC.Grid; 
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SKC.Bus
{
    
    public enum BusColor
    {
        Blue,
        Red,
        Green,
        Yellow,
        Orange,
        Purple
    }
    
    [Serializable]
    public class BusPartData
    {
        public int busId;
        public BusPartType partType;
        public GridPosition position;
    }

    [Serializable]
    public class BusInfo
    {
        public int busId;
        public BusColor busColor = BusColor.Blue;
        public List<BusPartData> parts = new List<BusPartData>();
    }

    [Serializable]
    public class BusLevelData
    {
        public List<BusInfo> buses = new List<BusInfo>();
    }
    
    public static class BusColorConverter
    {
        public static Color ToUnityColor(BusColor color)
        {
            switch (color)
            {
                case BusColor.Blue:   return new Color(0.2f, 0.5f, 1f);
                case BusColor.Red:    return new Color(1f, 0.3f, 0.3f);
                case BusColor.Green:  return new Color(0.4f, 0.8f, 0.4f);
                case BusColor.Yellow: return new Color(1f, 0.9f, 0.2f); 
                case BusColor.Orange: return new Color(1f, 0.6f, 0.2f); 
                case BusColor.Purple: return new Color(0.7f, 0.4f, 0.9f);
                default:              return Color.white;
            }
        }
    }
}