using BetterMaps.Maui.iOS;
using CoreLocation;
using MapKit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapElementHandler : ElementHandler<IMapElement, IMKOverlay>
    {
        public event EventHandler<EventArgs> OnRecreateRequested;

        protected override IMKOverlay CreatePlatformElement()
            => VirtualView switch
            {
                Polyline polyline => MKPolyline.FromCoordinates(polyline.Geopath
                        .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                        .ToArray()),
                Polygon polygon => MKPolygon.FromCoordinates(polygon.Geopath
                        .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                        .ToArray()),
                Circle circle => MKCircle.Circle(
                        new CLLocationCoordinate2D(circle.Center.Latitude, circle.Center.Longitude),
                        circle.Radius.Meters),
                _ => throw new NotImplementedException()

            };

        public static MKOverlayRenderer GetViewForOverlay(MKMapView mapView, IMKOverlay overlay)
            => mapView?.Superview is MauiMapView mauiMapView
            ? overlay switch
            {
                MKPolyline mkPolyline => GetViewForPolyline(mkPolyline, mauiMapView.VirtualViewForOverlay(overlay) as Polyline),
                MKPolygon mkPolygon => GetViewForPolygon(mkPolygon, mauiMapView.VirtualViewForOverlay(overlay) as Polygon),
                MKCircle mkCircle => GetViewForCircle(mkCircle, mauiMapView.VirtualViewForOverlay(overlay) as Circle),
                _ => throw new NotImplementedException()
            }
            : null;

        public static void MapStroke(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement.Stroke is not SolidPaint)
                return;

            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        public static void MapStrokeThickness(IMapElementHandler handler, IMapElement mapElement)
        {
            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        public static void MapFill(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement is not IFilledMapElement filledMapElement)
                return;
            if (filledMapElement.Fill is not SolidPaint)
                return;

            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        public static void MapGeopath(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement is not IGeoPathMapElement)
                return;

            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        public static void MapRadius(IMapElementHandler handler, IMapElement mapElement)
        {
            if (handler.PlatformView is MKCircle && mapElement is ICircleMapElement)
                return;

            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        public static void MapCenter(IMapElementHandler handler, IMapElement mapElement)
        {
            if (handler.PlatformView is MKCircle && mapElement is ICircleMapElement)
                return;

            if (handler is MapElementHandler mapElementHandler)
                mapElementHandler.OnRecreateRequested?.Invoke(handler, EventArgs.Empty);
        }

        protected static MKPolylineRenderer GetViewForPolyline(MKPolyline mkPolyline, Polyline polyline)
            => mkPolyline is not null && polyline is not null
                ? new MKPolylineRenderer(mkPolyline)
                {
                    StrokeColor = polyline.StrokeColor.ToPlatform(Colors.Black),
                    LineWidth = polyline.StrokeWidth
                }
                : null;

        protected static MKPolygonRenderer GetViewForPolygon(MKPolygon mkPolygon, Polygon polygon)
            => mkPolygon is not null && polygon is not null
                ? new MKPolygonRenderer(mkPolygon)
                {
                    StrokeColor = polygon.StrokeColor.ToPlatform(Colors.Black),
                    FillColor = polygon.FillColor?.ToPlatform(),
                    LineWidth = polygon.StrokeWidth
                }
                : null;

        protected static MKCircleRenderer GetViewForCircle(MKCircle mkCircle, Circle circle)
            => mkCircle is not null && circle is not null
                ? new MKCircleRenderer(mkCircle)
                {
                    StrokeColor = circle.StrokeColor.ToPlatform(Colors.Black),
                    FillColor = circle.FillColor?.ToPlatform(),
                    LineWidth = circle.StrokeWidth
                }
                : null;
    }
}
