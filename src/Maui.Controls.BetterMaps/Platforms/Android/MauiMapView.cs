using Android.Content;
using Android.Gms.Maps;
using Android.Runtime;
using Android.Util;

namespace Maui.Controls.BetterMaps.Android
{
    public class MauiMapView : MapView, IOnMapReadyCallback
    {
        private bool _disposed;
        private IOnMapReadyCallback _onMapCallback;

        public MauiMapView(Context context) : base(context) { }
        public MauiMapView(Context context, GoogleMapOptions options) : base(context, options) { }
        public MauiMapView(Context context, IAttributeSet attrs) : base(context, attrs) { }
        public MauiMapView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
        public MauiMapView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) { }

        public event EventHandler<OnGoogleMapReadyEventArgs> OnGoogleMapReady;

        public GoogleMap GoogleMap { get; protected set; }

        public void GetMapAsync() => GetMapAsync(null);

        public override void GetMapAsync(IOnMapReadyCallback callback)
        {
            if (_disposed) return;

            _onMapCallback = callback;
            base.GetMapAsync(this);
        }

        public void OnMapReady(GoogleMap map)
        {
            if (_disposed) return;

            GoogleMap = map;

            _onMapCallback?.OnMapReady(map);
            OnGoogleMapReady?.Invoke(this, new OnGoogleMapReadyEventArgs(map));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _disposed = true;

                GoogleMap?.Dispose();
                GoogleMap = null;

                if (_onMapCallback is not null)
                {
                    _onMapCallback.Dispose();
                    _onMapCallback = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
