using Android;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using AndroidX.Core.Content;
using System.Collections.Concurrent;

namespace BetterMaps.Maui.Android
{
    internal static class MauiMapViewExtensions
    {
        private static readonly ConcurrentDictionary<string, MapStyleOptions> MapStyles = new ConcurrentDictionary<string, MapStyleOptions>();

        internal static void UpdateTheme(this GoogleMap map, MapTheme mapTheme, Context context)
        {
            if (map is null)
                return;

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
                if (ActivityExtensions.MapThemeAssetNames.TryGetValue(mapTheme, out var assetName))
                {
                    if (!string.IsNullOrEmpty(assetName) && !MapStyles.ContainsKey(assetName))
                    {
                        var assets = context.Assets;
                        using var reader = new StreamReader(assets.Open(assetName));
                        MapStyles.AddOrUpdate(assetName, new MapStyleOptions(reader.ReadToEnd()), (k, v) => v);
                    }

                    map.SetMapStyle(MapStyles[assetName]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        internal static void UpdateType(this GoogleMap map, MapType type)
        {
            if (map is null)
                return;

            map.MapType = type switch
            {
                MapType.Street => GoogleMap.MapTypeNormal,
                MapType.Satellite => GoogleMap.MapTypeSatellite,
                MapType.Hybrid => GoogleMap.MapTypeHybrid,
                _ => throw new NotSupportedException($"Unknown map type '{type}'")
            };
        }

        internal static void UpdateIsShowingUser(this GoogleMap map, bool isShowingUser, Context context)
        {
            if (map is null)
                return;

            if (isShowingUser)
            {
                if (HasLocationPermission(context))
                {
                    map.MyLocationEnabled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MapHandler Missing location permissions for ShowUserLocation");
                    map.MyLocationEnabled = false;
                }
            }
            else
            {
                map.MyLocationEnabled = false;
            }
        }

        internal static void UpdateShowUserLocationButton(this GoogleMap map, bool showUserLocationButton, Context context)
        {
            if (map is null)
                return;

            if (showUserLocationButton)
            {
                if (HasLocationPermission(context))
                {
                    map.UiSettings.MyLocationButtonEnabled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MapHandler Missing location permissions for MyLocationButtonEnabled");
                    map.UiSettings.MyLocationButtonEnabled = false;
                }
            }
            else
            {
                map.UiSettings.MyLocationButtonEnabled = false;
            }
        }

        internal static void UpdateShowCompass(this GoogleMap map, bool showCompass)
        {
            if (map is null)
                return;
            map.UiSettings.CompassEnabled = showCompass;
        }

        internal static void UpdateHasScrollEnabled(this GoogleMap map, bool hasScrollEnabled)
        {
            if (map is null)
                return;
            map.UiSettings.ScrollGesturesEnabled = hasScrollEnabled;
        }

        internal static void UpdateHasZoomEnabled(this GoogleMap map, bool hasZoomEnabled)
        {
            if (map is null)
                return;
            map.UiSettings.ZoomGesturesEnabled = hasZoomEnabled;
        }

        internal static void UpdateTrafficEnabled(this GoogleMap map, bool trafficEnabled)
        {
            if (map is null)
                return;
            map.TrafficEnabled = trafficEnabled;
        }

        private static bool HasLocationPermission(Context context)
        {
            var coarseLocationPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessCoarseLocation);
            var fineLocationPermission = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation);
            return coarseLocationPermission == Permission.Granted || fineLocationPermission == Permission.Granted;
        }
    }
}
