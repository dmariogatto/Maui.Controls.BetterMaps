using Android;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using AndroidX.Core.Content;
using System.Collections.Concurrent;

namespace Maui.Controls.BetterMaps.Android
{
    internal static class MauiMapViewExtensions
    {
        private static readonly ConcurrentDictionary<string, MapStyleOptions> MapStyles = new ConcurrentDictionary<string, MapStyleOptions>();

        internal static void UpdateTheme(this MauiMapView map, MapTheme mapTheme, Context context)
        {
            if (map?.GoogleMap is null) return;

            if (mapTheme == MapTheme.System)
            {
                var uiModeFlags = context.Resources.Configuration.UiMode & UiMode.NightMask;
                mapTheme = uiModeFlags switch
                {
                    UiMode.NightYes => MapTheme.Dark,
                    UiMode.NightNo => MapTheme.Light,
                    _ => throw new NotSupportedException($"UiMode {uiModeFlags} not supported"),
                };
            }

            try
            {
                if (MauiBetterMaps.AssetFileNames.TryGetValue(mapTheme, out var assetName))
                {
                    if (!string.IsNullOrEmpty(assetName) && !MapStyles.ContainsKey(assetName))
                    {
                        var assets = context.Assets;
                        using var reader = new StreamReader(assets.Open(assetName));
                        MapStyles.AddOrUpdate(assetName, new MapStyleOptions(reader.ReadToEnd()), (k, v) => v);
                    }

                    map.GoogleMap.SetMapStyle(MapStyles[assetName]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        internal static void UpdateType(this MauiMapView map, MapType type)
        {
            if (map?.GoogleMap is null) return;

            map.GoogleMap.MapType = type switch
            {
                MapType.Street => GoogleMap.MapTypeNormal,
                MapType.Satellite => GoogleMap.MapTypeSatellite,
                MapType.Hybrid => GoogleMap.MapTypeHybrid,
                _ => throw new NotSupportedException($"Unknown map type '{type}'")
            };
        }

        internal static void UpdateIsShowingUser(this MauiMapView map, bool isShowingUser, Context context)
        {
            if (map?.GoogleMap is null) return;

            if (isShowingUser)
            {
                if (HasLocationPermission(context))
                {
                    map.GoogleMap.MyLocationEnabled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MapHandler Missing location permissions for ShowUserLocation");
                    map.GoogleMap.MyLocationEnabled = false;
                }
            }
            else
            {
                map.GoogleMap.MyLocationEnabled = false;
            }
        }

        internal static void UpdateShowUserLocationButton(this MauiMapView map, bool showUserLocationButton, Context context)
        {
            if (map?.GoogleMap is null) return;

            if (showUserLocationButton)
            {
                if (HasLocationPermission(context))
                {
                    map.GoogleMap.UiSettings.MyLocationButtonEnabled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MapHandler Missing location permissions for MyLocationButtonEnabled");
                    map.GoogleMap.UiSettings.MyLocationButtonEnabled = false;
                }
            }
            else
            {
                map.GoogleMap.UiSettings.MyLocationButtonEnabled = false;
            }
        }

        internal static void UpdateShowCompass(this MauiMapView map, bool showCompass)
        {
            if (map?.GoogleMap is null) return;
            map.GoogleMap.UiSettings.CompassEnabled = showCompass;
        }

        internal static void UpdateHasScrollEnabled(this MauiMapView map, bool hasScrollEnabled)
        {
            if (map?.GoogleMap is null) return;
            map.GoogleMap.UiSettings.ScrollGesturesEnabled = hasScrollEnabled;
        }

        internal static void UpdateHasZoomEnabled(this MauiMapView map, bool hasZoomEnabled)
        {
            if (map?.GoogleMap is null) return;
            map.GoogleMap.UiSettings.ZoomGesturesEnabled = hasZoomEnabled;
        }

        internal static void UpdateTrafficEnabled(this MauiMapView map, bool trafficEnabled)
        {
            if (map?.GoogleMap is null) return;
            map.GoogleMap.TrafficEnabled = trafficEnabled;
        }

        private static bool HasLocationPermission(Context context)
        {
            var coarseLocationPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessCoarseLocation);
            var fineLocationPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation);
            return coarseLocationPermission == Permission.Granted || fineLocationPermission == Permission.Granted;
        }
    }
}
