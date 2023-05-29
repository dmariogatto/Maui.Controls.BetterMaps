using Android.Gms.Maps;

namespace Maui.Controls.BetterMaps.Android
{
    public class OnGoogleMapReadyEventArgs : EventArgs
    {
        public GoogleMap Map { get; private set; }

        public OnGoogleMapReadyEventArgs(GoogleMap map)
        {
            Map = map;
        }
    }
}
