namespace BetterMaps.Maui.Sample;

public partial class MapPage : ContentPage
{
    private readonly string[] _imageUrls = new[]
    {
        "https://em-content.zobj.net/thumbs/120/microsoft/319/monkey_1f412.png",
        "https://em-content.zobj.net/thumbs/120/microsoft/319/dragon_1f409.png",
        "https://em-content.zobj.net/thumbs/120/microsoft/319/rooster_1f413.png",
        "https://em-content.zobj.net/thumbs/120/microsoft/319/tiger_1f405.png",
        "https://em-content.zobj.net/thumbs/120/microsoft/319/pig_1f416.png",
        "https://em-content.zobj.net/thumbs/120/microsoft/319/cow_1f404.png",
    };

    private readonly Color[] _colors = new[]
    {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
    };

    private bool _addingCircle;

    public MapPage()
    {
        InitializeComponent();
    }

    private void MauiMap_MapClicked(object sender, MapClickedEventArgs e)
    {
        if (_addingCircle)
        {
            _addingCircle = false;

            var circle = new Circle()
            {
                Center = new Position(e.Position.Latitude, e.Position.Longitude),
                Radius = Distance.FromKilometers(1),
                StrokeColor = Colors.LightSkyBlue,
                StrokeWidth = 8,
                FillColor = Colors.LightSkyBlue.WithAlpha(0.5f),
            };

            MauiMap.MapElements.Add(circle);
        }
        else
        {
            var pin = new Pin()
            {
                Label = $"Pin {MauiMap.Pins.Count + 1}",
                Position = e.Position,
            };

            MauiMap.Pins.Add(pin);
        }
    }

    private void MauiMap_MapLongClicked(object sender, MapClickedEventArgs e)
    {
        if (!_addingCircle)
        {
            var pin = new Pin()
            {
                Label = $"Pin {MauiMap.Pins.Count + 1}",
                Position = e.Position,
                ImageSource = new UriImageSource()
                {
                    Uri = new Uri(_imageUrls[Random.Shared.Next() % _imageUrls.Length])
                },
            };

            MauiMap.Pins.Add(pin);
        }
    }

    private void MauiMap_PinClicked(object sender, PinClickedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(MauiMap_PinClicked)}(\"{e.Pin.Label}\")");

        if (e.Pin.ImageSource is UriImageSource)
        {
            e.Pin.ImageSource = new UriImageSource()
            {
                Uri = new Uri(_imageUrls[Random.Shared.Next() % _imageUrls.Length])
            };
        }
        else
        {
            e.Pin.TintColor = _colors[Random.Shared.Next() % _colors.Length];
        }
    }

    private void MauiMap_InfoWindowClicked(object sender, PinClickedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(MauiMap_InfoWindowClicked)}(\"{e.Pin.Label}\")");

        var rnd = Random.Shared.Next(1000, 9999);
        e.Pin.Label = $"Hello there {rnd}";
        e.Pin.Address = $"Address {rnd}";
    }

    private void MauiMap_InfoWindowLongClicked(object sender, PinClickedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(MauiMap_InfoWindowLongClicked)}(\"{e.Pin.Label}\")");

        e.Pin.TintColor = null;
    }

    private async void OnToggleShowUserLocation(object sender, EventArgs e)
    {
        if (await Permissions.RequestAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted)
        {
            MauiMap.ShowUserLocationButton =
                MauiMap.IsShowingUser = !MauiMap.IsShowingUser;
        }
    }

    private void OnToggleCompass(object sender, EventArgs e)
    {
        MauiMap.ShowCompass = !MauiMap.ShowCompass;
    }

    private void OnCircleClicked(object sender, EventArgs e)
    {
        _addingCircle = true;
        DisplayAlertAsync("Map Element", "Tap on the map to add a circle!", "OK");
    }

    private void OnMoveToRegion(object sender, EventArgs e)
    {
        MauiMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Position(-53.165058, -70.910460),
                Distance.FromKilometers(10)));
    }

    private async void OnThemeClicked(object sender, EventArgs e)
    {
        var result = await DisplayActionSheetAsync("Theme", "Cancel", null, new[]
        {
            MapTheme.System.ToString(),
            MapTheme.Light.ToString(),
            MapTheme.Dark.ToString()

        });

        if (Enum.TryParse<MapTheme>(result, out var theme))
            MauiMap.MapTheme = theme;
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        _addingCircle = false;

        MauiMap.Pins.Clear();
        MauiMap.MapElements.Clear();
        MauiMap.ShowUserLocationButton = false;
        MauiMap.IsShowingUser = false;
        MauiMap.ShowCompass = false;
        MauiMap.MapTheme = MapTheme.System;
    }
}