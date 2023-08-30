using Android.Gms.Maps.Model;
using BetterMaps.Maui.Android;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Handlers;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapElementHandler : ElementHandler<IMapElement, IMauiMapElement>
    {
        protected override IMauiMapElement CreatePlatformElement()
            => VirtualView switch
            {
                Polyline _ => new MauiMapPolyline(),
                Polygon _ => new MauiMapPolygon(),
                Circle _ => new MauiMapCircle(),
                _ => throw new NotImplementedException()
            };

        public static void MapStroke(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement.Stroke is not SolidPaint solidPaint)
                return;

            var platformColor = solidPaint.Color.AsColor();

            _ = handler.PlatformView switch
            {
                MauiMapPolyline polyline => polyline.Color = platformColor,
                MauiMapPolygon polygon => polygon.StrokeColor = platformColor,
                MauiMapCircle circle => circle.StrokeColor = platformColor,
                _ => throw new NotImplementedException()
            };
        }

        public static void MapStrokeThickness(IMapElementHandler handler, IMapElement mapElement)
        {
            var thickness = (float)mapElement.StrokeThickness;

            _ = handler.PlatformView switch
            {
                MauiMapPolyline polyline => polyline.Width = thickness,
                MauiMapPolygon polygon => polygon.StrokeWidth = thickness,
                MauiMapCircle circle => circle.StrokeWidth = thickness,
                _ => throw new NotImplementedException()
            };
        }

        public static void MapFill(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement is not IFilledMapElement filledMapElement)
                return;
            if (filledMapElement.Fill is not SolidPaint solidPaintFill)
                return;

            var platformColor = solidPaintFill.Color.AsColor();

            _ = handler.PlatformView switch
            {
                MauiMapPolygon polygon => polygon.FillColor = platformColor,
                MauiMapCircle circle => circle.FillColor = platformColor,
                _ => throw new NotImplementedException()
            };
        }

        public static void MapGeopath(IMapElementHandler handler, IMapElement mapElement)
        {
            if (mapElement is not IGeoPathMapElement geoPathMapElement)
                return;

            var points = geoPathMapElement.Select(p => new LatLng(p.Latitude, p.Longitude));

            _ = handler.PlatformView switch
            {
                IMauiGeoPathMapElement polyline => polyline.ReplacePointsWith(points),
                _ => throw new NotImplementedException()
            };
        }

        public static void MapRadius(IMapElementHandler handler, IMapElement mapElement)
        {
            if (handler.PlatformView is MauiMapCircle circle && mapElement is ICircleMapElement element)
                circle.Radius = element.Radius.Meters;
        }

        public static void MapCenter(IMapElementHandler handler, IMapElement mapElement)
        {
            if (handler.PlatformView is MauiMapCircle circle && mapElement is ICircleMapElement element)
                circle.Center = new LatLng(element.Center.Latitude, element.Center.Longitude);
        }
    }
}
