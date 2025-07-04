using SKC.Grid;
using SKC.Bus;
using System;
using System.Collections.Generic;

namespace SKC.GameLogic
{
    [Serializable]
    public class DestinationData
    {
        public int destinationId;
        public GridPosition position;
        public BusColor targetColor;
    }

    [Serializable]
    public class DestinationLevelData
    {
        public List<DestinationData> destinations = new List<DestinationData>();
    }
}