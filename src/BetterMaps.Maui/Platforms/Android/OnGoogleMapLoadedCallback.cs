using Android.Gms.Maps;
using Android.Runtime;

namespace BetterMaps.Maui.Android
{
    internal class OnGoogleMapLoadedCallback : Java.Lang.Object, GoogleMap.IOnMapLoadedCallback
    {
        private Action _callback;
        private bool _disposed;

        public OnGoogleMapLoadedCallback()
        {
        }

        public OnGoogleMapLoadedCallback(Action callback)
        {
            _callback = callback;
        }

        public OnGoogleMapLoadedCallback(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public event EventHandler<EventArgs> OnGoogleMapLoaded;

        public void OnMapLoaded()
        {
            if (_disposed) return;

            OnGoogleMapLoaded?.Invoke(this, EventArgs.Empty);
            _callback?.Invoke();
        }


        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            _callback = null;
            base.Dispose(disposing);
        }
    }
}
