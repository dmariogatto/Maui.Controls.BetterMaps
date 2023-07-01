namespace BetterMaps.Maui
{
    public class MapSelectedPinChangedArgs
    {
        public Pin OldValue { get; }
        public Pin NewValue { get; }

        public MapSelectedPinChangedArgs(Pin oldValue, Pin newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}