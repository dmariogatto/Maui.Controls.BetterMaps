namespace BetterMaps.Maui
{
    public class MapClickedEventArgs
    {
        public Position Position { get; }

        public MapClickedEventArgs(Position position)
        {
            Position = position;
        }
    }
}