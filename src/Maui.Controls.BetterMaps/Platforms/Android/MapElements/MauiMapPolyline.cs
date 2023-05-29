using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using APolyline = Android.Gms.Maps.Model.Polyline;
using Color = Android.Graphics.Color;

namespace Maui.Controls.BetterMaps.Android
{
    public class MauiMapPolyline : MauiMapElement<APolyline>, IMauiGeoPathMapElement
    {
        public MauiMapPolyline()
        {
        }

        public override APolyline AddToMap(GoogleMap map)
        {
            var options = new PolylineOptions();
            options.InvokeColor(Color);
            options.InvokeWidth(Width);
            // Will throw an exception when added to the map if Points is empty
            if (!Points.Any())
            {
                options.Points.Add(new LatLng(0, 0));
            }
            else
            {
                foreach (var i in Points)
                    options.Points.Add(i);
            }
            options.Visible(Visible);
            options.InvokeZIndex(ZIndex);

            WeakRef = new WeakReference<APolyline>(map.AddPolyline(options));
            return Element;
        }

        private Color _color;
        public Color Color
        {
            get => Element is not null ? new Color(Element.Color) : _color;
            set
            {
                _color = value;
                if (Element is not null)
                    Element.Color = value;
            }
        }

        private float _width;
        public float Width
        {
            get => Element?.Width ?? _width;
            set
            {
                _width = value;
                if (Element is not null)
                    Element.Width = value;
            }
        }

        private ObservableRangeCollection<LatLng> _points;
        public IList<LatLng> Points
        {
            get
            {
                if (_points is null)
                {
                    _points = new ObservableRangeCollection<LatLng>();
                    _points.CollectionChanged += (sender, args) =>
                    {
                        if (Element is not null)
                            Element.Points = new List<LatLng>(_points);
                    };
                }

                return _points;
            }
        }

        private bool _visible = true;
        public override bool Visible
        {
            get => Element?.Visible ?? _visible;
            set
            {
                _visible = value;
                if (Element is not null)
                    Element.Visible = value;
            }
        }

        private float _zIndex;
        public override float ZIndex
        {
            get => Element?.ZIndex ?? _zIndex;
            set
            {
                _zIndex = value;
                if (Element is not null)
                    Element.ZIndex = value;
            }
        }

        public override string Id => Element?.Id;

        public override void RemoveFromMap()
        {
            Element?.Remove();
            WeakRef = null;
        }

        public int ReplacePointsWith(IEnumerable<LatLng> points)
        {
            ((ObservableRangeCollection<LatLng>)Points).ReplaceRange(points);
            return Points.Count;
        }
    }
}