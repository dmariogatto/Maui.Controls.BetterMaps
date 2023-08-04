using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace BetterMaps.Maui.Android
{
    public class MauiMapMarker : MauiMapElement<Marker>
    {
        public MauiMapMarker()
        {
        }

        public override Marker AddToMap(GoogleMap map)
        {
            var options = new MarkerOptions();
            options.SetTitle(Title);
            options.SetSnippet(Snippet);
            options.SetPosition(Position);
            options.SetAlpha(Alpha);
            options.Visible(Visible);
            options.Draggable(Draggable);
            options.InvokeZIndex(ZIndex);
            options.SetRotation(Rotation);
            options.Anchor(Anchor.u, Anchor.v);
            options.InfoWindowAnchor(InfoWindowAnchor.u, InfoWindowAnchor.v);
            options.SetIcon(Icon);

            WeakRef = new WeakReference<Marker>(map.AddMarker(options));
            return Element;
        }

        private string _title;
        public string Title
        {
            get => Element?.Title ?? _title;
            set
            {
                _title = value;
                if (Element is not null)
                    Element.Title = value;
            }
        }

        private string _snippet;
        public string Snippet
        {
            get => Element?.Snippet ?? _snippet;
            set
            {
                _snippet = value;
                if (Element is not null)
                    Element.Snippet = value;
            }
        }

        private LatLng _position;
        public LatLng Position
        {
            get => Element?.Position ?? _position;
            set
            {
                _position = value;
                if (Element is not null)
                    Element.Position = value;
            }
        }

        private float _alpha = 1f;
        public float Alpha
        {
            get => Element?.Alpha ?? _alpha;
            set
            {
                _alpha = value;
                if (Element is not null)
                    Element.Alpha = value;
            }
        }

        private bool _draggable;
        public bool Draggable
        {
            get => Element?.Draggable ?? _draggable;
            set
            {
                _draggable = value;
                if (Element is not null)
                    Element.Draggable = value;
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

        private float _rotation;
        public float Rotation
        {
            get => Element?.Rotation ?? _rotation;
            set
            {
                _rotation = value;
                if (Element is not null)
                    Element.Rotation = value;
            }
        }

        private (float u, float v) _anchor = (0.5f, 1.0f);
        public (float u, float v) Anchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
                Element?.SetAnchor(_anchor.u, _anchor.v);
            }
        }

        private (float u, float v) _infoWindowAnchor = (0.5f, 0.0f);
        public (float u, float v) InfoWindowAnchor
        {
            get => _infoWindowAnchor;
            set
            {
                _infoWindowAnchor = value;
                Element?.SetInfoWindowAnchor(_infoWindowAnchor.u, _infoWindowAnchor.v);
            }
        }

        private BitmapDescriptor _icon;
        public BitmapDescriptor Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                Element?.SetIcon(_icon);
            }
        }

        public override string Id => Element?.Id;

        public bool IsInfoWindowShown => Element?.IsInfoWindowShown ?? false;
        public void ShowInfoWindow() => Element?.ShowInfoWindow();
        public void HideInfoWindow() => Element?.HideInfoWindow();

        public override void RemoveFromMap()
        {
            Element?.Remove();
            WeakRef = null;
        }
    }
}