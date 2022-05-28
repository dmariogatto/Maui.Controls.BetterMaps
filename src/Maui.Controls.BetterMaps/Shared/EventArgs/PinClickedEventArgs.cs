namespace Maui.Controls.BetterMaps
{
    public class PinClickedEventArgs : EventArgs
    {
        public Pin Pin { get; }

        public PinClickedEventArgs(Pin pin)
        {
            Pin = pin;
        }
    }
}