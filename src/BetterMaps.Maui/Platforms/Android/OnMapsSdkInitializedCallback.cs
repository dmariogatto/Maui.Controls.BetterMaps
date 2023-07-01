using Android.Gms.Maps;
using Android.Runtime;

namespace BetterMaps.Maui.Android
{
    public class OnGoogleMapsSdkInitializedEventArgs : EventArgs
    {
        public MapsInitializer.Renderer Renderer { get; private set; }

        public OnGoogleMapsSdkInitializedEventArgs(MapsInitializer.Renderer renderer)
        {
            Renderer = renderer;
        }
    }

    internal class OnMapsSdkInitializedCallback : Java.Lang.Object, IOnMapsSdkInitializedCallback
    {
        private bool _disposed;

        public OnMapsSdkInitializedCallback()
        {
        }

        public OnMapsSdkInitializedCallback(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public event EventHandler<OnGoogleMapsSdkInitializedEventArgs> OnGoogleMapsSdkInitialized;

        public void OnMapsSdkInitialized(MapsInitializer.Renderer renderer)
        {
            if (_disposed)
                return;

            OnGoogleMapsSdkInitialized?.Invoke(this, new OnGoogleMapsSdkInitializedEventArgs(renderer));
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
