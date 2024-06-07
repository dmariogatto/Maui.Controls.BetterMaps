using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using APolygon = Android.Gms.Maps.Model.Polygon;
using Color = Android.Graphics.Color;

namespace BetterMaps.Maui.Android
{
    public class MauiMapPolygon : MauiMapElement<APolygon>, IMauiGeoPathMapElement
    {
        private bool _disposed;

        public MauiMapPolygon()
        {
        }

        public override APolygon AddToMap(GoogleMap map)
        {
            if (_disposed)
                return null;

            var options = new PolygonOptions();
            options.InvokeStrokeWidth(StrokeWidth);
            options.InvokeStrokeColor(StrokeColor);
            options.InvokeFillColor(FillColor);
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

            Element = map.AddPolygon(options);
            return Element;
        }

        private float _strokeWidth;
        public float StrokeWidth
        {
            get => Element?.StrokeWidth ?? _strokeWidth;
            set
            {
                _strokeWidth = value;
                if (Element is not null)
                    Element.StrokeWidth = value;
            }
        }

        private Color _strokeColor;
        public Color StrokeColor
        {
            get => Element is not null ? new Color(Element.StrokeColor) : _strokeColor;
            set
            {
                _strokeColor = value;
                if (Element is not null)
                    Element.StrokeColor = value;
            }
        }

        private Color _fillColor;
        public Color FillColor
        {
            get => Element is not null ? new Color(Element.FillColor) : _fillColor;
            set
            {
                _fillColor = value;
                if (Element is not null)
                    Element.FillColor = value;
            }
        }

        private ObservableRangeCollection<LatLng> _points;
        public IList<LatLng> Points
        {
            get
            {
                if (_disposed) return Array.Empty<LatLng>();

                if (_points is null)
                {
                    _points = new ObservableRangeCollection<LatLng>();
                    _points.CollectionChanged += PointsCollectionChanged;
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

        public int ReplacePointsWith(IEnumerable<LatLng> points)
        {
            (Points as ObservableRangeCollection<LatLng>)?.ReplaceRange(points);
            return Points?.Count ?? -1;
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                Element?.Dispose();
                Element = null;

                _points.CollectionChanged -= PointsCollectionChanged;
                _points.Clear();
                _points = null;
            }
        }

        private void PointsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Element is not null)
                Element.Points = new List<LatLng>(_points);
        }
    }
}