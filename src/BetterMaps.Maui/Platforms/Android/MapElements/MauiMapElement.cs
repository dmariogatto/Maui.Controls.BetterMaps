using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace BetterMaps.Maui.Android
{
    public interface IMauiMapElement
    {
        bool Visible { get; set; }

        float ZIndex { get; set; }

        string Id { get; }

        void RemoveFromMap();
    }

    public interface IMauiGeoPathMapElement : IMauiMapElement
    {
        IList<LatLng> Points { get; }

        int ReplacePointsWith(IEnumerable<LatLng> points);
    }

    public abstract class MauiMapElement<T> : IMauiMapElement where T : class
    {
        public MauiMapElement()
        {
        }

        protected WeakReference<T> WeakRef { get; set; }
        protected T Element => WeakRef?.TryGetTarget(out var e) == true ? e : null;

        public abstract T AddToMap(GoogleMap map);

        public abstract bool Visible { get; set; }

        public abstract float ZIndex { get; set; }

        public abstract string Id { get; }

        public abstract void RemoveFromMap();
    }
}