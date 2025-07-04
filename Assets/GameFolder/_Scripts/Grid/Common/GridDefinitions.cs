namespace SKC.Grid
{
    public enum GridContentType
    {
        Nothing,     
        Empty,       
        Blocked,     
        PlayerStart, 
        Target       
    }

    [System.Serializable]
    public struct GridPosition
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(GridPosition a, GridPosition b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(GridPosition a, GridPosition b) => !(a == b);
        public override bool Equals(object obj) => obj is GridPosition other && this == other;
        public override int GetHashCode() => (x, y).GetHashCode();
        public override string ToString() => $"({x}, {y})";
    }
}
